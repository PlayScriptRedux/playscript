using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using flash.utils;

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

			public string ToGLSL()
			{
				if (type == RegType.Output) {
					return programType == ProgramType.Vertex ? "gl_Position" : "gl_FragColor";
				}

				var str = PrefixFromType (type);
				str += n.ToString ();
				return str;
			}
		};

		class SourceReg
		{
			public ProgramType programType; 
			public int d;
			public int q;
			public int itype;
			public RegType type;
			public int s;
			public int o;
			public int n;

			public static SourceReg Parse (ulong v, ProgramType programType)
			{
				var sr = new SourceReg();
				sr.programType = programType;
				sr.d = (int)((v >> 63) & 1); //  Direct=0/Indirect=1 for direct Q and I are ignored, 1bit
				sr.q = (int)((v >> 48) & 0x3); // index register component select
				sr.itype = (int)((v >> 40) & 0xF); // index register type
				sr.type = (RegType)((v >> 32) & 0xF); // type
				sr.s = (int)((v >> 24) & 0xFF); // swizzle
				sr.o = (int)((v >> 16) & 0xFF);  // indirect offset
				sr.n = (int)(v & 0xFFFF);		// number
				return sr;
			}

			public string ToGLSL ()
			{
				if (type == RegType.Output) {
					return programType == ProgramType.Vertex ? "gl_Position" : "gl_FragColor";
				}

				if (d != 0 || q != 0 || itype != 0 || o != 0) {
					throw new NotImplementedException ();
				}

				if (type != RegType.Sampler && s != 228) {
					throw new NotImplementedException();
				}

				var str = PrefixFromType (type);
				str += n.ToString ();
				// str += "." + s.ToString();
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
				var str = PrefixFromType (type);
				str += n.ToString ();
				return str;
			}
		};



		enum RegisterUsage
		{
			Vector4,
			Matrix44,
			Sampler2D
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

			public void Add(SourceReg sr, RegisterUsage usage)
			{
				Add (sr.type, sr.ToGLSL(), sr.n, usage);
			}

			public void Add(SamplerReg sr, RegisterUsage usage)
			{
				Add (sr.type, sr.ToGLSL(), sr.n, usage);
			}

			public void Add(DestReg dr, RegisterUsage usage)
			{
				Add (dr.type, dr.ToGLSL(), dr.n, usage);
			}

			public void Add (RegType type, string name, int number, RegisterUsage usage)
			{
				foreach (var entry in mEntries) {

					if (entry.type == type && entry.name == name && entry.number == number) {
						if (entry.usage != usage) {
							throw new InvalidOperationException ("Cannot use register in multiple ways yet (mat44/vec4)");
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
				foreach (var entry in mEntries) {
				
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
						sb.Append("vec4 ");
						break;
					case RegisterUsage.Matrix44:
						sb.Append("mat4 ");
						break;
					case RegisterUsage.Sampler2D:
						sb.Append("sampler2D ");
						break;
					}

					sb.Append(entry.name);
					sb.AppendLine(";");
				}
				return sb.ToString();
			}


			private List<Entry> mEntries = new List<Entry>();
		};

		private static string PrefixFromType (RegType t)
		{
			switch (t) {
			case RegType.Attribute: return "va";
			case RegType.Constant:  return "vc";
			case RegType.Temporary: return "vt";
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

		public static string ConvertToGLSL (ByteArray agal)
		{
			agal.position = 0;

			int magic = agal.readByte ();
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
				sb.Append("\t");
				sb.AppendFormat("// opcode:{0:X} dest:{1:X} source1:{2:X} source2:{3:X}\n", opcode,
				                dest, source1, source2);

				// parse registers
				var dr  = DestReg.Parse(dest,programType);
				var sr1 = SourceReg.Parse(source1,programType);
				var sr2 = SourceReg.Parse(source2,programType);
				
				// switch on opcode and emit GLSL 
				sb.Append("\t");
				switch (opcode)
				{
				case 0x18: // m44
					sb.AppendFormat("{0} = {1} * {2}; // m44", dr.ToGLSL(), sr1.ToGLSL(), sr2.ToGLSL() ); 
					map.Add(dr, RegisterUsage.Vector4);
					map.Add(sr1, RegisterUsage.Vector4);
					map.Add(sr2, RegisterUsage.Matrix44);
					break;
				case 0x00: // mov
					sb.AppendFormat("{0} = {1}; // mov", dr.ToGLSL(), sr1.ToGLSL()); 
					map.Add(dr, RegisterUsage.Vector4);
					map.Add(sr1, RegisterUsage.Vector4);
					break;

				case 0x03: // mul
					sb.AppendFormat("{0} = {1} * {2}; // mul", dr.ToGLSL(), sr1.ToGLSL(), sr2.ToGLSL() ); 
					map.Add(dr, RegisterUsage.Vector4);
					map.Add(sr1, RegisterUsage.Vector4);
					map.Add(sr2, RegisterUsage.Vector4);
					break;

				case 0x28: // tex
					SamplerReg sampler = SamplerReg.Parse(source2, programType);
					sb.AppendFormat("{0} = texture2D({2}, {1}.st); // tex", dr.ToGLSL(), sr1.ToGLSL(), sampler.ToGLSL() ); 
					//sb.AppendFormat("{0} = vec4(0,1,0,1);", dr.ToGLSL() ); 
					map.Add(dr, RegisterUsage.Vector4);
					map.Add(sr1, RegisterUsage.Vector4);
					map.Add(sampler, RegisterUsage.Sampler2D);
					break;
				default:
					throw new NotSupportedException("Opcode " + opcode);
				}

				sb.AppendLine();
			}

#if PLATFORM_MONOMAC
			var glslVersion = 120;
#elif PLATFORM_MONOTOUCH
			var glslVersion = 100; // Actually this is glsl 1.20 but in gles it's 1.0
#endif

			// combine parts into final progam
			var glsl = new StringBuilder();
			glsl.AppendFormat("// AGAL {0} shader\n", (programType == ProgramType.Vertex) ? "vertex" : "fragment");
			glsl.AppendFormat("#version {0}\n", glslVersion);
#if PLATFORM_MONOTOUCH
			// Required to set the default precision of vectors
			glsl.Append("precision mediump float;\n");
#endif
			glsl.Append (map.ToGLSL(false));
			glsl.AppendLine("void main() {");
			glsl.Append (map.ToGLSL(true));
			glsl.Append(sb.ToString());
			glsl.AppendLine("}");
			return glsl.ToString();;
		}
	}
}

