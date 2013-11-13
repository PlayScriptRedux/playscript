using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using Amf;
using PlayScript;

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

		private static string Format(Variant value)
		{
			object o = value.AsObject();
			if (o == null) {
				return "null";
			} else if (o is string) {
				return '"' + ((string)o) + '"';
			} else if (o is Amf3Object) {
				var sb = new System.Text.StringBuilder();
				var ao = (Amf3Object)o; 
				sb.AppendFormat("[{0} ", ao.ClassDef.Name);
				for (int i=0; i < ao.ClassDef.Properties.Length; i++) {
					sb.AppendFormat("{0}:{1} ", ao.ClassDef.Properties[i], Format(ao.Values[i]));
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
			parser.OverrideSerializer = new Amf3Object.Serializer();

			int time = 0;
			int enterTime = 0;

			while (stream.Position < stream.Length ) {
				Variant v = new Variant();
				parser.ReadNextObject(ref v);
				if (!v.IsDefined)
					break;

				var amfObj = v.AsObject() as Amf3Object;
				if (amfObj == null)
					break;

				output.Write("{0:D8}: ", time);

				switch (amfObj.ClassDef.Name)
				{
					case ".value":
						{
							output.WriteLine("WriteValue({0}, {1});", 
							                Format(amfObj["name"]), 
							                Format(amfObj["value"])
							);
							break;
						}
					case ".span":
						{
							time += amfObj["delta"].AsInt();
							output.WriteLine("WriteSpan({0}, {1}, {2});", 
							                Format(amfObj["name"]), 
							                amfObj["span"],
							                amfObj["delta"]
							);

							// handle end of frame
							string name = amfObj["name"].AsString();
							if (name == ".exit") {
								int span = amfObj["span"].AsInt();
								int deltas = time - enterTime;
								output.WriteLine("// frame deltas:{0} span:{1} diff:{2}", deltas, span, deltas - span);
							}

							break;
						}
					case ".spanValue":
						{
							time += amfObj["delta"].AsInt();
							output.WriteLine("WriteSpanValue({0}, {1}, {2}, {3});", 
							                Format(amfObj["name"]), 
							                amfObj["span"],
							                amfObj["delta"],
							                Format(amfObj["value"])
							);
							break;
						}
					case ".time":
						{
							time += amfObj["delta"].AsInt();
							output.WriteLine("WriteTime({0}, {1});", 
							                Format(amfObj["name"]), 
							                amfObj["delta"]
							);

							// handle start of frame
							string name = amfObj["name"].AsString();
							if (name == ".enter") {
								enterTime = time;
							}
						}
						break;
					default:
						output.WriteLine(Format(v));
						break;
				}
			}
		}
	}
}

