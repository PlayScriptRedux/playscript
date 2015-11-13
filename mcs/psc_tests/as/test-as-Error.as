// Compiler options: -psstrict+
package {

    public class Foo {

        public static function Main():int {
		var err:Error;
		var newError:Error = new Error();
		err = newError;
		if (err != newError)
			return 1;
		return 0;
        }
    }
}
