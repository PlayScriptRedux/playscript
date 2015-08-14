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
using System.Runtime.CompilerServices;
using PlayScript;
using PlayScript.DynamicRuntime;

namespace _root
{

	// Interface implemented by immutable array backing store providers (like BinJsonArray)
	public interface IImmutableArray : IEnumerable
	{
		uint length { get; }
		string getStringAt (uint index);
		int getIntAt (uint index);
		uint getUIntAt (uint index);
		double getDoubleAt (uint index);
		bool getBoolAt (uint index);
		object getObjectAt (uint index);
	}

#if PERFORMANCE_MODE

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
				if (mArray._GetInnerArray () != null)
					return mArray._GetInnerArray ();
				else
					return mArray.ToArray ();
			}
		}
	}

	[DynamicClass]
	[DebuggerDisplay("length = {length}")]
	[DebuggerTypeProxy(typeof(ArrayDebugView))]
	public sealed class Array : _root.Object, IList, PlayScript.IDynamicClass, PlayScript.IKeyEnumerable
	{
		#region IList implementation

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		int IList.Add(object value)
		{
			return (int) push (value);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void IList.Clear()
		{
			this.length = 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool IList.Contains(object value)
		{
			return this.indexOf(value) >= 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		int IList.IndexOf(object value)
		{
			return this.indexOf(value);
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
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				return false;
			}
		}

		bool IList.IsReadOnly {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				return false;
			}
		}

		object IList.this[int index] {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				return (object)this[index];
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set {
				this[index] = value;
			}
		}

		#endregion

		#region ICollection implementation

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void ICollection.CopyTo(System.Array array, int index)
		{
			if (mImmutableArray != null)
				throw new InvalidOperationException ();
			System.Array.Copy(mArray, 0, array, index, mCount);
		}

		bool ICollection.IsSynchronized {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				return false;
			}
		}

		int ICollection.Count {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				return (int)this.length;
			}
		}

		object ICollection.SyncRoot {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				return null;
			}
		}

		#endregion

		private const string ERROR_RESIZING_FIXED = "Error resizing fixed vector.";

		public const uint CASEINSENSITIVE = 1;
		public const uint DESCENDING = 2;
		public const uint NUMERIC = 16;
		public const uint RETURNINDEXEDARRAY = 8;
		public const uint UNIQUESORT = 4;

		private object[] mArray;
		private uint mCount;
		private IImmutableArray mImmutableArray;
		private PlayScript.IDynamicClass __dynamicProps = null;		// By default it is not created as it is not commonly used (nor a good practice).
																	// We create it only if there is a dynamic set.

		private static object[] sEmptyArray = new object[0];

		//
		// Properties
		//

		public uint length
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get { return (mImmutableArray != null) ? mImmutableArray.length : mCount; } 
			set { 
				if (mImmutableArray != null)
					throw new InvalidOperationException ();
				if (value == 0) {
					System.Array.Clear (mArray, 0, (int)mCount);
				} else if (mCount < value) {
					EnsureCapacity(value);
				} else if (mCount > value) {
					System.Array.Clear (mArray, (int)value, (int)(mCount - value));
				}
				mCount = value;
			} 
		}

		//
		// Methods
		//

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Array()
		{
			mArray = sEmptyArray;
			mCount = 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Array(Array a)
		{
			if (a.mImmutableArray != null) {
				mImmutableArray = a.mImmutableArray;
			} else {
				mArray = new object[a.length];
				this.append((IEnumerable)a);
			}
		}

		public Array(IEnumerable e)
		{
			if (e is string) {
				mArray = sEmptyArray;
				push ((string)e);
			} else {
				mArray = sEmptyArray;
				this.append (e);
			}
		}

		public Array(uint length)
		{
			if (length != 0)
				mArray = new object[(int)length];
			else
				mArray = sEmptyArray;
			mCount = length;
		}

		public Array(int length)
		{
			if (length != 0)
				mArray = new object[(int)length];
			else
				mArray = sEmptyArray;
			mCount = (uint)length;
		}

		public Array(double length)
		{
			if (length != 0)
				mArray = new object[(int)length];
			else
				mArray = sEmptyArray;
			mCount = (uint)length;
		}

		public Array(object arg1, params object[] args)
		{
			mArray = sEmptyArray;
			if (args.Length == 0 && (arg1 is int || arg1 is uint || arg1 is double)) {
				this.expand(Convert.ToUInt32(arg1));
			} else {
				this.push(arg1);
				for ( var i=0; i < args.Length; i++) {
					this.push (args[i]);
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Array(IList a)
		{
			mArray = new object[a.Count];
			this.append((IEnumerable)a);
		}

		public Array(IImmutableArray staticArray)
		{
			mImmutableArray = staticArray;
		}

		private void ConvertToMutable () {
			if (mImmutableArray == null)
				throw new InvalidOperationException ();
			uint len = mImmutableArray.length;
			mArray = new object[len];
			for (uint i = 0; i < len; i++) {
				mArray [i] = mImmutableArray.getObjectAt (i);
			}
			this.mCount = len;
			mImmutableArray = null;
		}

		public dynamic this[int i]
		{
			[return: AsUntyped]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				if (mImmutableArray != null)
					return mImmutableArray.getObjectAt ((uint)i);
				#if PERFORMANCE_MODE && DEBUG
				if ((i >= mCount) || (i < 0))
				{
					throw new IndexOutOfRangeException();
				}
				#elif PERFORMANCE_MODE
				#else
				if ((i >= mCount) || (i < 0))
				{
					Console.WriteLine ("undef : " + Undefined._undefined);
					return Undefined._undefined;
				}
				#endif
				return i >= mCount ? null : mArray [i]; 
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set {
				if (mImmutableArray != null)
					ConvertToMutable ();
				#if PERFORMANCE_MODE && DEBUG
				if (i >= mCount) {
//					return Undefined._undefined;
					// TODO : Fix this (?)
					throw new IndexOutOfRangeException();
				}
				#elif PERFORMANCE_MODE
				#else
				if (i >= mCount) {
					expand((uint)(i+1));
				}
				#endif
				mArray[i] = (object)value;
			}
		}

		public dynamic this[uint i]
		{
			[return: AsUntyped]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				if (mImmutableArray != null)
					return mImmutableArray.getObjectAt ((uint)i);
				#if PERFORMANCE_MODE && DEBUG
				if (i >= mCount)
				{
					return Undefined._undefined;
					// TODO : Fix this (?)
					//throw new IndexOutOfRangeException();
				}
				#elif PERFORMANCE_MODE
				#else
				if (i >= mCount)
				{
					return PlayScript.Undefined._undefined;
				}
				#endif
				return mArray[(int)i];
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set {
				if (mImmutableArray != null)
					ConvertToMutable ();
				#if PERFORMANCE_MODE && DEBUG
				if (i >= mCount) {
//					// TODO : Fix this (?)
					throw new IndexOutOfRangeException();
				}
				#elif PERFORMANCE_MODE
				#else
				if (i >= mCount) {
					expand((uint)(i+1));
				}
				#endif
				mArray[(int)i] = (object)value;
			}
		}

		public dynamic this[long l]
		{
			[return: AsUntyped]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				return this [(int)l];

			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set {
				this [(int)l] = value;
			}
		}

		private bool TryParseIndex(string input, out int index)
		{
			double d;
			if (double.TryParse (input, out d) && System.Math.Truncate (d) == d) {
				index = (int)d;
				return true;
			}
			index = -1;
			return false;
		}

		public dynamic this[string name]
		{
			[return: AsUntyped]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				// If we can convert the string to an index, then it is an indexed access.
				int index;
				if (TryParseIndex (name, out index)) {
					return mArray [index];
				}
				if (mImmutableArray != null)
					return mImmutableArray.getObjectAt ((uint)index);
				// Otherwise this is a dynamic property.
				if (__dynamicProps == null) {
					return PlayScript.Undefined._undefined;
				}
				return __dynamicProps.__GetDynamicValue(name);	// The instance that was set was only of dynamic type (or undefined)
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set {
				if (mImmutableArray != null)
					ConvertToMutable ();
				// If we can convert the string to an index, then it is an indexed access.
				int index;
				if (TryParseIndex (name, out index)) {
					mArray[index] = value;
					return;
				}
				// Otherwise this is a dynamic property.
				if (__dynamicProps == null) {
					__dynamicProps = new PlayScript.DynamicProperties();	// Create the dynamic propertties only on the first set usage
				}
				__dynamicProps.__SetDynamicValue(name, (object)value);		// This will only inject dynamic type instances.
			}
		}

		//
		// Treat floating point as a string. It will be considered an indexed access if
		// the value is an integer, otherwise it will be a dynamic property access.
		//
		public dynamic this[double d]
		{
			[return: AsUntyped]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				return this [d.ToString ()];
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set {
				this [d.ToString ()] = value;
			}
		}

		//
		// Treat floating point as a string. It will be considered an indexed access if
		// the value is an integer, otherwise it will be a dynamic property access.
		//
		public dynamic this[float f]
		{
			[return: AsUntyped]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				return this [f.ToString ()];
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set {
				this [f.ToString ()] = value;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public object[] ToArray()
		{
			object[] ret;
			if (mImmutableArray != null) {
				int len = (int)mImmutableArray.length;
				ret = new object[len];
				for (var i = 0; i < len; i++) {
					ret [i] = mImmutableArray.getObjectAt ((uint)i);
				}
			} else {
				ret = new object[mCount];
				System.Array.Copy(mArray, ret, mCount);
			}
			return ret;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public object[] _GetInnerArray()
		{
			if (mImmutableArray != null)
				ConvertToMutable ();
			return mArray;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void _TrimCapacity()
		{
			if (mImmutableArray != null)
				return;
			if (mCount < mArray.Length) {
				mArray = ToArray();
			}
		}

		private void EnsureCapacity(uint size)
		{
			if (mArray.Length < size) {
				int newSize = mArray.Length * 2;
				if (newSize == 0) newSize = 4;
				while (newSize < size)
					newSize = newSize * 2;
				object[] newArray = new object[newSize];
				System.Array.Copy(mArray, newArray, mArray.Length);
				mArray = newArray;
			}
		}

		private void _Insert(int index, object value) 
		{
			if (index > mCount) throw new NotImplementedException();

			if (mCount >= mArray.Length)
				EnsureCapacity(mCount + 1);
			if (index < (int)mCount) {
				//	System.Array.Copy (mArray, index, mArray, index + 1, (int)mCount - index);
				for (int i=(int)mCount; i > index; i--)
				{
					mArray[i] = mArray[i-1];
				}
			}
			mArray[index] = value;
			mCount++;
		}

		private void _RemoveAt(int index)
		{
			if (index < 0 || index >= (int)mCount)
				throw new IndexOutOfRangeException();

			if (index == (int)mCount - 1) {
				mArray[index] = null;
			} else {
				System.Array.Copy (mArray, index + 1, mArray, index, (int)mCount - index - 1);
				mArray[mCount - 1] = null;
			}
			mCount--;
		}

		// optionally expands the vector to accomodate the new size
		// if the vector is big enough then nothing is done
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void expand(uint newSize) 
		{
			if (mImmutableArray != null)
				ConvertToMutable ();
			EnsureCapacity(newSize);
			if (mCount < newSize)
				mCount = newSize;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void append(Array vec)
		{
			if (mImmutableArray != null)
				ConvertToMutable ();

			EnsureCapacity(mCount + vec.mCount);
			System.Array.Copy (vec.mArray, 0, mArray, mCount, vec.mCount);
			mCount += vec.mCount;
		}

		public void append(IEnumerable items)
		{
			if (mImmutableArray != null)
				ConvertToMutable ();

			if (items == null) {
				return;
			}
			if (items is IList) {
				var list = (items as IList);
				EnsureCapacity(mCount + (uint)list.Count);
			}

			foreach (var item in items) {
				this.push (item);
			}
		}

		public void append(IEnumerable<object> items)
		{
			if (mImmutableArray != null)
				ConvertToMutable ();

			if (items is IList<object>) {
				var list = (items as IList<object>);
				EnsureCapacity(mCount + (uint)list.Count);
			}

			foreach (var item in items) {
				this.push (item);
			}
		}

		public Array concat(params object[] args) 
		{
			if (mImmutableArray != null)
				ConvertToMutable ();

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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void copyTo(Array dest, int sourceIndex, int destIndex, int count) {
			if (mImmutableArray != null) {
				throw new NotImplementedException ();
			} else {
				System.Array.Copy (this.mArray, sourceIndex, dest.mArray, destIndex, count);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Array clone() {
			return new Array(this);
		}

//		public Array concat(params object[] args) {
//			Array v = clone();
//			// concat all supplied vecots
//			foreach (var o in args)
//			{
//				if (o is IEnumerable<object>)
//				{
//					v.append(o as IEnumerable<object>);
//				} 
//				else
//				{
//					throw new System.NotImplementedException();
//				}
//			}
//			return v;
//		}

		public bool every(Delegate callback, object thisObject = null) 
		{
			throw new System.NotImplementedException();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Array sort(Delegate sortBehavior)
		{
			if (mImmutableArray != null)
				ConvertToMutable ();
			return sortInternal(sortBehavior);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Array sort(object sortBehavior = null) 
		{
			if (mImmutableArray != null)
				ConvertToMutable ();
			return sortInternal(sortBehavior);
		}

		private Array sortInternal(object sortBehavior)
		{
			IComparer<object> comparer;
			if (sortBehavior is System.Func<object, object,int>)
			{
				System.Func<object, object,int> func = (System.Func<object, object,int>)sortBehavior;
				// By definition, we know that the vector only contains type T,
				// so if the function passed has the exact expected signature, we use the fast path
				comparer = new TypedFunctionSorter(func);
			}
			else if (sortBehavior is Delegate)
			{
				comparer = new FunctionSorter(sortBehavior);
			}
			else if (sortBehavior is uint)
			{
				comparer = new OptionsSorter((uint)sortBehavior);
			}
			else 
			{
				//	http://help.adobe.com/en_US/FlashPlatform/reference/actionscript/3/Vector.html#sort%28%29
				comparer = new DefaultSorter();
			}
			System.Array.Sort (mArray, 0, (int)mCount, comparer);
			return this;
		}

		private class OptionsSorterOn : System.Collections.Generic.IComparer<object>
		{
			private string mFieldName;
			//private uint mOptions;
			private bool mDescending;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
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

				IDynamicClass right = y as IDynamicClass;
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
		}

		private class DefaultSorterOn : System.Collections.Generic.IComparer<object>
		{
			private string mFieldName;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
				//	http://help.adobe.com/en_US/FlashPlatform/reference/actionscript/3/Array.html#sort%28%29
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

			if (mImmutableArray != null)
				ConvertToMutable ();

			// Reference doc:
			// http://help.adobe.com/en_US/FlashPlatform/reference/actionscript/3/Array.html#sortOn%28%29

			IComparer<object> comparer;
			if (options is uint)
			{
				comparer = new OptionsSorterOn(fieldName.ToString(), (uint)options);
			}
			else 
			{
				//	http://help.adobe.com/en_US/FlashPlatform/reference/actionscript/3/Array.html#sort%28%29
				comparer = new DefaultSorterOn(fieldName.ToString());
			}
			System.Array.Sort (mArray, 0, (int)mCount, comparer);
			return this;
		}


		public Array filter(Delegate callback, object thisObject = null) 
		{
			throw new System.NotImplementedException();
		}

		public void forEach(Delegate callback, object thisObject = null) 
		{
			if (thisObject != null)
			{
				throw new NotImplementedException();
			}
			foreach (var item in this)
			{
				callback.DynamicInvoke(item);
			}
		}

		public int indexOf(object searchElement, int fromIndex = 0)
		{
			object elem = null;
			if (mImmutableArray != null) {
				for (var i = fromIndex; i < this.length; i++) {
					elem = this [i];
					if (elem == searchElement || (elem != null && PSBinaryOperation.EqualityObjObj (elem, searchElement))) {
						return i;
					}
				}
			} else {
				for (var i = fromIndex; i < mCount; i++) {
					elem = mArray [i];
					if (elem == searchElement || (elem != null && PSBinaryOperation.EqualityObjObj (elem, searchElement))) {
						return i;
					}
				}
			}
			return -1;
		}

		public string join(string sep = ",") 
		{
			var sb = new System.Text.StringBuilder();
			bool needsSeperator = false;
			if (mImmutableArray != null) {
				for (var i = 0; i < mImmutableArray.length; i++) {
					var item = mImmutableArray.getObjectAt((uint)i);
					if (needsSeperator) {
						sb.Append(sep);
					}
					if (!PlayScript.Dynamic.IsNullOrUndefined(item)) {
						sb.Append(item.ToString());
					}
					needsSeperator = true;
				}
			} else {
				for (var i = 0; i < mCount; i++) {
					var item = mArray [i];
					if (needsSeperator) {
						sb.Append(sep);
					}
					if (!PlayScript.Dynamic.IsNullOrUndefined(item)) {
						sb.Append(item.ToString());
					}
					needsSeperator = true;
				}
			}
			return sb.ToString();
		}

		public int lastIndexOf(object searchElement, int fromIndex = 0x7fffffff) 
		{
			object elem = null;
			if (fromIndex >= (int)this.length)
				fromIndex = (int)this.length - 1;
			if (mImmutableArray != null) {
				for (var i = fromIndex; i >= 0; i--) {
					elem = this [i];
					if (elem == searchElement || (elem != null && elem.Equals (searchElement))) {
						return i;
					}
				}
			} else {
				for (var i = fromIndex; i >= 0; i--) {
					elem = mArray [i];
					if (elem == searchElement || (elem != null && elem.Equals (searchElement))) {
						return i;
					}
				}
			}
			return -1;
		}

		public Array map(Delegate callback, object thisObject = null) 
		{
			throw new System.NotImplementedException();
		}

		[return: AsUntyped]
		public dynamic pop() 
		{
			if (mImmutableArray != null)
				ConvertToMutable ();

			if (mCount == 0) {
				return PlayScript.Undefined._undefined;
			}
			object val = mArray[mCount - 1];
			mCount--;
			mArray[mCount] = null;
			return val;
		}

		public uint push(object value)
		{
			if (mImmutableArray != null)
				ConvertToMutable ();

			if (mCount >= mArray.Length)
				EnsureCapacity((uint)(1.25 * (mCount + 1)));
			mArray[mCount] = value;
			mCount++;
			return mCount;
		}

		public uint push(object value, params object[] args) 
		{
			if (mImmutableArray != null)
				ConvertToMutable ();

			uint len = (uint)args.Length;
			if (mArray.Length < mCount + 1 + len)
				EnsureCapacity((uint)(1.25 * (mCount + len)));
			mArray[mCount++] = value;
			for (var i = 0; i < len; i++)
				mArray[mCount++] = args[i];
			return mCount;
		}

		public Array reverse() 
		{
			if (mImmutableArray != null)
				ConvertToMutable ();

			System.Array.Reverse(mArray, 0, (int)mCount);
			return this;
		}

		[return: AsUntyped]
		public dynamic shift() 
		{
			if (mImmutableArray != null)
				ConvertToMutable ();

			if (mCount == 0) {
				return PlayScript.Undefined._undefined;
			}
			object v = this[0];
			_RemoveAt(0);
			return v;
		}

		public Array slice(int startIndex = 0, int endIndex = 16777215) 
		{
			Array result = null;
			int count;

			if (startIndex < 0) 
				throw new InvalidOperationException("splice error");

			if (mImmutableArray != null) {
				uint immutableCount = mImmutableArray.length;

				if (endIndex < 0)
					endIndex = (int)immutableCount + endIndex;		// If negative, starts from the end

				if (endIndex > (int)immutableCount)
					endIndex = (int)immutableCount;

				count = endIndex - startIndex;
				if (count < 0)
					count = 0;

				result = new Array ((uint)count);
				for (var i = 0; i < count; i++)
					result.mArray [i] = mImmutableArray.getObjectAt ((uint)(i + startIndex));
			} else {
				if (endIndex < 0)
					endIndex = (int)mCount + endIndex;		// If negative, starts from the end

				if (endIndex > (int)mCount)
					endIndex = (int)mCount;

				count = endIndex - startIndex;
				if (count < 0)
					count = 0;

				result = new Array ((uint)count);
				System.Array.Copy (mArray, startIndex, result.mArray, 0, count);
			}

			return result;
		}

		public bool some(Delegate callback, object thisObject = null) 
		{
			throw new System.NotImplementedException();
		}

		private class TypedFunctionSorter : System.Collections.Generic.IComparer<object>
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public TypedFunctionSorter(System.Func<object, object, int> comparerDelegate)
			{
				mDelegate = comparerDelegate;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public int Compare(object x, object y)
			{
				return mDelegate.Invoke(x, y);
			}

			private System.Func<object, object,int> mDelegate;
		}


		private class FunctionSorter : System.Collections.Generic.IComparer<object>
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public FunctionSorter(object func)
			{
				mDelegate = func as Func<object,object,int>;
				mFunc = func;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public int Compare(object x, object y)
			{
				if (mDelegate != null)
					return mDelegate (x, y);
				else
					return (int)mFunc(x, y);
			}

			private Func<object,object,int> mDelegate;
			private dynamic mFunc;
		}

		private class OptionsSorter : System.Collections.Generic.IComparer<object>
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public OptionsSorter(uint options)
			{
				// mOptions = options;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public int Compare(object x, object y)
			{
				//$$TODO examine options
				var xc = x as System.IComparable<object>;
				if (xc != null)
				{
					return xc.CompareTo(y);
				}
				else
				{
					throw new System.NotImplementedException();
				}
			}

			// private uint mOptions;
		}

		private class DefaultSorter : System.Collections.Generic.IComparer<object>
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public int Compare(object x, object y)
			{
				// From doc:
				//	http://help.adobe.com/en_US/FlashPlatform/reference/actionscript/3/Array.html#sort%28%29
				// All elements, regardless of data type, are sorted as if they were strings, so 100 precedes 99, because "1" is a lower string value than "9".
				// That's going to be slow...
				return x.ToString().CompareTo(y.ToString());
			}
		}

		public Array splice(int startIndex = 0, uint deleteCount = 4294967295) 
		{
			if (mImmutableArray != null)
				ConvertToMutable ();

			Array removed = null;

			if (startIndex < 0) 
				throw new InvalidOperationException("splice error");

			// determine number of items to delete
			int toDelete = (int)mCount - startIndex;
			if ((uint)toDelete > deleteCount) 
				toDelete = (int)deleteCount;

			if (toDelete == 1) {
				removed = new Array(1);
				removed.mArray[0] = mArray[startIndex];
				int toMove = (int)mCount - 1 - startIndex;
				if (toMove > 0)
					System.Array.Copy (mArray, startIndex + toDelete, mArray, startIndex, toMove);
				mArray[mCount - 1] = null;
				mCount--;
			} else if (toDelete > 1) {
				removed = new Array((uint)toDelete);
				System.Array.Copy (mArray, startIndex, removed.mArray, 0, toDelete);
				int toMove = (int)mCount - toDelete - startIndex;
				if (toMove > 0)
					System.Array.Copy (mArray, startIndex + toDelete, mArray, startIndex, toMove);
				System.Array.Clear (mArray, startIndex + toMove, toDelete);
				mCount = (uint)(startIndex + toMove);
			}

			return removed;
		}

		public Array splice(int startIndex, uint deleteCount = 4294967295, params object[] items) 
		{
			if (mImmutableArray != null)
				ConvertToMutable ();

			Array removed = null;

			if (startIndex < 0) 
				throw new InvalidOperationException("splice error");

			// determine number of items to delete
			int toDelete = (int)mCount - startIndex;
			if ((uint)toDelete > deleteCount) 
				toDelete = (int)deleteCount;

			if (toDelete == 1) {
				removed = new Array(1);
				removed.mArray[0] = mArray[startIndex];
				int toMove = (int)mCount - 1 - startIndex;
				if (toMove > 0)
					System.Array.Copy (mArray, startIndex + toDelete, mArray, startIndex, toMove);
				mArray[mCount - 1] = null;
				mCount--;
			} else if (toDelete > 1) {
				removed = new Array((uint)toDelete);
				System.Array.Copy (mArray, startIndex, removed.mArray, 0, toDelete);
				int toMove = (int)mCount - toDelete - startIndex;
				if (toMove > 0)
					System.Array.Copy (mArray, startIndex + toDelete, mArray, startIndex, toMove);
				System.Array.Clear (mArray, startIndex + toMove, toDelete);
				mCount = (uint)(startIndex + toMove);
			}

			uint itemsLen = (uint)items.Length;
			if (itemsLen > 0) {
				EnsureCapacity(mCount + itemsLen);
				int toMove = (int)mCount - startIndex;
				if (toMove > 0)
					System.Array.Copy (mArray, startIndex, mArray, startIndex + itemsLen, toMove);
				if (itemsLen == 1)
					mArray[startIndex] = items[0];
				else
					System.Array.Copy (items, 0, mArray, startIndex, itemsLen);
				mCount += itemsLen;
			}

			return removed;
		}

		public string toLocaleString() 
		{
			throw new System.NotImplementedException();
		}

		// NOTE: This method should not be public!  However intializers depend on it and so it 
		// still has to be public for now.  The compiler should be switched to 
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Add(object o)
		{
			this.push (o);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override string toString() 
		{
			return this.join(",");
		}

		public uint unshift(object item) 
		{
			if (mImmutableArray != null)
				ConvertToMutable ();

			if (mCount >= mArray.Length)
				EnsureCapacity(mCount + 1);
			if (mCount > 0)
				System.Array.Copy (mArray, 0, mArray, 1, (int)mCount);
			mArray[0] = item;
			mCount++;
			return mCount;
		}

		public uint unshift(object item, params object[] args) 
		{
			if (mImmutableArray != null)
				ConvertToMutable ();

			uint argsLen = (uint)args.Length;
			EnsureCapacity(mCount + 1 + argsLen);
			if (mCount > 0)
				System.Array.Copy (mArray, 0, mArray, args.Length, (int)mCount);
			mArray[0] = item;
			System.Array.Copy (args, 0, mArray, 1, argsLen);
			mCount += 1 + argsLen;
			return mCount;
		}


		#region IEnumerable implementation

		private class ArrayEnumeratorClass : IEnumerator, IDisposable
		{
			private readonly IList mVector;
			private int mIndex;
			private IDynamicClass mDynamicProps;
			private IEnumerator mDynamicEnumerator;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public ArrayEnumeratorClass(IList vector, IDynamicClass dynamicProps)
			{
				mVector = vector;
				mIndex = -1;
				mDynamicProps = dynamicProps;
				mDynamicEnumerator = mDynamicProps != null ? mDynamicProps.__GetDynamicNames ().GetEnumerator () : null;
			}

			#region IEnumerator implementation

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public bool MoveNext ()
			{
				mIndex++;
				if (mIndex < mVector.Count)
					return true;
				if (mDynamicEnumerator != null)
					return mDynamicEnumerator.MoveNext ();
				return false;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Reset ()
			{
				mIndex = -1;
				if (mDynamicEnumerator != null)
					mDynamicEnumerator.Reset ();
			}

			object System.Collections.IEnumerator.Current {
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get {
					if (mIndex < mVector.Count)
						return mVector[mIndex];
					if (mDynamicProps != null)
						return mDynamicProps.__GetDynamicValue ((string)mDynamicEnumerator.Current);
					return null;
				}
			}

			#endregion

			#region IDisposable implementation

			public void Dispose ()
			{
			}

			#endregion

			#region IEnumerator implementation

			public object Current {
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get {
					if (mIndex < mVector.Count)
						return mVector[mIndex];
					if (mDynamicProps != null)
						return mDynamicProps.__GetDynamicValue ((string)mDynamicEnumerator.Current);
					return null;
				}
			}

			#endregion

		}

		// this is the public struct enumerator, it does not implement IDisposable and doesnt allocate space on the heap
		public struct ArrayEnumeratorStruct
		{
			private readonly IList mVector;
			private int mIndex;
			private IDynamicClass mDynamicProps;
			private IEnumerator mDynamicEnumerator;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public ArrayEnumeratorStruct(IList vector, IDynamicClass dynamicProps)
			{
				mVector = vector;
				mIndex = -1;
				mDynamicProps = dynamicProps;
				mDynamicEnumerator = mDynamicProps != null ? mDynamicProps.__GetDynamicNames ().GetEnumerator () : null;
			}

			#region IEnumerator implementation

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public bool MoveNext ()
			{
				mIndex++;
				if (mIndex < mVector.Count)
					return true;
				if (mDynamicEnumerator != null)
					return mDynamicEnumerator.MoveNext ();
				return false;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Reset ()
			{
				mIndex = -1;
				if (mDynamicEnumerator != null)
					mDynamicEnumerator.Reset ();
			}

			public object Current {
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get {
					if (mIndex < mVector.Count)
						return mVector[mIndex];
					if (mDynamicProps != null)
						return mDynamicProps.__GetDynamicValue ((string)mDynamicEnumerator.Current);
					return null;
				}
			}

			#endregion
		}

		// public get enumerator that returns a faster struct
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ArrayEnumeratorStruct GetEnumerator ()
		{
			return new ArrayEnumeratorStruct(this, __dynamicProps);
		}

		#endregion

		#region IEnumerable implementation

		// private IEnumerable get enumerator that returns a (slower) class on the heap
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return new ArrayEnumeratorClass(this, __dynamicProps);
		}

		#endregion


		#region IKeyEnumerable implementation

		private class ArrayKeyEnumeratorClass : IEnumerator, IDisposable
		{
			private readonly IList mVector;
			private int mIndex;
			private IEnumerator mDynamicEnumerator;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public ArrayKeyEnumeratorClass(IList vector, IDynamicClass dynamicProps)
			{
				mVector = vector;
				mIndex = -1;
				mDynamicEnumerator = dynamicProps != null ? dynamicProps.__GetDynamicNames ().GetEnumerator () : null;
			}

			#region IEnumerator implementation

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public bool MoveNext ()
			{
				mIndex++;
				if (mIndex < mVector.Count)
					return true;
				if (mDynamicEnumerator != null)
					return mDynamicEnumerator.MoveNext ();
				return false;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Reset ()
			{
				mIndex = -1;
				if (mDynamicEnumerator != null)
					mDynamicEnumerator.Reset ();
			}

			object System.Collections.IEnumerator.Current {
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get {
					if (mIndex < mVector.Count)
						return mIndex;
					if (mDynamicEnumerator != null)
						return mDynamicEnumerator.Current;
					return null;
				}
			}

			#endregion

			#region IDisposable implementation

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Dispose ()
			{
			}

			#endregion
		}

		// this is the public struct enumerator, it does not implement IDisposable and doesnt allocate space on the heap
		public struct ArrayKeyEnumeratorStruct 
		{
			private readonly IList mVector;
			private int mIndex;
			private IEnumerator mDynamicEnumerator;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public ArrayKeyEnumeratorStruct(IList vector, IDynamicClass dynamicProps)
			{
				mVector = vector;
				mIndex = -1;
				mDynamicEnumerator = dynamicProps != null ? dynamicProps.__GetDynamicNames ().GetEnumerator () : null;
			}

			#region IEnumerator implementation

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public bool MoveNext ()
			{
				mIndex++;
				if (mIndex < mVector.Count)
					return true;
				if (mDynamicEnumerator != null)
					return mDynamicEnumerator.MoveNext ();
				return false;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Reset ()
			{
				mIndex = -1;
				if (mDynamicEnumerator != null)
					mDynamicEnumerator.Reset ();
			}

			// unfortunately this has to return object because the for() loop could use a non-int as its variable, causing bad IL
			public object Current {
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get {
					if (mIndex < mVector.Count)
						return mIndex;
					if (mDynamicEnumerator != null)
						return mDynamicEnumerator.Current;
					return null;
				}
			}

			#endregion
		}

		// public get enumerator that returns a faster struct
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ArrayKeyEnumeratorStruct GetKeyEnumerator()
		{
			return new ArrayKeyEnumeratorStruct(this, __dynamicProps);
		}

		// private IKeyEnumerable get enumerator that returns a (slower) class on the heap
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		IEnumerator PlayScript.IKeyEnumerable.GetKeyEnumerator()
		{
			return new ArrayKeyEnumeratorClass(this, __dynamicProps);
		}

		#endregion

		#region IDynamicClass implementation

		// this method can be used to override the dynamic property implementation of this dynamic class
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void __SetDynamicProperties(PlayScript.IDynamicClass props) {
			__dynamicProps = props;
		}

		dynamic PlayScript.IDynamicClass.__GetDynamicValue (string name) {
			int index;
			if (TryParseIndex (name, out index)) {
				return this [index];
			}

			object value = PlayScript.Undefined._undefined;
			if (__dynamicProps != null) {
				value = __dynamicProps.__GetDynamicValue(name);
			}
			return value;
		}

		bool PlayScript.IDynamicClass.__TryGetDynamicValue (string name, out object value) {
			int index;
			if (TryParseIndex (name, out index)) {
				value = this [index];
				return true;
			}

			if (__dynamicProps != null) {
				return __dynamicProps.__TryGetDynamicValue(name, out value);
			} else {
				value = PlayScript.Undefined._undefined;
				return false;
			}
		}

		void PlayScript.IDynamicClass.__SetDynamicValue (string name, object value) {
			int index;
			if (TryParseIndex (name, out index)) {
				this [index] = value;
				return;
			}

			if (__dynamicProps == null) {
				__dynamicProps = new PlayScript.DynamicProperties(this);
			}
			__dynamicProps.__SetDynamicValue(name, value);
		}

		bool PlayScript.IDynamicClass.__DeleteDynamicValue (object name) {
			int index;
			if (name is string && TryParseIndex ((string)name, out index)) {
				this [index] = PlayScript.Undefined._undefined;
				return true;
			}

			if (__dynamicProps != null) {
				return __dynamicProps.__DeleteDynamicValue(name);
			}
			return false;
		}

		bool PlayScript.IDynamicClass.__HasDynamicValue (string name) {
			int index;
			if (TryParseIndex (name, out index)) {
				return index < mCount;
			}
			if (__dynamicProps != null) {
				return __dynamicProps.__HasDynamicValue(name);
			}
			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		IEnumerable PlayScript.IDynamicClass.__GetDynamicNames () {
			if (__dynamicProps != null) {
				return __dynamicProps.__GetDynamicNames();
			}
			return null;
		}

		#endregion

	}

#else

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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		int IList.Add(object value)
		{
			return ((IList)mList).Add (value);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void IList.Clear()
		{
			((IList)mList).Clear ();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool IList.Contains(object value)
		{
			return ((IList)mList).Contains (value);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		int IList.IndexOf(object value)
		{
			return ((IList)mList).IndexOf (value);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void IList.Insert(int index, object value)
		{
			((IList)mList).Insert (index, value);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void IList.Remove(object value)
		{
			((IList)mList).Remove (value);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void IList.RemoveAt(int index)
		{
			((IList)mList).RemoveAt (index);
		}

		bool IList.IsFixedSize {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				return mList.@fixed;
			}
		}

		bool IList.IsReadOnly {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				return false;
			}
		}

		object IList.this[int index] {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				return mList[index];
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set {
				mList[index] = value;
			}
		}

		#endregion

		#region ICollection implementation

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void ICollection.CopyTo(System.Array array, int index)
		{
			((ICollection)mList).CopyTo(array,index);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Array clone() {
			return new Array(this);
		}

		int ICollection.Count {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				return (int)mList.length;
			}
		}

		bool ICollection.IsSynchronized {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				return false;
			}
		}

		object ICollection.SyncRoot {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				return null;
			}
		}

		#endregion

		public struct ArrayKeyEnumeratorStruct : IEnumerator
		{
			private Vector<dynamic>.VectorKeyEnumeratorStruct mVectorKeyEnum;
			private IEnumerator mDynamicEnum;
			private bool enumerateDynamics;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public ArrayKeyEnumeratorStruct(Vector<dynamic>.VectorKeyEnumeratorStruct venum, PlayScript.IDynamicClass dynprops)
			{
				mVectorKeyEnum = venum;
				mDynamicEnum = (dynprops==null) ? null : dynprops.__GetDynamicNames().GetEnumerator();
				enumerateDynamics = false;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public bool MoveNext ()
			{
				bool hasNext = false;
				if (!enumerateDynamics) 
				{
					hasNext = mVectorKeyEnum.MoveNext ();
					if(!hasNext && mDynamicEnum!=null)
						enumerateDynamics = true;
				}
				if (enumerateDynamics)
					hasNext = mDynamicEnum.MoveNext ();
				return hasNext;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Reset ()
			{
				mVectorKeyEnum.Reset();
				enumerateDynamics = false;
				if(mDynamicEnum!=null)
					mDynamicEnum.Reset ();
			}

			public dynamic Current {
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get {
					return (enumerateDynamics)?  mDynamicEnum.Current : mVectorKeyEnum.Current;
				}
			}
		}

		// this is the public struct enumerator, it does not implement IDisposable and doesnt allocate space on the heap
		public struct ArrayEnumeratorStruct
		{
			private Vector<dynamic>.VectorEnumeratorStruct mVectorEnum;
			private IEnumerator mDynamicEnum;
			private PlayScript.IDynamicClass mDynprops;
			private bool enumerateDynamics;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public ArrayEnumeratorStruct(Vector<dynamic>.VectorEnumeratorStruct venum, PlayScript.IDynamicClass dynprops)
			{
				mVectorEnum = venum;
				mDynprops    = dynprops;
				mDynamicEnum = (dynprops==null)? null : dynprops.__GetDynamicNames().GetEnumerator();
				enumerateDynamics = false;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public bool MoveNext ()
			{
				bool hasNext = false;
				if (!enumerateDynamics) 
				{
					hasNext = mVectorEnum.MoveNext ();
					if(!hasNext && mDynamicEnum!=null)
						enumerateDynamics = true;
				}
				if (enumerateDynamics)
					hasNext = mDynamicEnum.MoveNext ();
				return hasNext;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Reset ()
			{
				mVectorEnum.Reset();
				enumerateDynamics = false;
				if(mDynamicEnum!=null)
					mDynamicEnum.Reset ();
			}

			public dynamic Current {
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get {
					return (enumerateDynamics)? mDynprops.__GetDynamicValue(mDynamicEnum.Current as string) : mVectorEnum.Current;
				}
			}
		}

		// public get enumerator that returns a faster struct
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ArrayEnumeratorStruct GetEnumerator()
		{
			return new ArrayEnumeratorStruct(mList.GetEnumerator(), __dynamicProps);
		}

		// public get key enumerator that returns a faster struct
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ArrayKeyEnumeratorStruct GetKeyEnumerator()
		{
			return new ArrayKeyEnumeratorStruct(mList.GetKeyEnumerator(),__dynamicProps);
		}


		#region IEnumerable implementation

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)mList).GetEnumerator();
		}

		#endregion

		#region IKeyEnumerable implementation

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Array() {
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Array(int size)
		{
			mList.expand((uint)size);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Array(uint size)
		{
			mList.expand(size);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Array(double size)
		{
			mList.expand((uint)size);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Array(string s)
		{
			mList.push (s);
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Array (IEnumerable list)
		{
			mList.append(list);
		}

		public uint length
		{ 
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get { return mList.length; } 

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set { 
				mList.length = value;
			} 
		}

		public dynamic this[int i]
		{
			[return: AsUntyped]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				return mList[i];
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set {
				mList[i] = value;
			}
		}

		public dynamic this[uint i]
		{
			[return: AsUntyped]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				return mList[i];
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set {
				mList[i] = value;
			}
		}

		public dynamic this[long l]
		{
			[return: AsUntyped]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				return this [(int)l];

			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set {
				this [(int)l] = value;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool TryParseIndex(string input, out int index)
		{
			double d;
			if (double.TryParse (input, out d) && System.Math.Truncate (d) == d) {
				index = (int)d;
				return true;
			}
			index = -1;
			return false;
		}

		public dynamic this[string name]
		{
			[return: AsUntyped]
			get {
				// If we can convert the string to an index, then it is an indexed access.
				int index;
				if (TryParseIndex (name, out index)) {
					return mList[index];
				}
				// Otherwise this is a dynamic property. However we can't use mList[name] as we would lose the undefined information,
				// it would be replaced by default(T), so in this case null.
				if (__dynamicProps != null) {
					return __dynamicProps.__GetDynamicValue(name);
				}
				return PlayScript.Undefined._undefined;
			}

			set {
				// If we can convert the string to an index, then it is an indexed access.
				int index;
				if (TryParseIndex (name, out index)) {
					mList[index] = value;
					return;
				}
				if (__dynamicProps == null) {
					__dynamicProps = new PlayScript.DynamicProperties(this);
				}
				__dynamicProps.__SetDynamicValue(name, value);
			}
		}

		//
		// Treat floating point as a string. It will be considered an indexed access if
		// the value is an integer, otherwise it will be a dynamic property access.
		//
		public dynamic this[double d]
		{
			[return: AsUntyped]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				return this [d.ToString ()];
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set {
				this [d.ToString ()] = value;
			}
		}

		//
		// Treat floating point as a string. It will be considered an indexed access if
		// the value is an integer, otherwise it will be a dynamic property access.
		//
		public dynamic this[float f]
		{
			[return: AsUntyped]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				return this [f.ToString ()];
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set {
				this [f.ToString ()] = value;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public object[] ToArray()
		{
			return mList.ToArray();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public uint push(object value) {
			return mList.push(value);
		}

		[return: AsUntyped]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Array AsArray(Vector<dynamic> v)
		{
			var a = new Array();
			if (v != null) {
				a.mList = v;
			}
			return a;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Array reverse() {
			return AsArray(mList.reverse());
		}

		[return: AsUntyped]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public dynamic shift() {
			if (mList.length == 0) {
				return PlayScript.Undefined._undefined;
			}
			return mList.shift();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public uint unshift(object o) {
			return mList.unshift(o);
		}

#if PERFORMANCE_MODE
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void slice(int startIndex = 0, int endIndex = 16777215) {
			mList.slice(startIndex, endIndex);
		}
#else
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Array slice(int startIndex = 0, int endIndex = 16777215) {
			return AsArray(mList.slice(startIndex, endIndex));
		}
#endif

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
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

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
		}

		private class DefaultSorterOn : System.Collections.Generic.IComparer<object>
		{
			private string mFieldName;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int indexOf(object searchElement)
		{
			return mList.indexOf(searchElement);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override string toString()
		{
			return this.join(",");
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Add(object value)
		{
			mList.push(value);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public string join(string sep = ",") {
			return mList.join(sep);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector<dynamic> _GetInnerVector()
		{
			return mList;
		}


		#region IDynamicClass implementation

		// this method can be used to override the dynamic property implementation of this dynamic class
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void __SetDynamicProperties(PlayScript.IDynamicClass props) {
			__dynamicProps = props;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		dynamic PlayScript.IDynamicClass.__GetDynamicValue (string name) {
			object value = null;
			if (__dynamicProps != null) {
				value = __dynamicProps.__GetDynamicValue(name);
			}
			return value;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool PlayScript.IDynamicClass.__TryGetDynamicValue (string name, out object value) {
			if (__dynamicProps != null) {
				return __dynamicProps.__TryGetDynamicValue(name, out value);
			} else {
				value = null;
				return false;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void PlayScript.IDynamicClass.__SetDynamicValue (string name, object value) {
			if (__dynamicProps == null) {
				__dynamicProps = new PlayScript.DynamicProperties(this);
			}
			__dynamicProps.__SetDynamicValue(name, value);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool PlayScript.IDynamicClass.__DeleteDynamicValue (object name) {
			if (__dynamicProps != null) {
				return __dynamicProps.__DeleteDynamicValue(name);
			}
			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool PlayScript.IDynamicClass.__HasDynamicValue (string name) {
			if (__dynamicProps != null) {
				return __dynamicProps.__HasDynamicValue(name);
			}
			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		IEnumerable PlayScript.IDynamicClass.__GetDynamicNames () {
			if (__dynamicProps != null) {
				return __dynamicProps.__GetDynamicNames();
			}
			return null;
		}

		#endregion

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator dynamic[](Array a) {
			return a._GetInnerVector ().ToArray ();
		}
	}

#endif // OLD_ARRAY

}

