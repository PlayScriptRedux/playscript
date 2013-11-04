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
using System.Diagnostics;
using PlayScript;
using PlayScript.DynamicRuntime;

namespace Amf
{
	[DebuggerDisplay ("Count = {Count}")]
	[DebuggerTypeProxy (typeof (Amf3ObjectDebugView))]
	public class Amf3Object : IAmf3Writable,
		IKeyEnumerable, IEnumerable<Variant>
    {
		// class definition
		public readonly Amf3ClassDef 					ClassDef;
		// property values (one for each Amf3ClassDef Properties)
		public readonly Variant[]						Values;
		// dynamic property values (if this class is dynamic)
		public readonly IDictionary<string, Variant>	DynamicProperties;

		public int 		Count 	
		{
			get {return Values.Length;}
		}

		public bool hasOwnProperty(string key)
		{
			if (DynamicProperties != null) {
				if (DynamicProperties.ContainsKey(key)) {
					return true;
				}
			}

			int index = ClassDef.GetPropertyIndex(key);
			return (index >= 0);
		}


		private Variant GetPropertyValue(string key)
		{
			if (DynamicProperties != null) {
				Variant dynamicValue;
				if (DynamicProperties.TryGetValue(key, out dynamicValue)) {
					// return value from dynamic properties if we have them
					return dynamicValue;
				}
			}

			int index = ClassDef.GetPropertyIndex(key);
			if (index >= 0) {
				// return value from class property
				return Values[index];
			} else {
				return Variant.Undefined;
			}
		}

		private void SetPropertyValue(string key, Variant value)
		{
			// get index of property from class definition
			int index = ClassDef.GetPropertyIndex(key);
			if (index >= 0) {
				// set class property
				Values[index] = value;
				return;
			} 

			if (DynamicProperties != null) {
				// set dynamic property
				DynamicProperties[key] = value;
			}
		}


        public Variant this[string key]
        {
            get
			{
				return GetPropertyValue(key);
            }
			set
			{
				SetPropertyValue(key, value);
			}
        }

		public Amf3Object(Amf3ClassDef classDef)
        {
            if (classDef == null)
                throw new ArgumentNullException("classDef");

			// set class definition
			ClassDef   = classDef;

			// allocate property values
			Values = new Variant[classDef.Properties.Length];

			if (classDef.Dynamic) {
				// create dynamic value store
				DynamicProperties = new Dictionary<string, Variant>();
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
			for (int i=0; i < Values.Length; i++) {
				writer.Write(Values[i]);
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
				for (int i=0; i < amfObj.Values.Length; i++)
				{
					reader.Read(ref amfObj.Values[i]);
				}
			}
			#endregion
		};


		#region IEnumerable implementation
		public IEnumerator<Variant> GetEnumerator()
		{
			return ((IEnumerable<Variant>)Values).GetEnumerator();
		}
		#endregion

		#region IEnumerable implementation
		IEnumerator IEnumerable.GetEnumerator()
		{
			// we must box all objects
			foreach (var value in Values) {
				yield return value.AsObject();
			}
		}
		#endregion

		#region IKeyEnumerable implementation
		IEnumerator IKeyEnumerable.GetKeyEnumerator()
		{
			return ClassDef.Properties.GetEnumerator();
		}
		#endregion

		// debugger support
		[DebuggerDisplay("{value}", Name = "{key}", Type = "{ValueTypeName}")]
		internal class KeyValuePairDebugView
		{
			public string key   {get { return _key; }}
			public object value 
			{
				get { return _expando[_key].AsObject();}
				set { _expando[_key] = Variant.FromAnyType(value);}
			}

			public KeyValuePairDebugView(Amf3Object expando, string key)
			{
				_expando = expando;
				_key = key;
			}

			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public string ValueTypeName
			{
				get {
					var v = value;
					if (v != null) {
						return v.GetType().Name;
					} else {
						return "";
					}
				}
			}

			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			private readonly Amf3Object _expando;
			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			private readonly string        _key;
		}

		internal class Amf3ObjectDebugView
		{
			private Amf3Object expando;

			public Amf3ObjectDebugView(Amf3Object expando)
			{
				this.expando = expando;
			}

			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			public KeyValuePairDebugView[] Keys
			{
				get
				{
					var keys = new KeyValuePairDebugView[expando.Values.Length];

					int i = 0;
					foreach(string key in expando.ClassDef.Properties)
					{
						keys[i] = new KeyValuePairDebugView(expando, key);
						i++;
					}
					return keys;
				}
			}
		}


    }
}
