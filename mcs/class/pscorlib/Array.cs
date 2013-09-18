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

	// for now we implement array as a vector of dynamics
	// there may be some subtle differences between array and vector that we need to handle here
	[DynamicClass]
	[DebuggerDisplay("length = {length}")]
	[DebuggerTypeProxy(typeof(ArrayDebugView))]
	public sealed class Array : Object, IDynamicClass, IList, IKeyEnumerable
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

		public struct ArrayKeyEnumeratorStruct : IEnumerator
		{
			private Vector<dynamic>.VectorKeyEnumeratorStruct mVectorKeyEnumerator;
			private IEnumerator mDynamicEnum;
			private bool enumerateDynamics;

			public ArrayKeyEnumeratorStruct(Vector<dynamic>.VectorKeyEnumeratorStruct venum, PlayScript.IDynamicClass dynprops)
			{
				mVectorKeyEnumerator = venum;
				mDynamicEnum = (dynprops==null) ? null : dynprops.__GetDynamicNames().GetEnumerator();
				enumerateDynamics = false;
			}
			public bool MoveNext ()
			{
				bool ok = false;
				if (!enumerateDynamics) 
				{
					ok = mVectorKeyEnumerator.MoveNext ();
					if(!ok && mDynamicEnum!=null)
						enumerateDynamics = true;
				}
				if (enumerateDynamics)
					ok = mDynamicEnum.MoveNext ();
				return ok;
			}

			public void Reset ()
			{
				mVectorKeyEnumerator.Reset();
				enumerateDynamics = false;
				if(mDynamicEnum!=null)
					mDynamicEnum.Reset ();
			}

			public dynamic Current {
				get {
					return (enumerateDynamics)?  mDynamicEnum.Current : mVectorKeyEnumerator.Current;
				}
			}
		}

		// this is the public struct enumerator, it does not implement IDisposable and doesnt allocate space on the heap
		public struct ArrayEnumeratorStruct
		{
			private Vector<dynamic>.VectorEnumeratorStruct mVectorEnumerator;
			private IEnumerator mDynamicEnum;
			private PlayScript.IDynamicClass mDynprops;
			private bool enumerateDynamics;

			public ArrayEnumeratorStruct(Vector<dynamic>.VectorEnumeratorStruct venum, PlayScript.IDynamicClass dynprops)
			{
				mVectorEnumerator = venum;
				mDynprops    = dynprops;
				mDynamicEnum = (dynprops==null)? null : dynprops.__GetDynamicNames().GetEnumerator();
				enumerateDynamics = false;
			}

			public bool MoveNext ()
			{
				bool ok = false;
				if (!enumerateDynamics) 
				{
					ok = mVectorEnumerator.MoveNext ();
					if(!ok && mDynamicEnum!=null)
						enumerateDynamics = true;
				}
				if (enumerateDynamics)
					ok = mDynamicEnum.MoveNext ();
				return ok;
			}

			public void Reset ()
			{
				mVectorEnumerator.Reset();
				enumerateDynamics = false;
				if(mDynamicEnum!=null)
					mDynamicEnum.Reset ();
			}

			public dynamic Current {
				get {
					return (enumerateDynamics)? mDynprops.__GetDynamicValue(mDynamicEnum.Current as string) : mVectorEnumerator.Current;
				}
			}
		}

		// public get enumerator that returns a faster struct
		public ArrayEnumeratorStruct GetEnumerator()
		{
			return new ArrayEnumeratorStruct(mList.GetEnumerator(), __dynamicProps);
		}

		// public get key enumerator that returns a faster struct
		public ArrayKeyEnumeratorStruct GetKeyEnumerator()
		{
			return new ArrayKeyEnumeratorStruct(mList.GetKeyEnumerator(),__dynamicProps);
		}

		
		#region IEnumerable implementation
		
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)mList).GetEnumerator();
		}
		
		#endregion

		#region IKeyEnumerable implementation
		
		IEnumerator IKeyEnumerable.GetKeyEnumerator()
		{
			return ((IKeyEnumerable)mList).GetKeyEnumerator();
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

		private PlayScript.IDynamicClass __dynamicProps = null;		// By default it is not created as it is not commonly used (nor a good practice).
																	// We create it only if there is a dynamic set.

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
				return mList[i];
			}
			set {
				mList[i] = value;
			}
		}
		
		public dynamic this[uint i]
		{
			get {
				return mList[i];
			}
			set {
				mList[i] = value;
			}
		}
		
		public dynamic this[string name]
		{
			get {
				int index;
				if (int.TryParse(name, out index))
				{
					// If we can convert the string to an index, then it is an indexed access
					return mList[index];
				}
				// Otherwise this is a dynamic property. However we can't use mList[name] as we would lose the undefined information,
				// it would be replaced bny default(T), so in this case null.
				if (__dynamicProps != null)
				{
					return __dynamicProps.__GetDynamicValue(name);
				}
				return PlayScript.Undefined._undefined;
			}
			set {
				int index;
				if (int.TryParse(name, out index))
				{
					// If we can convert the string to an index, then it is an indexed access
					mList[index] = value;
					return;
				}
				if (__dynamicProps == null) {
					__dynamicProps = new PlayScript.DynamicProperties(this);
				}
				__dynamicProps.__SetDynamicValue(name, value);
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
			if (mList.length == 0) {
				return null;
			}
			return mList.shift();
		}
		
		public uint unshift(object o) {
			return mList.unshift(o);
		}

		public Array slice(int startIndex = 0, int endIndex = 16777215) {
			return AsArray(mList.slice(startIndex, endIndex));
		}
		
		public Array splice(int startIndex = 0, uint deleteCount = 4294967295, params object[] items) {
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

		private class OptionsSorterOn : System.Collections.Generic.IComparer<object>
		{
			private string mFieldName;
			//private uint mOptions;
			private bool mDescending;

			public OptionsSorterOn(string fieldName, uint options)
			{
				mFieldName = fieldName;
				//mOptions = options;
				mDescending = ((options & DESCENDING) != 0);

				if ((options & (RETURNINDEXEDARRAY|UNIQUESORT)) != 0)
				{
					throw new NotImplementedException();
				}
			}

			public int Compare(object x, object y)
			{
				if (x == null)
				{
					if (y == null)
					{
						return 0;
					}
					return -1;
				}
				else if (y == null)
				{
					return 1;
				}

				// Do the field look up before doing the normal comparison
				IDynamicClass left = x as IDynamicClass;
				if (left != null)
				{
					x = left.__GetDynamicValue(mFieldName);
				}
				else
				{
					var field = x.GetType().GetField(mFieldName);	// This could be cached
					x = field.GetValue(x);
				}

				IDynamicClass right = x as IDynamicClass;
				if (right != null)
				{
					y = right.__GetDynamicValue(mFieldName);
				}
				else
				{
					var field = y.GetType().GetField(mFieldName);	// This could be cached
					y = field.GetValue(y);
				}

				//$$TODO examine options
				var xc = x as System.IComparable;
				int result;
				if (xc != null)
				{
					result = xc.CompareTo(y);
				}
				else
				{
					throw new System.NotImplementedException();
				}
				return mDescending ? -result : result;
			}
		};

		private class DefaultSorterOn : System.Collections.Generic.IComparer<object>
		{
			private string mFieldName;
			public DefaultSorterOn(string fieldName)
			{
				mFieldName = fieldName;
			}

			public int Compare(object x, object y)
			{
				if (x == null)
				{
					if (y == null)
					{
						return 0;
					}
					return -1;
				}
				else if (y == null)
				{
					return 1;
				}

				// Do the field look up before doing the normal comparison
				IDynamicClass left = x as IDynamicClass;
				if (left != null)
				{
					x = left.__GetDynamicValue(mFieldName);
				}
				else
				{
					var field = x.GetType().GetField(mFieldName);	// This could be cached
					x = field.GetValue(x);
				}

				IDynamicClass right = x as IDynamicClass;
				if (right != null)
				{
					y = right.__GetDynamicValue(mFieldName);
				}
				else
				{
					var field = y.GetType().GetField(mFieldName);	// This could be cached
					y = field.GetValue(y);
				}

				// Second check for null, this time for the field value
				if (x == null)
				{
					if (y == null)
					{
						return 0;
					}
					return -1;
				}
				else if (y == null)
				{
					return 1;
				}

				// From doc:
				//	http://help.adobe.com/en_US/FlashPlatform/reference/actionscript/3/Vector.html#sort%28%29
				// All elements, regardless of data type, are sorted as if they were strings, so 100 precedes 99, because "1" is a lower string value than "9".
				// That's going to be slow...

				return x.ToString().CompareTo(y.ToString());
			}
		}

		// Sorts the elements in an array according to one or more fields in the array.
		public Array sortOn(object fieldName, object options = null) {
			if (length == 0) {
				return this;
			}

			// Reference doc:
			// http://help.adobe.com/en_US/FlashPlatform/reference/actionscript/3/Array.html#sortOn%28%29

			IComparer<object> comparer;
			if (options is uint)
			{
				comparer = new OptionsSorterOn(fieldName.ToString(), (uint)options);
			}
			else 
			{
				//	http://help.adobe.com/en_US/FlashPlatform/reference/actionscript/3/Vector.html#sort%28%29
				comparer = new DefaultSorterOn(fieldName.ToString());
			}
			System.Array.Sort (mList._GetInnerArray(), 0, (int)mList.length, comparer);
			return this;
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
		
		public int indexOf(object searchElement)
		{
			return mList.indexOf(searchElement);
		}
		
		public override string toString()
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
		
		
		#region IDynamicClass implementation

		// this method can be used to override the dynamic property implementation of this dynamic class
		void __SetDynamicProperties(PlayScript.IDynamicClass props) {
			__dynamicProps = props;
		}

		dynamic PlayScript.IDynamicClass.__GetDynamicValue (string name) {
			object value = null;
			if (__dynamicProps != null) {
				value = __dynamicProps.__GetDynamicValue(name);
			}
			return value;
		}

		bool PlayScript.IDynamicClass.__TryGetDynamicValue (string name, out object value) {
			if (__dynamicProps != null) {
				return __dynamicProps.__TryGetDynamicValue(name, out value);
			} else {
				value = null;
				return false;
			}
		}

		void PlayScript.IDynamicClass.__SetDynamicValue (string name, object value) {
			if (__dynamicProps == null) {
				__dynamicProps = new PlayScript.DynamicProperties(this);
			}
			__dynamicProps.__SetDynamicValue(name, value);
		}

		bool PlayScript.IDynamicClass.__DeleteDynamicValue (object name) {
			if (__dynamicProps != null) {
				return __dynamicProps.__DeleteDynamicValue(name);
			}
			return false;
		}

		bool PlayScript.IDynamicClass.__HasDynamicValue (string name) {
			if (__dynamicProps != null) {
				return __dynamicProps.__HasDynamicValue(name);
			}
			return false;
		}

		IEnumerable PlayScript.IDynamicClass.__GetDynamicNames () {
			if (__dynamicProps != null) {
				return __dynamicProps.__GetDynamicNames();
			}
			return null;
		}

		#endregion

	}
}

