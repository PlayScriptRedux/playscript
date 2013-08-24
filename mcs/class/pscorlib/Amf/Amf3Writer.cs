// Amf3Writer.cs
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
using System.Linq;
using System.Text;

namespace Amf
{
    public class Amf3Writer
    {
		private static readonly Amf.DataConverter conv = Amf.DataConverter.BigEndian;

        public Stream Stream { get; private set; }

        private Dictionary<string, int> stringTable = new Dictionary<string, int>();

        private Dictionary<object, int> objectTable =
            new Dictionary<object, int>(new ReferenceEqualityComparer<object>());

        private Dictionary<Amf3ClassDef, int> classDefTable =
            new Dictionary<Amf3ClassDef, int>();

        private const int amfIntMaxValue = int.MaxValue >> 3;
        private const int amfIntMinValue = int.MinValue >> 3;

        public Amf3Writer(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            Stream = stream;
        }

        private bool CheckObjectTable(object obj)
        {
            int index;
            if (objectTable.TryGetValue(obj, out index)) {
                TypelessWrite(index << 1);
                return true;
            }

            return false;
        }

        private void Write(Amf3TypeCode type)
        {
            Stream.WriteByte((byte)type);
        }

        public void Write(object obj)
        {
            if (obj == null) {
                Write(Amf3TypeCode.Null);
                return;
            }

            if (obj is IAmf3Serializable) {
                ((IAmf3Serializable)obj).Serialize(this);
                return;
            }

            if (obj is bool) {
                Write((bool)obj);
                return;
            }

            if (obj is float ||
                    obj is double) {
                Write(Convert.ToDouble(obj));
                return;
            }

            if (obj is int ||
                    obj is byte ||
                    obj is sbyte ||
                    obj is short ||
                    obj is ushort ||
                    obj is uint) {
                Write(Convert.ToInt32(obj));
                return;
            }

            if (obj is string) {
                Write((string)obj);
                return;
            }

			if (obj is _root.Date) {
				Write((_root.Date)obj);
				return;
			}

            if (obj is _root.Array) {
				Write((_root.Array)obj);
                return;
            }

            if (obj is IList) {
                Write((IList)obj);
                return;
            }

            if (obj is Amf3Object) {
                Write((Amf3Object)obj);
                return;
            }

            throw new ArgumentException("Cannot serialize object of type " + obj.GetType().FullName);
        }

        public void Write(bool boolean)
        {
            Write(boolean ? Amf3TypeCode.True : Amf3TypeCode.False);
        }

        public void Write(byte integer)
        {
            Write((int)integer);
        }

        public void Write(sbyte integer)
        {
            Write((int)integer);
        }

        public void Write(short integer)
        {
            Write((int)integer);
        }

        public void Write(ushort integer)
        {
            Write((int)integer);
        }

        public void Write(uint integer)
        {
            Write(checked((int)integer));
        }

        public void Write(int integer)
        {
            Write(Amf3TypeCode.Integer);
            TypelessWrite(integer);
        }

        public void Write(double number)
        {
            Write(Amf3TypeCode.Number);
            TypelessWrite(number);
        }

        public void Write(string str)
        {
            Write(Amf3TypeCode.String);
            TypelessWrite(str);
        }

        public void Write(_root.Date date)
        {
            Write(Amf3TypeCode.Date);
            TypelessWrite(date);
        }

        public void Write(_root.Array array)
        {
            Write(Amf3TypeCode.Array);
            TypelessWrite(array);
        }

        public void Write(IList list)
        {
            Write(Amf3TypeCode.Array);
            TypelessWrite(list);
        }

        public void Write(Amf3Object obj)
        {
            Write(Amf3TypeCode.Object);
            TypelessWrite(obj);
        }

