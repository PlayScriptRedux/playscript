using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using Amf;

namespace Telemetry
{
	public static class Parser
	{
		public static void ParseFile(string inputPath, string outputPath)
		{
			using (var fs = File.OpenRead(inputPath)) {
				using (var tw = new StreamWriter(outputPath)) {
					Parse(fs, tw);
				}
			}
		}

		private static string Format(object o)
		{
			if (o == null) {
				return "null";
			} else if (o is string) {
				return '"' + ((string)o) + '"';
			} else if (o is Amf3Object) {
				var sb = new System.Text.StringBuilder();
				var ao = (Amf3Object)o; 
				sb.AppendFormat("[{0} ", ao.ClassDef.Name);
				foreach (var prop in ao.Properties) {
					sb.AppendFormat("{0}:{1} ", prop.Key, Format(prop.Value));
				}
				sb.AppendFormat("]");
				return sb.ToString();
			} else if (o is _root.Vector<int>){
				var a = o as _root.Vector<int>;
				return "(int)[" + a.ToString() + "]";
			} else if (o is _root.Vector<uint>){
				var a = o as _root.Vector<uint>;
				return "(uint)[" + a.ToString() + "]";
			} else if (o is _root.Vector<double>){
				var a = o as _root.Vector<double>;
				return "(number)[" + a.ToString() + "]";
			} else if (o is double){
				return "(number)" + o.ToString();
			} else {
				return o.ToString();
			}
		}

		public static void Parse(Stream stream, TextWriter output)
		{
			Amf3Parser parser = new Amf3Parser(stream);

			int time = 0;
			int enterTime = 0;

			while (stream.Position < stream.Length ) {
				var o =	parser.ReadNextObject();
				if (o == null)
					break;

				output.Write("{0:D8}: ", time);

				var amfObj = (Amf3Object)o;
				switch (amfObj.ClassDef.Name)
				{
					case ".value":
						{
							output.WriteLine("WriteValue({0}, {1});", 
							                Format(amfObj.Properties["name"]), 
							                Format(amfObj.Properties["value"])
							);
							break;
						}
					case ".span":
						{
							time += (int)amfObj.Properties["delta"];
							output.WriteLine("WriteSpan({0}, {1}, {2});", 
							                Format(amfObj.Properties["name"]), 
							                amfObj.Properties["span"],
							                amfObj.Properties["delta"]
							);

							// handle end of frame
							string name = (string)amfObj.Properties["name"];
							if (name == ".exit") {
								int span = (int)amfObj.Properties["span"];
								int deltas = time - enterTime;
								output.WriteLine("// frame deltas:{0} span:{1} diff:{2}", deltas, span, deltas - span);
							}

							break;
						}
					case ".spanValue":
						{
							time += (int)amfObj.Properties["delta"];
							output.WriteLine("WriteSpanValue({0}, {1}, {2}, {3});", 
							                Format(amfObj.Properties["name"]), 
							                amfObj.Properties["span"],
							                amfObj.Properties["delta"],
							                Format(amfObj.Properties["value"])
							);
							break;
						}
					case ".time":
						{
							time += (int)amfObj.Properties["delta"];
							output.WriteLine("WriteTime({0}, {1});", 
							                Format(amfObj.Properties["name"]), 
							                amfObj.Properties["delta"]
							);

							// handle start of frame
							string name = (string)amfObj.Properties["name"];
							if (name == ".enter") {
								enterTime = time;
							}
						}
						break;
					default:
						output.WriteLine(Format(o));
						break;
				}
			}
		}
	}
}

