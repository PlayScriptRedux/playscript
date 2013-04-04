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
	
	using System;

#if PLATFORM_MONOMAC
	using MonoMac.OpenGL;
#elif PLATFORM_MONOTOUCH
	using OpenTK.Graphics.ES20;
#endif

	/// <summary>
	/// Represents a collection of sampler state to be set together. 
	/// Usually parsed from an AGAL shader and associated with a tex instruction source
	/// </summary>
	public class SamplerState : IEquatable<SamplerState>
	{
#if OPENGL
		public TextureMinFilter		MinFilter;
		public TextureMagFilter		MagFilter;
		public TextureWrapMode		WrapModeS;
		public TextureWrapMode		WrapModeT;
		public float				LodBias;
		public float 				MaxAniso;
		
		public override string ToString ()
		{
			return string.Format ("[SamplerState min:{0} mag:{1} wrapS:{2} wrapT:{3} bias:{4} aniso:{5}]]", 
			                      MinFilter, MagFilter, WrapModeS, WrapModeT, LodBias, MaxAniso);
		}
		
		#region IEquatable implementation
		public bool Equals (SamplerState other)
		{
			return this.MinFilter == other.MinFilter &&
				this.MagFilter == other.MagFilter &&
					this.WrapModeS == other.WrapModeS &&
					this.WrapModeT == other.WrapModeT &&
					this.LodBias == other.LodBias &&
					MaxAniso == other.MaxAniso;
		}
		#endregion

#else
		#region IEquatable implementation
		public bool Equals (SamplerState other)
		{
			throw new NotImplementedException();
		}
		#endregion

#endif
	}
}
