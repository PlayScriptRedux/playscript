using System;
using System.Collections;

namespace _root
{
	public static class Extensions
	{
		public static string toString(this object o) 
		{
			return o.ToString ();
		}

		public static string toLocaleString(this object o) 
		{
			return o.ToString ();
		}

		public static bool hasOwnProperty(this object o, string name) 
		{
			var t = o.GetType ();
			return t.GetProperty(name) != null || t.GetField(name) != null;
		}

		//
		// IList extensions (for arrays, etc).
		//

		public static int length(this IList l) 
		{
			return l.Count;
		}

		public static int indexOf(this IList l, object o)
		{
			return l.indexOf(o);
		}

		public static void push(this IList l, object o)
		{
			l.Add(o);
		}
	}
}

