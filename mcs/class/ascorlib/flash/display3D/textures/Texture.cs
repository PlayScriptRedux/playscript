
using System;
using flash.utils;
using flash.display;
using flash.display3D;
using MonoMac.OpenGL;

namespace flash.display3D.textures {
	

	public class Texture : TextureBase {
		
		//
		// Methods
		//
		
		public Texture(Context3D context, int width, int height, string format, 
		                        bool optimizeForRenderToTexture, int streamingLevels)
		{
			mContext = context;
			mWidth = width;
			mHeight = height;
			mFormat = format;
			mOptimizeForRenderToTexture = optimizeForRenderToTexture;
			mStreamingLevels = streamingLevels;

			// create texture and pixel buffer
			GL.GenTextures (1, out mTextureId);
			// GL.GenBuffers (1, out mBufferId);
		}
		
		public void uploadCompressedTextureFromByteArray(ByteArray data, uint byteArrayOffset, bool async = false) {
			throw new NotImplementedException();
		}
		
		public void uploadFromBitmapData (BitmapData source, uint miplevel = 0)
		{
			if (miplevel != 0) {
				throw new NotImplementedException();
			}

			// Bind the texture
			GL.BindTexture (TextureTarget.Texture2D, mTextureId);
			
			// Bind the PBO
			//GL.BindBuffer (BufferTarget.PixelUnpackBuffer, mBufferId);
			//GL.BufferData (BufferTarget.PixelUnpackBuffer, new IntPtr (mWidth * mHeight * sizeof(System.UInt32)), source.getRawData(), BufferUsageHint.StaticDraw);
			//GL.TexImage2D (TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, mWidth, mHeight, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
			//GL.BindBuffer (BufferTarget.PixelUnpackBuffer, 0);

			GL.PixelStore (PixelStoreParameter.UnpackRowLength, 0);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, mWidth, mHeight, 0, PixelFormat.Rgba, PixelType.UnsignedByte,source.getRawData());

			// Setup texture parameters
			GL.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Nearest);
			GL.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Nearest);
			GL.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)All.ClampToEdge);
			GL.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)All.ClampToEdge);

			// unbind texture and pixel buffer
			GL.BindTexture (TextureTarget.Texture2D, 0);
		}
		
		public void uploadFromByteArray(ByteArray data, uint byteArrayOffset, uint miplevel = 0) {
			throw new NotImplementedException();
		}
		
		private readonly Context3D 	mContext;
		private readonly int 		mWidth;
		private readonly int 		mHeight;
		private readonly string 	mFormat;
		private readonly bool 		mOptimizeForRenderToTexture;
		private readonly int    	mStreamingLevels;

		// private int             	mBufferId;
		
	}
	
}
