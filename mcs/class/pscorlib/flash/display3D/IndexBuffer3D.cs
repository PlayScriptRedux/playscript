// Copyright 2013 Zynga Inc.
//	
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//		
//      Unless required by applicable law or agreed to in writing, software
//      distributed under the License is distributed on an "AS IS" BASIS,
//      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//      See the License for the specific language governing permissions and
//      limitations under the License.

using System;
using flash.utils;
using _root;

#if PLATFORM_MONOMAC
using MonoMac.OpenGL;
using BufferUsage = MonoMac.OpenGL.BufferUsageHint;
#elif PLATFORM_MONOTOUCH
using OpenTK.Graphics.ES20;
#elif PLATFORM_MONODROID
using OpenTK.Graphics.ES20;
using BufferTarget = OpenTK.Graphics.ES20.All;
using BufferUsage = OpenTK.Graphics.ES20.All;
#endif

namespace flash.display3D {

	public class IndexBuffer3D {

		//
		// Methods
		//

#if OPENGL

		internal IndexBuffer3D(Context3D context3D, int numIndices, int multiBufferCount, bool isDynamic)
		{
			if (multiBufferCount < 1)
				throw new ArgumentOutOfRangeException("multiBufferCount");

			mContext = context3D;
			mNumIndices = numIndices;
			mIds = new uint[multiBufferCount];
			GL.GenBuffers(multiBufferCount, mIds);

			mUsage = isDynamic ? BufferUsage.DynamicDraw : BufferUsage.StaticDraw;

			// update stats
			mContext.statsIncrement(Context3D.Stats.Count_IndexBuffer);
		}

		public void dispose() {
			GL.DeleteBuffers(mIds.Length, mIds);

			// update stats
			mContext.statsDecrement(Context3D.Stats.Count_IndexBuffer);
			mContext.statsSubtract(Context3D.Stats.Mem_IndexBuffer, mMemoryUsage);
			mMemoryUsage = 0;
		}

		public unsafe void uploadFromPointer(void *data, int dataLength, int startOffset, int count) {
			// swap to next buffer
			mBufferIndex++;
			if (mBufferIndex >= mIds.Length)
				mBufferIndex = 0;

			int byteCount = count * sizeof(uint);
			// bounds check
			if (byteCount > dataLength)
				throw new ArgumentOutOfRangeException("data buffer is not big enough for upload");

			GL.BindBuffer(BufferTarget.ElementArrayBuffer, mIds[mBufferIndex]);
			if (startOffset == 0) {
				GL.BufferData(BufferTarget.ElementArrayBuffer, 
				             new IntPtr(byteCount), 
				             new IntPtr(data), 
				             mUsage);

				if (byteCount != mMemoryUsage) {
					// update stats for memory usage
					mContext.statsAdd(Context3D.Stats.Mem_IndexBuffer, byteCount - mMemoryUsage);
					mMemoryUsage = byteCount;
				}
			} else {
				// update range of index buffer
				GL.BufferSubData(BufferTarget.ElementArrayBuffer,
				                 new IntPtr(startOffset * sizeof(uint)),
				                 new IntPtr(byteCount), 
				                 new IntPtr(data));
			}

		}

		public unsafe void uploadFromByteArray(ByteArray data, int byteArrayOffset, int startOffset, int count) {
			// pin pointer to byte array data
			fixed (byte *ptr = data.getRawArray()) {
				uploadFromPointer(ptr + byteArrayOffset, (int)(data.length - byteArrayOffset), startOffset, count);
			}
		}

		public unsafe void uploadFromArray(uint[] data, int startOffset, int count) {
			// pin pointer to array data
			fixed (uint *ptr = data) {
				uploadFromPointer(ptr, data.Length * sizeof(uint), startOffset, count);
			}
		}

		public void uploadFromVector(Vector<uint> data, int startOffset, int count) {
			uploadFromArray(data._GetInnerArray(), startOffset, count);
		}
		
		public uint 			id 			{get {return mIds[mBufferIndex];}}
		public int				numIndices 	{get {return mNumIndices;}}

		private readonly Context3D mContext;
		private readonly int	mNumIndices;
		private uint[]			mIds;
		private int 			mBufferIndex;		// buffer index for multibuffering
		private BufferUsage     mUsage;
		private int 			mMemoryUsage;


#else

		public IndexBuffer3D(Context3D context3D, int numIndices, int multiBufferCount = 1)
		{
			throw new NotImplementedException();
		}
		
		public void dispose() {
			throw new NotImplementedException();
		}
		
		public void uploadFromByteArray(ByteArray data, int byteArrayOffset, int startOffset, int count) {
			throw new NotImplementedException();
		}
		
		public void uploadFromVector(Vector<uint> data, int startOffset, int count) {
			throw new NotImplementedException();
		}

#endif

	}
}

