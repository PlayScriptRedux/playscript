package
{
	[PlayScript.NumberIsFloat]
	public class NumberIsFloatTest implements INumberIsFloatTest
	{
		public static const INF:Number = Number.POSITIVE_INFINITY;

		private static var Value:Number = 3.14;

		private var _value:Number = 4.0;

		public static function easeOut( t : Number, b : Number, c : Number, d : Number ) : Number
		{
			if ( ( t /= d ) < ( 1 / 2.75 ) )
			{
				return c * ( 7.5625 * t * t ) + b;
			}
			else if ( t < ( 2 / 2.75 ) )
			{
				return c * ( 7.5625 * ( t -= ( 1.5 / 2.75 ) ) * t + 0.75 ) + b;
			}
			else if ( t < ( 2.5 / 2.75 ) )
			{
				return c * ( 7.5625 * ( t -= ( 2.25 / 2.75 ) ) * t + 0.9375 ) + b;
			}
			else
			{
				return c * ( 7.5625 * ( t -= ( 2.625 / 2.75 ) ) * t + 0.984375 ) + b;
			}
		}

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
