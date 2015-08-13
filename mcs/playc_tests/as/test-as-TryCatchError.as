// Compiler options: -psstrict+
package {

    public class Foo {

        public static function Main():int {
		try { 
			var localErr:Error = new Error("NewError", 9);
			throw localErr;
		} 
		catch (err:Error) { 
			// Test if this is an ActionScript error class
			if (err.message != "NewError") {
				return 1;
			}
			if (err.errorID != 9) {
				return 2;
			}
			trace(err.message);
			trace(err.errorID);
		} 
		return 0;
        }
    }
}
