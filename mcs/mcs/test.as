package {

	public class Test {

		public static function throwException():void {
			throw new System.Exception("Blah blah");
		}

		public static function throwError():void {
			throw new Error("Blah blah");
		}

		public static function Main():void {

			trace("Testing error handling!");

			try { 
				throwException();
			} catch (e1:Error) {
				trace(e1.message);
			}

			try {
				throwError();
			} catch (e2:Error) {
				trace(e2.message);
			}
		}
	}

}
