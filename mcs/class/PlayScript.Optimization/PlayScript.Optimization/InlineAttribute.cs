using System;

namespace PlayScript.Optimization
{
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false)]
	public class InlineAttribute : Attribute
	{
	}
}

