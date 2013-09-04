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
#if !DYNAMIC_SUPPORT

using System;
using System.Collections.Generic;
using PlayScript;

namespace PlayScript.RuntimeBinder
{

	class PSConvertBinder : CallSiteBinder
	{
		private static Dictionary<Type, object> delegates = new Dictionary<Type, object>();

	//	readonly Type type;
	//	readonly CSharpBinderFlags flags;
	//	readonly Type context;
		
		public PSConvertBinder (Type type, Type context, CSharpBinderFlags flags)
		{
	//		this.type = type;
	//		this.flags = flags;
	//		this.context = context;
		}

		public static int ConvertToInt (CallSite site, object o)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.ConvertBinderInvoked);
#endif
			if (o is int) {
				return (int)o;
			} 
			if (o is uint) {
				return (int)(uint)o;
			} 
			if (o == null || o == PlayScript.Undefined._undefined) {
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

		public static uint ConvertToUInt (CallSite site, object o)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.ConvertBinderInvoked);
#endif
			if (o is uint) {
				return (uint)o;
			} 
			if (o is int) {
				return (uint)(int)o;
			} 
			if (o == null || o == PlayScript.Undefined._undefined) {
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

		public static double ConvertToDouble (CallSite site, object o)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.ConvertBinderInvoked);
#endif
			if (o is double) {
				return (double)o;
			} 
			if (o is float) {
				return (double)(float)o;
			} 
			if (o == null || o == PlayScript.Undefined._undefined) {
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

		public static bool ConvertToBool (CallSite site, object o)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.ConvertBinderInvoked);
#endif
			if (o is bool) {
				return (bool)o;
			} 
			if (o == null || o == PlayScript.Undefined._undefined) {
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

		public static string ConvertToString (CallSite site, object o)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.ConvertBinderInvoked);
#endif
			if (o == null || o == PlayScript.Undefined._undefined) {
				return null;
			} else if  (o is string) {
				return (string)o;
			} else {
				return o.ToString ();
			}
		}

		public static object ConvertToObj (CallSite site, object o)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.ConvertBinderInvoked);
#endif
			return o;
		}

		static PSConvertBinder ()
		{
			delegates.Add (typeof(Func<CallSite, object, int>), (Func<CallSite, object, int>)ConvertToInt);
			delegates.Add (typeof(Func<CallSite, object, uint>), (Func<CallSite, object, uint>)ConvertToUInt);
			delegates.Add (typeof(Func<CallSite, object, double>), (Func<CallSite, object, double>)ConvertToDouble);
			delegates.Add (typeof(Func<CallSite, object, bool>), (Func<CallSite, object, bool>)ConvertToBool);
			delegates.Add (typeof(Func<CallSite, object, string>), (Func<CallSite, object, string>)ConvertToString);
			delegates.Add (typeof(Func<CallSite, object, object>), (Func<CallSite, object, object>)ConvertToObj);
		}
		
		public override object Bind (Type delegateType)
		{
			object target;
			if (delegates.TryGetValue (delegateType, out target)) {
				return target;
			}
			throw new Exception("Unable to bind convert for target " + delegateType.FullName);
		}

	}

}
#endif

