namespace _root {

	public static class Number {
	
		//
		// Extension Methods
		//
	
 		public static string toExponential(this double d, uint fractionDigits) {
			throw new System.NotImplementedException();
 		}
 	 	
		public static string toFixed(this double d, uint fractionDigits) {
			return d.ToString ( "F" + fractionDigits.ToString() );
		}
 	 	
		public static string toPrecision(this double d, uint precision) {
			throw new System.NotImplementedException();
		}
 	 	
		public static string toString(this double d) {
			return d.ToString();
		}

		public static string toString(this double d, double radix) {
			throw new System.NotImplementedException();
		}
 	 	
		public static double valueOf(this double d) {
			return d;
		}

 	 	//
 	 	// Constants
 	 	//
 	 	
 	 	public const double MAX_VALUE = System.Double.MaxValue;
 	 		
		public const double MIN_VALUE = System.Double.MinValue;

 	 	public const double @NaN = System.Double.NaN;

 	 	public const double NEGATIVE_INFINITY = System.Double.NegativeInfinity;

 	 	public const double POSITIVE_INFINITY = System.Double.PositiveInfinity;
	
	}

}
