package {

    import System.Reflection.*;

    public class Foo {

        public static function a():void {
            trace("static Foo:a()");
        }

        public function a():void {
            trace("Foo:a()");
        }

        public function get q():int {
            trace("Foo:q get");
            return 100;
        }

        public function set q(value:int):void {
            trace("Foo:q set");
        }

        public static function get q():int {
            trace("static Foo:q get");
            return 100;
        }

        public static function set q(value:int):void {
            trace("static Foo:q set");
        }

    }

    public class Bar {

        public static function b():void {
            trace("static Bar:b()");
        }

        public function b():void {
            trace("Bar:b()");
        }

    }

    public class Foo {

        public static function Main():int {
            var f:Foo = new Foo();
            var b:Bar = new Bar();

            Foo.a();
            f.a();
            var i:int;
            i = f.q;
            f.q = i;
            i = Foo.q;
            Foo.q = i;

            var foo_m = f.GetType().GetMethod("a", BindingFlags.Public | BindingFlags.Instance);
            foo_m = f.GetType().GetMethod("a", BindingFlags.Public | BindingFlags.Static);

            Bar.b();
            b.b();

            return 0;
        }
    }

}
