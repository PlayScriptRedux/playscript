using System;
using System.Collections;
using System.Collections.Generic;

namespace PlayScript
{
	public class DynamicProperties : Dictionary<string, object>, IDynamicClass
	{
		// the parent object is the object that owns these properties
		// if a lookup fails we should use members of this object instead
		public DynamicProperties(object parentObject = null) {
//			mParentObject = parentObject;
		}

		#region IDynamicClass implementation

		public dynamic __GetDynamicValue(string name)
		{
			object value;
			__TryGetDynamicValue (name, out value);
			return value;
		}

		public bool __TryGetDynamicValue(string name, out object value)
		{
			if (!this.TryGetValue (name, out value)) {
				value = PlayScript.Undefined._undefined;
				return false;
			}
			return true;
		}

		public void __SetDynamicValue(string name, object value)
		{
			this[name] = value;
		}
		public bool __DeleteDynamicValue(object name)
		{
			return this.Remove((string)name);
		}

		public bool __HasDynamicValue(string name)
		{
			if (name == null) 
				return false;
			return this.ContainsKey(name);
		}

		public IEnumerable __GetDynamicNames()
		{
			return ((IEnumerable)this.Keys);
		}

		#endregion
	}

}

