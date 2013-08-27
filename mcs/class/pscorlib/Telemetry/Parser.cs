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
			} else {
				return o.ToString();
			}
		}

		public static void Parse(Stream stream, TextWriter output)
		{
			Amf3Parser parser = new Amf3Parser(stream);

			while (stream.Position < stream.Length ) {
				var o =	parser.ReadNextObject();
				if (o == null)
					break;

				var amfObj = (Amf3Object)o;
				switch (amfObj.ClassDef.Name)
				{
					case ".value":
						output.WriteLine("WriteValue({0}, {1});", 
						             Format(amfObj.Properties["name"]), 
						             Format(amfObj.Properties["value"])
						             );
						break;
					case ".span":
						output.WriteLine("WriteSpan({0}, {1}, {2});", 
						             Format(amfObj.Properties["name"]), 
						             amfObj.Properties["span"],
						             amfObj.Properties["delta"]
						             );
						break;
					case ".spanValue":
						output.WriteLine("WriteSpanValue({0}, {1}, {2}, {3});", 
						             Format(amfObj.Properties["name"]), 
						             amfObj.Properties["span"],
						             amfObj.Properties["delta"],
						             Format(amfObj.Properties["value"])
						             );
						break;
					case ".time":
						output.WriteLine("WriteTime({0}, {1});", 
						             Format(amfObj.Properties["name"]), 
						             amfObj.Properties["delta"]
						             );
						break;
					default:
						output.WriteLine(Format(o));
						break;
				}
			}
		}
	}
}

