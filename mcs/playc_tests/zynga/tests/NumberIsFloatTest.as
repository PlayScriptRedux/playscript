package
{
	[PlayScript.NumberIsFloat]
	public class NumberIsFloatTest
	{
		public static const INF:Number = Number.POSITIVE_INFINITY;

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
			return DoubleLogger.printNumber(num);
		}
	}
}

public class DoubleLogger
{
	public static function printNumber(num:Number):Number
	{
		trace(num);
		num = FloatLogger.printNumber(num);
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
