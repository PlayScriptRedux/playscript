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
using PlayScript.Expando;

namespace Amf
{
	// Amf3 utility functions
	public static class Amf3Utils
	{
		public static bool Verbose {get; set;}

		// this function converts a json file to an amf file
		public static void JsonFileToAmfFile(string jsonFile, string amfFile, bool generateClassDefinitions)
		{
			string jsonText = File.ReadAllText(jsonFile);
			JsonStringToAmfFile(jsonText, amfFile, generateClassDefinitions);
		}

		// this function converts json text to an amf file
		public static void JsonStringToAmfFile(string jsonText, string amfFile, bool generateClassDefinitions)
		{
			// write to memory stream and return bytes
			using (var outStream = File.Create(amfFile)) {
				JsonStringToAmfStream(jsonText, outStream, generateClassDefinitions);
			}
		}

		// this function converts json text to an amf byte array
		public static byte[] JsonStringToAmfBytes(string jsonText, bool generateClassDefinitions)
		{
			// write to memory stream and return bytes
			using (var outStream = new MemoryStream()) {
				JsonStringToAmfStream(jsonText, outStream, generateClassDefinitions);
				return outStream.ToArray();
			}
		}

		// this function parses json text, generates class definitions, and writes object graph to output stream
		public static void JsonStringToAmfStream(string jsonText, Stream outStream, bool generateClassDefinitions)
		{
			// parse json
			object jsonRoot = _root.JSON.parse(jsonText);

			// generate class definitions (optionally)
			if (generateClassDefinitions) {
				GenerateAndApplyClassDefinitions(jsonRoot);
			}

			// write to amf stream
			var amfWriter = new Amf3Writer(outStream);
			amfWriter.Write(jsonRoot);
		}

		// this function generates AMF class definitions for all dynamic (expando) objects it finds and assigns them to the ClassDefinition property of Expando
		// it does this by generating a class definition for each unique property list
		// having shared class definitions will result in a much more compact serialized form
		public static void GenerateAndApplyClassDefinitions(object root)
		{
			var expandos = new List<ExpandoObject>();
			var classDefs = new Dictionary<string, Amf3ClassDef>();
			GenerateAndApplyClassDefinitionsRecursive(root, expandos, classDefs);
			if (Verbose)  {
				Console.WriteLine("AMF3 expandos: {0} classDefs: {1}", expandos.Count, classDefs.Count);
			}
		}

		private static void GenerateAndApplyClassDefinitionsRecursive(object root, IList<ExpandoObject> expandos, Dictionary<string, Amf3ClassDef> classDefs)
		{
			if (root is ExpandoObject) {
				var expando = (ExpandoObject)root;

				// get properties from expando
				var properties = new List<String>();
				foreach (var kvp in expando) {
					properties.Add(kvp.Key);
				}

				// sort properties
				properties.Sort();

				// hash properties as one string
				var sb = new System.Text.StringBuilder();
				bool delimiter = false;
				foreach (var prop in properties) {
					if (delimiter) sb.Append(',');
					sb.Append(prop);
					delimiter = true;
				}
				var propertyHash = sb.ToString();

				Amf3ClassDef classDef;
				// see if a class definition already exists for these properties...
				if (!classDefs.TryGetValue(propertyHash, out classDef)) {
					// does not exist, so create it and add it to dictionary
					classDef = new Amf3ClassDef("*", properties.ToArray());
					classDefs.Add(propertyHash, classDef);

					if (Verbose) {
						Console.WriteLine("AMF3 generated class: {0}", propertyHash);
					}
				}

				// set class definition for expando object
				expando.ClassDefinition = classDef;

				// recurse expando properties
				foreach (var kvp in expando) {
					GenerateAndApplyClassDefinitionsRecursive(kvp.Value, expandos, classDefs);
				}
			} else if (root is System.Collections.IList) {
				var array = (System.Collections.IList)root;

				foreach (object element in array) {
					GenerateAndApplyClassDefinitionsRecursive(element, expandos, classDefs);
				}
			} 
		}

		// this function profiles the json and amf parsing code 
		// it takes in a path to a json file to load, converts json to amf, and parses amf
		public static void PerformanceTest(string path)
		{
			var newPath = PlayScript.Player.ResolveResourcePath(path);
			// read bytes from file
			var bytes = System.IO.File.ReadAllBytes(newPath);

			// perform json decoding
			var jsonTimer = System.Diagnostics.Stopwatch.StartNew();
			var jsonText = System.Text.Encoding.UTF8.GetString(bytes);
			object jsonRoot = _root.JSON.parse(jsonText);
			jsonTimer.Stop();

			// write to memory stream and return bytes
			using (var outStream = new MemoryStream()) {
				JsonStringToAmfStream(jsonText, outStream, true);

				outStream.Position = 0;

				var amfTimer = System.Diagnostics.Stopwatch.StartNew();
				var reader = new Amf3Parser(outStream);
				var obj = reader.ReadNextObject();
				amfTimer.Stop();
				Console.WriteLine("{0} JSONSize:{1} AMFSize:{2} JSONDecodeTime:{3} AMFDecodeTime:{4}", path, bytes.Length, outStream.Length, 
				                  jsonTimer.Elapsed, amfTimer.Elapsed);
			}
		}

	}
}
