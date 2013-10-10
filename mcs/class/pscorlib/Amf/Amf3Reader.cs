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
	// this class allows for the reading of object properties from an AMF stream
	// the deserialization function must first provide an Amf3ClassDef by calling ReadObjectHeader() that describes the desired property ordering needed by the reading code
	// the function must then read all properties in that order using the Read(out value) or the ReadInt() methods
	// the property values will be remapped from the ordering of the AMF stream to the ordering expected by the deserialization code
	public sealed class Amf3Reader 
	{
		// begins the reading of a new object
		// the classdef passed in here will define the ordering of the subsequent property reads
		public void ReadObjectHeader(Amf3ClassDef serializerClassDef)
		{
			// store serializer class definition
			mSerializerClassDef = serializerClassDef;

			// get property remap table to use
			// this maps properties from the serializer classDef to the stream mClassDef
			mRemapTable = mStreamClassDef.GetRemapTable(serializerClassDef);
		}

		//
		// property value readers using out values
		// because they are overloaded they do not require mangling of names (like the methods below: ReadInt, ReadNumber)
		// ReadObjectHeader(classDef) must first be called before reading property values in a serialization function so that the property ordering may be established
		//

		// read next property as boolean
		public void Read(out bool value)
		{
			int index = mRemapTable[mReadIndex++];
			value = mValues[index].AsBoolean();
		}

		// read next property as integer
		public void Read(out int value)
		{
			int index = mRemapTable[mReadIndex++];
			value = mValues[index].AsInt();
		}

		// read next property as unsigned integer
		public void Read(out uint value)
		{
			int index = mRemapTable[mReadIndex++];
			value = mValues[index].AsUInt();
		}

		// read next property as string
		public void Read(out double value)
		{
			int index = mRemapTable[mReadIndex++];
			value = mValues[index].AsNumber();
		}

		// read next property as string
		public void Read(out string value)
		{
			int index = mRemapTable[mReadIndex++];
			value = mValues[index].AsString();
		}

		// read next property as object
		public void Read(out object value)
		{
			int index = mRemapTable[mReadIndex++];
			value = mValues[index].AsObject();
		}

		// read next property as object
		public void Read<T>(out T value) where T:class
		{
			int index = mRemapTable[mReadIndex++];
			value = mValues[index].AsObject() as T;
		}

		//
		// property value readers that return values
		// the reading functions have the type name appended to the end (ie, ReadAsBoolean, ReadAsInt, etc)
		// these are mostly useful when reading values into class properties, which cannot use out-values
		// ReadObjectHeader(classDef) must first be called before reading property values in a serialization function so that the property ordering may be established
		//

		// read next property as boolean
		public bool ReadAsBoolean()
		{
			int index = mRemapTable[mReadIndex++];
			return mValues[index].AsBoolean();
		}

		// read next property as integer
		public int ReadAsInt()
		{
			int index = mRemapTable[mReadIndex++];
			return mValues[index].AsInt();
		}

		// read next property as unsigned integer
		public uint ReadAsUInt()
		{
			int index = mRemapTable[mReadIndex++];
			return mValues[index].AsUInt();
		}

		// read next property as number
		public double ReadAsNumber()
		{
			int index = mRemapTable[mReadIndex++];
			return mValues[index].AsNumber();
		}

		// read next property as string
		public string ReadAsString()
		{
			int index = mRemapTable[mReadIndex++];
			return mValues[index].AsString();
		}

		// read next property as object
		public object ReadAsObject()
		{
			int index = mRemapTable[mReadIndex++];
			return mValues[index].AsObject();
		}

		// read next property as T
		public T ReadAs<T>() where T:class
		{
			int index = mRemapTable[mReadIndex++];
			return mValues[index].AsObject() as T;
		}

		#region Internal
		internal Amf3Reader NextReader;

		internal Amf3Reader(Amf3Parser parser)
		{
			mParser = parser;
		}

		internal void BeginRead(Amf3ClassDef streamClassDef)
		{
			// store stream class definition
			mStreamClassDef     = streamClassDef;
			mSerializerClassDef = null;
			mRemapTable         = null;
			mReadIndex          = 0;

			// read all property values from amf stream
			// these are in the order specified by the amf stream classDef and must be remapped for the deserializer for each propery read
			// note that mValues[0] is reserved for undefined, any missing properties will be mapped there

			// resize value array if we need to 
			int count = mStreamClassDef.Properties.Length + 1; // +1 for undefined property 0
			if (mValues == null || mValues.Length < count) {
				mValues = new Amf3Variant[count];
			}

			// reset value[0] to undefined to elegantly handle missing properties
			mValues[0].Type        = Amf3TypeCode.Undefined;
			mValues[0].ObjectValue = null;

			// read all property values
			for (int i=1; i < count; i++){
				mParser.ReadNextObject(ref mValues[i]);
			}
		}

		internal void EndRead()
		{
			// empty
		}

		#endregion

		#region Private
		#pragma warning disable 414
		private Amf3Parser		mParser;				// parser
		private Amf3ClassDef	mStreamClassDef;		// class definition for amf stream we are reading
		private Amf3ClassDef	mSerializerClassDef;	// class definition for deserializer method being called
		private Amf3Variant[]	mValues;				// property value array, one for each value from stream with [0] being undefined
		private int 			mReadIndex;				// property serializer read index
		private int[] 			mRemapTable;  			// property remap table (serializer -> stream)
		#pragma warning restore 414
		#endregion
	}
}
