package {
    // Test variadic function and 'arguments' array.

    public class Foo {
        // No array type
        public function bar1(i:int, ...args):void {
            trace(args.length);
            args.pop();
        }

        // Following array type
        public function bar2(i:int, ...args:Array):void {
            trace(i, args);
        }

        // Implicit "arguments"
        public function bar3(i:int, s:String, n:Number, b:Boolean):Array {
            return arguments;
        }
    }

    public class Test {

        public static function anonVariadic():void {

            // Function with variadic argumetns should work
            var f:Function = function (...) {
                trace(arguments);
            }
        }

        public static function Main():int {
            var f:Foo = new Foo();
            f.bar1(100, "a", 123.45, true);
            f.bar2(200, "b", 234.56, false);
            var args:Array = f.bar3(300, "c", 345.67, true);
            trace(args);

            return 0;
        }
    }

}
