using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;
using Amf;
using Address = System.UInt32;

#if PLATFORM_MONOTOUCH
using MonoTouch.Foundation;
using MonoTouch.UIKit;
#endif

namespace Telemetry
{
	internal sealed class Sampler : IDisposable
	{
		public Sampler(long timeBase, int divisor, int samplerRate, int maxCallstackDepth, int startDelay, int bufferLength = 8192)
		{
			mTimeBase     = timeBase;
			mDivisor      = divisor;
			mSamplerRate  = samplerRate;
			mMaxCallstackDepth = maxCallstackDepth;
			mStartDelay   = startDelay;

			// allocate buffers
			mReadData  =  new Address[bufferLength];
			mData      =  new Address[bufferLength];

			// allocate reusable sample class
			mSample = new Protocol.Sampler_sample();
			mSample.ticktimes = new _root.Vector<double>();
			mSample.callstack = new _root.Vector<uint>();
			mSample.callstack.length = (uint)maxCallstackDepth;
			mSample.callstack.length = 0;

			// save target thread id
			mTargetThread = mach_thread_self();

			// setup architecture specific settings
			var arch = flash.system.Capabilities.cpuArchitecture;
			switch (arch)
			{
			case "ARM":
				mThreadStateLength = 17;
				mThreadStateFlavor = 1;
				mRegisterPC = 15;
				mRegisterBP = 7;
				break;

			case "x86":
				mThreadStateLength = 32;
				mThreadStateFlavor = 1;
				mRegisterPC = 10;
				mRegisterBP = 6;
				break;

			default:
				Console.WriteLine("Telemetry: Architecture not supported for sampling");
				return;
			}

			// start sampler thread
			mSamplerThread = new Thread(SamplerThreadFunc);
			mSamplerThread.Priority = ThreadPriority.Highest;
			mSamplerThread.Start ();
			Console.WriteLine("Telemetry: Sampler started rate: {0} ms", samplerRate);
		}

		// stops sampling and cleans up
		public void Dispose()
		{
			if (mSamplerThread == null) {
				Console.WriteLine("Telemetry: Stopping sampler...");
				mRunSampler = false;
				mSamplerThread.Join();
				mSamplerThread = null;
				Console.WriteLine("Telemetry: Sampler stopped.");
			}
		}

		// returns accumulated sampler data in a buffer
		// format:
		//    <call-stack-length>
		//    <timestamp>
		//    <call-stack-addr0> 
		//          .....
		//    <call-stack-addrN> 
		//   (repeat until zero-length callstack)
		public Address[] GetSamplerData()
		{
			lock (mSyncRoot) {
				// swap data with last data
				var data = mData;
				var readData = mReadData;
				mReadData  = data;
				mData      = readData;

				// reset count
				mData[0]   = (Address)0;
				mDataCount = 0;

				// return data to be read
				return mReadData;
			}
		}

		// compares two callstacks in their raw form
		private static bool ArrayEquals(Address[] data, int indexA, int indexB, int count)
		{
			for (int i=0; i < count; i++) {
				if (data[indexA++] != data[indexB++]) {
					return false;
				}
			}
			return true;
		}

		// write sampler data to AMF 
		public void Write(MethodMap methodMap, bool combineSamples = true)
		{
			// get accumulated sampler data
			Address[] data = GetSamplerData();

			// AMF serializable sample to write to
			Protocol.Sampler_sample sample = mSample;

			int lastCallStackIndex = 0;
			int lastCallStackCount = 0;

			// process all samples
			// this code is tricky because it tries to combine consecutive samples with the exact same callstack
			int sampleCount = 0;
			int index = 0;
			for (;;) {
				// get length of callstack
				int count = (int)data[index++];
				if (count == 0)
					break;

				// get sample time
				int time = (int)data[index++];

				// compare the last callstack with this one
				// if they are equal, the samples can be combined
				// else we have to start a new sample
				if (!combineSamples || (lastCallStackCount != count) || !ArrayEquals(data, index, lastCallStackIndex, count)) {
					// call stack is different... 

					if (sample.numticks > 0) {
						// write last sample to log
						Session.WriteValueImmediate(sNameSamplerSample, sample);
						// reset sample for new callstack
						sample.numticks         = 0;
						sample.ticktimes.length = 0;
					}

					// translate callstack to method ids
					var callstack = sample.callstack;
					callstack.length = 0;
					for (int i=0; i < count; i++) {
						bool topOfStack;
						uint methodId = methodMap.GetMethodId(data[index + i], out topOfStack, true );
						// add method id
						callstack.push(methodId);
						// abort callstack if we are at a "top of stack" method
						if (topOfStack) {
							break;
						}
					}

					// save last callstack position
					lastCallStackIndex = index;
					lastCallStackCount = count;
				} 

				// add tick to sample
				sample.numticks++;
				sample.time      = time;
				sample.ticktimes.push((double)time);

				// advance to next sample
				index += count;
				sampleCount++;
			}

			if (sample.numticks > 0) {
				// write last sample to log
				Session.WriteValueImmediate(sNameSamplerSample, sample);
				// reset sample for new callstack
				sample.numticks         = 0;
				sample.ticktimes.length = 0;
			}

			if (sampleCount > 0) {
				// TODO:
				Session.WriteValue(".sampler.medianInterval", 1000);
				Session.WriteValue(".sampler.averageInterval", 1000);
				Session.WriteValue(".sampler.maxInterval", 1000);
			}

		}

