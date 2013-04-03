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

namespace _root {


#if !NEWVECTOR

	public class Vector<T> : IEnumerable<T>
	{
		//
		// Properties
		//
	
		public bool @fixed 
		{ 
			get { return mFixed; } 
			set { mFixed = value;} 
		}

 	 	public uint length
 	 	{ 
 	 		get { return (uint)mList.Count; } 
 	 		set 
 	 		{ 
 	 			if (value == 0) {
 	 				mList.Clear();
 	 			} else {
					if (mList.Count < value)
					{
						// grow array
						while (mList.Count < value)
						{
							mList.Add(default(T));
						}
					} else if (mList.Count > value)
					{
						// shrink array
						mList.RemoveRange((int)value, (int)(mList.Count - value) );
					}

					if (mList.Count != value)
						throw new System.InvalidOperationException("there is a bug here");
 	 			}
 	 		} 
 	 	}

 	 	//
 	 	// Methods
 	 	//

		public Vector() {
		}
 	 	 	 	
		public Vector(Vector<T> v) {
			this.mList.AddRange(v);
 	 	}

		public Vector(Array a) {
			foreach (T item in a)
				this.mList.Add (item);
		}

		public Vector(uint length)
		{
			expand((int)length);
		}

		public Vector(uint length, bool @fixed = false)
		{
			expand((int)length);
			mFixed = @fixed;
		}


		public Vector(object o1)
		{
			this.mList.Add ((T)o1);
		}

		public Vector(T o1, T o2)
		{
			this.mList.Add (o1);
			this.mList.Add (o2);
		}

		public Vector(T o1, T o2, T o3)
		{
			this.mList.Add (o1);
			this.mList.Add (o2);
			this.mList.Add (o3);
		}

		public Vector(T o1, T o2, T o3, T o4)
		{
			this.mList.Add (o1);
			this.mList.Add (o2);
			this.mList.Add (o3);
			this.mList.Add (o4);
		}

		public Vector(T o1, T o2, T o3, T o4, T o5)
		{
			this.mList.Add (o1);
			this.mList.Add (o2);
			this.mList.Add (o3);
			this.mList.Add (o4);
			this.mList.Add (o5);
		}

		public Vector(T o1, T o2, T o3, T o4, T o5, T o6)
		{
			this.mList.Add (o1);
			this.mList.Add (o2);
			this.mList.Add (o3);
			this.mList.Add (o4);
			this.mList.Add (o5);
			this.mList.Add (o6);
		}

		public Vector(T o1, T o2, T o3, T o4, T o5, T o6, T o7)
		{
			this.mList.Add (o1);
			this.mList.Add (o2);
			this.mList.Add (o3);
			this.mList.Add (o4);
			this.mList.Add (o5);
			this.mList.Add (o6);
			this.mList.Add (o7);
		}

		public Vector(T o1, T o2, T o3, T o4, T o5, T o6, T o7, T o8)
		{
			this.mList.Add (o1);
			this.mList.Add (o2);
			this.mList.Add (o3);
			this.mList.Add (o4);
			this.mList.Add (o5);
			this.mList.Add (o6);
			this.mList.Add (o7);
			this.mList.Add (o8);
		}

		public Vector(T o1, T o2, T o3, T o4, T o5, T o6, T o7, T o8, params T[] args)
		{
			this.mList.Add (o1);
			this.mList.Add (o2);
			this.mList.Add (o3);
			this.mList.Add (o4);
			this.mList.Add (o5);
			this.mList.Add (o6);
			this.mList.Add (o7);
			this.mList.Add (o8);
			this.mList.AddRange (args);
		}

		public T this[int i]
		{
			get
			{
				return mList[i];
			}
			set
			{
				// auto expand vector
				expand(i + 1);
				mList[i] = value;
			}
		}

		public T this[uint i]
		{
			get
			{
				return mList[(int)i];
			}
			set
			{
				// auto expand vector
				expand( (int)(i + 1));
				mList[(int)i] = value;
			}
		}

		public T[] ToArray()
		{
			return mList.ToArray();
		}

