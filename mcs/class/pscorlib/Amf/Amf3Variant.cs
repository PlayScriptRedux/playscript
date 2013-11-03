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
#if PLATFORM_MONOTOUCH || PLATFORM_MONODROID
#define USE_UNION
#endif

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Amf
{
	// this struct can hold any value read from an AMF stream
	// this is used to prevent unnecessary boxing of value types (bool/int/number etc)
#if USE_UNION
	[StructLayout(LayoutKind.Explicit, Size=16)]
#endif
	public struct Amf3Variant : IEquatable<Amf3Variant>
	{
#if PLATFORM_MONOTOUCH || PLATFORM_MONODROID
		[FieldOffset(0)]
		public Amf3TypeCode Type;
		[FieldOffset(4)]
		public object 		ObjectValue;	// this handles all other object types
		[FieldOffset(8)]
		public double 		NumberValue;
		[FieldOffset(8)]
		public int 			IntValue;
#else
		public Amf3TypeCode Type;
		public int 			IntValue;
		public object 		ObjectValue;	// this handles all other object types
		public double 		NumberValue;
#endif
		public bool IsDefined
		{
			get
			{
				return Type != Amf3TypeCode.Undefined;
			}
		}

		public bool IsNumeric
		{
			get
			{
				return Type == Amf3TypeCode.Integer || Type == Amf3TypeCode.Number;
			}
		}

		public bool IsDefaultValue
		{
			get 
			{
				switch (Type) {
				case Amf3TypeCode.Undefined:
				case Amf3TypeCode.Null:
					return true;
				case Amf3TypeCode.False:
					return true;
				case Amf3TypeCode.True:
					return false;
				case Amf3TypeCode.Integer:
					return IntValue == 0;
				case Amf3TypeCode.Number:
					return NumberValue == 0.0;
				default:
					return ObjectValue == null;
				}
			}
		}

		public override string ToString()
		{
			switch (Type) {
			case Amf3TypeCode.Undefined:
				return "<undefined>";
			case Amf3TypeCode.Null:
				return "<null>";
			case Amf3TypeCode.False:
				return "false";
			case Amf3TypeCode.True:
				return "true";
			case Amf3TypeCode.Integer:
				return IntValue.ToString();
			case Amf3TypeCode.Number:
				return NumberValue.ToString();
			case Amf3TypeCode.String:
				return ObjectValue as string;
			default:
				return ObjectValue!=null ? ObjectValue.ToString() : null;
			}
		}

		public object AsObject()
		{
			switch (Type) {
			case Amf3TypeCode.Undefined:
			case Amf3TypeCode.Null:
				return null;
			case Amf3TypeCode.False:
				return sBoolFalse;
			case Amf3TypeCode.True:
				return sBoolTrue;
			case Amf3TypeCode.Integer:
				if (IntValue == 0) return sIntZero;
				if (IntValue == 1) return sIntOne;
				if (IntValue ==-1) return sIntNegOne;
				return (object)IntValue;	// box integer
			case Amf3TypeCode.Number:
				if (NumberValue == 0.0) return sNumberZero;
				if (NumberValue == 1.0) return sNumberOne;
				return (object)NumberValue; // box number
			default:
				return ObjectValue;			// return object value
			}
		}

		public int AsInt()
		{
			if (Type == Amf3TypeCode.Integer) {
				return (int)IntValue;
			}
			if (Type == Amf3TypeCode.Number) {
				return (int)NumberValue;
			}
			throw new InvalidCastException("Cannot cast to Int");
		}

		public uint AsUInt()
		{
			if (Type == Amf3TypeCode.Integer) {
				return (uint)IntValue;
			}
			if (Type == Amf3TypeCode.Number) {
				return (uint)NumberValue;
			}
			throw new InvalidCastException("Cannot cast to UInt");
		}

		public bool AsBoolean()
		{
			if (Type == Amf3TypeCode.False) {
				return false;
			}
			if (Type == Amf3TypeCode.True) {
				return true;
			}
			throw new InvalidCastException("Cannot cast to Boolean");
		}

		public double AsNumber()
		{
			if (Type == Amf3TypeCode.Number) {
				return (double)NumberValue;
			}
			if (Type == Amf3TypeCode.Integer) {
				return (double)IntValue;
			}
			throw new InvalidCastException("Cannot cast to Number");
		}

		public float AsFloat()
		{
			if (Type == Amf3TypeCode.Number) {
				return (float)NumberValue;
			}
			if (Type == Amf3TypeCode.Integer) {
				return (float)IntValue;
			}
			throw new InvalidCastException("Cannot cast to float");
		}

		public string AsString()
		{
			if (Type == Amf3TypeCode.String) {
				return (string)ObjectValue;
			}
			if (Type == Amf3TypeCode.Null) {
				return null;
			}

			throw new InvalidCastException("Cannot cast to String");
		}

		public object AsType(System.Type type)
		{
			var typeCode = System.Type.GetTypeCode (type);
			switch (typeCode) {
			case TypeCode.Int32:
				return AsInt();
			case TypeCode.Double:
				return AsNumber();
			case TypeCode.Boolean:
				return AsBoolean();
			case TypeCode.UInt32:
				return AsUInt();
			case TypeCode.Single:
				return AsFloat();
			case TypeCode.String:
				return AsString();
			case TypeCode.Object:
				return AsObject();
			default:
				throw new InvalidCastException ("Invalid cast to type:" + type.ToString());
			}
		}

		public static Amf3Variant Undefined
		{
			get {
				var v = new Amf3Variant();
				v.Type = Amf3TypeCode.Undefined;
				return v;
			}
		}

		public static Amf3Variant FromObject(object o)
		{
			var v = new Amf3Variant();
			if (o == null) {
				v.Type = Amf3TypeCode.Null;
				return v;
			}
			if (o == PlayScript.Undefined._undefined) {
				v.Type = Amf3TypeCode.Undefined;
				return v;
			}

			var typeCode = System.Type.GetTypeCode (o.GetType());
			switch (typeCode) {
			case TypeCode.Int32:
				v.Type = Amf3TypeCode.Integer;
				v.IntValue = (int)o;
				return v;
			case TypeCode.Double:
				v.Type = Amf3TypeCode.Number;
				v.NumberValue = (double)o;
				return v;
			case TypeCode.Boolean:
				v.Type = ((bool)o) ? Amf3TypeCode.True : Amf3TypeCode.False;
				return v;
			case TypeCode.UInt32:
				v.Type = Amf3TypeCode.Integer;
				v.IntValue = (int)(uint)o;
				return v;
			case TypeCode.String:
				v.Type = Amf3TypeCode.String;
				v.ObjectValue = o;
				return v;
			case TypeCode.Object:
				v.Type = Amf3TypeCode.Object;
				v.ObjectValue = o;
				return v;
			default:
				throw new InvalidCastException ("Invalid cast to type:" + o.GetType().ToString());
			}
		}

		#region IEquatable implementation
		public bool Equals(Amf3Variant other)
		{
			if (this.Type != other.Type) {

				if (this.IsNumeric && other.IsNumeric) {
					return this.AsNumber() == other.AsNumber();
				}

				// TODO we should do some type conversion here
				return false;
			}

			switch (Type) {
			case Amf3TypeCode.Undefined:
				return false;
			case Amf3TypeCode.Null:
				return true;
			case Amf3TypeCode.False:
				return true;
			case Amf3TypeCode.True:
				return true;
			case Amf3TypeCode.Integer:
				return this.IntValue == other.IntValue;
			case Amf3TypeCode.Number:
				return this.NumberValue == other.NumberValue;
			case Amf3TypeCode.String:
				return ((string)ObjectValue) == ((string)other.ObjectValue);
			default:
				return this.ObjectValue.Equals(other.ObjectValue);
			}

		}
		#endregion

		// pre-boxed values
		private static readonly object sBoolTrue = (object)true;
		private static readonly object sBoolFalse = (object)false;
		private static readonly object sIntNegOne = (object)(int)-1;
		private static readonly object sIntZero = (object)(int)0;
		private static readonly object sIntOne = (object)(int)1;
		private static readonly object sNumberZero = (object)(double)0.0;
		private static readonly object sNumberOne = (object)(double)1.0;

	};
}
