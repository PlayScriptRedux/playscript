using System;
using flash.utils;
using _root;

#if PLATFORM_MONOMAC
using MonoMac.OpenGL;
#elif PLATFORM_MONOTOUCH
using OpenTK.Graphics.ES20;
#endif

namespace flash.display3D {

	public class IndexBuffer3D {

		//
		// Methods
		//

#if OPENGL

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
#if PLATFORM_MONOMAC
		    GL.BufferData<uint>(BufferTarget.ElementArrayBuffer, 
		        (IntPtr)(count * sizeof(uint)), 
		        data.ToArray(), 
		        BufferUsageHint.StaticDraw);
#elif PLATFORM_MONOTOUCH
			GL.BufferData<uint>(BufferTarget.ElementArrayBuffer, 
                (IntPtr)(count * sizeof(uint)), 
                data.ToArray(), 
                BufferUsage.StaticDraw);
#endif
		}
		
		public uint id {get {return mId;}}
		public int numIndices {get{return mNumIndices;}}
		
		private readonly int mNumIndices;
		private uint 	mId;

#else

		public IndexBuffer3D(Context3D context3D, int numIndices)
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

