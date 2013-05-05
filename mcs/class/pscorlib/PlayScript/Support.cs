using System;

namespace PlayScript
{
	public static class Support
	{
		// Simple method to convert a .NET array to AS Array for var arg list methods.
		public static _root.Array CreateArgListArray(object[] argList) {
			// If this is optimized, vararg method calls will run faster.
			return new _root.Array(argList);
		}
	}
}

