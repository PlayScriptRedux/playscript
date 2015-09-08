// Compiler options: -r:./as/Assert.dll
package
{
	import com.adobe.test.Assert;
	import flash.utils.getQualifiedClassName;
	
	public class VarArgsTest
	{
		public static function RunTest(val1:Number, val2:Number, ... rest):Number
		{
			Assert.expectEq("getQualifiedClassName(rest) == \"Array\"", "Array", getQualifiedClassName(rest));
			trace(val1, val2, rest);
			return 0;
		}
		
		public static function Main():int
		{
			if (RunTest(0, 1) != 0)
				return 1;
			if (RunTest(1, 2, "hello") != 0)
				return 2;
			if (RunTest(2, 3, "hello", "world") != 0)
				return 3;
			trace ("ok");
			return 0;
		}
	}
}

