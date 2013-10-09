// Amf3Object.cs
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

namespace Amf
{
	public class Amf3Object : IAmf3Writable
    {
        public Amf3ClassDef ClassDef { get; private set; }

        public IDictionary<string, object> Properties { get; private set; }

        public object this[string key]
        {
            get
            {
                object r;
                return Properties.TryGetValue(key, out r) ? r : null;
            }
            set
            {
                Properties[key] = value;
            }
        }

        public Amf3Object(Amf3ClassDef classDef)
        {
            if (classDef == null)
                throw new ArgumentNullException("classDef");

            ClassDef = classDef;
            Properties = new Dictionary<string, object>();
        }

        public Amf3Object() : this(Amf3ClassDef.Anonymous) {}

        [Flags]
        internal enum Flags : int
        {
            Inline = 1,
            InlineClassDef = 2,
            Externalizable = 4,
            Dynamic = 8
        }

		#region IAmf3Writable implementation
		public void Serialize(Amf3Writer writer)
		{
			writer.Write(this);
		}
		#endregion
    }
}
