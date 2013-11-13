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

#if OPENGL

using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using flash.utils;

#if PLATFORM_MONOMAC
using MonoMac.OpenGL;
#elif PLATFORM_MONOTOUCH
using OpenTK.Graphics.ES20;
#elif PLATFORM_MONODROID
using OpenTK.Graphics.ES20;
#endif

namespace flash.display3D
{
	public static class AGALConverter
	{

		enum RegType
		{
			Attribute,
			Constant,
			Temporary,
			Output,
			Varying,
			Sampler
		};

		public enum ProgramType
		{
			Vertex,
			Fragment
		}

		class DestReg
		{
			public ProgramType programType; 
			public RegType type;
			public int 	mask;
			public int 	n;

			public static DestReg Parse (uint v, ProgramType programType)
			{
				var dr = new DestReg();
				dr.programType = programType;
				dr.type = (RegType)((v >> 24) & 0xF);
				dr.mask = (int)((v >> 16) & 0xF);
				dr.n = (int)(v & 0xFFFF);
				return dr;
			}

			public string GetWriteMask()
			{
				string str = ".";
				if ((mask & 1)!=0) str += "x";
				if ((mask & 2)!=0) str += "y";
				if ((mask & 4)!=0) str += "z";
				if ((mask & 8)!=0) str += "w";
				return str;
			}

			public string ToGLSL (bool useMask = true)
			{
				string str;
				if (type == RegType.Output) {
					str = programType == ProgramType.Vertex ? "gl_Position" : "gl_FragColor";
				}
				else
				{
					str = PrefixFromType (type, programType);
					str += n.ToString ();
				}

				if (useMask && mask != 0xF) {
					str += GetWriteMask();
				}
				return str;
			}
		};

		class SourceReg
		{
			public ProgramType programType; 
			public int d;
			public int q;
			public RegType itype;
			public RegType type;
			public int s;
			public int o;
			public int n;
			public int sourceMask;

			public static SourceReg Parse (ulong v, ProgramType programType, int sourceMask)
			{
				var sr = new SourceReg();
				sr.programType = programType;
				sr.d = (int)((v >> 63) & 1); //  Direct=0/Indirect=1 for direct Q and I are ignored, 1bit
				sr.q = (int)((v >> 48) & 0x3); // index register component select
				sr.itype = (RegType)((v >> 40) & 0xF); // index register type
				sr.type = (RegType)((v >> 32) & 0xF); // type
				sr.s = (int)((v >> 24) & 0xFF); // swizzle
				sr.o = (int)((v >> 16) & 0xFF);  // indirect offset
				sr.n = (int)(v & 0xFFFF);		// number
				sr.sourceMask = sourceMask;
				return sr;
			}

			public string ToGLSL (bool emitSwizzle = true, int offset = 0)
			{
				if (type == RegType.Output) {
					return programType == ProgramType.Vertex ? "gl_Position" : "gl_FragColor";
				}


				bool fullxyzw = (s == 228) && (sourceMask == 0xF);

				var swizzle = "";
				if (type != RegType.Sampler && !fullxyzw) {
					for (var i=0; i < 4; i++) {

						// only output swizzles for each source mask
						if ((sourceMask & (1<<i))!=0)
						{
							switch ((s >> (i * 2)) & 3) {
							case 0:
								swizzle += "x";
								break;
							case 1:
								swizzle += "y";
								break;
							case 2:
								swizzle += "z";
								break;
							case 3:
								swizzle += "w";
								break;
								
							}
						}
					}
				}

				string str = PrefixFromType(type, programType);
				if (d == 0) {
					// direct register
					str += (n + offset).ToString();
				} else {
					// indirect register
					str += o.ToString();
					char indexComponent = (char)('x' + q);  
					var indexRegister = PrefixFromType(itype, programType) + this.n.ToString() + "." + indexComponent.ToString(); 
					str += "[ int(" + indexRegister + ") +" + offset.ToString() + "]";
				}

				if (emitSwizzle && swizzle != "") {
					str += "." + swizzle;
				}
				return str;
			}
		};

		class SamplerReg
		{
			public ProgramType programType; 
			public int f; // Filter (0=nearest,1=linear) (4 bits)
			public int m; // Mipmap (0=disable,1=nearest, 2=linear)
			public int w; // wrap (0=clamp 1=repeat)
			public int s; // special flags bit 
			public int d; // dimension 0=2d 1=cube
			public RegType type;
			public int b; // lod bias
			public int n; // number 
			
