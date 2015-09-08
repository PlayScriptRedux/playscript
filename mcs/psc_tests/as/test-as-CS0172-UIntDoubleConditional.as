// Compiler options: -psstrict+
package {
    public class Foo {
        public static function Main():int {
		var val:uint = 1;
		var num:Number = 1.2345;
		var i:int;

		// The folliwing SHOULD NOT report this:

		// error CS0172: Type of conditional expression cannot be determined 
		// as `uint' and `int' convert implicitly to each other

		// but should in C#

		var y:Number = ( num < 1.987 ) ? val : num;
		var z:Number = ( num < 1 ) ? val : val * num;

		return 0;
        }
    }
}
