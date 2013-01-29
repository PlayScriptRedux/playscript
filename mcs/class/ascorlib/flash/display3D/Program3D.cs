using flash.utils;

using System;
using System.IO;
using System.Text;

#if PLATFORM_MONOMAC
using MonoMac.OpenGL;
#endif


namespace flash.display3D {

	public class Program3D {
		
		//
		// Methods
		//

#if OPENGL

		public Program3D(Context3D context3D)
		{
			mContext3D = context3D;
		}
		
		public void dispose() {
		}
		
		public void uploadFromByteArray(ByteArray data, int byteArrayOffset, int startOffset, int count) {
		}
		
		public void upload(ByteArray vertexProgram, ByteArray fragmentProgram) {
		}

		public int programId {
			get {
				return mProgramId;
			}
		}

		private void printProgramInfo (string name, int id)
		{
			int infoLogLen = 0;
			GL.GetProgram (id, ProgramParameter.InfoLogLength, out infoLogLen); 
			
			if (infoLogLen > 0) {
				var infoLog = GL.GetProgramInfoLog (id);
				Console.Write("{0} {1}", name, infoLog);
			}
		}
		
		private string loadShaderSource (string name)
		{
			//var path = NSBundle.MainBundle.ResourcePath + Path.DirectorySeparatorChar + "GLSL";
			//var filePath = path + Path.DirectorySeparatorChar + name;
			var filePath = "/Users/iaddis/Desktop/" + name;
			StreamReader streamReader = new StreamReader (filePath);
			string text = streamReader.ReadToEnd ();
			streamReader.Close ();
			return text;
		}

		public void uploadFromGLSLFiles (string vertexShaderName, string fragmentShaderName)
		{
			string vertexShaderSource = loadShaderSource(vertexShaderName);
			string fragmentShaderSource = loadShaderSource(fragmentShaderName); 
			uploadFromGLSL(vertexShaderSource, fragmentShaderSource);
		}

		public void uploadFromGLSL(string vertexShaderSource, string fragmentShaderSource)
		{
			// load vertex shader
			int vertexShader = GL.CreateShader(ShaderType.VertexShader);
			GL.ShaderSource(vertexShader, vertexShaderSource);
			GL.CompileShader(vertexShader);
			
			// load fragment shader
			int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
			GL.ShaderSource(fragmentShader, fragmentShaderSource);
			GL.CompileShader(fragmentShader);
			
			// create program
			int shaderProgram = GL.CreateProgram();
			GL.AttachShader(shaderProgram, vertexShader);
			GL.AttachShader(shaderProgram, fragmentShader);
			
			// bind shader attribute inputs
			// $$TODO these are hardcoded
			GL.BindAttribLocation(shaderProgram, 1, "inColor");
			GL.BindAttribLocation(shaderProgram, 2, "inTexCoord");
			// GL.BindBindAttribLocation(shaderProgram, 0, "texture0");

			// Link the program
			GL.LinkProgram(shaderProgram);
			
			// Output our shader object errors if there were problems 
			printProgramInfo("vertex:", vertexShader);
			printProgramInfo("fragment:", fragmentShader);
			printProgramInfo("shader:", shaderProgram);

			// store program
			mProgramId = shaderProgram;
		}

		
		private readonly Context3D mContext3D;
		private int 			   mProgramId = 0;

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

