//import flash.system.Capabilities;

package {

import flash.system.Capabilities;
	public class Test {

		public static var playerType:String = Capabilities.playerType;

		public static var completed:Boolean =	false;
		public static var testcases:Vector.<TestCase>; 
		public static var tc:int = 0;
		//var version; // ISSUE:: need to know why we need to comment this out

		public static var SECTION:String = "";
		public static var VERSION:String = "";
		public static var BUGNUMBER:String =	"";
		public static var TITLE:String = "";
		public static var STATUS:String = "STATUS: ";

		//  constant strings

		public static var GLOBAL:String = "[object global]";
		public static var PASSED:String = " PASSED!";
		public static var FAILED:String = " FAILED! expected: ";
		public static var PACKAGELIST:String = "{public,$1$private}::";
		public static var TYPEERROR:String = "TypeError: Error #";
		public static var REFERENCEERROR:String = "ReferenceError: Error #";
		public static var RANGEERROR:String = "RangeError: Error #";
		public static var URIERROR:String = "URIError: Error #";
		public static var EVALERROR:String = "EvalError: Error #";
		public static var VERIFYERROR:String = "VerifyError: Error #";
		public static var VERBOSE:Boolean = true;

		public static var DEBUG:Boolean =	false;

		public static function typeError( str:String ):String {
			return str.slice(0,TYPEERROR.length+4);
		}

		public static function referenceError( str:String ):String {
			return str.slice(0,REFERENCEERROR.length+4);
		}

		public static function rangeError( str:String ):String {
			return str.slice(0,RANGEERROR.length+4);
		}

		public static function uriError( str:String ):String {
			return str.slice(0,URIERROR.length+4);
		}

		public static function evalError( str:String ):String {
			return str.slice(0,EVALERROR.length+4);
		}

		public static function verifyError( str:String ):String {
			return str.slice(0,VERIFYERROR.length+4);
		}

		// wrapper for test cas constructor that doesn't require the SECTION
		//  argument.


		public static function AddTestCase( description:String, expect:Object, actual:Object, skip:Boolean = false ):void {
		    testcases[tc++] = new TestCase( SECTION, description, expect, actual, skip );
		}


		//  Set up test environment.


		public static function startTest():void {
		    // print out bugnumber
		    /*if ( BUGNUMBER ) {
		            writeLineToLog ("BUGNUMBER: " + BUGNUMBER );
		    }*/

		    testcases = new Vector.<TestCase>();
		    tc = 0;
		}

		public static function checkReason(passed:String):String {
		    var reason:String;
		    if (passed == 'true') {
				reason = "";
		    } else if (passed == 'false') {
				reason = "wrong value";
		    } else if (passed == 'type error') {
				reason = "type error";
		    }
		    return reason;
		}


		public static function test(... rest:Array):Array {

			if( rest.length == 0 ){
				// no args sent, use default test
		    	for ( tc=0; tc < testcases.length; tc++ ) {
		        	testcases[tc].passed = writeTestCaseResult(
		                    testcases[tc].expect,
		                    testcases[tc].actual,
		                    testcases[tc].description +" = "+ testcases[tc].actual );
				    testcases[tc].reason += checkReason(testcases[tc].passed);
		    	}
			} else {
				// we need to use a specialized call to writeTestCaseResult
				if( rest[0] == "no actual" ){
		    		for ( tc=0; tc < testcases.length; tc++ ) {
		        		testcases[tc].passed = writeTestCaseResult(
		                            		testcases[tc].expect,
		                            		testcases[tc].actual,
		                            		testcases[tc].description );
		        		testcases[tc].reason += checkReason(testcases[tc].passed);
		    		}
				}
			}
		    stopTest();
		    return ( testcases );
		}


		//  Compare expected result to the actual result and figure out whether
		//  the test case passed.

		public static function getTestCaseResult(expect:*,actual:*):String {
			//	because	( NaN == NaN ) always returns false, need to do
			//	a special compare to see if	we got the right result.
			if ( actual	!= actual )	{
				if ( typeof	actual == "object" ) {
					actual = "NaN object";
				} else {
					actual = "NaN number";
				}
			}
			if ( expect	!= expect )	{
				if ( typeof	expect == "object" ) {
					expect = "NaN object";
				} else {
					expect = "NaN number";
				}
			}
			var passed="";
			if (expect == actual) {
			    if ( typeof(expect) != typeof(actual) ){
				passed = "type error";
			    } else {
				passed = "true";
			    }
			} else { //expect != actual
			    passed = "false";
			    // if both objects are numbers
			    // need	to replace w/ IEEE standard	for	rounding
			    if (typeof(actual) == "number" && typeof(expect) == "number") {
				if ( Math.abs(actual-expect) < 0.0000001 ) {
				    passed = "true";
				}
			    }
			}
			return passed;
		}

		//  Begin printing functions.  These functions use the shell's
		//  print function.  When running tests in the browser, these
		//  functions, override these functions with functions that use
		//  document.write.

		public static function writeTestCaseResult( expect:String, actual:String, desc:String ):String {
		    var passed:String = getTestCaseResult(expect,actual);
			var s:String = desc;
		    if (passed == "true") {
		        s = PASSED + " " + s;
		    } else if (passed == "false") {
		        s = FAILED + expect + " " + s;
		    } else if (passed == "type error") {
		        s = FAILED + expect + " Type Mismatch - Expected Type: "+ typeof(expect) + ", Result Type: "+ typeof(actual) + " " + s;
		    } else { //should never happen
				s = FAILED + " UNEXPECTED ERROR - see shell.as:writeTestCaseResult() " + s;
		    }

	        writeLineToLog(s);

		    return passed;
		}

		public static function writeLineToLog( string:String ):void {
			_print( string );
		}

		public static function writeHeaderToLog( string:String ):void	{
			_print( string );
		}

		// end of print functions

		//  When running in the shell, run the garbage collector after the
		//  test has completed.

		public static function stopTest():void {
		}

		public static function results():int {
			var totalPassed:int = 0;
			var totalFailed:int = 0;
			var totalSkipped:int = 0;

			for each (var testCase:TestCase in testcases) {
				if (testCase.passed != "true") {
					totalFailed++;
				} else {
					totalPassed++;
				}
				if (testCase.skip) {
					totalSkipped++;
				}
			}

			writeLineToLog("Passed: " + totalPassed + " Failed: " + totalFailed + " Skipped: " + totalSkipped + "\n");

			return totalFailed > 0 ? 1 : 0;
		}

		public static function START(summary:String):void
		{
		      // print out bugnumber

		     /*if ( BUGNUMBER ) {
		              writeLineToLog ("BUGNUMBER: " + BUGNUMBER );
		      }*/
//		    XML.setSettings (null);
		    testcases = new Vector.<TestCase>();

		    // text field for results
		    tc = 0;
		    /*this.addChild ( tf );
		    tf.x = 30;
		    tf.y = 50;
		    tf.width = 200;
		    tf.height = 400;*/

		    _print(summary);
		    var summaryParts = summary.split(" ");
		    _print("section: " + summaryParts[0] + "!");
		    //fileName = summaryParts[0];

		}

		public static function BUG(bug:Number):void
		{
		  printBugNumber(bug);
		}

		public static function reportFailure (section:String, msg:String = ""):void
		{
		    _print(FAILED + inSection(section)+"\n"+msg);
		    /*var lines = msg.split ("\n");
		    for (var i=0; i<lines.length; i++)
		        print(FAILED + lines[i]);
		    */
		}

		public static function TEST(section:String, expected:*, actual:*):void
		{
		    AddTestCase(section, expected, actual);
		}

		public static function myGetNamespace (obj:Object, ns:*):String {
		    if (ns != undefined) {
		        return obj.namespace(ns);
		    } else {
		        return obj.namespace();
		    }
		}

		public static function TEST_XML(section:String, expected:*, actual:*):void
		{
		  var actual_t:String = typeof actual;
		  var expected_t:String = typeof expected;

		  if (actual_t != "xml") {
		    // force error on type mismatch
		    TEST(section, new XML(), actual);
		    return;
		  }

		  if (expected_t == "string") {

		    TEST(section, expected, actual.toXMLString());
		  } else if (expected_t == "number") {

		    TEST(section, String(expected), actual.toXMLString());
		  } else {
		    reportFailure ("Bad TEST_XML usage: type of expected is "+expected_t+", should be number or string");
		  }
		}

		public static function SHOULD_THROW(section:String):void
		{
		  reportFailure(section, "Expected to generate exception, actual behavior: no exception was thrown");
		}

		public static function END():void
		{
			test();
		}

		public static function NL():String
		{
		  //return java.lang.System.getProperty("line.separator");
		  return "\n";
		}

		public static function printBugNumber (num:Number):void
		{
		  //writeLineToLog (BUGNUMBER + num);
		}

		public static function toPrinted(value:*):String
		{
		  if (typeof value == "xml") {
		    return value.toXMLString();
		  } else {
		    return String(value);
		  }
		}

		public static function grabError(err:Error, str:String):String {
			var typeIndex = str.indexOf("Error:");
			var type = str.substr(0, typeIndex + 5);
			if (type == "TypeError") {
				AddTestCase("Asserting for TypeError", true, (err is TypeError));
			} else if (type == "ArgumentError") {
				AddTestCase("Asserting for ArgumentError", true, (err is ArgumentError));
			}
			var numIndex:int = str.indexOf("Error #");
			var num:String;
			if (numIndex >= 0) {
				num = str.substr(numIndex, 11);
			} else {
				num = str;
			}
			return num;
		}

		public static var cnNoObject:String = 'Unexpected Error!!! Parameter to this function must be an object';
		public static var cnNoClass:String = 'Unexpected Error!!! Cannot find Class property';

		// checks that it's safe to call findType()
		public static function getJSType(obj:Object):String
		{
		  if (isObject(obj))
		    return findType(obj);
		  return cnNoObject;
		}


		// checks that it's safe to call findType()
		public static function getJSClass(obj:Object):String
		{
			throw new System.NotImplementedException();
//		  if (isObject(obj))
//		    return findClass(findType(obj));
//		  return cnNoObject;
		}

		public static function isObject(obj:Object):Boolean
		{
			throw new System.NotImplementedException();
//		  return obj instanceof Object;
		}

		public static function findType(obj:Object):String
		{
			throw new System.NotImplementedException();
//		  return cnObjectToString.apply(obj);
		}

		// given '[object Number]',  return 'Number'
		public static function findClass(sType:Class):String
		{
			throw new System.NotImplementedException();
//		  var re =  /^\[.*\s+(\w+)\s*\]$/;
//		  var a = sType.match(re);

//		  if (a && a[1])
//		    return a[1];
//		  return cnNoClass;
		}

		public static function inSection(x:String):String {
		   return "Section "+x+" of test -";
		}

		public static function printStatus (msg:String):void
		{
		    var lines:Array = msg.split ("\n");

		    for (var i:int=0; i<lines.length; i++)
		        _print(STATUS + lines[i]);

		}

		public static function reportCompare (expected:*, actual:*, description:String):void
		{
		    var expected_t = typeof expected;
		    var actual_t = typeof actual;
		    var output = "";
		   	if ((VERBOSE) && (typeof description != "undefined"))
		            printStatus ("Comparing '" + description + "'");

		    if (expected_t != actual_t)
		            output += "Type mismatch, expected type " + expected_t +
		                ", actual type " + actual_t + "\n";
		    else if (VERBOSE)
		            printStatus ("Expected type '" + actual_t + "' matched actual " +
		                         "type '" + expected_t + "'");

		    if (expected != actual)
		            output += "Expected value '" + expected + "', Actual value '" + actual +
		                "'\n";
		    else if (VERBOSE)
		            printStatus ("Expected value '" + actual + "' matched actual " +
		                         "value '" + expected + "'");

		    if (output != "")
		        {
		            if (typeof description != "undefined")
		                reportFailure (description);
		            	reportFailure (output);
		        }
		    stopTest();
		}

		// encapsulate output in shell
		public static function _print(s:String):void {
		  trace(s);
		}

		// workaround for Debugger vm where error contains more details
		public static function parseError(error:String, len:int):String {
		  if (error.length>len) {
		    error=error.substring(0,len);
		  }
		  return error;
		}

		// helper function for api versioning tests
		public static function versionTest(testFunc:Function, desc:String, expected:*):void {
		   var result:*;
		   try {
		       result = testFunc();
		   } catch (e:Error) {
		       // Get the error type and code, but not desc if its a debug build
		       result = e.toString().substring(0,e.toString().indexOf(':')+13);
		   }
		   AddTestCase(desc, expected, result);
		}
	}

}

class TestCase {

	public var name:String;
	public var description:String;
	public var expect:*;
	public var actual:*;
	public var skip:Boolean;
	public var passed:String;
	public var reason:String;

	public function TestCase( n:String, d:String, e:*, a:*, s:Boolean ) {
		this.name		 = n;
		this.description = d;
		this.expect		 = e;
		this.actual		 = a;
		this.skip		 = s;
		this.passed		 = "";
		this.reason		 = "";
		//this.bugnumber	  =	BUGNUMBER;

		if (!skip) {
			this.passed	= Test.getTestCaseResult( this.expect, this.actual );
			if ( Test.DEBUG ) {
				Test.writeLineToLog(	"added " + this.description	);
			}
		} else {
			this.passed = "true";
		}
	}

}

