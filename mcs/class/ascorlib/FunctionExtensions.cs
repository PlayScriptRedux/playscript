using System;
using System.Reflection;

namespace _root {

	public static class FunctionExtensions {

		public static dynamic apply(this Delegate d, dynamic thisArg, Array argArray) {
			return d.DynamicInvoke(argArray != null ? argArray.ToArray() : null);
		}

		public static dynamic call(this Delegate d, dynamic thisArg, params object[] args) {
			return d.DynamicInvoke(args);
		}

		// this returns the number of arguments to the delegate method
		public static int get_length(this Delegate d) {
			return d.Method.GetParameters().Length;
		}

	}


}
