using System;

namespace _root
{
	[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
	public class EventAttribute : Attribute
	{
		public string name { get; set; }

		public string type { get; set; }

		public EventAttribute ()
		{
		}
	}
}

