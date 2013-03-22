using System;

namespace _root
{
	[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
	public class DeprecatedAttribute : Attribute
	{
		public DeprecatedAttribute()
		{
		}

		public string message {get;set;}
	}
}

