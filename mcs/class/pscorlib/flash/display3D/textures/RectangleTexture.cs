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

	using flash.utils;
	using flash.display;

#if PLATFORM_MONOMAC
	using MonoMac.OpenGL;
#elif PLATFORM_XAMMAC
	using OpenTK.Graphics.OpenGL;
#elif PLATFORM_MONOTOUCH
	using OpenTK.Graphics.ES20;
#elif PLATFORM_MONODROID
	using OpenTK.Graphics.ES20;
	using TextureTarget = OpenTK.Graphics.ES20.All;
#endif

	public class RectangleTexture : TextureBase
	{
	
		#if OPENGL

		//
		// Methods
		//

		public RectangleTexture (Context3D context)
			: base (context, TextureTarget.Texture2D)
		{
		}

		public void uploadFromBitmapData (BitmapData source)
		{
			throw new System.NotImplementedException ();
		}

		public void uploadFromByteArray (ByteArray data, uint byteArrayOffset)
		{
			throw new System.NotImplementedException ();
		}

		#else
		
		public void uploadFromBitmapData(BitmapData source) {
			throw new System.NotImplementedException();
		}
		
		public void uploadFromByteArray(ByteArray data, uint byteArrayOffset) {
			throw new System.NotImplementedException();
		}

		#endif
	
	}
}