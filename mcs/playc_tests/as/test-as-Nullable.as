// Compiler options: -psstrict-
package {
    public class Foo {

	public static var target:Foo = null;
	public static var pass:Boolean = true;
	public static var fail:Boolean = false;
	public static var fail2:Boolean = false;

        public static function Main():int {
		trace(target);
		trace(pass);
		trace(fail);
		if ((fail || fail2) && pass)
			return 9;
		if ((!pass) && fail)
			return 1;		
		if ((target || !pass) && fail)
			return 2;		
		if ((target != null) || !pass)
			return 3;
		if ((target) || fail)
			return 4;
		if (((target == null) || pass) && fail)
			return 5;
		if (((target) && pass) && fail)
			return 6;
		return 0;
        }
    }
}
