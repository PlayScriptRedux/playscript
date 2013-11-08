// Copyright 2013 Zynga Inc.
//	
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//		
//      Unless required by applicable law or agreed to in writing, software
//      distributed under the License is distributed on an "AS IS" BASIS,
//      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//      See the License for the specific language governing permissions and
//      limitations under the License.

namespace flash.utils {
	using System;
	using System.IO;
	using System.IO.Compression;
	using System.Diagnostics;
	using System.Text;
	using Amf;
	
	[DebuggerDisplay("length = {length}")]
	public class ByteArray : _root.Object, IDataInput, IDataOutput {

		enum ByteEndian
		{
			Big,
			Little
		};
	
		//
		// Properties
		//
		
		public uint bytesAvailable { 
			get { return length - position; } 
		}

		private static uint sDefaultObjectEncoding = 3;
		public static uint defaultObjectEncoding  {
			get { return sDefaultObjectEncoding; } 
			set { sDefaultObjectEncoding = value; } 
		}

		public string endian {
			get 
			{
				// convert enum to string
				switch (mEndian)
				{
				case ByteEndian.Big: return Endian.BIG_ENDIAN;
				case ByteEndian.Little: return Endian.LITTLE_ENDIAN;
				default:
					throw new NotImplementedException(); 
				}
			} 

			set 
			{
				// convert string to enum
				switch (value)
				{
				case Endian.LITTLE_ENDIAN:
					mEndian = ByteEndian.Little;
					break;
				case Endian.BIG_ENDIAN:
					mEndian = ByteEndian.Big;
					break;
				default:
					throw new NotImplementedException(); 
				}
			} 
		}

 	 	public uint length { 
 	 		get {return (uint)mLength;}
 	 		set 
			{
				setLength((int)value);
			} 
 	 	}

 	 	public uint objectEncoding { 
			get;
			set;
 	 	}

 	 	public uint position
 	 	{ 
 	 		get {return (uint)mPosition;} 
 	 		set {mPosition = (int)value;} 
 	 	}

		//
		// Methods
		//

		public ByteArray() {
			this.objectEncoding = ByteArray.defaultObjectEncoding;
		}
 	 	
		public void clear() {
			mPosition = 0;
			mLength   = 0;
			mData     = new byte[0];
		}
 	 	
		public void compress(string algorithm = null) {
			throw new NotImplementedException();
		}
 	 	
		public void deflate() {
			throw new NotImplementedException();
		}
 	 	
		public void inflate() {
			uncompress (CompressionAlgorithm.DEFLATE);
		}
 	 	
		public void setCapacity(int capacity){
			if (capacity >= mLength) {
				// resize array
				var newData = new byte[capacity];
				Buffer.BlockCopy(mData, 0, newData, 0, mLength);
				mData     = newData;
			}
		}

		public void trimCapacity() {
			setCapacity(mLength);
		}

		private void setLength(int newLength) {
			// do we need to resize our inner array?
			if (newLength > mData.Length)
			{
				// determine new capacity
				int newCapacity = newLength;

				// dont grow geometrically if its the first write
				if ((mData.Length != 0) && mPosition != 0) {
					// grow array geometrically
					newCapacity = newLength * 2;
				}

				// clamp to min capacity
				if (newCapacity < 64) newCapacity = 64;

				// set new capacity
				setCapacity(newCapacity);
			}

			// set new length
			mLength = newLength;
		}

		private void throwEndOfBuffer() {
			throw new _root.Error("Error #2030: End of file was encountered.");
		}
		
		private void checkReadLength(int readLength) {
			if ((mPosition + readLength) > mLength)	{
				throwEndOfBuffer();
			}
		}

		private void checkWriteLength(int writeLength) {
			int newLength = mPosition + writeLength;
			if (newLength > mLength)	{
				setLength(newLength);
			}
		}


		public bool readBoolean() {
			return readByte () != 0;
		}

		public int readByte(){
			checkReadLength(1);
			return (int)mData[mPosition++];
		}
 	 	
		public void readBytes(ByteArray bytes, uint offset = 0, uint len = 0) {
			uint pos = position;//need to save the position in case we are reading bytes into ourself.
			uint oldpos = bytes.position;
			bytes.position = offset;
			bytes.writeBytes (this, pos, len);
			bytes.position = oldpos;
			position = (len == 0) ? length : (pos + len);
		}

