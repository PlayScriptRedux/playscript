package {

    // Test dynamic class support..
    public dynamic class Foo {
        public function Foo() {
        }

        public function bar(i:int):void {
            trace("Hello!");
        }

    }

    // This shouldn't implement IDynamicClass again
    public class Bar extends Foo {
    }

    // This ALSO shouldn't implement IDynamicClass again
    public dynamic class Bar2 extends Foo {
    }

    public class Test {
        public static function Main():int {

            var f:Foo = new Foo();
            f.bar(100);

            f["blah"] = 100;
            trace(f["blah"]);
            var i:int = f["blah"];

            f.blah = "aaaa";
            trace(f.blah);
            var s:String = f.blah;

            var b:Bar = new Bar();
            b["blah"] = 100;
            b.blah = 100;

            var b2:Bar2 = new Bar2();
            b2["blah"] = 100;
            b2.blah = 100;

            return 0;
        }

    }

}
