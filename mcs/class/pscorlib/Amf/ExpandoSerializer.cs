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
using System.Linq;
using PlayScript.Expando;
using _root;

namespace Amf
{
	// serializer for playscript expando objects
	public class ExpandoSerializer : IAmf3Serializer
	{
		#region IAmfSerializer implementation

		public object NewInstance(Amf3ClassDef classDef)
		{
			var expando = new ExpandoObject(classDef.Properties.Length);
			// assign class definition to expando object
			expando.ClassDefinition = classDef;
			return expando;
		}

		public IList NewVector(uint num, bool isFixed)
		{
			return new Vector<dynamic>(num, isFixed);
		}

		public void   WriteObject(Amf3Writer writer, object obj)
		{
			var expando = (ExpandoObject)obj;
			writer.Write(expando);
		}

		public void ReadObject(Amf3Reader reader, object obj)
		{
			var expando = (ExpandoObject)obj;

			reader.ReadObjectHeader();

			// read class properties
			while (!reader.Done){
				string name = reader.Name;
				object value = reader.ReadAsObject();
				expando[name] = value;
			}
		}
		#endregion
	};
}
