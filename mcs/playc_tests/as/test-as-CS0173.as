// Compiler options: -psstrict-
package {

   public class Circle {
   }


   public class Square {
   }

    public class Test {
        public static function Main():int {
		var aa:Circle = new Circle();
		var ii:Square = new Square();

		// In ActionScript this is allowed (Conditional DoResolve in mcs/expression.cs):

		var o:* = ( 1 == 1 ) ? aa : ii;

		// Should not report this:

		// error CS0173: Type of conditional expression cannot be determined because there 
		// is no implicit conversion between `_root.Circle' and `_root.Square'

		return 0;
        }
    }
}
