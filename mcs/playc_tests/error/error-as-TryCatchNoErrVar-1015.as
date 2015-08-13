// Compiler options: -psstrict+
//
// error CS1015: A type that derives from `System.Exception', `object', or `string' expected
//
package {

    public class Foo {

        public static function Main():int {
		try { 
		    // some code that could throw an error 
		} 
		catch () { // Error CS1015 
		    // code to react to the error 
		} 
		return 0;
        }
    }
}
