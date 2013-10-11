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

namespace _root
{
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
				mArray._TrimCapacity();
				return mArray._GetInnerArray();
			}
		}
	}

	[DynamicClass]
	[DebuggerDisplay("length = {length}")]
	[DebuggerTypeProxy(typeof(ArrayDebugView))]
	public sealed class Array : _root.Object, IList, PlayScript.IDynamicClass, PlayScript.IKeyEnumerable
	{
		#region IList implementation

		int IList.Add(object value)
		{
			return (int) push (value);
		}

		void IList.Clear()
		{
			this.length = 0;
		}

		bool IList.Contains(object value)
		{
			return this.indexOf(value) >= 0;
		}

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
			get {
				return false;
			}
		}

		bool IList.IsReadOnly {
			get {
				return false;
			}
		}

		object IList.this[int index] {
			get {
				return (object)this[index];
			}
			set {
				this[index] = value;
			}
		}

		#endregion

		#region ICollection implementation

		void ICollection.CopyTo(System.Array array, int index)
		{
			System.Array.Copy(mArray, 0, array, index, mCount);
		}

		bool ICollection.IsSynchronized {
			get {
				return false;
			}
		}

		int ICollection.Count {
			get {
				return (int)this.length;
			}
		}

		object ICollection.SyncRoot {
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
		private PlayScript.IDynamicClass __dynamicProps = null;		// By default it is not created as it is not commonly used (nor a good practice).
																	// We create it only if there is a dynamic set.

		private static object[] sEmptyArray = new object[0];

		//
		// Properties
		//

		public uint length
		{
			#if NET_4_5 || PLATFORM_MONOTOUCH || PLATFORM_MONODROID
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			#endif
			get { return mCount; } 
			set { 
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

		public Array()
		{
			mArray = sEmptyArray;
			mCount = 0;
		}

		public Array(Array a)
		{
			mArray = new object[a.length];
			this.append((IEnumerable)a);
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

		public Array(IList a)
		{
			mArray = new object[a.Count];
			this.append((IEnumerable)a);
		}

		public dynamic this[int i]
		{
			#if NET_4_5 || PLATFORM_MONOTOUCH || PLATFORM_MONODROID
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			#endif
			get {
				#if PERFORMANCE_MODE && DEBUG
				if ((i >= mCount) || (i < 0))
				{
					throw new IndexOutOfRangeException();
				}
				#elif PERFORMANCE_MODE
				#else
				if ((i >= mCount) || (i < 0))
				{
					return PlayScript.Undefined._undefined;
				}
				#endif
				return mArray[i];
			}
			#if NET_4_5 || PLATFORM_MONOTOUCH || PLATFORM_MONODROID
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			#endif
			set {
				#if PERFORMANCE_MODE && DEBUG
				if (i >= mCount) {
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
			#if NET_4_5 || PLATFORM_MONOTOUCH || PLATFORM_MONODROID
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			#endif
			get {
				#if PERFORMANCE_MODE && DEBUG
				if (i >= mCount)
				{
					throw new IndexOutOfRangeException();
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
			#if NET_4_5 || PLATFORM_MONOTOUCH || PLATFORM_MONODROID
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			#endif
			set {
				#if PERFORMANCE_MODE && DEBUG
				if (i >= mCount) {
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
			#if NET_4_5 || PLATFORM_MONOTOUCH || PLATFORM_MONODROID
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			#endif
			get {
				return this [(int)l];

			}
			#if NET_4_5 || PLATFORM_MONOTOUCH || PLATFORM_MONODROID
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			#endif
			set {
				this [(int)l] = value;
			}
		}

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
			get {
				// If we can convert the string to an index, then it is an indexed access.
				int index;
				if (TryParseIndex (name, out index)) {
					return mArray [index];
				}
				// Otherwise this is a dynamic property.
				if (__dynamicProps == null) {
					return PlayScript.Undefined._undefined;
				}
				return __dynamicProps.__GetDynamicValue(name);	// The instance that was set was only of dynamic type (or undefined)
			}
			set {
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
			get {
				return this [d.ToString ()];
			}
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
			get {
				return this [f.ToString ()];
			}
			set {
				this [f.ToString ()] = value;
			}
		}

		public object[] ToArray()
		{
			object[] ret = new object[mCount];
			System.Array.Copy(mArray, ret, mCount);
			return ret;
		}

		public object[] _GetInnerArray()
		{
			return mArray;
		}

		public void _TrimCapacity()
		{
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

		public void Add(object value) 
		{
			this.push (value);
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
		public void expand(uint newSize) 
		{
			EnsureCapacity(newSize);
			if (mCount < newSize)
				mCount = newSize;
		}

		public void append(Array vec)
		{
			EnsureCapacity(mCount + vec.mCount);
			System.Array.Copy (vec.mArray, 0, mArray, mCount, vec.mCount);
			mCount += vec.mCount;
		}

		public void append(IEnumerable items)
		{
			if (items == null) {
				return;
			}
			if (items is IList) {
				var list = (items as IList);
				EnsureCapacity(mCount + (uint)list.Count);
			}

			foreach (var item in items) {
				this.Add (item);
			}
		}


		public void append(IEnumerable<object> items)
		{
			if (items is IList<object>) {
				var list = (items as IList<object>);
				EnsureCapacity(mCount + (uint)list.Count);
			}

			foreach (var item in items) {
				this.Add (item);
			}
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


		public void copyTo(Array dest, int sourceIndex, int destIndex, int count) {
			System.Array.Copy(this.mArray, sourceIndex, dest.mArray, destIndex, count);
		}

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

		public Array sort(Delegate sortBehavior)
		{
			return sortInternal(sortBehavior);
		}

		public Array sort(object sortBehavior = null) 
		{
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
			for (var i = fromIndex; i < mCount; i++) {
				if (mArray [i] == searchElement || mArray [i].Equals (searchElement)) {
					return i;
				}
			}
			return -1;
		}

		public string join(string sep = ",") 
		{
			var sb = new System.Text.StringBuilder();
			bool needsSeperator = false;
			for (var i = 0; i < mCount; i++) {
				var item = mArray [i];
				if (needsSeperator) {
					sb.Append(sep);
				}
				if (item != null) {
					sb.Append(item.ToString());
				}
				needsSeperator = true;
			}
			return sb.ToString();
		}

		public int lastIndexOf(object searchElement, int fromIndex = 0x7fffffff) 
		{
			throw new System.NotImplementedException();
		}

		public Array map(Delegate callback, object thisObject = null) 
		{
			throw new System.NotImplementedException();
		}

		public dynamic pop() 
		{
			if (mCount == 0) {
				return PlayScript.Undefined._undefined;
			}
			object val = mArray[mCount - 1];
			mCount--;
			mArray[mCount] = null;
			return val;
		}

		#if NET_4_5 || PLATFORM_MONOTOUCH || PLATFORM_MONODROID
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		#endif
		public uint push(object value)
		{
			if (mCount >= mArray.Length)
				EnsureCapacity((uint)(1.25 * (mCount + 1)));
			mArray[mCount] = value;
			mCount++;
			return mCount;
		}

		public uint push(object value, params object[] args) 
		{
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
			var nv = new Array(length);
			int l = (int)length;
			for (int i = 0; i < l; i++)
			{
				nv[i] = this[l - i - 1];
			}
			return nv;
		}

		public dynamic shift() 
		{
			if (mCount == 0)
			{
				return PlayScript.Undefined._undefined;
			}
			object v = this[0];
			_RemoveAt(0);
			return v;
		}

		public Array slice(int startIndex = 0, int endIndex = 16777215) 
		{
			if (startIndex < 0) 
				throw new InvalidOperationException("splice error");

			if (endIndex < 0) endIndex = (int)mCount + endIndex;		// If negative, starts from the end

			if (endIndex > (int)mCount) endIndex = (int)mCount;

			int count = endIndex - startIndex;
			if (count < 0)
				count = 0;

			var result = new Array((uint)count);
			System.Array.Copy(mArray, startIndex, result.mArray, 0, count);
			return result;
		}

		public bool some(Delegate callback, object thisObject = null) 
		{
			throw new System.NotImplementedException();
		}

		private class TypedFunctionSorter : System.Collections.Generic.IComparer<object>
		{
			public TypedFunctionSorter(System.Func<object, object, int> comparerDelegate)
			{
				mDelegate = comparerDelegate;
			}

			public int Compare(object x, object y)
			{
				return mDelegate.Invoke(x, y);
			}

			private System.Func<object, object,int> mDelegate;
		}


		private class FunctionSorter : System.Collections.Generic.IComparer<object>
		{
			public FunctionSorter(object func)
			{
				mDelegate = func as Func<object,object,int>;
				mFunc = func;
			}

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
			public OptionsSorter(uint options)
			{
				// mOptions = options;
			}

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

		public override string toString() 
		{
			return this.join(",");
		}

		public uint unshift(object item) 
		{
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

		private class ArrayEnumeratorClass : IEnumerator
		{
			private readonly IList mVector;
			private int mIndex;
			private IDynamicClass mDynamicProps;
			private IEnumerator mDynamicEnumerator;

			public ArrayEnumeratorClass(IList vector, IDynamicClass dynamicProps)
			{
				mVector = vector;
				mIndex = -1;
				mDynamicProps = dynamicProps;
				mDynamicEnumerator = mDynamicProps != null ? mDynamicProps.__GetDynamicNames ().GetEnumerator () : null;
			}

			#region IEnumerator implementation

			public bool MoveNext ()
			{
				mIndex++;
				if (mIndex < mVector.Count)
					return true;
				if (mDynamicEnumerator != null)
					return mDynamicEnumerator.MoveNext ();
				return false;
			}

			public void Reset ()
			{
				mIndex = -1;
				if (mDynamicEnumerator != null)
					mDynamicEnumerator.Reset ();
			}

			object System.Collections.IEnumerator.Current {
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

			public ArrayEnumeratorStruct(IList vector, IDynamicClass dynamicProps)
			{
				mVector = vector;
				mIndex = -1;
				mDynamicProps = dynamicProps;
				mDynamicEnumerator = mDynamicProps != null ? mDynamicProps.__GetDynamicNames ().GetEnumerator () : null;
			}

			#region IEnumerator implementation

			public bool MoveNext ()
			{
				mIndex++;
				if (mIndex < mVector.Count)
					return true;
				if (mDynamicEnumerator != null)
					return mDynamicEnumerator.MoveNext ();
				return false;
			}

			public void Reset ()
			{
				mIndex = -1;
				if (mDynamicEnumerator != null)
					mDynamicEnumerator.Reset ();
			}

			public object Current {
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
		public ArrayEnumeratorStruct GetEnumerator ()
		{
			return new ArrayEnumeratorStruct(this, __dynamicProps);
		}

		#endregion

		#region IEnumerable implementation

		// private IEnumerable get enumerator that returns a (slower) class on the heap
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return new ArrayEnumeratorClass(this, __dynamicProps);
		}

		#endregion


		#region IKeyEnumerable implementation

		private class ArrayKeyEnumeratorClass : IEnumerator
		{
			private readonly IList mVector;
			private int mIndex;
			private IEnumerator mDynamicEnumerator;

			public ArrayKeyEnumeratorClass(IList vector, IDynamicClass dynamicProps)
			{
				mVector = vector;
				mIndex = -1;
				mDynamicEnumerator = dynamicProps != null ? dynamicProps.__GetDynamicNames ().GetEnumerator () : null;
			}

			#region IEnumerator implementation

			public bool MoveNext ()
			{
				mIndex++;
				if (mIndex < mVector.Count)
					return true;
				if (mDynamicEnumerator != null)
					return mDynamicEnumerator.MoveNext ();
				return false;
			}

			public void Reset ()
			{
				mIndex = -1;
				if (mDynamicEnumerator != null)
					mDynamicEnumerator.Reset ();
			}

			object System.Collections.IEnumerator.Current {
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

			public ArrayKeyEnumeratorStruct(IList vector, IDynamicClass dynamicProps)
			{
				mVector = vector;
				mIndex = -1;
				mDynamicEnumerator = dynamicProps != null ? dynamicProps.__GetDynamicNames ().GetEnumerator () : null;
			}

			#region IEnumerator implementation

			public bool MoveNext ()
			{
				mIndex++;
				if (mIndex < mVector.Count)
					return true;
				if (mDynamicEnumerator != null)
					return mDynamicEnumerator.MoveNext ();
				return false;
			}

			public void Reset ()
			{
				mIndex = -1;
				if (mDynamicEnumerator != null)
					mDynamicEnumerator.Reset ();
			}

			// unfortunately this has to return object because the for() loop could use a non-int as its variable, causing bad IL
			public object Current {
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
		public ArrayKeyEnumeratorStruct GetKeyEnumerator()
		{
			return new ArrayKeyEnumeratorStruct(this, __dynamicProps);
		}

		// private IKeyEnumerable get enumerator that returns a (slower) class on the heap
		IEnumerator PlayScript.IKeyEnumerable.GetKeyEnumerator()
		{
			return new ArrayKeyEnumeratorClass(this, __dynamicProps);
		}

		#endregion

		#region IDynamicClass implementation

		// this method can be used to override the dynamic property implementation of this dynamic class
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

		int IList.Add(object value)
		{
			return ((IList)mList).Add (value);
		}

		void IList.Clear()
		{
			((IList)mList).Clear ();
		}

		bool IList.Contains(object value)
		{
			return ((IList)mList).Contains (value);
		}

		int IList.IndexOf(object value)
		{
			return ((IList)mList).IndexOf (value);
		}

		void IList.Insert(int index, object value)
		{
			((IList)mList).Insert (index, value);
		}

		void IList.Remove(object value)
		{
			((IList)mList).Remove (value);
		}

		void IList.RemoveAt(int index)
		{
			((IList)mList).RemoveAt (index);
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
				return (int)mList.length;
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
			private Vector<dynamic>.VectorKeyEnumeratorStruct mVectorKeyEnum;
			private IEnumerator mDynamicEnum;
			private bool enumerateDynamics;

			public ArrayKeyEnumeratorStruct(Vector<dynamic>.VectorKeyEnumeratorStruct venum, PlayScript.IDynamicClass dynprops)
			{
				mVectorKeyEnum = venum;
				mDynamicEnum = (dynprops==null) ? null : dynprops.__GetDynamicNames().GetEnumerator();
				enumerateDynamics = false;
			}
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

			public void Reset ()
			{
				mVectorKeyEnum.Reset();
				enumerateDynamics = false;
				if(mDynamicEnum!=null)
					mDynamicEnum.Reset ();
			}

			public dynamic Current {
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

			public ArrayEnumeratorStruct(Vector<dynamic>.VectorEnumeratorStruct venum, PlayScript.IDynamicClass dynprops)
			{
				mVectorEnum = venum;
				mDynprops    = dynprops;
				mDynamicEnum = (dynprops==null)? null : dynprops.__GetDynamicNames().GetEnumerator();
				enumerateDynamics = false;
			}

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

			public void Reset ()
			{
				mVectorEnum.Reset();
				enumerateDynamics = false;
				if(mDynamicEnum!=null)
					mDynamicEnum.Reset ();
			}

			public dynamic Current {
				get {
					return (enumerateDynamics)? mDynprops.__GetDynamicValue(mDynamicEnum.Current as string) : mVectorEnum.Current;
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

		public dynamic this[long l]
		{
			get {
				return this [(int)l];

			}
			set {
				this [(int)l] = value;
			}
		}

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
			get {
				return this [d.ToString ()];
			}
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
			get {
				return this [f.ToString ()];
			}
			set {
				this [f.ToString ()] = value;
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
				return PlayScript.Undefined._undefined;
			}
			return mList.shift();
		}

		public uint unshift(object o) {
			return mList.unshift(o);
		}

#if PERFORMANCE_MODE
		public void slice(int startIndex = 0, int endIndex = 16777215) {
			mList.slice(startIndex, endIndex);
		}
#else
		public Array slice(int startIndex = 0, int endIndex = 16777215) {
			return AsArray(mList.slice(startIndex, endIndex));
		}
#endif

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
		}

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

		public static implicit operator dynamic[](Array a) {
			return a._GetInnerVector ().ToArray ();
		}
	}

#endif // OLD_ARRAY

}

