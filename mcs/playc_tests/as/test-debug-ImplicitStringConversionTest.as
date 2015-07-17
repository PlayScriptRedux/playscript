// Compiler options: -psstrict- -r:./as/Assert.dll
package
{
	public class ImplicitStringConversionTest
	{
		public static function Main():int
		{
			var i:int = 1;
			Print(i);
			var n:Number = 4.12;
			Print(n);
			var o:Object = new Object();
			Print(o);
			var test:ImplicitStringConversionTest = new ImplicitStringConversionTest();
			Print(test);
			Print([0,1,2]);
			return 0;
		}

		private static function Print(s:String)
		{
			trace(s);
		}
	}
}
