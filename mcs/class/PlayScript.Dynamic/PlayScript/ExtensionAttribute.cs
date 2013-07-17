using System;

namespace PlayScript
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class ExtensionAttribute : Attribute
	{
		public ExtensionAttribute (Type overloadedType)
		{
			OverloadedType = overloadedType;
		}

		public Type OverloadedType { get; set; }
	}
}

