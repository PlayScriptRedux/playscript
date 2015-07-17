package
{
	public class FunctionTest
	{
		public static function aMethod(... args):String { return args.ToString(); }

		public static function Main():int
		{
			// this line should not throw this error:
			// "error CS0584: Internal compiler error: Object reference not set to an instance of an object"
			var f:Object = Function(FunctionTest.aMethod);

			trace("f:", f);
			return 0;
		}
	}
}
