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
using System.Reflection;
using _root;
using PlayScript.Expando;

namespace Amf
{
    public class Amf3Writer
    {
		private static readonly Amf.DataConverter conv = Amf.DataConverter.BigEndian;

        public Stream Stream { get; private set; }

		public bool TrackArrayReferences { get; set; }
		public bool WriteDoublesAsInts {get; set;}
		public bool WriteZerosAsNulls {get; set;}

        private Dictionary<string, int> stringTable = new Dictionary<string, int>();

        private Dictionary<object, int> objectTable =
            new Dictionary<object, int>(new ReferenceEqualityComparer<object>());
		private int 					objectTableIndex = 0;

        private Dictionary<Amf3ClassDef, int> classDefTable =
            new Dictionary<Amf3ClassDef, int>();

		private readonly byte[] tempData = new byte[8];

        private const int amfIntMaxValue = int.MaxValue >> 3;
        private const int amfIntMinValue = int.MinValue >> 3;

		private readonly Amf3ClassDef anonClassDef = new Amf3ClassDef("*", new string[0], true, false);

		private readonly Dictionary<Type, IAmf3Serializer> typeToSerializer = new Dictionary<Type, IAmf3Serializer>();

        public Amf3Writer(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            Stream = stream;

			// track array references by default
			TrackArrayReferences = true;
			// write doubles as integers by default
			WriteDoublesAsInts = true;
			// dont write zeros as nulls by default (non-standard?)
			WriteZerosAsNulls = false;
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

		private void StoreObject(object obj)
		{
			if (obj != null) {
				// store index for this object
				objectTable[obj] = objectTableIndex;
			}
			// increment object index
			objectTableIndex++;
		}

		private IAmf3Serializer GetSerializerForType(System.Type type)
		{
			IAmf3Serializer serializer;
			if (!typeToSerializer.TryGetValue(type, out serializer)) {
				var alias = Amf3ClassDef.GetAliasFromType(type);
				if (alias != null) {
					serializer = Amf3ClassDef.GetSerializerFromAlias(alias);
					if (serializer == null) {
						// create reflection serializer
						serializer = new ReflectionSerializer(alias, type, true, false);
						// automatically register it
						Amf3ClassDef.RegisterSerializer(alias, serializer);
					}
				} else {
					// create anonymous serializer
					serializer = new ReflectionSerializer("*", type, true, false);
				}
				// store serializer in our local cache
				typeToSerializer.Add(type, serializer);
			}
			return serializer;
		}

        private void Write(Amf3TypeCode type)
        {
            Stream.WriteByte((byte)type);
        }


		public void WriteDefaultObjectForType(Type type)
		{
			// get serializer
			IAmf3Serializer serializer = GetSerializerForType(type);
			// create default instance
			var defaultInstance = serializer.NewInstance(null);
			// write object using serializer
			serializer.WriteObject(this, defaultInstance);
		}

        public void Write(object obj)
        {
            if (obj == null) {
                Write(Amf3TypeCode.Null);
                return;
            }

			var serializable = obj as IAmf3Writable;
			if (serializable != null) {
				serializable.Serialize(this);
                return;
            }

			var str = obj as string;
			if (str != null) {
				Write(str);
				return;
			}

			var expando = obj as ExpandoObject;
			if (expando != null) {
				Write(expando);
				return;
			}

            if (obj is bool) {
                Write((bool)obj);
                return;
            }

            if (obj is double) {
                Write((double)obj);
                return;
            }

			if (obj is float) {
				Write((double)(float)obj);
				return;
			}

            if (obj is int) {
                Write((int)obj);
                return;
            }

			if (obj is uint) {
				Write((int)(uint)obj);
				return;
			}

			if (obj is _root.Date) {
				Write((_root.Date)obj);
				return;
			}

            if (obj is IList) {
				Write((IList)obj);
                return;
            }

			if (obj is flash.utils.ByteArray) {
				Write((flash.utils.ByteArray)obj);
				return;
			}

			if (obj is flash.utils.Dictionary) {
				Write((flash.utils.Dictionary)obj);
				return;
			}

			if (obj is byte ||
			    obj is sbyte ||
			    obj is short ||
			    obj is ushort) {
				Write(Convert.ToInt32(obj));
				return;
			}

			// get serializer from type
			var type = obj.GetType();
			IAmf3Serializer serializer = GetSerializerForType(type);
			// write object using serializer
			serializer.WriteObject(this, obj);
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
			if (WriteZerosAsNulls && integer == 0) {
				Write(Amf3TypeCode.Null);
				return;
			}

			if (integer > amfIntMaxValue || integer < amfIntMinValue) {
				// write large integers as doubles
				Write((double)integer);
				return;
			}

            Write(Amf3TypeCode.Integer);
            TypelessWrite(integer);
        }

        public void Write(double number)
        {
			// is number within range of an integer?
			if (WriteDoublesAsInts && !double.IsNaN(number) && (number >= amfIntMinValue) && (number <= amfIntMaxValue)) {
				// write number as integer if we can 
				int numberInt = (int)number;
				if (((double)numberInt) == number) {
					// write as integer
					Write(numberInt);
					return;
				}
			}

            Write(Amf3TypeCode.Number);
            TypelessWrite(number);
        }

        public void Write(string str)
        {
            Write(Amf3TypeCode.String);
            TypelessWrite(str);
        }

		public void Write(Amf3String str)
		{
			Write(Amf3TypeCode.String);
			TypelessWrite(str);
		}

        public void Write(_root.Date date)
        {
            Write(Amf3TypeCode.Date);
            TypelessWrite(date);
        }

        public void Write(IList obj)
        {
			if (obj == null) {
				Write(Amf3TypeCode.Null);
				return;
			}

			if (obj is _root.Array) {
				Write(Amf3TypeCode.Array);
				TypelessWriteArray((_root.Array)obj);
				return;
			} 

			if (obj is Vector<uint>) {
				Write((Vector<uint>)obj);
				return;
			}

			if (obj is Vector<int>) {
				Write((Vector<int>)obj);
				return;
			}

			if (obj is Vector<double>) {
				Write((Vector<double>)obj);
				return;
			}

			if (obj is Vector<object>) {
				Write((Vector<object>)obj);
				return;
			}

			if (obj is uint[]) {
				Write((uint[])obj);
				return;
			}

			if (obj is int[]) {
				Write((int[])obj);
				return;
			}

			if (obj is double[]) {
				Write((double[])obj);
				return;
			}

			// check for a typed vector, example: Vector<MyClass>
			var objType = obj.GetType();
			if (objType.Name == "Vector`1")	{
				Write(Amf3TypeCode.VectorObject);
				TypelessWriteVectorObject(obj);
				return;
			}

			throw new ArgumentException("Cannot serialize object of type " + objType.FullName);
        }

		public void Write(flash.utils.ByteArray byteArray)
		{
			Write(Amf3TypeCode.ByteArray);
			TypelessWrite(byteArray);
		}

		public void Write(flash.utils.Dictionary dict)
		{
			Write(Amf3TypeCode.Dictionary);
			TypelessWrite(dict);
		}


		public void Write(Vector<int> vector)
		{
			Write(Amf3TypeCode.VectorInt);
			TypelessWrite(vector);
		}

		public void Write(Vector<uint> vector)
		{
			Write(Amf3TypeCode.VectorUInt);
			TypelessWrite(vector);
		}

		public void Write(Vector<double> vector)
		{
			Write(Amf3TypeCode.VectorDouble);
			TypelessWrite(vector);
		}

		public void Write(Vector<object> vector)
		{
			Write(Amf3TypeCode.VectorObject);
			TypelessWrite(vector);
		}

		public void Write(int[] vector)
		{
			Write(Amf3TypeCode.VectorInt);
			TypelessWrite(vector);
		}

		public void Write(uint[] vector)
		{
			Write(Amf3TypeCode.VectorUInt);
			TypelessWrite(vector);
		}

		public void Write(double[] vector)
		{
			Write(Amf3TypeCode.VectorDouble);
			TypelessWrite(vector);
		}

		public void Write(ExpandoObject obj)
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

		public void TypelessWrite(uint integer)
		{
			TypelessWrite((int)integer);
		}

        public void TypelessWrite(double number)
        {
			conv.PutBytes(tempData, 0, number);
            Stream.Write(tempData, 0, sizeof(double));
        }

		public void TypelessWrite(Amf3String str)
		{
			// has this string been written before with this writer?
			if (str.mWriter == this) {
				// write string id reference
				TypelessWrite(str.mId << 1);
				return;
			}

			// special case empty strings
			if (string.IsNullOrEmpty(str.Value)) {
				TypelessWrite(1);
				return;
			}

			// lookup string by value
			int index;
			if (stringTable.TryGetValue(str.Value, out index)) {
				// cache id within string object
				str.mWriter = this;
				str.mId     = index;
				// write string id reference
				TypelessWrite(index << 1);
				return;
			}

			// write UTF8 bytes of string
			byte[] bytes = str.Bytes;
			TypelessWrite((bytes.Length << 1) | 1);
			Stream.Write(bytes);

			// cache id within string object
			str.mWriter = this;
			str.mId     = stringTable.Count;

			// store string value in string table
			stringTable[str.Value] = str.mId;
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

			StoreObject(date);
        }

		private void WriteDynamicClass(PlayScript.IDynamicClass dc)
        {
			var names = dc.__GetDynamicNames();
			if (names != null) {
				foreach (string key in names) {
					TypelessWrite(key);
					Write(dc.__GetDynamicValue(key));
				}
			}

            TypelessWrite("");
        }

        public void TypelessWriteArray(_root.Array array)
        {
			if (TrackArrayReferences) {
				if (CheckObjectTable(array))
					return;

				StoreObject(array);
			} else {
				StoreObject(null);
			}

            TypelessWrite((array.length << 1) | 1);

			var dc = (PlayScript.IDynamicClass)array;
            WriteDynamicClass(dc);

            foreach (object i in array) {
                Write(i);
            }
        }

		public void TypelessWriteVectorObject(IList vector)
		{
			if (WriteVectorHeader(vector, (uint)vector.Count, true))
				return;

			// get type of vector element
			Type elementType = vector.GetType().GetGenericArguments()[0];
			// get class info for type
			string alias = Amf3ClassDef.GetAliasFromType(elementType);
			// get alias of vector element from info
			if (alias == null) {
				alias = elementType.FullName;
			}
			// write vector element class alias
			TypelessWrite(alias);

			foreach (object i in vector) {
				Write(i);
			}
		}


		public void TypelessWrite(flash.utils.ByteArray byteArray)
		{
			if (TrackArrayReferences) {
				if (CheckObjectTable(byteArray))
					return;

				StoreObject(byteArray);
			} else {
				StoreObject(null);
			}

			// write byte array length
			TypelessWrite((byteArray.length << 1) | 1);

			// write bytes of byte array
			Stream.Write(byteArray.getRawArray(), 0, (int)byteArray.length);
		}

		public void TypelessWrite(flash.utils.Dictionary dict)
		{
			if (TrackArrayReferences) {
				if (CheckObjectTable(dict))
					return;

				StoreObject(dict);
			} else {
				StoreObject(null);
			}

			// write byte array length
			TypelessWrite((dict.length << 1) | 1);
			// no weak keys for now
			TypelessWrite((byte)0);

			// write dictionary key value pairs
			int count = 0;
			foreach (var kvp in dict) {
				Write(kvp.Key);
				Write(kvp.Value);
				count++;
			}

			if (count != dict.length) {
				throw new InvalidOperationException("flash.utils.Dictionary count does not match expected length");
			}
		}


		private bool WriteVectorHeader(object vector, uint length, bool isFixed)
		{
			if (TrackArrayReferences) {
				if (CheckObjectTable(vector))
					return true;

				StoreObject(vector);
			} else {
				StoreObject(null);
			}

			TypelessWrite((length << 1) | 1);
			Stream.WriteByte(isFixed ? (byte)1 : (byte)0); 
			return false;
		}

		public void TypelessWrite(Vector<uint> vector)
		{
			// write vector header
			if (WriteVectorHeader(vector, vector.length, vector.@fixed))
				return;

			// write vector data
			int length = (int)vector.length;
			for (int i=0; i < length; i++) {
				conv.PutBytes(tempData, 0, vector[i]);
				Stream.Write(tempData, 0, sizeof(uint));
			}
		}

		public void TypelessWrite(Vector<int> vector)
		{
			// write vector header
			if (WriteVectorHeader(vector, vector.length, vector.@fixed))
				return;

			// write vector data
			int length = (int)vector.length;
			for (int i=0; i < length; i++) {
				conv.PutBytes(tempData, 0, vector[i]);
				Stream.Write(tempData, 0, sizeof(int));
			}
		}

		public void TypelessWrite(Vector<double> vector)
		{
			// write vector header
			if (WriteVectorHeader(vector, vector.length, vector.@fixed))
				return;

			// write vector data
			int length = (int)vector.length;
			for (int i=0; i < length; i++) {
				conv.PutBytes(tempData, 0, vector[i]);
				Stream.Write(tempData, 0, sizeof(double));
			}
		}

		public void TypelessWrite(Vector<object> vector)
		{
			// write vector header
			if (WriteVectorHeader(vector, vector.length, vector.@fixed))
				return;

			// write object type
			// TODO: we need to maintain this if it a vector of a custom type
			TypelessWrite((string)"*");

			// write vector data
			int length = (int)vector.length;
			for (int i=0; i < length; i++) {
				Write(vector[i]);
			}
		}


		public void TypelessWrite(uint[] vector)
		{
			// write vector header
			if (WriteVectorHeader(vector, (uint)vector.Length, true))
				return;

			// write vector data
			int length = vector.Length;
			for (int i=0; i < length; i++) {
				conv.PutBytes(tempData, 0, vector[i]);
				Stream.Write(tempData, 0, sizeof(uint));
			}
		}

		public void TypelessWrite(int[] vector)
		{
			// write vector header
			if (WriteVectorHeader(vector, (uint)vector.Length, true))
				return;

			// write vector data
			int length = vector.Length;
			for (int i=0; i < length; i++) {
				conv.PutBytes(tempData, 0, vector[i]);
				Stream.Write(tempData, 0, sizeof(int));
			}
		}

		public void TypelessWrite(double[] vector)
		{
			// write vector header
			if (WriteVectorHeader(vector, (uint)vector.Length, true))
				return;

			// write vector data
			int length = vector.Length;
			for (int i=0; i < length; i++) {
				conv.PutBytes(tempData, 0, vector[i]);
				Stream.Write(tempData, 0, sizeof(double));
			}
		}

		public void TypelessWrite(Amf3ClassDef classDef)
		{
			// have we written this class before with this writer?
			if (classDef.mWriter == this) {
				// use the cached id in the class definition
				TypelessWrite((classDef.mId << 2) | 1);
				return;
			}

			int index;
			if (classDefTable.TryGetValue(classDef, out index)) {
				// store class id inside of class def for this writer
				classDef.mWriter = this;
				classDef.mId     = index;

				TypelessWrite((index << 2) | 1);
			} else {
				// store class id inside of class def for this writer
				classDef.mWriter = this;
				classDef.mId     = classDefTable.Count;

				// store class reference in lookup table
				classDefTable[classDef] = classDef.mId;

				Amf3Object.Flags flags = Amf3Object.Flags.Inline | Amf3Object.Flags.InlineClassDef;

				if (classDef.Externalizable)
					flags |= Amf3Object.Flags.Externalizable;
				if (classDef.Dynamic)
					flags |= Amf3Object.Flags.Dynamic;

				TypelessWrite((int)flags | (classDef.Properties.Length << 4));

				TypelessWrite(classDef.Name);

				foreach (string i in classDef.Properties) {
					TypelessWrite(i);
				}
			}
		}

		public void TypelessWrite(ExpandoObject obj)
		{
			// this allows for the sharing of expando values easily
			var redirect = obj.ClassDefinition as ExpandoObject;
			if (redirect != null) {
				TypelessWrite(redirect);
				return;
			}

			if (CheckObjectTable(obj))
				return;

			// get class definition from expando
			Amf3ClassDef classDef = obj.ClassDefinition as Amf3ClassDef;
			if (classDef == null) {
				classDef = anonClassDef;
			}

			TypelessWrite(classDef);
			StoreObject(obj);

			foreach (string i in classDef.Properties) {
				Write(obj[i]);
			}

			if (classDef.Dynamic) {
				foreach (var kvp in obj) {
					TypelessWrite(kvp.Key);
					Write(kvp.Value);
				}

				TypelessWrite("");
			}
		}

		public void WriteObjectHeader(Amf3ClassDef classDef, object obj = null)
		{
			Write(Amf3TypeCode.Object);
			TypelessWrite(classDef);
			StoreObject(obj);
		}
    }
}
