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
using System.Collections.Generic;
using PlayScript;

namespace flash.utils
{
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
				if (base.TryGetValue(key, out value)) {
					return value;
				} else {
					return null;
				}
			}
			set {
				base[key] = value;
			}
		}

		#region IDynamicClass implementation

		public dynamic __GetDynamicValue (string name)
		{
			return this[name];
		}

		public void __SetDynamicValue (string name, object value)
		{
			this[name] = value;
		}

		public bool __HasDynamicValue (string name)
		{
			return this.ContainsKey(name);
		}

		public _root.Array __GetDynamicNames ()
		{
			var a = new _root.Array();
			foreach (KeyValuePair<object, object> pair in this) {
				if (pair.Key is String) {
					a.push(pair.Key);
				}
			}

			return a;
		}

		#endregion
	}
}

