// Compiler options: 
package
{
    public class DynamicBinaryArithmeticTest
    {
        public static function Main():int
        {
                        if ("1" + null != "1null")
                                return 1;
                        if (null + "1" + null != "null1null")
                                return 2;
                        if (null + "1" != "null1")
                                return 1;
                        return 0;
                }
        }
}

