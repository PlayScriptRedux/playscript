using System;
using System.Collections;

namespace _root
{
	public static class Extensions
	{
//		public static string toString(this object o) 
//		{
//			return o.ToString ();
//		}

		public static string toLocaleString(this object o) 
		{
			return o.ToString ();
		}

		public static bool hasOwnProperty(this object o, string name) 
		{
			var t = o.GetType ();
			return t.GetProperty(name) != null || t.GetField(name) != null;
		}

		public static string toString(this uint o, int digits) 
		{
			return o.ToString ();
		}

		//
		// IList extensions (for arrays, etc).
		//

//		public static int get_length(this IList list) 
//		{
//			return list.Count;
//		}

	}
}

