// Compiler options: -psstrict+
package {
    public class Foo {
        public static function Main():int {

		var object:Object = {}; 
		var object:Object = new Object(); 

		var x:int = 0;
		var account:Object = {name:"Adobe Systems, Inc.", address:"601 Townsend Street", city:"San Francisco", state:"California", zip:"94103", balance:"1000"};

		for (var i:* in account) {
			trace("account."+i+" = "+account[i]);

			x++;
			if (x == 6) {
				if (i != "balance") return 1;
				if (account[i] != "1000") return 2;
			}
		}

		return 0;
        }
    }
}
