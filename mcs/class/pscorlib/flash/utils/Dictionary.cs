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
using PlayScript;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace flash.utils
{
	[DebuggerDisplay ("Count = {Count}")]
	[DebuggerTypeProxy (typeof (DictionaryDebugView))]
	[DynamicClass]
	public class Dictionary : Dictionary<object, object>, IDynamicClass, PlayScript.IKeyEnumerable
	{
		public Dictionary(bool weakKeys = false) 
			: base()
		{
		}

		public string toJSON(string k) {
			throw new NotImplementedException();
		}
		
		public int length { 
			get {
				return this.Count;
			} 	
			set {
				if (value == 0)
					this.Clear();
				else
					throw new System.NotImplementedException();
			}
		}

		public new dynamic this [object key] {
			get {
				// the flash dictionary implementation does not throw if key not found
				object value;
				if (key == null) {
					return null; // PlayScript.Undefined._undefined;
				}
				if (base.TryGetValue(key, out value)) {
					return value;
				} else {
					return null; // PlayScript.Undefined._undefined;
				}
			}
			set {
				base[key] = value;
			}
		}


		public bool hasOwnProperty(string name) {
			return ContainsKey(name);
		}

		public object[] cloneKeyArray() {
			var keys = new object[Count];
			int i=0;
			foreach (var key in Keys)
			{
				keys[i++] = key;
			}
			return keys;
		}

		#region IDynamicClass implementation

		dynamic IDynamicClass.__GetDynamicValue (string name)
		{
			return this[name];
		}

		bool IDynamicClass.__TryGetDynamicValue(string name, out object value) 
		{
			return this.TryGetValue(name, out value);
		}

		void IDynamicClass.__SetDynamicValue (string name, object value)
		{
			this[name] = value;
		}

		bool IDynamicClass.__DeleteDynamicValue (object name)
		{
			return this.Remove(name);
		}

		bool IDynamicClass.__HasDynamicValue (string name)
		{
			return this.ContainsKey(name);
		}

		IEnumerable IDynamicClass.__GetDynamicNames ()
		{
			return this.Keys;
		}

		#endregion

		#region IKeyEnumerable implementation

		public KeyCollection.Enumerator GetKeyEnumerator()
		{
			return this.Keys.GetEnumerator();
		}

		IEnumerator PlayScript.IKeyEnumerable.GetKeyEnumerator()
		{
			return this.Keys.GetEnumerator();
		}

		#endregion

		#region DebugView
		[DebuggerDisplay("{value}", Name = "{key}", Type = "{ValueTypeName}")]
		internal class KeyValuePairDebugView
		{
			public object key   {get { return _key; }}
			public object value 
			{
				get { return _dict[_key];}
				set { _dict[_key] = value;}
			}

			public KeyValuePairDebugView(Dictionary expando, object key)
			{
				_dict = expando;
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
			private readonly Dictionary    _dict;
			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			private readonly object        _key;
		}
		
		internal class DictionaryDebugView
		{
			private Dictionary _dict;
			
			public DictionaryDebugView(Dictionary dict)
			{
				_dict = dict;
			}
			
			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			public KeyValuePairDebugView[] Keys
			{
				get
				{
					var keys = new KeyValuePairDebugView[_dict.Count];
					
					int i = 0;
					foreach(Object key in _dict.Keys)
					{
						keys[i] = new KeyValuePairDebugView(_dict, key);
						i++;
					}
					return keys;
				}
			}
		}
		#endregion
	}
}