			public static SamplerReg Parse (ulong v, ProgramType programType)
			{
				var sr = new SamplerReg();
				sr.programType = programType;
				sr.f = (int)((v >> 60) & 0xF); // filter
				sr.m = (int)((v >> 56) & 0xF); // mipmap
				sr.w = (int)((v >> 52) & 0xF); // wrap
				sr.s = (int)((v >> 48) & 0xF); // special
				sr.d = (int)((v >> 44) & 0xF); // dimension
				sr.type = (RegType)((v >> 32) & 0xF); // type
				sr.b = (int)((v >> 16) & 0xFF); 
				sr.n = (int)(v & 0xFFFF);		// number
				return sr;
			}
			
			public string ToGLSL ()
			{
				var str = PrefixFromType (type, programType);
				str += n.ToString ();
				return str;
			}

			public textures.SamplerState ToSamplerState()
			{
				TextureMagFilter magFilter;
				TextureMinFilter minFilter;
				TextureWrapMode  wrapModeS;
				TextureWrapMode  wrapModeT;

				// translate mag filter
				switch (f)
				{
				case 0:	
					magFilter = TextureMagFilter.Nearest;		
					break;
				case 1:	
					magFilter = TextureMagFilter.Linear;		
					break;
				default:
					throw new NotImplementedException();
				}

				// translate min filter
				switch (m)
				{
					// disable
				case 0:	
#if !PLATFORM_MONODROID
					minFilter = (f!=0) ? TextureMinFilter.Linear : TextureMinFilter.Nearest;	
#else
					// TODO: figure out the right value for this
					minFilter = (TextureMinFilter)((f!=0) ? TextureMagFilter.Linear : TextureMagFilter.Nearest);
#endif
					break;
					// nearest
				case 1:	
					minFilter = (f!=0) ? TextureMinFilter.NearestMipmapLinear : TextureMinFilter.NearestMipmapNearest;		
					break;
					// linear
				case 2:	
					minFilter = (f!=0) ? TextureMinFilter.LinearMipmapLinear : TextureMinFilter.LinearMipmapNearest;		
					break;
				default:
					throw new NotImplementedException();
				}

				// translate wrapping mode
				switch (w)
				{
				case 0:
					wrapModeS = TextureWrapMode.ClampToEdge;
					wrapModeT = TextureWrapMode.ClampToEdge;
					break;
				case 1:
					wrapModeS = TextureWrapMode.Repeat;
					wrapModeT = TextureWrapMode.Repeat;
					break;
				default:
					throw new NotImplementedException();
				}

				// translate lod bias, sign extend and /8
				float lodBias = ((float)((b << 24) >> 24)) / 8.0f;

				float maxAniso = 0.0f;
				// create sampler state and intern to allow for faster equality comparisons
				return new textures.SamplerState(minFilter, magFilter, wrapModeS, wrapModeT, lodBias, maxAniso).Intern();
			}
		};



		enum RegisterUsage
		{
			Unused,
			Vector4,
			Matrix44,
			Sampler2D,
			Sampler2DAlpha,
			SamplerCube,
			Vector4Array
		};

		class RegisterMap
		{

			class Entry
			{
				public RegType type;
				public int number;
				public string name;
				public RegisterUsage usage;
			};

			public RegisterMap()
			{
			}

			public RegisterUsage GetUsage(RegType type, string name, int number)
			{
				foreach (var entry in mEntries) {
					if (entry.type == type && entry.name == name && entry.number == number) {
						return entry.usage;
					}
				}
				return RegisterUsage.Unused;
			}

			public RegisterUsage GetUsage(SourceReg sr)
			{
				if (sr.d != 0) {
					return RegisterUsage.Vector4Array;
				}
				return GetUsage(sr.type, sr.ToGLSL(false), sr.n);
			}


			public void Add(SourceReg sr, RegisterUsage usage, int offset  = 0)
			{
				if (sr.d != 0) {
					Add (sr.itype, PrefixFromType(sr.itype, sr.programType) + sr.n.ToString(), sr.n, RegisterUsage.Vector4);
					Add (sr.type, PrefixFromType(sr.type, sr.programType) + sr.o.ToString(), sr.o, RegisterUsage.Vector4Array);
					return;
				}
				Add (sr.type, sr.ToGLSL(false, offset), sr.n + offset, usage);
			}

