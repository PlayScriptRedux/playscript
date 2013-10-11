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



namespace flash.display3D {

#if PLATFORM_MONOMAC
	using MonoMac.OpenGL;
	using MonoMac.AppKit;
	using FramebufferSlot = MonoMac.OpenGL.FramebufferAttachment;
#elif PLATFORM_MONOTOUCH
	using MonoTouch.OpenGLES;
	using MonoTouch.UIKit;
	using OpenTK.Graphics;
	using OpenTK.Graphics.ES20;
#elif PLATFORM_MONODROID
	using OpenTK;
	using OpenTK.Graphics;
	using OpenTK.Graphics.ES20;

	using GetPName = OpenTK.Graphics.ES20.All;
	using BufferTarget = OpenTK.Graphics.ES20.All;
	using BeginMode = OpenTK.Graphics.ES20.All;
	using DrawElementsType = OpenTK.Graphics.ES20.All;
	using BlendingFactorSrc = OpenTK.Graphics.ES20.All;
	using BlendingFactorDest = OpenTK.Graphics.ES20.All;
	using EnableCap = OpenTK.Graphics.ES20.All;
	using CullFaceMode = OpenTK.Graphics.ES20.All;
	using TextureUnit = OpenTK.Graphics.ES20.All;
	using TextureParameterName = OpenTK.Graphics.ES20.All;
	using VertexAttribPointerType = OpenTK.Graphics.ES20.All;
	using FramebufferTarget = OpenTK.Graphics.ES20.All;
	using FramebufferErrorCode = OpenTK.Graphics.ES20.All;
	using DepthFunction = OpenTK.Graphics.ES20.All;
	using StringName = OpenTK.Graphics.ES20.All;
	using TextureTarget = OpenTK.Graphics.ES20.All;
	using FramebufferAttachment = OpenTK.Graphics.ES20.All;
	using ActiveUniformType = OpenTK.Graphics.ES20.All;
	using RenderbufferTarget = OpenTK.Graphics.ES20.All;
	using RenderbufferInternalFormat = OpenTK.Graphics.ES20.All;
	using FramebufferSlot = OpenTK.Graphics.ES20.All;
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
		// Constants
		//

#if PLATFORM_MONOTOUCH || PLATFORM_MONOMAC
		public const int MaxSamplers = 16;
#elif PLATFORM_MONODROID
		public const int MaxSamplers = 8;
#endif
		public const int MaxAttributes = 16;

		// stats enumeration
		// these will be used to send data to telemetry and are named such that they translate easily to telemetry names
		internal enum Stats
		{
			DrawCalls,
			Count_IndexBuffer,
			Count_VertexBuffer,
			Count_Texture,
			Count_Texture_Compressed,
			Count_Program,
			Mem_IndexBuffer,
			Mem_VertexBuffer,
			Mem_Texture,
			Mem_Texture_Compressed,
			Mem_Program,

			Length
		};


		//
		// Properties
		//
	
		public string driverInfo { get; private set; }

		public bool enableErrorChecking { get; set; }


		/// <summary>
		/// This method gets invoked whenever Present is called on the context
		/// </summary>
		public static System.Action<Context3D> OnPresent;

		//
		// Methods
		//


#if OPENGL

		// this is the default sampler state
		public static readonly SamplerState DefaultSamplerState = new SamplerState(
#if PLATFORM_MONODROID
												(TextureMinFilter)All.Linear, 
#else
												TextureMinFilter.Linear, 
#endif
												TextureMagFilter.Linear,
												TextureWrapMode.Repeat,
												TextureWrapMode.Repeat).Intern();


