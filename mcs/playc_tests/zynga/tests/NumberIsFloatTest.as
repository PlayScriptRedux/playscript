package
{
	[PlayScript.NumberIsFloat]
	public class NumberIsFloatTest implements INumberIsFloatTest
	{
		public static const INF:Number = Number.POSITIVE_INFINITY;

		private static var Value:Number = 3.14;

		private var _value:Number = 4.0;

		public function get value():Number
		{
			return _value;
		}

		public function set value(n:Number):void
		{
			_value = n;
		}

		public function update(num:Number):void
		{
		}

		public static function Main():void
		{
			var result:Number;
			var i:Number = Value;
			result = printNumber(i);
			var num:Number;
			num = INF;
			result = printNumber(num);

			var instance:NumberIsFloatTest = new NumberIsFloatTest();
			var j = instance._value;
			result = printNumber(j);

			result = printNumber(instance.value);
			instance.value = 1.25;
			result = printNumber(instance.value);
		}

		private static function printNumber(num:Number):Number
		{
			return DoubleLogger.printNumber(num);
		}
	}
}

[PlayScript.NumberIsFloat]
public interface INumberIsFloatTest
{
	function get value():Number;

	function set value(n:Number):void;

	function update(num:Number):void;
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
