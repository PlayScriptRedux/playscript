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

	using flash.events;
	
	#if PLATFORM_MONOMAC
	using MonoMac.OpenGL;
	#elif PLATFORM_MONOTOUCH
	using OpenTK.Graphics.ES20;
	#endif

	public abstract class TextureBase : EventDispatcher {

		#if OPENGL
		protected TextureBase(TextureTarget target)
		{
			// set texture target
			mTextureTarget = target;

			// generate texture id
			GL.GenTextures (1, out mTextureId);
		}
	
		public virtual void dispose() {
			// delete texture
			GL.DeleteTexture(mTextureId);
		}

		/// <summary>
		/// Computes a complete set of mipmap arrays derived from the zero level array. 
		/// Array levels up to and including the 1x1 dimension texture image are replaced with the derived arrays, regardless of previous contents. 
		/// The zero level texture image is left unchanged.
		/// </summary>
		public void generateMipmaps() {
			GL.BindTexture(mTextureTarget, mTextureId);
			GL.GenerateMipmap(mTextureTarget);

			// now enable trilinear filtering
			// $$TODO filtering modes should be setup later on based on AGAL samplers, but we do them here now
			GL.TexParameter (textureTarget, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);

			#if false
			// set max aniso
			float maxAniso;
			GL.GetFloat((GetPName) Unknown.MaxTextureMaxAnisotropyExt, out maxAniso);
			GL.TexParameter(textureTarget, (TextureParameterName) Unknown.TextureMaxAnisotropyExt, maxAniso);
			#endif
		}

		public int	 		 textureId 		{get {return mTextureId;}}
		public TextureTarget textureTarget 	{get {return mTextureTarget;}}

		private readonly int 		   mTextureId;
		private readonly TextureTarget mTextureTarget;

		#else
		
		public virtual void dispose() {
		}

		#endif
	}

}
