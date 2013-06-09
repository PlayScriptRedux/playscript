using System;

namespace Mono.Optimization
{
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false)]
	public class InlineAttribute : Attribute
	{
	}
}

