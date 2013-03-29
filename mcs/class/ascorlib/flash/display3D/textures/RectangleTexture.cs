namespace flash.display3D.textures {

	using flash.utils;
	using flash.display;

	#if PLATFORM_MONOMAC
	using MonoMac.OpenGL;
	#elif PLATFORM_MONOTOUCH
	using OpenTK.Graphics.ES20;
	#endif

	public class RectangleTexture : TextureBase {
	
		#if OPENGL

		//
		// Methods
		//

		public RectangleTexture()
			: base(TextureTarget.Texture2D)
		{
		}
	
		public void uploadFromBitmapData(BitmapData source) {
			throw new System.NotImplementedException();
		}
 	 	
		public void uploadFromByteArray(ByteArray data, uint byteArrayOffset) {
			throw new System.NotImplementedException();
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