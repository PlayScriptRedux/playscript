// Compiler options: -psstrict+
package {

    public class Foo {

        public static function Main():int {
		try { 
		    // some code that could throw an error 
		} 
		finally { 
		    // Code that runs whether an error was thrown. This code can clean 
		    // up after the error, or take steps to keep the application running. 
		}
		return 0;
        }
    }
}
