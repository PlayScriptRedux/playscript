using System;

namespace _root
{
	public static class @int
	{
		//
		// Methods
		//

		public static string toExponential(this int i, uint fractionDigits) {
			throw new NotImplementedException();
		}
			
		public static string toFixed(this int i, uint fractionDigits) {
			throw new NotImplementedException();
		}
			
		public static string toPrecision(this int i, uint precision) {
			throw new NotImplementedException();
		}
			
		public static string toString(this int i) {
			throw new NotImplementedException();
		}
	
		public static string toString(this int i, uint radix) {
			return Convert.ToString(i, (int)radix);
		}
			
		public static int valueOf(this int i) {
			return i;
		}

		//
		// Constants
		//
			
		public const int MAX_VALUE  = 2147483647;

		public const int MIN_VALUE = -2147483648;

	}

	public static class @uint
	{
		public const uint MAX_VALUE = System.UInt32.MaxValue;
		public const uint MIN_VALUE = System.UInt32.MinValue;
	}
}

