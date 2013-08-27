package
{
	import com.adobe.test.Assert;
	
	public class UntypedReturnTypeTest
	{
		public static const A = "blah";
		public static const B = 3.14;

		public static function Main():int
		{
			// TODO: warning CS0252: Possible unintended reference comparison. Consider casting the left side expression to type `string' to get value comparison
			Assert.expectEq("A == \"blah\"", true, A == "blah");
			Assert.expectEq("A === \"blah\"", true, A === "blah");
			// TODO: error CS0019: Operator `==' cannot be applied to operands of type `object' and `double'
			//Assert.expectEq("B == 3.14", true, B == 3.14);
			Assert.expectEq("B === 3.14", true, B === 3.14);
			return 0;
		}
	}
}

