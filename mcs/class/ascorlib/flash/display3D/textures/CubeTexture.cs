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

namespace flash.display3D.textures 
{
	
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


	public class CubeTexture : TextureBase {

		#if OPENGL
		
		public CubeTexture(Context3D context, int size, string format, 
		               bool optimizeForRenderToTexture, int streamingLevels)
			: base(TextureTarget.TextureCubeMap)
		{
			mContext = context;
			mSize = size;
			mFormat = format;
			mOptimizeForRenderToTexture = optimizeForRenderToTexture;
			mStreamingLevels = streamingLevels;
		}

		public void uploadCompressedTextureFromByteArray(ByteArray data, uint byteArrayOffset, bool async = false) {
			throw new System.NotImplementedException();
		}
		
		public void uploadFromBitmapData(BitmapData source, uint side, uint miplevel = 0) {
			// bind the texture
			GL.BindTexture (TextureTarget.TextureCubeMap, textureId);

			// determine which side of the cubmap to upload
			TextureTarget target;
			switch (side)
			{
			case 0: target = TextureTarget.TextureCubeMapPositiveX; break;
			case 1: target = TextureTarget.TextureCubeMapNegativeX; break;
			case 2: target = TextureTarget.TextureCubeMapPositiveY; break;
			case 3: target = TextureTarget.TextureCubeMapNegativeY; break;
			case 4: target = TextureTarget.TextureCubeMapPositiveZ; break;
			case 5: target = TextureTarget.TextureCubeMapNegativeZ; break;
			}

			// perform upload
			GL.TexImage2D(target, (int)miplevel, PixelInternalFormat.Rgba, size, size, 0, PixelFormat.Rgba, PixelType.UnsignedByte, source.getRawData());

			// setup texture parameters
			GL.TexParameter (TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
			GL.TexParameter (TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
			GL.TexParameter (TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
			GL.TexParameter (TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
			
			// unbind texture and pixel buffer
			GL.BindTexture (TextureTarget.TextureCubeMap, 0);
		}
		
		public void uploadFromByteArray(ByteArray data, uint byteArrayOffset, uint side, uint miplevel = 0) {
			throw new System.NotImplementedException();
		}
		
		public int size 	{ get { return mSize; } }
		public int width 	{ get { return mSize; } }
		public int height 	{ get { return mSize; } }
		
		
		private readonly Context3D 	mContext;
		private readonly int 		mSize;
		private readonly string 	mFormat;
		private readonly bool 		mOptimizeForRenderToTexture;
		private readonly int    	mStreamingLevels;

		#else
		public void uploadCompressedTextureFromByteArray(ByteArray data, uint byteArrayOffset, bool async = false) {
			throw new System.NotImplementedException();
		}

		public void uploadFromBitmapData(BitmapData source, uint side, uint miplevel = 0) {
			throw new System.NotImplementedException();
		}

		public void uploadFromByteArray(ByteArray data, uint byteArrayOffset, uint side, uint miplevel = 0) {
			throw new System.NotImplementedException();
		}
		#endif
	}

}
