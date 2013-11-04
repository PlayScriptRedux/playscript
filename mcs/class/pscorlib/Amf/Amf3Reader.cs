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
using PlayScript;

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
			// did serializer definition change?
			if (mSerializerClassDef != serializerClassDef) {
				// set new serializer definition
				mNames = serializerClassDef.Properties;
				mSerializerClassDef   = serializerClassDef;

				// build remap table from the serializer properties to stream properties
				mSerializerRemapTable = new int[mNames.Length];
				if (!mStreamClassDef.Equals(serializerClassDef)) {
					// mapping is required, create remap table 
					for (int i=0; i < mSerializerRemapTable.Length; i++) {
						// get stream property index
						int streamIndex = mStreamClassDef.GetPropertyIndex(mNames[i]);
						// store in remap table
						mSerializerRemapTable[i] = streamIndex + 1;
					}
				} else {
					// no remapping required, create direct mapped table
					for (int i=0; i < mSerializerRemapTable.Length; i++) {
						mSerializerRemapTable[i] = i + 1; 
					}
				}
			}

			// begin reading using remap table
			mRemapTable	= mSerializerRemapTable;
			mReadIndex  = 0;
			mReadCount	= mRemapTable.Length;
		}

		// begins the reading of a new object
		// no remapping of properties are done in this mode
		// the properties are read in the order they appear in the stream
		public void ReadObjectHeader()
		{
			// read objects in the order they are in the stream
			ReadObjectHeader(mStreamClassDef);
		}

		// true if done reading
		public bool Done
		{
			get	{return mReadIndex >= mReadCount;}
		}

		// current property index 
		public int Index
		{
			get { return mReadIndex; }
		}

		// current property index 
		public int Count
		{
			get { return mReadCount; }
		}

		// current property name
		public string Name
		{
			get 
			{ 
				return mNames[mReadIndex];
			}
		}

		// current property value
		public Variant Value
		{
			get
			{
				int index = mRemapTable[mReadIndex];
				if (index < 0) return Variant.Undefined; // property does not exist in source stream
				return mValues[index];
			}
		}

		// returns true if property exists
		public bool IsDefined
		{
			get
			{
				int index = mRemapTable[mReadIndex];
				if (index < 0) return false; 		// property does not exist in source stream
				return mValues[index].IsDefined;
			}
		}

		// advances to the next property
		public void NextProperty()
		{
			mReadIndex++;
		}

		//
		// property value readers using out values
		// because they are overloaded they do not require mangling of names (like the methods below: ReadInt, ReadNumber)
		// ReadObjectHeader(classDef) must first be called before reading property values in a serialization function so that the property ordering may be established
		//

		// read next property as boolean
		public void Read(ref bool value)
		{
			int index = mRemapTable[mReadIndex++];
			if (index < 0) return;
			value = mValues[index].AsBoolean();
		}

		// read next property as integer
		public void Read(ref int value)
		{
			int index = mRemapTable[mReadIndex++];
			if (index < 0) return;
			value = mValues[index].AsInt();
		}

		// read next property as unsigned integer
		public void Read(ref uint value)
		{
			int index = mRemapTable[mReadIndex++];
			if (index < 0) return;
			value = mValues[index].AsUInt();
		}

		// read next property as double
		public void Read(ref double value)
		{
			int index = mRemapTable[mReadIndex++];
			if (index < 0) return;
			value = mValues[index].AsNumber();
		}

		// read next property as float
		public void Read(ref float value)
		{
			int index = mRemapTable[mReadIndex++];
			if (index < 0) return;
			value = mValues[index].AsFloat();
		}

		// read next property as string
		public void Read(ref string value)
		{
			int index = mRemapTable[mReadIndex++];
			if (index < 0) return;
			value = mValues[index].AsString();
		}

		// read next property as object
		public void Read(ref object value)
		{
			int index = mRemapTable[mReadIndex++];
			if (index < 0) return;
			value = mValues[index].AsObject();
		}

		// read next property as object
		public void Read<T>(ref T value) where T:class
		{
			int index = mRemapTable[mReadIndex++];
			if (index < 0) return;
			value = mValues[index].AsObject() as T;
		}

		// read next property as variant (any type)
		public void Read(ref Variant value)
		{
			int index = mRemapTable[mReadIndex++];
			if (index < 0) return;
			value = mValues[index];
		}

		//
		// property value readers that return values
		// the reading functions have the type name appended to the end (ie, ReadAsBoolean, ReadAsInt, etc)
		// these are mostly useful when reading values into class properties, which cannot use out-values
		// a default value may be specified here for the cases when the property does not exist in the input stream
		// ReadObjectHeader(classDef) must first be called before reading property values in a serialization function so that the property ordering may be established
		//

		// read next property as boolean
		public bool ReadAsBoolean(bool defaultValue = false)
		{
			int index = mRemapTable[mReadIndex++];
			if (index < 0) return defaultValue;
			return mValues[index].AsBoolean();
		}

		// read next property as integer
		public int ReadAsInt(int defaultValue = 0)
		{
			int index = mRemapTable[mReadIndex++];
			if (index < 0) return defaultValue;
			return mValues[index].AsInt();
		}

		// read next property as unsigned integer
		public uint ReadAsUInt(uint defaultValue = 0)
		{
			int index = mRemapTable[mReadIndex++];
			if (index < 0) return defaultValue;
			return mValues[index].AsUInt();
		}

		// read next property as number
		public double ReadAsNumber(double defaultValue = 0.0)
		{
			int index = mRemapTable[mReadIndex++];
			if (index < 0) return defaultValue;
			return mValues[index].AsNumber();
		}

		// read next property as float
		public float ReadAsFloat(float defaultValue = 0.0f)
		{
			int index = mRemapTable[mReadIndex++];
			if (index < 0) return defaultValue;
			return mValues[index].AsFloat();
		}

		// read next property as string
		public string ReadAsString(string defaultValue = null)
		{
			int index = mRemapTable[mReadIndex++];
			if (index < 0) return defaultValue;
			return mValues[index].AsString();
		}

		// read next property as object
		public object ReadAsObject(object defaultValue = null)
		{
			int index = mRemapTable[mReadIndex++];
			if (index < 0) return null;
			return mValues[index].AsObject();
		}

		// read next property as T
		public T ReadAs<T>(T defaultValue = null) where T:class
		{
			int index = mRemapTable[mReadIndex++];
			if (index < 0) return defaultValue;
			return mValues[index].AsObject() as T;
		}

		// read next property as variant
		public Variant ReadAsVariant()
		{
			int index = mRemapTable[mReadIndex++];
			if (index < 0) return Variant.Undefined;
			return mValues[index];
		}


		#region Internal
		internal Amf3Reader NextReader;

		internal Amf3Reader(Amf3ClassDef streamClassDef)
		{
			mStreamClassDef     = streamClassDef;
			mValues             = new Variant[streamClassDef.Properties.Length + 1];  // +1 for undefined property 0
			mNames              = streamClassDef.Properties;
		}

		internal void BeginRead(Amf3Parser parser)
		{
			mParser = parser;

			// read all property values from amf stream
			// these are in the order specified by the amf stream classDef and must be remapped for the deserializer for each propery read
			// note that mValues[0] is reserved for undefined, any missing properties will be mapped there
			int count = mStreamClassDef.Properties.Length;
			for (int i=0; i < count; i++){
				mParser.ReadNextObject(ref mValues[i + 1]);
			}

			// force value[0] to undefined to elegantly handle missing properties as index 0
			mValues[0]	= Variant.Undefined;
		}

		internal void EndRead()
		{
			// reset read state
			mParser             = null;
			mRemapTable			= null;
			mReadIndex          = -1;
			mReadCount			= 0;
		}

		#endregion

		#region Private
		#pragma warning disable 414
		private Amf3Parser		mParser;				// parser
		private Amf3ClassDef	mStreamClassDef;		// class definition for amf stream we are reading
		private Amf3ClassDef	mSerializerClassDef;	// class definition for deserializer method being called
		private int[] 			mSerializerRemapTable;	// remap table for the last serializer used
		private string[] 		mNames;					// property name array
		private Variant[]		mValues;				// property value array, one for each value from stream with [0] being undefined
		private int 			mReadIndex;				// property serializer read index
		private int 			mReadCount;				// number of properties available
		private int[] 			mRemapTable;  			// property remap table that is accessed for each Read() (maps read index to value table)
		#pragma warning restore 414
		#endregion
	}
}
