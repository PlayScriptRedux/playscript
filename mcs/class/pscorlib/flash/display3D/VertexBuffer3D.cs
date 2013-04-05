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
			// allocate temporary buffer for conversion
			mData = new float[numVertices * dataPerVertex];
			GL.GenBuffers(1, out mId);
		}

		public void dispose() {
			GL.DeleteBuffers(1, ref mId);
		}
		
		public void uploadFromByteArray(ByteArray data, int byteArrayOffset, int startVertex, int numVertices) {
			throw new NotImplementedException();
		}

		public void uploadFromVector(Vector<double> data, int startVertex, int numVertices) 
		{
			int start = startVertex * mVertexSize;
			int count = numVertices * mVertexSize;

			// System.Console.WriteLine ("VertexBuffer3D.uploadFromVector:");

			// convert to floating point
			for (int i=0; i < count; i++)
			{
				mData[start + i] = (float)data[i];
				// System.Console.WriteLine ("{0}: {1}", i, data[i]);
			}
		
			GL.BindBuffer(BufferTarget.ArrayBuffer, mId);
			
			// upload whole array
			int byteCount = mNumVertices * mVertexSize * sizeof(float);
#if PLATFORM_MONOMAC
		    GL.BufferData<float>(BufferTarget.ArrayBuffer, 
		        new IntPtr(byteCount), 
		        mData, 
		        BufferUsageHint.StaticDraw);
#elif PLATFORM_MONOTOUCH
			GL.BufferData<float>(BufferTarget.ArrayBuffer, 
			                     new IntPtr(byteCount), 
			                     mData, 
			                     BufferUsage.StaticDraw);
#endif
		}
		
		public int stride { 
			get {return mVertexSize * sizeof(float);}
		}
		
		public uint id {
			get {return mId;}
		}
		
		private readonly int		mNumVertices;
		private readonly int		mVertexSize; 		// size in floats
		private readonly float[]	mData;
		private uint		 		mId;

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
