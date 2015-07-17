// Compiler options: -psstrict- -r:./as/Assert.dll
package {
    // Test variadic function and 'arguments' array.

    public class Foo {
        // No array type
        public function bar1(i:int, ...args):void {
            trace(args.length);
            args.pop();
        }

        function traceArgArray(x: int, ... args):void
        {
            for (var i:uint = 0; i < args.length; i++)
            {
                trace(args[i]);
            }
        }

        // Following array type
        public function bar2(i:int, ...args:Array):void {
            trace(i, args);
        }

        // See CS7009-MagicArgumentsVariable.as / Magic arguments var is not support
//        // Implicit "arguments"
//        public function bar3(i:int, s:String, n:Number, b:Boolean):Array {
//            return arguments;
//        }
    }

    public class Test {

//        public static function anonVariadic():void {
//
//            // Function with variadic arguments should work
//            var f:Function = function (... arguments) {
//                trace(arguments);
//            }
//        }

        public static function Main():int {
            var f:Foo = new Foo();
            f.bar1(100, "a", 123.45, true);
            f.bar2(200, "b", 234.56, false);

            return 0;
        }
    }

}