		public void Add(T value) {
			mList.Add(value);
		}


		// optionally expands the vector to accomodate the new size
		// if the vector is big enough then nothing is done
		public void expand(int newSize) {
			if (mList.Count < newSize)
			{
				mList.Capacity = newSize;
				while (mList.Count < newSize)
				{
					mList.Add(default(T));
				}
			}
		}
		
		public Vector<T> concat(params object[] args) {

			Vector<T> v = new Vector<T>();
			// add this vector
			v.mList.AddRange(this);

			// concat all supplied vecots
			foreach (var o in args)
			{
				if (o is IEnumerable<T>)
				{
					v.mList.AddRange(o as IEnumerable<T>);
				} 
				else
				{
					throw new System.NotImplementedException();
				}

			}

			return v;
		}

		public bool every(Delegate callback, dynamic thisObject = null) {
			throw new System.NotImplementedException();
		}

		public Vector<T> filter(Delegate callback, dynamic thisObject = null) {
			throw new System.NotImplementedException();
 	 	}

		public void forEach(Delegate callback, dynamic thisObject = null) {
			throw new System.NotImplementedException();
		}

		public int indexOf(T searchElement) {
			return mList.IndexOf(searchElement);
		}

		public int indexOf(T searchElement, int fromIndex) {
			throw new System.NotImplementedException();
		}
 	 	
		public string join(string sep = ",") {
			throw new System.NotImplementedException();
		}

		public int lastIndexOf(T searchElement, int fromIndex = 0x7fffffff) {
			throw new System.NotImplementedException();
		}

		public Vector<T> map(Delegate callback, dynamic thisObject = null) {
			throw new System.NotImplementedException();
		}
 	 	
		public T pop() {
			var v = mList[ mList.Count - 1];
			mList.RemoveAt(mList.Count - 1);
			return v;
		}

		public uint push(T value) {
			mList.Add(value);
			return length;
		}
  
		public uint push(T value, params T[] args) {
			push(value);
			foreach(var e in args) {
				push(e);
			}
			return length;
		}
	 	 	
		public Vector<T> reverse() {
			throw new System.NotImplementedException();
		}
 	 	
		public T shift() {
			throw new System.NotImplementedException();
		}
 	 	
		public Vector<T> slice(int startIndex = 0, int endIndex = 16777215) {
			throw new System.NotImplementedException();
		}
 	 	
		public bool some(Delegate callback, dynamic thisObject = null) {
			throw new System.NotImplementedException();
		}

		private class FunctionSorter : System.Collections.Generic.IComparer<T>
		{
			public FunctionSorter(dynamic func)
			{
				mFunc = func;
			}

			public int Compare(T x, T y)
			{
				return (int)mFunc(x,y);
			}
			
			private dynamic mFunc;
		};

		private class OptionsSorter : System.Collections.Generic.IComparer<T>
		{
			public OptionsSorter(uint options)
			{
				// mOptions = options;
			}
			
			public int Compare(T x, T y)
			{
				//$$TODO examine options
				var xc = x as System.IComparable<T>;
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
		};


		public Vector<T> sort(dynamic sortBehavior) {

			if (sortBehavior is Delegate)
			{
				var fs = new FunctionSorter(sortBehavior);
			 	mList.Sort(fs);
				return this;
			}
			else if (sortBehavior is uint)
			{
				var os = new OptionsSorter((uint)sortBehavior);
				mList.Sort(os);
				return this;
			}
			else 
			{
				throw new System.NotImplementedException();
			}
		}
 	 	
		public Vector<T> splice(int startIndex, uint deleteCount = 4294967295, params T[] items) {
			Vector<T> removed = null;
			
			// determine number of items to delete
			uint toDelete = (uint)(this.length - startIndex);
			if (toDelete > deleteCount) toDelete = deleteCount;

			if (toDelete > 0)
			{
				removed = new Vector<T>();
				
				// build list of items we removed
				for (int i=0; i < toDelete; i++)
				{
					removed.push( mList[startIndex + i] );
				}
			
				// remove items
				mList.RemoveRange((int)startIndex, (int)toDelete);
			}
			
			if (items.Length > 0)
			{
				// insert range doesnt work when converting an object[] to dynamic[]
				// this.InsertRange(startIndex, items);

				for (int i=0; i < items.Length; i++)
				{
					mList.Insert((int)(startIndex + i), items[i] );
				}
			}
			
			return removed;
		}
 	 	
