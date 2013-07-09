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
	public class Dictionary : Dictionary<object, object>, IDynamicClass
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

		bool IDynamicClass.__HasDynamicValue (string name)
		{
			return this.ContainsKey(name);
		}

		IEnumerable IDynamicClass.__GetDynamicNames ()
		{
			return this.Keys;
		}

		#endregion

		#region DebugView
		[DebuggerDisplay("{value}", Name = "{key}")]
		internal class KeyValuePairDebugView
		{
			public object key;
			public object value;
			
			public KeyValuePairDebugView(object key, object value)
			{
				this.value = value;
				this.key = key;
			}
		}
		
		internal class DictionaryDebugView
		{
			private Dictionary dict;
			
			public DictionaryDebugView(Dictionary expando)
			{
				this.dict = expando;
			}
			
			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			public KeyValuePairDebugView[] Keys
			{
				get
				{
					var keys = new KeyValuePairDebugView[dict.Count];
					
					int i = 0;
					foreach(Object key in dict.Keys)
					{
						keys[i] = new KeyValuePairDebugView(key, dict[key]);
						i++;
					}
					return keys;
				}
			}
		}
		#endregion
	}
}

