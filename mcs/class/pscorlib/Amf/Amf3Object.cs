// Amf3Object.cs
//
// Copyright (c) 2009 Chris Howie
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Amf
{
	public class Amf3Object : IAmf3Writable
    {
		// class definition
		public readonly Amf3ClassDef 					 ClassDef;
		// property values (one for each Amf3ClassDef Properties)
		public readonly Amf3Variant[]					 Properties;
		// dynamic property values (if this class is dynamic)
		public readonly IDictionary<string, Amf3Variant> DynamicProperties;

        public Amf3Variant this[string key]
        {
            get
            {
				// return undefined by default
				var r = new Amf3Variant();

				if (DynamicProperties != null) {
					if (DynamicProperties.TryGetValue(key, out r)) {
						// return value from dynamic properties if we have them
						return r;
					}
				}

				int index = ClassDef.GetPropertyIndex(key);
				if (index >= 0) {
					// return value from normal property
					return Properties[index];
				} 

				// return value
				return r;
            }
			set
			{
				int index = ClassDef.GetPropertyIndex(key);
				if (index >= 0) {
					// set class definition property
					Properties[index] = value;
					return;
				} 

				if (DynamicProperties != null) {
					// set dynamic property
					DynamicProperties[key] = value;
				}
			}
        }

		public Amf3Object(Amf3ClassDef classDef)
        {
            if (classDef == null)
                throw new ArgumentNullException("classDef");

			// set class definition
			ClassDef   = classDef;

			// allocate property store
			Properties = new Amf3Variant[classDef.Properties.Length];

			if (classDef.Dynamic) {
				// create dynamic value store
				DynamicProperties = new Dictionary<string, Amf3Variant>();
			}
        }

		[Flags]
		internal enum Flags : int
		{
			Inline = 1,
			InlineClassDef = 2,
			Externalizable = 4,
			Dynamic = 8
		}

		#region IAmf3Serializable implementation
		public void Serialize(Amf3Writer writer) {
			writer.WriteObjectHeader(ClassDef, this);

			// write class properties
			for (int i=0; i < Properties.Length; i++) {
				writer.Write(Properties[i]);
			}

			if (ClassDef.Dynamic) {
				// write dynamic properties
				// TODO: this is a little weird and shouldnt be here.. should be handled by the writer
				foreach (var kvp in DynamicProperties) {
					writer.TypelessWrite(kvp.Key);
					writer.Write(kvp.Value);
				}

				// write terminator
				writer.TypelessWrite("");
			}
		}
		#endregion

		// serializer for playscript expando objects
		public class Serializer : IAmf3Serializer
		{
			#region IAmfSerializer implementation

			public object NewInstance(Amf3ClassDef classDef)
			{
				return new Amf3Object(classDef);
			}

			public IList NewVector(uint num, bool isFixed)
			{
				return new _root.Vector<Amf3Object>(num, isFixed);
			}

			public void   WriteObject(Amf3Writer writer, object obj)
			{
				var amfObj = (Amf3Object)obj;
				amfObj.Serialize(writer);
			}

			public void ReadObject(Amf3Reader reader, object obj)
			{
				var amfObj = (Amf3Object)obj;
				reader.ReadObjectHeader(amfObj.ClassDef);

				// read class properties
				for (int i=0; i < amfObj.Properties.Length; i++)
				{
					reader.Read(ref amfObj.Properties[i]);
				}
			}
			#endregion
		};

    }
}
