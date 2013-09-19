package
{
	[PlayScript.NumberIsFloat]
	public class NumberIsFloatTest
	{
		// TODO: Need implicit cast from double to float
		//public static const INF:Number = Number.POSITIVE_INFINITY;
		public static const INF:Number = 12.3432;

		private static var Value:Number = 3.14;

		private var value:Number = 4.0;

		public static function Main():void
		{
			var result:Number;
			var i:Number = Value;
			result = printNumber(i);
			var num:Number;
			num = INF;
			result = printNumber(num);

			var instance:NumberIsFloatTest = new NumberIsFloatTest();
			var j = instance.value;
			result = printNumber(j);
		}

		private static function printNumber(num:Number):Number
		{
			// TODO: Need implicit cast from double to float
			//return DoubleLogger.printNumber(num);
			return System.Convert.ToSingle(DoubleLogger.printNumber(num));
		}
	}
}

public class DoubleLogger
{
	public static function printNumber(num:Number):Number
	{
		trace(num);
		// TODO: Need implicit cast from double to float
		//num = FloatLogger.printNumber(num);
		num = FloatLogger.printNumber(System.Convert.ToSingle(num));
		return num;
	}
}

[PlayScript.NumberIsFloat]
public class FloatLogger
{
	public static function printNumber(num:Number):Number
	{
		trace(num);
		return num;
	}
}
