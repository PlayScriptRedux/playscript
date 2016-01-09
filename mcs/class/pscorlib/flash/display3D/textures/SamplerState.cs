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

namespace flash.display3D.textures
{
	using System;
	using System.Collections.Generic;
#if PLATFORM_MONOMAC
	using MonoMac.OpenGL;
#elif PLATFORM_XAMMAC
	using OpenTK;
	using OpenTK.Graphics;
	using OpenTK.Graphics.OpenGL;
	using OpenTK.Platform.MacOS;
	using Foundation;
	using CoreGraphics;
	using OpenGL;
	using GLKit;
	using AppKit;
#elif PLATFORM_MONOTOUCH
	using OpenTK.Graphics.ES20;
#elif PLATFORM_MONODROID
	using OpenTK.Graphics.ES20;
#endif

	/// <summary>
	/// Represents a collection of sampler state to be set together. 
	/// Usually parsed from an AGAL shader and associated with a tex instruction source
	/// </summary>
	public class SamplerState : IEquatable<SamplerState>
	{
		#if OPENGL
		public readonly TextureMinFilter	MinFilter;
		public readonly TextureMagFilter	MagFilter;
		public readonly TextureWrapMode WrapModeS;
		public readonly TextureWrapMode WrapModeT;
		public readonly float LodBias;
		public readonly float MaxAniso;

		public SamplerState (TextureMinFilter minFilter, TextureMagFilter magFilter, TextureWrapMode wrapModeS, TextureWrapMode wrapModeT, 
		                    float lodBias = 0.0f, float maxAniso = 0.0f)
		{
			this.MinFilter = minFilter;
			this.MagFilter = magFilter;
			this.WrapModeS = wrapModeS;
			this.WrapModeT = wrapModeT;
			this.LodBias = lodBias;
			this.MaxAniso = maxAniso;
		}


		/// <summary>
		/// Interns this sampler state. 
		/// Interning will return an object of the same value but makes equality testing faster since 
		/// it is a reference comparison only.
		/// </summary>
		public SamplerState Intern ()
		{
			if (mIsInterned)
				return this;

			// find intern'd object that equals this
			// TODO: use hash or dictionary
			foreach (var intern in sInterns) {
				if (intern.Equals (this)) {
					return intern;
				}
			}

			// intern this object
			sInterns.Add (this);
			mIsInterned = true;
			return this;
		}

		public override string ToString ()
		{
			return string.Format ("[SamplerState min:{0} mag:{1} wrapS:{2} wrapT:{3} bias:{4} aniso:{5}]]", 
				MinFilter, MagFilter, WrapModeS, WrapModeT, LodBias, MaxAniso);
		}

		public bool Equals (SamplerState other)
		{
			// handle reference equality
			if (this == other)
				return true;

			// handle null case
			if (other == null)
				return false;

			return this.MinFilter == other.MinFilter &&
			this.MagFilter == other.MagFilter &&
			this.WrapModeS == other.WrapModeS &&
			this.WrapModeT == other.WrapModeT &&
			this.LodBias == other.LodBias &&
			MaxAniso == other.MaxAniso;
		}

		private bool mIsInterned = false;

		private static List<SamplerState> sInterns = new List<SamplerState> ();

		#else
		
		public bool Equals (SamplerState other)
		{
			throw new NotImplementedException();
		}

		#endif
	}
}
