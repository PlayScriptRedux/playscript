using System;
using System.Collections;
using System.Collections.Generic;

namespace PlayScript
{
	public class DynamicProperties : IDynamicClass
	{
		public DynamicProperties() {
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

		private readonly Dictionary<string, object> mDictionary = new Dictionary<string, object>();
	}

}

