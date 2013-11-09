// Amf3Parser.cs
//
// Copyright (c) 2009 Chris Howie
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using _root;
using System.Reflection;
using PlayScript;
using System.Runtime.CompilerServices;

namespace Amf
{
    public class Amf3Parser
    {
		private static readonly Amf.DataConverter conv = Amf.DataConverter.BigEndian;

		private byte[] 	mData;
		private int 	mPosition;
		private int		mLength;

        private List<string> stringTable = new List<string>();
        private List<object> objectTable = new List<object>();
        private List<Amf3ClassDef> traitTable = new List<Amf3ClassDef>();

		private static readonly object sBoolTrue = (object)true;
		private static readonly object sBoolFalse = (object)false;
		private static readonly object sIntNegOne = (object)(int)-1;
		private static readonly object sIntZero = (object)(int)0;
		private static readonly object sIntOne = (object)(int)1;
		private static readonly object sNumberZero = (object)(double)0.0;
		private static readonly object sNumberOne = (object)(double)1.0;

		public IAmf3Serializer DefaultSerializer;
		public IAmf3Serializer OverrideSerializer;

		// if true, all unregistered class aliases will use Expandos, if false they will use Amf3Objects
		// this can be overridden by changing the DefaultSerializer
		public static bool UseExpandoAsDefault = true;

        public Amf3Parser(Stream stream, bool autoSetCapacity = true)
			: this(stream.Read((int)stream.Length), 0, (int)stream.Length, autoSetCapacity)	// read stream into byte array
        {
		}

		public Amf3Parser(byte[] data, int offset, int length, bool autoSetCapacity = true)
		{
			// set data source
			mData   = data;
			mPosition = offset;
			mLength = length;

			// set default serializer
			if (UseExpandoAsDefault) {
				this.DefaultSerializer = new ExpandoSerializer();
			} else {
				this.DefaultSerializer = new Amf3Object.Serializer();
			}

			if (autoSetCapacity) 
			{
				AutoSetCapacity();
			}
		}

		public void AutoSetCapacity()
		{
			// use some simple tests to set capacity automatically
			if (mLength > 1024 * 1024)
			{
				// large file
				SetCapacity(32 * 1024, 80 * 1024, 1024);
			} 
			else if (mLength > 128 * 1024)
			{
				// medium file
				SetCapacity(2048, 16 * 1024, 128);
			} 
			else 
			{
				// small file
				SetCapacity(1024, 4 * 1024, 64);
			}
		}

