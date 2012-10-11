using System;

namespace _root
{
	public static partial class trace_fn
	{

		//
		// Trace (overloaded versions) - overloading package methods not supported in ASX.
		//

		public static void trace(object o) {
//			System.Diagnostics.Debug.WriteLine(o);
			Console.WriteLine(o);
		}

	}

	public static partial class trace_fn 
	{

		public static void trace(object o1, object o2) {
//			System.Diagnostics.Debug.WriteLine("{0}{1}", o1, o2);
			Console.WriteLine("{0}{1}", o1, o2);
		}

		public static void trace(object o1, object o2, object o3) {
//			System.Diagnostics.Debug.WriteLine("{0}{1}{2}", o1, o2, o3);
			Console.WriteLine("{0}{1}{2}", o1, o2, o3);
		}

		public static void trace(object o1, object o2, object o3, params object[] args) {
			var argsStr = String.Concat(args);
//			System.Diagnostics.Debug.WriteLine("{0}{1}{2}{3}", o1, o2, o3, argsStr);
			Console.WriteLine("{0}{1}{2}{3}", o1, o2, o3, argsStr);
		}
	}

	//
	// Conversions (must be in C# to avoid conflicts).
	//

	public static class String_fn
	{
		public static string String (object o)
		{
			return (o as String) ?? o.ToString ();
		}
	}

	public static class Number_fn
	{
		// Inlineable method
		public static double Number (object o)
		{
			return o is double ? (double)o : internalNumber(o);
		}

		private static double internalNumber(object o)
		{
			var type = o.GetType ();
			var typeCode = Type.GetTypeCode (type);
			switch (typeCode) {
			case TypeCode.Boolean:
				return (bool)o ? 1 : 0;
			case TypeCode.String:
				var s = o as string;
				if (s == String.Empty)
					return 0;
				double r = 0;
				if (!double.TryParse (s, out r))
					return Double.NaN;
				return r;
			case TypeCode.Byte:
				return (byte)o;
			case TypeCode.SByte:
				return (sbyte)o;
			case TypeCode.Int16:
				return (short)o;
			case TypeCode.UInt16:
				return (ushort)o;
			case TypeCode.Int32:
				return (int)o;
			case TypeCode.UInt32:
				return (uint)o;
			case TypeCode.Int64:
				return (long)o;
			case TypeCode.UInt64:
				return (ulong)o;
			case TypeCode.Decimal:
				return (double)(decimal)o;
			case TypeCode.Char:
				return (char)o;
			case TypeCode.Double:
				return (double)o;
			case TypeCode.Single:
				return (float)o;
			default:
				return Double.NaN;
			}
		}
	}
}

