using System;
using System.Collections.Generic;

namespace _root
{
	public class Vector<T> : List<T>
	{
		public Vector ()
		{
		}

		public int length {
			get { return Count; }
			set { }
		}

		public void push(T value) {
			Add (value);
		}

		public int indexOf(T value) {
			return IndexOf(value);
		}

	}
}

