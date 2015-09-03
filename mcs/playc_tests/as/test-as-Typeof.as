// Compiler options: -psstrict+
package {
    public class Foo {
        public static function Main():int {

		trace(typeof Array); // object
		trace(typeof Boolean); // boolean
		trace(typeof Function); // function
		trace(typeof int);
		trace(typeof Number);
		trace(typeof Object);
		trace(typeof String);
		trace(typeof uint);
		trace(typeof XML);
		trace(typeof XMLList);
		//trace(typeof *);

		if ((typeof Array) == "object") return 1;
		if ((typeof Boolean) == "boolean") return 2;
		if ((typeof Function) == "function") return 3;
		if ((typeof int) == "number") return 4;
		if ((typeof Number) == "number") return 5;
		if ((typeof Object) == "object") return 6;
		if ((typeof String) == "string") return 7;
		if ((typeof uint) == "number") return 8;
		if ((typeof XML) == "xml") return 9;
		if ((typeof XMLList) == "xml") return 10;
		//if ((typeof *) == "undefined") return 11;

		return 0;
        }
    }
}