		public Context3D(Stage3D stage3D)
		{
			mStage3D = stage3D;

			// compose driver info string
			driverInfo = string.Format("OpenGL Vendor={0} Version={1} Renderer={2} GLSL={3}", 
			                           GL.GetString(StringName.Vendor), 
			                           GL.GetString(StringName.Version), 
			                           GL.GetString(StringName.Renderer),
			                           GL.GetString(StringName.ShadingLanguageVersion));
			// write driver info to telemetry
			Telemetry.Session.WriteValue(".platform.3d.driverinfo", driverInfo);


			// get default framebuffer for use when restoring rendering to backbuffer
			GL.GetInteger(GetPName.FramebufferBinding, out mDefaultFrameBufferId);

			// generate depth buffer for backbuffer
			GL.GenRenderbuffers (1, out mDepthRenderBufferId);
			GLUtils.CheckGLError ();

			// generate framebuffer for render to texture
			GL.GenFramebuffers(1, out mTextureFrameBufferId);
			GLUtils.CheckGLError ();

			// generate depth buffer for render to texture
			GL.GenRenderbuffers(1, out mTextureDepthBufferId);
			GLUtils.CheckGLError ();
		}
		
		public void clear(double red = 0.0, double green = 0.0, double blue = 0.0, double alpha = 1.0, 
		                  double depth = 1.0, uint stencil = 0, uint mask = 0xffffffff) {

//			if (mask != 0xffffffff)
//				System.Console.WriteLine("Context3D.clear() - Not implemented with mask=" + mask);

			// save old depth mask
			bool oldDepthWriteMask;
			GL.GetBoolean(GetPName.DepthWritemask, out oldDepthWriteMask);
			GLUtils.CheckGLError ();

			// depth writes must be enabled to clear the depth buffer!
			GL.DepthMask(true);
			GLUtils.CheckGLError ();

			GL.ClearColor ((float)red, (float)green, (float)blue, (float)alpha);
			GLUtils.CheckGLError ();

			GL.ClearDepth((float)depth);
			GLUtils.CheckGLError ();

			GL.ClearStencil((int)stencil);
			GLUtils.CheckGLError ();

			GL.Clear (ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
			GLUtils.CheckGLError ();

			// restore depth mask
			GL.DepthMask(oldDepthWriteMask);
			GLUtils.CheckGLError ();
		}
		
		public void configureBackBuffer(int width, int height, int antiAlias, 
			bool enableDepthAndStencil = true, bool wantsBestResolution = false) {

			GL.Viewport(0,0, width, height);
			GLUtils.CheckGLError ();

			// $$TODO allow for resizing of frame buffer here
			mBackBufferWidth = width;
			mBackBufferHeight = height;
			mBackBufferAntiAlias = antiAlias;
			mBackBufferEnableDepthAndStencil = enableDepthAndStencil;
			mBackBufferWantsBestResolution = wantsBestResolution;

			#if PLATFORM_MONOTOUCH
			if (enableDepthAndStencil)
			{
				// setup depth buffer size to match backbuffer size
				GL.BindRenderbuffer (RenderbufferTarget.Renderbuffer, mDepthRenderBufferId);
				GLUtils.CheckGLError ();

				GL.RenderbufferStorage (RenderbufferTarget.Renderbuffer, RenderbufferInternalFormat.DepthComponent16, width, height);
				GLUtils.CheckGLError ();

				GL.FramebufferRenderbuffer (FramebufferTarget.Framebuffer,
				                            FramebufferSlot.DepthAttachment,
				                            RenderbufferTarget.Renderbuffer, 
				                            mDepthRenderBufferId);
				GLUtils.CheckGLError ();
			}
			else
			{
				// disable depth render buffer
				GL.FramebufferRenderbuffer (FramebufferTarget.Framebuffer,
				                            FramebufferSlot.DepthAttachment,
				                            RenderbufferTarget.Renderbuffer, 
				                            0);
				GLUtils.CheckGLError ();
			}
			#endif

			// validate framebuffer status
			var status = GL.CheckFramebufferStatus (FramebufferTarget.Framebuffer);
			GLUtils.CheckGLError ();

			if (status != FramebufferErrorCode.FramebufferComplete) {
				Console.Error.WriteLine("FrameBuffer configuration error: {0}", status);
			}
		}
	
		public CubeTexture createCubeTexture(int size, string format, bool optimizeForRenderToTexture, int streamingLevels = 0) {
			return new CubeTexture(this, size, format, optimizeForRenderToTexture, streamingLevels);
		}

		public IndexBuffer3D createIndexBuffer(int numIndices, int multiBufferCount = 1, bool isDynamic = true) {
 	 		return new IndexBuffer3D(this, numIndices, multiBufferCount, isDynamic);
 	 	}
 	 	
		public Program3D createProgram() {
			return new Program3D(this);
		}
 	 	
		public Texture createTexture(int width, int height, string format, 
			bool optimizeForRenderToTexture, int streamingLevels = 0) {
			return new Texture(this, width, height, format, optimizeForRenderToTexture, streamingLevels);
		}

		public VertexBuffer3D createVertexBuffer(int numVertices, int data32PerVertex, int multiBufferCount = 1, bool isDynamic = true) {
 	 		return new VertexBuffer3D(this, numVertices, data32PerVertex, multiBufferCount, isDynamic);
 	 	}

		public int createVertexArray() {
#if PLATFORM_MONOTOUCH 
			int id;
			GL.Oes.GenVertexArrays(1, out id);
			GLUtils.CheckGLError ();

			return id;
#else
			// not supported
			return -1;
#endif
		}

		public void bindVertexArray(int id) {
#if PLATFORM_MONOTOUCH 
			GL.Oes.BindVertexArray(id);
			GLUtils.CheckGLError ();
#endif
		}

		public void disposeVertexArray(int id) {
#if PLATFORM_MONOTOUCH
			GL.Oes.DeleteVertexArrays(1, ref id);
			GLUtils.CheckGLError ();
#endif
		}

 	 	
		public void dispose() {
			throw new NotImplementedException();
		}
 	 	
		public void drawToBitmapData(BitmapData destination) {
		 	throw new NotImplementedException();
		}
 	 	
		public void drawTriangles(IndexBuffer3D indexBuffer, int firstIndex = 0, int numTriangles = -1) {

			// flush sampler state before drawing
			flushSamplerState();

			int count = (numTriangles == -1) ? indexBuffer.numIndices : (numTriangles * 3);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBuffer.id);
			GLUtils.CheckGLError ();

			GL.DrawElements(BeginMode.Triangles, count, indexBuffer.elementType, new IntPtr(firstIndex));	
			GLUtils.CheckGLError ();

			// increment draw call count
			statsIncrement(Stats.DrawCalls);
		}

