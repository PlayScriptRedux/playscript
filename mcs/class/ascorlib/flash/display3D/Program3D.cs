using flash.utils;

using System;
using System.IO;
using System.Text;

#if PLATFORM_MONOMAC
using MonoMac.OpenGL;
#elif PLATFORM_MONOTOUCH
using OpenTK.Graphics.ES20;
#endif


namespace flash.display3D {

	public class Program3D {
		
		//
		// Methods
		//

#if OPENGL

		public Program3D(Context3D context3D)
		{
		}
		
		public void dispose() {
			deleteShaders();
		}

		private void deleteShaders ()
		{
			if (mProgramId!=0) {
				// this causes an exception EntryPointNotFound ..
				// GL.DeleteProgram (1, ref mProgramId  );
				mProgramId = 0;
			}

			if (mVertexShaderId!=0) {
				GL.DeleteShader (mVertexShaderId);
				mVertexShaderId = 0;
			}

			if (mFragmentShaderId!=0) {
				GL.DeleteShader (mFragmentShaderId);
				mFragmentShaderId = 0;
			}
		}
		
		public void uploadFromByteArray(ByteArray data, int byteArrayOffset, int startOffset, int count) {
			throw new NotImplementedException();
		}
		
		public void upload(ByteArray vertexProgram, ByteArray fragmentProgram) {
			// convert shaders from AGAL to GLSL
			var glslVertex = AGALConverter.ConvertToGLSL(vertexProgram);
			var glslFragment = AGALConverter.ConvertToGLSL(fragmentProgram);
			// upload as GLSL
			uploadFromGLSL(glslVertex, glslFragment);
		}

		public int programId {
			get {
				return mProgramId;
			}
		}
		
		private string loadShaderSource (string filePath)
		{
			//var path = NSBundle.MainBundle.ResourcePath + Path.DirectorySeparatorChar + "GLSL";
			//var filePath = path + Path.DirectorySeparatorChar + name;
			using (StreamReader streamReader = new StreamReader (filePath)) {
				return streamReader.ReadToEnd ();
			}
		}

		public void uploadFromGLSLFiles (string vertexShaderName, string fragmentShaderName)
		{
			string vertexShaderSource = loadShaderSource(vertexShaderName);
			string fragmentShaderSource = loadShaderSource(fragmentShaderName); 
			uploadFromGLSL(vertexShaderSource, fragmentShaderSource);
		}

		public void uploadFromGLSL (string vertexShaderSource, string fragmentShaderSource)
		{
			// delete existing shaders
			deleteShaders ();

			// Console.WriteLine (vertexShaderSource);
			// Console.WriteLine (fragmentShaderSource);

			mVertexSource = vertexShaderSource;
			mFragmentSource = fragmentShaderSource;
			
			// compiler vertex shader
			mVertexShaderId = GL.CreateShader (ShaderType.VertexShader);
			GL.ShaderSource (mVertexShaderId, vertexShaderSource);
			GL.CompileShader (mVertexShaderId);
			var vertexInfoLog = GL.GetShaderInfoLog (mVertexShaderId);
			if (!string.IsNullOrEmpty (vertexInfoLog)) {
				Console.Write ("ERROR vertex: {0}", vertexInfoLog);
			}

			// compile fragment shader
			mFragmentShaderId = GL.CreateShader (ShaderType.FragmentShader);
			GL.ShaderSource (mFragmentShaderId, fragmentShaderSource);
			GL.CompileShader (mFragmentShaderId);
			var fragmentInfoLog = GL.GetShaderInfoLog (mFragmentShaderId);
			if (!string.IsNullOrEmpty (fragmentInfoLog)) {
				Console.Write ("ERROR fragment: {0}", fragmentInfoLog);
			}
			
			// create program
			mProgramId = GL.CreateProgram ();
			GL.AttachShader (mProgramId, mVertexShaderId);
			GL.AttachShader (mProgramId, mFragmentShaderId);

			GL.BindAttribLocation (mProgramId, 0, "va0");
			GL.BindAttribLocation (mProgramId, 1, "va1");
			GL.BindAttribLocation (mProgramId, 2, "va2");

			// Link the program
			GL.LinkProgram (mProgramId);

			var infoLog = GL.GetProgramInfoLog (mProgramId);
			if (!string.IsNullOrEmpty (infoLog)) {
				Console.Write ("ERROR program: {0}", infoLog);
			}

			// $$TEMP hack
			mVc0 = GL.GetUniformLocation (mProgramId, "vc0");
			mVc1 = GL.GetUniformLocation (mProgramId, "vc1");
			// Console.WriteLine ("vc {0} {1}", mVc0, mVc1);
		}

		public int getLocation(int register)
		{
			// $$TEMP hack
			if (register == 0) return mVc0;
			if (register == 1) return mVc1;
			return -1;
		}

		
		private int 			   mVertexShaderId = 0;
		private int 			   mFragmentShaderId = 0;
		private int 			   mProgramId = 0;

		private string 			   mVertexSource;
		private string 			   mFragmentSource;

		// $$TEMP hack
		private int mVc0 = -1;
		private int mVc1 = -1;
		
#else

		public Program3D(Context3D context3D)
		{
			throw new NotImplementedException();
		}
		
		public void dispose() {
			throw new NotImplementedException();
		}
		
		public void uploadFromByteArray(ByteArray data, int byteArrayOffset, int startOffset, int count) {
			throw new NotImplementedException();
		}
		
		public void upload(ByteArray vertexProgram, ByteArray fragmentProgram) {
			throw new NotImplementedException();
		}
		
		public int programId {
			get {
				throw new NotImplementedException();
			}
		}
		
		private void printProgramInfo (string name, int id)
		{
			throw new NotImplementedException();
		}
		
		private string loadShaderSource (string name)
		{
			throw new NotImplementedException();
		}
		
		public void uploadFromGLSLFiles (string vertexShaderName, string fragmentShaderName)
		{
			throw new NotImplementedException();
		}
		
		public void uploadFromGLSL(string vertexShaderSource, string fragmentShaderSource)
		{
			throw new NotImplementedException();
		}

#endif

	}
	
}

