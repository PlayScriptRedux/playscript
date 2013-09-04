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
using System.Collections.Generic;
using System.IO;
using System.Text;
using _root;

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

			case Amf3TypeCode.VectorObject:
			case Amf3TypeCode.Dictionary:
			default:
				throw new NotImplementedException("Cannot parse type " + type.ToString());
            }
        }

		public uint StreamPosition()
		{
			return (uint) stream.Position;
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


        public Amf3Object ReadAmf3Object()
        {
            Amf3Object.Flags flags = (Amf3Object.Flags)ReadInteger();

            if ((flags & Amf3Object.Flags.Inline) == 0) {
                return (Amf3Object)GetTableEntry(objectTable, ((int)flags) >> 1);
            }

            Amf3ClassDef classDef;

            if ((flags & Amf3Object.Flags.InlineClassDef) == 0) {
                classDef = GetTableEntry(traitTable, ((int)flags) >> 2);
            } else {
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

                classDef = new Amf3ClassDef(name, properties, dynamic, externalizable);
                traitTable.Add(classDef);
            }

            Amf3Object obj = new Amf3Object(classDef);
            objectTable.Add(obj);

            if (classDef.Externalizable) {
                obj.Properties["inner"] = ReadNextObject();
                return obj;
            }

            foreach (string i in classDef.Properties) {
                obj.Properties[i] = ReadNextObject();
            }

            if (classDef.Dynamic) {
                string key = ReadString();
                while (key != "") {
                    obj.Properties[key] = ReadNextObject();
                    key = ReadString();
                }
            }

            return obj;
        }
    }
}