		public void present() {
			if (OnPresent != null)
				OnPresent(this);

			// send stats for frame
			statsSendToTelemetry();

			// begin and end span for present
			// note that this span must include the above stats call
			mSpanPresent.End();
			mSpanPresent.Begin();

			// clear draw call count
			statsClear(Stats.DrawCalls);

			// increment frame count
			mFrameCount++;
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
				program.Use();
				program.SetPositionScale(mPositionScale);
			} else {
				// ?? 
				throw new NotImplementedException();
			}

			// store current program
			mProgram = program;

			// mark all samplers that this program uses as dirty
			mSamplerDirty |= mProgram.samplerUsageMask;
		}
 
		public void setProgramConstantsFromByteArray(string programType, int firstRegister, int numRegisters, ByteArray data, uint byteArrayOffset) 
		{
			if (numRegisters == 0) return;

			if (numRegisters == -1) {
				numRegisters = (int)((data.length >>2) - byteArrayOffset);
			}

			bool isVertex = (programType == "vertex");

			// set all registers
			int register = firstRegister;
			uint dataIndex = byteArrayOffset;

			while (numRegisters > 0)
			{
				// get uniform mapped to register
				Program3D.Uniform uniform = mProgram.getUniform(isVertex, register);

				if (uniform == null) 
				{
					// skip this register
					register += 1;
					numRegisters -= 1;
					dataIndex += 16;

					if (enableErrorChecking) {
						Console.WriteLine ("warning: program register not found: {0}", register);
					}
					continue;
				}
	
				// rhow many registers are we going to writte?
				int numRegistersWritten = (uniform.RegCount < numRegisters )? uniform.RegCount : numRegisters;

				// set uniforms based on type
				//pin ByteArray data
				unsafe
				{
					// Pin the buffer to a fixed location in memory. 
					fixed (byte* dataRawByte = data.getRawArray() ) 
					{
						float* temp = (float*)(dataRawByte + dataIndex);
						switch (uniform.Type) {
						case ActiveUniformType.FloatMat2:
							GL.UniformMatrix2 (uniform.Location+firstRegister-uniform.RegIndex, uniform.Size, false, temp);
							GLUtils.CheckGLError ();
							break;
						case ActiveUniformType.FloatMat3:
							GL.UniformMatrix3 (uniform.Location+firstRegister-uniform.RegIndex, uniform.Size, false, temp);
							GLUtils.CheckGLError ();
							break;
						case ActiveUniformType.FloatMat4:
							GL.UniformMatrix4 (uniform.Location+firstRegister-uniform.RegIndex, uniform.Size, false, temp);
							GLUtils.CheckGLError ();
							break;
						default:
							GL.Uniform4 (uniform.Location+firstRegister-uniform.RegIndex, numRegistersWritten, temp);
							GLUtils.CheckGLError ();
							break;
						}
					}
				}

				dataIndex += (uint) (numRegistersWritten<<4); // <<4 <=> 16 bytes (4 floats, so *16 per registers

				// advance register number
				register     += numRegistersWritten;
				numRegisters -= numRegistersWritten;
				firstRegister+= numRegistersWritten;
			}
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
			bool isVertex = (programType == "vertex");
			double[] source = matrix.mData;

			if (transposedMatrix) {
				//    0  1  2  3
				//    4  5  6  7
				//    8  9 10 11
				//   12 13 14 15
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
				mTemp[0] = (float)source[0];
				mTemp[1] = (float)source[1];
				mTemp[2] = (float)source[2];
				mTemp[3] = (float)source[3];

				mTemp[4] = (float)source[4];
				mTemp[5] = (float)source[5];
				mTemp[6] = (float)source[6];
				mTemp[7] = (float)source[7];

				mTemp[8] = (float)source[8];
				mTemp[9] = (float)source[9];
				mTemp[10]= (float)source[10];
				mTemp[11]= (float)source[11];

				mTemp[12]= (float)source[12];
				mTemp[13]= (float)source[13];
				mTemp[14]= (float)source[14];
				mTemp[15]= (float)source[15];
			}

			// set uniform registers
			Program3D.Uniform uniform =mProgram.getUniform(isVertex, firstRegister);
			if (uniform != null) 
			{
				int deltaReg = (firstRegister - uniform.RegIndex);
				if (uniform.RegCount == 4 && (firstRegister == uniform.RegIndex)) {
					GL.UniformMatrix4 (uniform.Location + deltaReg, 1, false, mTemp);
					GLUtils.CheckGLError ();	
				} else {
					GL.Uniform4 (uniform.Location + deltaReg, uniform.RegCount - deltaReg, mTemp);
					GLUtils.CheckGLError ();
				}
			}
			else
			{
				if (enableErrorChecking) {
					Console.WriteLine ("warning: program register not found: {0}", firstRegister);
				}
			}
		}

		public void setProgramConstantsFromVector (string programType, int firstRegister, Vector<double> data, int numRegisters = -1)
		{
			if (numRegisters == 0) return;

			if (numRegisters == -1) {
				numRegisters = (int)(data.length>>2);
			}

			bool isVertex = (programType == "vertex");
		
			// set all registers
			int register = firstRegister;
			int dataIndex = 0;
			while (numRegisters > 0)
			{
				// get uniform mapped to register
				Program3D.Uniform uniform = mProgram.getUniform(isVertex, register);
				if (uniform == null)
				{
					// skip this register
					register     += 1;
					numRegisters -= 1;
					dataIndex    += 4;

					if (enableErrorChecking) 
					{
						Console.WriteLine ("warning: program register not found: {0}", register);
					}
					continue;
				}
				// convert source data into floating point
				int tempIndex = 0;
				for (int i=0; i < uniform.RegCount; i++)
				{
					// debug print the constant data
//					Console.WriteLine ("{5}[{0}]: {1}, {2}, {3}, {4}", register + i, data[dataIndex+0], data[dataIndex+1], data[dataIndex+2], data[dataIndex+3], programType);

					// convert vector4 double->float
					mTemp[tempIndex++] = (float)data[dataIndex++];
					mTemp[tempIndex++] = (float)data[dataIndex++];
					mTemp[tempIndex++] = (float)data[dataIndex++];
					mTemp[tempIndex++] = (float)data[dataIndex++];
				}
					
				// set uniforms based on type
				switch (uniform.Type) 
				{
				case ActiveUniformType.FloatMat2:
					GL.UniformMatrix2 (uniform.Location, uniform.Size, false, mTemp);
					GLUtils.CheckGLError ();
					break;
				case ActiveUniformType.FloatMat3:
					GL.UniformMatrix3 (uniform.Location, uniform.Size, false, mTemp);
					GLUtils.CheckGLError ();
					break;
				case ActiveUniformType.FloatMat4:
					GL.UniformMatrix4 (uniform.Location, uniform.Size, false, mTemp);
					GLUtils.CheckGLError ();
					break;
				default:
					GL.Uniform4 (uniform.Location, uniform.RegCount, mTemp);
					GLUtils.CheckGLError ();
					break;
				}
		
				// advance register number
				register     += uniform.RegCount;
				numRegisters -= uniform.RegCount;
			}
			
		}
 	 	

 	 	public void setRenderToBackBuffer ()
		{
#if PLATFORM_MONOTOUCH
			if (mRenderToTexture != null) {
				// discard depth buffer after doing a render to texture
				// this is a hint to GL to not keep the data around
				All discard = (All)FramebufferSlot.DepthAttachment;
				GL.Ext.DiscardFramebuffer((All)FramebufferTarget.Framebuffer, 1, ref discard);
			}
#endif

			// draw to backbuffer
			GL.BindFramebuffer (FramebufferTarget.Framebuffer, mDefaultFrameBufferId);
			GLUtils.CheckGLError ();

			// setup viewport for render to backbuffer
			GL.Viewport(0,0, mBackBufferWidth, mBackBufferHeight);
			GLUtils.CheckGLError ();

			// normal scaling
			mPositionScale[1] = 1.0f;
			if (mProgram != null) {
				mProgram.SetPositionScale(mPositionScale);
			}
			// clear render to texture
			mRenderToTexture = null;
		}
 	 	
		public void setRenderToTexture(TextureBase texture, bool enableDepthAndStencil = false, int antiAlias = 0, 
		                               int surfaceSelector = 0) {

			var texture2D = texture as Texture;
			if (texture2D == null) 
				throw new Exception("Invalid texture");

#if PLATFORM_MONOTOUCH
			// discard depth buffer before doing a render to texture
			// this is a hint to GL to not keep the data around
			All discard = (All)FramebufferSlot.DepthAttachment;
			GL.Ext.DiscardFramebuffer((All)FramebufferTarget.Framebuffer, 1, ref discard);
#endif

			if (!texture.allocated)
			{
				GL.BindTexture( TextureTarget.Texture2D, texture.textureId);
				GL.TexImage2D (TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, texture2D.width, texture2D.height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
				texture.allocated = true;
			}

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, mTextureFrameBufferId);
			GLUtils.CheckGLError ();

			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferSlot.ColorAttachment0, TextureTarget.Texture2D, texture.textureId, 0);
			GLUtils.CheckGLError ();

