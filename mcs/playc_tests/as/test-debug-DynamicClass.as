//
package Sushi {

	dynamic class Protean 
	{ 
		private var privateGreeting:String = "hi"; 
		public var publicGreeting:String = "hello"; 
		public function Protean() 
		{ 
			trace( privateGreeting, "Protean instance created" ); 
		} 
	}

	public class Program {

		public static function Main():int {

			var myProtean:Protean = new Protean();
			trace(myProtean.publicGreeting);
			myProtean.aString = "testing"; 
			myProtean.aNumber = 3; 
			trace(myProtean.aString, myProtean.aNumber); // testing 3
			return 0;
		}
	}
}