		#region Private
		[DllImport ("__Internal", EntryPoint="mach_thread_self")]
		extern static IntPtr mach_thread_self();

		[DllImport ("__Internal", EntryPoint="thread_suspend")]
		extern static int thread_suspend (IntPtr thread);

		[DllImport ("__Internal", EntryPoint="thread_resume")]
		extern static int thread_resume (IntPtr thread);

		[DllImport ("__Internal", EntryPoint="thread_get_state")]
		extern static int thread_get_state (IntPtr thread, int flavor, IntPtr[] state, ref int stateCount);

		[DllImport ("__Internal", EntryPoint="mono_thread_info_suspend_lock")]
		extern static void mono_thread_info_suspend_lock();

		[DllImport ("__Internal", EntryPoint="mono_thread_info_suspend_unlock")]
		extern static void mono_thread_info_suspend_unlock();

		// this walks a callstack starting with the provided frame pointer and program counter
		private static unsafe int BacktraceCallStack(IntPtr pc, IntPtr bp, Address[] callStack)
		{
			int i = 0;

			// write pc to buffer
			callStack[i++] = (Address)pc;

			// get pointer to top stack frame
			IntPtr *frame = (IntPtr *)bp.ToPointer();
			if (frame == null)
			{
				return i;
			}

			// process all stack frames
			while (i < callStack.Length)
			{
				// get pointer to previous frame
				IntPtr *previous = (IntPtr *)frame[0].ToPointer();
				if (previous == null)
				{
					break;
				}

				// write caller address to buffer
				callStack[i++] = (Address)frame[1];

				// go to previous frame
				frame = previous;
			}

			return i;
		}

		// this captures a single sample from the target thread and writes information to the mData log
		private bool CaptureSample(IntPtr[] state, Address[] callStack)
		{
			int count = 0;
			long time = 0;

			// suspend thread
#if PLATFORM_MONOTOUCH
			mono_thread_info_suspend_lock();
#endif
			thread_suspend(mTargetThread);

			try {
				// compute time since we started sampling
				time = (Stopwatch.GetTimestamp() - mTimeBase);

				// get thread state
				int stateCount = state.Length;
				int result = thread_get_state(mTargetThread, mThreadStateFlavor, state, ref stateCount);
				if (result != 0) {
					// could not get thread state
					return false;
				}

				// get pc from thread state
				IntPtr pc = state[mRegisterPC];
				// get frame pointer from thread state
				IntPtr bp = state[mRegisterBP];
				// trace callstack for suspended thread
				count = BacktraceCallStack(pc, bp, callStack);
				if (count == 0) {
					// no callstack data found
					return false;
				}
			} finally {
				// resume thread always
				thread_resume(mTargetThread);
#if PLATFORM_MONOTOUCH
				mono_thread_info_suspend_unlock();
#endif
			}

			// lock for writing to internal data log
			// TODO: we could use a lockless queue but this is only called ~16 times per frame
			lock (mSyncRoot)
			{
				// ensure we have enough space
				int length = 2 + count;
				if ((mDataCount + length + 1) >= mData.Length) {
					// buffer is full? no one is polling us?
					return false;
				}

				// write sample to log

				// write count
				mData[mDataCount++] = (Address)count;

				// write time converted to desired resolution
				mData[mDataCount++] = (Address)(int)(time / mDivisor);

				// copy call stack data
				Array.Copy(callStack, 0, mData, mDataCount, count);
				mDataCount += count;

				// null terminate
				mData[mDataCount] = (Address)0;
			}

			// success
			return true;
		}

		private void SamplerThreadFunc()
		{
			// wait before doing any work
			Thread.Sleep(mStartDelay);

			// allocate buffers here so they can be reused by capture code
			var state = new IntPtr[mThreadStateLength];
			var callStack = new Address[mMaxCallstackDepth];

			int errorCount = 0;
			while (mRunSampler)
			{
				try {
					// capture a sample
					if (CaptureSample(state, callStack)) {
						// success, clear error count
						errorCount = 0;
					} else {
						// error capturing
						errorCount++;
					}
				}
				catch {
					// there was an exception while capturing
					errorCount++;
				}

				if (errorCount == 0) {
					// sleep for sample period
					Thread.Sleep(mSamplerRate);
				} else {
					// we had errors, so back off
					Thread.Sleep(1000);
				}
			}
		}

		// object for locking
		private readonly object 	  mSyncRoot = new System.Object();

		// sampler thread
		private bool				  mRunSampler =  true;
		private Thread          	  mSamplerThread;

		// sampler data
		private int 				  mDataCount;
		private Address[]		 	  mData;

		// this buffer is provided to the caller for reading
		private Address[]		 	  mReadData;

		// reusable AMF sample
		private readonly Protocol.Sampler_sample mSample = new Protocol.Sampler_sample();

		// settings
		private readonly IntPtr 	  mTargetThread;
		private readonly int     	  mSamplerRate;
		private readonly long 		  mTimeBase;
		private readonly int 		  mDivisor;
		private readonly int 		  mMaxCallstackDepth;

		// arch specific settings
		private readonly int 		  mThreadStateFlavor;
		private readonly int 		  mThreadStateLength;
		private readonly int 		  mRegisterPC;
		private readonly int 		  mRegisterBP;

		// start delay in milliseconds
		private readonly int    	  mStartDelay;

		private static readonly Amf3String     sNameSamplerSample  = new Amf3String(".sampler.sample");
		#endregion
	}
}

