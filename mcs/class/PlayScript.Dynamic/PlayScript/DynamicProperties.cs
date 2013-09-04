using System;
using System.Collections;
using System.Collections.Generic;

namespace PlayScript
{
	public class DynamicProperties : IDynamicClass
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
			mDictionary.TryGetValue(name, out value);
			return value;
		}

		public bool __TryGetDynamicValue(string name, out object value)
		{
			return mDictionary.TryGetValue(name, out value);
		}

		public void __SetDynamicValue(string name, object value)
		{
			mDictionary[name] = value;
		}
		public bool __DeleteDynamicValue(object name)
		{
			return mDictionary.Remove((string)name);
		}

		public bool __HasDynamicValue(string name)
		{
			if (name == null) 
				return false;
			return mDictionary.ContainsKey(name);
		}

		public IEnumerable __GetDynamicNames()
		{
			return ((IEnumerable)mDictionary.Keys);
		}

		#endregion

//		private readonly object mParentObject;
		private readonly Dictionary<string, object> mDictionary = new Dictionary<string, object>();
	}

}

