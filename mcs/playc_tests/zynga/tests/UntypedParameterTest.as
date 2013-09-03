package
{
	import com.adobe.test.Assert;
	
	public class UntypedParameterTest
	{
		public static function CheckType(obj):void
		{
			if (obj is String) {
				trace("string", obj);
			} else if (obj is int) {
				trace("int", obj);
			} else if (obj is Number) {
				trace("number", obj);
			} else {
				trace("unknown type:", obj);
			}
		}
		
		public static function RunTest(pi:* = 3.14):void
		{
			CheckType(pi);
			Assert.expectEq("pi == 3.14", true, pi == 3.14);
			Assert.expectEq("pi === 3.14", true, pi === 3.14);
			// TODO: Need to make string/object comparisons invoke the dynamic runtime
			//Assert.expectEq("pi == \"3.14\"", true, pi == "3.14");
			Assert.expectEq("pi !== \"3.14\"", true, pi !== "3.14");
		}
		
		public function RunTest2(pi = 3.14):void
		{
			CheckType(pi);
			RunTest(pi);
		}
		
		public function RunTest3(pi):void
		{
			RunTest2(pi);
		}
		
		public static function Main():int
		{
			// TODO: Default arguments aren't currently supported for dynamic types
			//RunTest();
			RunTest(3.14);
			var instance = new UntypedParameterTest();
			// TODO: Default arguments aren't currently supported for dynamic types
			//instance.RunTest2();
			instance.RunTest2(3.14);
			instance.RunTest3(3.14);
			return 0;
		}
	}
}

