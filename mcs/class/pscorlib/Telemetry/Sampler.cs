using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;
using Amf;

#if PLATFORM_MONOTOUCH
using MonoTouch.Foundation;
using MonoTouch.UIKit;
#endif

namespace Telemetry
{
	public sealed class Sampler : IDisposable
	{
		enum Architecture
		{
			Unknown,
			x86,
			ARM
		};

		static Architecture GetArchitecture()
		{
			#if PLATFORM_MONOMAC
			return Architecture.x86;
			#elif PLATFORM_MONOTOUCH
			if (PlayScript.IOSDeviceHardware.Version.ToString().Contains("Simulator")) {
				return Architecture.x86;
			} else {
				return Architecture.ARM;
			}
			#else
			return Architecture.Unknown;
			#endif
		}

		public Sampler(long timeBase, int divisor, int samplerRate, int maxCallstackDepth, int bufferLength = 8192)
		{
			mArchitecture = GetArchitecture();
			mTimeBase     = timeBase;
			mDivisor      = divisor;
			mSamplerRate  = samplerRate;
			mMaxCallstackDepth = maxCallstackDepth;

			// allocate buffers
			mReadData  =  new IntPtr[bufferLength];
			mData      =  new IntPtr[bufferLength];

			// save target thread id
			mTargetThread = mach_thread_self();

			// start sampler thread
			mSamplerThread = new Thread(SamplerThreadFunc);
			mSamplerThread.Start ();

			Console.WriteLine("Telemetry: Sampler started rate: {0} ms", samplerRate);
		}

		#region IDisposable implementation
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
		#endregion

		// returns accumulated sampler data in a buffer
		// format:
		//    <call-stack-length>
		//    <timestamp>
		//    <call-stack-addr0> 
		//          .....
		//    <call-stack-addrN> 
		//   (repeat until zero-length callstack)
		public IntPtr[] GetSamplerData()
		{
			lock (mSyncRoot) {
				// swap data with last data
				var data = mData;
				var readData = mReadData;
				mReadData  = data;
				mData      = readData;

				// reset count
				mData[0]   = (IntPtr)0;
				mDataCount = 0;

				// return data to be read
				return mReadData;
			}
		}

		// write sampler data to AMF 
		public void Write(Amf3Writer output, MethodMap methodMap)
		{
			// get sampler data
			IntPtr[] data = GetSamplerData();

			// process all samples
			int sampleCount = 0;
			int index = 0;
			double[] ticktimes = new double[1];
			for (;;) {
				// get length of callstack
				int count = data[index++].ToInt32();
				if (count == 0)
					break;

				// get time in microseconds since startup
				int time = data[index++].ToInt32();

				// only one tick at a time for now
				int numticks = 1;
				ticktimes[0] = (double)time;

				// lookup address to method ids
				uint[] callstack = new uint[count];
				for (int i=0; i < count; i++) {
					callstack[i] = methodMap.GetMethodId(data[index++], true );
				}

				// write sample as value
				output.WriteObjectHeader(Protocol.Value.ClassDef);
				output.Write(sNameSamplerSample);

				output.WriteObjectHeader(Protocol.Sampler_sample.ClassDef);
				output.Write(time + 4);
				output.Write(numticks);
				output.Write(ticktimes);
				output.Write(callstack);
				sampleCount++;
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

		private static unsafe int BacktraceCallStack(IntPtr pc, IntPtr bp, IntPtr[] callStack)
		{
			int i = 0;

			// write pc to buffer
			callStack[i++] = pc;

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
				callStack[i++] = frame[1];

				// go to previous frame
				frame = previous;
			}

			return i;
		}

		private void SamplerThreadFunc()
		{
			int threadStateFlavor;
			int threadStateLength;
			int	registerPC;
			int	registerBP;

			switch (mArchitecture)
			{
			case Architecture.ARM:
				// ARM
				threadStateLength = 17;
				threadStateFlavor = 1;
				registerPC = 15;
				registerBP = 7;
				break;

			case Architecture.x86:
				// x86
				threadStateLength = 32;
				threadStateFlavor = 1;
				registerPC = 10;
				registerBP = 6;
				break;

			default:
				return;
			}

			// wait before doing any work
			Thread.Sleep(250);

			var state = new IntPtr[threadStateLength];
			var callStack = new IntPtr[mMaxCallstackDepth];
			while (mRunSampler)
			{
				int count = 0;

				// suspend thread
				thread_suspend(mTargetThread);

				// compute time since we started sampling
				long timelong = Stopwatch.GetTimestamp() - mTimeBase;
				int time = (int)(timelong / mDivisor);

				// get thread state
				int stateCount = state.Length;
				int result = thread_get_state (mTargetThread, threadStateFlavor, state, ref stateCount);
				if (result == 0) {
					// get pc from thread state
					IntPtr pc = state [registerPC];
					// get frame pointer from thread state
					IntPtr bp = state [registerBP];
					// trace callstack for suspended thread
					count = BacktraceCallStack (pc, bp, callStack);
				}

				// resume thread
				thread_resume (mTargetThread);

				// did we get any callstack data?
				if (count > 0)
				{
					lock (mSyncRoot)
					{
						// write callstack to log
						int length = 2 + count;
						if ((mDataCount + length + 1) < mData.Length) {
						
							// write count
							mData[mDataCount++] = (IntPtr)count;

							// write time
							mData[mDataCount++] = (IntPtr)time;

							// copy call stack data
							Array.Copy(callStack, 0, mData, mDataCount, count);
							mDataCount += count;

							// null terminate
							mData[mDataCount] = (IntPtr)0;
						}
					}
				}

				// sleep 
				Thread.Sleep(mSamplerRate);
			}
		}

		// object for locking
		private readonly object 	  mSyncRoot = new System.Object();

		// sampler thread
		private bool				  mRunSampler =  true;
		private Thread          	  mSamplerThread;

		// sampler data
		private int 				  mDataCount;
		private IntPtr[]		 	  mData;

		// this buffer is provided to the caller for reading
		private IntPtr[]		 	  mReadData;

		// settings
		private readonly Architecture mArchitecture;
		private readonly IntPtr 	  mTargetThread;
		private readonly int     	  mSamplerRate;
		private readonly long 		  mTimeBase;
		private readonly int 		  mDivisor;
		private readonly int 		  mMaxCallstackDepth;

		private static readonly Amf3String     sNameSamplerSample  = new Amf3String(".sampler.sample");
		#endregion
	}
}

