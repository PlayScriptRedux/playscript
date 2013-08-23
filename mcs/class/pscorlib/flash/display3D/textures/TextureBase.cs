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
	#elif PLATFORM_MONODROID
	using OpenTK.Graphics.ES20;
	using TextureTarget = OpenTK.Graphics.ES20.All;
	using TextureParameterName = OpenTK.Graphics.ES20.All;
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

		public int	 		 textureId 		{get {return mTextureId;}}
		public TextureTarget textureTarget 	{get {return mTextureTarget;}}

		// sets the sampler state associated with this texture
		// due to the way GL works, sampler states are parameters of texture objects
		public void setSamplerState(SamplerState state)
		{
			// prevent redundant setting of sampler state
			if (!state.Equals(mSamplerState)) {
				// set texture
				GL.BindTexture(mTextureTarget, mTextureId);
				// apply state to texture
				GL.TexParameter (mTextureTarget, TextureParameterName.TextureMinFilter, (int)state.MinFilter);
				GL.TexParameter (mTextureTarget, TextureParameterName.TextureMagFilter, (int)state.MagFilter);
				GL.TexParameter (mTextureTarget, TextureParameterName.TextureWrapS, (int)state.WrapModeS);
				GL.TexParameter (mTextureTarget, TextureParameterName.TextureWrapT, (int)state.WrapModeT);
				if (state.LodBias != 0.0) {
					throw new System.NotImplementedException("Lod bias setting not supported yet");
				}

				mSamplerState = state;
			}
		}

		private readonly int 		   mTextureId;
		private readonly TextureTarget mTextureTarget;
		private SamplerState		   mSamplerState;

		#else
		
		public virtual void dispose() {
		}

		#endif
	}

}
