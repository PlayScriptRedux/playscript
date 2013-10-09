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
		// out-based readers (unnamed)
		// because they are unnamed they require that a class definition be supplied via ReadObjectHeader
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
		// out-based readers with property names
		//

		// read next property as boolean
		public void Read(string name, out bool value)
		{
			int index = RemapProperty(name);
			value = mValues[index].AsBoolean();
		}

		// read next property as integer
		public void Read(string name, out int value)
		{
			int index = RemapProperty(name);
			value = mValues[index].AsInt();
		}

		// read next property as unsigned integer
		public void Read(string name, out uint value)
		{
			int index = RemapProperty(name);
			value = mValues[index].AsUInt();
		}

		// read next property as string
		public void Read(string name, out double value)
		{
			int index = RemapProperty(name);
			value = mValues[index].AsNumber();
		}

		// read next property as string
		public void Read(string name, out string value)
		{
			int index = RemapProperty(name);
			value = mValues[index].AsString();
		}

		// read next property as object
		public void Read(string name, out object value)
		{
			int index = RemapProperty(name);
			value = mValues[index].AsObject();
		}

		// read next property as object
		public void Read<T>(string name, out T value) where T:class
		{
			int index = RemapProperty(name);
			value = mValues[index].AsObject() as T;
		}

		//
		// returning readers without property names
		// because they are unnamed they require that a class definition be supplied via ReadObjectHeader
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

		//
		// returning readers with property names
		//

		// read next property as boolean
		public bool ReadAsBoolean(string name)
		{
			int index = RemapProperty(name);
			return mValues[index].AsBoolean();
		}

		// read next property as integer
		public int ReadAsInt(string name)
		{
			int index = RemapProperty(name);
			return mValues[index].AsInt();
		}

		// read next property as unsigned integer
		public uint ReadAsUInt(string name)
		{
			int index = RemapProperty(name);
			return mValues[index].AsUInt();
		}

		// read next property as number
		public double ReadAsNumber(string name)
		{
			int index = RemapProperty(name);
			return mValues[index].AsNumber();
		}

		// read next property as string
		public string ReadAsString(string name)
		{
			int index = RemapProperty(name);
			return mValues[index].AsString();
		}

		// read next property as object
		public object ReadAsObject(string name)
		{
			int index = RemapProperty(name);
			return mValues[index].AsObject();
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
			// these are in the order specified by the amf stream classDef and must be remapped for the deserializer
			// note that mValues[0] is reserved for undefined, in missing properties will be mapped there

			// resize value array if we need to 
			int count = mStreamClassDef.Properties.Length + 1; // +1 for undefined property 0
			if (mValues == null || mValues.Length < count) {
				mValues = new Amf3Variant[count];
			}

			// reset value[0] to undefined to elegantly handle missing properties
			mValues[0].Type        = Amf3TypeCode.Undefined;
			mValues[0].ObjectValue = null;

			// read all objects
			for (int i=1; i < count; i++){
				mParser.ReadNextObject(ref mValues[i]);
			}
		}

		internal void EndRead()
		{
			if (mSerializerPropertyNames != null)
			{
				// dynamically generate serializer class definition here based on the order that the properties were read
				mStreamClassDef.Info.DeserializerClassDef = new Amf3ClassDef(mStreamClassDef.Name, mSerializerPropertyNames.ToArray());
				mSerializerPropertyNames = null;
			}
		}

		#endregion

		#region Private
		private int RemapProperty(string name)
		{
			if (mSerializerClassDef != null)  { 
				// if the property values are already remapped, then return next remapped index
				return mRemapTable[mReadIndex++];
			}

			// if we dont have a serializer class definition then we cant read a named property quickly...

			// is there a autogenerated deserializer class definition we can use?
			if (mStreamClassDef.Info.DeserializerClassDef != null) {
				// use it to create remap table...
				ReadObjectHeader(mStreamClassDef.Info.DeserializerClassDef);
				// return next remapped index
				return mRemapTable[mReadIndex++];
			} 
				
			// fallback to slow property lookup...
			// this only happens once though, it will not happen after the remap table has been built

			// add name to internal list for autogenerated deserializer class creation
			if (mSerializerPropertyNames == null) {
				mSerializerPropertyNames = new List<string>();
			}
			mSerializerPropertyNames.Add(name);

			// lookup index of property
			int propIndex = mStreamClassDef.GetPropertyIndex(name);
			if (propIndex < 0) {
				// property is not defined
				Console.WriteLine("Warning: could not find AMF property {0} for class {1}", name, mStreamClassDef.Name);
			}
			return propIndex + 1; // +1 for undefined property 0
		}

		private Amf3Parser		mParser;				// parser
		private Amf3ClassDef	mStreamClassDef;		// class definition for amf stream we are reading
		private Amf3ClassDef	mSerializerClassDef;	// class definition for deserializer method being called
		private Amf3Variant[]	mValues;				// property value array, one for each value from stream
		private int 			mReadIndex;				// property seralizer read index
		private int[] 			mRemapTable;  			// property remap table (serializer -> stream)
		private List<string>	mSerializerPropertyNames;
		#endregion
	}
}
