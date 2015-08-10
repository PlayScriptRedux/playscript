// Compiler options: -psstrict+
package {

    public class Foo {

        public static function Main():void {
		try { 
		    // some code that could throw an error 
		} 
		catch (err:Error) { 
		    // code to react to the error 
			trace(err.message);
		} 
        }
    }
}
