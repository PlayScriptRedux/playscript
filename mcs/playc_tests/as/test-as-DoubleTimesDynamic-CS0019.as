// Compiler options: -psstrict-
package {
    public class Foo {
        public static function Main():int {
		var arr:Array = [1.0, 1.5, 2.0];
		trace(arr[2]);
		trace(Math.PI);

		// Should not produce:

		// error CS0019: Operator `*' cannot be applied to 
		// operands of type `double' and `dynamic'
			
		var foo = Math.PI * arr[2];
		trace(foo);		
		return 0;
        }
    }
}
