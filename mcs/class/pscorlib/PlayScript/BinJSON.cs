/*
 * Binary JSON replacement system.
 * 
 * Replaces JSON class in flash with a replacement that can parse to static binary buffers, and can load directly from
 * a static binary buffer.
 * 
 * This library essentially exposes two methods:
 * 
 * var o:Object = BinJSON.parse(json:String);
 * 
 * and
 * 
 * var o:Object = BinJSON.fromBinary(data:ByteArray);
 * 
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using PlayScript;
using PlayScript.DynamicRuntime;
using _root;

using PTRINT = System.UInt32;

namespace playscript.utils {

	// CRC32 calculator
	internal static class BinJSonCrc32
	{
		public const uint DefaultPolynomial = 0xedb88320u;
		public const uint DefaultSeed = 0xffffffffu;

		public static uint[] Table;

		public static void InitializeTable()
		{
			uint polynomial = DefaultPolynomial;
			var createTable = new uint[256];
			for (var i = 0; i < 256; i++)
			{
				var entry = (uint)i;
				for (var j = 0; j < 8; j++)
					if ((entry & 1) == 1)
						entry = (entry >> 1) ^ polynomial;
				else
					entry = entry >> 1;
				createTable[i] = entry;
			}

			Table = createTable;
		}

		public static uint Calculate(string buffer)
		{
			if (buffer == null)
				return 0;
			if (Table == null)
				InitializeTable ();
			uint crc = DefaultSeed;
			int size = buffer.Length;
			for (var i = 0; i < size; i++)
				crc = (crc >> 8) ^ Table[(byte)buffer[i] ^ crc & 0xff];
			return ~crc;
		}

	}

	// Element of json ARRAY and OBJECT in binary data.  Top 3 bits of "id" is the data type of the element, b
	// bottom 13 bits are index into the key string table for objects, and unused for arrays.
	[StructLayout(LayoutKind.Explicit, Size = 8)]
	internal struct DataElem {
		[FieldOffset(0)]
		public uint id; 		// Data type in top 3 bits, plus string id in bottom 13 bits (8192 possible strings)
		[FieldOffset(4)]
		public uint offset;
		[FieldOffset(4)]
		public int intValue;
		[FieldOffset(4)]
		public float floatValue;
	}

	// String table element
	[StructLayout(LayoutKind.Explicit, Size = 8)]
	internal struct StringTableElem {
		[FieldOffset(0)]
		public uint crc;
		[FieldOffset(4)]
		public uint offset;
	}

	// Stores a pair of string and crc
	public class KeyCrcPairs {
		public int length;
		public string[] keys;
		public uint[] crcs;
	}

	// Type of list - ARRAY or OBJECT.  Used in the LIST header for objects/arrays to indicate what type of 
	// DataElem array this will be.   Each LIST has a two uint header, the first uint of which is the relative pointer
	// to the lists data, and the second of which is an 8 bit list type plus a 24 bit list length.  The actual
	// DataElem array follows this 8 byte header, followed by any data for the list.
	internal enum LIST_TYPE : uint {
		ARRAY  	 = 1 << 24,
		OBJECT 	 = 2 << 24
	}

	// 3 bit data type used in DataElem to indicate the type of element.
	internal enum DATA_TYPE : uint {
		STRING 	 = 0,				// String stored in data in "short16" + chars format (not null terminated)
		INT 	 = 1,				// 32bit int stored in data
		FLOAT 	 = 2,				// 32bit double stored in data
		DOUBLE 	 = 3,				// 32bit double stored in data
		TRUE 	 = 4,				// True (no data)
		FALSE 	 = 5,				// False (no data)
		NULL 	 = 6,				// Null (no data)
		OBJARRAY = 7				// 32 bit relative pointer to object or array
	}

	// Document object for binary json documents.  Stores the C# string cache for the document, plus data pointer.
	public unsafe class BinJsonDocument
	{
		internal byte* data;							// The static data buffer
		internal StringTableElem* keyStringTable;		// Pointer to the stringtable offset array in the static data buffer
		internal Dictionary<uint, string> keyStringsByCrc; // Strings by crc hash

		internal Vector<string> valueStringCache;
		internal string[] valueStringCacheArray;

		public BinJsonDocument()
		{
			valueStringCache = new Vector<string> ();
			// Make sure we have something at element 0 so it's not used.
			valueStringCache.push ("");
		}

		public int Size {
			get {
				return *(int*)(data + 4) + 8;
			}
		}

		public int KeyTableCount { 
			get {
				// Location of string table count is 20
				return *(int*)(data + 20);
			}
		}

		public Dictionary<uint, string> KeyTable {
			get {
				if (keyStringsByCrc == null) {
					keyStringsByCrc = new Dictionary<uint, string> ();
					int len = KeyTableCount;
					for (var i = 0; i < len; i++) {
						StringTableElem* strElem = (StringTableElem*)(keyStringTable + i);
						uint crc = strElem->crc;
						if (crc != 0) {
							string str = Marshal.PtrToStringAnsi (new IntPtr (data + strElem->offset));
							keyStringsByCrc.Add (crc, str);
						}
					}
				}
				return keyStringsByCrc;
			}
		}

		public byte[] ToArray() 
		{
			byte[] array = new byte[this.Size];
			uint* s = (uint*)this.data;
			fixed (byte* a = &array[0]) {
				uint* d = (uint*)a;
				int size = this.Size;
				for (var i = 0; i < size; i += 4)
					*d++ = *s++;
			}
			return array;
		}

		#if DEBUG

		public byte[] Data { 
			get {
				byte[] d = new byte[200];
				for (var i = 0; i < 200; i++) 
					d [i] = data [i];
				return d;
			}
		}

		public string[] ValueStringCache { 
			get {
				return valueStringCacheArray;
			}
		}

		public uint[] KeyStringTableOffsets { 
			get {
				uint[] d = new uint[KeyTableCount];
				for (var i = 0; i < KeyTableCount; i++) 
					d [i] = keyStringTable[i].offset;
				return d;
			}
		}

		public string[] KeyStringTable {
			get {
				string[] d = new string[KeyTableCount];
				for (var i = 0; i < KeyTableCount; i++) {
					d [i] = Marshal.PtrToStringAnsi (new IntPtr (data + keyStringTable[i].offset));
				}
				return d;
			}
		}

		#endif

	}

	[DebuggerDisplay("{value}", Name = "{key}", Type = "{ValueTypeName}")]
	internal class KeyValuePairDebugView
	{
		public string key   {get { return _key; }}
		public object value 
		{
			get { 
				return ((IGetMemberProvider<object>)_binObj).GetMember(_key);
			}
			set { }
		}

		public KeyValuePairDebugView(BinJsonObject binObj, string key)
		{
			_binObj = binObj;
			_key = key;
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public string ValueTypeName
		{
			get {
				var v = value;
				if (v != null) {
					return v.GetType().Name;
				} else {
					return "";
				}
			}
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private readonly BinJsonObject _binObj;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private readonly string        _key;
	}

	internal class BinJsonObjectDebugView
	{
		private BinJsonObject binObj;

		public BinJsonObjectDebugView(BinJsonObject binObj)
		{
			this.binObj = binObj;
		}

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public KeyValuePairDebugView[] Keys
		{
			get
			{
				var keys = new KeyValuePairDebugView[binObj.Count];

				int i = 0;
				foreach(string key in binObj.KeyPairs.keys)
				{
					keys[i] = new KeyValuePairDebugView(binObj, key);
					i++;
				}
				return keys;
			}
		}
	}

	// Used to implement "Object" semantics for binary json document.  Implements fast versions of dynamic get index and dynamic
	// get member that accellerate the dynamic runtime.
	[DebuggerDisplay ("Count = {Count}")]
	[DebuggerTypeProxy (typeof (BinJsonObjectDebugView))]
	public unsafe class BinJsonObject : 
		IGetIndexProvider<string>, IGetIndexProvider<int>, IGetIndexProvider<uint>, IGetIndexProvider<double>, IGetIndexProvider<bool>, IGetIndexProvider<object>,
		IGetMemberProvider<string>, IGetMemberProvider<int>, IGetMemberProvider<uint>, IGetMemberProvider<double>, IGetMemberProvider<bool>, IGetMemberProvider<object>,
		IKeyEnumerable
	{
		protected BinJsonDocument doc;
		protected byte* list;
		protected WeakReference<KeyCrcPairs> keyPairs;

		public BinJsonObject(BinJsonDocument doc, byte* list) {
			this.doc = doc;
			this.list = list;
		}

		public int Count {
			get { return (int)(*(uint*)(list + 4) & 0xFFFFFF); }
		}

		public BinJsonDocument Document {
			get { return doc; }
		}

		internal KeyCrcPairs KeyPairs {
			get {
				KeyCrcPairs pairs;
				if (keyPairs == null || keyPairs.TryGetTarget(out pairs) == false) {
					int len = Count;
					pairs = new KeyCrcPairs ();
					pairs.length = len;
					string[] keyArray = new string[len];
					pairs.keys = keyArray;
					uint[] crcArray = new uint[len];
					pairs.crcs = crcArray;
					Dictionary<uint,string> crcStrTable = doc.KeyTable;
					DataElem* dataElem = (DataElem*)(list + 8);
					for (var i = 0; i < len; i++) {
						uint crc = dataElem->id & 0x1FFFFFFF;
						string key = crcStrTable[crc];
						keyArray [i] = key;
						crcArray [i] = crc;
						dataElem++;
					}
					if (keyPairs == null)
						keyPairs = new WeakReference<KeyCrcPairs> (pairs);
					else 
						keyPairs.SetTarget (pairs);
				}
				return pairs;
			}
		}

		#region IKeyEnumerable implementation

		private class CrcKeyEnumerator : IEnumerator {

			private KeyCrcPairs _pairs;
			private int _index;

			public CrcKeyEnumerator(KeyCrcPairs pairs) {
				_pairs = pairs;
				_index = -1;
			}

			#region IEnumerator implementation

			public bool MoveNext ()
			{
				if (_index + 1 >= _pairs.length)
					return false;
				_index++;
				return true;
			}

			public void Reset ()
			{
				_index = -1;
			}

			public object Current {
				get {
					string key;
					// We store the last string and crc in the PSGetIndex so it can short path to using them during a loop over
					// keys in an object.
					PlayScript.DynamicRuntime.PSGetIndex.LastKeyString = key = _pairs.keys [_index];
					PlayScript.DynamicRuntime.PSGetIndex.LastKeyCrc = _pairs.crcs [_index];
					return key;
				}
			}

			#endregion
		}

		IEnumerator IKeyEnumerable.GetKeyEnumerator ()
		{
			return new CrcKeyEnumerator (this.KeyPairs);
		}

		#endregion

		#if DEBUG

		public int Ptr { get { return (int)(list - doc.data); } }

		public byte[] Data { 
			get {
				byte[] data = new byte[100];
				for (var i = 0; i < 100; i++) 
					data [i] = list [i];
				return data;
			}
		}

		#endif

		#region GetValue Methods

		private static object _falseObject = false;
		private static object _trueObject = true;
		private static object _zeroDoubleObject = 0.0;
		private static object _zeroIntObject = 0;

		internal string GetValueString(DATA_TYPE elemType, byte* data, DataElem* dataElem)
		{
			switch (elemType) {
			case DATA_TYPE.STRING:
				byte* ptr = data + dataElem->offset;
				short stridx = *(short*)ptr;
				if (stridx >= 0) {
					string s = Marshal.PtrToStringAnsi (new IntPtr(ptr + 2), stridx);
					var stringCache = doc.valueStringCache;
					stringCache.push (s);
					doc.valueStringCacheArray = stringCache._GetInnerArray ();
					*(short*)ptr = (short)-(stringCache.length - 1);
					return s;
				} else {
					return doc.valueStringCacheArray [-stridx];
				}
				case DATA_TYPE.INT:
				return dataElem->intValue.ToString();
				case DATA_TYPE.FLOAT:
				return dataElem->floatValue.ToString();
				case DATA_TYPE.DOUBLE:
				return (*(double*)(data + dataElem->offset)).ToString();
				case DATA_TYPE.TRUE:
				return "true";
				case DATA_TYPE.FALSE:
				return "false";
				case DATA_TYPE.NULL:
				return "null";
				case DATA_TYPE.OBJARRAY:
				return null;
			}
			return null;
		}

		internal int GetValueInt(DATA_TYPE elemType, byte* data, DataElem* dataElem)
		{
			int i;
			switch (elemType) {
				case DATA_TYPE.STRING:
				int.TryParse (GetValueString (elemType, data, dataElem), out i);
				return i;
				case DATA_TYPE.INT:
				return dataElem->intValue;
				case DATA_TYPE.FLOAT:
				return (int)dataElem->floatValue;
				case DATA_TYPE.DOUBLE:
				return (int)*(double*)(data + dataElem->offset);
				case DATA_TYPE.TRUE:
				return 1;
				case DATA_TYPE.FALSE:
				case DATA_TYPE.NULL:
				case DATA_TYPE.OBJARRAY:
				return 0;
			}
			return 0;
		}

		internal uint GetValueUInt(DATA_TYPE elemType, byte* data, DataElem* dataElem)
		{
			uint i;
			switch (elemType) {
				case DATA_TYPE.STRING:
				uint.TryParse (GetValueString (elemType, data, dataElem), out i);
				return i;
				case DATA_TYPE.INT:
				return dataElem->offset;
				case DATA_TYPE.FLOAT:
				return (uint)dataElem->floatValue;
				case DATA_TYPE.DOUBLE:
				return (uint)*(double*)(data + dataElem->offset);
				case DATA_TYPE.TRUE:
				return 1u;
				case DATA_TYPE.FALSE:
				case DATA_TYPE.NULL:
				case DATA_TYPE.OBJARRAY:
				return 0u;
			}
			return 0;
		}

		internal double GetValueDouble(DATA_TYPE elemType, byte* data, DataElem* dataElem)
		{
			double d;
			switch (elemType) {
				case DATA_TYPE.STRING:
				double.TryParse (GetValueString (elemType, data, dataElem), out d);
				return d;
				case DATA_TYPE.INT:
				return (double)dataElem->intValue;
				case DATA_TYPE.FLOAT:
				return (double)dataElem->floatValue;
				case DATA_TYPE.DOUBLE:
				return *(double*)(data + dataElem->offset);
				case DATA_TYPE.TRUE:
				return 1.0;
				case DATA_TYPE.FALSE:
				return 0.0;
				case DATA_TYPE.NULL:
				case DATA_TYPE.OBJARRAY:
				return double.NaN;
			}
			return double.NaN;
		}

		internal bool GetValueBool(DATA_TYPE elemType, byte* data, DataElem* dataElem)
		{
			switch (elemType) {
				case DATA_TYPE.STRING:
				string s = GetValueString (elemType, data, dataElem);
				if (s == "1" || s == "true")
					return true;
				return false;
				case DATA_TYPE.INT:
				return dataElem->intValue != 0;
				case DATA_TYPE.FLOAT:
				return dataElem->floatValue != 0.0f;
				case DATA_TYPE.DOUBLE:
				return *(double*)(data + dataElem->offset) != 0.0;
				case DATA_TYPE.TRUE:
				return true;
				case DATA_TYPE.FALSE:
				return false;
				case DATA_TYPE.NULL:
				case DATA_TYPE.OBJARRAY:
				return false;
			}
			return false;
		}

		internal object GetValueObject(DATA_TYPE elemType, byte* data, DataElem* dataElem)
		{
			switch (elemType) {
				case DATA_TYPE.STRING:
				return GetValueString (elemType, data, dataElem);
				case DATA_TYPE.INT:
				int i = dataElem->intValue;
				return (i == 0) ? _zeroIntObject : (object)i;
				case DATA_TYPE.FLOAT:
				float f = dataElem->floatValue;
				return (f == 0.0f) ? _zeroDoubleObject : (object)(double)f;
				case DATA_TYPE.DOUBLE:
				double d = *(double*)(data + dataElem->offset);
				return (d == 0.0) ? _zeroDoubleObject : (object)d;
				case DATA_TYPE.TRUE:
				return _trueObject;
				case DATA_TYPE.FALSE:
				return _falseObject;
				case DATA_TYPE.NULL:
				return null;
				case DATA_TYPE.OBJARRAY:
				byte* list = doc.data + *(uint*)(data + dataElem->offset);
				LIST_TYPE listType = (LIST_TYPE)(*(uint*)(list + 4) & 0xFF000000);
				switch (listType) {
				case LIST_TYPE.OBJECT:
					return new BinJsonObject (doc, list);
				case LIST_TYPE.ARRAY:
					return new _root.Array (new BinJsonArray (doc, list));
				}
				return null;
			}
			return false;
		}

		#endregion

		#region IGetIndexProvider Implementations

		string IGetIndexProvider<string>.GetIndex(int index)
		{
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			if (listType == LIST_TYPE.ARRAY) {
				len &= 0xFFFFFF;
				if (index < len) {
					DataElem* dataElem = ((DataElem*)(list + 8)) + index;
					DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 29);
					return GetValueString (dataType, doc.data + *(uint*)list, dataElem);
				} else {
					return null;
				}
			} else {
				throw new NotSupportedException ();
			}
		}

		int IGetIndexProvider<int>.GetIndex(int index)
		{
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			if (listType == LIST_TYPE.ARRAY) {
				len &= 0xFFFFFF;
				if (index < len) {
					DataElem* dataElem = ((DataElem*)(list + 8)) + index;
					DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 29);
					byte* data = doc.data + *(uint*)list;
					int i;
					switch (dataType) {
						case DATA_TYPE.STRING:
						int.TryParse (GetValueString (dataType, data, dataElem), out i);
						return i;
						case DATA_TYPE.INT:
						return dataElem->intValue;
						case DATA_TYPE.FLOAT:
						return (int)dataElem->floatValue;
						case DATA_TYPE.DOUBLE:
						return (int)*(double*)(data + dataElem->offset);
						case DATA_TYPE.TRUE:
						return 1;
						case DATA_TYPE.FALSE:
						case DATA_TYPE.NULL:
						case DATA_TYPE.OBJARRAY:
						return 0;
					}
					return 0;
				} else {
					return 0;
				}
			} else {
				throw new NotSupportedException ();
			}
		}

		double IGetIndexProvider<double>.GetIndex(int index)
		{
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			if (listType == LIST_TYPE.ARRAY) { 
				len &= 0xFFFFFF;
				if (index < len) {
					DataElem* dataElem = ((DataElem*)(list + 8)) + index;
					DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 29);
					return GetValueDouble (dataType, doc.data + *(uint*)(list), dataElem);
				} else {
					return 0.0;
				}
			} else {
				throw new NotSupportedException ();
			}
		}

		uint IGetIndexProvider<uint>.GetIndex(int index)
		{
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			if (listType == LIST_TYPE.ARRAY) {
				len &= 0xFFFFFF;
				if (index < len) {
					DataElem* dataElem = ((DataElem*)(list + 8)) + index;
					DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 29);
					return GetValueUInt (dataType, doc.data + *(uint*)list, dataElem);
				} else {
					return 0u;
				}
			} else {
				throw new NotSupportedException ();
			}
		}

		bool IGetIndexProvider<bool>.GetIndex(int index)
		{
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			if (listType == LIST_TYPE.ARRAY) {
				len &= 0xFFFFFF;
				if (index < len) {
					DataElem* dataElem = ((DataElem*)(list + 8)) + index;
					DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 29);
					return GetValueBool (dataType, doc.data + *(uint*)list, dataElem);
				} else {
					return false;
				}
			} else {
				throw new NotSupportedException ();
			}
		}

		object IGetIndexProvider<object>.GetIndex(int index)
		{
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			if (listType == LIST_TYPE.ARRAY) {
				len &= 0xFFFFFF;
				if (index < len) {
					DataElem* dataElem = ((DataElem*)(list + 8)) + index;
					DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 29);
					return GetValueObject (dataType, doc.data + *(uint*)list, dataElem);
				} else {
					return null;
				}
			} else {
				throw new NotSupportedException ();
			}
		}

		#endregion

		#region IGetMemberProvider Implementations

		string IGetMemberProvider<string>.GetMember(uint crc)
		{
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			len &= 0xFFFFFF;
			if (listType == LIST_TYPE.OBJECT && len > 0) {
				DataElem* firstElem = (DataElem*)(list + 8);
				DataElem* dataElem = firstElem + (crc % len);
				if ((dataElem->id & 0x1FFFFFFF) != crc) {
					DataElem* lastElem = firstElem + len;
					DataElem* startElem = dataElem;
					DataElem* curElem = dataElem;
					while (true) {
						curElem++;
						if (curElem >= lastElem)
							curElem = firstElem;
						if (curElem == startElem)
							return null;
						if ((curElem->id & 0x1FFFFFFF) == crc) {
							dataElem = curElem;
							break;
						}
					}
				}
				DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 29);
				return GetValueString (dataType, doc.data + *(uint*)list, dataElem);
			} else {
				return null;
			}
		}

		string IGetMemberProvider<string>.GetMember(string key)
		{
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			if (listType == LIST_TYPE.OBJECT) {
				if (System.Object.ReferenceEquals (key, PlayScript.DynamicRuntime.PSGetIndex.LastKeyString))
					return ((IGetMemberProvider<string>)this).GetMember (PlayScript.DynamicRuntime.PSGetIndex.LastKeyCrc);
				else {
					uint crc = BinJSonCrc32.Calculate (key) & 0x1FFFFFFF;
					PlayScript.DynamicRuntime.PSGetIndex.LastKeyString = key;
					PlayScript.DynamicRuntime.PSGetIndex.LastKeyCrc = crc;
					return ((IGetMemberProvider<string>)this).GetMember (crc);
				}
			} else {
				int i;
				if (int.TryParse (key, out i)) 
					return ((IGetIndexProvider<string>)this).GetIndex (i);
				else
					return null;
			}
		}

		int IGetMemberProvider<int>.GetMember(uint crc)
		{
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			len &= 0xFFFFFF;
			if (listType == LIST_TYPE.OBJECT && len > 0) {
				DataElem* firstElem = (DataElem*)(list + 8);
				DataElem* dataElem = firstElem + (crc % len);
				if ((dataElem->id & 0x1FFFFFFF) != crc) {
					DataElem* lastElem = firstElem + len;
					DataElem* startElem = dataElem;
					DataElem* curElem = dataElem;
					while (true) {
						curElem++;
						if (curElem >= lastElem)
							curElem = firstElem;
						if (curElem == startElem)
							return 0;
						if ((curElem->id & 0x1FFFFFFF) == crc) {
							dataElem = curElem;
							break;
						}
					}
				}
				DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 29);
				byte* data = doc.data + *(uint*)list;
				int i;
				switch (dataType) {
				case DATA_TYPE.STRING:
					int.TryParse (GetValueString (dataType, data, dataElem), out i);
					return i;
				case DATA_TYPE.INT:
					return dataElem->intValue;
				case DATA_TYPE.FLOAT:
					return (int)dataElem->floatValue;
				case DATA_TYPE.DOUBLE:
					return (int)*(double*)(data + dataElem->offset);
				case DATA_TYPE.TRUE:
					return 1;
				case DATA_TYPE.FALSE:
				case DATA_TYPE.NULL:
				case DATA_TYPE.OBJARRAY:
					return 0;
				}
				return 0;
			} else {
				return 0;
			}
		}

		int IGetMemberProvider<int>.GetMember(string key)
		{
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			if (listType == LIST_TYPE.OBJECT) {
				if (System.Object.ReferenceEquals (key, PlayScript.DynamicRuntime.PSGetIndex.LastKeyString))
					return ((IGetMemberProvider<int>)this).GetMember (PlayScript.DynamicRuntime.PSGetIndex.LastKeyCrc);
				else {
					uint crc = BinJSonCrc32.Calculate (key) & 0x1FFFFFFF;
					PlayScript.DynamicRuntime.PSGetIndex.LastKeyString = key;
					PlayScript.DynamicRuntime.PSGetIndex.LastKeyCrc = crc;
					return ((IGetMemberProvider<int>)this).GetMember (crc);
				}
			} else {
				int i;
				if (int.TryParse (key, out i)) 
					return ((IGetIndexProvider<int>)this).GetIndex (i);
				else
					return 0;
			}
		}

		double IGetMemberProvider<double>.GetMember(uint crc)
		{
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			len &= 0xFFFFFF;
			if (listType == LIST_TYPE.OBJECT && len > 0) {
				DataElem* firstElem = (DataElem*)(list + 8);
				DataElem* dataElem = firstElem + (crc % len);
				if ((dataElem->id & 0x1FFFFFFF) != crc) {
					DataElem* lastElem = firstElem + len;
					DataElem* startElem = dataElem;
					DataElem* curElem = dataElem;
					while (true) {
						curElem++;
						if (curElem >= lastElem)
							curElem = firstElem;
						if (curElem == startElem)
							return 0.0;
						if ((curElem->id & 0x1FFFFFFF) == crc) {
							dataElem = curElem;
							break;
						}
					}
				}
				if (dataElem != null) {
					DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 29);
					return GetValueDouble (dataType, doc.data + *(uint*)list, dataElem);
				} else {
					return 0.0;
				}
			} else {
				return 0.0;
			}
		}

		double IGetMemberProvider<double>.GetMember(string key)
		{
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			if (listType == LIST_TYPE.OBJECT) {
				if (System.Object.ReferenceEquals (key, PlayScript.DynamicRuntime.PSGetIndex.LastKeyString))
					return ((IGetMemberProvider<double>)this).GetMember (PlayScript.DynamicRuntime.PSGetIndex.LastKeyCrc);
				else {
					uint crc = BinJSonCrc32.Calculate (key) & 0x1FFFFFFF;
					PlayScript.DynamicRuntime.PSGetIndex.LastKeyString = key;
					PlayScript.DynamicRuntime.PSGetIndex.LastKeyCrc = crc;
					return ((IGetMemberProvider<double>)this).GetMember (crc);
				}
			} else {
				int i;
				if (int.TryParse (key, out i)) 
					return ((IGetIndexProvider<double>)this).GetIndex (i);
				else
					return 0.0;
			}
		}

		uint IGetMemberProvider<uint>.GetMember(uint crc)
		{
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			len &= 0xFFFFFF;
			if (listType == LIST_TYPE.OBJECT && len > 0) {
				DataElem* firstElem = (DataElem*)(list + 8);
				DataElem* dataElem = firstElem + (crc % len);
				if ((dataElem->id & 0x1FFFFFFF) != crc) {
					DataElem* lastElem = firstElem + len;
					DataElem* startElem = dataElem;
					DataElem* curElem = dataElem;
					while (true) {
						curElem++;
						if (curElem >= lastElem)
							curElem = firstElem;
						if (curElem == startElem)
							return 0u;
						if ((curElem->id & 0x1FFFFFFF) == crc) {
							dataElem = curElem;
							break;
						}
					}
				}
				if (dataElem != null) {
					DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 29);
					return GetValueUInt (dataType, doc.data + *(uint*)list, dataElem);
				} else {
					return 0u;
				}
			} else {
				return 0u;
			}
		}

		uint IGetMemberProvider<uint>.GetMember(string key)
		{
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			if (listType == LIST_TYPE.OBJECT) {
				if (System.Object.ReferenceEquals (key, PlayScript.DynamicRuntime.PSGetIndex.LastKeyString))
					return ((IGetMemberProvider<uint>)this).GetMember (PlayScript.DynamicRuntime.PSGetIndex.LastKeyCrc);
				else {
					uint crc = BinJSonCrc32.Calculate (key) & 0x1FFFFFFF;
					PlayScript.DynamicRuntime.PSGetIndex.LastKeyString = key;
					PlayScript.DynamicRuntime.PSGetIndex.LastKeyCrc = crc;
					return ((IGetMemberProvider<uint>)this).GetMember (crc);
				}
			} else {
				int i;
				if (int.TryParse (key, out i)) 
					return ((IGetIndexProvider<uint>)this).GetIndex (i);
				else
					return 0u;
			}
		}

		bool IGetMemberProvider<bool>.GetMember(uint crc)
		{
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			len &= 0xFFFFFF;
			if (listType == LIST_TYPE.OBJECT && len > 0) {
				DataElem* firstElem = (DataElem*)(list + 8);
				DataElem* dataElem = firstElem + (crc % len);
				if ((dataElem->id & 0x1FFFFFFF) != crc) {
					DataElem* lastElem = firstElem + len;
					DataElem* startElem = dataElem;
					DataElem* curElem = dataElem;
					while (true) {
						curElem++;
						if (curElem >= lastElem)
							curElem = firstElem;
						if (curElem == startElem)
							return false;
						if ((curElem->id & 0x1FFFFFFF) == crc) {
							dataElem = curElem;
							break;
						}
					}
				}
				if (dataElem != null) {
					DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 29);
					return GetValueBool (dataType, doc.data + *(uint*)list, dataElem);
				} else {
					return false;
				}
			} else {
				return false;
			}
		}

		bool IGetMemberProvider<bool>.GetMember(string key)
		{
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			if (listType == LIST_TYPE.OBJECT) {
				if (System.Object.ReferenceEquals (key, PlayScript.DynamicRuntime.PSGetIndex.LastKeyString))
					return ((IGetMemberProvider<bool>)this).GetMember (PlayScript.DynamicRuntime.PSGetIndex.LastKeyCrc);
				else {
					uint crc = BinJSonCrc32.Calculate (key) & 0x1FFFFFFF;
					PlayScript.DynamicRuntime.PSGetIndex.LastKeyString = key;
					PlayScript.DynamicRuntime.PSGetIndex.LastKeyCrc = crc;
					return ((IGetMemberProvider<bool>)this).GetMember (crc);
				}
			} else {
				int i;
				if (int.TryParse (key, out i)) 
					return ((IGetIndexProvider<bool>)this).GetIndex (i);
				else
					return false;
			}
		}

		object IGetMemberProvider<object>.GetMember(uint crc)
		{
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			len &= 0xFFFFFF;
			if (listType == LIST_TYPE.OBJECT && len > 0) {
				DataElem* firstElem = (DataElem*)(list + 8);
				DataElem* dataElem = firstElem + (crc % len);
				if ((dataElem->id & 0x1FFFFFFF) != crc) {
					DataElem* lastElem = firstElem + len;
					DataElem* startElem = dataElem;
					DataElem* curElem = dataElem;
					while (true) {
						curElem++;
						if (curElem >= lastElem)
							curElem = firstElem;
						if (curElem == startElem)
							return null;
						if ((curElem->id & 0x1FFFFFFF) == crc) {
							dataElem = curElem;
							break;
						}
					}
				}
				DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 29);
				switch (dataType) {
				case DATA_TYPE.STRING:
					return GetValueString (dataType, doc.data + *(uint*)list, dataElem);
				case DATA_TYPE.INT:
					int i = dataElem->intValue;
					return (i == 0) ? _zeroIntObject : (object)i;
				case DATA_TYPE.FLOAT:
					float f = dataElem->floatValue;
					return (f == 0.0f) ? _zeroDoubleObject : (object)(double)f;
				case DATA_TYPE.DOUBLE:
					double d = *(double*)(doc.data + *(uint*)list + dataElem->offset);
					return (d == 0.0) ? _zeroDoubleObject : (object)d;
				case DATA_TYPE.TRUE:
					return _trueObject;
				case DATA_TYPE.FALSE:
					return _falseObject;
				case DATA_TYPE.NULL:
					return null;
				case DATA_TYPE.OBJARRAY:
					byte* childList = doc.data + *(uint*)(doc.data + *(uint*)list + dataElem->offset);
					LIST_TYPE childListType = (LIST_TYPE)(*(uint*)(childList + 4) & 0xFF000000);
					switch (childListType) {
					case LIST_TYPE.OBJECT:
						return new BinJsonObject (doc, list);
					case LIST_TYPE.ARRAY:
						return new _root.Array (new BinJsonArray (doc, list));
					}
					return null;
				}
				return null;
			} else {
				return null;
			}
		}

		object IGetMemberProvider<object>.GetMember(string key)
		{
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			if (listType == LIST_TYPE.OBJECT) {
				if (System.Object.ReferenceEquals (key, PlayScript.DynamicRuntime.PSGetIndex.LastKeyString))
					return ((IGetMemberProvider<object>)this).GetMember (PlayScript.DynamicRuntime.PSGetIndex.LastKeyCrc);
				else {
					uint crc = BinJSonCrc32.Calculate (key) & 0x1FFFFFFF;
					PlayScript.DynamicRuntime.PSGetIndex.LastKeyString = key;
					PlayScript.DynamicRuntime.PSGetIndex.LastKeyCrc = crc;
					return ((IGetMemberProvider<object>)this).GetMember (crc);
				}
			} else {
				int i;
				if (int.TryParse (key, out i)) 
					return ((IGetIndexProvider<object>)this).GetIndex (i);
				else
					return null;
			}
		}

		#endregion

	}

	//
	// Implements IStaticArray interface for BinJSON static data buffers.  Allows Array objects to
	// use this binary data as it's backing store.
	//

	internal unsafe class BinJsonArray : BinJsonObject, IStaticArray, IEnumerable
	{
		public BinJsonArray(BinJsonDocument doc, byte* list) : base(doc, list) {
		}

		#region IStaticArray implementation

		public string getStringAt(uint index)
		{
			uint len = *(uint*)(list + 4) & 0xFFFFFF;
			if (index < len) {
				DataElem* dataElem = ((DataElem*)(list + 8)) + index;
				DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 29);
				return GetValueString(dataType, doc.data + *(int*)list, dataElem);
			} else {
				return null;
			}
		}

		public int getIntAt(uint index)
		{
			uint len = *(uint*)(list + 4) & 0xFFFFFF;
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			if (index < len) {
				DataElem* dataElem = ((DataElem*)(list + 8)) + index;
				DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 29);
				return GetValueInt(dataType, doc.data + *(int*)list, dataElem);
			} else {
				return 0;
			}
		}

		public double getDoubleAt(uint index)
		{
			uint len = *(uint*)(list + 4) & 0xFFFFFF;
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			if (index < len) {
				DataElem* dataElem = ((DataElem*)(list + 8)) + index;
				DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 29);
				return GetValueDouble(dataType, doc.data + *(int*)list, dataElem);
			} else {
				return double.NaN;
			}
		}

		public uint getUIntAt(uint index)
		{
			uint len = *(uint*)(list + 4) & 0xFFFFFF;
			if (index < len) {
				DataElem* dataElem = ((DataElem*)(list + 8)) + index;
				DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 29);
				return GetValueUInt(dataType, doc.data + *(int*)list, dataElem);
			} else {
				return 0u;
			}
		}

		public bool getBoolAt(uint index)
		{
			uint len = *(uint*)(list + 4) & 0xFFFFFF;
			if (index < len) {
				DataElem* dataElem = ((DataElem*)(list + 8)) + index;
				DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 29);
				return GetValueBool(dataType, doc.data + *(int*)list, dataElem);
			} else {
				return false;
			}
		}

		public object getObjectAt(uint index)
		{
			uint len = *(uint*)(list + 4) & 0xFFFFFF;
			if (index < len) {
				DataElem* dataElem = ((DataElem*)(list + 8)) + index;
				DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 29);
				return GetValueObject(dataType, doc.data + *(int*)list, dataElem);
			} else {
				return null;
			}
		}

		public uint length {
			get {
				return *(uint*)(list + 4) & 0xFFFFFF;
			}
		}

		#endregion

		#region IEnumerable implementation

		public class BinJsonArrayEnumerator : IEnumerator 
		{
			private BinJsonArray _array;
			private int _index = -1;

			public BinJsonArrayEnumerator(BinJsonArray array)
			{
				_array = array;
			}

			#region IEnumerator implementation

			public bool MoveNext ()
			{
				int len = (int)_array.length;
				if (_index < len) {
					_index++;
				}
				return _index < len;
			}

			public void Reset ()
			{
				_index = -1;
			}

			public object Current {
				get {
					return _array.getObjectAt ((uint)_index);
				}
			}

			#endregion
		}

		public IEnumerator GetEnumerator ()
		{
			throw new NotImplementedException ();
		}

		#endregion
	}

	public static class NumberSize
	{
		public const string Float32 = "float32";
		public const string Float64 = "float64";
	}

	/// <summary>
	/// This class encodes and decodes JSON strings.
	/// Spec. details, see http://www.json.org/
	///
	/// JSON uses Arrays and Objects. These correspond here to the datatypes IList and IDictionary.
	/// All numbers are parsed to doubles.
	/// </summary>
	public unsafe static class BinJSON {

		internal static string _numberSize = NumberSize.Float32;
		internal static bool _useFloat32 = true;
		internal static bool _preserveOrder = false;

		public static dynamic parse(string json) {
			// save the string for debug information
			if (json == null) {
				throw new ArgumentNullException ("json");
			}

			return Parser.Parse(json);
		}

		public static dynamic fromBinary(flash.utils.ByteArray buffer) {
			// TODO: Implement instantiating a binary json tree from the buffer.
			throw new NotImplementedException ();
		}

		/// <summary>
		/// Gets or sets the size of the number to use when storing floating point values.
		/// </summary>
		/// <value>The size of the number either NumberSize.Float32 or NumberSize.Float64.</value>
		public static string numberSize {
			get { 
				return _numberSize;
			}
			set {
				_numberSize = value;
				_useFloat32 = _numberSize == NumberSize.Float32;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether to preserve the key order in objects.
		/// </summary>
		/// <value><c>true</c> to preserve key order in objects <c>false</c> to use crc hash order for fast value lookups.</value>
		public static bool preserveOrder {
			get {
				return _preserveOrder;
			}
			set {
				_preserveOrder = value;
			}
		}

		private sealed unsafe class Parser : IDisposable {

			// Maximum number of unique key strings
			public const int MAX_NUM_UNIQUE_KEYS = 0x1FFFFFFF;  // Top 3 bits reserved for data type.. 8192 unique key strings

			// Default pointer size
			public const int OFFSET_SIZE = sizeof(uint);

			// Size increment to grow buffers with
			public const int GROW_SIZE = 0x10000; // 64K

			// Don't double past this size for data mem
			public const int MAX_DOUBLE_SIZE = 1 * 1024 * 1024;

			// Default ratio of the binary data buffer to allocate to the JSON string size
			public const double JSON_TO_BINARY_SIZE_RATIO_FLOAT64 = 0.7;

			// Default ratio of the binary data buffer to allocate to the JSON string size
			public const double JSON_TO_BINARY_SIZE_RATIO_FLOAT32 = 0.6;

			// Initial TEMP buffer size
			public const int INITIAL_TEMP_BUF_SIZE = GROW_SIZE;

			// Initial TEMP DATA buffer size
			public const int INITIAL_TEMP_DATA_BUF_SIZE = GROW_SIZE;

			// Initial TEMP string buffer size
			public const int INITIAL_TEMP_STRING_BUF_SIZE = GROW_SIZE * 2;

			enum TOKEN {
				NONE,
				CURLY_OPEN,
				CURLY_CLOSE,
				SQUARED_OPEN,
				SQUARED_CLOSE,
				COLON,
				COMMA,
				STRING,
				NUMBER,
				TRUE,
				FALSE,
				NULL
			};

			// The input string buffer (in 16 bit chars) that we're parsing.
			GCHandle gch;
			char* json;
			char* end;

			// The output binary buffer - this buffer holds no pointers and is location independent (only reallocates if we guessed the wrong initial size)
			byte* data;
			byte* dataEnd;
			byte* dataPtr;
			int dataSize;

			// Temporarily stores object and array DataElem arrays until they are copied to "data" (usually shouldn't reallocate)
			public static byte* temp;
			public static byte* tempEnd;
			public static byte* tempPtr;
			public static int tempSize;

			// Stores data values like pointers, doubles, etc. until they are copied to "data" (usually shouldn't reallocate)
			public static byte* tempData;
			public static byte* tempDataPtr;
			public static byte* tempDataEnd;
			public static int tempDataSize;

			// Stores strings until they are copied to "data" (usually shouldn't reallocate)
			public static byte* tempString;
			public static byte* tempStringPtr;
			public static byte* tempStringEnd;
			public static int tempStringSize;

			// Float ptr memory
			public static byte* floatConvMem;

			// Unique string dictionary
			UniqueStringDictionary stringTable = new UniqueStringDictionary();

			Parser(string jsonString) 
			{
				gch = GCHandle.Alloc(jsonString, GCHandleType.Pinned);
				json = (char*)gch.AddrOfPinnedObject().ToPointer();
				end = json + jsonString.Length;

				double sizeRatio = _useFloat32 ? JSON_TO_BINARY_SIZE_RATIO_FLOAT32 : JSON_TO_BINARY_SIZE_RATIO_FLOAT64;

				dataSize = (int)((jsonString.Length * sizeRatio + GROW_SIZE - 1)  / GROW_SIZE ) * GROW_SIZE;
				data = (byte*)Marshal.AllocHGlobal(dataSize).ToPointer();
				dataEnd = data + dataSize;
				dataPtr = data;

				// Allocate the temp buffer if we haven't yet
				if (temp == null) {
					tempSize = INITIAL_TEMP_BUF_SIZE;
					temp = (byte*)Marshal.AllocHGlobal(tempSize).ToPointer();
					tempEnd = temp + tempSize;
				}
				tempPtr = temp;

				// Allocate the temp data buffer if we haven't yet
				if (tempData == null) {
					tempDataSize = INITIAL_TEMP_DATA_BUF_SIZE;
					tempData = (byte*)Marshal.AllocHGlobal(tempDataSize).ToPointer();
					tempDataEnd = tempData + tempDataSize;
				}
				tempDataPtr = tempData;

				// Allocate the temp data buffer if we haven't yet
				if (tempString == null) {
					tempStringSize = INITIAL_TEMP_STRING_BUF_SIZE;
					tempString = (byte*)Marshal.AllocHGlobal(tempStringSize).ToPointer();
					tempStringEnd = tempString + tempStringSize;
				}
				tempStringPtr = tempString;

				// We need a few bytes to do float to uint conversions
				if (floatConvMem == null) {
					floatConvMem = (byte*)Marshal.AllocHGlobal(16).ToPointer();
				}

			}

			public static object Parse(string jsonString) {
				using (var instance = new Parser(jsonString)) {
					if (BinJSonCrc32.Table == null)
						BinJSonCrc32.InitializeTable ();
					instance.Parse();
					BinJsonDocument doc = new BinJsonDocument ();
					doc.data = instance.data;
					doc.keyStringTable = (StringTableElem*)(instance.data + *(uint*)(instance.data + 16));
					return new BinJsonObject (doc, doc.data + *(uint*)(doc.data + 12));
				}
			}

			public void Dispose() {
				gch.Free ();
				json = null;
			}

			#if DEBUG

			//
			// Some properties for figuring out what's going on inside the binary data
			//

			public char Ch { get { return *json; } }
			public int DataPos { get { return (int)(dataPtr - data); } }
			public int TempPos { get { return (int)(tempPtr - temp); } }
			public int TempDataPos { get { return (int)(tempDataPtr - tempData); } }

			public byte[] Data { 
				get {
					byte[] d = new byte[100];
					for (var i = 0; i < 100; i++) 
						d [i] = data [i];
					return d;
				}
			}

			public byte[] Temp { 
				get {
					byte[] d = new byte[100];
					for (var i = 0; i < 100; i++) 
						d [i] = temp [i];
					return d;
				}
			}

			public byte[] TempData { 
				get {
					byte[] d = new byte[100];
					for (var i = 0; i < 100; i++) 
						d [i] = tempData [i];
					return d;
				}
			}

			public byte[] TempString { 
				get {
					byte[] d = new byte[100];
					for (var i = 0; i < 100; i++) 
						d [i] = tempString [i];
					return d;
				}
			}

			#endif

			public void MemCopy(void* srcPtr, void* dstPtr, int size)
			{
				if (size == 0)
					return;
				size = size >> 2;
				uint* src = (uint*)srcPtr;
				uint* dst = (uint*)dstPtr;
				for (var i = 0; i < size; i++)
					*dst++ = *src++;
			}

			public void ExpandData()
			{
				int curPos = (int)(dataPtr - data);
				int newSize;
				if (dataSize < MAX_DOUBLE_SIZE)
					newSize = dataSize + dataSize;
				else
					newSize = dataSize + MAX_DOUBLE_SIZE;
				byte* newData = (byte*)Marshal.AllocHGlobal (newSize).ToPointer();
				MemCopy (data, newData, curPos);
				Marshal.FreeHGlobal (new IntPtr(data));
				dataSize = newSize;
				data = newData;
				dataEnd = data + newSize;
				dataPtr = data + curPos;
			}

			public void ExpandTemp()
			{
				int curPos = (int)(tempPtr - temp);
				int newSize = tempSize + GROW_SIZE;
				byte* newtemp = (byte*)Marshal.AllocHGlobal (newSize).ToPointer();
				MemCopy (temp, newtemp, curPos);
				Marshal.FreeHGlobal (new IntPtr(temp));
				tempSize = newSize;
				temp = newtemp;
				tempEnd = temp + newSize;
				tempPtr = temp + curPos;
			}

			public void ExpandTempData()
			{
				int curPos = (int)(tempDataPtr - tempData);
				int newSize = tempDataSize + GROW_SIZE;
				byte* newtempData = (byte*)Marshal.AllocHGlobal (newSize).ToPointer();
				MemCopy (tempData, newtempData, curPos);
				Marshal.FreeHGlobal (new IntPtr(tempData));
				tempDataSize = newSize;
				tempData = newtempData;
				tempDataEnd = tempData + newSize;
				tempDataPtr = tempData + curPos;
			}

			public void ExpandTempString()
			{
				int curPos = (int)(tempStringPtr - tempString);
				int newSize = tempStringSize + GROW_SIZE;
				byte* newtempString = (byte*)Marshal.AllocHGlobal (newSize).ToPointer();
				MemCopy (tempString, newtempString, curPos);
				Marshal.FreeHGlobal (new IntPtr(tempString));
				tempStringSize = newSize;
				tempString = newtempString;
				tempStringEnd = tempString + newSize;
				tempStringPtr = tempString + curPos;
			}

			void CopyToData(void* ptr, int size)
			{
				if (size == 0)
					return;
				while (dataPtr + size >= dataEnd)
					ExpandData ();
				MemCopy (ptr, dataPtr, size);
				dataPtr += size;
			}

			// Copies and also arranges CRC's in hash lookup order
			void HashtableCopyToData(byte* ptr, int size)
			{
				if (size == 0)
					return;
				while (dataPtr + size >= dataEnd)
					ExpandData ();

				// Get table length
				int len = *(int*)(ptr + 4) & 0xFFFFFF;

				// Copy length and offset
				*(uint*)dataPtr = *(uint*)ptr;  // data offset
				dataPtr += 4;
				ptr += 4;
				*(uint*)dataPtr = *(uint*)ptr;  // length & type
				dataPtr += 4;
				ptr += 4;

				// Place hashes in hash lookup order
				if (len > 0) {
					DataElem* firstElem = (DataElem*)dataPtr;
					DataElem* lastElem = firstElem + len;
					DataElem* srcElem = (DataElem*)ptr;
					int i;
					for (i = 0; i < len; i++)
						firstElem[i].id = 0;  // Make sure table is clear
					for (i = 0; i < len; i++) {
						uint crc = srcElem->id & 0x1FFFFFFF;
						uint slot = crc % (uint)len;
						DataElem* startElem = firstElem + slot;
						DataElem* curElem = startElem;
						while (true) {
							if (curElem->id == 0) {
								curElem->id = srcElem->id;
								curElem->offset = srcElem->offset;
								break;
							}
							curElem++;
							if (curElem >= lastElem)
								curElem = firstElem;
							if (curElem == startElem)
								throw new InvalidOperationException ();  // BUG: There should always be at least one empty slot!
						}
						srcElem++;
					}
				}

				dataPtr += size - 8;
			}

			void Parse()
			{
				// Four byte "BINJ" prefix to identify this as a binary json data buffer
				*(byte*)dataPtr++ = (byte)'B';
				*(byte*)dataPtr++ = (byte)'I';
				*(byte*)dataPtr++ = (byte)'N';
				*(byte*)dataPtr++ = (byte)'J';

				// Size
				*(uint*)dataPtr = 0;
				dataPtr += 4;

				// Flags (not used right now - will have byte order, alignment info)
				*(uint*)dataPtr = 0;
				dataPtr += 4;

				// Offset to root structure (offset 12)
				*(uint*)dataPtr = 0;
				dataPtr += 4;

				// Offset to key string table (offset 16)
				*(uint*)dataPtr = 0;
				dataPtr += 4;

				// Size of string table (offset 20)
				*(uint*)dataPtr = 0;
				dataPtr += 4;

				// Parse data
				TOKEN token = GetNextToken ();
				if (token == TOKEN.CURLY_OPEN) {
					ParseObject (0);
				} else if (token == TOKEN.SQUARED_OPEN) {
					ParseArray (0);
				} else {
					throw new InvalidDataException ("Parse error");
				}

				// Should only have a reference to the top root object/array on tempPtr stack
				System.Diagnostics.Debug.Assert ((tempPtr - temp) == 0);

				// Shouldn't have anything left in data stack
				System.Diagnostics.Debug.Assert ((tempDataPtr - tempData) == 4);

				// Last 4 bytes will be offset address of root array/object - we don't need it.
				tempDataPtr -= 4;

				// Copy pointer at end of tempData buffer
				*(uint*)(data + 12) = *(uint*)(tempDataPtr);

				// Align string table to even 4 byte boundry
				while (((uint)dataPtr & 0x3) != 0) 
					*dataPtr++ = 0;

				// Write key string table (and write offset to location at offset 16)
				*(uint*)(data + 16) = (uint)(dataPtr - data);
				CopyToData (tempString, (int)(tempStringPtr - tempString));
				*(uint*)(data + 20) = (uint)(tempStringPtr - tempString) / (uint)sizeof(StringTableElem); // String table length

				// Write final size
				*(uint*)(data + 4) = (uint)(dataPtr - data - 8);  // Size is length of bytes after size field
			}

			uint ParseObject(uint parentStart) 
			{
				if (temp + 16 >= tempEnd)
					ExpandTemp ();

				uint tempStart = (uint)(tempPtr - temp);
				uint tempDataSave = (uint)(tempDataPtr - tempData);

				// Align temp data to 8 byte boundry
				while (((uint)tempDataPtr & 0x7) != 0)
					*tempDataPtr++ = 0;

				uint tempDataStart = (uint)(tempDataPtr - tempData);

				*(int*)tempPtr = 0;
				tempPtr += 4;
				*(uint*)tempPtr = (uint)LIST_TYPE.OBJECT;
				tempPtr += 4;

				int len = 0;
				uint id = 0;
				uint offset = 0;

				bool parsing = true;
				do {
					if (tempPtr + sizeof(DataElem) >= tempEnd)
						ExpandTemp ();

					TOKEN token = GetNextToken();

					switch (token) {
					case TOKEN.NONE:
					case TOKEN.SQUARED_CLOSE:
						throw new InvalidDataException("Parse error");
					case TOKEN.COMMA:
						continue;
					case TOKEN.CURLY_CLOSE:
						parsing = false;
						break;
					default:
						// crcid (name)
						id = ParseKeyString ();

						// :
						token = GetNextToken ();
						if (token != TOKEN.COLON) {
							throw new InvalidOperationException ("Colon expected.");
						}

						token = GetNextToken ();

						// value
						switch (token) {
							case TOKEN.STRING:
							id |= (uint)DATA_TYPE.STRING << 29;
							offset = ParseDataString (tempDataStart);
							break;
							case TOKEN.NUMBER:
							offset = ParseNumber (tempDataStart, ref id);
							break;
							case TOKEN.CURLY_OPEN:
							id |= (uint)DATA_TYPE.OBJARRAY << 29;
							offset = ParseObject (tempDataStart);
							break;
							case TOKEN.SQUARED_OPEN:
							id |= (uint)DATA_TYPE.OBJARRAY << 29;
							offset = ParseArray (tempDataStart);
							break;
							case TOKEN.TRUE:
							id |= (uint)DATA_TYPE.TRUE << 29;
							offset = 0;
							break;
							case TOKEN.FALSE:
							id |= (uint)DATA_TYPE.FALSE << 29;
							offset = 0;
							break;
							default:
							id |= (uint)DATA_TYPE.NULL << 29;
							offset = 0;
							break;
						}

						DataElem* dataElem = (DataElem*)tempPtr;
						dataElem->id = id;
						dataElem->offset = offset;
						tempPtr += sizeof(DataElem);
						len++;

						break;
					}
				} while (parsing);

				// Set length in lower 24 bits (upper 8 bits is type)
				*(int*)(temp + tempStart + 4) |= (int)(len & 0xFFFFFF);

				// Align data ptr to 4 byte boundry
				while (((uint)dataPtr & 0x3) != 0)
					*dataPtr++ = 0;

				// Copy elem array to data buffer
				uint dataStart = (uint)(dataPtr - data);
				if (_preserveOrder)
					CopyToData (temp + tempStart, (int)(tempPtr - (temp + tempStart)));
				else
					HashtableCopyToData (temp + tempStart, (int)(tempPtr - (temp + tempStart)));

				// Align data ptr to 8 byte boundry
				while (((uint)dataPtr & 0x7) != 0)
					*dataPtr++ = 0;

				// Set relative ptr to first data elem
				*(uint*)(data + dataStart) = (uint)(dataPtr - data); 

				// Copy data to data buffer
				CopyToData (tempData + tempDataStart, (int)(tempDataPtr - (tempData + tempDataStart)));

				// Restore current temp stacks
				tempPtr = temp + tempStart;
				tempDataPtr = tempData + tempDataSave;

				// Align temp data to even 4 bytes
				while (((uint)tempDataPtr & 0x3) != 0)
					*tempDataPtr++ = 0;

				// Add relative pointer to this object to parent's data
				*(uint*)tempDataPtr = dataStart;
				tempDataPtr += 4;

				// Return the offset to the pointer
				return (uint)(tempDataPtr - (int)(tempData + parentStart) - 4); 
			}

			uint ParseArray(uint parentStart) 
			{
				if (temp + 16 >= tempEnd)
					ExpandTemp ();

				uint tempStart = (uint)(tempPtr - temp);
				uint tempDataSave = (uint)(tempDataPtr - tempData);

				// Align temp data to 8 byte boundry
				while (((uint)tempDataPtr & 0x7) != 0)
					*tempDataPtr++ = 0;

				uint tempDataStart = (uint)(tempDataPtr - tempData);

				*(int*)tempPtr = 0;  							// The relative offset to start of data block
				tempPtr += 4;
				*(uint*)tempPtr = (uint)LIST_TYPE.ARRAY;  		// Type plus length
				tempPtr += 4;

				int len = 0;
				uint id = 0;
				uint offset = 0;

				// [
				bool parsing = true;
				while (true) {

					if (tempPtr + sizeof(DataElem) >= tempEnd)
						ExpandTemp ();

					TOKEN token = GetNextToken ();
					if (token == TOKEN.COMMA)
						token = GetNextToken ();

					switch (token) {
					case TOKEN.SQUARED_CLOSE:
						parsing = false;
						break;
					case TOKEN.STRING:
						id = (uint)DATA_TYPE.STRING << 29;
						offset = ParseDataString (tempDataStart);
						break;
					case TOKEN.NUMBER:
						id = 0;
						offset = ParseNumber (tempDataStart, ref id);
						break;
					case TOKEN.CURLY_OPEN:
						id = (uint)DATA_TYPE.OBJARRAY << 29;
						offset = ParseObject (tempDataStart);
						break;
					case TOKEN.SQUARED_OPEN:
						id = (uint)DATA_TYPE.OBJARRAY << 29;
						offset = ParseArray (tempDataStart);
						break;
					case TOKEN.TRUE:
						id = (uint)DATA_TYPE.TRUE << 29;
						offset = 0;
						break;
					case TOKEN.FALSE:
						id = (uint)DATA_TYPE.FALSE << 29;
						offset = 0;
						break;
					case TOKEN.NULL:
						id = (uint)DATA_TYPE.NULL << 29;
						offset = 0;
						break;
					default:
						throw new InvalidDataException("Parse error");
					}

					if (!parsing)
						break;

					DataElem* dataElem = (DataElem*)tempPtr;
					dataElem->id = id;
					dataElem->offset = offset;
					tempPtr += sizeof(DataElem);
					len++;

				} while (parsing);

				// Set length in lower 24 bits (upper 8 bits is type)
				*(int*)(temp + tempStart + 4) |= (int)(len & 0xFFFFFF);

				// Align data ptr to 4 byte boundry
				while (((uint)dataPtr & 0x3) != 0)
					*dataPtr++ = 0;

				// Copy elem array to data buffer
				uint dataStart = (uint)(dataPtr - data);
				CopyToData (temp + tempStart, (int)(tempPtr - (temp + tempStart)));

				// Align data ptr to 8 byte boundry
				while (((uint)dataPtr & 0x7) != 0)
					*dataPtr++ = 0;

				// Set relative ptr to first data elem
				*(uint*)(data + dataStart) = (uint)(dataPtr - data);

				// Copy data to data buffer
				CopyToData (tempData + tempDataStart, (int)(tempDataPtr - (tempData + tempDataStart)));

				// Restore current temp stacks
				tempPtr = temp + tempStart;
				tempDataPtr = tempData + tempDataSave;

				// Align temp data to even 4 bytes
				while (((uint)tempDataPtr & 0x3) != 0)
					*tempDataPtr++ = 0;

				// Add relative pointer to this object to parent's data
				*(uint*)tempDataPtr = dataStart;
				tempDataPtr += 4;

				// Return the offset to the pointer
				return (uint)(tempDataPtr - (int)(tempData + parentStart) - 4); 
			}

			uint ParseDataString(uint parentDataStart) 
			{
				char* src = json;
				char* srcEnd = end;

				// Align to even address
				if (((uint)tempDataPtr & 0x1) != 0) {
					if (tempDataPtr + 1 >= tempDataEnd)
						ExpandTempData ();
					*tempDataPtr++ = 0;
				}

				// Get the initial string and set the length to 0
				uint strPtr = (uint)(tempDataPtr - tempData);
				*(short*)tempDataPtr = 0;
				tempDataPtr += 2;

				// ditch opening quote
				src++;

				byte d = 0;
				bool parsing = true;
				while (true) {

					if (src == srcEnd) {
						parsing = false;
						break;
					}

					char c = *src++;
					switch (c) {
						case '"':
						parsing = false;
						break;
						case '\\':
						if (src == srcEnd) {
							parsing = false;
							break;
						}

						c = *src++;
						switch (c) {
							case '"':
							case '\\':
							case '/':
							d = (byte)c;
							break;
							case 'b':
							d = (byte)'\b';
							break;
							case 'f':
							d = (byte)'\f';
							break;
							case 'n':
							d = (byte)'\n';
							break;
							case 'r':
							d = (byte)'\r';
							break;
							case 't':
							d = (byte)'\t';
							break;
							case 'u':  // NOTE: Not optimized.. hopefully not too many of these in a normal file!
							var hex = new char[4];
							for (int i=0; i< 4; i++) {
								hex[i] = *src++;
							}
							d = (byte)Convert.ToInt32(new string(hex), 16);
							break;
						}
						break;
						default:
						d = (byte)c;
						break;
					}

					if (!parsing)
						break;

					if (tempDataPtr + 1 >= tempDataEnd)
						ExpandTempData ();
					*tempDataPtr++ = d;
				}

				// Advance parse position
				json = src;

				// Set final string length
				short len = (short)(tempDataPtr - (int)(tempData + strPtr) - 2);
				*(short*)(tempData + strPtr) = len;

				return (ushort)(strPtr - parentDataStart);
			}

			uint ParseKeyString() 
			{
				char* src = json;
				char* srcEnd = end;

				// Get the initial string and set the length to 0
				uint strPtr = (uint)(dataPtr - data);

				// ditch opening quote
				src++;

				// Make string hash (FNV-1a hash offset_basis)
				uint crc = BinJSonCrc32.DefaultSeed;

				byte d = 0;
				bool parsing = true;
				while (true) {

					if (src == srcEnd) {
						parsing = false;
						break;
					}

					char c = *src++;
					switch (c) {
						case '"':
						parsing = false;
						break;
						case '\\':
						if (src == srcEnd) {
							parsing = false;
							break;
						}

						c = *src++;
						switch (c) {
							case '"':
							case '\\':
							case '/':
							d = (byte)c;
							break;
							case 'b':
							d = (byte)'\b';
							break;
							case 'f':
							d = (byte)'\f';
							break;
							case 'n':
							d = (byte)'\n';
							break;
							case 'r':
							d = (byte)'\r';
							break;
							case 't':
							d = (byte)'\t';
							break;
							case 'u':  // NOTE: Not optimized.. hopefully not too many of these in a normal file!
							var hex = new char[4];
							for (int i=0; i< 4; i++) {
								hex[i] = *src++;
							}
							d = (byte)Convert.ToInt32(new string(hex), 16);
							break;
						}
						break;
						default:
						d = (byte)c;
						break;
					}

					if (!parsing)
						break;

					// Add char to string
					if (dataPtr + 1 >= dataEnd)
						ExpandData ();
					*dataPtr++ = d;

					// FNV-1a hash
					crc = (crc >> 8) ^ BinJSonCrc32.Table[(byte)c ^ crc & 0xff];
				}

				crc = ~crc & 0x1FFFFFFF; // top 3 bits 0 to allow for type bits

				// Add null terminator
				if (dataPtr + 1 >= dataEnd)
					ExpandData ();
				*dataPtr++ = 0;

				// Advance parse position
				json = src;

				// Check if it's already in the string table..
				int stridx = stringTable.GetStringIndex (data, strPtr, crc);
				if (stridx == -1) {
					if (tempStringPtr + 8 > tempStringEnd)
						ExpandTempString ();
					stridx = stringTable.Count;
					StringTableElem* strTableElem = (StringTableElem*)tempStringPtr;
					strTableElem->crc = crc;
					strTableElem->offset = strPtr;
					tempStringPtr += sizeof(StringTableElem);
					stringTable.AddString (data, strPtr, crc, stridx);
				} else {
					// We already have this string in our string table.. don't save it again in data
					dataPtr = data + strPtr;
				}

				return crc;
			}

			uint ParseNumber(uint parentDataStart, ref uint id) {

				uint offset = 0;

				bool isInt = true;

				bool isNeg;
				if (*json == '-') {
					isNeg = true;
					json++;
				} else {
					isNeg = false;
				}
				
				double v = 0.0;
				char ch = '\x0';
				while (json != end) {
					ch = *json;
					if (ch < '0' || ch > '9')
						break;
					v = v * 10.0 + (double)(ch - '0');
					json++;
				}

				if (json != end) {
					if (ch == '.') {
						isInt = false;
						json++;
						double dec = 0.1;
						while (json != end) {
							ch = *json;
							if (ch < '0' || ch > '9')
								break;
							v = v + dec * (double)(ch - '0');
							dec = dec * 0.1;
							json++;
						}
					}
				}

				if (json != end) {
					if (ch == 'e' || ch == 'E') {
						json++;
						if (json == end)
							throw new InvalidDataException ("Invalid number");
						ch = *json;
						bool isNegExp = false;
						if (ch == '-') {
							isNegExp = true;
							json++;
						} else if (ch == '+') {
							json++;
						}
						if (json == end)
							throw new InvalidDataException ("Invalid number");
						double e = 0.0;
						while (json != end) {
							ch = *json;
							if (ch < '0' || ch > '9')
								break;
							e = e * 10.0 + (double)(ch - '0');
							json++;
						}
						if (isNegExp)
							e = -e;
						v = v * System.Math.Pow(10.0, e);
					}
				}

				if (isNeg)
					v = -v;

				if (isInt) {
					int i = (int)v;
					id |= (uint)DATA_TYPE.INT << 29;
					offset = (uint)i;
				} else if (_useFloat32) {
					id |= (uint)DATA_TYPE.FLOAT << 29;
					*(float*)floatConvMem = (float)v;
					return *(uint*)floatConvMem;
				} else {
					id |= (uint)DATA_TYPE.DOUBLE << 29;
					if (tempDataPtr + 16 >= tempDataEnd)
						ExpandTempData ();
					while (((uint)tempDataPtr & 0x7) != 0) { // Align to even 8 bytes
						*tempDataPtr++ = 0;
					}
					*(double*)tempDataPtr = v;
					offset = (uint)(tempDataPtr - (tempData + parentDataStart));
					tempDataPtr += 8;
				}

				return offset;
			}

			TOKEN GetNextToken() {
				if (json == end) {
					return TOKEN.NONE;
				}

				char ch = *json;
				while (ch == ' ' || ch == '\t' || ch == '\r' || ch == '\n') {
					json++;
					if (json == end)
						return TOKEN.NONE;
					ch = *json;
				}

				switch (ch) {
				case '{':
						json++;
						return TOKEN.CURLY_OPEN;
					case '}':
						json++;
						return TOKEN.CURLY_CLOSE;
					case '[':
						json++;
						return TOKEN.SQUARED_OPEN;
					case ']':
						json++;
						return TOKEN.SQUARED_CLOSE;
					case ',':
						json++;
						return TOKEN.COMMA;
					case '"':
						return TOKEN.STRING;
					case ':':
						json++;
						return TOKEN.COLON;
					case '0':
					case '1':
					case '2':
					case '3':
					case '4':
					case '5':
					case '6':
					case '7':
					case '8':
					case '9':
					case '-':
						return TOKEN.NUMBER;
					case 't':
						if (end - json < 4) {
							json = end;
							return TOKEN.NONE;
						}
						if (json[1] != 'r' || json[2] != 'u' || json[3] != 'e') {
							return TOKEN.NONE;
						}
						json += 4;
						return TOKEN.TRUE;
					case 'f':
						if (end - json < 5) {
							json = end;
							return TOKEN.NONE;
						}
						if (json[1] != 'a' || json[2] != 'l' || json[3] != 's' || json[4] != 'e')	
							return TOKEN.NONE;
						json += 5;
						return TOKEN.FALSE;
					case 'n':
						if (end - json < 4) {
							json = end;
							return TOKEN.NONE;
						}
						if (json[1] != 'u' || json[2] != 'l' || json[3] != 'l')	
							return TOKEN.NONE;
						json += 4;
						return TOKEN.NULL;

				}

				return TOKEN.NONE;
			}
		}

	}

	#region UniqueStringDictionary

	// Ultra cut down version of dictionary that is used to store the string table
	internal unsafe class UniqueStringDictionary
	{
		private struct Link {
			public int HashCode;
			public int Next;
		}

		private struct Key {
			public uint StrPtr;
			public uint StrHash;
			public Key(uint strptr, uint hash) {
				StrPtr = strptr;
				StrHash = hash;
			}
		}

		private static readonly int [] primeTbl = {
			11,
			19,
			37,
			73,
			109,
			163,
			251,
			367,
			557,
			823,
			1237,
			1861,
			2777,
			4177,
			6247,
			9371,
			14057,
			21089,
			31627,
			47431,
			71143,
			106721,
			160073,
			240101,
			360163,
			540217,
			810343,
			1215497,
			1823231,
			2734867,
			4102283,
			6153409,
			9230113,
			13845163
		};


		//
		// Private static methods
		//
		private static bool TestPrime (int x)
		{
			if ((x & 1) != 0) {
				int top = (int)System.Math.Sqrt (x);

				for (int n = 3; n < top; n += 2) {
					if ((x % n) == 0)
						return false;
				}
				return true;
			}
			// There is only one even prime - 2.
			return (x == 2);
		}

		private static int CalcPrime (int x)
		{
			for (int i = (x & (~1))-1; i< Int32.MaxValue; i += 2) {
				if (TestPrime (i)) return i;
			}
			return x;
		}

		private static int ToPrime (int x)
		{
			for (int i = 0; i < primeTbl.Length; i++) {
				if (x <= primeTbl [i])
					return primeTbl [i];
			}
			return CalcPrime (x);
		}

		const int INITIAL_SIZE = 10;
		const float DEFAULT_LOAD_FACTOR = (90f / 100);
		const int NO_SLOT = -1;
		const int HASH_FLAG = -2147483648;

		// The hash table contains indices into the linkSlots array
		int [] table;

		// All (key,value) pairs are chained into linked lists. The connection
		// information is stored in "linkSlots" along with the key's hash code
		// (for performance reasons).
		// TODO: get rid of the hash code in Link (this depends on a few
		// JIT-compiler optimizations)
		// Every link in "linkSlots" corresponds to the (key,value) pair
		// in "keySlots"/"valueSlots" with the same index.
		Link [] linkSlots;
		Key [] keySlots;
		int [] valueSlots;

		// The number of slots in "linkSlots" and "keySlots"/"valueSlots" that
		// are in use (i.e. filled with data) or have been used and marked as
		// "empty" later on.
		int touchedSlots;

		// The index of the first slot in the "empty slots chain".
		// "Remove()" prepends the cleared slots to the empty chain.
		// "Add()" fills the first slot in the empty slots chain with the
		// added item (or increases "touchedSlots" if the chain itself is empty).
		int emptySlot;

		// The number of (key,value) pairs in this dictionary.
		int count;

		// The number of (key,value) pairs the dictionary can hold without
		// resizing the hash table and the slots arrays.
		int threshold;

		public int Count {
			get { return count; }
		}

		public UniqueStringDictionary() 
		{
			int capacity = INITIAL_SIZE;
			if (capacity < 0)
				throw new ArgumentOutOfRangeException ("capacity");
			if (capacity == 0)
				capacity = INITIAL_SIZE;

			/* Modify capacity so 'capacity' elements can be added without resizing */
			capacity = (int)(capacity / DEFAULT_LOAD_FACTOR) + 1;

			table = new int [capacity];

			linkSlots = new Link [capacity];
			emptySlot = NO_SLOT;

			keySlots = new Key [capacity];
			valueSlots = new int [capacity];
			touchedSlots = 0;

			threshold = (int)(table.Length * DEFAULT_LOAD_FACTOR);
			if (threshold == 0 && table.Length > 0)
				threshold = 1;
		}

		private static bool StringEquals(byte* data, uint _a, uint _b)
		{
			byte* a = data + _a;
			byte* b = data + _b;
			while (*a != 0 && *b != 0) {
				if (*a != *b)
					return false;
				a++;
				b++;
			}
			if (*a != *b)
				return false;
			return true;
		}

		public int GetStringIndex(byte *data, uint strptr, uint hash) {
			int hashCode = (int)hash | HASH_FLAG;
			int cur = table [(hashCode & int.MaxValue) % table.Length] - 1;

			while (cur != NO_SLOT) {
				if (linkSlots [cur].HashCode == hashCode && StringEquals(data, keySlots [cur].StrPtr, strptr)) {
					return valueSlots [cur];
				}
				cur = linkSlots [cur].Next;
			}
			return -1;
		}

		public void AddString (byte* data, uint strptr, uint hash, int stridx)
		{
			// get first item of linked list corresponding to given key
			int hashCode = (int)hash | HASH_FLAG;
			int index = (hashCode & int.MaxValue) % table.Length;
			int cur = table [index] - 1;

			while (cur != NO_SLOT) {
				if (linkSlots [cur].HashCode == hashCode && StringEquals(data, keySlots [cur].StrPtr, strptr))
					throw new ArgumentException ();
				cur = linkSlots [cur].Next;
			}

			if (++count > threshold) {
				Resize ();
				index = (hashCode & int.MaxValue) % table.Length;
			}

			// find an empty slot
			cur = emptySlot;
			if (cur == NO_SLOT)
				cur = touchedSlots++;
			else 
				emptySlot = linkSlots [cur].Next;

			// store the hash code of the added item,
			// prepend the added item to its linked list,
			// update the hash table
			linkSlots [cur].HashCode = hashCode;
			linkSlots [cur].Next = table [index] - 1;
			table [index] = cur + 1;

			// store item's data 
			keySlots [cur] = new Key(strptr, hash);
			valueSlots [cur] = stridx;
		}

		private void Resize ()
		{
			// From the SDK docs:
			//	 Hashtable is automatically increased
			//	 to the smallest prime number that is larger
			//	 than twice the current number of Hashtable buckets
			int newSize = ToPrime ((table.Length << 1) | 1);

			// allocate new hash table and link slots array
			int [] newTable = new int [newSize];
			Link [] newLinkSlots = new Link [newSize];

			for (int i = 0; i < table.Length; i++) {
				int cur = table [i] - 1;
				while (cur != NO_SLOT) {
					int hashCode = newLinkSlots [cur].HashCode = (int)keySlots [cur].StrHash | HASH_FLAG;
					int index = (hashCode & int.MaxValue) % newSize;
					newLinkSlots [cur].Next = newTable [index] - 1;
					newTable [index] = cur + 1;
					cur = linkSlots [cur].Next;
				}
			}
			table = newTable;
			linkSlots = newLinkSlots;

			// allocate new data slots, copy data
			Key [] newKeySlots = new Key [newSize];
			int [] newValueSlots = new int [newSize];
			System.Array.Copy (keySlots, 0, newKeySlots, 0, touchedSlots);
			System.Array.Copy (valueSlots, 0, newValueSlots, 0, touchedSlots);
			keySlots = newKeySlots;
			valueSlots = newValueSlots;			

			threshold = (int)(newSize * DEFAULT_LOAD_FACTOR);
		}

	}

	#endregion

}
