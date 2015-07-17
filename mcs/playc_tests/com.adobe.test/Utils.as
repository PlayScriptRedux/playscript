/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */
 
package com.adobe.test
{
    import Assert;
    public class Utils
    {
        public static var GLOBAL:String = "[object global]";
        public static var PASSED:String = " PASSED!";
        public static var FAILED:String = " FAILED! expected: ";
        public static var PACKAGELIST:String = "{public,$1$private}::";
        public static var ARGUMENTERROR:String = "ArgumentError: Error #";
        public static var TYPEERROR:String = "TypeError: Error #";
        public static var REFERENCEERROR:String = "ReferenceError: Error #";
        public static var RANGEERROR:String = "RangeError: Error #";
        public static var URIERROR:String = "URIError: Error #";
        public static var EVALERROR:String = "EvalError: Error #";
        public static var VERIFYERROR:String = "VerifyError: Error #";
        
        // Return the "Error #XXXX" String from the error
        public static function grabError(err:*, str:String):String
        {
            var typeIndex:int = str.indexOf("Error:");
            var type:String = str.substr(0, typeIndex + 5);
            if (type == "TypeError") {
                Assert.expectEq("Asserting for TypeError", true, (err is TypeError));
            } else if (type == "ArgumentError") {
                Assert.expectEq("Asserting for ArgumentError", true, (err is ArgumentError));
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
    
        /*
        * Functions that pull the error string.
        */
        public static function argumentError( str:String ):String {
            return str.slice(0,ARGUMENTERROR.length+4);
        }
        
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
        
        public static function parseError(errorStr:String, len:int):String
        {
            if (errorStr.length > len) {
                errorStr=errorStr.substring(0,len);
            }
            return errorStr;
        }
    }
    
    
}
