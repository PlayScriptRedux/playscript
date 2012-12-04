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
			return (o as string) ?? o.ToString ();
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
				if (s == string.Empty)
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

