using System;

namespace _root
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Assembly | 
	                AttributeTargets.Method, AllowMultiple = false)]
	public class AllowDynamicAttribute : Attribute
	{
		/// <summary>
		/// The target namespace if this is an assembly level attribute.
		/// </summary>
		public string package;

		/// <summary>
		/// Initializes a new instance of the <see cref="_root.AllowDynamicAttribute"/> class.
		/// </summary>
		public AllowDynamicAttribute ()
		{
		}
	}
}

