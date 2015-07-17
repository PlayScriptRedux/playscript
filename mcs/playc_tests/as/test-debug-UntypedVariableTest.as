// Compiler options: -psstrict- -r:./as/Assert.dll
package
{
	import com.adobe.test.Assert;
	import flash.utils.getQualifiedClassName;
	
	public class UntypedVariableTest
	{
		public static function Main():int
		{
            const A = "blah";
            const B = 3.14;
            var x = 2;
            var y = "9", z;

            Assert.expectEq("A == \"blah\"", true, A == "blah");
			Assert.expectEq("A === \"blah\"", true, A === "blah");
			Assert.expectEq("B == 3.14", true, B == 3.14);
			Assert.expectEq("B === 3.14", true, B === 3.14);
			Assert.expectEq("B === 3.14", true, B === 3.14);
			Assert.expectEq("x === 2", true, x === 2);
			Assert.expectEq("x == 2", true, x == 2);
			Assert.expectEq("y == \"9\"", true, y == "9");
			Assert.expectEq("y === \"9\"", true, y === "9");
			z = 10;
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
            return Assert.errorcount;
        }
	}
}