		public void readBytes(byte[] dest, int offset, int length) {
			checkReadLength(length);
			// copy data from internal array
			Buffer.BlockCopy(mData, mPosition, dest, offset, length);
			mPosition += length;
		}

		public double readDouble() {
			checkReadLength(sizeof(double));
			if ((mEndian == ByteEndian.Little) && (BitConverter.IsLittleEndian))
			{
				var value = BitConverter.ToDouble(mData, mPosition);
				mPosition += sizeof(double);
				return value;
			}
			else
			{
				// TODO
				throw new NotImplementedException();
			}
		}
 	 	
		public double readFloat() {
			checkReadLength(sizeof(float));
			if ((mEndian == ByteEndian.Little) && (BitConverter.IsLittleEndian))
			{
				var value = BitConverter.ToSingle(mData, mPosition);
				mPosition += sizeof(float);
				return (double)value;
			}
			else
			{
				// TODO
				throw new NotImplementedException();
			}
		}
 	 	
		public int readInt() {
			// convert unsigned int
			return (int)readUnsignedInt();
		}
 	 	
		public string readMultiByte(uint length, string charSet) {
			checkReadLength((int)length);
			string str;

			if(charSet.Length>0)
			{
				Encoding encoding = Encoding.GetEncoding(charSet);
				str = encoding.GetString(mData,mPosition, (int)length);
			}
			else 
			{
				str = System.Text.Encoding.UTF8.GetString(mData, mPosition, (int)length);
			}
				// Decode byte sequence
			mPosition += (int)length;
			return str;		
		}
 	 	
		[return: PlayScript.AsUntyped]
		public dynamic readObject() {
			PlayScript.Profiler.Begin("amf-parse");
			Amf3Parser amfparser = new Amf3Parser(mData, 0, mLength);
			object obj = amfparser.ReadNextObject();
			PlayScript.Profiler.End("amf-parse");
			return obj;
		}
 	 	
		public int readShort() {
			// sign extend unsigned short
			return (((int)readUnsignedShort()) << 16) >> 16;
		}
 	 	
		public uint readUnsignedByte() {
			checkReadLength(1);
			return (uint)mData[mPosition++];
		}
 	 	
		public uint readUnsignedInt() {
			checkReadLength(4);
			if (mEndian == ByteEndian.Little)
			{
				uint d;
				d = ((uint)mData[mPosition++]) << 0;
				d|= ((uint)mData[mPosition++]) << 8;
				d|= ((uint)mData[mPosition++]) << 16;
				d|= ((uint)mData[mPosition++]) << 24;
				return d;
			} 
			else 
			{
				uint d;
				d = ((uint)mData[mPosition++]) << 24;
				d|= ((uint)mData[mPosition++]) << 16;
				d|= ((uint)mData[mPosition++]) << 8;
				d|= ((uint)mData[mPosition++]) << 0;
				return d;
			}
		}
 	 	
		public uint readUnsignedShort() {
			checkReadLength(2);
			if (mEndian == ByteEndian.Little)
			{
				uint d;
				d = ((uint)mData[mPosition++]) << 0;
				d|= ((uint)mData[mPosition++]) << 8;
				return d;
			} 
			else 
			{
				uint d;
				d = ((uint)mData[mPosition++]) << 8;
				d|= ((uint)mData[mPosition++]) << 0;
				return d;
			}
		}
 	 	
		public string readUTF() {
			uint length = readUnsignedShort();
			return readUTFBytes(length);
		}
 	 	
		public string readUTFBytes(uint length) {
			checkReadLength((int)length);
			// decode as UTF8 string
			var str = System.Text.Encoding.UTF8.GetString(mData, mPosition, (int)length);
			mPosition += (int)length;
			return str;
		}

		public dynamic toJSON(string k) {
			throw new NotImplementedException();
		}

