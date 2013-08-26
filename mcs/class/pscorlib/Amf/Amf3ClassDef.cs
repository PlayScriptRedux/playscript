// Amf3ClassDef.cs
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
using System.Linq;

namespace Amf
{
    public class Amf3ClassDef : IEquatable<Amf3ClassDef>
    {
        public static readonly Amf3ClassDef Anonymous =
            new Amf3ClassDef("", Enumerable.Empty<string>(), true, false);

        public IList<string> Properties { get; private set; }

        public string Name { get; private set; }

        public bool Dynamic { get; private set; }

        public bool Externalizable { get; private set; }

        public Amf3ClassDef(string name, IEnumerable<string> properties, bool dynamic, bool externalizable)
        {
            if (dynamic && externalizable)
                throw new ArgumentException("AMF classes cannot be both dynamic and externalizable");

            Name = name;
            Properties = properties.ToList().AsReadOnly();
            Dynamic = dynamic;
            Externalizable = externalizable;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^
                (Dynamic ? 1 : 0) ^
                (Externalizable ? 2 : 0);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Amf3ClassDef);
        }

        public bool Equals(Amf3ClassDef other)
        {
            if (other == null)
                return false;

            if (object.ReferenceEquals(this, other))
                return true;

            if (Name             != other.Name ||
                Dynamic          != other.Dynamic ||
                Externalizable   != other.Externalizable ||
                Properties.Count != other.Properties.Count)
                return false;

            for (int i = 0; i < Properties.Count; i++)
            {
                if (Properties[i] != other.Properties[i])
                    return false;
            }

            return true;
        }

		// the last writer to write this object
		internal Amf3Writer				mWriter;
		// the id associated with this object
		internal int 					mId;
    }
}
