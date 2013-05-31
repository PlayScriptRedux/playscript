using System;
using System.Reflection;

namespace PlayScript
{
	public static class Support
	{
		// Simple method to convert a .NET array to AS Array for var arg list methods.
		public static _root.Array CreateArgListArray(object[] argList) {
			// If this is optimized, vararg method calls will run faster.
			return new _root.Array(argList);
		}

		// Call a static method with an argument list
		public static object VarArgCall(Type type, string methodName, object[] argList) {
			var mi = type.GetMethod (methodName, BindingFlags.Public | BindingFlags.Static);
			return mi.Invoke (null, argList);
		}
	}
}

