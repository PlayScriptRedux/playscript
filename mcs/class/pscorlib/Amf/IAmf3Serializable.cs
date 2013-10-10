// IAmf3Serializable.cs
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
using System.IO;

namespace Amf
{
	// this interface applies to an object that can be written to an AMF stream
	public interface IAmf3Writable
	{
		void Serialize(Amf3Writer writer);
	}

	// this interface applies to an object that can be read from an AMF stream
	public interface IAmf3Readable
	{
		void Serialize(Amf3Reader reader);
	}

	// this interface applies to an object that can be read from or written to an AMF stream
    public interface IAmf3Serializable : IAmf3Readable, IAmf3Writable
    {
    }

	// object serializer delegates (for use when you cannot implement IAmf3Serializable)
	// using the delegates is much faster than using reflection to construct or serialize an object
	// they can be added to an Amf3Serializable or Amf3ExternalSerializer class and named this way:
	//	public static class AmfSerializer_Value	{
	//		public static object ObjectConstructor() {return new Value();}
	//		public static System.Collections.IList VectorObjectConstructor(uint len, bool isFixed) {return new _root.Vector<Value>(len, isFixed);}
	//		public static void ObjectSerializer(object o, Amf3Writer writer) { ... }
	//		public static void ObjectDeserializer(object o, Amf3Reader reader) { ... }
	//	}
	public delegate object Amf3ObjectConstructor();
	public delegate IList  Amf3ObjectVectorConstructor(uint num, bool isFixed);
	public delegate void   Amf3ObjectSerializer(object obj, Amf3Writer writer);
	public delegate void   Amf3ObjectDeserializer(object obj, Amf3Reader reader);
}
