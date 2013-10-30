/*
 * Copyright (c) 2013 Calvin Rien
 *
 * Based on the JSON parser by Patrick van Bergen
 * http://techblog.procurios.nl/k/618/news/view/14605/14863/How-do-I-write-my-own-parser-for-JSON.html
 *
 * Simplified it so that it doesn't throw exceptions
 * and can be used in Unity iPhone with maximum code stripping.
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
 * CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using PlayScript;
using PlayScript.RuntimeBinder;
using _root;

using PTRINT = System.UInt32;

namespace playscript.utils {

	internal struct DataElem {
		public ushort id; 		// Data type in top 3 bits, plus string id in bottom 13 bits (8192 possible strings)
		public ushort offset;
	}

	internal enum LIST_TYPE : uint {
		ARRAY  	 = 1 << 24,
		OBJECT 	 = 2 << 24
	}

	internal enum DATA_TYPE : ushort {
		STRING 	 = 0,
		SMALLINT = 1,
		INT 	 = 2,
		DOUBLE 	 = 3,
		TRUE 	 = 4,
		FALSE 	 = 5,
		NULL 	 = 6,
		OBJARRAY = 7
	}

	internal unsafe class BinJsonDocument
	{
		public byte* data;				// The static data buffer
		public uint* stringTable;		// Pointer to the stringtable offset array in the static data buffer
		public Vector<string> strings;
		public string[] stringArray;

		public BinJsonDocument()
		{
			strings = new Vector<string> ();
			// Make sure we have something at element 0 so it's not used.
			strings.push ("");
		}

		public int AddStringToStringTable (string s)
		{
			strings.push (s);
			stringArray = strings._GetInnerArray ();
			return (int)strings.length;
		}
	}

	internal unsafe class BinJsonObject : IGetIndexBindable, IGetMemberBindable
	{
		protected BinJsonDocument doc;
		protected byte* list;

		private static Dictionary<Type, object> getIndexDelegates = new Dictionary<Type, object>();
		private static Dictionary<Type, object> getMemberDelegates = new Dictionary<Type, object>();

		static BinJsonObject() 
		{
			getIndexDelegates.Add (typeof(Func<CallSite, object, int, int>),    (Func<CallSite, object, int, int>)GetIndexInt);
			getIndexDelegates.Add (typeof(Func<CallSite, object, int, uint>),   (Func<CallSite, object, int, uint>)GetIndexUInt);
			getIndexDelegates.Add (typeof(Func<CallSite, object, int, double>), (Func<CallSite, object, int, double>)GetIndexDouble);
			getIndexDelegates.Add (typeof(Func<CallSite, object, int, bool>),   (Func<CallSite, object, int, bool>)GetIndexBool);
			getIndexDelegates.Add (typeof(Func<CallSite, object, int, string>), (Func<CallSite, object, int, string>)GetIndexString);
			getIndexDelegates.Add (typeof(Func<CallSite, object, int, object>), (Func<CallSite, object, int, object>)GetIndexObject);

			getIndexDelegates.Add (typeof(Func<CallSite, object, uint, int>),    (Func<CallSite, object, uint, int>)GetIndexIntU);
			getIndexDelegates.Add (typeof(Func<CallSite, object, uint, uint>),   (Func<CallSite, object, uint, uint>)GetIndexUIntU);
			getIndexDelegates.Add (typeof(Func<CallSite, object, uint, double>), (Func<CallSite, object, uint, double>)GetIndexDoubleU);
			getIndexDelegates.Add (typeof(Func<CallSite, object, uint, bool>),   (Func<CallSite, object, uint, bool>)GetIndexBoolU);
			getIndexDelegates.Add (typeof(Func<CallSite, object, uint, string>), (Func<CallSite, object, uint, string>)GetIndexStringU);
			getIndexDelegates.Add (typeof(Func<CallSite, object, uint, object>), (Func<CallSite, object, uint, object>)GetIndexObjectU);

			getIndexDelegates.Add (typeof(Func<CallSite, object, double, int>),    (Func<CallSite, object, double, int>)GetIndexIntD);
			getIndexDelegates.Add (typeof(Func<CallSite, object, double, uint>),   (Func<CallSite, object, double, uint>)GetIndexUIntD);
			getIndexDelegates.Add (typeof(Func<CallSite, object, double, double>), (Func<CallSite, object, double, double>)GetIndexDoubleD);
			getIndexDelegates.Add (typeof(Func<CallSite, object, double, bool>),   (Func<CallSite, object, double, bool>)GetIndexBoolD);
			getIndexDelegates.Add (typeof(Func<CallSite, object, double, string>), (Func<CallSite, object, double, string>)GetIndexStringD);
			getIndexDelegates.Add (typeof(Func<CallSite, object, double, object>), (Func<CallSite, object, double, object>)GetIndexObjectD);

			getIndexDelegates.Add (typeof(Func<CallSite, object, string, int>),    (Func<CallSite, object, string, int>)GetIndexIntS);
			getIndexDelegates.Add (typeof(Func<CallSite, object, string, uint>),   (Func<CallSite, object, string, uint>)GetIndexUIntS);
			getIndexDelegates.Add (typeof(Func<CallSite, object, string, double>), (Func<CallSite, object, string, double>)GetIndexDoubleS);
			getIndexDelegates.Add (typeof(Func<CallSite, object, string, bool>),   (Func<CallSite, object, string, bool>)GetIndexBoolS);
			getIndexDelegates.Add (typeof(Func<CallSite, object, string, string>), (Func<CallSite, object, string, string>)GetIndexStringS);
			getIndexDelegates.Add (typeof(Func<CallSite, object, string, object>), (Func<CallSite, object, string, object>)GetIndexObjectS);

			getIndexDelegates.Add (typeof(Func<CallSite, object, object, int>),    (Func<CallSite, object, object, int>)GetIndexIntO);
			getIndexDelegates.Add (typeof(Func<CallSite, object, object, uint>),   (Func<CallSite, object, object, uint>)GetIndexUIntO);
			getIndexDelegates.Add (typeof(Func<CallSite, object, object, double>), (Func<CallSite, object, object, double>)GetIndexDoubleO);
			getIndexDelegates.Add (typeof(Func<CallSite, object, object, bool>),   (Func<CallSite, object, object, bool>)GetIndexBoolO);
			getIndexDelegates.Add (typeof(Func<CallSite, object, object, string>), (Func<CallSite, object, object, string>)GetIndexStringO);
			getIndexDelegates.Add (typeof(Func<CallSite, object, object, object>), (Func<CallSite, object, object, object>)GetIndexObjectO);

			getMemberDelegates.Add (typeof(Func<CallSite, object, int>), (Func<CallSite, object, int>)GetMemberInt);
			getMemberDelegates.Add (typeof(Func<CallSite, object, uint>), (Func<CallSite, object, uint>)GetMemberUInt);
			getMemberDelegates.Add (typeof(Func<CallSite, object, double>), (Func<CallSite, object, double>)GetMemberDouble);
			getMemberDelegates.Add (typeof(Func<CallSite, object, bool>), (Func<CallSite, object, bool>)GetMemberBool);
			getMemberDelegates.Add (typeof(Func<CallSite, object, string>), (Func<CallSite, object, string>)GetMemberString);
			getMemberDelegates.Add (typeof(Func<CallSite, object, object>), (Func<CallSite, object, object>)GetMemberObject);
		}

		public BinJsonObject(BinJsonDocument doc, byte* list) {
			this.doc = doc;
			this.list = list;
		}

		#region GetValue Methods

		protected string GetValueString(DATA_TYPE elemType, byte* data, ushort offset)
		{
			switch (elemType) {
			case DATA_TYPE.STRING:
				byte* ptr = doc.data + doc.stringTable [offset];
				short stridx = *(short*)ptr;
				if (stridx >= 0) {
					stridx = (short)-doc.AddStringToStringTable (Marshal.PtrToStringAnsi (new IntPtr(ptr + 2), stridx));
					*(short*)ptr = stridx;
				}
				return doc.strings [-stridx];
				case DATA_TYPE.SMALLINT:
				return ((int)(short)offset).ToString ();
				case DATA_TYPE.INT:
				return (*(int*)(data + offset)).ToString();
				case DATA_TYPE.DOUBLE:
				return (*(double*)(data + offset)).ToString();
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

		protected int GetValueInt(DATA_TYPE elemType, byte* data, ushort offset)
		{
			int i;
			switch (elemType) {
				case DATA_TYPE.STRING:
				int.TryParse (GetValueString (elemType, data, offset), out i);
				return i;
				case DATA_TYPE.SMALLINT:
				return (int)(short)offset;
				case DATA_TYPE.INT:
				return (int)*(int*)(data + offset);
				case DATA_TYPE.DOUBLE:
				return (int)*(double*)(data + offset);
				case DATA_TYPE.TRUE:
				return 1;
				case DATA_TYPE.FALSE:
				case DATA_TYPE.NULL:
				case DATA_TYPE.OBJARRAY:
				return 0;
			}
			return 0;
		}

		protected uint GetValueUInt(DATA_TYPE elemType, byte* data, ushort offset)
		{
			uint i;
			switch (elemType) {
				case DATA_TYPE.STRING:
				uint.TryParse (GetValueString (elemType, data, offset), out i);
				return i;
				case DATA_TYPE.SMALLINT:
				return (uint)(short)offset;
				case DATA_TYPE.INT:
				return (uint)*(int*)(data + offset);
				case DATA_TYPE.DOUBLE:
				return (uint)*(double*)(data + offset);
				case DATA_TYPE.TRUE:
				return 1u;
				case DATA_TYPE.FALSE:
				case DATA_TYPE.NULL:
				case DATA_TYPE.OBJARRAY:
				return 0u;
			}
			return 0;
		}

		protected double GetValueDouble(DATA_TYPE elemType, byte* data, ushort offset)
		{
			double d;
			switch (elemType) {
				case DATA_TYPE.STRING:
				double.TryParse (GetValueString (elemType, data, offset), out d);
				return d;
				case DATA_TYPE.SMALLINT:
				return (double)(short)offset;
				case DATA_TYPE.INT:
				return (double)*(int*)(data + offset);
				case DATA_TYPE.DOUBLE:
				return *(double*)(data + offset);
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

		protected bool GetValueBool(DATA_TYPE elemType, byte* data, ushort offset)
		{
			switch (elemType) {
				case DATA_TYPE.STRING:
				string s = GetValueString (elemType, data, offset);
				if (s == "1" || s == "true")
					return true;
				return false;
				case DATA_TYPE.SMALLINT:
				return offset != 0;
				case DATA_TYPE.INT:
				return *(int*)(data + offset) != 0;
				case DATA_TYPE.DOUBLE:
				return *(double*)(data + offset) != 0.0;
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

		protected object GetValueObject(DATA_TYPE elemType, byte* data, ushort offset)
		{
			switch (elemType) {
				case DATA_TYPE.STRING:
				return GetValueString (elemType, data, offset);
				case DATA_TYPE.SMALLINT:
				return (int)(short)offset;
				case DATA_TYPE.INT:
				return *(int*)(data + offset);
				case DATA_TYPE.DOUBLE:
				return *(double*)(data + offset);
				case DATA_TYPE.TRUE:
				return true;
				case DATA_TYPE.FALSE:
				return false;
				case DATA_TYPE.NULL:
				return null;
				case DATA_TYPE.OBJARRAY:
				byte* list = doc.data + *(uint*)(data + offset);
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

		#region

		private static string GetIndexString(CallSite site, object obj, int index)
		{
			var bsonObj = (BinJsonObject)obj;
			if (obj == null) {
				site.SetTarget (((PSGetIndexBinder)site.Binder).Bind (typeof(Func<CallSite,object,int,string>)));
				return ((CallSite<Func<CallSite,object,int,string>>)site).Target(site, obj, index);
			}
			byte* list = bsonObj.list;
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			len &= 0xFFFFFF;
			if (listType == LIST_TYPE.ARRAY && index < len) {
				DataElem* dataElem = (DataElem*)(list + 8) [index];
				DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 13);
				return bsonObj.GetValueString(dataType, bsonObj.doc.data + *(uint*)list, dataElem->offset);
			} else {
				throw new NotSupportedException ();
			}
		}

		private static string GetIndexStringU(CallSite site, object obj, uint index)
		{
			return GetIndexString (site, obj, (int)index);
		}

		private static string GetIndexStringD(CallSite site, object obj, double index)
		{
			return GetIndexString (site, obj, (int)index);
		}

		private static string GetIndexStringO(CallSite site, object obj, object index)
		{
			if (index is int)
				return GetIndexString (site, obj, (int)index);
			else if (index is uint)
				return GetIndexString (site, obj, (int)(uint)index);
			else if (index is double)
				return GetIndexString (site, obj, (int)(double)index);
			else
				throw new NotSupportedException ();
		}

		private static int GetIndexInt(CallSite site, object obj, int index)
		{
			var bsonObj = (BinJsonObject)obj;
			if (obj == null) {
				site.SetTarget (((PSGetIndexBinder)site.Binder).Bind (typeof(Func<CallSite,object,int,int>)));
				return ((CallSite<Func<CallSite,object,int,int>>)site).Target(site, obj, index);
			}
			byte* list = bsonObj.list;
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			len &= 0xFFFFFF;
			if (listType == LIST_TYPE.ARRAY && index < len) {
				DataElem* dataElem = (DataElem*)(list + 8) [index];
				DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 13);
				return bsonObj.GetValueInt(dataType, bsonObj.doc.data + *(uint*)list, dataElem->offset);
			} else {
				throw new NotSupportedException ();
			}
		}

		private static int GetIndexIntU(CallSite site, object obj, uint index)
		{
			return GetIndexInt (site, obj, (int)index);
		}

		private static int GetIndexIntD(CallSite site, object obj, double index)
		{
			return GetIndexInt (site, obj, (int)index);
		}

		private static int GetIndexIntO(CallSite site, object obj, object index)
		{
			if (index is int)
				return GetIndexInt (site, obj, (int)index);
			else if (index is uint)
				return GetIndexInt (site, obj, (int)(uint)index);
			else if (index is double)
				return GetIndexInt (site, obj, (int)(double)index);
			else
				throw new NotSupportedException ();
		}

		private static double GetIndexDouble(CallSite site, object obj, int index)
		{
			var bsonObj = (BinJsonObject)obj;
			if (obj == null) {
				site.SetTarget (((PSGetIndexBinder)site.Binder).Bind (typeof(Func<CallSite,object,int,double>)));
				return ((CallSite<Func<CallSite,object,int,double>>)site).Target(site, obj, index);
			}
			byte* list = bsonObj.list;
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			len &= 0xFFFFFF;
			if (listType == LIST_TYPE.ARRAY && index < len) {
				DataElem* dataElem = (DataElem*)(list + 8) [index];
				DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 13);
				return bsonObj.GetValueDouble(dataType, bsonObj.doc.data + *(uint*)(list), dataElem->offset);
			} else {
				throw new NotSupportedException ();
			}
		}

		private static double GetIndexDoubleU(CallSite site, object obj, uint index)
		{
			return GetIndexDouble (site, obj, (int)index);
		}

		private static double GetIndexDoubleD(CallSite site, object obj, double index)
		{
			return GetIndexDouble (site, obj, (int)index);
		}

		private static double GetIndexDoubleO(CallSite site, object obj, object index)
		{
			if (index is int)
				return GetIndexDouble (site, obj, (int)index);
			else if (index is uint)
				return GetIndexDouble (site, obj, (int)(uint)index);
			else if (index is double)
				return GetIndexDouble (site, obj, (int)(double)index);
			else
				throw new NotSupportedException ();
		}

		private static uint GetIndexUInt(CallSite site, object obj, int index)
		{
			var bsonObj = (BinJsonObject)obj;
			if (obj == null) {
				site.SetTarget (((PSGetIndexBinder)site.Binder).Bind (typeof(Func<CallSite,object,int,uint>)));
				return ((CallSite<Func<CallSite,object,int,uint>>)site).Target(site, obj, index);
			}
			byte* list = bsonObj.list;
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			len &= 0xFFFFFF;
			if (listType == LIST_TYPE.ARRAY && index < len) {
				DataElem* dataElem = (DataElem*)(list + 8) [index];
				DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 13);
				return bsonObj.GetValueUInt(dataType, bsonObj.doc.data + *(uint*)list, dataElem->offset);
			} else {
				throw new NotSupportedException ();
			}
		}

		private static uint GetIndexUIntU(CallSite site, object obj, uint index)
		{
			return GetIndexUInt (site, obj, (int)index);
		}

		private static uint GetIndexUIntD(CallSite site, object obj, double index)
		{
			return GetIndexUInt (site, obj, (int)index);
		}

		private static uint GetIndexUIntO(CallSite site, object obj, object index)
		{
			if (index is int)
				return GetIndexUInt (site, obj, (int)index);
			else if (index is uint)
				return GetIndexUInt (site, obj, (int)(uint)index);
			else if (index is double)
				return GetIndexUInt (site, obj, (int)(double)index);
			else
				throw new NotSupportedException ();
		}

		private static bool GetIndexBool(CallSite site, object obj, int index)
		{
			var bsonObj = (BinJsonObject)obj;
			if (obj == null) {
				site.SetTarget (((PSGetIndexBinder)site.Binder).Bind (typeof(Func<CallSite,object,int,bool>)));
				return ((CallSite<Func<CallSite,object,int,bool>>)site).Target(site, obj, index);
			}
			byte* list = bsonObj.list;
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			len &= 0xFFFFFF;
			if (listType == LIST_TYPE.ARRAY && index < len) {
				DataElem* dataElem = (DataElem*)(list + 8) [index];
				DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 13);
				return bsonObj.GetValueBool(dataType, bsonObj.doc.data + *(uint*)list, dataElem->offset);
			} else {
				throw new NotSupportedException ();
			}
		}

		private static bool GetIndexBoolU(CallSite site, object obj, uint index)
		{
			return GetIndexBool (site, obj, (int)index);
		}

		private static bool GetIndexBoolD(CallSite site, object obj, double index)
		{
			return GetIndexBool (site, obj, (int)index);
		}

		private static bool GetIndexBoolO(CallSite site, object obj, object index)
		{
			if (index is int)
				return GetIndexBool (site, obj, (int)index);
			else if (index is uint)
				return GetIndexBool (site, obj, (int)(uint)index);
			else if (index is double)
				return GetIndexBool (site, obj, (int)(double)index);
			else
				throw new NotSupportedException ();
		}

		private static object GetIndexObject(CallSite site, object obj, int index)
		{
			var bsonObj = (BinJsonObject)obj;
			if (obj == null) {
				site.SetTarget (((PSGetIndexBinder)site.Binder).Bind (typeof(Func<CallSite,object,int,object>)));
				return ((CallSite<Func<CallSite,object,int,object>>)site).Target(site, obj, index);
			}
			byte* list = bsonObj.list;
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			len &= 0xFFFFFF;
			if (listType == LIST_TYPE.ARRAY && index < len) {
				DataElem* dataElem = (DataElem*)(list + 8) [index];
				DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 13);
				return bsonObj.GetValueObject(dataType, bsonObj.doc.data + *(uint*)list, dataElem->offset);
			} else {
				throw new NotSupportedException ();
			}
		}

		private static object GetIndexObjectU(CallSite site, object obj, uint index)
		{
			return GetIndexObject (site, obj, (int)index);
		}

		private static object GetIndexObjectD(CallSite site, object obj, double index)
		{
			return GetIndexObject (site, obj, (int)index);
		}

		private static object GetIndexObjectO(CallSite site, object obj, object index)
		{
			if (index is int)
				return GetIndexObject (site, obj, (int)index);
			else if (index is uint)
				return GetIndexObject (site, obj, (int)(uint)index);
			else if (index is double)
				return GetIndexObject (site, obj, (int)(double)index);
			else
				throw new NotSupportedException ();
		}

		private static string GetIndexStringS(CallSite site, object obj, string key)
		{
			var bsonObj = (BinJsonObject)obj;
			if (obj == null) {
				site.SetTarget (((PSGetIndexBinder)site.Binder).Bind (typeof(Func<CallSite,object,string, string>)));
				return ((CallSite<Func<CallSite,object,string,string>>)site).Target(site, obj, key);
			}
			if (key == null || key.Length == 0)
				return null;
			byte firstchar = (byte)key [0];
			byte* list = bsonObj.list;
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			if (listType == LIST_TYPE.OBJECT) {
				len &= 0xFFFFFF;
				DataElem* dataElem = bsonObj.FindDataElemForKey (firstchar, key, (DataElem*)(list + 8), len);
				if (dataElem != null) {
					DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 13);
					return bsonObj.GetValueString (dataType, bsonObj.doc.data + *(uint*)list, dataElem->offset);
				} else {
					return null;
				}
			} else {
				int i;
				if (int.TryParse (key, out i)) 
					return GetIndexString (site, obj, i);
				else
					return null;
			}
		}

		private static int GetIndexIntS(CallSite site, object obj, string key)
		{
			var bsonObj = (BinJsonObject)obj;
			if (obj == null) {
				site.SetTarget (((PSGetIndexBinder)site.Binder).Bind (typeof(Func<CallSite,object,string,int>)));
				return ((CallSite<Func<CallSite,object,string,int>>)site).Target(site, obj, key);
			}
			if (key == null || key.Length == 0)
				return 0;
			byte firstchar = (byte)key [0];
			byte* list = bsonObj.list;
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			if (listType == LIST_TYPE.OBJECT) {
				len &= 0xFFFFFF;
				DataElem* dataElem = bsonObj.FindDataElemForKey (firstchar, key, (DataElem*)(list + 8), len);
				if (dataElem != null) {
					DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 13);
					return bsonObj.GetValueInt (dataType, bsonObj.doc.data + *(uint*)list, dataElem->offset);
				} else {
					return 0;
				}
			} else {
				int i;
				if (int.TryParse (key, out i)) 
					return GetIndexInt (site, obj, i);
				else
					return 0;
			}
		}

		private static double GetIndexDoubleS(CallSite site, object obj, string key)
		{
			var bsonObj = (BinJsonObject)obj;
			if (obj == null) {
				site.SetTarget (((PSGetIndexBinder)site.Binder).Bind (typeof(Func<CallSite,object,string,double>)));
				return ((CallSite<Func<CallSite,object,string,double>>)site).Target(site, obj,key);
			}
			if (key == null || key.Length == 0)
				return 0.0;
			byte firstchar = (byte)key [0];
			byte* list = bsonObj.list;
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			if (listType == LIST_TYPE.OBJECT) {
				len &= 0xFFFFFF;
				DataElem* dataElem = bsonObj.FindDataElemForKey (firstchar, key, (DataElem*)(list + 8), len);
				if (dataElem != null) {
					DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 13);
					return bsonObj.GetValueDouble (dataType, bsonObj.doc.data + *(uint*)list, dataElem->offset);
				} else {
					return 0.0;
				}
			} else {
				int i;
				int.TryParse (key, out i);
				if (int.TryParse (key, out i)) 
					return GetIndexDouble (site, obj, i);
				else
					return 0.0;
			}
		}

		private static uint GetIndexUIntS(CallSite site, object obj, string key)
		{
			var bsonObj = (BinJsonObject)obj;
			if (obj == null) {
				site.SetTarget (((PSGetIndexBinder)site.Binder).Bind (typeof(Func<CallSite,object,string,uint>)));
				return ((CallSite<Func<CallSite,object,string,uint>>)site).Target(site, obj,key);
			}
			if (key == null || key.Length == 0)
				return 0u;
			byte firstchar = (byte)key [0];
			byte* list = bsonObj.list;
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			if (listType == LIST_TYPE.OBJECT) {
				len &= 0xFFFFFF;
				DataElem* dataElem = bsonObj.FindDataElemForKey (firstchar, key, (DataElem*)(list + 8), len);
				if (dataElem != null) {
					DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 13);
					return bsonObj.GetValueUInt (dataType, bsonObj.doc.data + *(uint*)list, dataElem->offset);
				} else {
					return 0u;
				}
			} else {
				int i;
				if (int.TryParse (key, out i)) 
					return GetIndexUInt (site, obj, i);
				else
					return 0u;
			}
		}

		private static bool GetIndexBoolS(CallSite site, object obj, string key)
		{
			var bsonObj = (BinJsonObject)obj;
			if (obj == null) {
				site.SetTarget (((PSGetIndexBinder)site.Binder).Bind (typeof(Func<CallSite,object,string,bool>)));
				return ((CallSite<Func<CallSite,object,string,bool>>)site).Target(site, obj, key);
			}
			if (key == null || key.Length == 0)
				return false;
			byte firstchar = (byte)key [0];
			byte* list = bsonObj.list;
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			if (listType == LIST_TYPE.OBJECT) {
				len &= 0xFFFFFF;
				DataElem* dataElem = bsonObj.FindDataElemForKey (firstchar, key, (DataElem*)(list + 8), len);
				if (dataElem != null) {
					DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 13);
					return bsonObj.GetValueBool (dataType, bsonObj.doc.data + *(uint*)list, dataElem->offset);
				} else {
					return false;
				}
			} else {
				int i;
				if (int.TryParse (key, out i)) 
					return GetIndexBool (site, obj, i);
				else
					return false;
			}
		}

		private static object GetIndexObjectS(CallSite site, object obj, string key)
		{
			var bsonObj = (BinJsonObject)obj;
			if (obj == null) {
				site.SetTarget (((PSGetIndexBinder)site.Binder).Bind (typeof(Func<CallSite,object,string,object>)));
				return ((CallSite<Func<CallSite,object,string,object>>)site).Target(site, obj, key);
			}
			if (key == null || key.Length == 0)
				return null;
			byte firstchar = (byte)key [0];
			byte* list = bsonObj.list;
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			if (listType == LIST_TYPE.OBJECT) {
				len &= 0xFFFFFF;
				DataElem* dataElem = bsonObj.FindDataElemForKey (firstchar, key, (DataElem*)(list + 8), len);
				if (dataElem != null) {
					DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 13);
					return bsonObj.GetValueObject (dataType, bsonObj.doc.data + *(uint*)list, dataElem->offset);
				} else {
					return null;
				}
			} else {
				int i;
				if (int.TryParse (key, out i)) 
					return GetIndexObject (site, obj, i);
				else
					return null;
			}
		}

		object IGetIndexBindable.BindGetIndex (Type delegateType)
		{
			object target;
			if (getIndexDelegates.TryGetValue (delegateType, out target)) {
				return target;
			}
			throw new Exception ("Unable to bind get index for target " + delegateType.FullName);
		}

		#endregion

		#region Implementation of IGetMemberBindable

		private DataElem* FindDataElemForKey(byte firstchar, string key, DataElem* dataElem, uint len)
		{
			// Do fast linear search..  fast out on first char.
			byte* data = doc.data;
			uint* strTable = doc.stringTable;
			for (var i = 0; i < len; i++) {
				byte* id = data + strTable[(int)dataElem->id & 0x1FFF];
				if (firstchar == *id) {
					id++;
					bool matches = true;
					byte ch = *id; 
					int j = 1;
					int keylen = key.Length;
					while (j < keylen && ch != 0) {
						if ((byte)key[j] != ch) {
							matches = false;
							break;
						}
						ch = *id++;
					}
					if (matches)
						return dataElem;
				}
				dataElem++;
			}
			return null;
		}

		private static string GetMemberString(CallSite site, object obj)
		{
			var bsonObj = (BinJsonObject)obj;
			if (obj == null) {
				site.SetTarget (((PSGetMemberBinder)site.Binder).Bind (typeof(Func<CallSite,object,string>)));
				return ((CallSite<Func<CallSite,object,string>>)site).Target(site, obj);
			}
			string key = ((PSGetMemberBinder)site.Binder).name;
			byte firstchar = (byte)key [0];
			byte* list = bsonObj.list;
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			if (listType == LIST_TYPE.OBJECT) {
				len &= 0xFFFFFF;
				DataElem* dataElem = bsonObj.FindDataElemForKey (firstchar, key, (DataElem*)(list + 8), len);
				if (dataElem != null) {
					DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 13);
					return bsonObj.GetValueString (dataType, bsonObj.doc.data + *(uint*)list, dataElem->offset);
				} else {
					return null;
				}
			} else {
				int i;
				if (int.TryParse (key, out i)) 
					return GetIndexString (site, obj, i);
				else
					return null;
			}
		}

		private static int GetMemberInt(CallSite site, object obj)
		{
			var bsonObj = (BinJsonObject)obj;
			if (obj == null) {
				site.SetTarget (((PSGetMemberBinder)site.Binder).Bind (typeof(Func<CallSite,object,int>)));
				return ((CallSite<Func<CallSite,object,int>>)site).Target(site, obj);
			}
			string key = ((PSGetMemberBinder)site.Binder).name;
			byte firstchar = (byte)key [0];
			byte* list = bsonObj.list;
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			if (listType == LIST_TYPE.OBJECT) {
				len &= 0xFFFFFF;
				DataElem* dataElem = bsonObj.FindDataElemForKey (firstchar, key, (DataElem*)(list + 8), len);
				if (dataElem != null) {
					DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 13);
					return bsonObj.GetValueInt (dataType, bsonObj.doc.data + *(uint*)list, dataElem->offset);
				} else {
					return 0;
				}
			} else {
				int i;
				if (int.TryParse (key, out i)) 
					return GetIndexInt (site, obj, i);
				else
					return 0;
			}
		}

		private static double GetMemberDouble(CallSite site, object obj)
		{
			var bsonObj = (BinJsonObject)obj;
			if (obj == null) {
				site.SetTarget (((PSGetMemberBinder)site.Binder).Bind (typeof(Func<CallSite,object,double>)));
				return ((CallSite<Func<CallSite,object,double>>)site).Target(site, obj);
			}
			string key = ((PSGetMemberBinder)site.Binder).name;
			byte firstchar = (byte)key [0];
			byte* list = bsonObj.list;
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			if (listType == LIST_TYPE.OBJECT) {
				len &= 0xFFFFFF;
				DataElem* dataElem = bsonObj.FindDataElemForKey (firstchar, key, (DataElem*)(list + 8), len);
				if (dataElem != null) {
					DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 13);
					return bsonObj.GetValueDouble (dataType, bsonObj.doc.data + *(uint*)list, dataElem->offset);
				} else {
					return 0.0;
				}
			} else {
				int i;
				int.TryParse (key, out i);
				if (int.TryParse (key, out i)) 
					return GetIndexDouble (site, obj, i);
				else
					return 0.0;
			}
		}

		private static uint GetMemberUInt(CallSite site, object obj)
		{
			var bsonObj = (BinJsonObject)obj;
			if (obj == null) {
				site.SetTarget (((PSGetMemberBinder)site.Binder).Bind (typeof(Func<CallSite,object,uint>)));
				return ((CallSite<Func<CallSite,object,uint>>)site).Target(site, obj);
			}
			string key = ((PSGetMemberBinder)site.Binder).name;
			byte firstchar = (byte)key [0];
			byte* list = bsonObj.list;
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			if (listType == LIST_TYPE.OBJECT) {
				len &= 0xFFFFFF;
				DataElem* dataElem = bsonObj.FindDataElemForKey (firstchar, key, (DataElem*)(list + 8), len);
				if (dataElem != null) {
					DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 13);
					return bsonObj.GetValueUInt (dataType, bsonObj.doc.data + *(uint*)list, dataElem->offset);
				} else {
					return 0u;
				}
			} else {
				int i;
				if (int.TryParse (key, out i)) 
					return GetIndexUInt (site, obj, i);
				else
					return 0u;
			}
		}

		private static bool GetMemberBool(CallSite site, object obj)
		{
			var bsonObj = (BinJsonObject)obj;
			if (obj == null) {
				site.SetTarget (((PSGetMemberBinder)site.Binder).Bind (typeof(Func<CallSite,object,bool>)));
				return ((CallSite<Func<CallSite,object,bool>>)site).Target(site, obj);
			}
			string key = ((PSGetMemberBinder)site.Binder).name;
			byte firstchar = (byte)key [0];
			byte* list = bsonObj.list;
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			if (listType == LIST_TYPE.OBJECT) {
				len &= 0xFFFFFF;
				DataElem* dataElem = bsonObj.FindDataElemForKey (firstchar, key, (DataElem*)(list + 8), len);
				if (dataElem != null) {
					DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 13);
					return bsonObj.GetValueBool (dataType, bsonObj.doc.data + *(uint*)list, dataElem->offset);
				} else {
					return false;
				}
			} else {
				int i;
				if (int.TryParse (key, out i)) 
					return GetIndexBool (site, obj, i);
				else
					return false;
			}
		}

		private static object GetMemberObject(CallSite site, object obj)
		{
			var bsonObj = (BinJsonObject)obj;
			if (obj == null) {
				site.SetTarget (((PSGetMemberBinder)site.Binder).Bind (typeof(Func<CallSite,object,object>)));
				return ((CallSite<Func<CallSite,object,object>>)site).Target(site, obj);
			}
			string key = ((PSGetMemberBinder)site.Binder).name;
			byte firstchar = (byte)key [0];
			byte* list = bsonObj.list;
			uint len = *(uint*)(list + 4);
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			if (listType == LIST_TYPE.OBJECT) {
				len &= 0xFFFFFF;
				DataElem* dataElem = bsonObj.FindDataElemForKey (firstchar, key, (DataElem*)(list + 8), len);
				if (dataElem != null) {
					DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 13);
					return bsonObj.GetValueObject (dataType, bsonObj.doc.data + *(uint*)list, dataElem->offset);
				} else {
					return null;
				}
			} else {
				int i;
				if (int.TryParse (key, out i)) 
					return GetIndexObject (site, obj, i);
				else
					return null;
			}
		}

		object IGetMemberBindable.BindGetMember (Type delegateType)
		{
			object target;
			if (getMemberDelegates.TryGetValue (delegateType, out target)) {
				return target;
			}
			throw new Exception("Unable to bind get member for target " + delegateType.FullName);
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

		public string _GetStringAt(uint index)
		{
			uint len = (uint)*(ushort*)(list + 4) & 0xFFFFFF;
			if (index < len) {
				DataElem* dataElem = (DataElem*)(list + 8) [index];
				DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 13);
				return GetValueString(dataType, doc.data + *(int*)list, dataElem->offset);
			} else {
				return null;
			}
		}

		public int _GetIntAt(uint index)
		{
			uint len = *(uint*)(list + 4) & 0xFFFFFF;
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			if (index < len) {
				byte* data = doc.data + *(int*)(list);
				DataElem* dataElem = (DataElem*)(list + 8) [index];
				DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 13);
				return GetValueInt(dataType, doc.data + *(int*)list, dataElem->offset);
			} else {
				return 0;
			}
		}

		public double _GetDoubleAt(uint index)
		{
			uint len = *(uint*)(list + 4) & 0xFFFFFF;
			LIST_TYPE listType = (LIST_TYPE)(len & 0xFF000000);
			if (index < len) {
				byte* data = doc.data + *(int*)(list);
				DataElem* dataElem = (DataElem*)(list + 8) [index];
				DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 13);
				return GetValueDouble(dataType, doc.data + *(int*)list, dataElem->offset);
			} else {
				return double.NaN;
			}
		}

		public uint _GetUIntAt(uint index)
		{
			uint len = *(uint*)(list + 4) & 0xFFFFFF;
			if (index < len) {
				byte* data = doc.data + *(int*)(list);
				DataElem* dataElem = (DataElem*)(list + 8) [index];
				DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 13);
				return GetValueUInt(dataType, doc.data + *(int*)list, dataElem->offset);
			} else {
				return 0u;
			}
		}

		public bool _GetBoolAt(uint index)
		{
			uint len = *(uint*)(list + 4) & 0xFFFFFF;
			if (index < len) {
				byte* data = doc.data + *(int*)(list);
				DataElem* dataElem = (DataElem*)(list + 8) [index];
				DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 13);
				return GetValueBool(dataType, doc.data + *(int*)list, dataElem->offset);
			} else {
				return false;
			}
		}

		public object _GetObjectAt(uint index)
		{
			uint len = *(uint*)(list + 4) & 0xFFFFFF;
			if (index < len) {
				byte* data = doc.data + *(int*)(list);
				DataElem* dataElem = (DataElem*)(list + 8) [index];
				DATA_TYPE dataType = (DATA_TYPE)(dataElem->id >> 13);
				return GetValueObject(dataType, doc.data + *(int*)list, dataElem->offset);
			} else {
				return null;
			}
		}

		public uint length {
			get {
				return (uint)*(ushort*)(list + 4) & 0xFFFFFF;
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
					return _array._GetObjectAt ((uint)_index);
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

	/// <summary>
	/// This class encodes and decodes JSON strings.
	/// Spec. details, see http://www.json.org/
	///
	/// JSON uses Arrays and Objects. These correspond here to the datatypes IList and IDictionary.
	/// All numbers are parsed to doubles.
	/// </summary>
	public unsafe static class BinJSON {

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

		private sealed unsafe class Parser : IDisposable {

			// Maximum number of unique key strings
			public const int MAX_NUM_UNIQUE_KEYS = 0x1FFF;  // Top 3 bits reserved for data type.. 8192 unique key strings

			// Default pointer size
			public const int OFFSET_SIZE = sizeof(uint);

			// Size increment to grow buffers with
			public const int GROW_SIZE = 0x4000; // 16K

			// Default ratio of the binary data buffer to allocate to the JSON string size
			public const double JSON_TO_BINARY_SIZE_RATIO = 0.5;

			// Initial TEMP buffer size
			public const int INITIAL_TEMP_BUF_SIZE = GROW_SIZE * 4;

			// Initial TEMP DATA buffer size
			public const int INITIAL_TEMP_DATA_BUF_SIZE = GROW_SIZE * 4;

			// Initial TEMP string buffer size
			public const int INITIAL_TEMP_STRING_BUF_SIZE = GROW_SIZE * 4;

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

			// Unique string dictionary
			UniqueStringDictionary stringTable = new UniqueStringDictionary();

			Parser(string jsonString) 
			{
				gch = GCHandle.Alloc(jsonString, GCHandleType.Pinned);
				json = (char*)gch.AddrOfPinnedObject().ToPointer();
				end = json + jsonString.Length;

				dataSize = (int)(jsonString.Length * JSON_TO_BINARY_SIZE_RATIO / GROW_SIZE) * GROW_SIZE;
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
			}

			public static object Parse(string jsonString) {
				using (var instance = new Parser(jsonString)) {
					instance.Parse();
					BinJsonDocument doc = new BinJsonDocument ();
					doc.data = instance.data;
					doc.stringTable = (uint*)(instance.data + *(uint*)(instance.data + 16));
					return new BinJsonObject (doc, doc.data + *(uint*)(doc.data + 12));
				}
			}

			public void Dispose() {
				gch.Free ();
				json = null;
			}

			public void MemCopy(void* srcPtr, void* dstPtr, int size)
			{
				size = size >> 2;
				uint* src = (uint*)srcPtr;
				uint* dst = (uint*)dstPtr;
				for (var i = 0; i < size; i++)
					*src++ = *dst++;
			}

			public void ExpandData()
			{
				int curPos = (int)(dataPtr - data);
				int newSize = dataSize + GROW_SIZE;
				byte* newData = (byte*)Marshal.AllocHGlobal (newSize).ToPointer();
				MemCopy (data, newData, curPos);
				Marshal.FreeHGlobal (new IntPtr(data));
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
				tempString = newtempString;
				tempStringEnd = tempString + newSize;
				tempStringPtr = tempString + curPos;
			}

			void CopyToData(void* ptr, int size)
			{
				while (dataPtr + size > dataEnd)
					ExpandData ();
				MemCopy (ptr, data, size);
				dataPtr += size;
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
					ParseObject (tempDataPtr);
				} else if (token == TOKEN.SQUARED_OPEN) {
					ParseArray (tempDataPtr);
				} else {
					throw new InvalidDataException ("Parse error");
				}

				// Last 4 bytes will be offset address of root array/object - we don't need it.
				dataPtr -= 4;

				// Copy pointer at end of buffer to offset at 12
				*(uint*)(data + 12) = *(uint*)(dataPtr);

				// Write key string table (and write offset to location at offset 16)
				*(uint*)(data + 16) = (uint)(dataPtr - data);
				CopyToData (tempString, (int)(tempStringPtr - tempString));
				*(uint*)(data + 20) = (uint)(tempStringPtr - tempString) / 4; // String table length

				// Write final size
				*(uint*)(data + 4) = (uint)(dataPtr - data - 8);  // Size is length of bytes after size field
			}

			ushort ParseObject(byte* parentStart) 
			{
				if (temp + 16 >= tempEnd)
					ExpandTemp ();

				byte* tempStart = temp;
				byte* tempDataStart = tempData;

				*(int*)tempPtr++ = 0;
				*(int*)tempPtr++ = (int)LIST_TYPE.OBJECT;

				int len = 0;

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
						// name
						int stridx = ParseKeyString ();
						if (stridx > MAX_NUM_UNIQUE_KEYS) {
							throw new InvalidOperationException ("Too many unique key ids");
						}

						DataElem* dataElem = (DataElem*)tempPtr;
						tempPtr += sizeof(DataElem);
						dataElem->id = (ushort)stridx;

						// :
						token = GetNextToken ();
						if (token != TOKEN.COLON) {
							throw new InvalidOperationException ("Colon expected.");
						}

						token = GetNextToken ();

						// value
						switch (token) {
							case TOKEN.STRING:
							dataElem->id |= (ushort)DATA_TYPE.STRING << 13;
							dataElem->offset = ParseDataString (tempDataStart);
							break;
							case TOKEN.NUMBER:
							ParseNumber (dataElem, tempDataStart);
							break;
							case TOKEN.CURLY_OPEN:
							dataElem->id |= (ushort)DATA_TYPE.OBJARRAY << 13;
							dataElem->offset = ParseObject (tempDataStart);
							case TOKEN.SQUARED_OPEN:
							dataElem->id |= (ushort)DATA_TYPE.OBJARRAY << 13;
							dataElem->offset = ParseArray (tempDataStart);
							case TOKEN.TRUE:
							dataElem->id |= (ushort)DATA_TYPE.TRUE << 13;
							dataElem->offset = 0;
							break;
							case TOKEN.FALSE:
							dataElem->id |= (ushort)DATA_TYPE.FALSE << 13;
							dataElem->offset = 0;
							break;
							default:
							dataElem->id |= (ushort)DATA_TYPE.NULL << 13;
							dataElem->offset = 0;
							break;
						}

						len++;
						break;
					}
				} while (parsing);

				// Set length in lower 24 bits (upper 8 bits is type)
				*(int*)(tempStart + 4) |= (int)(len & 0xFFFFFF);

				// Copy elem array to data buffer
				byte* dataStart = dataPtr;
				CopyToData (tempStart, (int)(tempPtr - tempStart));

				// Align to 8 byte boundry
				uint align = (uint)((uint)dataPtr & 0xFFFFFFF8u);
				if (align == 4) {
					*(int*)dataPtr = 0;
					dataPtr += 4;
				}

				// Set relative ptr to first data elem
				*(int*)dataPtr = (int)(dataPtr - data); 

				// Copy data to data buffer
				CopyToData (tempDataStart, (int)(tempDataPtr - tempData));
				*(int*)(dataStart) |= (int)(dataPtr - data);

				// Restore current temp stacks
				tempPtr = tempStart;
				tempDataPtr = tempDataStart;

				// Add relative pointer to this object to parent's data
				*(uint*)tempDataPtr = (uint)(dataStart - data);
				tempDataPtr += OFFSET_SIZE;

				// Return the offset to the pointer
				return (ushort)(tempDataPtr - parentStart - OFFSET_SIZE); 
			}

			ushort ParseArray(byte* parentStart) 
			{
				if (temp + 16 >= tempEnd)
					ExpandTemp ();

				byte* tempStart = temp;
				byte* tempDataStart = tempData;

				*(int*)tempPtr++ = 0;
				*(int*)tempPtr++ = (int)LIST_TYPE.ARRAY;

				int len = 0;

				DataElem* dataElem = (DataElem*)tempPtr;

				// [
				bool parsing = true;
				while (true) {

					if (tempPtr + sizeof(DataElem) >= tempEnd)
						ExpandTemp ();

					TOKEN token = GetNextToken ();

					switch (token) {
					case TOKEN.COMMA:
						continue;
					case TOKEN.SQUARED_CLOSE:
						parsing = false;
						break;
					case TOKEN.STRING:
						dataElem->id = (ushort)DATA_TYPE.STRING << 13;
						dataElem->offset = ParseDataString (tempDataStart);
						break;
					case TOKEN.NUMBER:
						dataElem->id = 0;
						ParseNumber (dataElem, tempDataStart);
						break;
					case TOKEN.CURLY_OPEN:
						dataElem->id = (ushort)DATA_TYPE.OBJARRAY << 13;
						dataElem->offset = ParseObject (tempDataStart);
						break;
					case TOKEN.SQUARED_OPEN:
						dataElem->id = (ushort)DATA_TYPE.OBJARRAY << 13;
						dataElem->offset = ParseArray (tempDataStart);
						break;
					case TOKEN.TRUE:
						dataElem->id = (ushort)DATA_TYPE.TRUE << 13;
						dataElem->offset = 0;
						break;
					case TOKEN.FALSE:
						dataElem->id = (ushort)DATA_TYPE.FALSE << 13;
						dataElem->offset = 0;
						dataElem++;
						break;
					case TOKEN.NULL:
						dataElem = (DataElem*)tempPtr;
						tempPtr += sizeof(DataElem);
						dataElem->id |= (ushort)DATA_TYPE.NULL << 13;
						dataElem->offset = 0;
						dataElem++;
						break;
					default:
						throw new InvalidDataException("Parse error");
					}

					if (!parsing)
						break;

					tempPtr += sizeof(DataElem);
					dataElem++;
					len++;

				} while (parsing);

				// Set length in lower 24 bits (upper 8 bits is type)
				*(int*)(tempStart + 4) |= (int)(len & 0xFFFFFF);

				// Copy elem array to data buffer
				byte* dataStart = dataPtr;
				CopyToData (tempStart, (int)(tempPtr - tempStart));

				// Align to 8 byte boundry
				uint align = (uint)((uint)dataPtr & 0xFFFFFFF8u);
				if (align == 4) {
					*(int*)dataPtr = 0;
					dataPtr += 4;
				}

				// Set relative ptr to first data elem
				*(int*)dataPtr = (int)(dataPtr - data); 

				// Copy data to data buffer
				CopyToData (tempDataStart, (int)(tempDataPtr - tempData));
				*(int*)(dataStart) |= (int)(dataPtr - data);

				// Restore current temp stacks
				tempPtr = tempStart;
				tempDataPtr = tempDataStart;

				// Add relative pointer to this object to parent's data
				*(uint*)tempDataPtr = (uint)(dataStart - data);
				tempDataPtr += OFFSET_SIZE;

				// Return the offset to the pointer
				return (ushort)(tempDataPtr - parentStart - OFFSET_SIZE); 
			}

			ushort ParseDataString(byte* parentDataStart) 
			{
				char* src = json;
				char* srcEnd = end;

				// Save old data pointer (in case string is not unique and we don't save it)
				byte* saveDataPtr = tempDataPtr;

				// Align to even address
				if ((int)((uint)tempDataPtr & 0x1) != 0) {
					if (tempDataPtr + 1 > tempDataEnd)
						ExpandTempData ();
					*tempDataPtr++ = 0;
				}

				// Get the initial string and set the length to 0
				byte* strPtr = tempDataPtr;
				*(short*)tempDataPtr = 0;
				tempDataPtr += 2;

				// ditch opening quote
				src++;

				byte d = 0;
				bool parsing = true;
				while (parsing) {

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
							var hex = new char[4];
							for (int i=0; i< 4; i++) {
								hex[i] = *json++;
							}
							d = (byte)Convert.ToInt32(new string(hex), 16);
							break;
						}
						break;
						default:
						d = (byte)c;
						break;
					}

					if (tempDataPtr + 1 >= tempDataEnd)
						ExpandTempData ();

					*tempDataPtr++ = d;
				}

				// Advance parse position
				json = src;

				// Set final string length
				*(short*)strPtr = (short)(tempDataPtr - strPtr - 2);

				return (ushort)(strPtr - parentDataStart);
			}

			int ParseKeyString() 
			{
				char* src = json;
				char* srcEnd = end;

				// Save old data pointer (in case string is not unique and we don't save it)
				byte* saveDataPtr = dataPtr;

				// Align to even address
				if ((int)((uint)dataPtr & 0x1) != 0) {
					if (dataPtr + 1 > dataEnd)
						ExpandData ();
					*dataPtr++ = 0;
				}

				// Get the initial string and set the length to 0
				byte* strPtr = dataPtr;
				*(short*)dataPtr = 0;
				dataPtr += 2;

				// ditch opening quote
				src++;

				// Make string hash (FNV-1a hash offset_basis)
				uint hash = 2166136261;

				byte d = 0;
				bool parsing = true;
				while (parsing) {

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
							var hex = new char[4];
							for (int i=0; i< 4; i++) {
								hex[i] = *json++;
							}
							d = (byte)Convert.ToInt32(new string(hex), 16);
							break;
						}
						break;
						default:
						d = (byte)c;
						break;
					}

					if (dataPtr + 1 >= dataEnd)
						ExpandData ();

					*dataPtr++ = d;

					// FNV-1a hash
					hash = (hash ^ d) * 16777619;
				}

				// Advance parse position
				json = src;

				// Set final string length
				*(short*)strPtr = (short)(tempDataPtr - strPtr - 2);

				// Check if it's already in the stringtable..
				int stridx = stringTable.GetStringIndex (strPtr, hash);
				if (stridx == -1) {
					if (tempStringPtr + 4 > tempStringEnd)
						ExpandTempString ();
					stridx = *(int*)tempString;
					*(int*)tempString = stridx + 1;
					*(uint*)tempStringPtr = (uint)(dataPtr - data);
					tempStringPtr += 4;
					stringTable.AddString (strPtr, hash, stridx);
				} else {
					dataPtr = saveDataPtr;
				}

				return stridx;
			}

			void ParseNumber(DataElem* dataElem, byte* parentDataStart) {

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
					ch = *json++;
					if (ch < '0' || ch > '9')
						break;
					v = v * 10.0 + (double)(ch - '0');
				}

				if (json != end) {
					if (ch == '.') {
						isInt = false;
						json++;
						double dec = 0.1;
						while (json != end) {
							ch = *json++;
							if (ch < '0' || ch > '9')
								break;
							v = v + dec * (double)(ch - '0');
							dec = dec * 0.1;
						}
					}
				}

				if (json != end) {
					if (ch == 'e' || ch == 'E') {
						// TODO: Support exponential notation.
						throw new NotSupportedException();
					}
				}

				if (isNeg)
					v = -v;


				if (isInt) {
					int i = (int)v;
					if (i >= short.MinValue && i <= short.MaxValue) {
						dataElem->id |= (ushort)DATA_TYPE.SMALLINT << 13;
						dataElem->offset = (ushort)(short)i;
						return;
					} else {
						dataElem->id |= (ushort)DATA_TYPE.INT << 13;
						if (tempDataPtr + 8 < tempDataEnd)
							ExpandTempData ();
						while (((uint)tempDataPtr & 0x3) != 0) {
							*tempDataPtr++ = 0;
						}
						*(int*)tempDataPtr = i;
						dataElem->offset = (ushort)(tempDataPtr - parentDataStart);
						tempDataPtr += 4;
					}
				} else {
					dataElem->id |= (ushort)DATA_TYPE.DOUBLE << 13;
					if (tempDataPtr + 16 < tempDataEnd)
						ExpandTempData ();
					while (((uint)tempDataPtr & 0x7) != 0) {
						*tempDataPtr++ = 0;
					}
					*(double*)tempDataPtr = v;
					dataElem->offset = (ushort)(tempDataPtr - parentDataStart);
					tempDataPtr += 8;
				}
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

				if (json == end) {
					return TOKEN.NONE;
				}

				ch = *json;
				switch (ch) {
					case '{':
						return TOKEN.CURLY_OPEN;
					case '}':
						json++;
						return TOKEN.CURLY_CLOSE;
					case '[':
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
						if (end - json < 3) {
							json = end;
							return TOKEN.NONE;
						}
						if (json[1] != 'r' || json[2] != 'u' || json[3] != 'e') {
							return TOKEN.NONE;
						}
						json += 4;
						return TOKEN.TRUE;
					case 'f':
						if (end - json < 4) {
							json = end;
							return TOKEN.NONE;
						}
						if (json[1] != 'a' || json[2] != 'l' || json[3] != 's' || json[3] != 'e')	
							return TOKEN.NONE;
						json += 5;
						return TOKEN.FALSE;
					case 'n':
						if (end - json < 3) {
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
			public byte* StrPtr;
			public uint StrHash;
			public Key(byte* strptr, uint hash) {
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

		private static bool StringEquals(byte* a, byte* b)
		{
			short lenA = *(short*)a;
			short lenB = *(short*)b;
			if (lenA != lenB)
				return false;
			for (var i = 0; i < lenA; i++) {
				if (a [i] != b [i])
					return false;
			}
			return true;
		}

		public int GetStringIndex(byte* strptr, uint hash) {
			int hashCode = (int)hash | HASH_FLAG;
			int cur = table [(hashCode & int.MaxValue) % table.Length] - 1;

			while (cur != NO_SLOT) {
				if (linkSlots [cur].HashCode == hashCode && StringEquals(keySlots [cur].StrPtr, strptr)) {
					return valueSlots [cur];
				}
				cur = linkSlots [cur].Next;
			}
			return -1;
		}

		public void AddString (byte* strptr, uint hash, int stridx)
		{
			// get first item of linked list corresponding to given key
			int hashCode = (int)hash | HASH_FLAG;
			int index = (hashCode & int.MaxValue) % table.Length;
			int cur = table [index] - 1;

			while (cur != NO_SLOT) {
				if (linkSlots [cur].HashCode == hashCode && StringEquals(keySlots [cur].StrPtr, strptr))
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
