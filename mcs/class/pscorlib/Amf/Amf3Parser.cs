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

namespace Amf
{
    public class Amf3Parser
    {
		private static readonly Amf.DataConverter conv = Amf.DataConverter.BigEndian;
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private Stream stream;

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
            }

            throw new InvalidOperationException("Cannot parse type " + type.ToString());
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

        public double ReadNumber()
        {
            return conv.GetDouble(stream.Read(8), 0);
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

        public DateTime ReadDate()
        {
            int num = ReadInteger();

            if ((num & 1) == 0) {
                return (DateTime)GetTableEntry(objectTable, num >> 1);
            }

            double val = ReadNumber();

            // Ticks are 100 nanoseconds (conversion is 1000000 / 100)
            long ticks = checked(Epoch.Ticks + ((long)val * 10000));

            DateTime date = new DateTime(ticks, DateTimeKind.Utc);

            objectTable.Add(date);

            return date;
        }

        public Amf3Array ReadArray()
        {
            int num = ReadInteger();

            if ((num & 1) == 0) {
                return (Amf3Array)GetTableEntry(objectTable, num >> 1);
            }

            num >>= 1;

            Amf3Array array = new Amf3Array();

            objectTable.Add(array);

            string key = ReadString();
            while (key != "") {
                object value = ReadNextObject();
                array.AssociativeArray[key] = value;

                key = ReadString();
            }

            while (num-- > 0) {
                array.DenseArray.Add(ReadNextObject());
            }

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
