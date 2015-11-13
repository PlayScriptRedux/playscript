// Compiler options: -psstrict- -r:./as/Assert.dll
package
{
	import com.adobe.test.Assert;
	import flash.utils.getQualifiedClassName;
	
	public class UntypedParameterTest
	{
		public static function RunTest(pi:* = 3.14):void
		{
			Assert.expectEq("pi == 3.14", true, pi == 3.14);
			Assert.expectEq("pi === 3.14", true, pi === 3.14);
			// TODO: Need to make string/object comparisons invoke the dynamic runtime
			//Assert.expectEq("pi == \"3.14\"", true, pi == "3.14");
			Assert.expectEq("pi !== \"3.14\"", true, pi !== "3.14");
		}
		
		public function RunTest2(pi = 3.14):void
		{
			RunTest(pi);
		}
		
		public function RunTest3(pi):void
		{
			RunTest2(pi);
			RunTest2();
			try {
				throw new Error("blah");
			} catch (e) {
				Assert.expectEq("getQualifiedClassName(e) == \"Error\"", "Error", getQualifiedClassName(e));
			}
		}
		
		public static function Main():int
		{
			RunTest();
			var instance = new UntypedParameterTest();
			instance.RunTest2();
			instance.RunTest3(3.14);
			return 0;
		}
	}
}

