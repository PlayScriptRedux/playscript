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

			mNumIndices = numIndices;
			mIds = new uint[multiBufferCount];
			GL.GenBuffers(multiBufferCount, mIds);

			mUsage = isDynamic ? BufferUsage.DynamicDraw : BufferUsage.StaticDraw;
		}

		public void dispose() {
			GL.DeleteBuffers(mIds.Length, mIds);
		}
		
		unsafe public void uploadFromByteArray(ByteArray data, int byteArrayOffset, int startOffset, int count) 
		{
			//System.Console.WriteLine ("IndexBuffer3D.uploadFromByteArray:");
			int byteStart = byteArrayOffset;
			int countTotal = (count+startOffset); //ignore the startOffset
			byte[] dataBytes = data.getRawArray();
			
			fixed (byte* dataBytesPtr = &dataBytes[byteStart])
			{
				//second allocation ... horrible
				short *shortArray = (short*) dataBytesPtr;
				uint[] dataInts = new uint[countTotal* sizeof(uint)];
				for(int i=0;i<countTotal;i++)
				{
					dataInts[i] = (uint)shortArray[i];
				}
				uploadFromArray(dataInts,startOffset,countTotal);
			}
		}

		public void uploadFromArray(uint[] data, int startOffset, int count) {
			// swap to next buffer
			mBufferIndex++;
			if (mBufferIndex >= mIds.Length)
				mBufferIndex = 0;

			GL.BindBuffer(BufferTarget.ElementArrayBuffer, mIds[mBufferIndex]);
			GL.BufferData<uint>(BufferTarget.ElementArrayBuffer, 
			                    new IntPtr(count * sizeof(uint)), 
			                    data, 
			                    mUsage);
		}

		public void uploadFromVector(Vector<uint> data, int startOffset, int count) {
			uploadFromArray(data._GetInnerArray(), startOffset, count);
		}
		
		public uint 			id 			{get {return mIds[mBufferIndex];}}
		public int				numIndices 	{get {return mNumIndices;}}
		
		private readonly int	mNumIndices;
		private uint[]			mIds;
		private int 			mBufferIndex;		// buffer index for multibuffering
		private BufferUsage     mUsage;


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

