

namespace flash.display3D {

#if PLATFORM_MONOMAC
	using MonoMac.OpenGL;
	using MonoMac.AppKit;
#elif PLATFORM_MONOTOUCH
	using MonoTouch.OpenGLES;
	using MonoTouch.UIKit;
	using OpenTK.Graphics;
	using OpenTK.Graphics.ES20;
#endif

	using System;
	using System.IO;
	using flash.events;
	using flash.display;
	using flash.utils;
	using flash.geom;
	using flash.display3D;
	using flash.display3D.textures;
	using _root;
	
	public class Context3D : EventDispatcher {
	
		//
		// Properties
		//
	
		public string driverInfo { get { return "MonoGL"; } }

		public bool enableErrorChecking { get; set; }

		//
		// Methods
		//


#if OPENGL
		
		public Context3D(Stage3D stage3D)
		{
			mStage3D = stage3D;
		}
		
		public void clear(double red = 0.0, double green = 0.0, double blue = 0.0, double alpha = 1.0, 
		                  double depth = 1.0, uint stencil = 0, uint mask = 0xffffffff) {
#if PLATFORM_MONOMAC
			GL.ClearColor (NSColor.FromDeviceRgba((float)red,(float)green,(float)blue,(float)alpha));
			GL.ClearDepth(depth);
#elif PLATFORM_MONOTOUCH
			GL.ClearColor ((float)red, (float)green, (float)blue, (float)alpha);
			GL.ClearDepth((float)depth);
#endif
			GL.ClearStencil((int)stencil);
			GL.Clear (ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
		}
		
		public void configureBackBuffer(int width, int height, int antiAlias, 
			bool enableDepthAndStencil = true, bool wantsBestResolution = false) {
		}
	
		public CubeTexture createCubeTexture(int size, string format, bool optimizeForRenderToTexture, int streamingLevels = 0) {
			throw new NotImplementedException();
		}

 	 	public IndexBuffer3D createIndexBuffer(int numIndices) {
 	 		return new IndexBuffer3D(this, numIndices);
 	 	}
 	 	
		public Program3D createProgram() {
			return new Program3D(this);
		}
 	 	
		public Texture createTexture(int width, int height, string format, 
			bool optimizeForRenderToTexture, int streamingLevels = 0) {
			return new Texture(this, width, height, format, optimizeForRenderToTexture, streamingLevels);
		}

 	 	public VertexBuffer3D createVertexBuffer(int numVertices, int data32PerVertex) {
 	 		return new VertexBuffer3D(this, numVertices, data32PerVertex);
 	 	}
 	 	
		public void dispose() {
			throw new NotImplementedException();
		}
 	 	
		public void drawToBitmapData(BitmapData destination) {
		 	throw new NotImplementedException();
		}
 	 	
		public void drawTriangles(IndexBuffer3D indexBuffer, int firstIndex = 0, int numTriangles = -1) {
			int count = (numTriangles == -1) ? indexBuffer.numIndices : (numTriangles * 3);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBuffer.id);
			GL.DrawElements(BeginMode.Triangles, count, DrawElementsType.UnsignedInt, firstIndex );
		}
 	 	
		public void present() {
			GL.Flush();
		}
 	 	
		public void setBlendFactors(string sourceFactor, string destinationFactor) {
		}
 	 	
		public void setColorMask(bool red, bool green, bool blue, bool alpha) {
		}
 	 	
		public void setCulling (string triangleFaceToCull)
		{
			switch (triangleFaceToCull) {
			case "none":
				GL.Disable(EnableCap.CullFace);
				break;
			default:
				throw new NotImplementedException();
				// GL.CullFace(CullFaceMode.
			}
		}
 	 	
		public void setDepthTest(bool depthMask, string passCompareMode) {
		}
 	 	
		public void setProgram (Program3D program)
		{
			if (program != null) {
				GL.UseProgram (program.programId);
			} else {
				// ?? 
				throw new NotImplementedException();
				
			}

			// store current program
			mProgram = program;
		}
 	 	
