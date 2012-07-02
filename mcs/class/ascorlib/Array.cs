using System;
using System.Collections.Generic;

namespace _root
{
	public class Array : List<dynamic>
	{
		public Array() {
		}

		public Array(object arg1, params object[] args) {
			if (arg1 is int || arg1 is double) {
				int n = (int)arg1;
				for (var i = 0; i < n; i++) {
					this.Add (null);
				}
			} else {
				this.Add (arg1);
				if (args.Length > 0) {
					this.AddRange(args);
				}
			}
		}

		public int length 
		{
			get { return Count; }
			set { }
		}

		// Concatenates the elements specified in the parameters with the elements in an array and creates a new array.
		public Array concat(params object[] args) {
			var a = new Array();
			a.AddRange(this);
			a.AddRange (args);
			return null;
		}
	
		// Executes a test function on each item in the array until an item is reached that returns false for the specified function.
		public bool every(Delegate callback, object thisObject = null) {
			Func<object,bool> comp = (Func<object,bool>)(object)Delegate.CreateDelegate(typeof(Func<object,bool>), callback.Target, callback.Method, true);
			var l = this.Count;
			for (var i = 0; i < l; i++) {
				if (!comp(this[i])) {
					return false;
				}
			}
			return true;
		}

		// Executes a test function on each item in the array and constructs a new array for all items that return true for the specified function.
		public Array filter(Delegate callback, object thisObject) {
			throw new NotImplementedException();
		}
 	 	
		// Executes a function on each item in the array.
		public void forEach(Delegate callback, object thisObject = null) {
			throw new NotImplementedException();
		}
 	 	
		// Searches for an item in an array by using strict equality (===) and returns the index position of the item.
		public int indexOf(object searchElement, int fromIndex = 0) {
			return this.IndexOf(searchElement, fromIndex);
		}

		// Converts the elements in an array to strings, inserts the specified separator between the elements, concatenates them, and returns the resulting string.
		public string join(object sep) 
		{
			throw new NotImplementedException();
		}
 	 	
		// Searches for an item in an array, working backward from the last item, and returns the index position of the matching item using strict equality (===).
		public int lastIndexOf (object searchElement, int fromIndex = 0x7fffffff)
		{
			if (fromIndex == 0x7fffffff) {
				return this.LastIndexOf (searchElement);
			} else {
				return this.LastIndexOf (searchElement, fromIndex);
			}
		}
 	 	
		// Executes a function on each item in an array, and constructs a new array of items corresponding to the results of the function on each item in the original array.
		public Array map(Delegate callback, object thisObject = null) {
			throw new NotImplementedException();
		}
 	 	
		// Removes the last element from an array and returns the value of that element.
		public dynamic pop() {
			throw new NotImplementedException();
		}
 	 	
		// Adds one elements to the end of an array and returns the new length of the array.
		public uint push(object val) {
			this.Add (val);
			return (uint)Count;
		}

		// Adds one or more elements to the end of an array and returns the new length of the array.
		public uint push(object arg1, params object[] args) {
			this.Add (arg1);
			this.AddRange(args);
			return (uint)Count;
		}
 	 	
		// Reverses the array in place.
		public Array reverse() {
			throw new NotImplementedException();
		}
 	 	
		// Removes the first element from an array and returns that element.
		public object shift() {
			throw new NotImplementedException();
		}
 	 	
		// Returns a new array that consists of a range of elements from the original array, without modifying the original array.
		public Array slice(int startIndex = 0, int endIndex = 16777215) {
			throw new NotImplementedException();
		}
 	 	
		// Executes a test function on each item in the array until an item is reached that returns true.
		public bool some(Delegate callback, object thisObject = null) {
			throw new NotImplementedException();
		}
 	 	
		// Sorts the elements in an array.
		public Array sort(params object[] args) {
			throw new NotImplementedException();
		}
 	 	
		// Sorts the elements in an array according to one or more fields in the array.
		public Array sortOn(object fieldName, object options = null) {
			throw new NotImplementedException();
		}
 	 	
		// Adds elements to and removes elements from an array.
		public void splice(int startIndex, int deleteCount) {
			if (deleteCount == 1) {
				this.RemoveAt(startIndex);
			} else {
				this.RemoveRange(startIndex, deleteCount);
			}
		}

		public void splice(int startIndex, int deleteCount, params object[] values) {
			if (deleteCount == 1) {
				this.Remove (startIndex);
			} else {
				this.RemoveRange (startIndex, deleteCount);
			}
			this.InsertRange(startIndex, values);
		}

		public uint unshift(object o) {
			throw new NotImplementedException();
		}

		public uint unshift(object arg1, params object[] args) {
			throw new NotImplementedException();
		}
	}
}

