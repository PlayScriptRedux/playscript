using System;
using System.Reflection;

namespace _root {

	public static class FunctionExtensions {

		public static dynamic apply(this Delegate d, dynamic thisArg, Array argArray) {
			var m = d.Method;
			return m.Invoke(d.Target, argArray.ToArray() );
		}

		public static dynamic call(this Delegate d, dynamic thisArg, params object[] args) {
			var m = d.Method;
			return m.Invoke(d.Target, args );
		}

		// this returns the number of arguments to the delegate method
		public static int get_length(this Delegate d) {
			return d.Method.GetParameters().Length;
		}

	}


}
