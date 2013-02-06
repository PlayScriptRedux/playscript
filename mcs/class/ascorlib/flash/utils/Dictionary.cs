using System;
using System.Collections.Generic;

namespace flash.utils
{
	public class Dictionary : Dictionary<object, object>
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

	}
}