#if PLATFORM_MONOTOUCH
			if (enableDepthAndStencil) {
				// resize depth attachment to be the appropriate size
				GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, mTextureDepthBufferId);
				GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferInternalFormat.DepthComponent16, texture2D.width, texture2D.height);
				// set depth attachment for render to texture
				GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer,
				                           FramebufferSlot.DepthAttachment,
				                           RenderbufferTarget.Renderbuffer, 
				                           mTextureDepthBufferId);

			} else {
				// remove depth attachment
				GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer,
				                           FramebufferSlot.DepthAttachment,
				                           RenderbufferTarget.Renderbuffer, 
				                           0);
			}
#endif
			// setup viewport for render to texture
			GL.Viewport(0,0, texture2D.width, texture2D.height);
			GLUtils.CheckGLError ();

			// validate framebuffer status
			var code = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
			GLUtils.CheckGLError ();

			if (code != FramebufferErrorCode.FramebufferComplete)
			{
				throw new Exception("FrameBuffer status error:" + code);
			}

			// invert the output y to flip the texture upside down when rendering
			mPositionScale[1] = -1.0f;
			if (mProgram != null) {
				mProgram.SetPositionScale(mPositionScale);
			}

			// save texture we're rendering to
			mRenderToTexture = texture;
		}

		public void setSamplerStateAt(int sampler, string wrap, string filter, string mipfilter)
		{
			throw new System.NotImplementedException();
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
			// see if texture changed
			if (mSamplerTextures[sampler] != texture)
			{
				// set sampler texture
				mSamplerTextures[sampler] = texture;

				// set flag indicating that this sampler is dirty
				mSamplerDirty |= (1 << sampler);
			}
		}

		public void setVertexBufferAt (int index, VertexBuffer3D buffer, int bufferOffset = 0, string format = "float4")
		{
			if (buffer == null) {
				GL.DisableVertexAttribArray (index);
				GLUtils.CheckGLError ();

				GL.BindBuffer (BufferTarget.ArrayBuffer, 0);
				GLUtils.CheckGLError ();

				return;
			}		

			// enable vertex attribute array
			GL.EnableVertexAttribArray (index);
			GLUtils.CheckGLError ();

			GL.BindBuffer (BufferTarget.ArrayBuffer, buffer.id);
			GLUtils.CheckGLError ();

			IntPtr byteOffset = new IntPtr(bufferOffset * 4); // buffer offset is in 32-bit words

			// set attribute pointer within vertex buffer
			switch (format) {
			case "float4":
				GL.VertexAttribPointer(index, 4, VertexAttribPointerType.Float, false, buffer.stride, byteOffset);
				GLUtils.CheckGLError ();
				break;
			case "float3":
				GL.VertexAttribPointer(index, 3, VertexAttribPointerType.Float, false, buffer.stride, byteOffset);
				GLUtils.CheckGLError ();
				break;
			case "float2":
				GL.VertexAttribPointer(index, 2, VertexAttribPointerType.Float, false, buffer.stride, byteOffset);
				GLUtils.CheckGLError ();
				break;
			case "float1":
				GL.VertexAttribPointer(index, 1, VertexAttribPointerType.Float, false, buffer.stride, byteOffset);
				GLUtils.CheckGLError ();
				break;
			default:
				throw new NotImplementedException();
			}
		}

		/// <summary>
		/// This method flushes all sampler state. Sampler state comes from two sources, the context.setTextureAt() 
		/// and the Program3D's filtering parameters that are specified per tex instruction. Due to the way that GL works,
		/// the filtering parameters need to be associated with bound textures so that is performed here before drawing.
		/// </summary>
		private void flushSamplerState()
		{
			int sampler=0;
			// loop until all dirty samplers have been processed
			while (mSamplerDirty != 0) {

				// determine if sampler is dirty
				if ((mSamplerDirty & (1 << sampler)) != 0) {

					// activate texture unit for GL
					GL.ActiveTexture(TextureUnit.Texture0 + sampler);
					GLUtils.CheckGLError ();

					// get texture for sampler
					TextureBase texture = mSamplerTextures[sampler];
					if (texture != null) {
						// bind texture 
						var target = texture.textureTarget;

						GL.BindTexture( target, texture.textureId);
						GLUtils.CheckGLError ();

						// TODO: support sampler state overrides through setSamplerAt(...)
						// get sampler state from program
						SamplerState state = mProgram.getSamplerState(sampler);
						if (state != null) {
							// apply sampler state to texture
							texture.setSamplerState(state);
						} else {
							// use default state if program has none
							texture.setSamplerState(Context3D.DefaultSamplerState);
						}
						
						#if PLATFORM_MONODROID
						// set alpha texture
						if (texture.alphaTexture != null) {
							GL.ActiveTexture (TextureUnit.Texture4 + sampler);
							GLUtils.CheckGLError ();

							TextureBase alphaTexture = texture.alphaTexture;
							var alphaTarget = alphaTexture.textureTarget;
							GL.BindTexture (alphaTarget, alphaTexture.textureId);
							GLUtils.CheckGLError ();

							if (state != null) {
								alphaTexture.setSamplerState (state);
							} else {
								alphaTexture.setSamplerState (Context3D.DefaultSamplerState);
							}
						} 
						else {
							GL.ActiveTexture (TextureUnit.Texture4 + sampler);
							GL.BindTexture( target, texture.textureId);
						}
						#endif

					} else {
						// texture is null so unbind texture
						GL.BindTexture (TextureTarget.Texture2D, 0);
						GLUtils.CheckGLError ();
					}

					// clear dirty bit
					mSamplerDirty &= ~(1<<sampler);
				}

				// next sampler
				sampler++;
			}

		}

		// functions for updating the rendering stats
		internal void statsClear(Stats stat)
		{
			mStats[(int)stat] = 0;
		}

		internal void statsIncrement(Stats stat)
		{
			mStats[(int)stat]++;
		}

		internal void statsDecrement(Stats stat)
		{
			mStats[(int)stat]--;
		}

		internal void statsAdd(Stats stat, int value)
		{
			mStats[(int)stat]+= value;
		}

		internal void statsSubtract(Stats stat, int value)
		{
			mStats[(int)stat]-= value;
		}

		// this function sends stats to telemetry
		private void statsSendToTelemetry()
		{
			// write telemetry stats if we are connected
			if (!Telemetry.Session.Connected)
				return;

			// create telemetry values on demand
			if (mStatsValues == null) {
				mStatsValues = new Telemetry.Value[(int)Stats.Length];
				for (Stats i=0; i < Stats.Length; i++) {
					string name;
					switch (i) {
						case Stats.DrawCalls: 
							name = ".3d.resource.drawCalls";
							break;
						default:
							name = ".3d.resource." + i.ToString().toLowerCase().Replace('_', '.');
							break;
					}
					mStatsValues[(int)i] = new Telemetry.Value(name);
				}
			}

			// write all telemetry values
			for (int i=0; i < (int)Stats.Length; i++) {
				if (mStats[i] != mLastStats[i]) {
					// write stat value if it changed
					mStatsValues[i].WriteValue(mStats[i]);
					mLastStats[i] = mStats[i];
				}
			}

			// write frame count after stats
			mValueFrame.WriteValue(mFrameCount);
		}

