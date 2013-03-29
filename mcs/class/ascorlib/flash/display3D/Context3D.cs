

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

			// get default framebuffer for use when restoring rendering to backbuffer
			GL.GetInteger(GetPName.FramebufferBinding, out mDefaultFrameBufferId);

			// generate framebuffer for render to texture
			GL.GenFramebuffers(1, out mTextureFrameBufferId);
		}
		
		public void clear(double red = 0.0, double green = 0.0, double blue = 0.0, double alpha = 1.0, 
		                  double depth = 1.0, uint stencil = 0, uint mask = 0xffffffff) {
			if (mask != 0xffffffff)
				throw new NotImplementedException();

			GL.ClearColor ((float)red, (float)green, (float)blue, (float)alpha);
			GL.ClearDepth((float)depth);
			GL.ClearStencil((int)stencil);
			GL.Clear (ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
		}
		
		public void configureBackBuffer(int width, int height, int antiAlias, 
			bool enableDepthAndStencil = true, bool wantsBestResolution = false) {
			mBackBufferWidth = width;
			mBackBufferHeight = height;
			mBackBufferAntiAlias = antiAlias;
			mBackBufferEnableDepthAndStencil = enableDepthAndStencil;
			mBackBufferWantsBestResolution = wantsBestResolution;
		}
	
		public CubeTexture createCubeTexture(int size, string format, bool optimizeForRenderToTexture, int streamingLevels = 0) {
			return new CubeTexture(this, size, format, optimizeForRenderToTexture, streamingLevels);
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
 	 	

		public void setBlendFactors (string sourceFactor, string destinationFactor)
		{
			BlendingFactorSrc src;
			BlendingFactorDest dest;

			// translate strings into enums
			switch (sourceFactor) {
			case Context3DBlendFactor.ONE: 							src = BlendingFactorSrc.One; break;
			case Context3DBlendFactor.ZERO: 						src = BlendingFactorSrc.Zero; break;
			case Context3DBlendFactor.SOURCE_ALPHA: 				src = BlendingFactorSrc.SrcAlpha; break;
#if PLATFORM_MONOTOUCH
			case Context3DBlendFactor.SOURCE_COLOR: 				src = BlendingFactorSrc.SrcColor; break;
#endif
			case Context3DBlendFactor.DESTINATION_ALPHA: 			src = BlendingFactorSrc.DstAlpha; break;
			case Context3DBlendFactor.DESTINATION_COLOR: 			src = BlendingFactorSrc.DstColor; break;
			case Context3DBlendFactor.ONE_MINUS_SOURCE_ALPHA: 		src = BlendingFactorSrc.OneMinusSrcAlpha; break;
#if PLATFORM_MONOTOUCH
			case Context3DBlendFactor.ONE_MINUS_SOURCE_COLOR: 		src = BlendingFactorSrc.OneMinusSrcColor; break;
#endif
			case Context3DBlendFactor.ONE_MINUS_DESTINATION_ALPHA: 	src = BlendingFactorSrc.OneMinusDstAlpha; break;
			case Context3DBlendFactor.ONE_MINUS_DESTINATION_COLOR: 	src = BlendingFactorSrc.OneMinusDstColor; break;
			default:
				throw new NotImplementedException();
			}

			// translate strings into enums
			switch (destinationFactor) {
			case Context3DBlendFactor.ONE: 							dest = BlendingFactorDest.One; break;
			case Context3DBlendFactor.ZERO: 						dest = BlendingFactorDest.Zero; break;
			case Context3DBlendFactor.SOURCE_ALPHA: 				dest = BlendingFactorDest.SrcAlpha; break;
			case Context3DBlendFactor.SOURCE_COLOR: 				dest = BlendingFactorDest.SrcColor; break;
			case Context3DBlendFactor.DESTINATION_ALPHA: 			dest = BlendingFactorDest.DstAlpha; break;
#if PLATFORM_MONOTOUCH
			case Context3DBlendFactor.DESTINATION_COLOR: 			dest = BlendingFactorDest.DstColor; break;
#endif
			case Context3DBlendFactor.ONE_MINUS_SOURCE_ALPHA: 		dest = BlendingFactorDest.OneMinusSrcAlpha; break;
			case Context3DBlendFactor.ONE_MINUS_SOURCE_COLOR: 		dest = BlendingFactorDest.OneMinusSrcColor; break;
			case Context3DBlendFactor.ONE_MINUS_DESTINATION_ALPHA: 	dest = BlendingFactorDest.OneMinusDstAlpha; break;
#if PLATFORM_MONOTOUCH
			case Context3DBlendFactor.ONE_MINUS_DESTINATION_COLOR: 	dest = BlendingFactorDest.OneMinusDstColor; break;
#endif
			default:
				throw new NotImplementedException();
			}

			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(src, dest);
		}
 	 	
		public void setColorMask(bool red, bool green, bool blue, bool alpha) {
			GL.ColorMask (red, green, blue, alpha);
		}
 	 	
		public void setCulling (string triangleFaceToCull)
		{
			switch (triangleFaceToCull) {
			case Context3DTriangleFace.NONE:
				GL.Disable(EnableCap.CullFace);
				break;
			case Context3DTriangleFace.BACK:
				GL.Enable(EnableCap.CullFace);
				GL.CullFace (CullFaceMode.Front);		// oddly this is inverted
				break;
			case Context3DTriangleFace.FRONT:
				GL.Enable(EnableCap.CullFace);
				GL.CullFace (CullFaceMode.Back);		// oddly this is inverted
				break;
			case Context3DTriangleFace.FRONT_AND_BACK:
				GL.Enable(EnableCap.CullFace);
				GL.CullFace (CullFaceMode.FrontAndBack);
				break;
			default:
				throw new NotImplementedException();
			}
		}
 	 	
		public void setDepthTest (bool depthMask, string passCompareMode)
		{
			GL.Enable (EnableCap.DepthTest);
			GL.DepthMask(depthMask);

			switch (passCompareMode) {
			case Context3DCompareMode.ALWAYS:
				GL.DepthFunc(DepthFunction.Always);
				break;
			case Context3DCompareMode.EQUAL:
				GL.DepthFunc(DepthFunction.Equal);
				break;
			case Context3DCompareMode.GREATER:
				GL.DepthFunc(DepthFunction.Greater);
				break;
			case Context3DCompareMode.GREATER_EQUAL:
				GL.DepthFunc(DepthFunction.Gequal);
				break;
			case Context3DCompareMode.LESS:
				GL.DepthFunc(DepthFunction.Less);
				break;
			case Context3DCompareMode.LESS_EQUAL:
				GL.DepthFunc(DepthFunction.Lequal);
				break;
			case Context3DCompareMode.NEVER:
				GL.DepthFunc(DepthFunction.Never);
				break;
			case Context3DCompareMode.NOT_EQUAL:
				GL.DepthFunc(DepthFunction.Notequal);
				break;
			default:
				throw new NotImplementedException();
			}
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
			throw new NotImplementedException();
		}

		private static void convertDoubleToFloat (float[] dest, double[] source, int count)
		{
			// $$TODO optimize this
			for (int i=0; i < count; i++) {
				dest[i] = (float)source[i];
			}
		}

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
			// GLES does not support transposed uniform setting so do it manually 
			if (transposedMatrix) {
				//    0  1  2  3
				//    4  5  6  7
				//    8  9 10 11
				//   12 13 14 15
				double[] source = matrix.mData;
				mTemp[0] = (float)source[0];
				mTemp[1] = (float)source[4];
				mTemp[2] = (float)source[8];
				mTemp[3] = (float)source[12];
				
				mTemp[4] = (float)source[1];
				mTemp[5] = (float)source[5];
				mTemp[6] = (float)source[9];
				mTemp[7] = (float)source[13];

				mTemp[8] = (float)source[2];
				mTemp[9] = (float)source[6];
				mTemp[10]= (float)source[10];
				mTemp[11]= (float)source[14];

				mTemp[12]= (float)source[3];
				mTemp[13]= (float)source[7];
				mTemp[14]= (float)source[11];
				mTemp[15]= (float)source[15];
			} else {
				// convert double->float
				convertDoubleToFloat (mTemp, matrix.mData, 16);
			}


			if (programType == "vertex") {
				// set uniform registers
				int location = mProgram.getVertexLocation(firstRegister);
				if (location >= 0)
				{
					GL.UniformMatrix4(location, 1, false, mTemp);
				}

			} else {
				// set uniform registers
				int location = mProgram.getFragmentLocation(firstRegister);
				if (location >= 0)
				{
					GL.UniformMatrix4(location, 1, false, mTemp);
				}
			}
		}

 	 	
		public void setProgramConstantsFromVector (string programType, int firstRegister, Vector<double> data, int numRegisters = -1)
		{
			if (numRegisters == 0) return;

			if (numRegisters == -1) {
				numRegisters = (int)(data.length / 4);
			}

			if (programType == "vertex") {
				// set uniform registers
				for (int i=0; i < numRegisters; i++)
				{
					// set each register individually because they can be at non-contiguous locations
					int location = mProgram.getVertexLocation(firstRegister + i);
					if (location >= 0)
					{
						mTemp[0] = (float)data[i * 4 + 0];
						mTemp[1] = (float)data[i * 4 + 1];
						mTemp[2] = (float)data[i * 4 + 2];
						mTemp[3] = (float)data[i * 4 + 3];
						GL.Uniform4(location, 1, mTemp);
					}
				}
			} else {

				// set uniform registers
				for (int i=0; i < numRegisters; i++)
				{
					// set each register individually because they can be at non-contiguous locations
					int location = mProgram.getFragmentLocation(firstRegister + i);
					if (location >= 0)
					{
						mTemp[0] = (float)data[i * 4 + 0];
						mTemp[1] = (float)data[i * 4 + 1];
						mTemp[2] = (float)data[i * 4 + 2];
						mTemp[3] = (float)data[i * 4 + 3];
						GL.Uniform4(location, 1, mTemp);
					}
				}
			}
		}
 	 	

 	 	public void setRenderToBackBuffer ()
		{
			// draw to backbuffer
			GL.BindFramebuffer (FramebufferTarget.Framebuffer, mDefaultFrameBufferId);
			// setup viewport for render to backbuffer
			GL.Viewport(0,0, mBackBufferWidth, mBackBufferHeight);
			// clear render to texture
			mRenderToTexture = null;
		}
 	 	
		public void setRenderToTexture(TextureBase texture, bool enableDepthAndStencil = false, int antiAlias = 0, 
		                               int surfaceSelector = 0) {

			var texture2D = texture as Texture;
			if (texture2D == null) 
				throw new Exception("Invalid texture");

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, mTextureFrameBufferId);
#if PLATFORM_MONOTOUCH
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferSlot.ColorAttachment0, TextureTarget.Texture2D, texture.textureId, 0);
#else
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, texture.textureId, 0);
#endif
			// setup viewport for render to texture
			// $$TODO figure out a way to invert the viewport vertically to account for GL's texture origin
			GL.Viewport(0,0, texture2D.width, texture2D.height);

			// validate framebuffer status
			var code = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
			if (code != FramebufferErrorCode.FramebufferComplete)
			{
				throw new Exception("FrameBuffer status error:" + code);
			}

			// save texture we're rendering to
			mRenderToTexture = texture;
		}


		public void setScissorRectangle (Rectangle rectangle)
		{
			if (rectangle != null) {
				GL.Scissor((int)rectangle.x, (int)rectangle.y, (int)rectangle.width, (int)rectangle.height);
			} else {
				GL.Scissor(0, 0, mBackBufferWidth, mBackBufferHeight);
			}
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
				GL.BindTexture (texture.textureTarget, texture.textureId);
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

		// stage3D that owns us
		private readonly Stage3D mStage3D;

		// temporary floating point array for constant conversion
		private readonly float[] mTemp = new float[4 * 1024];
	
		// current program
		private Program3D mProgram;

		// settings for backbuffer
		private int  mDefaultFrameBufferId;
		private int  mBackBufferWidth = 0;
		private int  mBackBufferHeight = 0;
		private int  mBackBufferAntiAlias = 0;
		private bool mBackBufferEnableDepthAndStencil = true;
		private bool mBackBufferWantsBestResolution = false;

		// settings for render to texture
		private TextureBase mRenderToTexture = null;
		private int  		mTextureFrameBufferId;

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
