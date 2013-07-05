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

using System;
using System.Collections.Generic;
using System.Collections;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using System.Diagnostics;
using PlayScript;

namespace _root
{
	// this class is used to display a custom view of the vector values to the debugger
	// TODO: we need to make these elements editable 
	internal class ArrayDebugView
	{
		private Array  mArray;
		
		// The constructor for the type proxy class must have a 
		// constructor that takes the target type as a parameter.
		public ArrayDebugView(Array array)
		{
			this.mArray = array;
		}
		
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public object[] Values
		{
			get
			{
				mArray._GetInnerVector()._TrimCapacity();
				return mArray._GetInnerVector()._GetInnerArray();
			}
		}
	}

	//[DynamicClass]
	[DebuggerDisplay("length = {length}")]
	[DebuggerTypeProxy(typeof(ArrayDebugView))]
	public sealed class Array : IDynamicClass, IList
	{
		#region IList implementation
		
		int IList.Add(object value)
		{
			throw new NotImplementedException();
		}
		
		void IList.Clear()
		{
			throw new NotImplementedException();
		}
		
		bool IList.Contains(object value)
		{
			throw new NotImplementedException();
		}
		
		int IList.IndexOf(object value)
		{
			throw new NotImplementedException();
		}
		
		void IList.Insert(int index, object value)
		{
			throw new NotImplementedException();
		}
		
		void IList.Remove(object value)
		{
			throw new NotImplementedException();
		}
		
		void IList.RemoveAt(int index)
		{
			throw new NotImplementedException();
		}
		
		bool IList.IsFixedSize {
			get {
				return mList.@fixed;
			}
		}
		
		bool IList.IsReadOnly {
			get {
				return false;
			}
		}
		
		object IList.this[int index] {
			get {
				return mList[index];
			}
			set {
				mList[index] = value;
			}
		}
		
		#endregion
		
		#region ICollection implementation
		
		void ICollection.CopyTo(System.Array array, int index)
		{
			((ICollection)mList).CopyTo(array,index);
		}
		
		int ICollection.Count {
			get {
				return (int)length;
			}
		}
		
		bool ICollection.IsSynchronized {
			get {
				return false;
			}
		}
		
		object ICollection.SyncRoot {
			get {
				return null;
			}
		}
		
		#endregion
		
		#region IEnumerable implementation
		
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)mList).GetEnumerator();
		}
		
		#endregion
		
		//
		// Constants
		//
		
		public const uint CASEINSENSITIVE = 1;
		public const uint DESCENDING = 2;
		public const uint NUMERIC = 16;
		public const uint RETURNINDEXEDARRAY = 8;
		public const uint UNIQUESORT = 4;
		
		private Vector<dynamic> mList = new Vector<dynamic>();
		
		public Array() {
		}

		public Array(int size)
		{
			mList.expand((uint)size);
		}

		public Array(uint size)
		{
			mList.expand(size);
		}

		public Array(double size)
		{
			mList.expand((uint)size);
		}

		public Array(object arg1, params object[] args)
		{
			if (args.Length == 0 && (arg1 is int || arg1 is uint || arg1 is double)) {
				mList.expand(Convert.ToUInt32(arg1));
			} else {
				mList.push(arg1);
				for ( var i=0; i < args.Length; i++) {
					mList.push (args[i]);
				}
			}
		}
		
		public Array (IEnumerable list)
		{
			mList.append(list);
		}
		
		public uint length
		{ 
			get { return mList.length; } 
			set { 
				mList.length = value;
			} 
		}
		
		public dynamic this[int i]
		{
			get {
				return (dynamic)mList[i];
			}
			set {
				mList[i] = value;
			}
		}
		
		public dynamic this[uint i]
		{
			get {
				return (dynamic)mList[i];
			}
			set {
				mList[i] = value;
			}
		}
		
		public dynamic this[string i]
		{
			get {
				return mList[i];
			}
			set {
				mList[i] = value;
			}
		}
		
		public object[] ToArray()
		{
			return mList.ToArray();
		}
		
		
		public uint push(object value) {
			return mList.push(value);
		}
		
		public dynamic pop() {
			return mList.pop();
		}
		
		public uint push(object value, params object[] args) {
			mList.push(value);
			foreach(var e in args) {
				mList.push(e);
			}
			return mList.length;
		}

		public static Array AsArray(Vector<dynamic> v)
		{
			var a = new Array();
			if (v != null) {
				a.mList = v;
			}
			return a;
		}
		
		public Array reverse() {
			return AsArray(mList.reverse());
		}
		
		public dynamic shift() {
			return mList.shift();
		}
		
		public uint unshift(object o) {
			return mList.unshift(o);
		}

		public Array slice(int startIndex = 0, int endIndex = 16777215) {
			return AsArray(mList.slice(startIndex, endIndex));
		}
		
		public Array splice(int startIndex, uint deleteCount = 4294967295, params object[] items) {
			if (items.Length > 0) {
				return AsArray(mList.splice(startIndex, deleteCount, items));
			} else {
				return AsArray(mList.splice(startIndex, deleteCount));
			}
		}
		
		
		public Array map(Delegate callback, dynamic thisObject = null) 
		{
			throw new System.NotImplementedException();
		}
		
		
		public Array sort(dynamic sortBehavior = null) 
		{
			mList.sort(sortBehavior);
			return this;
		}
		
		// Sorts the elements in an array according to one or more fields in the array.
		public Array sortOn(object fieldName, object options = null) {
			throw new NotImplementedException();
		}
		
		public Array filter(Delegate callback, dynamic thisObject = null) {
			throw new System.NotImplementedException();
		}
		
		public void append(IEnumerable items)
		{
			mList.append(items);
		}
		
		public Array concat(params object[] args) 
		{
			Array v = new Array();
			// add this vector
			v.append (this);
			
			// concat all supplied vectors
			foreach (var o in args) {
				if (o is IEnumerable) {
					v.append (o as IEnumerable);
				} else {
					throw new System.NotImplementedException();
				}
			}
			return v;
		}
		
		public int indexOf(dynamic searchElement)
		{
			return mList.indexOf(searchElement);
		}
		
		public override string ToString()
		{
			return this.join(",");
		}
		
		public void Add(object value)
		{
			mList.push(value);
		}
		
		public string join(string sep = ",") {
			return mList.join(sep);
		}

		public Vector<dynamic> _GetInnerVector()
		{
			return mList;
		}
		
		
		#region IDynamicClass Implementation
		
		dynamic IDynamicClass.__GetDynamicValue(string name) 
		{
			throw new NotImplementedException ();
		}
		
		void IDynamicClass.__SetDynamicValue(string name, object value)
		{
			throw new NotImplementedException ();
		}
		
		bool IDynamicClass.__HasDynamicValue(string name)
		{
			throw new NotImplementedException ();
		}
		
		Array IDynamicClass.__GetDynamicNames()
		{
			throw new NotImplementedException ();
		}
		
		#endregion
		
	}
}

