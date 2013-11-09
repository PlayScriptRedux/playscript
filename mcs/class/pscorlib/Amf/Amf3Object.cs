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
using System.Linq;
using PlayScript;
using PlayScript.DynamicRuntime;

namespace Amf
{
	[DebuggerDisplay ("Count = {Count}")]
	[DebuggerTypeProxy (typeof (Amf3ObjectDebugView))]
	public class Amf3Object : IAmf3Writable,
		IDynamicAccessor<Variant>, 
		IEnumerable< KeyValuePair<string, Variant> >,
		IDynamicClass,	
		IDynamicObject, IDynamicAccessorTyped
    {

		// class definition
		public readonly Amf3ClassDef 					ClassDef;
		// property values (one for each Amf3ClassDef Properties)
		public readonly Variant[]						Values;
		// dynamic property values (if this class is dynamic)
		public IDictionary<string, Variant>				DynamicProperties;

		// returns the number of properties in this object (class & dynamic)
		public int 		Count 	
		{
			get 
			{
				if (DynamicProperties != null) {
					return Values.Length + DynamicProperties.Count;
				} else {
					return Values.Length;
				}
			}
		}

		public bool ContainsKey(string key)
		{
			// lookup index of value from class definition
			int index = ClassDef.GetPropertyIndex(key);
			if (index >= 0) {
				// value exists in class
				return true;
			} 

			// lookup value from dynamic properties 
			if (DynamicProperties != null) {
				return DynamicProperties.ContainsKey(key);
			}

			return false;
		}

		public bool Remove(string key)
		{
			if (DynamicProperties != null) {
				// remove dynamic properties
				if (DynamicProperties.Remove(key)) {
					return true;
				}
			}
			return false;
		}

		private string ConvertKey(string key)
		{
			if (key == null)
				return "null";
			return key;
		}

		private string ConvertKey(int key)
		{
			return key.ToString();
		}

		private string ConvertKey(object key)
		{
			if (key == null)
				return "null";
			if (Object.ReferenceEquals(key, PlayScript.Undefined._undefined))
				return "undefined";
			return key.ToString();
		}

		public Variant GetPropertyValue(object key)
		{
			return GetPropertyValue(ConvertKey(key));
		}

		public Variant GetPropertyValue(int key)
		{
			return GetPropertyValue(ConvertKey(key));
		}

		public Variant GetPropertyValue(string key)
		{
			// lookup index of value from class definition
			int index = ClassDef.GetPropertyIndex(key);
			// does value exist in the class?
			if (index >= 0) {
				// return value from class property
				return Values[index];
			} 

			// lookup value from dynamic properties 
			if (DynamicProperties != null) {
				Variant dynamicValue;
				if (DynamicProperties.TryGetValue(key, out dynamicValue)) {
					// return value from dynamic properties if we have them
					return dynamicValue;
				}
			}

			// not found, return undefined
			return Variant.Undefined;
		}

		public Variant GetPropertyValue(string key, Variant defaultValue)
		{
			// lookup index of value from class definition
			int index = ClassDef.GetPropertyIndex(key);
			// does value exist in the class?
			if (index >= 0) {
				// return value from class property
				return Values[index];
			} 

			// lookup value from dynamic properties 
			if (DynamicProperties != null) {
				Variant dynamicValue;
				if (DynamicProperties.TryGetValue(key, out dynamicValue)) {
					// return value from dynamic properties if we have them
					return dynamicValue;
				}
			}

			// not found, return undefined
			return defaultValue;
		}

		public object GetPropertyValueAsObject(string key)
		{
			// lookup index of value from class definition
			int index = ClassDef.GetPropertyIndex(key);
			// does value exist in the class?
			if (index >= 0) {
				// return value from class property
				// we do this AsObject here so that the boxed value will be cached
				return Values[index].AsObject();
			} 

			// lookup value from dynamic properties 
			if (DynamicProperties != null) {
				Variant dynamicValue;
				if (DynamicProperties.TryGetValue(key, out dynamicValue)) {
					// return value from dynamic properties if we have them
					return dynamicValue.AsObject();
				}
			}

			// not found, return null
			return null;
		}

		public object GetPropertyValueAsUntyped(string key)
		{
			// lookup index of value from class definition
			int index = ClassDef.GetPropertyIndex(key);
			// does value exist in the class?
			if (index >= 0) {
				// return value from class property
				// we do this AsObject here so that the boxed value will be cached
				return Values[index].AsUntyped();
			} 

			// lookup value from dynamic properties 
			if (DynamicProperties != null) {
				Variant dynamicValue;
				if (DynamicProperties.TryGetValue(key, out dynamicValue)) {
					// return value from dynamic properties if we have them
					return dynamicValue.AsUntyped();
				}
			}

			// not found, return undefined
			return PlayScript.Undefined._undefined;
		}


		public void SetPropertyValue(object key, Variant value)
		{
			SetPropertyValue(ConvertKey(key), value);
		}

		public void SetPropertyValue(int key, Variant value)
		{
			SetPropertyValue(ConvertKey(key), value);
		}

		public void SetPropertyValue(string key, Variant value)
		{
			// lookup index of value from class definition
			int index = ClassDef.GetPropertyIndex(key);
			// does value exist in the class?
			if (index >= 0) {
				// set class property
				Values[index] = value;
				return;
			} 

			if (DynamicProperties == null) {
				// automatically create dynamic properties 
				DynamicProperties = new Dictionary<string, Variant>();
			}

			// set dynamic property
			DynamicProperties[key] = value;
		}

		public void SetPropertyValueAsObject(string key, object value)
		{
			SetPropertyValue(key, Variant.FromAnyType(value));
		}

		// untyped means that the value could be undefined
		public void SetPropertyValueAsUntyped(string key, object value)
		{
			SetPropertyValue(key, Variant.FromAnyType(value));
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

		#region IKeyEnumerable implementation
		IEnumerator IKeyEnumerable.GetKeyEnumerator()
		{
			if (DynamicProperties != null) {
				return ClassDef.GetKeyEnumerator(DynamicProperties.Keys);
			} else {
				return ClassDef.GetKeyEnumerator();
			}
		}
		#endregion

		#region IEnumerable implementation
		IEnumerator IEnumerable.GetEnumerator()
		{
			if (DynamicProperties != null) {
				// return all dynamic values
				foreach (var value in DynamicProperties.Values) {
					yield return value;
				}
			} 
			// we must box all objects unfortunately
			for (int i=0; i < Values.Length; i++) {
				yield return Values[i].AsObject();
			}
		}

		// key value pair enumerator
		IEnumerator<KeyValuePair<string, Variant>> IEnumerable<KeyValuePair<string, Variant>>.GetEnumerator()
		{
			// return dynamic kvps
			if (DynamicProperties != null)  {
				foreach (var kvp in this.DynamicProperties) {
					yield return kvp;
				}
			}

			// return class kvps
			for (int i=0; i < Values.Length; i++) {
				yield return new KeyValuePair<string, Variant>(this.ClassDef.Properties[i], this.Values[i]);
			}
		}

		// key value pair enumerator
		IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
		{
			// return dynamic kvps
			if (DynamicProperties != null)  {
				foreach (var kvp in this.DynamicProperties) {
					yield return new KeyValuePair<string, object>(kvp.Key, kvp.Value.AsObject());
				}
			}

			// return class kvps
			for (int i=0; i < Values.Length; i++) {
				yield return new KeyValuePair<string, object>(this.ClassDef.Properties[i], this.Values[i].AsObject());
			}
		}

		#endregion

		#region IDynamicAccessorUntyped implementation

		object IDynamicAccessorUntyped.GetMember(string name, ref uint hint, object defaultValue)
		{
			return GetPropertyValueAsUntyped(name) ?? defaultValue;
		}

		void IDynamicAccessorUntyped.SetMember(string name, ref uint hint, object value)
		{
			SetPropertyValueAsUntyped(name, value);
		}

		object IDynamicAccessorUntyped.GetIndex(string key)
		{
			return GetPropertyValueAsUntyped(key);
		}

		void IDynamicAccessorUntyped.SetIndex(string key, object value)
		{
			SetPropertyValueAsUntyped(key, value);
		}

		object IDynamicAccessorUntyped.GetIndex(int key)
		{
			return GetPropertyValueAsUntyped(ConvertKey(key));
		}

		void IDynamicAccessorUntyped.SetIndex(int key, object value)
		{
			SetPropertyValueAsUntyped(ConvertKey(key), value);
		}

		object IDynamicAccessorUntyped.GetIndex(object key)
		{
			return GetPropertyValueAsUntyped(ConvertKey(key));
		}

		void IDynamicAccessorUntyped.SetIndex(object key, object value)
		{
			SetPropertyValueAsUntyped(ConvertKey(key), value);
		}

		bool IDynamicAccessorUntyped.HasMember(string name)
		{
			return this.ContainsKey(name);
		}

		bool IDynamicAccessorUntyped.HasMember(string name, ref uint hint)
		{
			return this.ContainsKey(name);
		}

		bool IDynamicAccessorUntyped.DeleteMember(string name)
		{
			return this.Remove(name);
		}

		bool IDynamicAccessorUntyped.HasIndex(int key)
		{
			return this.ContainsKey(ConvertKey(key));
		}

		bool IDynamicAccessorUntyped.DeleteIndex(int key)
		{
			return this.Remove(ConvertKey(key));
		}

		bool IDynamicAccessorUntyped.HasIndex(object key)
		{
			return this.ContainsKey(ConvertKey(key));
		}

		bool IDynamicAccessorUntyped.DeleteIndex(object key)
		{
			return this.Remove(ConvertKey(key));
		}

		#endregion

		#region IDynamicAccessor implementation

		object IDynamicAccessor<object>.GetMember(string name, ref uint hint, object defaultValue)
		{
			return GetPropertyValueAsObject(name) ?? defaultValue;
		}

		void IDynamicAccessor<object>.SetMember(string name, ref uint hint, object value)
		{
			SetPropertyValueAsObject(name, value);
		}

		object IDynamicAccessor<object>.GetIndex(string key)
		{
			return GetPropertyValueAsObject(key);
		}

		void IDynamicAccessor<object>.SetIndex(string key, object value)
		{
			SetPropertyValueAsObject(key, value);
		}

		object IDynamicAccessor<object>.GetIndex(int key)
		{
			return GetPropertyValueAsObject(ConvertKey(key));
		}

		void IDynamicAccessor<object>.SetIndex(int key, object value)
		{
			SetPropertyValueAsObject(ConvertKey(key), value);
		}

		object IDynamicAccessor<object>.GetIndex(object key)
		{
			return GetPropertyValueAsObject(ConvertKey(key));
		}
		void IDynamicAccessor<object>.SetIndex(object key, object value)
		{
			SetPropertyValueAsObject(ConvertKey(key), value);
		}

		#endregion

		#region IDynamicAccessor implementation

		string IDynamicAccessor<string>.GetMember(string name, ref uint hint, string defaultValue)
		{
			return GetPropertyValue(name).AsString(defaultValue);
		}

		void IDynamicAccessor<string>.SetMember(string name, ref uint hint, string value)
		{
			SetPropertyValue(name, value);
		}

		string IDynamicAccessor<string>.GetIndex(string key)
		{
			return GetPropertyValue(key).AsString();
		}

		void IDynamicAccessor<string>.SetIndex(string key, string value)
		{
			SetPropertyValue(key, value);
		}

		string IDynamicAccessor<string>.GetIndex(int key)
		{
			return GetPropertyValue(key).AsString();
		}

		void IDynamicAccessor<string>.SetIndex(int key, string value)
		{
			SetPropertyValue(key, value);
		}

		string IDynamicAccessor<string>.GetIndex(object key)
		{
			return GetPropertyValue(key).AsString();
		}

		void IDynamicAccessor<string>.SetIndex(object key, string value)
		{
			SetPropertyValue(key, value);
		}

		#endregion

		#region IDynamicAccessor implementation

		double IDynamicAccessor<double>.GetMember(string name, ref uint hint, double defaultValue)
		{
			return GetPropertyValue(name).AsNumber(defaultValue);
		}

		void IDynamicAccessor<double>.SetMember(string name, ref uint hint, double value)
		{
			SetPropertyValue(name, value);
		}

		double IDynamicAccessor<double>.GetIndex(string key)
		{
			return GetPropertyValue(key).AsNumber();
		}

		void IDynamicAccessor<double>.SetIndex(string key, double value)
		{
			SetPropertyValue(key, value);
		}

		double IDynamicAccessor<double>.GetIndex(int key)
		{
			return GetPropertyValue(key).AsNumber();
		}

		void IDynamicAccessor<double>.SetIndex(int key, double value)
		{
			SetPropertyValue(key, value);
		}

		double IDynamicAccessor<double>.GetIndex(object key)
		{
			return GetPropertyValue(key).AsNumber();
		}

		void IDynamicAccessor<double>.SetIndex(object key, double value)
		{
			SetPropertyValue(key, value);
		}

		#endregion

		#region IDynamicAccessor implementation

		float IDynamicAccessor<float>.GetMember(string name, ref uint hint, float defaultValue)
		{
			return GetPropertyValue(name).AsFloat(defaultValue);
		}

		void IDynamicAccessor<float>.SetMember(string name, ref uint hint, float value)
		{
			SetPropertyValue(name, value);
		}

		float IDynamicAccessor<float>.GetIndex(string key)
		{
			return GetPropertyValue(key).AsFloat();
		}

		void IDynamicAccessor<float>.SetIndex(string key, float value)
		{
			SetPropertyValue(key, value);
		}

		float IDynamicAccessor<float>.GetIndex(int key)
		{
			return GetPropertyValue(key).AsFloat();
		}

		void IDynamicAccessor<float>.SetIndex(int key, float value)
		{
			SetPropertyValue(key, value);
		}
		float IDynamicAccessor<float>.GetIndex(object key)
		{
			return GetPropertyValue(key).AsFloat();
		}

		void IDynamicAccessor<float>.SetIndex(object key, float value)
		{
			SetPropertyValue(key, value);
		}

		#endregion

		#region IDynamicAccessor implementation

		uint IDynamicAccessor<uint>.GetMember(string name, ref uint hint, uint defaultValue)
		{
			return GetPropertyValue(name).AsUInt(defaultValue);
		}

		void IDynamicAccessor<uint>.SetMember(string name, ref uint hint, uint value)
		{
			SetPropertyValue(name, value);
		}

		uint IDynamicAccessor<uint>.GetIndex(string key)
		{
			return GetPropertyValue(key).AsUInt();
		}

		void IDynamicAccessor<uint>.SetIndex(string key, uint value)
		{
			SetPropertyValue(key, value);
		}

		uint IDynamicAccessor<uint>.GetIndex(int key)
		{
			return GetPropertyValue(key).AsUInt();
		}

		void IDynamicAccessor<uint>.SetIndex(int key, uint value)
		{
			SetPropertyValue(key, value);
		}

		uint IDynamicAccessor<uint>.GetIndex(object key)
		{
			return GetPropertyValue(key).AsUInt();
		}

		void IDynamicAccessor<uint>.SetIndex(object key, uint value)
		{
			SetPropertyValue(key, value);
		}

		#endregion

		#region IDynamicAccessor implementation

		int IDynamicAccessor<int>.GetMember(string name, ref uint hint, int defaultValue)
		{
			return GetPropertyValue(name).AsInt();
		}

		void IDynamicAccessor<int>.SetMember(string name, ref uint hint, int value)
		{
			SetPropertyValue(name, value);
		}

		int IDynamicAccessor<int>.GetIndex(string key)
		{
			return GetPropertyValue(key).AsInt();
		}

		void IDynamicAccessor<int>.SetIndex(string key, int value)
		{
			SetPropertyValue(key, value);
		}

		int IDynamicAccessor<int>.GetIndex(int key)
		{
			return GetPropertyValue(key).AsInt();
		}

		void IDynamicAccessor<int>.SetIndex(int key, int value)
		{
			SetPropertyValue(key, value);
		}

		int IDynamicAccessor<int>.GetIndex(object key)
		{
			return GetPropertyValue(key).AsInt();
		}

		void IDynamicAccessor<int>.SetIndex(object key, int value)
		{
			SetPropertyValue(key, value);
		}

		#endregion

		#region IDynamicAccessor implementation

		bool IDynamicAccessor<bool>.GetMember(string name, ref uint hint, bool defaultValue)
		{
			return GetPropertyValue(name).AsBoolean(defaultValue);
		}

		void IDynamicAccessor<bool>.SetMember(string name, ref uint hint, bool value)
		{
			SetPropertyValue(name, value);
		}

		bool IDynamicAccessor<bool>.GetIndex(string key)
		{
			return GetPropertyValue(key).AsBoolean();
		}

		void IDynamicAccessor<bool>.SetIndex(string key, bool value)
		{
			SetPropertyValue(key, value);
		}

		bool IDynamicAccessor<bool>.GetIndex(int key)
		{
			return GetPropertyValue(key).AsBoolean();
		}

		void IDynamicAccessor<bool>.SetIndex(int key, bool value)
		{
			SetPropertyValue(key, value);
		}

		bool IDynamicAccessor<bool>.GetIndex(object key)
		{
			return GetPropertyValue(key).AsBoolean();
		}

		void IDynamicAccessor<bool>.SetIndex(object key, bool value)
		{
			SetPropertyValue(key, value);
		}

		#endregion

		#region IDynamicAccessor implementation

		Variant IDynamicAccessor<Variant>.GetMember(string name, ref uint hint, Variant defaultValue)
		{
			return GetPropertyValue(name, defaultValue);
		}

		void IDynamicAccessor<Variant>.SetMember(string name, ref uint hint, Variant value)
		{
			SetPropertyValue(name, value);
		}

		Variant IDynamicAccessor<Variant>.GetIndex(string key)
		{
			return GetPropertyValue(key);
		}

		void IDynamicAccessor<Variant>.SetIndex(string key, Variant value)
		{
			SetPropertyValue(key, value);
		}

		Variant IDynamicAccessor<Variant>.GetIndex(int key)
		{
			return GetPropertyValue(key);
		}

		void IDynamicAccessor<Variant>.SetIndex(int key, Variant value)
		{
			SetPropertyValue(key, value);
		}

		Variant IDynamicAccessor<Variant>.GetIndex(object key)
		{
			return GetPropertyValue(key);
		}

		void IDynamicAccessor<Variant>.SetIndex(object key, Variant value)
		{
			SetPropertyValue(key, value);
		}

		#endregion

		#region IDynamicClass implementation

		dynamic IDynamicClass.__GetDynamicValue(string name)
		{
			return this.GetPropertyValue(name).AsDynamic();
		}

		bool IDynamicClass.__TryGetDynamicValue(string name, out object value)
		{
			Variant v = this.GetPropertyValue(name);
			if (v.IsDefined) {
				value = v.AsObject();
				return true;
			} else {
				value = null;
				return false;
			}
		}

		void IDynamicClass.__SetDynamicValue(string name, object value)
		{
			this.SetPropertyValue(name, Variant.FromAnyType(value));
		}

		bool IDynamicClass.__DeleteDynamicValue(object name)
		{
			return Remove(ConvertKey(name));
		}

		bool IDynamicClass.__HasDynamicValue(string name)
		{
			return ContainsKey(name);
		}

		IEnumerable IDynamicClass.__GetDynamicNames()
		{
			throw new NotImplementedException();
		}
		#endregion

		#region Debugger Support
		[DebuggerDisplay("{value}", Name = "{key}", Type = "{ValueTypeName}")]
		internal class KeyValuePairDebugView
		{
			public string key   {get { return _isDynamic ? (_key + "*") : _key; }}
			public object value 
			{
				get { return _expando[_key].AsObject();}
				set { _expando[_key] = Variant.FromAnyType(value);}
			}

			public KeyValuePairDebugView(Amf3Object expando, string key, bool isDynamic)
			{
				_expando = expando;
				_key = key;
				_isDynamic = isDynamic;
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
			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			private readonly bool _isDynamic;
		}

		internal class Amf3ObjectDebugView
		{
			private Amf3Object mObject;

			public Amf3ObjectDebugView(Amf3Object expando)
			{
				this.mObject = expando;
			}

			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			public KeyValuePairDebugView[] Keys
			{
				get
				{
					var keys = new KeyValuePairDebugView[mObject.Count];

					int i = 0;
					foreach(string key in mObject.ClassDef.Properties)
					{
						keys[i] = new KeyValuePairDebugView(mObject, key, false);
						i++;
					}

					if (mObject.DynamicProperties!=null)  {
						foreach(string key in mObject.DynamicProperties.Keys)
						{
							keys[i] = new KeyValuePairDebugView(mObject, key, true);
							i++;
						}
					}
					return keys;
				}
			}
		}
		#endregion 

		#region IDynamicAccessorTyped implementation

		string IDynamicAccessorTyped.GetMemberString (string key, ref uint hint, string defaultValue)
		{
			return GetPropertyValue (key).AsString (defaultValue);
		}

		void IDynamicAccessorTyped.SetMemberString (string key, string value)
		{
			SetPropertyValue (key, value);
		}

		int IDynamicAccessorTyped.GetMemberInt (string key, ref uint hint, int defaultValue)
		{
			return GetPropertyValue (key).AsInt (defaultValue);
		}

		void IDynamicAccessorTyped.SetMemberInt (string key, int value)
		{
			SetPropertyValue (key, value);
		}

		uint IDynamicAccessorTyped.GetMemberUInt (string key, ref uint hint, uint defaultValue)
		{
			return GetPropertyValue (key).AsUInt (defaultValue);
		}

		void IDynamicAccessorTyped.SetMemberUInt (string key, uint value)
		{
			SetPropertyValue (key, value);
		}

		double IDynamicAccessorTyped.GetMemberNumber (string key, ref uint hint, double defaultValue)
		{
			return GetPropertyValue (key).AsNumber (defaultValue);
		}

		void IDynamicAccessorTyped.SetMemberNumber (string key, double value)
		{
			SetPropertyValue (key, value);
		}

		bool IDynamicAccessorTyped.GetMemberBool (string key, ref uint hint, bool defaultValue)
		{
			return GetPropertyValue (key).AsBoolean (defaultValue);
		}

		void IDynamicAccessorTyped.SetMemberBool (string key, bool value)
		{
			SetPropertyValue (key, value);
		}

		#endregion


    }
}
