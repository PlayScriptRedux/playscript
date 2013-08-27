using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;

namespace Telemetry
{
	// this stream will multiplex write to multiple streams (network, file, memory, etc) at the same time
	// unfortunately Telemetry will disconnect if it receives a partial AMF object so care must be taken
	// to flush the stream at regular intervals, between AMF objects and without exceeding the MTU
	public sealed class SessionStream : System.IO.Stream
	{
		public SessionStream(int bufferSize, int flushThreshold)
		{
			mBuffer      	= new byte[bufferSize];
			mFlushThreshold = flushThreshold;
		}

		public int StreamCount 
		{
			get { return mStreamCount;}
		}

		public void AddStream(Stream stream)
		{
			if (mTotalData > 0)
				throw new InvalidOperationException("Can't add stream once data has been written");

			mStreams[mStreamCount++] = stream;
		}

		public void FlushBoundary()
		{
			// flush if buffered data exceeds the the flush threshold
			if (mCount > mFlushThreshold) {
				Flush();
			}
		}

		#region implemented abstract members of Stream

		public override void Close()
		{
			base.Close();

			for (int i=0; i < mStreamCount; i++) {
				try 
				{
					mStreams[i].Close();
					mStreams[i] = null;
				}
				catch 
				{
				}
			}
			mStreamCount = 0;
		}

		private void WriteMulti(byte[] buffer, int offset, int count)
		{
			for (int i=0; i < mStreamCount; ) {
				try 
				{
					mStreams[i].Write(buffer, offset, count);

					i++;
				} 
				catch 
				{
					// there was an error with this stream
					// close it and remove it from the list
					mStreams[i].Close();

					// remove it by swapping with the last stream
					mStreams[i] = mStreams[mStreamCount - 1];
					mStreams[mStreamCount - 1] = null;

					// decrement stream count
					mStreamCount--;
				}
			}
		}

		public override void Flush()
		{
			if (mCount > 0) {
				WriteMulti(mBuffer, 0, mCount);
			}

			// clear index
			mCount = 0;
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			throw new NotImplementedException();
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotImplementedException();
		}

		public override void SetLength(long value)
		{
			throw new NotImplementedException();
		}

		public override void WriteByte(byte value)
		{
			if (mStreamCount == 0)
				return;

			// buffer network writes
			if (mCount >= mBuffer.Length) 
			{
				// flush to output
				Flush();
			}

			// buffer output
			mBuffer[mCount++] = value;
			mTotalData++;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			if (mStreamCount == 0)
				return;

			// buffer network writes
			if ((mCount + count) > mBuffer.Length)  
			{ 
				// flush to output
				Flush();

				// write the rest directly to output
				WriteMulti(buffer, offset, count);

			} 
			else 
			{
				// copy into buffer
				Buffer.BlockCopy(buffer, offset, mBuffer, mCount, count);
				mCount += count;
			}

			mTotalData += count;
		}

		public override bool CanRead {
			get {
				return false;
			}
		}

		public override bool CanSeek {
			get {
				return false;
			}
		}

		public override bool CanWrite {
			get {
				return true;
			}
		}

		public override long Length {
			get {
				return mTotalData;
			}
		}

		public override long Position {
			get {
				return mTotalData;
			}
			set {
			}
		}

		#endregion

		// output streams
		private int 						mStreamCount;
		private readonly Stream[] 			mStreams = new Stream[8];

		// buffering
		private byte[]		mBuffer;
		private int 		mCount;
		private int 		mFlushThreshold;
		private long 		mTotalData;
	}
}

