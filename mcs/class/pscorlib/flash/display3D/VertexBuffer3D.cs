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
#elif PLATFORM_MONOTOUCH
using OpenTK.Graphics.ES20;
#endif


namespace flash.display3D 
{

	public class VertexBuffer3D 
	{
		//
		// Methods
		//

#if OPENGL

		public VertexBuffer3D(Context3D context3D, int numVertices, int dataPerVertex)
		{
			mNumVertices = numVertices;
			mVertexSize = dataPerVertex;
			GL.GenBuffers(1, out mId);
		}

		public void dispose() {
			GL.DeleteBuffers(1, ref mId);
		}
		
		public void uploadFromByteArray(ByteArray data, int byteArrayOffset, int startVertex, int numVertices) {
			throw new NotImplementedException();
		}

		public void uploadFromArray(float[] data, int startVertex, int numVertices) 
		{
			// System.Console.WriteLine ("VertexBuffer3D.uploadFromVector:");
			GL.BindBuffer(BufferTarget.ArrayBuffer, mId);
			
			int byteStart = startVertex * mVertexSize * sizeof(float);
			int byteCount = numVertices * mVertexSize * sizeof(float);
			if (byteStart == 0)
			{
				// upload whole array
				GL.BufferData<float>(BufferTarget.ArrayBuffer, 
				                     new IntPtr(byteCount), 
				                     data, 
				                     mUsage);
			} 
			else 
			{
				// upload whole array
				GL.BufferSubData<float>(BufferTarget.ArrayBuffer, 
				                        new IntPtr(byteStart), 
				                        new IntPtr(byteCount), 
				                        data);
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

			// System.Console.WriteLine ("VertexBuffer3D.uploadFromVector:");

			// convert to floating point
			int count = numVertices * mVertexSize;
			var array = data._GetInnerArray();
			for (int i=0; i < count; i++)
			{
				mData[i] = (float)array[i];
				// System.Console.WriteLine ("{0}: {1}", i, data[i]);
			}

			uploadFromArray(mData, startVertex, numVertices);
		}
		
		public int stride { 
			get {return mVertexSize * sizeof(float);}
		}
		
		public uint id {
			get {return mId;}
		}
		
		private readonly int		mNumVertices;
		private readonly int		mVertexSize; 		// size in floats
		private float[]				mData;
		private uint		 		mId;
#if PLATFORM_MONOMAC
		private BufferUsageHint     mUsage = BufferUsageHint.DynamicDraw;
#elif PLATFORM_MONOTOUCH
		private BufferUsage         mUsage = BufferUsage.DynamicDraw;
#endif
		

#else

		public VertexBuffer3D(Context3D context3D, int numVertices, int dataPerVertex)
		{
			throw new NotImplementedException();
		}
		
		public void dispose() {
			throw new NotImplementedException();
		}
		
		public void uploadFromByteArray(ByteArray data, int byteArrayOffset, int startVertex, int numVertices) {
			throw new NotImplementedException();
		}
		
		public void uploadFromVector(Vector<double> data, int startVertex, int numVertices) 
		{
			throw new NotImplementedException();
		}
		
		public int stride { 
			get {
				throw new NotImplementedException();
			}
		}
		
		public uint id {
			get {
				throw new NotImplementedException();
			}
		}

#endif

	}
	
}
