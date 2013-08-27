// Rocks.cs
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
using System.IO;

namespace Amf
{
    internal static class Rocks
    {
        internal static byte[] Read(this Stream stream, int len)
        {
            byte[] buf = new byte[len];
            stream.ReadFully(buf, 0, len);

            return buf;
        }

        internal static void ReadFully(this Stream stream, byte[] buf, int offset, int length)
        {
            if (length == 0)
                return;

            int totalRead = 0;

            do {
                int read = stream.Read(buf, offset + totalRead, length - totalRead);

                if (read <= 0)
                    throw new EndOfStreamException();

                totalRead += read;
            } while (totalRead < length);
        }

        internal static byte ReadByteOrThrow(this Stream stream)
        {
            int b = stream.ReadByte();

            if (b < 0)
                throw new EndOfStreamException();

            return (byte)b;
        }

        internal static void Write(this Stream stream, byte[] buf)
        {
            stream.Write(buf, 0, buf.Length);
        }
    }
}
