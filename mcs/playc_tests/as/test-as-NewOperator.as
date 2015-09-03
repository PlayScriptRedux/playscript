// Compiler options: -psstrict+
package {

	class Book {
		public var bName:String;
		public var bPrice:Number;
  
		public function Book(nameParam:String, priceParam:Number){
			bName = nameParam;
			bPrice = priceParam;
		}
	}

	public class Foo {
        	public static function Main():int {
			var book1:Book = new Book("Confederacy of Dunces", 19.95);
			var book2:Book = new Book("The Floating Opera", 10.95);
			trace(book1); // [object Book]
			if (book1.bName != "Confederacy of Dunces") return 1;
			if (book2.bPrice != 10.95) return 2;
			trace(book1); // [object Book]
			if (book1.toString() != "[object Book]") return 3;
			return 0;
        	}
    	}
}