        public void TypelessWrite(int integer)
        {
            if (integer > amfIntMaxValue || integer < amfIntMinValue)
                throw new ArgumentOutOfRangeException("integer");

            // Remove the last three bits, which will only be set if the
            // number is negative.  This will simplify the math below.
            integer &= ~(7 << 29);

            // The four-byte form is the trickiest since the last byte is used
            // entirely for numeric data and does not have a continuation
            // bit, which offsets all of the shift operations.  It's easier to
            // check if the number is large enough and handle that case
            // specially.
            if ((integer & (0xff << 21)) != 0) {
                Stream.WriteByte((byte)(((integer >> 22) & 0x7f) | 0x80));
                Stream.WriteByte((byte)(((integer >> 15) & 0x7f) | 0x80));
                Stream.WriteByte((byte)(((integer >>  8) & 0x7f) | 0x80));
                Stream.WriteByte((byte)(integer & 0xff));
                return;
            }

            // Now all that remains are the forms with 1-3 bytes.  Test from
            // the high group to the low group.
            bool force = false;

            if ((integer & (0x7f << 14)) != 0) {
                Stream.WriteByte((byte)(((integer >> 14) & 0x7f) | 0x80));
                force = true;
            }

            if (force || (integer & (0x7f << 7)) != 0) {
                Stream.WriteByte((byte)(((integer >>  7) & 0x7f) | 0x80));
            }

            Stream.WriteByte((byte)(integer & 0x7f));
        }

        public void TypelessWrite(double number)
        {
            Stream.Write(conv.GetBytes(number));
        }

        public void TypelessWrite(string str)
        {
            if (string.IsNullOrEmpty(str)) {
                TypelessWrite(1);
                return;
            }

            int index;
            if (stringTable.TryGetValue(str, out index)) {
                TypelessWrite(index << 1);
                return;
            }

            byte[] bytes = Encoding.UTF8.GetBytes(str);

            TypelessWrite((bytes.Length << 1) | 1);
            Stream.Write(bytes);

            stringTable[str] = stringTable.Count;
        }

        public void TypelessWrite(_root.Date date)
        {
            TypelessWrite(1);

			double ticks = date.getTime();

            TypelessWrite(ticks);

            objectTable[date] = objectTable.Count;
        }

		private void WriteDynamicClass(PlayScript.IDynamicClass dc)
        {
            foreach (string key in dc.__GetDynamicNames()) {
                TypelessWrite(key);
                Write(dc.__GetDynamicValue(key));
            }

            TypelessWrite("");
        }

        public void TypelessWrite(_root.Array array)
        {
            if (CheckObjectTable(array))
                return;

            objectTable[array] = objectTable.Count;

            TypelessWrite((array.length << 1) | 1);

			var dc = (PlayScript.IDynamicClass)array;
            WriteDynamicClass(dc);

            foreach (object i in array) {
                Write(i);
            }
        }

        public void TypelessWrite(IList list)
        {
            if (CheckObjectTable(list))
                return;

            objectTable[list] = objectTable.Count;

            TypelessWrite((list.Count << 1) | 1);

            TypelessWrite(""); // Empty associative array

            foreach (object i in list) {
                Write(i);
            }
        }

		public void TypelessWrite(Amf3ClassDef classDef)
		{
			int index;
			if (classDefTable.TryGetValue(classDef, out index)) {
				TypelessWrite((index << 2) | 1);
			} else {
				classDefTable[classDef] = classDefTable.Count;

				Amf3Object.Flags flags = Amf3Object.Flags.Inline | Amf3Object.Flags.InlineClassDef;

				if (classDef.Externalizable)
					flags |= Amf3Object.Flags.Externalizable;
				if (classDef.Dynamic)
					flags |= Amf3Object.Flags.Dynamic;

				TypelessWrite((int)flags | (classDef.Properties.Count << 4));

				TypelessWrite(classDef.Name);

				foreach (string i in classDef.Properties) {
					TypelessWrite(i);
				}
			}
		}

        public void TypelessWrite(Amf3Object obj)
        {
            if (CheckObjectTable(obj))
                return;

			TypelessWrite(obj.ClassDef);

            objectTable[obj] = objectTable.Count;

            if (obj.ClassDef.Externalizable) {
                Write(obj["inner"]);
                return;
            }

            foreach (string i in obj.ClassDef.Properties) {
                Write(obj[i]);
            }

            if (obj.ClassDef.Dynamic) {
                var dynamicProperties =
                    from i in obj.Properties
                    where !obj.ClassDef.Properties.Contains(i.Key)
                    select i;

                foreach (var i in dynamicProperties) {
                    TypelessWrite(i.Key);
                    Write(i.Value);
                }

                TypelessWrite("");
            }
        }
    }
}
