package {
    // Test expressions.

    class Foo {
        public function Foo() {
            cl = Foo;
            f = this;
        }

        public var cl:Class = Foo;
        public var f:Foo;
    }

    public class Test extends Foo {

        public function test():int {
            // Test basic numeric operators.
            var i:int = 0;
            i = i + i;
            i = i - i;

            // TODO: System.DivideByZeroException: Division by zero
            // Moved to separate test : test-debug-DivideByZeroTest.as
            // i = i / i;

            i = i * i;
            i = i << i;
            i = i >>> i;
            i = i >> i;

            // Moved to separate test : test-debug-DivideByZeroTest.as
            // i = i % i;

            i = i | i;
            i = i & i;
            i = i ^ i;
            i = ~i;

            // Test bool operators.
            var b:Boolean = false;
            b = !b;
            b = b && b;
            b = b || b;
            b = b == b;
            b = b != b;
            b = i < i;
            b = i > i;
            b = i <= i;
            b = i >= i;

            // Test new expressions.

            var f:Foo = new Foo();
            var f2:Foo;

            f2 = new Foo();
            f2 = new Foo;
            f2 = new cl();
            f2 = new this.cl();
            f2 = new this.f.f.cl;
            f2 = new super.f.cl();
            f2 = new super.f.f.cl();

            return i;
        }

        public static function Main():int {

            var t:Test = new Test();
            t.test();
            trace("Hello")

            return 0;
        }

    }

}