			public void Add(SamplerReg sr, RegisterUsage usage)
			{
				Add (sr.type, sr.ToGLSL(), sr.n, usage);
			}

			public void Add(DestReg dr, RegisterUsage usage)
			{
				Add (dr.type, dr.ToGLSL(false), dr.n, usage);
			}

			public void Add (RegType type, string name, int number, RegisterUsage usage)
			{
				foreach (var entry in mEntries) {

					if (entry.type == type && entry.name == name && entry.number == number) {
						if (entry.usage != usage) {
							throw new InvalidOperationException ("Cannot use register in multiple ways yet (mat4/vec4)");
						}
						return;
					}
				}

				{
					var entry = new Entry ();
					entry.type = type;
					entry.name = name;
					entry.number = number;
					entry.usage = usage;
					mEntries.Add (entry);
				}
			}

			public string ToGLSL (bool tempRegsOnly)
			{
				mEntries.Sort( (Entry a, Entry b) => {
					if (a.type != b.type) {
						return a.type - b.type;
					} else {
						return a.number - b.number;
					}
				}
				);

				var sb = new StringBuilder ();
				for(int i=0;i<mEntries.Count;i++)
				{
					Entry entry = mEntries [i];
				
					// only emit temporary registers based on boolean passed in
					// this is so temp registers can be grouped in the main() block
					if (
						(tempRegsOnly && entry.type != RegType.Temporary) ||
						(!tempRegsOnly && entry.type == RegType.Temporary)
						)
					{
						continue;
					}


					// dont emit output registers
					if (entry.type == RegType.Output)
					{
						continue;
					}

					switch (entry.type)
					{
					case RegType.Attribute:
						// sb.AppendFormat("layout(location = {0}) ", entry.number); 
						sb.Append("attribute ");
						break;
					case RegType.Constant:
						//sb.AppendFormat("layout(location = {0}) ", entry.number); 
						sb.Append("uniform ");
						break;
					case RegType.Temporary:
						sb.Append("\t");
						break;
					case RegType.Output:
						break;
					case RegType.Varying:
						sb.Append("varying ");
						break;
					case RegType.Sampler:
						sb.Append("uniform ");
						break;
					default:
						throw new NotImplementedException();
					}

					switch (entry.usage)
					{
					case RegisterUsage.Vector4:
					case RegisterUsage.Vector4Array:
						sb.Append("vec4 ");
						break;
					case RegisterUsage.Matrix44:
						sb.Append("mat4 ");
						break;
					case RegisterUsage.Sampler2D:
						sb.Append("sampler2D ");
						break;
					case RegisterUsage.SamplerCube:
						sb.Append("samplerCube ");
						break;
					}

					if (entry.usage == RegisterUsage.Sampler2DAlpha) {
						sb.Append ("sampler2D ");
						sb.Append (entry.name);
						sb.AppendLine (";");

						sb.Append ("uniform ");
						sb.Append ("sampler2D ");
						sb.Append (entry.name + "_alpha");
						sb.AppendLine (";");
					} else if (entry.usage == RegisterUsage.Vector4Array) {
						int count = 128;
						if (i < mEntries.Count - 1) //find how many registers based on the next entry.
							count = mEntries [i + 1].number - entry.number;
						sb.AppendFormat ("{0}[{1}]", entry.name, count);// this is an array of "count" elements.
						sb.AppendLine (";");
					} else {
						sb.Append (entry.name);
						sb.AppendLine (";");
					}
				}
				return sb.ToString();
			}


			private List<Entry> mEntries = new List<Entry>();
		};

		private static string PrefixFromType (RegType t, ProgramType pt)
		{
			switch (t) {
			case RegType.Attribute: return "va";
			case RegType.Constant:  return (pt == ProgramType.Vertex) ? "vc" : "fc";
			case RegType.Temporary: return (pt == ProgramType.Vertex) ? "vt" : "ft";
			case RegType.Output:    return "output_";
			case RegType.Varying:   return "v";
			case RegType.Sampler:   return "sampler";
			default:
				throw new InvalidOperationException("Invalid data!");
			}
		}


		private static ulong ReadUInt64 (ByteArray ba)
		{
			ulong lo = (ulong)(uint)ba.readInt();
			ulong hi = (ulong)(uint)ba.readInt();
			return (hi << 32) | lo;
		}

