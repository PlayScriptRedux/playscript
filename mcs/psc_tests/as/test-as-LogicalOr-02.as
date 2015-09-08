// Compiler options: -psstrict+
package {
    public class Foo {
        public static function Main():int {

		var a:Number = 10; 
		var b:Number = 250; 
		var start:Boolean = false; 
		if ((a > 25) || (b > 200) || (start)) { 
    			trace("the logical OR test passed"); // the logical OR test passed 
			return 0;
		}
		return 1;
        }
    }
}
