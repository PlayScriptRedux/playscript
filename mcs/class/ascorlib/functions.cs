using System;

namespace _root
{

	//
	// Conversions (must be in C# to avoid conflicts).
	//

	public static class String_fn
	{
		public static string String (object o)
		{
			return o.ToString();
		}

		public static string String (string s)
		{
			return s;
		}

		public static string String (int i)
		{
			return i.ToString ();
		}

		public static string String (uint u)
		{
			return u.ToString ();
		}

		public static string String (double d)
		{
			return d.ToString ();
		}

		public static string String (bool b)
		{
			return b.ToString ();
		}

	}

	public static class Number_fn
	{  
		// Inlineable method
		public static double Number (string s)
		{
			double d;
			double.TryParse(s, out d);
			return d;
		}

	}

	public static class int_fn
	{  
		// Inlineable method
		public static int @int (string s)
		{
			int i;
			int.TryParse(s, out i);
			return i;
		}
		
	}

	public static class uint_fn
	{  

		// Inlineable method
		public static uint @uint (string s)
		{
			uint u;
			uint.TryParse(s, out u);
			return u;
		}

	}

	public static class Boolean_fn
	{  

		// Inlineable method
		public static bool Boolean (string s)
		{
			throw new System.NotImplementedException();
		}
		
	}

}

