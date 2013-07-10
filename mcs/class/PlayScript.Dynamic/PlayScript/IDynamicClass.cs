using System;
using System.Collections;

namespace PlayScript
{
	public interface IDynamicClass
	{

		dynamic __GetDynamicValue(string name);

		bool __TryGetDynamicValue(string name, out object value);

		void __SetDynamicValue(string name, object value);

		bool __DeleteDynamicValue(object name);

		bool __HasDynamicValue(string name);

		IEnumerable __GetDynamicNames();

	}
}

