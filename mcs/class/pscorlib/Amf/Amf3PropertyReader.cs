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
	public sealed class Amf3PropertyReader
	{
		// read next property
		public void Read<T>(out T o)
		{
			throw new NotImplementedException();
		}

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
		internal Amf3PropertyReader NextReader;

		internal void BeginReadProperties(Amf3Parser parser, Amf3ClassDef classDef, string[] propertyOrder)
		{
			// store class definition
			mClassDef   = classDef;

			var names = classDef.Properties;

			// resize value array if we need to 
			if (mValues == null || mValues.Length < names.Length) {
				mValues = new Amf3Variant[names.Length];
			}

			// were we provided with a property ordering?
			if ((classDef.PropertyRemapTable == null) && (propertyOrder != null)) {
				// create remap table from ordering
				var table = new int[names.Length];
				for (int i=0; i < propertyOrder.Length; i++) {
					// get index of property
					int remap = classDef.GetPropertyIndex(propertyOrder[i]);
					if (remap < 0) {
						throw new Exception("Could not find property in class definition: " + propertyOrder[i]);
					}
					table[i] = remap;
				}
				// set remap table
				classDef.PropertyRemapTable = table;
			}

			// does class have a remap table?
			if (classDef.PropertyRemapTable == null) { 
				// read all property values (without remapping)
				for (int i=0; i < names.Length; i++){
					parser.ReadNextObject(ref mValues[i]);
				}

				// properties have not been remapped
				mRemapped = false;

				// create property remap table
				mRemapTable = new int[mClassDef.Properties.Length];
			} else {
				// get property remap table from class
				int[] table = classDef.PropertyRemapTable;

				// read all property values (with remapping)
				for (int i=0; i < names.Length; i++){
					parser.ReadNextObject(ref mValues[table[i]]);
				}

				// properties have been remapped
				mRemapped = true;

				// create property remap table
				mRemapTable = null;
			}

			// reset read index
			mReadIndex    = 0;
		}

		internal void EndReadProperties()
		{
			// set remap table into class definition
			if (mRemapTable != null) {
				mClassDef.PropertyRemapTable = mRemapTable;
			}
		}

		#endregion

		#region Private
		private int RemapProperty(string name)
		{
			if (mRemapped)  { 
				// if the property values are already remapped, then return next sequential index
				return mReadIndex++;
			}

			// fallback to slow property lookup...
			// this only happens once though, it will not happen after the remap table has been built

			// lookup index of property
			int propIndex = mClassDef.GetPropertyIndex(name);
			if (propIndex < 0) {
				throw new Exception("Could not find property in class definition: " + name);
			}

			// update remap table
			mRemapTable[propIndex] = mReadIndex++;

			// return index to use
			return propIndex;
		}

		private Amf3ClassDef  mClassDef;	// class definition we are reading
		private Amf3Variant[] mValues;		// property value array
		private int 		  mReadIndex;	// read property index
		private bool 		  mRemapped;    // do properties need remapping?
		private int[] 		  mRemapTable;  // remap table being built
		#endregion
	}
}
