package DivideByZero
{

    /*
     In the preceding example, you might think that attempting to divide by 0 (when y is 0) would cause
     ActionScript itself to throw a "Division by zero" exception, but no such luck.
     ActionScript doesn't throw exceptions. It is up to the developer to check for error conditions
     and invoke throw as desired. Furthermore, in ActionScript, dividing anything
     other than 0 by 0 yields Infinity (for positive numerators) or -Infinity (for negative numerators).
     Fron : http://www.actionscript.org/resources/articles/603/2/Exceptions-and-Exception-Handling/Page2.html
     */
    public class CollectionsTest {
        public static function Main():int {
            var x:Number = 0;
            var y:Number = 0;
            Assert.expectEq("0/0; Quotient is NaN", true IsNan(x / y));
            Assert.expectEq("0/0; Quotient is NaN", true IsNan(x % y));
            return Assert.errcount;
        }
    }
}