// Copyright 2013 Zynga Inc.
//	
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//		
//      Unless required by applicable law or agreed to in writing, software
//      distributed under the License is distributed on an "AS IS" BASIS,
//      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//      See the License for the specific language governing permissions and
//      limitations under the License.

using System;

namespace _root
{




	//
	// Conversions (must be in C# to avoid conflicts).
	//

	public static class String_fn
	{
		public static string CastToString (object o)
		{
			if (o is string) {
				return (string)o;
			}
			if (o == null) {
				return "null";
			}
			if (o == PlayScript.Undefined._undefined) {
				return "undefined";
			}

			return o.ToString();
		}

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
			if (!double.TryParse(s, out d)) {
				return double.NaN;
			}
			return d;
		}

		public static double Number (object o)
		{
			if (o == null) return 0.0;
			if (o == PlayScript.Undefined._undefined) return double.NaN;

			double d;

			TypeCode tc = Type.GetTypeCode(o.GetType());
			switch (tc) {
				case TypeCode.Boolean:
					return (bool)o ? 1 : 0;
				case TypeCode.SByte:
					return (sbyte)o;
				case TypeCode.Byte:
					return (byte)o;
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
				case TypeCode.Single:
					return (float)o;
				case TypeCode.Double:
					return (double)o;
				case TypeCode.Decimal:
					return (double)(decimal)o;
				case TypeCode.String:
					if (double.TryParse((string)o, out d))
					{
						return d;
					}
					return double.NaN;
			}

			return double.NaN;
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

		public static int @int (object o)
		{
			if (o == null) return 0;

			int i;

			TypeCode tc = Type.GetTypeCode(o.GetType());
			switch (tc) {
				case TypeCode.Boolean:
					return (bool)o ? 1 : 0;
				case TypeCode.SByte:
					return (sbyte)o;
				case TypeCode.Byte:
					return (byte)o;
				case TypeCode.Int16:
					return (short)o;
				case TypeCode.UInt16:
					return (ushort)o;
				case TypeCode.Int32:
					return (int)o;
				case TypeCode.UInt32:
					return (int)(uint)o;
				case TypeCode.Int64:
					return (int)(long)o;
				case TypeCode.UInt64:
					return (int)(ulong)o;
				case TypeCode.Single:
					return (int)(float)o;
				case TypeCode.Double:
					return (int)(double)o;
				case TypeCode.Decimal:
					return (int)(decimal)o;
				case TypeCode.String:
					i = 0;
					int.TryParse((string)o, out i);
					return i;
			}

			return 0;
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

		public static uint @uint (object o)
		{
			if (o == null) return 0;

			uint u;

			TypeCode tc = Type.GetTypeCode(o.GetType());
			switch (tc) {
				case TypeCode.Boolean:
					return (bool)o ? 1u : 0u;
				case TypeCode.SByte:
					return (uint)(sbyte)o;
				case TypeCode.Byte:
					return (byte)o;
				case TypeCode.Int16:
					return (uint)(short)o;
				case TypeCode.UInt16:
					return (ushort)o;
				case TypeCode.Int32:
					return (uint)(int)o;
				case TypeCode.UInt32:
					return (uint)o;
				case TypeCode.Int64:
					return (uint)(long)o;
				case TypeCode.UInt64:
					return (uint)(ulong)o;
				case TypeCode.Single:
					return (uint)(float)o;
				case TypeCode.Double:
					return (uint)(double)o;
				case TypeCode.Decimal:
					return (uint)(decimal)o;
				case TypeCode.String:
					u = 0;
					uint.TryParse((string)o, out u);
					return u;
			}

			return 0;
		}

	}

	public static class Boolean_fn
	{  

		// Not inlinable.. but required to get correct results in flash.
		public static bool Boolean (object d)
		{
			// handle most common cases first
			if (d == null) {
				return false;
			}

			if (d is bool) {
				return (bool)d;
			}

			if (d == PlayScript.Undefined._undefined) {
				return false;
			}

			if (d is string) {
				var s = (string)d;
				return !string.IsNullOrEmpty(s) && s != "0" && s != "false";
			}

			TypeCode tc = Type.GetTypeCode(d.GetType());
			switch (tc) {
			case TypeCode.Boolean:
				return (bool)d;
			case TypeCode.SByte:
				return (sbyte)d != 0;
			case TypeCode.Byte:
				return (byte)d != 0;
			case TypeCode.Int16:
				return (short)d != 0;
			case TypeCode.UInt16:
				return (ushort)d != 0;
			case TypeCode.Int32:
				return (int)d != 0;
			case TypeCode.UInt32:
				return (uint)d != 0;
			case TypeCode.Int64:
				return (long)d != 0;
			case TypeCode.UInt64:
				return (ulong)d != 0;
			case TypeCode.Single:
				return (float)d != 0.0f;
			case TypeCode.Double:
				return (double)d != 0.0;
			case TypeCode.Decimal:
				return (decimal)d != 0;
			case TypeCode.String:
				var s = (string)d;
				return !string.IsNullOrEmpty(s) && s != "0" && s != "false";
			case TypeCode.Empty:
				return false;
			case TypeCode.Object:
				return (d != null) && (d != PlayScript.Undefined._undefined);
			}
			return false;
		}
	}

	public static class _typeof_fn
	{
		public static string _typeof (object d) {
			if (d == null || d == PlayScript.Undefined._undefined) 
				return "undefined";
			
			if (d is XML || d is XMLList)
				return "xml";
			
			if (d is Delegate)
				return "function";
			
			TypeCode tc = Type.GetTypeCode(d.GetType());
			switch (tc) {
			case TypeCode.Boolean:
				return "boolean";
			case TypeCode.SByte:
			case TypeCode.Byte:
			case TypeCode.Int16:
			case TypeCode.UInt16:
			case TypeCode.Int32:
			case TypeCode.UInt32:
			case TypeCode.Int64:
			case TypeCode.UInt64:
			case TypeCode.Single:
			case TypeCode.Double:
			case TypeCode.Decimal:
				return "number";
			default:
				return "object";
			}
		}
	}

}

