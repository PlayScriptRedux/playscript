using System;

namespace _root
{
	public static class Extensions
	{
		public static String toString(this object o) {
			return o.ToString ();
		}

		public static String toLocaleString(this object o) {
			return o.ToString ();
		}

		public static bool hasOwnProperty(this object o, string name) {
			var t = o.GetType ();
			return t.GetProperty(name) != null || t.GetField(name) != null;
		}
	}
}

