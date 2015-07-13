package {
// Test local variable hoisting.

    public class Test {

        public static function Main():int {

            // This should generate a warning, but work.
            i = 100;

            {
                {
                    {
                        // This should be hoisted to top block.
                        var i:int = 100;
                    }
                }
            }

            // This should generate a warning, but work.
            var i:int = 200;

            // These two declarations should compile, and be the same variable.
            try {
            } catch (e:Error) {
                trace("err1");
            }
            try {
            } catch (e:Error) {
                trace("err2");
            }

            // The var "a" should be set to two different values but be the same variable.
            {
                var a:int = 1;
                trace(a);       // prints 1
                if (a != 1) {
                    return 1;
                }
            }
            {
                var a:int = 2;
                trace(a);       // should print 2
                if (a != 2) {
                    return 1;
                }
            }
            return 0;

        }

    }

}
