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
#elif PLATFORM_XAMMAC
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Platform.MacOS;
using Foundation;
using CoreGraphics;
using OpenGL;
using GLKit;
using AppKit;
using BufferUsage = OpenTK.Graphics.OpenGL.BufferUsageHint;
#elif PLATFORM_MONOTOUCH
using OpenTK.Graphics.ES20;
#elif PLATFORM_MONODROID
using OpenTK.Graphics.ES20;
using BufferTarget = OpenTK.Graphics.ES20.All;
using BufferUsage = OpenTK.Graphics.ES20.All;
#endif


namespace flash.display3D 
{

	public class VertexBuffer3D 
	{
		//
		// Methods
		//

#if OPENGL

		internal VertexBuffer3D(Context3D context3D, int numVertices, int dataPerVertex, int multiBufferCount, bool isDynamic)
		{
			if (multiBufferCount < 1)
				throw new ArgumentOutOfRangeException("multiBufferCount");

			mContext = context3D;
			mNumVertices = numVertices;
			mVertexSize = dataPerVertex;
			mIds = new uint[multiBufferCount];
			GL.GenBuffers(mIds.Length, mIds);

			mUsage = isDynamic ? BufferUsage.DynamicDraw : BufferUsage.StaticDraw;

			// update stats
			mContext.statsIncrement(Context3D.Stats.Count_VertexBuffer);
		}

		public void dispose() {
			GL.DeleteBuffers(mIds.Length, mIds);

			// update stats
			mContext.statsDecrement(Context3D.Stats.Count_VertexBuffer);
			mContext.statsSubtract(Context3D.Stats.Mem_VertexBuffer, mMemoryUsage);
			mMemoryUsage = 0;
		}


		public unsafe void uploadFromPointer(void *data, int dataLength, int startVertex, int numVertices) 
		{
			// swap to next buffer
			mBufferIndex++;
			if (mBufferIndex >= mIds.Length)
				mBufferIndex = 0;

			GL.BindBuffer(BufferTarget.ArrayBuffer, mIds[mBufferIndex]);

			// get pointer to byte array data
			int byteStart = startVertex * mVertexSize * sizeof(float);
			int byteCount = numVertices * mVertexSize * sizeof(float);
			// bounds check
			if (byteCount > dataLength)
				throw new ArgumentOutOfRangeException("data buffer is not big enough for upload");
			if (byteStart == 0) {
				// upload whole array
				GL.BufferData(BufferTarget.ArrayBuffer, 
				              new IntPtr(byteCount), 
				              new IntPtr(data), 
				              mUsage);

				if (byteCount != mMemoryUsage) {
					// update stats for memory usage
					mContext.statsAdd(Context3D.Stats.Mem_VertexBuffer, byteCount - mMemoryUsage);
					mMemoryUsage = byteCount;
				}
			} else {
				// upload whole array
				GL.BufferSubData(BufferTarget.ArrayBuffer, 
				                 new IntPtr(byteStart), 
				                 new IntPtr(byteCount), 
				                 new IntPtr(data));
			}
		}

		
		public unsafe void uploadFromByteArray(ByteArray data, int byteArrayOffset, int startVertex, int numVertices) 
		{
			// get pointer to byte array data
			fixed (byte *ptr = data.getRawArray()) {
				uploadFromPointer(ptr + byteArrayOffset, (int)(data.length - byteArrayOffset), startVertex, numVertices);
			}
		}

		public unsafe void uploadFromArray(float[] data, int startVertex, int numVertices) 
		{
			fixed (float *ptr = data) {
				uploadFromPointer(ptr, data.Length * sizeof(float), startVertex, numVertices);
			}
		}

		public void uploadFromVector(float[] data, int startVertex, int numVertices) 
		{
			uploadFromArray(data, startVertex, numVertices);
		}

		public void uploadFromVector(Vector<float> data, int startVertex, int numVertices) 
		{
			uploadFromArray(data._GetInnerArray(), startVertex, numVertices);
		}

		public void uploadFromVector(Vector<double> data, int startVertex, int numVertices) 
		{
			// allocate temporary buffer for conversion
			if (mData == null) {
				mData = new float[mNumVertices * mVertexSize];
			}

			// convert to floating point
			int count = numVertices * mVertexSize;
			var array = data._GetInnerArray();
			for (int i=0; i < count; i++)
			{
				mData[i] = (float)array[i];
			}

			uploadFromArray(mData, startVertex, numVertices);
		}
		
		public int stride { 
			get {return mVertexSize * sizeof(float);}
		}
		
		public uint id {
			get {return mIds[mBufferIndex];}
		}
		
		private readonly Context3D  mContext;
		private readonly int		mNumVertices;
		private readonly int		mVertexSize; 		// size in floats
		private float[]				mData;
		private uint[]		 		mIds;
		private int 				mBufferIndex;		// buffer index for multibuffering
		private BufferUsage         mUsage;
		private int 				mMemoryUsage;
#else

		public VertexBuffer3D(Context3D context3D, int numVertices, int dataPerVertex, int multiBufferCount = 1)
		{
			throw new NotImplementedException();
		}
		
		public void dispose() 
		{
			throw new NotImplementedException();
		}
		
		public void uploadFromByteArray(ByteArray data, int byteArrayOffset, int startVertex, int numVertices) 
		{
			throw new NotImplementedException();
		}
		
		public void uploadFromVector(Vector<double> data, int startVertex, int numVertices) 
		{
			throw new NotImplementedException();
		}
		
		public int stride 
		{ 
			get {
				throw new NotImplementedException();
			}
		}
		
		public uint id 
		{
			get {
				throw new NotImplementedException();
			}
		}

#endif

	}
	
}
