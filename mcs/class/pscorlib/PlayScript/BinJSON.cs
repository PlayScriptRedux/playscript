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
	internal static class BinJsonCrc32
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
				crc = (crc >> 8) ^ Table[(byte)buffer[i] ^ (byte)crc & 0xff];
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

		public BinJsonDocument(byte* data)
		{
			valueStringCache = new Vector<string> ();
			// Make sure we have something at element 0 so it's not used.
			valueStringCache.push ("");

			this.data = data;
			this.keyStringTable = (StringTableElem*)(data + *(uint*)(data + 16));
		}

		public BinJsonObject GetRootObject()
		{
			return new BinJsonObject (this, this.data + *(uint*)(this.data + 12));
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
				if (keyStringsByCrc == null || keyStringsByCrc.Count != KeyTableCount) {
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
		public string key   { get { return _key; }}
		public object value 
		{
			get {
				uint crc = 0;
				return ((IDynamicAccessor<object>)_binObj).GetMemberOrDefault(_key, ref crc, null);
			}
			set { 
				uint crc = 0;
				((IDynamicAccessor<object>)_binObj).SetMember(_key, ref crc, value);
			}
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
				var keys = binObj.GetKeys ();
				var dbgKeys = new KeyValuePairDebugView[keys.Count];

				int i = 0;
				foreach(string key in keys)
				{
					dbgKeys[i] = new KeyValuePairDebugView(binObj, key);
					i++;
				}

				return dbgKeys;
			}
		}
	}

	// Used to implement "Object" semantics for binary json document.  Implements fast versions of dynamic get index and dynamic
	// get member that accellerate the dynamic runtime.
	[DebuggerDisplay ("Count = {Count}")]
	[DebuggerTypeProxy (typeof (BinJsonObjectDebugView))]
	public unsafe class BinJsonObject : _root.Object, IDynamicObject, IDynamicAccessorTyped, 
		IDictionary<string,object>, IDictionary, IDynamicClass
	{
		protected BinJsonDocument doc;
		protected byte* list;
		protected WeakReference<KeyCrcPairs> keyPairs;
		protected PlayScript.Expando.ExpandoObject expando;

		internal static string _lastKeyString;
		internal static uint _lastCrc;

		public BinJsonObject(BinJsonDocument doc, byte* list) {
			this.doc = doc;
			this.list = list;
		}

		public int ListCount {
			get { return (int)(*(uint*)(list + 4) & 0xFFFFFF); }
		}

		public int Count {
			get {
				if (expando != null)
					return expando.Count;
				return KeyPairs.length;
			}
		}

		public BinJsonDocument Document {
			get { return doc; }
		}

		internal KeyCrcPairs KeyPairs {
			get {
				KeyCrcPairs pairs = null;
				if (keyPairs != null)
					keyPairs.TryGetTarget (out pairs);
				if (pairs == null) {
					int i;
					int listCount = this.ListCount;
					int len = 0;
					DataElem* dataElem = (DataElem*)(list + 8);
					for (i = 0; i < listCount; i++) {
						uint id = dataElem->id;
						if (id != 0)
							len++;
						dataElem++;
					}
					pairs = new KeyCrcPairs ();
					pairs.length = len;
					string[] keyArray = new string[len];
					pairs.keys = keyArray;
					uint[] crcArray = new uint[len];
					pairs.crcs = crcArray;
					Dictionary<uint,string> crcStrTable = doc.KeyTable;
					dataElem = (DataElem*)(list + 8);
					int pos = 0;
					for (i = 0; i < listCount; i++) {
						uint crc = dataElem->id & 0x1FFFFFFF;
						if (crc != 0) {
							string key = crcStrTable [crc];
							keyArray [pos] = key;
							crcArray [pos] = crc;
							pos++;
						}
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

		internal ICollection<string> GetKeys()
		{
			if (expando != null)
				return ((IDictionary<string,object>)expando).Keys;
			else
				return KeyPairs.keys;
		}

		internal object[] GetValues()
		{
			object[] values = new object[this.Count];
			var enumerator = ((IEnumerator<KeyValuePair<string,object>>)this);
			int i = 0;
			while (enumerator.MoveNext()) {
				values [i] = enumerator.Current.Value;
				i++;
			}
			return values;
		}

		public PlayScript.Expando.ExpandoObject CloneToExpando() {
			var newExpando = new PlayScript.Expando.ExpandoObject ();
			KeyCrcPairs keyPairs = this.KeyPairs;
			int len = keyPairs.length;
			IDynamicAccessor<object> getMemProv = (IDynamicAccessor<object>)this;
			for (var i = 0; i < len; i++) {
				string key = keyPairs.keys [i];
				uint crc = keyPairs.crcs [i];
				newExpando.Add (key, getMemProv.GetMemberOrDefault (key, ref crc, null));
			}
			return newExpando;
		}

		public void CloneToInnerExpando() {
			expando = CloneToExpando ();
		}

		#region IKeyEnumerable implementation

		private class KeyEnumerator : IEnumerator {

			private KeyCrcPairs _pairs;
			private int _index;

			public KeyEnumerator(KeyCrcPairs pairs) {
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
					BinJsonObject._lastKeyString = key = _pairs.keys [_index];
					BinJsonObject._lastCrc = _pairs.crcs [_index];
					return key;
				}
			}

			#endregion
		}

		IEnumerator IKeyEnumerable.GetKeyEnumerator ()
		{
#if DYNAMIC_SUPPORT
			#warning BinJSON.GetKeyEnumerator is not implemented
			throw new NotImplementedException();
#else
			if (expando != null)
				return ((IKeyEnumerable)expando).GetKeyEnumerator ();
			else
				return new KeyEnumerator (this.KeyPairs);
#endif
		}

		#endregion

		#region IEnumerable implementation

		private class ValueEnumerator : IEnumerator {

			private IDynamicAccessor<object> _jsonObj;
			private KeyCrcPairs _pairs;
			private int _index;

			public ValueEnumerator(IDynamicAccessor<object> jsonObj, KeyCrcPairs pairs) {
				_jsonObj = jsonObj;
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
					// We store the last string and crc in the PSGetIndex so it can short path to using them during a loop over
					// keys in an object.
					uint crc;
					string key;
					BinJsonObject._lastKeyString = key = _pairs.keys [_index];
					BinJsonObject._lastCrc = crc = _pairs.crcs [_index];
					return _jsonObj.GetMemberOrDefault(key, ref crc, null);
				}
			}

			#endregion
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			if (expando != null)
				return ((IEnumerable)expando).GetEnumerator ();
			else
				return new ValueEnumerator (this, this.KeyPairs);
		}

		#endregion

		#region IEnumerable<KeyValuePair> implementation

		private class KeyValuePairEnumerator : IEnumerator<KeyValuePair<string,object>> {

			private IDynamicAccessor<object> _jsonObj;
			private KeyCrcPairs _pairs;
			private int _index;

			public KeyValuePairEnumerator(IDynamicAccessor<object> jsonObj, KeyCrcPairs pairs) {
				_jsonObj = jsonObj;
				_pairs = pairs;
				_index = -1;
			}

			#region IEnumerator implementation

			public void Dispose()
			{
			}

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

			public KeyValuePair<string,object> Current {
				get {
					// We store the last string and crc in the PSGetIndex so it can short path to using them during a loop over
					// keys in an object.
					string key;
					uint crc;
					BinJsonObject._lastKeyString = key = _pairs.keys [_index];
					BinJsonObject._lastCrc = crc = _pairs.crcs [_index];
					return new KeyValuePair<string, object>(key, _jsonObj.GetMemberOrDefault(key, ref crc, null));
				}
			}

			object IEnumerator.Current {
				get {
					// We store the last string and crc in the PSGetIndex so it can short path to using them during a loop over
					// keys in an object.
					string key;
					uint crc;
					BinJsonObject._lastKeyString = key = _pairs.keys [_index];
					BinJsonObject._lastCrc = crc = _pairs.crcs [_index];
					return new KeyValuePair<string, object>(key, _jsonObj.GetMemberOrDefault(key, ref crc, null));
				}
			}

			#endregion

		}

		IEnumerator<KeyValuePair<string,object>> IEnumerable<KeyValuePair<string,object>>.GetEnumerator ()
		{
			if (expando != null)
				return ((IEnumerable<KeyValuePair<string,object>>)expando).GetEnumerator ();
			else
				return new KeyValuePairEnumerator (this, this.KeyPairs);
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
//		private static object _zeroIntObject = 0;

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
				return null;
				case DATA_TYPE.OBJARRAY:
				throw new NotImplementedException ();
			}
			return null;
		}

		internal int GetValueInt(DATA_TYPE elemType, byte* data, DataElem* dataElem)
		{
			int i;
			switch (elemType) {
			case DATA_TYPE.STRING:
				string s = GetValueString (elemType, data, dataElem);
				if (s.Length > 2 && s [0] == '0' && s [1] == 'x') {
					i = Convert.ToInt32 (s, 16);
				} else {
					int.TryParse (s, out i);
				}
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
				string s = GetValueString (elemType, data, dataElem);
				if (s.Length > 2 && s [0] == '0' && s [1] == 'x') {
					i = Convert.ToUInt32 (s, 16);
				} else {
					uint.TryParse (s, out i);
				}
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
				return (i == 0) ? _zeroDoubleObject : (object)(double)i;
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
					return new _root.Array ((IImmutableArray)new BinJsonArray (doc, list));
				}
				return null;
			}
			return false;
		}

		#endregion

		#region Index Accessors

		string IDynamicAccessor<string>.GetIndex(int index)
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

		void IDynamicAccessor<string>.SetIndex(int index, string value)
		{
			throw new NotImplementedException ();
		}

		string IDynamicAccessor<string>.GetIndex(object index)
		{
			if (index != null) {
				uint len = *(uint*)(list + 4);
				LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
				if (listType == LIST_TYPE.ARRAY) {
					return ((IDynamicAccessor<string>)this).GetIndex(PlayScript.DynamicRuntime.PSConverter.ConvertToInt(index));
				} else {
					uint crc = 0;
					return ((IDynamicAccessor<string>)this).GetMemberOrDefault(index.ToString(), ref crc, null);
				}
			}
			return null;
		}

		void IDynamicAccessor<string>.SetIndex(object index, string value)
		{
			throw new NotImplementedException ();
		}

		string IDynamicAccessor<string>.GetIndex(string index)
		{
			if (index != null) {
				uint len = *(uint*)(list + 4);
				LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
				if (listType == LIST_TYPE.ARRAY) {
					return ((IDynamicAccessor<string>)this).GetIndex(PlayScript.DynamicRuntime.PSConverter.ConvertToInt(index));
				} else {
					uint crc = 0;
					return ((IDynamicAccessor<string>)this).GetMemberOrDefault(index.ToString(), ref crc, null);
				}
			}
			return null;
		}

		void IDynamicAccessor<string>.SetIndex(string index, string value)
		{
			throw new NotImplementedException ();
		}

		int IDynamicAccessor<int>.GetIndex(int index)
		{
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			if (listType == LIST_TYPE.ARRAY) {
				len &= 0xFFFFFF;
				if (index < len) {
					DataElem* dataElem = ((DataElem*)(list + 8)) + index;
					DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 29);
					return GetValueInt (dataType, doc.data + *(uint*)list, dataElem);
				} else {
					return 0;
				}
			} else {
				throw new NotSupportedException ();
			}
		}

		void IDynamicAccessor<int>.SetIndex(int index, int value)
		{
			throw new NotImplementedException ();
		}

		int IDynamicAccessor<int>.GetIndex(object index)
		{
			if (index != null) {
				uint len = *(uint*)(list + 4);
				LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
				if (listType == LIST_TYPE.ARRAY) {
					return ((IDynamicAccessor<int>)this).GetIndex(PlayScript.DynamicRuntime.PSConverter.ConvertToInt(index));
				} else {
					uint crc = 0;
					return ((IDynamicAccessor<int>)this).GetMember(index.ToString(), ref crc);
				}
			}
			return 0;
		}

		void IDynamicAccessor<int>.SetIndex(object index, int value)
		{
			throw new NotImplementedException ();
		}

		int IDynamicAccessor<int>.GetIndex(string index)
		{
			if (index != null) {
				uint len = *(uint*)(list + 4);
				LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
				if (listType == LIST_TYPE.ARRAY) {
					return ((IDynamicAccessor<int>)this).GetIndex(PlayScript.DynamicRuntime.PSConverter.ConvertToInt(index));
				} else {
					uint crc = 0;
					return ((IDynamicAccessor<int>)this).GetMember(index.ToString(), ref crc);
				}
			}
			return 0;
		}

		void IDynamicAccessor<int>.SetIndex(string index, int value)
		{
			throw new NotImplementedException ();
		}

		double IDynamicAccessor<double>.GetIndex(int index)
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

		void IDynamicAccessor<double>.SetIndex(int index, double value)
		{
			throw new NotImplementedException ();
		}

		double IDynamicAccessor<double>.GetIndex(object index)
		{
			if (index != null) {
				uint len = *(uint*)(list + 4);
				LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
				if (listType == LIST_TYPE.ARRAY) {
					return ((IDynamicAccessor<double>)this).GetIndex(PlayScript.DynamicRuntime.PSConverter.ConvertToInt(index));
				} else {
					uint crc = 0;
					return ((IDynamicAccessor<double>)this).GetMember(index.ToString(), ref crc);
				}
			}
			return 0.0;
		}

		void IDynamicAccessor<double>.SetIndex(object index, double value)
		{
			throw new NotImplementedException ();
		}

		double IDynamicAccessor<double>.GetIndex(string index)
		{
			if (index != null) {
				uint len = *(uint*)(list + 4);
				LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
				if (listType == LIST_TYPE.ARRAY) {
					return ((IDynamicAccessor<double>)this).GetIndex(PlayScript.DynamicRuntime.PSConverter.ConvertToInt(index));
				} else {
					uint crc = 0;
					return ((IDynamicAccessor<double>)this).GetMember(index.ToString(), ref crc);
				}
			}
			return 0.0;
		}

		void IDynamicAccessor<double>.SetIndex(string index, double value)
		{
			throw new NotImplementedException ();
		}

		uint IDynamicAccessor<uint>.GetIndex(int index)
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

		void IDynamicAccessor<uint>.SetIndex(int index, uint value)
		{
			throw new NotImplementedException ();
		}

		uint IDynamicAccessor<uint>.GetIndex(object index)
		{
			if (index != null) {
				uint len = *(uint*)(list + 4);
				LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
				if (listType == LIST_TYPE.ARRAY) {
					return ((IDynamicAccessor<uint>)this).GetIndex(PlayScript.DynamicRuntime.PSConverter.ConvertToInt(index));
				} else {
					uint crc = 0;
					return ((IDynamicAccessor<uint>)this).GetMember(index.ToString(), ref crc);
				}
			}
			return 0u;
		}

		void IDynamicAccessor<uint>.SetIndex(object index, uint value)
		{
			throw new NotImplementedException ();
		}

		uint IDynamicAccessor<uint>.GetIndex(string index)
		{
			if (index != null) {
				uint len = *(uint*)(list + 4);
				LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
				if (listType == LIST_TYPE.ARRAY) {
					return ((IDynamicAccessor<uint>)this).GetIndex(PlayScript.DynamicRuntime.PSConverter.ConvertToInt(index));
				} else {
					uint crc = 0;
					return ((IDynamicAccessor<uint>)this).GetMember(index.ToString(), ref crc);
				}
			}
			return 0u;
		}

		void IDynamicAccessor<uint>.SetIndex(string index, uint value)
		{
			throw new NotImplementedException ();
		}

		bool IDynamicAccessor<bool>.GetIndex(int index)
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

		void IDynamicAccessor<bool>.SetIndex(int index, bool value)
		{
			throw new NotImplementedException ();
		}

		bool IDynamicAccessor<bool>.GetIndex(object index)
		{
			if (index != null) {
				uint len = *(uint*)(list + 4);
				LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
				if (listType == LIST_TYPE.ARRAY) {
					return ((IDynamicAccessor<bool>)this).GetIndex(PlayScript.DynamicRuntime.PSConverter.ConvertToInt(index));
				} else {
					uint crc = 0;
					return ((IDynamicAccessor<bool>)this).GetMember(index.ToString(), ref crc);
				}
			}
			return false;
		}

		void IDynamicAccessor<bool>.SetIndex(object index, bool value)
		{
			throw new NotImplementedException ();
		}

		bool IDynamicAccessor<bool>.GetIndex(string index)
		{
			if (index != null) {
				uint len = *(uint*)(list + 4);
				LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
				if (listType == LIST_TYPE.ARRAY) {
					return ((IDynamicAccessor<bool>)this).GetIndex(PlayScript.DynamicRuntime.PSConverter.ConvertToInt(index));
				} else {
					uint crc = 0;
					return ((IDynamicAccessor<bool>)this).GetMember(index.ToString(), ref crc);
				}
			}
			return false;
		}

		void IDynamicAccessor<bool>.SetIndex(string index, bool value)
		{
			throw new NotImplementedException ();
		}

		public object GetIndex(int index)
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

		public void SetIndex(int index, object value)
		{
			throw new NotImplementedException ();
		}

		public object GetIndex(object index)
		{
			if (index != null) {
				uint len = *(uint*)(list + 4);
				LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
				if (listType == LIST_TYPE.ARRAY) {
					return ((IDynamicAccessor<object>)this).GetIndex(PlayScript.DynamicRuntime.PSConverter.ConvertToInt(index));
				} else {
					uint crc = 0;
					return ((IDynamicAccessor<object>)this).GetMember(index.ToString(), ref crc);
				}
			}
			return null;
		}

		public void SetIndex(object index, object value)
		{
			throw new NotImplementedException ();
		}

		public object GetIndex(string index)
		{
			if (index != null) {
				uint len = *(uint*)(list + 4);
				LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
				if (listType == LIST_TYPE.ARRAY) {
					return ((IDynamicAccessor<object>)this).GetIndex(PlayScript.DynamicRuntime.PSConverter.ConvertToInt(index));
				} else {
					uint crc = 0;
					return ((IDynamicAccessor<object>)this).GetMember(index.ToString(), ref crc);
				}
			}
			return null;
		}

		public void SetIndex(string index, object value)
		{
			throw new NotImplementedException ();
		}

		// Handle .NET types that aren't commonly used in AS but can come up in interop..

		float IDynamicAccessor<float>.GetIndex(int index)
		{
			return (float)((IDynamicAccessor<double>)this).GetIndex (index);
		}

		void IDynamicAccessor<float>.SetIndex(int index, float value)
		{
			throw new NotImplementedException ();
		}

		float IDynamicAccessor<float>.GetIndex(object index)
		{
			if (index != null) {
				uint len = *(uint*)(list + 4);
				LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
				if (listType == LIST_TYPE.ARRAY) {
					return (float)((IDynamicAccessor<double>)this).GetIndex(PlayScript.DynamicRuntime.PSConverter.ConvertToInt(index));
				} else {
					uint crc = 0;
					return (float)((IDynamicAccessor<double>)this).GetMember(index.ToString(), ref crc);
				}
			}
			return 0.0f;
		}

		void IDynamicAccessor<float>.SetIndex(object index, float value)
		{
			throw new NotImplementedException ();
		}

		float IDynamicAccessor<float>.GetIndex(string index)
		{
			if (index != null) {
				uint len = *(uint*)(list + 4);
				LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
				if (listType == LIST_TYPE.ARRAY) {
					return (float)((IDynamicAccessor<double>)this).GetIndex(PlayScript.DynamicRuntime.PSConverter.ConvertToInt(index));
				} else {
					uint crc = 0;
					return (float)((IDynamicAccessor<double>)this).GetMember(index.ToString(), ref crc);
				}
			}
			return 0.0f;
		}

		void IDynamicAccessor<float>.SetIndex(string index, float value)
		{
			throw new NotImplementedException ();
		}

		#endregion

		#region HasMember & HasIndex Methods

		// Note: method assumes we know this is an object, not an array
		private DataElem* FindDataElem(uint crc, uint len)
		{
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
					uint curCrc = curElem->id & 0x1FFFFFFF;
					if (curCrc == crc) {
						return curElem;
					} else if (curCrc == 0) { // Fast out if key is not in obj
						return null;
					}
				}
			}
			return dataElem;
		}

		public bool HasMember(string key, ref uint crc) 
		{
			if (expando != null)
				return expando.ContainsKey (key);
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			if (listType == LIST_TYPE.OBJECT) {
				len &= 0xFFFFFF;
				if (len == 0)
					return false;
				if (crc == 0) {
					if (System.Object.ReferenceEquals (key, _lastKeyString)) {
						crc = _lastCrc;
					} else {
						_lastKeyString = key;
						crc = _lastCrc = BinJsonCrc32.Calculate ((string)key) & 0x1FFFFFFF;
					}
				}
				return FindDataElem (crc, len) != null;
			} else {
				return false;
			}
		}

		public bool HasMember(string key) 
		{
			uint crc = 0;
			return HasMember (key, ref crc);
		}

		public bool DeleteMember(string key)
		{
			if (expando != null)
				CloneToInnerExpando ();
			return expando.Remove (key);
		}

		public bool HasIndex(int key) 
		{
			return HasMember (key.ToString ());
		}

		public bool DeleteIndex(int key)
		{
			return DeleteMember (key.ToString ());
		}

		public bool HasIndex(object key)
		{
			return HasMember (key.ToString ());
		}

		public bool DeleteIndex(object key)
		{
			return DeleteMember (key.ToString ());
		}

		#endregion

		#region Member Accessors

		string IDynamicAccessor<string>.GetMember(string key, ref uint crc)
		{
			if (expando != null)
				return expando [key];
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			len &= 0xFFFFFF;
			if (len == 0)
				return null;
			if (crc == 0) {
				if (System.Object.ReferenceEquals (key, _lastKeyString)) {
					crc = _lastCrc;
				} else {
					_lastKeyString = key;
					crc = _lastCrc = BinJsonCrc32.Calculate ((string)key) & 0x1FFFFFFF;
				}				
			}
			if (listType == LIST_TYPE.OBJECT && len > 0) {
				DataElem* dataElem = FindDataElem (crc, len);
				if (dataElem == null)
					return null;
				DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 29);
				return GetValueString (dataType, doc.data + *(uint*)list, dataElem);
			} else {
				return null;
			}
		}

		string IDynamicAccessor<string>.GetMemberOrDefault(string key, ref uint crc, string defaultValue)
		{
			if (expando != null)
				return expando [key];
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			len &= 0xFFFFFF;
			if (len == 0)
				return defaultValue;
			if (crc == 0) {
				if (System.Object.ReferenceEquals (key, _lastKeyString)) {
					crc = _lastCrc;
				} else {
					_lastKeyString = key;
					crc = _lastCrc = BinJsonCrc32.Calculate ((string)key) & 0x1FFFFFFF;
				}				
			}
			if (listType == LIST_TYPE.OBJECT && len > 0) {
				DataElem* dataElem = FindDataElem (crc, len);
				if (dataElem == null)
					return defaultValue;
				DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 29);
				return GetValueString (dataType, doc.data + *(uint*)list, dataElem);
			} else {
				return defaultValue;
			}
		}

		void IDynamicAccessor<string>.SetMember(string key, ref uint crc, string value)
		{
			if (expando == null)
				CloneToInnerExpando ();
			expando [key] = value;
		}

		int IDynamicAccessor<int>.GetMember(string key, ref uint crc)
		{
			if (expando != null)
				return expando [key];
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			len &= 0xFFFFFF;
			if (len == 0)
				return 0;
			if (crc == 0) {
				if (System.Object.ReferenceEquals (key, _lastKeyString)) {
					crc = _lastCrc;
				} else {
					_lastKeyString = key;
					crc = _lastCrc = BinJsonCrc32.Calculate ((string)key) & 0x1FFFFFFF;
				}				
			}
			if (listType == LIST_TYPE.OBJECT && len > 0) {
				DataElem* dataElem = FindDataElem (crc, len);
				if (dataElem == null)
					return 0;
				DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 29);
				return GetValueInt (dataType, doc.data + *(uint*)list, dataElem);
			} else {
				return 0;
			}
		}

		int IDynamicAccessor<int>.GetMemberOrDefault(string key, ref uint crc, int defaultValue)
		{
			if (expando != null)
				return expando [key];
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			len &= 0xFFFFFF;
			if (len == 0)
				return defaultValue;
			if (crc == 0) {
				if (System.Object.ReferenceEquals (key, _lastKeyString)) {
					crc = _lastCrc;
				} else {
					_lastKeyString = key;
					crc = _lastCrc = BinJsonCrc32.Calculate ((string)key) & 0x1FFFFFFF;
				}				
			}
			if (listType == LIST_TYPE.OBJECT && len > 0) {
				DataElem* dataElem = FindDataElem (crc, len);
				if (dataElem == null)
					return defaultValue;
				DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 29);
				return GetValueInt (dataType, doc.data + *(uint*)list, dataElem);
			} else {
				return defaultValue;
			}
		}

		void IDynamicAccessor<int>.SetMember(string key, ref uint crc, int value)
		{
			if (expando == null)
				CloneToInnerExpando ();
			expando [key] = value;
		}

		double IDynamicAccessor<double>.GetMember(string key, ref uint crc)
		{
			if (expando != null)
				return expando [key];
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			len &= 0xFFFFFF;
			if (len == 0)
				return double.NaN;
			if (crc == 0) {
				if (System.Object.ReferenceEquals (key, _lastKeyString)) {
					crc = _lastCrc;
				} else {
					_lastKeyString = key;
					crc = _lastCrc = BinJsonCrc32.Calculate ((string)key) & 0x1FFFFFFF;
				}				
			}
			if (listType == LIST_TYPE.OBJECT && len > 0) {
				DataElem* dataElem = FindDataElem (crc, len);
				if (dataElem == null)
					return double.NaN;
				DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 29);
				return GetValueDouble (dataType, doc.data + *(uint*)list, dataElem);
			} else {
				return double.NaN;
			}
		}

		double IDynamicAccessor<double>.GetMemberOrDefault(string key, ref uint crc, double defaultValue)
		{
			if (expando != null)
				return expando [key];
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			len &= 0xFFFFFF;
			if (len == 0)
				return defaultValue;
			if (crc == 0) {
				if (System.Object.ReferenceEquals (key, _lastKeyString)) {
					crc = _lastCrc;
				} else {
					_lastKeyString = key;
					crc = _lastCrc = BinJsonCrc32.Calculate ((string)key) & 0x1FFFFFFF;
				}				
			}
			if (listType == LIST_TYPE.OBJECT && len > 0) {
				DataElem* dataElem = FindDataElem (crc, len);
				if (dataElem == null)
					return defaultValue;
				DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 29);
				return GetValueDouble (dataType, doc.data + *(uint*)list, dataElem);
			} else {
				return defaultValue;
			}
		}

		void IDynamicAccessor<double>.SetMember(string key, ref uint crc, double value)
		{
			if (expando == null)
				CloneToInnerExpando ();
			expando [key] = value;
		}

		uint IDynamicAccessor<uint>.GetMember(string key, ref uint crc)
		{
			if (expando != null)
				return expando [key];
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			len &= 0xFFFFFF;
			if (len == 0)
				return 0u;
			if (crc == 0) {
				if (System.Object.ReferenceEquals (key, _lastKeyString)) {
					crc = _lastCrc;
				} else {
					_lastKeyString = key;
					crc = _lastCrc = BinJsonCrc32.Calculate ((string)key) & 0x1FFFFFFF;
				}				
			}
			if (listType == LIST_TYPE.OBJECT && len > 0) {
				DataElem* dataElem = FindDataElem (crc, len);
				if (dataElem == null)
					return 0u;
				DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 29);
				return GetValueUInt (dataType, doc.data + *(uint*)list, dataElem);
			} else {
				return 0u;
			}
		}

		uint IDynamicAccessor<uint>.GetMemberOrDefault(string key, ref uint crc, uint defaultValue)
		{
			if (expando != null)
				return expando [key];
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			len &= 0xFFFFFF;
			if (len == 0)
				return defaultValue;
			if (crc == 0) {
				if (System.Object.ReferenceEquals (key, _lastKeyString)) {
					crc = _lastCrc;
				} else {
					_lastKeyString = key;
					crc = _lastCrc = BinJsonCrc32.Calculate ((string)key) & 0x1FFFFFFF;
				}				
			}
			if (listType == LIST_TYPE.OBJECT && len > 0) {
				DataElem* dataElem = FindDataElem (crc, len);
				if (dataElem == null)
					return defaultValue;
				DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 29);
				return GetValueUInt (dataType, doc.data + *(uint*)list, dataElem);
			} else {
				return defaultValue;
			}
		}

		void IDynamicAccessor<uint>.SetMember(string key, ref uint crc, uint value)
		{
			if (expando == null)
				CloneToInnerExpando ();
			expando [key] = value;
		}

		bool IDynamicAccessor<bool>.GetMember(string key, ref uint crc)
		{
			if (expando != null)
				return expando [key];
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			len &= 0xFFFFFF;
			if (len == 0)
				return false;
			if (crc == 0) {
				if (System.Object.ReferenceEquals (key, _lastKeyString)) {
					crc = _lastCrc;
				} else {
					_lastKeyString = key;
					crc = _lastCrc = BinJsonCrc32.Calculate ((string)key) & 0x1FFFFFFF;
				}				
			}
			if (listType == LIST_TYPE.OBJECT && len > 0) {
				DataElem* dataElem = FindDataElem (crc, len);
				if (dataElem == null)
					return false;
				DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 29);
				return GetValueBool (dataType, doc.data + *(uint*)list, dataElem);
			} else {
				return false;
			}
		}

		bool IDynamicAccessor<bool>.GetMemberOrDefault(string key, ref uint crc, bool defaultValue)
		{
			if (expando != null)
				return expando [key];
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			len &= 0xFFFFFF;
			if (len == 0)
				return defaultValue;
			if (crc == 0) {
				if (System.Object.ReferenceEquals (key, _lastKeyString)) {
					crc = _lastCrc;
				} else {
					_lastKeyString = key;
					crc = _lastCrc = BinJsonCrc32.Calculate ((string)key) & 0x1FFFFFFF;
				}				
			}
			if (listType == LIST_TYPE.OBJECT && len > 0) {
				DataElem* dataElem = FindDataElem (crc, len);
				if (dataElem == null)
					return defaultValue;
				DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 29);
				return GetValueBool (dataType, doc.data + *(uint*)list, dataElem);
			} else {
				return defaultValue;
			}
		}

		void IDynamicAccessor<bool>.SetMember(string key, ref uint crc, bool value)
		{
			if (expando == null)
				CloneToInnerExpando ();
			expando [key] = value;
		}

		public object GetMemberOrDefault(string key, ref uint crc, object defaultValue)
		{
			if (expando != null)
				return expando [key];
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			len &= 0xFFFFFF;
			if (len == 0)
				return defaultValue;
			if (crc == 0) {
				if (System.Object.ReferenceEquals (key, _lastKeyString)) {
					crc = _lastCrc;
				} else {
					_lastKeyString = key;
					crc = _lastCrc = BinJsonCrc32.Calculate ((string)key) & 0x1FFFFFFF;
				}				
			}
			if (listType == LIST_TYPE.OBJECT && len > 0) {
				DataElem* dataElem = FindDataElem (crc, len);
				if (dataElem == null)
					return defaultValue;
				DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 29);
				return GetValueObject (dataType, doc.data + *(uint*)list, dataElem);
			} else {
				return defaultValue;
			}
		}

		public object GetMember(string key, ref uint crc)
		{
			if (expando != null)
				return expando [key];
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			len &= 0xFFFFFF;
			if (len == 0)
				return null;
			if (crc == 0) {
				if (System.Object.ReferenceEquals (key, _lastKeyString)) {
					crc = _lastCrc;
				} else {
					_lastKeyString = key;
					crc = _lastCrc = BinJsonCrc32.Calculate ((string)key) & 0x1FFFFFFF;
				}				
			}
			if (listType == LIST_TYPE.OBJECT && len > 0) {
				DataElem* dataElem = FindDataElem (crc, len);
				if (dataElem == null)
					return null;
				DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 29);
				return GetValueObject (dataType, doc.data + *(uint*)list, dataElem);
			} else {
				return null;
			}
		}

		public void SetMember(string key, ref uint crc, object value)
		{
			if (expando == null)
				CloneToInnerExpando ();
			expando [key] = value;
		}

		[return: AsUntyped]
		object IDynamicAccessorUntyped.GetMemberOrDefault(string key, ref uint crc, [AsUntyped] object defaultValue)
		{
			if (expando != null)
				return expando [key];
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			len &= 0xFFFFFF;
			if (len == 0)
				return defaultValue;
			if (crc == 0) {
				if (System.Object.ReferenceEquals (key, _lastKeyString)) {
					crc = _lastCrc;
				} else {
					_lastKeyString = key;
					crc = _lastCrc = BinJsonCrc32.Calculate ((string)key) & 0x1FFFFFFF;
				}				
			}
			if (listType == LIST_TYPE.OBJECT && len > 0) {
				DataElem* dataElem = FindDataElem (crc, len);
				if (dataElem == null)
					return defaultValue;
				DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 29);
				return GetValueObject (dataType, doc.data + *(uint*)list, dataElem);
			} else {
				return defaultValue;
			}
		}

		[return: AsUntyped]
		object IDynamicAccessorUntyped.GetMember(string key, ref uint crc)
		{
			if (expando != null)
				return expando [key];
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			len &= 0xFFFFFF;
			if (len == 0)
				return PlayScript.Undefined._undefined;
			if (crc == 0) {
				if (System.Object.ReferenceEquals (key, _lastKeyString)) {
					crc = _lastCrc;
				} else {
					_lastKeyString = key;
					crc = _lastCrc = BinJsonCrc32.Calculate ((string)key) & 0x1FFFFFFF;
				}				
			}
			if (listType == LIST_TYPE.OBJECT && len > 0) {
				DataElem* dataElem = FindDataElem (crc, len);
				if (dataElem == null)
					return PlayScript.Undefined._undefined;
				DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 29);
				return GetValueObject (dataType, doc.data + *(uint*)list, dataElem);
			} else {
				return PlayScript.Undefined._undefined;
			}
		}

		void IDynamicAccessorUntyped.SetMember(string key, ref uint crc, [AsUntyped] object value)
		{
			if (expando == null)
				CloneToInnerExpando ();
			expando [key] = value;
		}


		// Handle .NET types that aren't commonly used in AS but can come up in interop..

		float IDynamicAccessor<float>.GetMember(string key, ref uint crc)
		{
			return (float)((IDynamicAccessor<double>)this).GetMember (key, ref crc);
		}

		float IDynamicAccessor<float>.GetMemberOrDefault(string key, ref uint crc, float defaultValue)
		{
			return (float)((IDynamicAccessor<double>)this).GetMemberOrDefault(key, ref crc, (double)defaultValue);
		}
		
		void IDynamicAccessor<float>.SetMember(string key, ref uint crc, float value)
		{
			if (expando == null)
				CloneToInnerExpando ();
			expando [key] = value;
		}

		#endregion

		#region IDynamicAccessorTyped implementation

		public object GetMemberObject (string key, ref uint hint, object defaultValue)
		{
			return GetMemberOrDefault(key, ref hint, defaultValue);
		}

		public void SetMemberObject (string key, ref uint hint, object value)
		{
			if (expando == null)
				CloneToInnerExpando ();
			expando [key] = value;
		}

		[return: AsUntyped]
		public object GetMemberUntyped (string key, ref uint hint, [AsUntyped] object defaultValue)
		{
			return ((IDynamicAccessor<object>)this).GetMemberOrDefault(key, ref hint, defaultValue);
		}

		public void SetMemberUntyped (string key, ref uint hint, [AsUntyped] object value)
		{
			if (expando == null)
				CloneToInnerExpando ();
			expando [key] = value;
		}

		public string GetMemberString (string key, ref uint hint, string defaultValue)
		{
			return ((IDynamicAccessor<string>)this).GetMemberOrDefault(key, ref hint, defaultValue);
		}

		public void SetMemberString (string key, ref uint hint, string value)
		{
			if (expando == null)
				CloneToInnerExpando ();
			expando [key] = value;
		}

		public int GetMemberInt (string key, ref uint hint, int defaultValue)
		{
			return ((IDynamicAccessor<int>)this).GetMemberOrDefault(key, ref hint, defaultValue);
		}

		public void SetMemberInt (string key, ref uint hint, int value)
		{
			if (expando == null)
				CloneToInnerExpando ();
			expando [key] = value;
		}

		public uint GetMemberUInt (string key, ref uint hint, uint defaultValue)
		{
			return ((IDynamicAccessor<uint>)this).GetMemberOrDefault(key, ref hint, defaultValue);
		}

		public void SetMemberUInt (string key, ref uint hint, uint value)
		{
			if (expando == null)
				CloneToInnerExpando ();
			expando [key] = value;
		}

		public double GetMemberNumber (string key, ref uint hint, double defaultValue)
		{
			return ((IDynamicAccessor<double>)this).GetMemberOrDefault(key, ref hint, defaultValue);
		}

		public void SetMemberNumber (string key, ref uint hint, double value)
		{
			if (expando == null)
				CloneToInnerExpando ();
			expando [key] = value;
		}

		public bool GetMemberBool (string key, ref uint hint, bool defaultValue)
		{
			return ((IDynamicAccessor<bool>)this).GetMemberOrDefault(key, ref hint, defaultValue);
		}

		public void SetMemberBool (string key, ref uint hint, bool value)
		{
			if (expando == null)
				CloneToInnerExpando ();
			expando [key] = value;
		}

		#endregion

		#region IDictionary implementation

		void IDictionary<string, object>.Add (string key, object value)
		{
			uint crc = 0;
			this.SetMember (key, ref crc, value);
		}

		bool IDictionary<string, object>.ContainsKey (string key)
		{
			return this.HasMember (key);
		}

		bool IDictionary<string, object>.Remove (string key)
		{
			if (this.HasMember (key)) {
				this.DeleteMember (key);
				return true;
			}
			return false;
		}

		bool IDictionary<string, object>.TryGetValue (string key, out object value)
		{
			value = null;
			if (this.HasMember (key)) {
				uint crc = 0;
				value = this.GetMemberOrDefault (key, ref crc, null); 
				return true;
			}
			return false;
		}

		object IDictionary<string, object>.this [string index] {
			get {
				uint crc = 0;
				return this.GetMemberOrDefault (index, ref crc, null);
			}
			set {
				uint crc = 0;
				this.SetMember (index, ref crc, value);
			}
		}

		ICollection<string> IDictionary<string, object>.Keys {
			get {
				return GetKeys ();
			}
		}
		ICollection<object> IDictionary<string, object>.Values {
			get {
				return GetValues ();
			}
		}

		#endregion

		#region ICollection implementation

		void ICollection<KeyValuePair<string, object>>.Add (KeyValuePair<string, object> item)
		{
			uint crc = 0;
			this.SetMember (item.Key, ref crc, item.Value);
		}

		void ICollection<KeyValuePair<string, object>>.Clear ()
		{
			if (expando == null)
				expando = new PlayScript.Expando.ExpandoObject ();
		}

		bool ICollection<KeyValuePair<string, object>>.Contains (KeyValuePair<string, object> item)
		{
			return this.HasMember (item.Key);
		}

		void ICollection<KeyValuePair<string, object>>.CopyTo (KeyValuePair<string, object>[] array, int arrayIndex)
		{
			throw new NotImplementedException ();
		}

		bool ICollection<KeyValuePair<string, object>>.Remove (KeyValuePair<string, object> item)
		{
			if (this.HasMember (item.Key)) {
				this.DeleteMember(item.Key);
				return true;
			}
			return false;
		}

		bool ICollection<KeyValuePair<string, object>>.IsReadOnly {
			get {
				return false;
			}
		}

		#endregion

		#region IDictionary implementation

		void IDictionary.Add (object key, object value)
		{
			uint crc = 0;
			this.SetMember (key.ToString(), ref crc, value);
		}

		void IDictionary.Clear ()
		{
			if (expando == null)
				expando = new PlayScript.Expando.ExpandoObject ();
		}

		private class DictionaryEnumerator : IDictionaryEnumerator {

			private IDynamicAccessor<object> _jsonObj;
			private KeyCrcPairs _pairs;
			private int _index;
			private string key;
			private object value;

			public DictionaryEnumerator(IDynamicAccessor<object> jsonObj, KeyCrcPairs pairs) {
				_jsonObj = jsonObj;
				_pairs = pairs;
				_index = -1;
			}

			#region IEnumerator implementation

			public void Dispose()
			{
			}

			public bool MoveNext ()
			{
				if (_index + 1 >= _pairs.length)
					return false;
				_index++;
				uint crc = 0;
				BinJsonObject._lastKeyString = key = _pairs.keys [_index];
				BinJsonObject._lastCrc = crc = _pairs.crcs [_index];
				value = _jsonObj.GetMemberOrDefault (key, ref crc, null);
				return true;
			}

			public void Reset ()
			{
				_index = -1;
			}

			public object Current {
				get {
					return new DictionaryEntry(key, value);
				}
			}

			#endregion

			#region IDictionaryEnumerator implementation

			public DictionaryEntry Entry {
				get {
					return new DictionaryEntry(key, value);
				}
			}

			public object Key {
				get {
					return key;
				}
			}

			public object Value {
				get {
					return value;
				}
			}

			#endregion
		}

		IDictionaryEnumerator IDictionary.GetEnumerator ()
		{
#if DYNAMIC_SUPPORT
			#warning BinJSON.GetEnumerator is not implemented
			throw new NotImplementedException();
#else
			if (expando != null)
				return ((IDictionary)expando).GetEnumerator ();
			else
				return new DictionaryEnumerator (this, this.KeyPairs);
#endif
		}

		void IDictionary.Remove (object key)
		{
			this.DeleteMember (key.ToString ());
		}

		bool IDictionary.IsFixedSize {
			get {
				return false;
			}
		}

		ICollection IDictionary.Keys {
			get {
				return this.KeyPairs.keys;
			}
		}

		ICollection IDictionary.Values {
			get {
				return GetValues ();
			}
		}

		bool IDictionary.IsReadOnly {
			get {
				return false;
			}
		}

		object IDictionary.this [object key] {
			get {
				uint crc = 0;
				return this.GetMemberOrDefault (key.ToString (), ref crc, null);
			}
			set {
				uint crc = 0;
				this.SetMember (key.ToString (), ref crc, value);
			}
		}

		#endregion

		#region ICollection implementation

		void ICollection.CopyTo (System.Array array, int index)
		{
			throw new NotImplementedException ();
		}

		bool ICollection.IsSynchronized {
			get {
				return false;
			}
		}

		object ICollection.SyncRoot {
			get {
				return this;
			}
		}

		#endregion

		#region IDynamicClass implementation

		dynamic IDynamicClass.__GetDynamicValue (string name)
		{
			uint crc = 0;
			return this.GetMemberOrDefault (name, ref crc, PlayScript.Undefined._undefined);
		}

		bool IDynamicClass.__TryGetDynamicValue (string name, out object value)
		{
			value = null;
			if (this.HasMember (name)) {
				uint crc = 0;
				value = this.GetMemberOrDefault (name, ref crc, PlayScript.Undefined._undefined);
				return true;
			}
			return false;
		}

		void IDynamicClass.__SetDynamicValue (string name, object value)
		{
			uint crc = 0;
			this.SetMember (name, ref crc, value);
		}

		bool IDynamicClass.__DeleteDynamicValue (object name)
		{
			string key = name.ToString ();
			if (this.HasMember (key)) {
				this.DeleteMember (key);
				return true;
			}
			return false;
		}

		bool IDynamicClass.__HasDynamicValue (string name)
		{
			return this.HasMember (name);
		}

		IEnumerable IDynamicClass.__GetDynamicNames ()
		{
			return this.KeyPairs.keys;
		}

		#endregion
	}

	//
	// Implements IImmutableArray interface for BinJSON static data buffers.  Allows Array objects to
	// use this binary data as it's backing store.
	//

	internal unsafe class BinJsonArray : BinJsonObject, IEnumerable, IImmutableArray
	{
		public BinJsonArray(BinJsonDocument doc, byte* list) : base(doc, list) {
		}

		#region IImmutableArray implementation

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

		internal static string _numberSize = NumberSize.Float64;
		internal static bool _useFloat32 = false;
		internal static bool _preserveOrder = false;
		internal static bool _useJson = false;
		internal static bool _dumpBinary = false;
		internal static string _dumpBinaryPath = "";
		internal static bool _registeredHandlers = false;

		private static System.Text.StringBuilder _log = new System.Text.StringBuilder();

		public static void trace(string msg)
		{
			_log.Append (msg + "\n");
			_root.trace_fn.trace ("#############\n" + _log.ToString());
		}

		/// <summary>
		/// Parse the specified json string and return a binary json tree.
		/// </summary>
		/// <param name="json">The json string.</param>
		/// <param name="url">The original relative load url from which the file was loaded.</param>
		public static dynamic parse(string json, string url = null) 
		{
			object ret;

			if (_useJson || json.StartsWith ("binjson$$")) {

				// Do just ordinary JSON parsing..
				ret = _root.JSON.parse (json);
			
			} else {

//				#if DEBUG
				System.Diagnostics.Stopwatch sw = new Stopwatch ();
				Process proc = Process.GetCurrentProcess ();
				long memStart = proc.WorkingSet64;
				sw.Reset ();
				sw.Start ();
//				#endif


				_useFloat32 = _numberSize == NumberSize.Float32;
				if (json == null) {
					throw new ArgumentNullException ("json");
				}
				ret = Parser.Parse (json);

//				#if DEBUG
				sw.Stop ();
				trace ("** " + (_useJson ? "JSON" : "BINJSON") + ": Parse '" + (url != null ? url : "<unknown>") + "': " + 
					sw.ElapsedMilliseconds + "ms memUsed: " + (proc.WorkingSet64 - memStart) + 
					" totalMem: " + proc.WorkingSet64);
//				#endif

				// Dump binary
				if (_dumpBinary && !_registeredHandlers && url != null) {
					string saveUrl = url.Replace (".json", ".binj");
					var data = ((BinJsonObject)ret).Document.ToArray ();
					System.IO.File.WriteAllBytes (_dumpBinaryPath + saveUrl, data);
				}
			}

			return ret;
		}

		public static dynamic parseBinary(object buffer) 
		{
			if (!(buffer is flash.utils.ByteArray)) {
				throw new ArgumentException ("buffer");
			}
			var byteArray = buffer as flash.utils.ByteArray;
			byte[] array = byteArray.getRawArray ();
			GCHandle gch = GCHandle.Alloc(array, GCHandleType.Pinned);
			byte* data = (byte*)gch.AddrOfPinnedObject().ToPointer();
			BinJsonDocument doc = new BinJsonDocument (data);
			return doc.GetRootObject ();
		}



		/// <summary>
		/// Registers the load handlers for the .binj and .binj.z file extensions when .json files are encountered.
		/// </summary>
		public static void registerLoadHandlers ()
		{
			if (_registeredHandlers == false) {
				flash.net.URLLoader.addLoaderHandler (".json", ".binj.z", (Func<string, flash.utils.ByteArray,flash.utils.ByteArray>)BinJsonLoaderHandler);
				flash.net.URLLoader.addLoaderHandler (".json", ".binj", (Func<string, flash.utils.ByteArray,flash.utils.ByteArray>)BinJsonLoaderHandler);
				_registeredHandlers = true;
			}
		}

		private static flash.utils.ByteArray BinJsonLoaderHandler(string path, flash.utils.ByteArray byteArray) 
		{
			string key = "binjson$$" + path;

			// uncompress data
			if (path.EndsWith(".z"))
				byteArray.uncompress();

			// create function to do parsing
			Func<string,dynamic> func = delegate(string jsonKey) { 
				return BinJSON.parseBinary(byteArray);
			};

			// store parse function in json translator
			JSON.storeJsonParseFunc(key, func);

			return flash.utils.ByteArray.fromArray(System.Text.Encoding.UTF8.GetBytes(key));
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
		/// Preserve the key order in objects.
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

		/// <summary>
		/// Use the standard JSON implementation instead of binary JSON.
		/// </summary>
		/// <value><c>true</c> if use json; otherwise, <c>false</c>.</value>
		public static bool useJson {
			get {
				return _useJson;
			}
			set {
				_useJson = value;
			}
		}

		/// <summary>
		/// True to automatically dump all binary buffers to the path specified by dumpBinaryPath.
		/// </summary>
		/// <value><c>true</c> if dump binary; otherwise, <c>false</c>.</value>
		public static bool dumpBinary {
			get {
				return _dumpBinary;
			}
			set {
				_dumpBinary = value;
			}
		}

		/// <summary>
		/// Gets or sets the dump binary path.
		/// </summary>
		/// <value>The dump binary path.</value>
		public static string dumpBinaryPath {
			get {
				return _dumpBinaryPath;
			}
			set {
				_dumpBinaryPath = value;
			}
		}


		/// <summary>
		/// Returns the CRC id for the given identifier.
		/// </summary>
		/// <returns>The crc identifier.</returns>
		/// <param name="key">The key string.</param>
		public static uint crcId(string key)
		{
			return BinJsonCrc32.Calculate (key) & 0x1FFFFFFF;
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
			public static byte* doubleStrMem;

			// Unique string dictionary
			UniqueStringDictionary stringTable = new UniqueStringDictionary();


#if PLATFORM_MONOTOUCH || PLATFORM_MONODROID
			[DllImport ("__Internal", EntryPoint="strtod")]
			public static extern double strtod (byte* start, byte* end);
#else
			[DllImport ("__Internal", EntryPoint="mono_strtod")]
			public static extern double strtod (byte* start, byte* end);
#endif

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

				// We need some memory for double string conversions
				if (doubleStrMem == null) {
					doubleStrMem = (byte*)Marshal.AllocHGlobal(64).ToPointer();
				}

			}

			public static object Parse(string jsonString) {
				uint crc1 = BinJsonCrc32.Calculate ("saveAtLocation");
				uint crc2 = BinJsonCrc32.Calculate ("save");
				using (var instance = new Parser(jsonString)) {
					if (BinJsonCrc32.Table == null)
						BinJsonCrc32.InitializeTable ();
					instance.Parse();
					BinJsonDocument doc = new BinJsonDocument (instance.data);
					return doc.GetRootObject ();
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
			public string ParseStr { get { return Marshal.PtrToStringUni (new IntPtr (json), 30); } }
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

				// Copy 4 byte words
				int words = size / 4;
				uint* src = (uint*)srcPtr;
				uint* dst = (uint*)dstPtr;
				for (int i = 0; i < words; i++)
					*dst++ = *src++;

				// Copy any remaining bytes
				int bytes = size % 4;
				if (bytes != 0) {
					byte* srcb = (byte*)src;
					byte* dstb = (byte*)dst;
					switch (bytes) {
					case 1:
						*dstb++ = *srcb++;
						break;
					case 2:
						*dstb++ = *srcb++;
						*dstb++ = *srcb++;
						break;
					case 3:
						*dstb++ = *srcb++;
						*dstb++ = *srcb++;
						*dstb++ = *srcb++;
						break;
					}
				}
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

				// Get table length
				int len = *(int*)(ptr + 4) & 0xFFFFFF;
				int hashlen = len + len / 4;  // Increase size by 25% to avoid long lookups when item is not in table

				size = hashlen * sizeof(DataElem) + 8;
				while (dataPtr + size >= dataEnd)
					ExpandData ();

				// Copy length and offset
				*(uint*)dataPtr = *(uint*)ptr;  // data offset
				dataPtr += 4;
				ptr += 4;
				*(uint*)dataPtr = (*(uint*)ptr & 0xFF000000) | (uint)hashlen;  // length & type
				dataPtr += 4;
				ptr += 4;

				// Place hashes in hash lookup order
				if (len > 0) {
					DataElem* firstElem = (DataElem*)dataPtr;
					DataElem* lastElem = firstElem + hashlen;
					DataElem* srcElem = (DataElem*)ptr;
					int i;
					for (i = 0; i < hashlen; i++)
						firstElem[i].id = firstElem[i].offset = 0;  // Make sure table is clear
					for (i = 0; i < len; i++) {
						uint crc = srcElem->id & 0x1FFFFFFF;
						uint slot = crc % (uint)hashlen;
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
						case 'u':
							if (src + 4 > srcEnd)
								throw new InvalidOperationException ("Invalid unicode literal");
							// Regular JSON parser seems to be parsing to HTML literals.. duplicate this behavior..
							if (tempDataPtr + 8 >= tempDataEnd)
								ExpandTempData ();
							*tempDataPtr++ = (byte)'&';
							*tempDataPtr++ = (byte)'#';
							*tempDataPtr++ = (byte)'x';
							*tempDataPtr++ = (byte)*src++;
							*tempDataPtr++ = (byte)*src++;
							*tempDataPtr++ = (byte)*src++;
							*tempDataPtr++ = (byte)*src++;
							d = (byte)';';
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
				uint crc = BinJsonCrc32.DefaultSeed;

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

					if (d == 0)
						throw new InvalidDataException("Null character not allowed in key strings!");

					// Add char to string
					if (dataPtr + 1 >= dataEnd)
						ExpandData ();
					*dataPtr++ = d;

					// FNV-1a hash
					crc = (crc >> 8) ^ BinJsonCrc32.Table[(byte)d ^ (byte)crc & 0xff];
				}

				crc = ~crc & 0x1FFFFFFFu; // top 3 bits 0 to allow for type bits

				// Add null terminator
				if (dataPtr + 1 >= dataEnd)
					ExpandData ();
				*dataPtr++ = 0;

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

				// Advance parse position
				json = src;

				return crc;
			}

			uint ParseNumber(uint parentDataStart, ref uint id) {

				byte* d = doubleStrMem;

				uint offset = 0;

				bool isInt = true;
				int intValue = 0;
				int sign;

				// Parse sign
				if (*json == '-') {
					sign = -1;
					*d++ = (byte)'-';
					json++;
				} else {
					sign = 1;
				}
				
				// Parse number before decimal
				char ch = '\x0';
				while (json != end) {
					ch = *json;
					if (ch < '0' || ch > '9')
						break;
					*d++ = (byte)ch;
					intValue = intValue * 10 + (ch - '0');
					json++;
				}

				// Parse decimal
				if (json != end && ch == '.') {
					*d++ = (byte)ch;
					isInt = false;
					json++;
					while (json != end) {
						ch = *json;
						if (ch < '0' || ch > '9')
							break;
						*d++ = (byte)ch;
						json++;
					}

					// Parse exponent
					if (json != end) {
						if (ch == 'e' || ch == 'E') {
							json++;
							if (json == end)
								throw new InvalidDataException ("Invalid number");
							*d++ = (byte)ch;
							ch = *json;
							if (ch == '-') {
								*d++ = (byte)ch;
								json++;
							} else if (ch == '+') {
								*d++ = (byte)ch;
								json++;
							}
							if (json == end)
								throw new InvalidDataException ("Invalid number");
							while (json != end) {
								ch = *json;
								*d++ = (byte)ch;
								if (ch < '0' || ch > '9')
									break;
								json++;
							}
						}
					}
				}

				*d++ = 0;

				// Write value (either int, float, or double)
				if (isInt && intValue > (long)int.MinValue && intValue < (long)int.MaxValue) {
					intValue = intValue * sign;
					id |= (uint)DATA_TYPE.INT << 29;
					offset = (uint)intValue;
				} else {
					double doubleValue = strtod (doubleStrMem, d);
					if (_useFloat32) {
						id |= (uint)DATA_TYPE.FLOAT << 29;
						*(float*)floatConvMem = (float)doubleValue;
						return *(uint*)floatConvMem;
					} else {
						id |= (uint)DATA_TYPE.DOUBLE << 29;
						if (tempDataPtr + 16 >= tempDataEnd)
							ExpandTempData ();
						while (((uint)tempDataPtr & 0x7) != 0) { // Align to even 8 bytes
							*tempDataPtr++ = 0;
						}
						*(double*)tempDataPtr = doubleValue;
						offset = (uint)(tempDataPtr - (tempData + parentDataStart));
						tempDataPtr += 8;
					}
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
