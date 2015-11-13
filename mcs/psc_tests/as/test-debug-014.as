package {

    // Test to ensure dynamic boxed cast of null literal is not generating invalid IL code
    public class Test {

        public static function foo(o:Array):void {
//			trace(o);

            var c:Class = Object(o).constructor;

            var testInt:Object = 5;
            var testNumber:Number = Number(testInt);
        }

        public static function Main():int {
//			foo([{"type": "smasher", "quantity":1}]);
            foo([100, 200, 300]);

            return 0;
        }
    }

}

