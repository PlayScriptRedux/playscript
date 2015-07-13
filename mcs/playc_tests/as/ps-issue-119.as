package {

    // Issue #121 - Can't use 'int' and 'uint' as class

    public class Program {

        public var _i:int;
        public var _u:uint;

        public function foo(i:int):uint {
            return 0;
        }

        public static function Main():int {

            var i:int = 0;
            var u:uint = 100;

            // These assignments should work..
            var intClass:Class = int;
            var uintClass:Class = uint;

            // Allow class names in initializers..
            var a:Array = [Number, String, Object, int, uint];

            return 0;

        }

    }

}
