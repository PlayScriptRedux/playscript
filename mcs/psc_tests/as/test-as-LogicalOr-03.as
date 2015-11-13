// Compiler options: -psstrict-
package {
    public class Foo {

	static var fail:Boolean = false;

	static function fx1():Boolean { 
		trace("fx1 called"); 
		return true; 
	}
 
	static function fx2():Boolean { 
		trace("fx2 called"); 
		fail = true;
		return true; 
	} 

	// The following example demonstrates how using a function call as 
	// the second operand can lead to unexpected results. If the expression 
	// on the left of the operator evaluates to true, that result is returned 
	// without evaluating the expression on the right (the function fx2() 
	// is not called).

        public static function Main():int {
		if (fx1() || fx2()) { 
			trace("IF statement entered");
			if (fail) return 1;
		}
		return 0;
        }
    }
}
