package {

	// Issue #121 - Can't use 'int' and 'uint' as class

	public class Program {

		public static function Main():void {

			var o:Object = "blah";
			var s:String = o as String;

		}

	}


}
