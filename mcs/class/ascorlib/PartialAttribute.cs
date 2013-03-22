using System;

namespace _root
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
	public class PartialAttribute : Attribute
	{
		public PartialAttribute ()
		{
		}
	}
}

