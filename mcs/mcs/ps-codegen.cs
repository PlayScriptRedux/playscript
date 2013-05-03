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
using Mono.CSharp;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace Mono.PlayScript
{
	public static class CodeGenerator
	{
		public static void GenerateCode (ModuleContainer module, ParserSession session, Report report)
		{
			GenerateDynamicPartialClasses(module, session, report);
			if (report.Errors > 0)
				return;

		}

		public static void FindDynamicClasses(TypeContainer container, List<Class> classes) 
		{
			foreach (var cont in container.Containers) {
				if (cont is Class) {

					// Is class marked as dynamic?
					var cl = cont as Class;
					if (cl.IsAsDynamicClass && !(cl.BaseType != null && cl.BaseType.IsAsDynamicClass)) {
						classes.Add ((Class)cont);
					}
				}

				// Recursively find more classes
				if (cont.Containers != null)
					FindDynamicClasses(cont, classes);
			}
		}

		public static void GenerateDynamicPartialClasses(ModuleContainer module, ParserSession session, Report report)
		{
			List<Class> classes = new List<Class>();
			FindDynamicClasses(module, classes);

			if (classes.Count == 0)
				return;

			var os = new StringWriter();

			os.Write (@"
// Generated dynamic class partial classes

using System.Collections.Generic;
");

			foreach (var cl in classes) {
				os.Write (@"
namespace {1} {{

	partial class {2} : PlayScript.IDynamicClass {{

		private Dictionary<string, object> __dynamicDict;

		dynamic PlayScript.IDynamicClass.__GetDynamicValue(string name) {{
			object value = null;
			if (__dynamicDict != null) {{
				__dynamicDict.TryGetValue(name, out value);
			}}
			return value;
		}}
			
		void PlayScript.IDynamicClass.__SetDynamicValue(string name, object value) {{
			if (__dynamicDict == null) {{
				__dynamicDict = new Dictionary<string, object>();
			}}
			__dynamicDict[name] = value;
		}}
			
		bool PlayScript.IDynamicClass.__HasDynamicValue(string name) {{
			if (__dynamicDict != null) {{
				return __dynamicDict.ContainsKey(name);
			}}
			return false;
		}}

		_root.Array PlayScript.IDynamicClass.__GetDynamicNames() {{
			if (__dynamicDict != null) {{
				return new _root.Array(__dynamicDict.Keys);
			}}
			return new _root.Array();
		}}
	}}
}}

", PsConsts.PsRootNamespace, ((ITypeDefinition)cl).Namespace, cl.MemberName.Basename);
			}

			string fileStr = os.ToString();
			var path = System.IO.Path.Combine (System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(module.Compiler.Settings.OutputFile)), "dynamic.g.cs");
			System.IO.File.WriteAllText(path, fileStr);

			byte[] byteArray = Encoding.ASCII.GetBytes( fileStr );
			var input = new MemoryStream( byteArray, false );
			var reader = new SeekableStreamReader (input, System.Text.Encoding.UTF8);

			SourceFile file = new SourceFile(path, path, 0);
			file.FileType = SourceFileType.CSharp;

			Driver.Parse (reader, file, module, session, report);

		}
	}
}