#pragma warning disable 414		// the field below is assigned but never used
		// stage3D that owns us
		private readonly Stage3D mStage3D;
#pragma warning restore 414

		// temporary floating point array for constant conversion
		private readonly float[] mTemp = new float[4 * 1024];
	
		// current program
		private Program3D mProgram;

		// this is the post-transform scale applied to all positions coming out of the vertex shader
		private readonly float[] mPositionScale = new float[4] {1.0f, 1.0f, 1.0f, 1.0f};

		// sampler settings
		private int 				mSamplerDirty = 0;
		private TextureBase[]		mSamplerTextures = new TextureBase[Context3D.MaxSamplers];

		// settings for backbuffer
		private int  mDefaultFrameBufferId;
		private int  mBackBufferWidth = 0;
		private int  mBackBufferHeight = 0;

#pragma warning disable 414		// the fields below are assigned but never used
		private int  mDepthRenderBufferId;

		private int  mBackBufferAntiAlias = 0;
		private bool mBackBufferEnableDepthAndStencil = true;
		private bool mBackBufferWantsBestResolution = false;

		// settings for render to texture
		private TextureBase mRenderToTexture = null;

		private int  mTextureDepthBufferId;
#pragma warning restore 414

		private int  		mTextureFrameBufferId;
		// various stats
		private int 				mFrameCount;
		private readonly int[]		mStats = new int[(int)Stats.Length];
		private readonly int[]		mLastStats = new int[(int)Stats.Length];
		private Telemetry.Value[]	mStatsValues;

		private readonly Telemetry.Span  mSpanPresent = new Telemetry.Span(".rend.molehill.present");
		private readonly Telemetry.Value mValueFrame = new Telemetry.Value(".rend.molehill.frame");

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