		public string toLocaleString() {
			throw new System.NotImplementedException();
		}

		public string toString() {
			throw new System.NotImplementedException();
		}

		public uint unshift(params T[] args) {
			for (int i=0; i < args.Length; i++)
			{
				mList.Insert(i, args[i] );
			}
			return this.length;
		}

		#region IEnumerable implementation
		public IEnumerator<T> GetEnumerator ()
		{
			return mList.GetEnumerator();
		}
		#endregion
		#region IEnumerable implementation
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return ((System.Collections.IEnumerable)mList).GetEnumerator();
		}
		#endregion

		private bool    mFixed = false;
		private readonly List<T> mList = new List<T>();
	}

#else

	// Optimized vector.
		
	public class Vector<T> : IList<T>
	{
		private const string ERROR_RESIZING_FIXED = "Error resizing fixed vector.";

		private T[] mArray;
		private uint mCount;
		private bool mFixed = false;
		
		//
		// Properties
		//
		
		public bool @fixed 
		{ 
			get { return mFixed; } 
			set { mFixed = value;} 
		}
		
		public uint length
		{ 
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
		
		public Vector(Vector<T> v) 
		{
			mArray = v.mArray.Clone() as T[];
			mCount = v.mCount;
			mFixed = v.mFixed;
		}
		
		public Vector(Array a)
		{
			throw new System.NotImplementedException();
		}
		
		public Vector(uint length = 0, bool @fixed = false)
		{
			if (length != 0)
				mArray = new T[(int)length];
			else
				mArray = new T[4];
			mCount = length;
			mFixed = @fixed;
		}
		
		public T this[int i]
		{
			get {
				return mArray[i];
			}
			set {
				if (i >= mCount) {
					expand(i+1);
				}
				mArray[i] = value;
			}
		}

		public T this[uint i]
		{
			get {
				return mArray[(int)i];
			}
			set {
				if (i >= mCount) {
					expand((int)(i+1));
				}
				mArray[(int)i] = value;
			}
		}
		
		public T[] ToArray()
		{
			T[] ret = new T[mCount];
			System.Array.Copy(mArray, ret, mCount);
			return ret;
		}

		public T[] _GetInnerArray()
		{
			return mArray;
		}

		private void EnsureCapacity(uint size)
		{
			if (mArray.Length < size) {
				if (mFixed)
					throw new InvalidOperationException(ERROR_RESIZING_FIXED);
				int newSize = mArray.Length * 2;
				while (newSize < size)
					newSize = newSize * 2;
				T[] newArray = new T[newSize];
				System.Array.Copy(mArray, newArray, mArray.Length);
				mArray = newArray;
			}
		}
		
		public void Add(T value) 
		{
			this.push (value);
		}

		private void _Insert(int index, T value) 
		{
			if (mCount >= mArray.Length)
				EnsureCapacity(mCount + 1);
			if (index < (int)mCount)
				System.Array.Copy (mArray, index, mArray, index + 1, (int)mCount - index);
			mArray[index] = value;
			mCount++;
		}

		private void _RemoveAt(int index)
		{
			if (index < 0 || index >= (int)mCount)
				throw new IndexOutOfRangeException();

			if (index == (int)mCount - 1) {
				mArray[index] = default(T);
			} else {
				System.Array.Copy (mArray, index + 1, mArray, index, (int)mCount - index - 1);
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

		public void append(Vector<T> vec)
		{
			EnsureCapacity(mCount + vec.mCount);
			System.Array.Copy (mArray, mCount, vec.mArray, 0, vec.mCount);
			mCount += vec.mCount;
		}

		public void append(IEnumerable<T> items)
		{
			if (items is IList<T>) {
				var list = (items as IList<T>);
				EnsureCapacity(mCount + (uint)list.Count);
			}
		
			foreach (var item in items) {
				this.Add (item);
			}
		}

		public Vector<T> concat(params object[] args) 
		{
			
			Vector<T> v = new Vector<T>((uint)args.Length + mCount);
			// add this vector
			v.append (this);

			// concat all supplied vectors
			foreach (var o in args) {
				if (o is Vector<T>) {
					v.append (o as Vector<T>);
				} else if (o is IEnumerable<T>) {
					v.append (o as IEnumerable<T>);
				} else {
					throw new System.NotImplementedException();
				}
			}
			
			return v;
		}
		
		public bool every(Delegate callback, dynamic thisObject = null) 
		{
			throw new System.NotImplementedException();
		}
		
		public Vector<T> filter(Delegate callback, dynamic thisObject = null) 
		{
			throw new System.NotImplementedException();
		}
		
		public void forEach(Delegate callback, dynamic thisObject = null) 
		{
			throw new System.NotImplementedException();
		}
		
		public int indexOf(T searchElement) 
		{
			uint len = mCount;
			if (len > 0) {
				switch (Type.GetTypeCode(typeof(T))) {
				case TypeCode.Object:
					object obj = searchElement;
					for (var i = 0; i < len; i++) {
						if (Object.ReferenceEquals(mArray[i], obj))
							return i;
					}
					break;
				case TypeCode.Int32:
					int[] intArry = mArray as int[];
					int intElem = (int)(object)searchElement;
					for (var i = 0; i < len; i++) {
						if (intArry[i] == intElem)
							return i;
					}
					break;
				case TypeCode.String:
					string[] strArry = mArray as string[];
					string strElem = searchElement as string;
					for (var i = 0; i < len; i++) {
						if (strArry[i] == strElem)
							return i;
					}
					break;
				default:
					for (var i = 0; i < len; i++) {
						if (mArray[i].Equals(searchElement))
							return i;
					}
					break;
				}
			}
			return -1;
		}
		
		public int indexOf(T searchElement, int fromIndex) 
		{
			throw new System.NotImplementedException();
		}
		
		public string join(string sep = ",") 
		{
			throw new System.NotImplementedException();
		}
		
		public int lastIndexOf(T searchElement, int fromIndex = 0x7fffffff) 
		{
			throw new System.NotImplementedException();
		}
		
		public Vector<T> map(Delegate callback, dynamic thisObject = null) 
		{
			throw new System.NotImplementedException();
		}
		
		public T pop() 
		{
			if (mFixed)
				throw new InvalidOperationException(ERROR_RESIZING_FIXED);
			T val = mArray[mCount - 1]; // Will throw if out of range
			mCount--;
			mArray[mCount] = default(T);
			return val;
		}
		
		public uint push(T value) 
		{
			if (mFixed)
				throw new InvalidOperationException(ERROR_RESIZING_FIXED);
			if (mCount >= mArray.Length)
				EnsureCapacity(mCount + 1);
			mArray[mCount] = value;
			mCount++;
			return mCount;
		}
		
		public uint push(T value, params T[] args) 
		{
			if (mFixed)
				throw new InvalidOperationException(ERROR_RESIZING_FIXED);
			uint len = (uint)args.Length;
			if (mArray.Length < mCount + 1 + len)
				EnsureCapacity(mCount + 1 + len);
			mArray[mCount++] = value;
			for (var i = 0; i < len; i++)
				mArray[mCount++] = args[i];
			return mCount;
		}
		
		public Vector<T> reverse() 
		{
			throw new System.NotImplementedException();
		}
		
		public T shift() 
		{
			if (mFixed)
				throw new InvalidOperationException(ERROR_RESIZING_FIXED);
			throw new System.NotImplementedException();
		}
		
		public Vector<T> slice(int startIndex = 0, int endIndex = 16777215) 
		{
			throw new System.NotImplementedException();
		}
		
		public bool some(Delegate callback, dynamic thisObject = null) 
		{
			throw new System.NotImplementedException();
		}
		
		private class FunctionSorter : System.Collections.Generic.IComparer<T>
		{
			public FunctionSorter(dynamic func)
			{
				mFunc = func;
			}
			
			public int Compare(T x, T y)
			{
				return (int)mFunc(x,y);
			}
			
			private dynamic mFunc;
		};
		
		private class OptionsSorter : System.Collections.Generic.IComparer<T>
		{
			public OptionsSorter(uint options)
			{
				// mOptions = options;
			}
			
			public int Compare(T x, T y)
			{
				//$$TODO examine options
				var xc = x as System.IComparable<T>;
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
		};
		
		public Vector<T> sort(dynamic sortBehavior) 
		{
			
			if (sortBehavior is Delegate)
			{
				var fs = new FunctionSorter(sortBehavior);
				System.Array.Sort (mArray, 0, (int)mCount, fs);
				return this;
			}
			else if (sortBehavior is uint)
			{
				var os = new OptionsSorter((uint)sortBehavior);
				System.Array.Sort (mArray, 0, (int)mCount, os);
				return this;
			}
			else 
			{
				throw new System.NotImplementedException();
			}
		}
		
		public Vector<T> splice(int startIndex, uint deleteCount = 4294967295) 
		{
			Vector<T> removed = null;

			if (mFixed)
				throw new InvalidOperationException(ERROR_RESIZING_FIXED);

			// determine number of items to delete
			int toDelete = (int)mCount - startIndex;
			if ((uint)toDelete > deleteCount) 
				toDelete = (int)deleteCount;

			if (toDelete == 1) {
				removed = new Vector<T>(1);
				removed.mArray[0] = mArray[startIndex];
				int toMove = (int)mCount - 1 - startIndex;
				if (toMove > 0)
					System.Array.Copy (mArray, startIndex + toDelete, mArray, startIndex, toMove);
				mArray[mCount - 1] = default(T);
				mCount--;
			} else if (toDelete > 1) {
				removed = new Vector<T>((uint)toDelete);
				System.Array.Copy (mArray, startIndex, removed.mArray, 0, toDelete);
				int toMove = (int)mCount - toDelete - startIndex;
				if (toMove > 0)
					System.Array.Copy (mArray, startIndex + toDelete, mArray, startIndex, toMove);
				System.Array.Clear (mArray, startIndex + toMove, toDelete);
				mCount = (uint)(startIndex + toMove);
			}
			
			return removed;
		}
		
		public Vector<T> splice(int startIndex, uint deleteCount = 4294967295, params T[] items) 
		{
			Vector<T> removed = null;

			if (mFixed)
				throw new InvalidOperationException(ERROR_RESIZING_FIXED);

			// determine number of items to delete
			int toDelete = (int)mCount - startIndex;
			if ((uint)toDelete > deleteCount) 
				toDelete = (int)deleteCount;
			
			if (toDelete == 1) {
				removed = new Vector<T>(1);
				removed.mArray[0] = mArray[startIndex];
				int toMove = (int)mCount - 1 - startIndex;
				if (toMove > 0)
					System.Array.Copy (mArray, startIndex + toDelete, mArray, startIndex, toMove);
				mArray[mCount - 1] = default(T);
				mCount--;
			} else if (toDelete > 1) {
				removed = new Vector<T>((uint)toDelete);
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
		
		public void splice_noret(int startIndex, uint deleteCount = 4294967295) 
		{
			if (mFixed)
				throw new InvalidOperationException(ERROR_RESIZING_FIXED);

			// determine number of items to delete
			int toDelete = (int)mCount - startIndex;
			if ((uint)toDelete > deleteCount) 
				toDelete = (int)deleteCount;
			
			if (toDelete == 1) {
				int toMove = (int)mCount - 1 - startIndex;
				if (toMove > 0)
					System.Array.Copy (mArray, startIndex + toDelete, mArray, startIndex, toMove);
				mArray[mCount - 1] = default(T);
				mCount--;
			} else if (toDelete > 1) {
				int toMove = (int)mCount - toDelete - startIndex;
				if (toMove > 0)
					System.Array.Copy (mArray, startIndex + toDelete, mArray, startIndex, toMove);
				System.Array.Clear (mArray, startIndex + toMove, toDelete);
				mCount = (uint)(startIndex + toMove);
			}
		}

		public void splice_noret(int startIndex, uint deleteCount = 4294967295, params T[] items) 
		{
			if (mFixed)
				throw new InvalidOperationException(ERROR_RESIZING_FIXED);

			// determine number of items to delete
			int toDelete = (int)mCount - startIndex;
			if ((uint)toDelete > deleteCount) 
				toDelete = (int)deleteCount;
			
			if (toDelete == 1) {
				int toMove = (int)mCount - 1 - startIndex;
				if (toMove > 0)
					System.Array.Copy (mArray, startIndex + toDelete, mArray, startIndex, toMove);
				mArray[mCount - 1] = default(T);
				mCount--;
			} else if (toDelete > 1) {
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
		}
		
		public string toLocaleString() 
		{
			throw new System.NotImplementedException();
		}
		
		public string toString() 
		{
			throw new System.NotImplementedException();
		}

		public uint unshift(T item) 
		{
			if (mFixed)
				throw new InvalidOperationException(ERROR_RESIZING_FIXED);
			if (mCount >= mArray.Length)
				EnsureCapacity(mCount + 1);
			if (mCount > 0)
				System.Array.Copy (mArray, 0, mArray, 1, (int)mCount);
			mArray[0] = item;
			mCount++;
			return mCount;
		}

		public uint unshift(T item, params T[] args) 
		{
			if (mFixed)
				throw new InvalidOperationException(ERROR_RESIZING_FIXED);
			uint argsLen = (uint)args.Length;
			EnsureCapacity(mCount + 1 + argsLen);
			if (mCount > 0)
				System.Array.Copy (mArray, 0, mArray, args.Length, (int)mCount);
			mArray[0] = item;
			System.Array.Copy (args, 0, mArray, 1, argsLen);
			mCount += 1 + argsLen;
			return mCount;
		}
		
		#region IList implementation

		int IList<T>.IndexOf (T item)
		{
			return this.indexOf(item);
		}

		void IList<T>.Insert (int index, T item)
		{
			_Insert (index, item);
		}

		void IList<T>.RemoveAt (int index)
		{
			_RemoveAt(index);
		}

		#endregion

		#region ICollection implementation

//		void ICollection<T>.Add (T item)
//		{
//			this._Add (item);
//		}

		void ICollection<T>.Clear ()
		{
			this.length = 0;
		}

		bool ICollection<T>.Contains (T item)
		{
			return this.indexOf(item) != -1;
		}

		void ICollection<T>.CopyTo (T[] array, int arrayIndex)
		{
			System.Array.Copy (mArray, 0, array, arrayIndex, (int)mCount);
		}

		bool ICollection<T>.Remove (T item)
		{
			int i = this.indexOf(item);
			if (i >= 0) {
				_RemoveAt(i);
				return true;
			} else {
				return false;
			}
		}

		int ICollection<T>.Count {
			get {
				return (int)mCount;
			}
		}

		bool ICollection<T>.IsReadOnly {
			get {
				return false;
			}
		}

		#endregion

		#region IEnumerable implementation

		private class VectorEnumerator : IEnumerator<T>
		{
			public Vector<T> mVector;
			public int mIndex;

			public VectorEnumerator(Vector<T> vector)
			{
				mVector = vector;
				mIndex = -1;
			}

			#region IEnumerator implementation

			public bool MoveNext ()
			{
				mIndex++;
				return mIndex < mVector.mCount;
			}

			public void Reset ()
			{
				mIndex = -1;
			}

			object System.Collections.IEnumerator.Current {
				get {
					return mVector.mArray[mIndex];
				}
			}

			#endregion

			#region IDisposable implementation

			public void Dispose ()
			{
			}

			#endregion

			#region IEnumerator implementation

			public T Current {
				get {
					return mVector.mArray[mIndex];
				}
			}

			#endregion

		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator ()
		{
			return new VectorEnumerator(this);
		}

		#endregion

		#region IEnumerable implementation

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return new VectorEnumerator(this);
		}

		#endregion
	}

#endif

}

