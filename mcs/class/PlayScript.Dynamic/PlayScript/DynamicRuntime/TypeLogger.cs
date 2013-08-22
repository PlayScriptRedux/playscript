//
// PSGetIndex.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Diagnostics;

using PlayScript;

namespace PlayScript.DynamicRuntime
{
	public static class TypeLogger
	{

		public static Dictionary<Assembly,HashSet<Type>> TypesUsed = new Dictionary<Assembly,HashSet<Type>>();
		public static int log_count = 0;

		[Conditional("LOG_TYPES")]
		public static void LogType(object o) 
		{
			if (o == null)
				return;

			Type type = o as Type;
			if (type == null) {
				type = o.GetType ();
			}

			if (Type.GetTypeCode (type) != TypeCode.Object) 
				return;

			if (type.IsGenericType && type.GetGenericTypeDefinition () != null)
				type = type.GetGenericTypeDefinition ();

			Assembly assembly = type.Assembly;

			HashSet<Type> hashset = null;

			if (!TypesUsed.TryGetValue (assembly, out hashset)) {
				hashset = new HashSet<Type> ();
				TypesUsed [assembly] = hashset;
			}

			if (hashset.Add (type)) {

				// Log inner types..
				var asmTypes = assembly.GetTypes ();
				foreach (var asmType in asmTypes) {
					string fullName = type.FullName;
					if (asmType != type && asmType.FullName.StartsWith (fullName)) {
						LogType (asmType);
					}
				}

				if (type.BaseType != typeof(object))
					LogType (type.BaseType);
			}

			if (log_count == 20000) {
				DumpLinkerXML ();
				log_count = 0;
			}
			log_count++;
		}

		private static string GetTypeName(Type type)
		{
			return type.FullName.Replace ("<", "&lt;").Replace (">", "&gt;");
		}

		public static void DumpLinkerXML()
		{
			StringBuilder sb = new StringBuilder ();

			sb.Append("<linker>");
			foreach (KeyValuePair<Assembly, HashSet<Type>> pair in TypesUsed) {
				string assemName = pair.Key.FullName;
				int comma = assemName.IndexOf (",");
				assemName = assemName.Substring (0, comma);
				sb.Append("<assembly fullname=\"" + assemName + "\">");
				foreach (var type in pair.Value) {
					sb.Append("<type fullname=\"" + GetTypeName(type) + "\" preserve=\"all\" />");
				}
				sb.Append("</assembly>");
			}
			sb.Append("</linker>");
			string s = sb.ToString ();
			System.Diagnostics.Debug.WriteLine (s);
		}


	}

}

