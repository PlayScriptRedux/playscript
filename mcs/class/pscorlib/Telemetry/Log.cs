using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using Amf;

namespace Telemetry
{
	// class for efficiently logging telemetry events and writing them to AMF stream
	internal class Log
	{
		public const long EntryValue = -2;
		public const long EntryTime = -1;

		public long StartTime { get { return mLogStartTime; } }
		public int 	Divisor   { get { return mDivisor; } }

		public Log(Stream stream, bool autoCloseStream = true, int capacity = 1 * 1024)
		{
			mStream = stream;
			mAutoCloseStream = autoCloseStream;

			// allocate log buffer (this can grow)
			mLog   = new LogEntry[capacity];
			mCount = 0;

			// create AMF writer from stream
			mOutput = new Amf3Writer(stream);
			mOutput.TrackArrayReferences = false;

			// set log start time
			mLogStartTime = Stopwatch.GetTimestamp();

			// set log timebase (in microseconds)
			mTimeBase = ToMicroSeconds(mLogStartTime);
		}

		public void Close()
		{
			// flush stream
			Flush();

			// close session stream
			if (mAutoCloseStream) {
				mStream.Close();
			}
		}

		// gets the time in microseconds since the log was started
		public int GetTime()
		{
			return ToMicroSeconds(Stopwatch.GetTimestamp() - mLogStartTime);
		}

		// enqueues a log entry
		// this is overloaded to handle span, spanvalue, value, and time entries
		public void AddEntry(long time, long span, object name, object value)
		{
			if (mCount >= mLog.Length) {
				// grow geometrically
				int newLength = mLog.Length * 2;
				var newLog = new LogEntry[newLength];
				Array.Copy(mLog, newLog, mLog.Length);
				mLog = newLog;
			}

			// add entry to log
			int i = mCount++;
			mLog[i].Time = time;
			mLog[i].Span = span;
			mLog[i].Name = name;
			mLog[i].Value = value;
		}

		// writes a value to the log immediately, flushing any pending entries if needed
		// the value object is free to be reused after writing via this method
		public void WriteValueImmediate(object name, object value)
		{
			// flush all entries before writing
			FlushEntries();

			// write header
			mOutput.WriteObjectHeader(Protocol.Value.ClassDef);
			mOutput.Write(name);
			mOutput.Write(value);
		}

		public void Flush()
		{
			FlushEntries();
			mStream.Flush();
		}


		#region Private
		struct LogEntry
		{
			public long 	Time;		// time of entry (in ticks)
			public long 	Span;		// span length (in ticks) for Span and SpanValue (could also be EntryValue or EntryTime)
			public object 	Name;		// string or Amf3String
			public object	Value;		// non-null if SpanValue or Value
		};


		private int ToMicroSeconds(long ticks)
		{
			// NOTE: this requires a 64-bit division on ARM
			return (int)(ticks / mDivisor);
		}

		// writes a log entry to the AMF stream
		private void WriteLogEntry(ref LogEntry entry)
		{
			if (entry.Span == EntryValue) {
				// emit Value
				mOutput.WriteObjectHeader(Protocol.Value.ClassDef);
				mOutput.Write(entry.Name);
				mOutput.Write(entry.Value);
			} else 	if (entry.Span == EntryTime) {
				// emit Time
				int time = ToMicroSeconds(entry.Time);
				int delta = time - mTimeBase; 
				mTimeBase = time;

				mOutput.WriteObjectHeader(Protocol.Time.ClassDef);
				mOutput.Write(entry.Name);
				mOutput.Write(delta);
			} else {
				// emit Span or SpanValue
				// convert times to microseconds for output
				int time      = ToMicroSeconds(entry.Time);
				int beginTime = ToMicroSeconds(entry.Time - entry.Span);

				// compute span and delta in microseconds
				// this must be done this exact way to preserve rounding errors across spans
				// if not, the server may produce an error if a span exceeds its expected length
				int span  = time - beginTime;
				int delta = time - mTimeBase; 
				mTimeBase = time;

				if (entry.Value == null) {
					mOutput.WriteObjectHeader(Protocol.Span.ClassDef);
					mOutput.Write(entry.Name);
					mOutput.Write(span);
					mOutput.Write(delta);
				} else {
					mOutput.WriteObjectHeader(Protocol.SpanValue.ClassDef);
					mOutput.Write(entry.Name);
					mOutput.Write(span);
					mOutput.Write(delta);
					mOutput.Write(entry.Value);
				}
			}
		}

		// writes all enqueued log entries to the AMF stream
		private void FlushEntries()
		{
			for (int i=0; i < mCount; i++) {
				WriteLogEntry(ref mLog[i]);
			}
			// clear log count
			mCount = 0;
		}

		// fast intermediate log used for storing data within a frame as array of packed structs
		// this gets flushed to the active AMF stream each frame
		private LogEntry[]	  mLog;
		private int 		  mCount = 0;
		private int 		  mTimeBase;

		private readonly int  mDivisor = (int)(Stopwatch.Frequency / Session.Frequency);
		private readonly long mLogStartTime;

		private readonly Amf3Writer mOutput;
		private readonly Stream     mStream;
		private readonly bool       mAutoCloseStream;
		#endregion
	}
}

