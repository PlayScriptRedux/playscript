using System;

namespace _root
{
	[AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
	public class SWFAttribute : Attribute
	{
		public string width { get; set; }
		public string height { get; set; }
		public string frameRate { get; set; }
		public string backgroundColor { get; set; }
		public string quality {get;set;}
		
		public SWFAttribute ()
		{
		}
	}
}

