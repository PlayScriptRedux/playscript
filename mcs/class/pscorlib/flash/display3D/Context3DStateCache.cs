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
	
	public class Context3DStateCache  
	{

		// blend
		private string  		_srcBlendFactor;
		private string  		_destlendFactor;

		// depth test
		private bool 			_deptTestEnabled;
		private bool 		   	_depthTestMask;
		private string 		   	_depthTestCompareMode;

		// program
		private Program3D 		_program;

		// culling 
		private string			_cullingMode;

		// texture
		private int				_activeTexture;

		// vertex array			
		private int 			_activeVertexArray;

		// viewport
		private int 			_viewportOriginX;
		private int 			_viewportOriginY;
		private int 			_viewportWidth;
		private int 			_viewportHeight;

		// registers
		private const int MAX_NUM_REGISTERS 	= 1024;
		private const int FLOATS_PER_REGISTER 	= 4;

		private float [] 	    _registers  = new float[MAX_NUM_REGISTERS * FLOATS_PER_REGISTER];


		public Context3DStateCache(){
			clearSettings ();
		}

		public void clearSettings()
		{

			_srcBlendFactor			= "";
			_destlendFactor 		= "";
			_deptTestEnabled 		= false;
			_depthTestMask 			= false;
			_depthTestCompareMode 	= "";
			_program 				= null;
			_cullingMode 			= "";
			_activeTexture 			= -1;
			_activeVertexArray 		= -1;
			_viewportOriginX 		= -1;
			_viewportOriginY 		= -1;
			_viewportWidth 			= -1;
			_viewportHeight 		= -1;

			clearRegisters ();
		}

		private void clearRegisters()
		{
			int numFloats = MAX_NUM_REGISTERS * FLOATS_PER_REGISTER;
			for (int c = 0; c < numFloats; ++c) 
			{
				_registers [c] = (-999999999.0f);
			}
		}

		[inline]
		public bool updateBlendSrcFactor(string factor)
		{
			if (factor == _srcBlendFactor)
				return false;
			_srcBlendFactor = factor;
			return true;
		}

		[inline]
		public bool updateBlendDestFactor(string factor)
		{
			if (factor == _destlendFactor)
				return false;
			_destlendFactor = factor;
			return true;
		}

		[inline]
		public bool updateDepthTestEnabled(bool test)
		{
			if (test == _deptTestEnabled)
				return false;
			_deptTestEnabled = test;
			return true;
		}

		[inline]
		public bool updateDepthTestMask(bool mask)
		{
			if (mask == _depthTestMask)
				return false;
			_depthTestMask = mask;
			return true;
		}

		[inline]
		public bool updateDepthCompareMode(string mode)
		{
			if (mode == _depthTestCompareMode)
				return false;
			_depthTestCompareMode = mode;
			return true;
		}

		[inline]
		public bool updateProgram3D(Program3D program3d)
		{
			if (program3d == _program)
				return false;
			_program = program3d;
			return true;
		}

		[inline]
		public bool updateCullingMode(string cullMode)
		{
			if (cullMode == _cullingMode)
				return false;
			_cullingMode = cullMode;
			return true;
		}

		[inline]
		public bool updateActiveTextureSample(int texture)
		{
			if (texture == _activeTexture)
				return false;
			_activeTexture = texture;
			return true;
		}

		[inline]
		public bool updateActiveVertexArray(int vertexArray)
		{
			if (vertexArray == _activeVertexArray)
				return false;
			_activeVertexArray = vertexArray;
			return true;
		}

		[inline]
		public bool updateViewport(int originX, int originY, int width, int height)
		{
			if (_viewportOriginX == originX && _viewportOriginY == originY && _viewportWidth == width && _viewportHeight == height)
				return false;

			_viewportOriginX = originX;
			_viewportOriginY = originY;
			_viewportWidth   = width;
			_viewportHeight  = height;

			return true;
		}

		public bool updateRegisters(float[] mTemp, int startRegister, int numRegisters)
		{
			return true;

			/*
			bool needToUpdate		= false;
			int  startFloat 		= startRegister * FLOATS_PER_REGISTER;
			int  numFloat   		= numRegisters  * FLOATS_PER_REGISTER;
			int  inCounter 			= 0;

			while (numFloat != 0)
			{
				if (_registers [startFloat] != mTemp [inCounter]) 
				{
					_registers [startFloat] = mTemp [inCounter];
					needToUpdate = true;
				}
				
				--numFloat;
				++startFloat;
				++inCounter;
			}

			return needToUpdate;*/
		}

	}

}