		public static string ConvertToGLSL (ByteArray agal, textures.SamplerState[] outSamplers)
		{
			agal.position = 0;

			int magic = agal.readByte ();
			if (magic == 0xB0) {
				// use embedded GLSL shader instead
				return agal.readUTF();
			}

			if (magic != 0xA0) {
				throw new InvalidOperationException ("Magic value must be 0xA0, may not be AGAL");
			}

			int version = agal.readInt ();
			if (version != 1) {
				throw new InvalidOperationException ("Version must be 1");
			}

			int shaderTypeId = agal.readByte ();
			if (shaderTypeId != 0xA1) {
				throw new InvalidOperationException ("Shader type id must be 0xA1");
			}

			ProgramType programType = (agal.readByte () == 0) ? ProgramType.Vertex : ProgramType.Fragment;

			var map = new RegisterMap();
			var sb = new StringBuilder();
			while (agal.position < agal.length) {

				// fetch instruction info
				int opcode = agal.readInt();
				uint dest = (uint)agal.readInt();
				ulong source1 = ReadUInt64(agal);
				ulong source2 = ReadUInt64(agal);
//				sb.Append("\t");
//				sb.AppendFormat("// opcode:{0:X} dest:{1:X} source1:{2:X} source2:{3:X}\n", opcode,
//				                dest, source1, source2);

				// parse registers
				var dr  = DestReg.Parse(dest,programType);
				var sr1 = SourceReg.Parse(source1,programType, dr.mask);
				var sr2 = SourceReg.Parse(source2,programType, dr.mask);

				// switch on opcode and emit GLSL 
				sb.Append("\t");
				switch (opcode)
				{
				case 0x00: // mov
					sb.AppendFormat("{0} = {1}; // mov", dr.ToGLSL(), sr1.ToGLSL()); 
					map.Add(dr, RegisterUsage.Vector4);
					map.Add(sr1, RegisterUsage.Vector4);
					break;
					
				case 0x01: // add
					sb.AppendFormat("{0} = {1} + {2}; // add", dr.ToGLSL(), sr1.ToGLSL(), sr2.ToGLSL() ); 
					map.Add(dr, RegisterUsage.Vector4);
					map.Add(sr1, RegisterUsage.Vector4);
					map.Add(sr2, RegisterUsage.Vector4);
					break;
					
				case 0x02: // sub
					sb.AppendFormat("{0} = {1} - {2}; // sub", dr.ToGLSL(), sr1.ToGLSL(), sr2.ToGLSL() ); 
					map.Add(dr, RegisterUsage.Vector4);
					map.Add(sr1, RegisterUsage.Vector4);
					map.Add(sr2, RegisterUsage.Vector4);
					break;
					
				case 0x03: // mul
					sb.AppendFormat("{0} = {1} * {2}; // mul", dr.ToGLSL(), sr1.ToGLSL(), sr2.ToGLSL() ); 
					map.Add(dr, RegisterUsage.Vector4);
					map.Add(sr1, RegisterUsage.Vector4);
					map.Add(sr2, RegisterUsage.Vector4);
					break;
					
				case 0x04: // div
					sb.AppendFormat("{0} = {1} / {2}; // div", dr.ToGLSL(), sr1.ToGLSL(), sr2.ToGLSL() ); 
					map.Add(dr, RegisterUsage.Vector4);
					map.Add(sr1, RegisterUsage.Vector4);
					map.Add(sr2, RegisterUsage.Vector4);
					break;

				case 0x05: // rcp
					sb.AppendFormat("{0} = vec4(1) / {1}; // rcp (untested)", dr.ToGLSL(), sr1.ToGLSL() ); 
					map.Add(dr, RegisterUsage.Vector4);
					map.Add(sr1, RegisterUsage.Vector4);
					break;

				case 0x06: // min
					sb.AppendFormat("{0} = min({1}, {2}); // min", dr.ToGLSL(), sr1.ToGLSL(), sr2.ToGLSL() ); 
					map.Add(dr, RegisterUsage.Vector4);
					map.Add(sr1, RegisterUsage.Vector4);
					map.Add(sr2, RegisterUsage.Vector4);
					break;
					
				case 0x07: // max
					sb.AppendFormat("{0} = max({1}, {2}); // max", dr.ToGLSL(), sr1.ToGLSL(), sr2.ToGLSL() ); 
					map.Add(dr, RegisterUsage.Vector4);
					map.Add(sr1, RegisterUsage.Vector4);
					map.Add(sr2, RegisterUsage.Vector4);
					break;

				case 0x08: // frc
					sb.AppendFormat("{0} = fract({1}); // frc", dr.ToGLSL(), sr1.ToGLSL() ); 
					map.Add(dr, RegisterUsage.Vector4);
					map.Add(sr1, RegisterUsage.Vector4);
					break;
					
				case 0x09: // sqrt
					sb.AppendFormat("{0} = sqrt({1}); // sqrt", dr.ToGLSL(), sr1.ToGLSL() ); 
					map.Add(dr, RegisterUsage.Vector4);
					map.Add(sr1, RegisterUsage.Vector4);
					break;
					
				case 0x0A: // rsq
					sb.AppendFormat("{0} = inversesqrt({1}); // rsq", dr.ToGLSL(), sr1.ToGLSL() ); 
					map.Add(dr, RegisterUsage.Vector4);
					map.Add(sr1, RegisterUsage.Vector4);
					break;
					
				case 0x0B: // pow
					sb.AppendFormat("{0} = pow({1}, {2}); // pow", dr.ToGLSL(), sr1.ToGLSL(), sr2.ToGLSL() ); 
					map.Add(dr, RegisterUsage.Vector4);
					map.Add(sr1, RegisterUsage.Vector4);
					map.Add(sr2, RegisterUsage.Vector4);
					break;
				
				case 0x0C: // log
					sb.AppendFormat("{0} = log2({1}); // log", dr.ToGLSL(), sr1.ToGLSL()); 
					map.Add(dr, RegisterUsage.Vector4);
					map.Add(sr1, RegisterUsage.Vector4);
					break;

				case 0x0D: // exp
					sb.AppendFormat("{0} = exp2({1}); // exp", dr.ToGLSL(), sr1.ToGLSL()); 
					map.Add(dr, RegisterUsage.Vector4);
					map.Add(sr1, RegisterUsage.Vector4);
					break;

				case 0x0E: // normalize
					sb.AppendFormat("{0} = normalize({1}); // normalize", dr.ToGLSL(), sr1.ToGLSL() ); 
					map.Add(dr, RegisterUsage.Vector4);
					map.Add(sr1, RegisterUsage.Vector4);
					break;

				case 0x0F: // sin
					sb.AppendFormat("{0} = sin({1}); // sin", dr.ToGLSL(), sr1.ToGLSL() ); 
					map.Add(dr, RegisterUsage.Vector4);
					map.Add(sr1, RegisterUsage.Vector4);
					break;

				case 0x10: // cos
					sb.AppendFormat("{0} = cos({1}); // cos", dr.ToGLSL(), sr1.ToGLSL() ); 
					map.Add(dr, RegisterUsage.Vector4);
					map.Add(sr1, RegisterUsage.Vector4);
					break;

				case 0x11: // crs
					sr1.sourceMask = sr2.sourceMask = 7; // adjust source mask for xyz input to dot product
					sb.AppendFormat("{0} = cross(vec3({1}), vec3({2})); // crs", dr.ToGLSL(), sr1.ToGLSL(), sr2.ToGLSL() ); 
					map.Add(dr, RegisterUsage.Vector4);
					map.Add(sr1, RegisterUsage.Vector4);
					map.Add(sr2, RegisterUsage.Vector4);
					break;

				case 0x12: // dp3
					sr1.sourceMask = sr2.sourceMask = 7; // adjust source mask for xyz input to dot product
					sb.AppendFormat("{0} = vec4(dot(vec3({1}), vec3({2}))){3}; // dp3", dr.ToGLSL(), sr1.ToGLSL(), sr2.ToGLSL(), dr.GetWriteMask() ); 
					map.Add(dr, RegisterUsage.Vector4);
					map.Add(sr1, RegisterUsage.Vector4);
					map.Add(sr2, RegisterUsage.Vector4);
					break;
					
				case 0x13: // dp4
					sr1.sourceMask = sr2.sourceMask = 0xF; // adjust source mask for xyzw input to dot product
					sb.AppendFormat("{0} = vec4(dot(vec4({1}), vec4({2}))){3}; // dp4", dr.ToGLSL(), sr1.ToGLSL(), sr2.ToGLSL(), dr.GetWriteMask() ); 
					map.Add(dr, RegisterUsage.Vector4);
					map.Add(sr1, RegisterUsage.Vector4);
					map.Add(sr2, RegisterUsage.Vector4);
					break;

				case 0x14: // abs
					sb.AppendFormat("{0} = abs({1}); // abs", dr.ToGLSL(), sr1.ToGLSL() ); 
					map.Add(dr, RegisterUsage.Vector4);
					map.Add(sr1, RegisterUsage.Vector4);
					break;

				case 0x15: // neg
					sb.AppendFormat("{0} = -{1}; // neg", dr.ToGLSL(), sr1.ToGLSL() ); 
					map.Add(dr, RegisterUsage.Vector4);
					map.Add(sr1, RegisterUsage.Vector4);
					break;

				case 0x16: // saturate
					sb.AppendFormat("{0} = clamp({1}, 0.0, 1.0); // saturate", dr.ToGLSL(), sr1.ToGLSL() ); 
					map.Add(dr, RegisterUsage.Vector4);
					map.Add(sr1, RegisterUsage.Vector4);
					break;

				case 0x17: // m33
					//destination.x = (source1.x * source2[0].x) + (source1.y * source2[0].y) + (source1.z * source2[0].z)
					//destination.y = (source1.x * source2[1].x) + (source1.y * source2[1].y) + (source1.z * source2[1].z)
					//destination.z = (source1.x * source2[2].x) + (source1.y * source2[2].y) + (source1.z * source2[2].z)
				{
					var existingUsage = map.GetUsage(sr2);
					if (existingUsage != RegisterUsage.Vector4 && existingUsage != RegisterUsage.Vector4Array)
					{
						sb.AppendFormat("{0} = {1} * mat3({2}); // m33", dr.ToGLSL(), sr1.ToGLSL(), sr2.ToGLSL(false) ); 
						map.Add(dr, RegisterUsage.Vector4);
						map.Add(sr1, RegisterUsage.Vector4);
						map.Add(sr2, RegisterUsage.Matrix44); // 33?
					}
					else
					{
                        // compose the matrix multiply from dot products
                        sr1.sourceMask = sr2.sourceMask = 7;
                        sb.AppendFormat("{0} = vec3(dot({1},{2}), dot({1},{3}), dot({1},{4})); // m33", dr.ToGLSL(), sr1.ToGLSL(true), 
                                        sr2.ToGLSL(true,0),
                                        sr2.ToGLSL(true,1), 
                                        sr2.ToGLSL(true,2) 
                                        ); 

                        map.Add(dr, RegisterUsage.Vector4);
						map.Add(sr1, RegisterUsage.Vector4);
						map.Add(sr2, RegisterUsage.Vector4, 0); 
						map.Add(sr2, RegisterUsage.Vector4, 1); 
						map.Add(sr2, RegisterUsage.Vector4, 2); 
					}
				}
					break;

				case 0x18: // m44
			//multiply matrix 4x4
			//destination.x = (source1.x * source2[0].x) + (source1.y * source2[0].y) + (source1.z * source2[0].z)+ (source1.w * source2[0].w)
			//destination.y = (source1.x * source2[1].x) + (source1.y * source2[1].y) + (source1.z * source2[1].z)+ (source1.w * source2[1].w)
			//destination.z = (source1.x * source2[2].x) + (source1.y * source2[2].y) + (source1.z * source2[2].z)+ (source1.w * source2[2].w)
			//destination.w = (source1.x * source2[3].x) + (source1.y * source2[3].y) + (source1.z * source2[3].z)+ (source1.w * source2[3].w)
				{
					var existingUsage = map.GetUsage(sr2);
					if (existingUsage != RegisterUsage.Vector4 && existingUsage != RegisterUsage.Vector4Array)
					{
						sb.AppendFormat("{0} = {1} * {2}; // m44", dr.ToGLSL(), sr1.ToGLSL(), sr2.ToGLSL(false) ); 
						map.Add(dr, RegisterUsage.Vector4);
						map.Add(sr1, RegisterUsage.Vector4);
						map.Add(sr2, RegisterUsage.Matrix44);
					}
					else
					{
                        // compose the matrix multiply from dot products
                        sr1.sourceMask = sr2.sourceMask = 0xF;
                        sb.AppendFormat("{0} = vec4(dot({1},{2}), dot({1},{3}), dot({1},{4}, dot({1},{5})); // m44", dr.ToGLSL(), sr1.ToGLSL(true), 
                                        sr2.ToGLSL(true,0),
                                        sr2.ToGLSL(true,1), 
                                        sr2.ToGLSL(true,2), 
                                        sr2.ToGLSL(true,3) 
                                        ); 

                        map.Add(dr, RegisterUsage.Vector4);
						map.Add(sr1, RegisterUsage.Vector4);
						map.Add(sr2, RegisterUsage.Vector4, 0); 
						map.Add(sr2, RegisterUsage.Vector4, 1); 
						map.Add(sr2, RegisterUsage.Vector4, 2); 
						map.Add(sr2, RegisterUsage.Vector4, 3); 
					}
				}
					break;

				case 0x19: // m34
					//m34 0x19 multiply matrix 3x4
					//destination.x = (source1.x * source2[0].x) + (source1.y * source2[0].y) + (source1.z * source2[0].z)+ (source1.w * source2[0].w)
					//destination.y = (source1.x * source2[1].x) + (source1.y * source2[1].y) + (source1.z * source2[1].z)+ (source1.w * source2[1].w)
					//destination.z = (source1.x * source2[2].x) + (source1.y * source2[2].y) + (source1.z * source2[2].z)+ (source1.w * source2[2].w)
				{
					// prevent w from being written for a m34
					dr.mask &= 7;

					var existingUsage = map.GetUsage(sr2);
					if (existingUsage != RegisterUsage.Vector4 && existingUsage != RegisterUsage.Vector4Array)
					{
						sb.AppendFormat("{0} = {1} * {2}; // m34", dr.ToGLSL(), sr1.ToGLSL(), sr2.ToGLSL(false) ); 
						map.Add(dr, RegisterUsage.Vector4);
						map.Add(sr1, RegisterUsage.Vector4);
						map.Add(sr2, RegisterUsage.Matrix44);
					}
					else
					{
                        // compose the matrix multiply from dot products
                        sr1.sourceMask = sr2.sourceMask = 0xF;
                        sb.AppendFormat("{0} = vec3(dot({1},{2}), dot({1},{3}), dot({1},{4})); // m34", dr.ToGLSL(), sr1.ToGLSL(true), 
                                        sr2.ToGLSL(true,0),
                                        sr2.ToGLSL(true,1), 
                                        sr2.ToGLSL(true,2)
                                        ); 

                        map.Add(dr, RegisterUsage.Vector4);
						map.Add(sr1, RegisterUsage.Vector4);
						map.Add(sr2, RegisterUsage.Vector4, 0); 
						map.Add(sr2, RegisterUsage.Vector4, 1); 
						map.Add(sr2, RegisterUsage.Vector4, 2); 
					}
				}
					break;

				case 0x27: // kill /  discard
					if (flash.display.Stage3D.allowDiscard) {
						// ensure we have a full source mask since there is no destination register
						sr1.sourceMask = 0xF;
						sb.AppendFormat("if (any(lessThan({0}, vec4(0)))) discard;", sr1.ToGLSL()); 
						map.Add(sr1, RegisterUsage.Vector4);
					}
					break;

				case 0x28: // tex
					SamplerReg sampler = SamplerReg.Parse(source2, programType);

					switch (sampler.d)
					{
					case 0: // 2d texture
#if PLATFORM_MONOMAC || PLATFORM_MONOTOUCH
						sr1.sourceMask = 0x3;
						sb.AppendFormat("{0} = texture2D({2}, {1}); // tex", dr.ToGLSL(), sr1.ToGLSL(), sampler.ToGLSL() ); 
						map.Add(sampler, RegisterUsage.Sampler2D);
#elif PLATFORM_MONODROID
						sr1.sourceMask = 0x3;
						sb.AppendFormat("{0} = texture2D({2}, {1}); // tex", dr.ToGLSL(), sr1.ToGLSL(), sampler.ToGLSL() ); 
						sb.AppendLine();

						int mask = dr.mask;

						dr.mask = mask & 8;
						if (dr.mask != 0)
						{
							sb.AppendFormat("if ( {0} == 1.0 ) {{ ", dr.ToGLSL() ); 
							sb.AppendLine();
							sb.AppendFormat("{0} = texture2D({2}_alpha, {1}).r; // tex \n", dr.ToGLSL(), sr1.ToGLSL(), sampler.ToGLSL() ); 
							sb.AppendLine();
							sb.AppendFormat("}}");
						}

						map.Add(sampler, RegisterUsage.Sampler2DAlpha);										
#endif
						break;
					case 1: // cube texture
						sr1.sourceMask = 0x7;
						sb.AppendFormat("{0} = textureCube({2}, {1}); // tex", dr.ToGLSL(), sr1.ToGLSL(), sampler.ToGLSL() ); 
						map.Add(sampler, RegisterUsage.SamplerCube);
						break;
					}
					//sb.AppendFormat("{0} = vec4(0,1,0,1);", dr.ToGLSL() ); 
					map.Add(dr, RegisterUsage.Vector4);
					map.Add(sr1, RegisterUsage.Vector4);

					if (outSamplers != null)
					{
						// add sampler state to output list for caller
						outSamplers[sampler.n] = sampler.ToSamplerState();
					}
					break;

				case 0x29: // sge
					sr1.sourceMask = sr2.sourceMask = 0xF; // sge only supports vec4
					sb.AppendFormat("{0} = vec4(greaterThanEqual({1}, {2})){3}; // ste", dr.ToGLSL(), sr1.ToGLSL(), sr2.ToGLSL(), dr.GetWriteMask() ); 
					map.Add(dr, RegisterUsage.Vector4);
					map.Add(sr1, RegisterUsage.Vector4);
					map.Add(sr2, RegisterUsage.Vector4);
					break;

				case 0x2A: // slt
					sr1.sourceMask = sr2.sourceMask = 0xF; // slt only supports vec4
					sb.AppendFormat("{0} = vec4(lessThan({1}, {2})){3}; // slt", dr.ToGLSL(), sr1.ToGLSL(), sr2.ToGLSL(), dr.GetWriteMask() ); 
					map.Add(dr, RegisterUsage.Vector4);
					map.Add(sr1, RegisterUsage.Vector4);
					map.Add(sr2, RegisterUsage.Vector4);
					break;
				
				case 0x2C: // seq
					sr1.sourceMask = sr2.sourceMask = 0xF; // seq only supports vec4
					sb.AppendFormat("{0} = vec4(equal({1}, {2})){3}; // seq", dr.ToGLSL(), sr1.ToGLSL(), sr2.ToGLSL(), dr.GetWriteMask() ); 
					map.Add(dr, RegisterUsage.Vector4);
					map.Add(sr1, RegisterUsage.Vector4);
					map.Add(sr2, RegisterUsage.Vector4);
					break;
				
				case 0x2D: // sne
					sr1.sourceMask = sr2.sourceMask = 0xF; // sne only supports vec4
					sb.AppendFormat("{0} = vec4(notEqual({1}, {2})){3}; // sne", dr.ToGLSL(), sr1.ToGLSL(), sr2.ToGLSL(), dr.GetWriteMask() ); 
					map.Add(dr, RegisterUsage.Vector4);
					map.Add(sr1, RegisterUsage.Vector4);
					map.Add(sr2, RegisterUsage.Vector4);
					break;

				default:
					//sb.AppendFormat ("unsupported opcode" + opcode);
					throw new NotSupportedException("Opcode " + opcode);
				}

				sb.AppendLine();
			}


#if PLATFORM_MONOMAC
			var glslVersion = 120;
#elif PLATFORM_MONOTOUCH
			var glslVersion = 100; // Actually this is glsl 1.20 but in gles it's 1.0
#elif PLATFORM_MONODROID
			var glslVersion = 100;
#endif
			// combine parts into final progam
			var glsl = new StringBuilder();
			glsl.AppendFormat("// AGAL {0} shader\n", (programType == ProgramType.Vertex) ? "vertex" : "fragment");
			glsl.AppendFormat("#version {0}\n", glslVersion);
#if PLATFORM_MONOTOUCH || PLATFORM_MONODROID
			// Required to set the default precision of vectors
			glsl.Append("precision mediump float;\n");
#endif
			glsl.Append (map.ToGLSL(false));
			if (programType == ProgramType.Vertex) {
				// this is needed for flipping render textures upside down
				glsl.AppendLine("uniform vec4 vcPositionScale;");
			}
			glsl.AppendLine("void main() {");
			glsl.Append (map.ToGLSL(true));
			glsl.Append(sb.ToString());

			if (programType == ProgramType.Vertex) {
				// this is needed for flipping render textures upside down
				glsl.AppendLine("gl_Position *= vcPositionScale;");
			}
			glsl.AppendLine("}");
			// System.Console.WriteLine(glsl);
			return glsl.ToString();;
		}
	}
}

#endif


