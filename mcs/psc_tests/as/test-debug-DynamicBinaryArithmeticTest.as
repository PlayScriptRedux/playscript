// Compiler options: -r:./as/Assert.dll
package
{
    public class DynamicBinaryArithmeticTest
    {
        public static function Main():int
        {
			if ("1" + null != "1null")
				return 1;
			var o:Object = "2";
			if (o + null != "2null")
				return 2;
			if (parseInt(o) + null != 2)
				return 3;
			if ("1" + undefined != "1undefined")
				return 4;
			if (o + undefined != "2undefined")
				return 5;
			if (!isNaN(parseInt(o) + undefined))
				return 6;
			return 0;
		}
	}
}
