package
{
	import com.adobe.test.Assert;
	import flash.utils.getQualifiedClassName;
	
	public class UntypedVariableTest
	{
		public static const A = "blah";
		public static const B = 3.14;
		public var x = 2;
		public var y = "9", z;

		public static function Main():int
		{
			var instance:UntypedVariableTest = new UntypedVariableTest();
			instance.RunTest();
			return 0;
		}

		protected function RunTest():void
		{
			// TODO: warning CS0252: Possible unintended reference comparison. Consider casting the left side expression to type `string' to get value comparison
			Assert.expectEq("A == \"blah\"", true, A == "blah");
			Assert.expectEq("A === \"blah\"", true, A === "blah");
			Assert.expectEq("B == 3.14", true, B == 3.14);
			Assert.expectEq("B === 3.14", true, B === 3.14);
			Assert.expectEq("B === 3.14", true, B === 3.14);
			Assert.expectEq("x === 2", true, x === 2);
			Assert.expectEq("x == 2", true, x == 2);
			// TODO: warning CS0252: Possible unintended reference comparison. Consider casting the left side expression to type `string' to get value comparison
			Assert.expectEq("y == \"9\"", true, y == "9");
			Assert.expectEq("y === \"9\"", true, y === "9");
			z = 10;
			// TODO: warning CS0252: Possible unintended reference comparison. Consider casting the left side expression to type `string' to get value comparison
			Assert.expectEq("z == 10", true, z == 10);
			Assert.expectEq("z === 10", true, z === 10);

			for (var i = 0; i < 3; i++)
			{
				trace(i);
			}

			var chars = ["a", "b", 3, 4, null];
			for each (var char in chars)
			{
				trace(char);
			}

			try
			{
				throw new Error("blah");
			}
			catch (e)
			{
				trace(flash.utils.getQualifiedClassName(e));
			}
		}
	}
}
