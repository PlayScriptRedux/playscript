using System;

namespace _root
{
	public static class Boolean
	{
		public static string toString(this bool b) {
			return b ? "true" : "false";
		}
		
		public static bool valueOf(this bool b) {
			return b;
		}
	}
}