		public void setProgramConstantsFromByteArray(string programType, int firstRegister, 
			int numRegisters, ByteArray data, uint byteArrayOffset) {
		}

		// temporary floating point array for constant conversion
		private float[] mTemp = new float[4 * 1024];

		private static void convertDoubleToFloat (float[] dest, Vector<double> source, int count)
		{
			// $$TODO optimize this
			for (int i=0; i < count; i++) {
				dest[i] = (float)source[i];
			}
		}

		public void setProgramConstantsFromMatrix (string programType, int firstRegister, Matrix3D matrix, 
			bool transposedMatrix = false)
		{
			// convert double->float
			convertDoubleToFloat(mTemp, matrix.mData, 16);

			if (programType == "vertex") {
				// set uniform registers
				int location = mProgram.getLocation(firstRegister);
				GL.UniformMatrix4(location, 1, transposedMatrix, mTemp);
			} else {
				throw new NotImplementedException ();
			}
		}

 	 	
		public void setProgramConstantsFromVector (string programType, int firstRegister, Vector<double> data, int numRegisters = -1)
		{
			// convert double->float
			convertDoubleToFloat(mTemp, data, numRegisters * 4);

			if (programType == "vertex") {
				// set uniform registers
				int location = mProgram.getLocation(firstRegister);
				GL.Uniform4(location, numRegisters, mTemp);
			} else {
				throw new NotImplementedException();
			}
		}
 	 	
 	 	public void setRenderToBackBuffer() {
			// throw new NotImplementedException();
		}
 	 	
		public void setRenderToTexture(TextureBase texture, bool enableDepthAndStencil = false, int antiAlias = 0, 
		                               int surfaceSelector = 0) {
			throw new NotImplementedException();
		}


		public void setScissorRectangle(Rectangle rectangle) {
			throw new NotImplementedException();
		}

		public void setStencilActions(string triangleFace = "frontAndBack", string compareMode = "always", string actionOnBothPass = "keep", 
			string actionOnDepthFail = "keep", string actionOnDepthPassStencilFail = "keep") {
			throw new NotImplementedException();
		}
 	 	
		public void setStencilReferenceValue(uint referenceValue, uint readMask = 255, uint writeMask = 255) {
			throw new NotImplementedException();
		}

		public void setTextureAt (int sampler, TextureBase texture)
		{
			if (texture != null) {
				GL.ActiveTexture(TextureUnit.Texture0 + sampler);
				GL.BindTexture (TextureTarget.Texture2D, texture.textureId);
			} else {
				GL.ActiveTexture(TextureUnit.Texture0 + sampler);
				GL.BindTexture (TextureTarget.Texture2D, 0);
			}
		}

		public void setVertexBufferAt (int index, VertexBuffer3D buffer, int bufferOffset = 0, string format = "float4")
		{
			if (buffer == null) {
				GL.DisableVertexAttribArray (index);
				GL.BindBuffer (BufferTarget.ArrayBuffer, 0);
				return;
			}
		
			// enable vertex attribute array
			GL.EnableVertexAttribArray (index);
			GL.BindBuffer (BufferTarget.ArrayBuffer, buffer.id);

			int byteOffset = (bufferOffset * 4); // buffer offset is in 32-bit words

			// set attribute pointer within vertex buffer
			switch (format) {
			case "float4":
				GL.VertexAttribPointer(index, 4, VertexAttribPointerType.Float, false, buffer.stride, byteOffset);
				break;
			case "float3":
				GL.VertexAttribPointer(index, 3, VertexAttribPointerType.Float, false, buffer.stride, byteOffset);
				break;
			case "float2":
				GL.VertexAttribPointer(index, 2, VertexAttribPointerType.Float, false, buffer.stride, byteOffset);
				break;
			case "float1":
				GL.VertexAttribPointer(index, 1, VertexAttribPointerType.Float, false, buffer.stride, byteOffset);
				break;
			default:
				throw new NotImplementedException();
			}
		}

		private readonly Stage3D mStage3D;
	
		// current program
		private Program3D mProgram;

#else

