// Compiler options: -newdynamic+
package {
    // Test embed support..

    public class Test {
        [Embed(source="Makefile")]
        public static var data:Class;

        public static function Main():int {
            //var d:Object = new data();
            return 0;
        }

    }
}
