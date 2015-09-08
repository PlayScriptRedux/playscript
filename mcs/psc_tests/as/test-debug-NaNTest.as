// Compiler options: -r:./as/Assert.dll 
package
{
    import com.adobe.test.Assert;

    public class NaNTest
    {
        public static function Main():int
        {
			var a:Number = NaN;
			Assert.expectEq("a != a", true, a != a);
			Assert.expectEq("a == a", false, a == a);
			var b:Object = NaN;
			Assert.expectEq("b != b", true, b != b);
			Assert.expectEq("b == b", false, b == b);
			return 0;
		}
	}
}
