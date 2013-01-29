using System;
using flash.utils;
using _root;

#if PLATFORM_MONOMAC
using MonoMac.OpenGL;
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
		    GL.BufferData<float>(BufferTarget.ArrayBuffer, 
		        new IntPtr(byteCount), 
		        mData, 
		        BufferUsageHint.StaticDraw);			
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
