//
// PSConverter.cs
//
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
using System.Reflection;
using System.Collections;

namespace PlayScript.DynamicRuntime
{
	public static class PSConverter
	{
		public static object ConvertToString(object o, Type targetType)
		{
			return ConvertToString (o);
		}

		public static Func<object, Type, object> GetConversionFunction(object value, Type targetType, bool valueTypeIsConstant)
		{
			if (!valueTypeIsConstant) {
				// must use the slower convert method
				return Dynamic.ConvertValue;
			}

			if (value == null) {
				// no conversion required
				return null;
			}

			Type valueType = value.GetType();
			if (targetType == valueType) {
				// no conversion required
				return null;
			}

			if (targetType == typeof(System.Object)) {
				// no conversion required
				return null;
			}

			if (targetType.IsAssignableFrom(valueType)) {
				// no conversion required
				return null;
			} else {
				if (targetType == typeof(String)) {
					// conversion required
					return ConvertToString;
				}
				// conversion required
				return System.Convert.ChangeType;
			}
		}
		
		public static int ConvertToInt (object o)
		{
			Stats.Increment(StatsCounter.ConvertBinderInvoked);

			if (o is int) {
				return (int)o;
			}
			if (o is uint) {
				return (int)(uint)o;
			}
			if (PlayScript.Dynamic.IsNullOrUndefined (o)) {
				return 0;
			}

			var typeCode = Type.GetTypeCode (o.GetType ());
			switch (typeCode) {
			case TypeCode.Int32:
				return (int)o;
			case TypeCode.Double:
				return (int)((double)o);
			case TypeCode.Boolean:
				return (bool)o ? 1 : 0;
			case TypeCode.UInt32:
				return (int)((uint)o);
			case TypeCode.Single:
				return (int)((float)o);
			case TypeCode.String: {
					string s =(string)o;
					if (s.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase)) {
						// Hex number - Use Convert.ToInt32() so we don't have to strip "0x" from the string.
						return Convert.ToInt32(s, 16);
					} else {
						return int.Parse(s);
					}
				}
			default:
				throw new Exception ("Invalid cast to int");
			}
		}

		public static uint ConvertToUInt (object o)
		{
			Stats.Increment(StatsCounter.ConvertBinderInvoked);

			if (o is uint) {
				return (uint)o;
			}
			if (o is int) {
				return (uint)(int)o;
			}
			if (PlayScript.Dynamic.IsNullOrUndefined (o)) {
				return 0;
			}

			var typeCode = Type.GetTypeCode (o.GetType ());
			switch (typeCode) {
			case TypeCode.Int32:
				return (uint)((int)o);
			case TypeCode.Double:
				return (uint)((double)o);
			case TypeCode.Boolean:
				return (bool)o ? (uint)1 : (uint)0;
			case TypeCode.UInt32:
				return (uint)o;
			case TypeCode.Single:
				return (uint)((float)o);
			case TypeCode.String:
				return uint.Parse((String)o);
			default:
				throw new Exception ("Invalid cast to int");
			}
		}

		public static float ConvertToFloat (object o)
		{
			Stats.Increment(StatsCounter.ConvertBinderInvoked);

			if (o is float) {
				return (float)o;
			} 
			if (o is double) {
				return (float)(double)o;
			}
			if (PlayScript.Dynamic.IsUndefined (o)) {
				return float.NaN;
			}
			if (o == null) {
				return 0.0f;
			}

			var typeCode = Type.GetTypeCode (o.GetType ());
			switch (typeCode) {
			case TypeCode.Int32:
				return (int)o;
			case TypeCode.Double:
				return (float)o;
			case TypeCode.Boolean:
				return (bool)o ? 1 : 0;
			case TypeCode.UInt32:
				return (uint)o;
			case TypeCode.Single:
				return (float)o;
			case TypeCode.String:
				return float.Parse((String)o);
			default:
				throw new Exception ("Invalid cast to float");
			}
		}

		public static double ConvertToDouble (object o)
		{
			Stats.Increment(StatsCounter.ConvertBinderInvoked);

			if (o is double) {
				return (double)o;
			} 
			if (o is float) {
				return (double)(float)o;
			} 
			if (PlayScript.Dynamic.IsUndefined (o)) {
				return double.NaN;
			}
			if (o == null) {
				return 0.0;
			}

			var typeCode = Type.GetTypeCode (o.GetType ());
			switch (typeCode) {
			case TypeCode.Int32:
				return (int)o;
			case TypeCode.Double:
				return (double)o;
			case TypeCode.Boolean:
				return (bool)o ? 1 : 0;
			case TypeCode.UInt32:
				return (uint)o;
			case TypeCode.Single:
				return (float)o;
			case TypeCode.String:
				return double.Parse((String)o);
			default:
				throw new Exception ("Invalid cast to double");
			}
		}

		public static bool ConvertToBool (object o)
		{
			Stats.Increment(StatsCounter.ConvertBinderInvoked);

			if (o is bool) {
				return (bool)o;
			} 
			if (PlayScript.Dynamic.IsNullOrUndefined (o)) {
				return false;
			}

			var typeCode = Type.GetTypeCode (o.GetType ());
			switch (typeCode) {
			case TypeCode.Int32:
				return (int)o != 0;
			case TypeCode.Double:
				return (double)o != 0.0;
			case TypeCode.Boolean:
				return (bool)o;
			case TypeCode.UInt32:
				return (uint)o != 0;
			case TypeCode.Single:
				return (float)o != 0.0f;
			default:
				throw new Exception ("Invalid cast to int");
			}
		}

		public static string ConvertToString (object o)
		{
			Stats.Increment(StatsCounter.ConvertBinderInvoked);

			if (PlayScript.Dynamic.IsNullOrUndefined (o)) {
				return null;
			} else if  (o is string) {
				return (string)o;
			} else {
				return o.ToString ();
			}
		}

		public static object ConvertToObj (object o)
		{
			Stats.Increment(StatsCounter.ConvertBinderInvoked);

			if (o == PlayScript.Undefined._undefined) {
				return null; // only type "*" can be undefined
			}

			return o;
		}

	}

}
