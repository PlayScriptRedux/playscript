// Compiler options: -psstrict+
package {

	public class AppError extends Error 
	{ 
    		public function AppError(message:String, errorID:int) 
		{		 
			super(message, errorID); 
		} 
	}

	public class Foo {

		public static function Main():int {

			try { 
				throw new AppError("Custom", 99); 
			} 
			catch (error:AppError) { 
				trace(error.errorID + ": " + error.message);
				if (error.errorID != 99) 
					return 1;
			}
			return 0;
		}
	}
}