		public void SetCapacity(int stringTableCapacity, int objectTableCapacity, int traitTableCapacity)
		{
			stringTable.Capacity = stringTableCapacity;
			objectTable.Capacity = objectTableCapacity;
			traitTable.Capacity = traitTableCapacity;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private byte ReadByte()
		{
			if (mPosition >= mLength) {
				throw new EndOfStreamException();
			}

			return mData[mPosition++];
		}


        public object ReadNextObject()
        {
            byte b = ReadByte();

            Amf3TypeCode type = (Amf3TypeCode) b;

            switch (type) {
            case Amf3TypeCode.Undefined:
            case Amf3TypeCode.Null:
                return null;

            case Amf3TypeCode.False:
                return sBoolFalse;

            case Amf3TypeCode.True:
                return sBoolTrue;

            case Amf3TypeCode.Integer:
				{
					int i = ReadInteger();
					if (i == 0) return sIntZero;
					if (i == 1) return sIntOne;
					if (i ==-1) return sIntNegOne;
					return (object)i;
				}

            case Amf3TypeCode.Number:
				{
					double d = ReadNumber();
					if (d == 0.0) return sNumberZero;
					if (d == 1.0) return sNumberOne;
					return (object)d;
				}

            case Amf3TypeCode.String:
                return ReadString();

            case Amf3TypeCode.Date:
                return ReadDate();

            case Amf3TypeCode.Array:
                return ReadArray();

            case Amf3TypeCode.Object:
                return ReadAmf3Object();

			case Amf3TypeCode.ByteArray:
				return ReadByteArray();

			case Amf3TypeCode.VectorInt:
				return ReadVectorInt();

			case Amf3TypeCode.VectorUInt:
				return ReadVectorUInt();

			case Amf3TypeCode.VectorDouble:
				return ReadVectorDouble();

			case Amf3TypeCode.Dictionary:
				return ReadDictionary();

			case Amf3TypeCode.VectorObject:
				return ReadVectorObject();

			default:
				throw new NotImplementedException("Cannot parse type " + type.ToString());
            }
        }

		// this read the next object into a value structure
		// this avoids unnecessary boxing/unboxing of value types and speeds up deserialization
		public void ReadNextObject(ref Variant value)
        {
			byte b = ReadByte();
			Amf3TypeCode type = (Amf3TypeCode) b;
            switch (type) {
            case Amf3TypeCode.Undefined:
				value = Variant.Undefined;
				return;
            case Amf3TypeCode.Null:
				value = Variant.Null;
				return;
			case Amf3TypeCode.False:
				value = false;
				return;
			case Amf3TypeCode.True:
				value = true;
				return;

            case Amf3TypeCode.Integer:
                value = ReadInteger();
				break;

            case Amf3TypeCode.Number:
                value = ReadNumber();
				break;

            case Amf3TypeCode.String:
				value = ReadString();
				break;

            case Amf3TypeCode.Date:
				value = new Variant(ReadDate());
				break;

            case Amf3TypeCode.Array:
				value = new Variant(ReadArray());
				break;

            case Amf3TypeCode.Object:
				value = new Variant(ReadAmf3Object());
				break;

			case Amf3TypeCode.ByteArray:
				value = new Variant(ReadByteArray());
				break;

			case Amf3TypeCode.VectorInt:
				value = new Variant(ReadVectorInt());
				break;

			case Amf3TypeCode.VectorUInt:
				value = new Variant(ReadVectorUInt());
				break;

			case Amf3TypeCode.VectorDouble:
				value = new Variant(ReadVectorDouble());
				break;

			case Amf3TypeCode.Dictionary:
				value = new Variant(ReadDictionary());
				break;

			case Amf3TypeCode.VectorObject:
				value = new Variant(ReadVectorObject());
				break;

			default:
				throw new NotImplementedException("Cannot parse type " + type.ToString());
            }
        }


        public int ReadInteger()
        {
            int integer = 0;
            int seen = 0;

            for (;;) {
                int b = (int)ReadByte();

                if (seen == 3) {
                    integer = (integer << 8) | b;
                    break;
                }

                integer = (integer << 7) | (b & 0x7f);

                if ((b & 0x80) == 0x80) {
                    seen++;
                } else {
                    break;
                }
            }

            if (integer > (int.MaxValue >> 3))
                integer -= (1 << 29);

            return integer;
        }

		public int ReadInt32()
		{
			int value = conv.GetInt32(mData, mPosition);
			mPosition += sizeof(Int32);
			return value;
		}

		public uint ReadUInt32()
		{
			uint value = conv.GetUInt32(mData, mPosition);
			mPosition += sizeof(UInt32);
			return value;
		}

        public double ReadNumber()
        {
			double value = conv.GetDouble(mData, mPosition);
			mPosition += sizeof(double);
			return value;
        }

        private static T GetTableEntry<T>(IList<T> table, int index)
        {
            if (table == null)
                throw new InvalidOperationException("Cannot extract references with no reference table");

            if ((table.Count - 1) < index)
                throw new InvalidOperationException("Reference table does not contain requested index " +
                    index + " (max index " + (table.Count - 1) + ")");

            return table[index];
        }

        public string ReadString()
        {
            int num = ReadInteger();

            if ((num & 1) == 0) {
                return GetTableEntry(stringTable, num >> 1);
            }

			int count = num >> 1;
            string str = Encoding.UTF8.GetString(mData, mPosition, count);
			mPosition += count;

            if (str != "")
                stringTable.Add(str);

            return str;
        }

        public _root.Date ReadDate()
        {
            int num = ReadInteger();

            if ((num & 1) == 0) {
				return (_root.Date)GetTableEntry(objectTable, num >> 1);
            }

            double val = ReadNumber();

			var date = new _root.Date();
			date.setTime(val);

            objectTable.Add(date);
            return date;
        }

        public _root.Array ReadArray()
        {
            int num = ReadInteger();

            if ((num & 1) == 0) {
				return (_root.Array)GetTableEntry(objectTable, num >> 1);
            }

            num >>= 1;

			var array = new _root.Array(num);

            objectTable.Add(array);

            string key = ReadString();
            while (key != "") {
                object value = ReadNextObject();
				array[key] = value;

                key = ReadString();
            }

			for (int i=0; i < num; i++) {
                array[i] = ReadNextObject();
            }

            return array;
        }

		public Vector<double> ReadVectorDouble()
		{
			int num = ReadInteger();

			if ((num & 1) == 0) {
				return (Vector<double>)GetTableEntry(objectTable, num >> 1);
			}

			num >>= 1;
			bool isFixed = ReadByte() != 0;
			var vector = new Vector<double>((uint)num, isFixed);

			objectTable.Add(vector);

			// read all values
			for (int i=0; i < num; i++) {
				vector[i] = ReadNumber();
			}

			return vector;
		}

		public Vector<int> ReadVectorInt()
		{
			int num = ReadInteger();

			if ((num & 1) == 0) {
				return (Vector<int>)GetTableEntry(objectTable, num >> 1);
			}

			num >>= 1;
			bool isFixed = ReadByte() != 0;
			var vector = new Vector<int>((uint)num, isFixed);

			objectTable.Add(vector);

			// read all values
			for (int i=0; i < num; i++) {
				vector[i] = ReadInt32();
			}

			return vector;
		}

		public Vector<uint> ReadVectorUInt()
		{
			int num = ReadInteger();

			if ((num & 1) == 0) {
				return (Vector<uint>)GetTableEntry(objectTable, num >> 1);
			}

			num >>= 1;
			bool isFixed = ReadByte() != 0;
			var vector = new Vector<uint>((uint)num, isFixed);

			objectTable.Add(vector);

			// read all values
			for (int i=0; i < num; i++) {
				vector[i] = ReadUInt32();
			}

			return vector;
		}

		public object ReadVectorObject()
		{
			int num = ReadInteger();
			if ((num & 1) == 0) {
				return GetTableEntry(objectTable, num >> 1);
			}

			num >>= 1;
			bool isFixed = ReadByte() != 0;

			// read object type name
			// this class definition is not known until the first object has been read, but we don't need it here
			string objectTypeName = ReadString();

			// get serializer for class alias
			IAmf3Serializer serializer = GetSerializerFromAlias(objectTypeName);

			IList vector;
			if (serializer != null) {
				// create vector using serializer
				vector = serializer.NewVector((uint)num, isFixed);
			} else {
				// create a new vector of dynamic
				vector = new _root.Vector<dynamic>((uint)num, isFixed);
			}

			objectTable.Add(vector);

			// read all values
			for (int i=0; i < num; i++) {
				vector[i] = ReadNextObject();
			}

			return vector;
		}

		public flash.utils.ByteArray ReadByteArray()
		{
			int num = ReadInteger();

			if ((num & 1) == 0) {
				return (flash.utils.ByteArray)GetTableEntry(objectTable, num >> 1);
			}

			num >>= 1;

			// read all data into byte array
			byte[] data = new byte[num];
			for (int i=0; i < num; i++) {
				data[i] = ReadByte();
			}

			var array = flash.utils.ByteArray.fromArray(data);
			objectTable.Add(array);
			return array;
		}

		public flash.utils.Dictionary ReadDictionary()
		{
			// get entry count
			int num = ReadInteger();
			if ((num & 1) == 0) {
				return (flash.utils.Dictionary)GetTableEntry(objectTable, num >> 1);
			}
			num >>= 1;

			// weak keys?
			bool weakKeys = ReadByte()!=0;

			// create dictionary
			var dict = new flash.utils.Dictionary(weakKeys);
			objectTable.Add(dict);

			// read entries
			for (int i=0; i < num; i++) {
				// read key
				object key = ReadNextObject();
				// read value
				object value = ReadNextObject();
				// store in dictionary
				dict[key] = value;
			}
			return dict;
		}

		private Amf3ClassDef ReadAmf3ClassDef(Amf3Object.Flags flags)
		{
			bool externalizable = ((flags & Amf3Object.Flags.Externalizable) != 0);
			bool dynamic = ((flags & Amf3Object.Flags.Dynamic) != 0);
			string name = ReadString();

			if (externalizable && dynamic)
				throw new InvalidOperationException("Serialized objects cannot be both dynamic and externalizable");

			List<string> properties = new List<string>();

			int members = ((int) flags) >> 4;

			for (int i = 0; i < members; i++) {
				properties.Add(ReadString());
			}

			Amf3ClassDef classDef = new Amf3ClassDef(name, properties.ToArray(), dynamic, externalizable);

			// lookup serializer to use for this class definition
			classDef.Serializer = GetSerializerFromAlias(classDef.Name);

			traitTable.Add(classDef);
			return classDef;
		}

		// gets serializer for a class alias string
		private IAmf3Serializer GetSerializerFromAlias(string alias)
		{
			IAmf3Serializer serializer = Amf3ClassDef.GetSerializerFromAlias(alias);
			if (serializer == null) {
				var type = Amf3ClassDef.GetTypeFromAlias(alias);
				if (type != null) {
					// create reflection serializer
					serializer = new ReflectionSerializer(alias, type, true, false);
					// automatically register it
					Amf3ClassDef.RegisterSerializer(alias, serializer);
				} 
			}
			return serializer;
		}

        public object ReadAmf3Object()
        {
            Amf3Object.Flags flags = (Amf3Object.Flags)ReadInteger();

            if ((flags & Amf3Object.Flags.Inline) == 0) {
                return GetTableEntry(objectTable, ((int)flags) >> 1);
            }

            Amf3ClassDef classDef;
            if ((flags & Amf3Object.Flags.InlineClassDef) == 0) {
                classDef = GetTableEntry(traitTable, ((int)flags) >> 2);
            } else {
				classDef = ReadAmf3ClassDef(flags);
            }

			// get the serializer to use (start with the override serializer)
			IAmf3Serializer serializer = OverrideSerializer;
			if (serializer == null) {
				// no override? get serializer from class definition
				serializer = classDef.Serializer;
				if (serializer == null) {
					// no serializer for class definition? use default serializer 
					serializer = DefaultSerializer;
				}
			}

			// fast path the Amf3Object serializer
			if (serializer is Amf3Object.Serializer) {
				var obj = new Amf3Object(classDef);

				// add to object table
				objectTable.Add(obj);

				// read all properties into object
				int count = classDef.Properties.Length;
				for(int i=0; i < count; i++) {
					ReadNextObject(ref obj.Values[i]);
				}
				
				// read dynamic properties
				if (classDef.Dynamic) {
					string key = ReadString();
					while (key != "") {
						var value  = ReadNextObject();
						obj.SetPropertyValueAsObject(key, value);
						key = ReadString();
					}
				}
				return obj;
			} else {
				// create object instance using serializer
				object obj = serializer.NewInstance(classDef);

				// add to object table
	            objectTable.Add(obj);

				// begin property remapping
				var reader = classDef.CreatePropertyReader();
				reader.BeginRead(this);
				// read object using serializer
				serializer.ReadObject(reader, obj);
				// end property remapping
				classDef.ReleasePropertyReader(reader);

				// read dynamic properties
				if (classDef.Dynamic) {
					var dc = obj as PlayScript.IDynamicClass;
					string key = ReadString();
					while (key != "") {
						var value  = ReadNextObject();
						if (dc != null) {
							dc.__SetDynamicValue(key, value);
						}
						key = ReadString();
					}
				}
				return obj;
			}
        }

		// returns all the class definitions that have been found while parsing
		public Amf3ClassDef[] GetClassDefinitions()
		{
			return traitTable.ToArray();
		}

		// returns all the objects that have been found while parsing
		public object[] GetObjectTable()
		{
			return objectTable.ToArray();
		}
    }

}
