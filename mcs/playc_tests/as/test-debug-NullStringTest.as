// Compiler options: -r:./as/Assert.dll
package
{
	public class NullStringTest
	{
		public static function Main():int
		{
			var s1:String = String(null);
			if (s1 !== "null")
				return 1;
			var o:Object = null;
			var s2:String = String(o);
			if (s2 !== "null")
				return 2;
			var s3:String = null;
			if (s3 !== null)
				return 3;
			var o:Object = null;
			var s4:String = String(o);
			if (s4 !== "null")
				return 4;
			var s5:String = o as String;
			if (s5 !== null)
				return 5;

			return 0;
		}
	}
}