		public void  uncompress(string algorithm = null) {
			position = 0;
			var inStream = getRawStream();
			var outStream = new MemoryStream();

			switch (algorithm) {
			case null:
			case "":
			case CompressionAlgorithm.ZLIB:
				{
					// parse zlib header
					int header0 = inStream.ReadByte();
					/*int flag =*/ inStream.ReadByte();
					if ((header0 & 0xF) == 8) {
						// deflate
						using (DeflateStream decompressionStream = new DeflateStream(inStream, CompressionMode.Decompress)) {
							decompressionStream.CopyTo(outStream);
						}
					} else {
							throw new System.NotImplementedException("ZLIB compression format:" + header0);
					}
				}
				break;

			case CompressionAlgorithm.LZMA:
			case CompressionAlgorithm.DEFLATE:
				{
					using (DeflateStream decompressionStream = new DeflateStream(inStream, CompressionMode.Decompress)) {
						inStream.Position = 0;
						decompressionStream.CopyTo (outStream);
					}
				}
				break;
			}

			// resize to be just the right length
			this.length = 0;
			this.length = (uint)outStream.Length;
			// read from stream
			outStream.Position = 0;
			outStream.Read(mData, 0, (int)this.length);
			position = 0;
		}
 	
		public void writeBoolean(bool value) {
			writeByte (value ? 1 : 0);
		}

		private void internalWriteByte(byte value) {
			mData[mPosition++] = value;
		}

		public void writeByte(int value) {
			checkWriteLength(1);
			internalWriteByte((byte)value);
		}
 	 	
		public void writeBytes(ByteArray bytes, uint offset = 0, uint len = 0) { 
			writeBytes (bytes.getRawArray(), (int)offset, (int)((len == 0) ? bytes.length - offset : len));
		}
 	 	
		public void writeDouble(double value) {
			checkWriteLength(8);
			long bits = BitConverter.DoubleToInt64Bits(value);
			if (mEndian == ByteEndian.Little)
			{
				writeUnsignedInt((uint)bits);
				writeUnsignedInt((uint)(bits >> 32) );
			}
			else
			{
				writeUnsignedInt((uint)(bits >> 32) );
				writeUnsignedInt((uint)bits);
			}
		}
 	 	
		public void writeFloat(double value) {
			checkWriteLength(4);
			// this seems slow
			byte[] b = BitConverter.GetBytes( (float)value );
			if ((mEndian == ByteEndian.Little) && (BitConverter.IsLittleEndian))
			{
				internalWriteByte( b[0] );
				internalWriteByte( b[1] );
				internalWriteByte( b[2] );
				internalWriteByte( b[3] );
			} 
			else
			{
				internalWriteByte( b[3] );
				internalWriteByte( b[2] );
				internalWriteByte( b[1] );
				internalWriteByte( b[0] );
			}
		}
 	 	
		public void writeInt(int value) {
			checkWriteLength(4);
			if (mEndian == ByteEndian.Little)
			{
				internalWriteByte( (byte)(value >> 0) );
				internalWriteByte( (byte)(value >> 8) );
				internalWriteByte( (byte)(value >> 16) );
				internalWriteByte( (byte)(value >> 24) );
			} 
			else
			{
				internalWriteByte( (byte)(value >> 24) );
				internalWriteByte( (byte)(value >> 16) );
				internalWriteByte( (byte)(value >> 8) );
				internalWriteByte( (byte)(value >> 0) );
			}
		}
 	 	
		public void writeMultiByte(string value, string charSet) {
			throw new NotImplementedException();
		}
 	 	
		public void writeObject(object obj) {
			throw new NotImplementedException();
		}
 	 	
		public void writeShort(int value) {
			checkWriteLength(2);
			if (mEndian == ByteEndian.Little)
			{
				internalWriteByte( (byte)(value >> 0) );
				internalWriteByte( (byte)(value >> 8) );
			}
			else
			{
				internalWriteByte( (byte)(value >> 8) );
				internalWriteByte( (byte)(value >> 0) );
			}
		}
 	 	
		public void writeUnsignedInt(uint value) {
			checkWriteLength(4);
			if (mEndian == ByteEndian.Little)
			{
				internalWriteByte( (byte)(value >> 0) );
				internalWriteByte( (byte)(value >> 8) );
				internalWriteByte( (byte)(value >> 16) );
				internalWriteByte( (byte)(value >> 24) );
			} 
			else
			{
				internalWriteByte( (byte)(value >> 24) );
				internalWriteByte( (byte)(value >> 16) );
				internalWriteByte( (byte)(value >> 8) );
				internalWriteByte( (byte)(value >> 0) );
			}
		}
 	 	
