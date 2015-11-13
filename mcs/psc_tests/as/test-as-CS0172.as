// Compiler options: -psstrict+
package {
    public class Foo {
        public static function Main():int {
		var val:uint = 1;
		var num:uint = 1;
		var i:uint;

		// The folliwing SHOULD NOT report this:

		// error CS0172: Type of conditional expression cannot be determined 
		// as `uint' and `int' convert implicitly to each other

		// but should in C#

		i = ( 1 == 1) ? num : val ;  // not a problem
		trace(i); // 1
		i = ( 1 == 1) ? num : val == val ? val : 0 ; // error CS0172 
		trace(i); // 1
		i = ( 1 == 1) ? num - 1 : (val > 0) ? val : 0; // error CS0172
		trace(i); // 0
		return 0;
        }
    }
}
