// Compiler options: -psstrict-
package {
    public class Foo {
        public static function Main():int {

		// Both return true because no conversion is done 
		var string1:String = "5"; 
		var string2:String = "5"; 

		trace(string1 == string2); // true 
		if (string1 != string2) return 1;

		trace(string1 === string2); // true 
		if (string1 !== string2) return 2;

		// Automatic data typing in this example converts 5 to "5" 
		var string1:String = "5"; 
		var num:Number = 5; 

		trace(string1 == num); // true
		if (string1 != num) return 3;

		trace(string1 === num); // false 
		if (string1 === num) return 4;

		// Automatic data typing in this example converts true to "1" 
		var string1:String = "1"; 
		var bool1:Boolean = true; 

		trace(string1 == bool1); // true 
		if (string1 != bool1) return 5;

		trace(string1 === bool1); // false 
		if (string1 === bool1) return 6;

		// Automatic data typing in this example converts false to "0" 
		var string1:String = "0"; 
		var bool2:Boolean = false; 

		trace(string1 == bool2); // true 
		if (string1 != bool2) return 7;

		trace(string1 === bool2); // false 
		if (string1 === bool2) return 8;

		return 0;
        }
    }
}