		public void writeUTF(string value) {
			// encode as UTF8 string
			byte[] buffer = System.Text.Encoding.UTF8.GetBytes(value);
			writeShort(buffer.Length);
			writeBytes(buffer);
		}
 	 	
		public void writeUTFBytes(string value) {
			// encode as UTF8 string
			byte[] buffer = System.Text.Encoding.UTF8.GetBytes(value);
			writeBytes(buffer);
		}

		public void writeBytes(byte[] source) {
			writeBytes (source, 0, source.Length);
		}

		public void writeBytes(byte[] source, int offset, int length) {
			checkWriteLength(length);

			// copy data to internal array
			Buffer.BlockCopy(source, offset, mData, mPosition, length);
			mPosition += length;
		}
		
		public int this[int index] {
			get 
			{
				if (index < 0 || index >= mLength)
					throwEndOfBuffer();

				return (int)mData[index];
			}
			set 
			{
				if (index < 0)
					throwEndOfBuffer();

				if (index >= mLength) {
					// resize length on write
					setLength(index + 1);
				}

				mData[index] = (byte)value;
			}
		}


		public Stream getRawStream() {
			return new ByteArrayStream(this);
		}

		public byte[] getRawArray() {
			return mData;
		}

		public void readFromStream(System.IO.Stream stream)
		{
			stream.CopyTo(this.getRawStream());
		}

		public static ByteArray fromArray(byte[] array) {
			ByteArray ba = new ByteArray();
			ba.mData = array;
			ba.mLength = array.Length;
			return ba;
		}

		public static ByteArray cloneFromArray<T>(T[] array, int count) where T:struct {
			int byteLength = Buffer.ByteLength(array);
			if (count != array.Length) {
				byteLength = byteLength / array.Length * count;
			}

			byte[] clone = new byte[byteLength];
			Buffer.BlockCopy(array, 0, clone, 0, clone.Length);
			return ByteArray.fromArray(clone);
		}

		public static ByteArray loadFromPath(string path) {
			var newPath = PlayScript.Player.ResolveResourcePath(path);
			byte[] data = System.IO.File.ReadAllBytes(newPath);
			return ByteArray.fromArray (data);
		}

		public override string toString()
		{
			// encode entire byte array as a UTF8 string
			return System.Text.Encoding.UTF8.GetString(mData, 0, mLength);
		}


		ByteEndian mEndian = ByteEndian.Big; // bytearrays are big endian by default
		int        mPosition = 0;
		int        mLength = 0;
		byte[]     mData = new byte[0];

		#region ByteArrayStream
		// internal System.IO.Stream wrapper for a byte array
		private class ByteArrayStream : Stream
		{
			public ByteArrayStream(ByteArray array)
			{
				mArray = array;
			}

			#region implemented abstract members of Stream

			public override void Flush()
			{
			}

			public override int ReadByte()
			{
				if (mArray.position < mArray.length) {
					return mArray.readByte();
				} else {
					return -1;
				}
			}

			public override void WriteByte(byte value)
			{
				mArray.writeByte((int)value);
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				int available = (int)mArray.length - (int)mArray.position;
				if (count > available) {
					count = available;
				}

				mArray.readBytes(buffer, offset, count);
				return count;
			}

			public override long Seek(long offset, SeekOrigin origin)
			{
				throw new NotImplementedException();
			}

			public override void SetLength(long value)
			{
				mArray.length = (uint)value;
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				mArray.writeBytes(buffer, offset, count);
			}

			public override bool CanRead {
				get {
					return true;
				}
			}

			public override bool CanSeek {
				get {
					return true;
				}
			}

			public override bool CanWrite {
				get {
					return true;
				}
			}

			public override long Length {
				get {
					return (long)mArray.length;
				}
			}

			public override long Position {
				get {
					return (long)mArray.position;
				}
				set {
					mArray.position = (uint)value;
				}
			}
			#endregion

			private readonly ByteArray mArray;
		}
		#endregion
	}

}