		public Context3D(Stage3D stage3D)
		{
			throw new NotImplementedException();
		}
		
		private void setupShaders ()
		{
			throw new NotImplementedException();
		}
		
		public void clear(double red = 0.0, double green = 0.0, double blue = 0.0, double alpha = 1.0, 
		                  double depth = 1.0, uint stencil = 0, uint mask = 0xffffffff) 
		{
			throw new NotImplementedException();
		}
		
		public void configureBackBuffer(int width, int height, int antiAlias, 
		                                bool enableDepthAndStencil = true, bool wantsBestResolution = false) 
		{
			throw new NotImplementedException();
		}
		
		public CubeTexture createCubeTexture(int size, string format, bool optimizeForRenderToTexture, int streamingLevels = 0) 
		{
			throw new NotImplementedException();
		}
		
		public IndexBuffer3D createIndexBuffer(int numIndices) 
		{
			throw new NotImplementedException();
		}
		
		public Program3D createProgram() 
		{
			throw new NotImplementedException();
		}
		
		public Texture createTexture(int width, int height, string format, 
		                             bool optimizeForRenderToTexture, int streamingLevels = 0) 
		{
			throw new NotImplementedException();
		}
		
		public VertexBuffer3D createVertexBuffer(int numVertices, int data32PerVertex) 
		{
			throw new NotImplementedException();
		}
		
		public void dispose() 
		{
			throw new NotImplementedException();
		}
		
		public void drawToBitmapData(BitmapData destination) 
		{
			throw new NotImplementedException();
		}
		
		public void drawTriangles(IndexBuffer3D indexBuffer, int firstIndex = 0, int numTriangles = -1) 
		{
			throw new NotImplementedException();
		}
		
		public void present() 
		{
			throw new NotImplementedException();
		}
		
		public void setBlendFactors(string sourceFactor, string destinationFactor) 
		{
		}
		
		public void setColorMask(bool red, bool green, bool blue, bool alpha) 
		{
		}
		
		public void setCulling (string triangleFaceToCull)
		{
			throw new NotImplementedException();
		}
		
		public void setDepthTest(bool depthMask, string passCompareMode) 
		{
			throw new NotImplementedException();
		}
		
		public void setProgram(Program3D program) 
		{
			throw new NotImplementedException();
		}
		
		public void setProgramConstantsFromByteArray(string programType, int firstRegister, 
		                                             int numRegisters, ByteArray data, uint byteArrayOffset) 
		{
			throw new NotImplementedException();
		}
		
		public void setProgramConstantsFromMatrix(string programType, int firstRegister, Matrix3D matrix, 
		                                          bool transposedMatrix = false) 
		{
			throw new NotImplementedException();
		}
		
		public void setProgramConstantsFromVector(string programType, int firstRegister, Vector<double> data, int numRegisters = -1) 
		{
			throw new NotImplementedException();
		}
		
		public void setRenderToBackBuffer() 
		{
			throw new NotImplementedException();
		}
		
		public void setRenderToTexture(TextureBase texture, bool enableDepthAndStencil = false, int antiAlias = 0, 
		                               int surfaceSelector = 0) 
		{
			throw new NotImplementedException();
		}
		
		
		public void setScissorRectangle(Rectangle rectangle) 
		{
			throw new NotImplementedException();
		}
		
		public void setStencilActions(string triangleFace = "frontAndBack", string compareMode = "always", string actionOnBothPass = "keep", 
		                              string actionOnDepthFail = "keep", string actionOnDepthPassStencilFail = "keep") 
		{
			throw new NotImplementedException();
		}
		
		public void setStencilReferenceValue(uint referenceValue, uint readMask = 255, uint writeMask = 255) 
		{
			throw new NotImplementedException();
		}
		
		public void setTextureAt (int sampler, TextureBase texture)
		{
			throw new NotImplementedException();
		}
		
		public void setVertexBufferAt (int index, VertexBuffer3D buffer, int bufferOffset = 0, string format = "float4")
		{
			throw new NotImplementedException();
		}

#endif

	}

}
