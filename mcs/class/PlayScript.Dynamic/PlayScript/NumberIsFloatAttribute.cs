using System;

namespace PlayScript
{
	//
	// This attribute provides a mechanism to use single precision floats
	// instead of doubles for the PlayScript Number type. This is a class
	// level attribute that is applied recursively - everything from fields,
	// constants, method parameters and return values are converted to floats.
	//
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false)]
	public class NumberIsFloatAttribute : Attribute
	{
		public NumberIsFloatAttribute ()
		{
		}
	}
}
