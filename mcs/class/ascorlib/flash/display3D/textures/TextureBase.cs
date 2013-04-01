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
