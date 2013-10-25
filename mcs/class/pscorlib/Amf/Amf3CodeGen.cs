//
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

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Reflection;

namespace Amf
{
	// this class performs code generation of AMF serializers in C#
	// this can be used by the runtime or the compiler to attach methods to classes that need serialization
	public static class Amf3CodeGen
    {
		// code generation mode
		public enum Mode
		{
			Skip,				// do not generate serialization code
			PartialClass,		// create a partial class that adds IAmfSerializable instance methods
			ExternalClass		// create an external static class that does serialization vias static methods
		};

		public static void EmitAllSerializerCode(string path, Func<System.Type, Mode> modeSelector)
		{
			using (var fs = File.CreateText(path))
			{
				EmitAllSerializerCode(fs, modeSelector);
			}
		}

		public static void EmitAllSerializerCode(TextWriter tw, Func<System.Type, Mode> modeSelector)
		{
			var classInfos = Amf3ClassDef.GetAllRegisteredClasses();
			foreach (var info in classInfos)
			{
				if (info.Type != null) {
					// apply filter
					var mode = modeSelector(info.Type);
					if (mode != Mode.Skip) {
						EmitSerializerCode(tw, mode, info.Alias, info.Type);
					}
				}
			}
		}

		// this emits serialization code to an AMF stream by using reflection to get the fields of a concrete type
		public static void EmitSerializerCode(TextWriter tw, Mode mode, string classAlias, Type type)
		{
			if (mode == Mode.Skip) return;

			// get all instance fields (public or private)
			var fields = new List<string>();
			foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
			{
				fields.Add(field.Name);
			}

			// we dont do properties for now
			EmitSerializerCode(tw, mode, classAlias, type.Namespace, type.Name, fields.ToArray());
		}

		// this emits a either a C# partial class that adds AMF serialization methods or a static class that does serialization via static methods
		public static void EmitSerializerCode(TextWriter tw, Mode mode, string classAlias, string namespaceName, string className, string[] fields)
		{
			if (mode == Mode.Skip) return;

			string fieldPrefix = mode == (Mode.ExternalClass) ? "obj." : "";

			tw.WriteLine("// this serialization code was automatically generated from Amf.Amf3CodeGen");
			tw.WriteLine("namespace {0}", namespaceName);
			tw.Write("{");
			Indent(tw); 

			tw.WriteLine("using Amf;");

			var propertyTypes = new Dictionary<string, string>();

			if (mode == Mode.PartialClass) {
				tw.WriteLine("[Amf3Serializable({0})]", Quote(classAlias));
				tw.WriteLine("public partial class {0} : IAmf3Serializable", className);
			} else {
				tw.WriteLine("[Amf3ExternalSerializer({0}, typeof({1}))]", Quote(classAlias), className);
				tw.WriteLine("public static class AmfSerializer_{0}", className);
			}
			tw.Write("{");
			Indent(tw); 

			if (mode == Mode.PartialClass) 
				tw.WriteLine("#region IAmf3Serializable implementation");

				// generate serialization writer
			if (mode == Mode.PartialClass) {
				tw.Write("public void Serialize(Amf3Writer writer) {");
			} else {
				tw.Write("public static void ObjectSerializer(object o, Amf3Writer writer) {");
			}

			Indent(tw);
			if (mode == Mode.ExternalClass) 
				tw.WriteLine("var obj = ({0})o;", className);
			tw.Write("writer.WriteObjectHeader(ClassDef);");
			foreach (var field in fields) {
				tw.WriteLine();
				tw.Write("writer.Write({0});", fieldPrefix + field);
			}
			UnIndent(tw);
			tw.WriteLine("}");
			tw.WriteLine();

			// generate serialization reader
			if (mode == Mode.PartialClass) {
				tw.Write("public void Serialize(Amf3Reader reader) {");
			} else { 
				tw.Write("public static void ObjectDeserializer(object o, Amf3Reader reader) {");
			}
			Indent(tw);
			if (mode == Mode.ExternalClass) 
				tw.WriteLine("var obj = ({0})o;", className);
			tw.Write("reader.ReadObjectHeader(ClassDef);");
			foreach (var field in fields) {
				tw.WriteLine();
				tw.Write("reader.Read(out {0});", fieldPrefix + field);
			}
			UnIndent(tw);
			tw.WriteLine("}");

			// end region
			if (mode == Mode.PartialClass) 
				tw.WriteLine("#endregion");
			tw.WriteLine();


			// generate class constructor
			tw.Write("public static object ObjectConstructor() {");
			Indent(tw);
			tw.Write("return new {0}();", className);
			UnIndent(tw);
			tw.WriteLine("}");
			tw.WriteLine();

			// generate class vector constructor
			tw.Write("public static System.Collections.IList VectorObjectConstructor(uint len, bool isFixed) {");
			Indent(tw);
			tw.Write("return new _root.Vector<{0}>(len, isFixed);", className);
			UnIndent(tw);
			tw.WriteLine("}");
			tw.WriteLine();


			// write class definition
			string names = "{";
			bool delimiter = false;
			foreach (var field in fields) {
				if (delimiter) names += ", ";
				names += Quote(field);
				delimiter = true;
			}
			names += "}";
			tw.Write("public static Amf3ClassDef ClassDef = new Amf3ClassDef({0}, new string[] {1} );",
			         Quote(classAlias),
			         names);

			// end class
			UnIndent(tw);
			tw.WriteLine("}");

			// end namespace
			UnIndent(tw);
			tw.WriteLine("}");
		}

		// quotes a string and returns it
		private static string Quote(string str)
		{
			return "\"" + str + "\"";
		}

		// this is hacky way to do indenting but it works
		private static void Indent(TextWriter tw)
		{
			// add indent
			tw.NewLine += "    ";
			// newline and indent next line
			tw.WriteLine();
		}

		// this is hacky way to do indenting but it works
		private static void UnIndent(TextWriter tw)
		{
			var newline = tw.NewLine;
			// remove indent
			tw.NewLine = newline.Substring(0, newline.Length - 4);
			// newline and indent next line
			tw.WriteLine();
		}

    }
}
