using System;

namespace _root
{
	[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
	public class EmbedAttribute : Attribute
	{
		public string source { get; set; }

		public string mimeType { get; set; }

		public string embedAsCFF { get; set; }

		public string fontFamily { get; set; }

		public string symbol { get; set; }

		public EmbedAttribute (string _source = null)
		{
			source = _source;
		}
	}
}

