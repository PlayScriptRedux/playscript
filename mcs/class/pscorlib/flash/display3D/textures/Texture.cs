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
#elif PLATFORM_MONODROID
	using OpenTK.Graphics.ES20;
	using TextureTarget = OpenTK.Graphics.ES20.All;
	using PixelInternalFormat = OpenTK.Graphics.ES20.All;
	using PixelFormat = OpenTK.Graphics.ES20.All;
	using PixelType = OpenTK.Graphics.ES20.All;
#endif

	public class Texture : TextureBase {
		
		//
		// Methods
		//

#if OPENGL

		public Texture(Context3D context, int width, int height, string format, 
		                        bool optimizeForRenderToTexture, int streamingLevels)
			: base(TextureTarget.Texture2D)
		{
			mContext = context;
			mWidth = width;
			mHeight = height;
			mFormat = format;
			mOptimizeForRenderToTexture = optimizeForRenderToTexture;
			mStreamingLevels = streamingLevels;

			// we do this to clear the texture on creation
			// $$TODO we dont need to allocate a bitmapdata to do this, we should just use a PBO and clear it
			if (optimizeForRenderToTexture) {
				var clearData = new BitmapData(width, height);
				uploadFromBitmapData(clearData);
				clearData.dispose();
			}
		}

		private static int sColor = 0;
		private static uint[] sColors = new uint[] {0x0000FF, 0x00FF00, 0xFF0000, 0xFF00FF, 0x00FFFF, 0xFFFF00};

		private bool mDidUpload = false;

		private static int sMemoryUsedForTextures = 0;

		public void uploadCompressedTextureFromByteArray (ByteArray data, uint byteArrayOffset, bool async = false)
		{
			// $$TODO 
			// this is empty for now
#if PLATFORM_MONOMAC
			System.Console.WriteLine("NotImplementedWarning: Texture.uploadCompressedTextureFromByteArray()");

			if (!mDidUpload) {
				var clearData = new BitmapData(32,32, true, sColors[sColor % sColors.Length]);
				sColor++; 
				uploadFromBitmapData(clearData);
				clearData.dispose();
				mDidUpload = true;
			}
#endif

#if PLATFORM_MONOTOUCH
			int memUsage = (mWidth * mHeight) / 2;
			sMemoryUsedForTextures += memUsage;
			Console.WriteLine("Texture.uploadCompressedTextureFromByteArray() - " + mWidth + "x" + mHeight + " - Mem: " + (memUsage / 1024) + " KB - Total Mem: " + (sMemoryUsedForTextures / 1024) + " KB");

			// Bind the texture
			GL.BindTexture (textureTarget, textureId);

			if (byteArrayOffset != 0) {
				throw new NotSupportedException();
			}

			int dataLength = (int)(data.length - byteArrayOffset) - 4;		// We remove the 4 bytes footer
																			// TODO: Fix hardcoded value here

			OpenTK.Graphics.ES20.PixelInternalFormat pixelFormat = (OpenTK.Graphics.ES20.PixelInternalFormat)0x8C02;
			GL.CompressedTexImage2D(textureTarget, 0, pixelFormat, mWidth, mHeight, 0, dataLength, data.getRawArray());

			// unbind texture and pixel buffer
			GL.BindTexture (textureTarget, 0);
#endif
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
		
		public void uploadFromBitmapData (BitmapData source, uint miplevel = 0, bool generateMipmap = false)
		{
			int memUsage = (mWidth * mHeight) * 4;
			sMemoryUsedForTextures += memUsage;
			Console.WriteLine("Texture.uploadFromBitmapData() - " + mWidth + "x" + mHeight + " - Mem: " + (memUsage / 1024) + " KB - Total Mem: " + (sMemoryUsedForTextures / 1024) + " KB");

			// Bind the texture
			GL.BindTexture (textureTarget, textureId);

#if PLATFORM_MONOMAC
            if (generateMipmap) {
                GL.TexParameter (TextureTarget.Texture2D, TextureParameterName.GenerateMipmap, 1);
            }
#endif
#if PLATFORM_MONOMAC || PLATFORM_MONOTOUCH
			GL.TexImage2D(textureTarget, (int)miplevel, PixelInternalFormat.Rgba, mWidth, mHeight, 0, PixelFormat.Rgba, PixelType.UnsignedByte,source.getRawData());
#elif PLATFORM_MONODROID
			GL.TexImage2D<uint>(textureTarget, (int)miplevel, (int) PixelInternalFormat.Rgba, mWidth, mHeight, 0, PixelFormat.Rgba, PixelType.UnsignedByte, source.getRawData());
#endif

#if PLATFORM_MONOTOUCH || PLATFORM_MONODROID
            GL.GenerateMipmap(textureTarget);
#endif

			// unbind texture and pixel buffer
			GL.BindTexture (textureTarget, 0);

			source.dispose();
		}
		
		public void uploadFromByteArray(ByteArray data, uint byteArrayOffset, uint miplevel = 0) {
			throw new NotImplementedException();
		}

		public int width 	{ get { return mWidth; } }
		public int height 	{ get { return mHeight; } }
		
		
		private readonly Context3D 	mContext;
		private readonly int 		mWidth;
		private readonly int 		mHeight;
		private readonly string 	mFormat;
		private readonly bool 		mOptimizeForRenderToTexture;
		private readonly int    	mStreamingLevels;

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
