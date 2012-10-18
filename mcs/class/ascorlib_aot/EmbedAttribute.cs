using System;

namespace _root
{
	[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
	public class EmbedAttribute : Attribute
	{
		public string source { get; set; }

		public EmbedAttribute ()
		{
		}
	}
}

