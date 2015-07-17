// Compiler options: -r:./as/Assert.dll
package
{
	public class ScalarConstructorTest 
	{
		public static function Main():void
		{
			var num1:Number = new Number(GetNumber());
			trace(num1);
			var num2:Number = new Number(3.14);
			trace(num2);
			var bool:Boolean = new Boolean(true);
			trace(bool);
			var string:String = new String("some string"); 
			trace(string);
		}

		public static function GetNumber():Number
		{
			return Number.NaN;
		}
	}
}
