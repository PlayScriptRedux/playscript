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
using System.Collections.Generic;

namespace Amf
{
	// this struct can hold any value read from an AMF stream
	// this is used to prevent unnecessary boxing of value types (bool/int/number etc)
	public struct Amf3Variant
	{
		public Amf3TypeCode Type;
		public int 			IntValue;
		public double 		NumberValue;
		public object 		ObjectValue;	// this handles all other object types

		public object AsObject()
		{
			switch (Type) {
			case Amf3TypeCode.Undefined:
			case Amf3TypeCode.Null:
				return null;
			case Amf3TypeCode.False:
				return (object)false;	// box boolean
			case Amf3TypeCode.True:
				return (object)true;	// box boolean
			case Amf3TypeCode.Integer:
				return (object)IntValue;	// box integer
			case Amf3TypeCode.Number:
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
				return (float)AsNumber();
			case TypeCode.String:
				return AsString();
			case TypeCode.Object:
				return AsObject();
			default:
				throw new InvalidCastException ("Invalid cast to type:" + type.ToString());
			}
		}

	};
}
