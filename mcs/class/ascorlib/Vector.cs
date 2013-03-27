
using System;
using System.Collections.Generic;

namespace _root {


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
 	 	 	 	
		public Vector(Vector<T> v) {
			throw new System.NotImplementedException();
 	 	}

		public Vector(Array a) {
			throw new System.NotImplementedException();
		}

		public Vector(uint length = 0, bool @fixed = false)
		{
			mFixed = @fixed;
			expand((int)length);
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
			return (uint)(mList.Count - 1);
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


}

