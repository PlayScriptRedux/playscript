using System;

namespace PlayScript
{
	// Indicates this class was declared using "dynamic"
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class DynamicClassAttribute : Attribute
	{
	}
}

