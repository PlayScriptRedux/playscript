namespace flash.display3D.textures {
	
	using System;
	using flash.utils;
	using flash.display;
	using flash.display3D;
	using flash.events;
	
#if PLATFORM_MONOMAC
	using MonoMac.OpenGL;
#elif PLATFORM_MONOTOUCH
	using OpenTK.Graphics.ES20;
#endif

	public class Texture : TextureBase {
		
		//
		// Methods
		//

#if OPENGL

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

			// we do this to clear the texture on creation
			// $$TODO we dont need to allocate a bitmapdata to do this, we should just use a PBO and clear it
			var clearData = new BitmapData(width, height);
			uploadFromBitmapData(clearData);
			clearData.dispose();
		}

		public void uploadCompressedTextureFromByteArray (ByteArray data, uint byteArrayOffset, bool async = false)
		{
			// $$TODO 
			// this is empty for now
			Console.WriteLine ("uploadCompressedTextureFromByteArray not implemented");

			if (async) {
				// load with a delay
				var timer = new flash.utils.Timer(1, 1);
				timer.addEventListener(TimerEvent.TIMER, (System.Action<Event>)this.OnTextureReady );
				timer.start();
			}

		}

		private void OnTextureReady (Event e)
		{
			this.dispatchEvent(new Event(Event.TEXTURE_READY)  );
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

#if PLATFORM_MONOMAC
			GL.PixelStore (PixelStoreParameter.UnpackRowLength, 0);
#elif PLATFORM_MONOTOUCH
			GL.PixelStore (PixelStoreParameter.UnpackAlignment, 0);
#endif
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, mWidth, mHeight, 0, PixelFormat.Rgba, PixelType.UnsignedByte,source.getRawData());

			// Setup texture parameters
			GL.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Linear);
			GL.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Linear);
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

#else

		public Texture(Context3D context, int width, int height, string format, 
		               bool optimizeForRenderToTexture, int streamingLevels)
		{
			throw new NotImplementedException();
		}
		
		public void uploadCompressedTextureFromByteArray(ByteArray data, uint byteArrayOffset, bool async = false) {
			throw new NotImplementedException();
		}
		
		public void uploadFromBitmapData (BitmapData source, uint miplevel = 0)
		{
			throw new NotImplementedException();
		}
		
		public void uploadFromByteArray(ByteArray data, uint byteArrayOffset, uint miplevel = 0) {
			throw new NotImplementedException();
		}

#endif

	}
	
}
