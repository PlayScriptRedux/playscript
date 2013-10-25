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

namespace Amf
{
    public class Amf3Parser
    {
		private static readonly Amf.DataConverter conv = Amf.DataConverter.BigEndian;

        private Stream stream;
		private readonly byte[] tempData = new byte[8];

        private List<string> stringTable = new List<string>();
        private List<object> objectTable = new List<object>();
        private List<Amf3ClassDef> traitTable = new List<Amf3ClassDef>();

        public Amf3Parser(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            this.stream = stream;
        }

        public object ReadNextObject()
        {
            int b = stream.ReadByte();

            if (b < 0)
                throw new EndOfStreamException();

            Amf3TypeCode type = (Amf3TypeCode) b;

            switch (type) {
            case Amf3TypeCode.Undefined:
            case Amf3TypeCode.Null:
                return null;

            case Amf3TypeCode.False:
                return false;

            case Amf3TypeCode.True:
                return true;

            case Amf3TypeCode.Integer:
                return ReadInteger();

            case Amf3TypeCode.Number:
                return ReadNumber();

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
		public void ReadNextObject(ref Amf3Variant value)
        {
            int b = stream.ReadByte();

            if (b < 0)
                throw new EndOfStreamException();

			// store type in value
			value.Type = (Amf3TypeCode) b;
			value.ObjectValue = null;

            switch (value.Type) {
            case Amf3TypeCode.Undefined:
            case Amf3TypeCode.Null:
			case Amf3TypeCode.False:
			case Amf3TypeCode.True:
                break;

            case Amf3TypeCode.Integer:
                value.IntValue = ReadInteger();
				break;

            case Amf3TypeCode.Number:
                value.NumberValue = ReadNumber();
				break;

            case Amf3TypeCode.String:
				value.ObjectValue = ReadString();
				break;

            case Amf3TypeCode.Date:
				value.ObjectValue = ReadDate();
				break;

            case Amf3TypeCode.Array:
				value.ObjectValue = ReadArray();
				break;

            case Amf3TypeCode.Object:
				value.ObjectValue = ReadAmf3Object();
				break;

			case Amf3TypeCode.ByteArray:
				value.ObjectValue = ReadByteArray();
				break;

			case Amf3TypeCode.VectorInt:
				value.ObjectValue = ReadVectorInt();
				break;

			case Amf3TypeCode.VectorUInt:
				value.ObjectValue = ReadVectorUInt();
				break;

			case Amf3TypeCode.VectorDouble:
				value.ObjectValue = ReadVectorDouble();
				break;

			case Amf3TypeCode.Dictionary:
				value.ObjectValue = ReadDictionary();
				break;

			case Amf3TypeCode.VectorObject:
				value.ObjectValue = ReadVectorObject();
				break;

			default:
				throw new NotImplementedException("Cannot parse type " + value.Type.ToString());
            }
        }


        public int ReadInteger()
        {
            int integer = 0;
            int seen = 0;

            for (;;) {
                int b = stream.ReadByte();
                if (b < 0)
                    throw new EndOfStreamException();

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
			stream.Read(tempData, 0, sizeof(Int32));
			return conv.GetInt32(tempData, 0);
		}

		public uint ReadUInt32()
		{
			stream.Read(tempData, 0, sizeof(UInt32));
			return conv.GetUInt32(tempData, 0);
		}

        public double ReadNumber()
        {
			stream.Read(tempData, 0, sizeof(double));
            return conv.GetDouble(tempData, 0);
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

            string str = Encoding.UTF8.GetString(stream.Read(num >> 1));

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
			bool isFixed = stream.ReadByteOrThrow() != 0;
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
			bool isFixed = stream.ReadByteOrThrow() != 0;
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
			bool isFixed = stream.ReadByteOrThrow() != 0;
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
			bool isFixed = stream.ReadByteOrThrow() != 0;

			// read object type name
			// this class definition is not known until the first object has been read, but we don't need it here
			string objectTypeName = ReadString();

			// create vector
			IList vector = Amf3ClassDef.CreateObjectVector(objectTypeName, (uint)num, isFixed);
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
			stream.ReadFully(data, 0, num);

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
			bool weakKeys = stream.ReadByteOrThrow()!=0;

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
			traitTable.Add(classDef);
			return classDef;
		}

        public object ReadAmf3Object()
        {
            Amf3Object.Flags flags = (Amf3Object.Flags)ReadInteger();

            if ((flags & Amf3Object.Flags.Inline) == 0) {
                return (Amf3Object)GetTableEntry(objectTable, ((int)flags) >> 1);
            }

            Amf3ClassDef classDef;
            if ((flags & Amf3Object.Flags.InlineClassDef) == 0) {
                classDef = GetTableEntry(traitTable, ((int)flags) >> 2);
            } else {
				classDef = ReadAmf3ClassDef(flags);
            }

			// construct instance of class
			object obj = classDef.CreateInstance();
            objectTable.Add(obj);

            if (classDef.Externalizable) {
				throw new NotSupportedException ();
            }

			// do we have a custom deserializer method?
			if ((classDef.Info.Deserializer != null) || (obj is IAmf3Readable)) {
				// create property reader
				Amf3Reader propReader = AllocateReader();
				// begin reading
				propReader.BeginRead(classDef);
				// we support either a deserializer delegate or a serializer interface
				if (classDef.Info.Deserializer != null) {
					// invoke deserialize delegate on object
					classDef.Info.Deserializer.Invoke(obj, propReader);
				} else {
					// invoke deserialize method on object
					var serializable = (IAmf3Readable)obj;
					serializable.Serialize(propReader);
				}
				// finish reading 
				propReader.EndRead();
				// release reader back to pool
				ReleasePropertyReader(propReader);
			} else if (obj is PlayScript.Expando.ExpandoObject) {
				// read expando object
				var expando = obj as PlayScript.Expando.ExpandoObject;
				foreach (var name in classDef.Properties){
					object value = ReadNextObject();
					expando[name] = value;
				}
			} else {
				// read object properties with reflection
				Amf3Variant value = new Amf3Variant();
				System.Type type = obj.GetType();
				// If we want to keep reflection, we should cache the property setter or field setter
				foreach (var name in classDef.Properties){
					// read value
					ReadNextObject(ref value);

					System.Reflection.PropertyInfo prop = type.GetProperty(name);
					if (prop != null) {
						prop.SetValue(obj, value.AsType(prop.PropertyType), null);
						continue;
					}
					System.Reflection.FieldInfo field = type.GetField(name);
					if (field != null) {
						field.SetValue(obj, value.AsType(field.FieldType));
						continue;
					}
					// Private property or field? Something else?
					throw new Exception("Property not found:" + name);
				}
			}

			// read dynamic properties
            if (classDef.Dynamic) {
				var dc = obj as PlayScript.IDynamicClass;
				if (dc == null) {
					throw new NotSupportedException ("Trying to deserialize class that is not dynamic");
				}

                string key = ReadString();
                while (key != "") {
					dc.__SetDynamicValue(key, ReadNextObject());
                    key = ReadString();
                }
            }
            return obj;
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

		
		private Amf3Reader AllocateReader()
		{
			var reader = mReaderPool;
			if (reader == null) {
				// create new property reader if pool is empty
				return new Amf3Reader(this);
			}

			// use next property reader from pool
			mReaderPool = mReaderPool.NextReader;
			return reader;
		}

		private void ReleasePropertyReader(Amf3Reader reader)
		{
			// add reader to pool
			reader.NextReader = mReaderPool;
			mReaderPool = reader;
		}

		// singly linked list of readers in pool
		private Amf3Reader mReaderPool;
    }

}
