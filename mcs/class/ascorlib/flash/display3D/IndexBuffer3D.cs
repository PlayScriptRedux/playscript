namespace flash.display3D {
	using MonoMac.OpenGL;

	using System;
	using flash.utils;
	using _root;

	public class IndexBuffer3D {

		//
		// Methods
		//
		
		public IndexBuffer3D(Context3D context3D, int numIndices)
		{
			mNumIndices = numIndices;
			GL.GenBuffers(1, out mId);
		}


		public void dispose() {
			GL.DeleteBuffers(1, ref mId);
		}
		
		public void uploadFromByteArray(ByteArray data, int byteArrayOffset, int startOffset, int count) {
			throw new NotImplementedException();
		}

		public void uploadFromVector(Vector<uint> data, int startOffset, int count) {
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, mId);
		    GL.BufferData<uint>(BufferTarget.ElementArrayBuffer, 
		        (IntPtr)(count * sizeof(uint)), 
		        data.ToArray(), 
		        BufferUsageHint.StaticDraw);			
		}
		
		public uint id {get {return mId;}}
		public int numIndices {get{return mNumIndices;}}
		
		private readonly int mNumIndices;
		private uint 	mId;

	}
}

