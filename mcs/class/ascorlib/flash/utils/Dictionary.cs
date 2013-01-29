using System;
using System.Collections.Generic;

namespace flash.utils
{
	public class Dictionary : IEnumerable<object>
	{
		#region IEnumerable implementation
		public IEnumerator<object> GetEnumerator ()
		{
			return ((IEnumerable<object>)mDictionary.Values).GetEnumerator();
		}
		#endregion
		#region IEnumerable implementation
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return mDictionary.Values.GetEnumerator();
		}
		#endregion

		public Dictionary(bool weakKeys = false) {
			mDictionary = new Dictionary<object, object>();
		}

		public string toJSON(string k) {
			return null;
		}
		
		public int length { 
			get {
				return mDictionary.Count;
			} 	
			set {
				if (value == 0)
					mDictionary.Clear();
				else
					throw new System.NotImplementedException();
			}
		}

		public dynamic this [object key] {
			get {
				// the flash dictionary implementation does not throw if key not found
				object value;
				if (mDictionary.TryGetValue(key, out value)) {
					return value;
				} else {
					return null;
				}
			}
			set {
				mDictionary[key] = value;
			}
		}

		public void Remove (object key)
		{
			mDictionary.Remove(key);
		}

		public bool ContainsKey (object key)
		{
			return mDictionary.ContainsKey(key);
		}

		public bool Contains (object key)
		{
			return mDictionary.ContainsKey(key);
		}

		private Dictionary<object, object> mDictionary;
	}
}

