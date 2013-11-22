package
{
	import System.*;

	class TestData
	{
		public static var testsFailed:int = 0;
	}

	// prints out a test failure
	public function testFailure(frameIndex:int, exception:Exception):void 
	{
		TestData.testsFailed++;

		// get stack frame and line number
		var stackTrace = (exception != null) ? new System.Diagnostics.StackTrace(exception, true) : new System.Diagnostics.StackTrace(true);
		var stackFrame = stackTrace.GetFrames()[frameIndex];  
		var lineNumber = stackFrame.GetFileLineNumber();
		var fileName = stackFrame.GetFileName();
		var module = System.IO.Path.GetFileNameWithoutExtension(fileName);
		var line:String = "<unknown>";
		if ((fileName != null) && System.IO.File.Exists(fileName)) {
			var lines = System.IO.File.ReadAllLines(fileName);
			if (lineNumber >= 0 && lineNumber < lines.Length) {
				line = lines[lineNumber-1].Trim();
			}
		} 
		trace("Test Failed: " + module + " Line(" + lineNumber.ToString("00") + ") " + line );
		if (exception != null) {
			trace("\tException: " + exception.Message);
		}
	}

	// test a condition and prints out a failure message
	public function test(condition:Boolean):void
	{
		if (!condition) {
			testFailure(2, null);
		}
	}

	// does a test run and catches exceptions
	public function testRun(func:Action):int
	{
		var result:Boolean = false;
		try
		{
			func();
		} catch (e:Exception)
		{
			testFailure(0, e);
		}

		if (TestData.testsFailed > 0) {
			trace("Tests Complete Failed:" + TestData.testsFailed);
		} else {
			trace("Tests Complete All Passed");
		}

		return TestData.testsFailed;
	}

}
