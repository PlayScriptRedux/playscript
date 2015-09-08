package {
    // Test defaulting to using indexers to access class fields/properties.

    public class Foo {

        public var x:Number = 100.0;

        private var _y:Number = 200.0;

        public function get y():Number {
            return _y;
        }

        public function set y(value:Number):void {
            _y = value;
        }
    }

    public class Test {
        public static function Main():int {
            var f:Foo = new Foo();

            // Normal property accessors should work
            f.x = 50.0;
            f.y = 100.0;

            trace(f.x);
            trace(f.y);

            // Accessing properties using indexer syntax should invoke properties via dynamic
            f["x"] = 20.0;
            f["y"] = 30.0;

            trace(f["x"]);
            trace(f["y"]);

            return 0;
        }
    }

}
