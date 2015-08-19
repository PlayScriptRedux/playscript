// Compiler Options: -psstrict-
package {

    public class Flow {
        public static function Main():int {
		var o:A = new A();

		// Does not become a AsNonAssignStatementExpression
		var i:int = o.B;
		if (i != 99) return 1;
		var x = o.B;
		if (x != 99) return 2;

		// Legal in ActionScript
		// Becomes AsNonAssignStatementExpression
		o.B;

		if (o.refCount != 3) return 99;
                return 0;
        }
    }

}

class A {
	public var refCount:int = 0;
	public function get B():int {
		trace("inside A.B getter");
		refCount++;
		return 99;
	}
}

