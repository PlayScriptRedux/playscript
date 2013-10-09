// Amf3String.cs
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

namespace Amf
{
	/// <summary>
	/// This class wraps a string that can be serialized via the AMF writer.
	/// Using this class is more efficient than a plain string when the same string is written multiple times.
	/// The string id reference is cached in here, avoiding a dictionary lookup.
	/// The string is pre-converted to UTF8.
	/// </summary>
	public class Amf3String : IAmf3Writable
    {
		public string Value 	{ get { return mValue; } }
		public byte[] Bytes     { get { return mBytes; } }

        public Amf3String(string value)
        {
			mValue = value;
			mBytes = System.Text.Encoding.UTF8.GetBytes(value);
        }

		public override string ToString()
		{
			return mValue;
		}

		#region IAmf3Writable implementation
		public void Serialize(Amf3Writer writer)
		{
			writer.Write(this);
		}
		#endregion

		private readonly string 		mValue;
		private readonly byte[]			mBytes;

		// the last writer to write this object
		internal Amf3Writer				mWriter;
		// the id associated with this object
		internal int 					mId;
    }
}
