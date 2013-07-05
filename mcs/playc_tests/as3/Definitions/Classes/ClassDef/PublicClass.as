/* ***** BEGIN LICENSE BLOCK *****
 * Version: MPL 1.1/GPL 2.0/LGPL 2.1
 *
 * The contents of this file are subject to the Mozilla Public License Version
 * 1.1 (the "License"); you may not use this file except in compliance with
 * the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 *
 * Software distributed under the License is distributed on an "AS IS" basis,
 * WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License
 * for the specific language governing rights and limitations under the
 * License.
 *
 * The Original Code is [Open Source Virtual Machine.].
 *
 * The Initial Developer of the Original Code is
 * Adobe System Incorporated.
 * Portions created by the Initial Developer are Copyright (C) 2005-2006
 * the Initial Developer. All Rights Reserved.
 *
 * Contributor(s):
 *   Adobe AS3 Team
 *
 * Alternatively, the contents of this file may be used under the terms of
 * either the GNU General Public License Version 2 or later (the "GPL"), or
 * the GNU Lesser General Public License Version 2.1 or later (the "LGPL"),
 * in which case the provisions of the GPL or the LGPL are applicable instead
 * of those above. If you wish to allow use of your version of this file only
 * under the terms of either the GPL or the LGPL, and not to allow others to
 * use your version of this file under the terms of the MPL, indicate your
 * decision by deleting the provisions above and replace them with the notice
 * and other provisions required by the GPL or the LGPL. If you do not delete
 * the provisions above, a recipient may use your version of this file under
 * the terms of any one of the MPL, the GPL or the LGPL.
 *
 * ***** END LICENSE BLOCK ***** */

package {

	import PublicClassPackage.*;

	public class PublicClassTests {

		public static function test():void {

			T.SECTION = "Definitions";           // provide a document reference (ie, ECMA section)
			T.VERSION = "AS3";                   // Version of JavaScript or ECMA
			T.TITLE   = "Access Class Properties & Methods";  // Provide ECMA section title or a description
			T.BUGNUMBER = "";

			var arr:Array = new Array(1,2,3);
			var arr2:Array = new Array(3,2,1);
			var Obj:Object = new PublicClass();
			var d:Date = new Date(0);
			var d2:Date = new Date(1);
			var f:Function = function():void {};
			var str:String = "Test";
			var ob:Object = new Object();
			var foo:Object;

			T.startTest();

			// ********************************************
			// Basic Constructor tests
			// ********************************************
			T.AddTestCase( "*** No param constructor test ***", 1, 1 );
			foo = new PublicClass();
			T.AddTestCase( "var foo = new PublicClass(), foo.constructorCount", 2, PublicClass.constructorCount );
			T.AddTestCase( "*** No param constructor test ***", 1, 1 );
			foo = new PublicClass;
			T.AddTestCase( "var foo = new PublicClass, foo.constructorCount", 3, PublicClass.constructorCount );

			// ********************************************
			// Access Default method
			// ********************************************
//			T.AddTestCase( "*** Access default method of a class ***", 1, 1 );
//			T.AddTestCase( "Obj.setArray(arr), Obj.getArray()", arr, Obj.testGetSetArray(arr) );

			// ********************************************
			// Access Default virtual method
			// ********************************************
//			T.AddTestCase( "*** Access default virtual method of a class ***", 1, 1 );
//			T.AddTestCase( "Obj.setVirtArray(arr), Obj.getVirtArray()", arr2, Obj.testGetSetVirtualArray(arr2) );

			// ********************************************
			// Access Default Static method
			// ********************************************
			T.AddTestCase( "*** Access static method of a class ***", 1, 1 );
			T.AddTestCase( "Obj.setStatFunction(f), Obj.getStatFunction()", f, Obj.testGetSetStatFunction(f) );

			// ********************************************
			// Access Default Final method
			// ********************************************
//			T.AddTestCase( "*** Access final method of a class ***", 1, 1 );
//			T.AddTestCase( "Obj.setFinNumber(10), Obj.getFinNumber()", 10, Obj.testGetSetFinNumber(10) );

			// ********************************************
			// Access Internal method
			// ********************************************
			T.AddTestCase( "*** Access internal method of a class ***", 1, 1 );
			T.AddTestCase( "Obj.setInternalArray(arr), Obj.getInternalArray()", arr, Obj.testGetSetInternalArray(arr) );

			// ********************************************
			// Access Internal virtual method
			// ********************************************
//			T.AddTestCase( "*** Access internal virtual method of a class ***", 1, 1 );
//			T.AddTestCase( "Obj.setInternalVirtualArray(arr), Obj.getInternalVirtualArray()", arr2, Obj.testGetSetInternalVirtualArray(arr2) );


			// ********************************************
			// Access Internal Static method
			// ********************************************
			T.AddTestCase( "*** Access internal static method of a class ***", 1, 1 );
			T.AddTestCase( "Obj.setInternalStatFunction(f), Obj.getInternalStatFunction()", f, Obj.testGetSetInternalStatFunction(f) );


			// ********************************************
			// Access Internal Final method
			// ********************************************
//			T.AddTestCase( "*** Access internal final method of a class ***", 1, 1 );
//			T.AddTestCase( "Obj.setInternalFinNumber(10), Obj.getInternalFinNumber()", 10, Obj.testGetSetInternalFinNumber(10) );

			// ********************************************
			// Access Private method
			// ********************************************
//			T.AddTestCase( "*** Access private method of a class ***", 1, 1 );
//			T.AddTestCase( "Obj.setPrivDate(date), Obj.getPrivDate()", d.getFullYear(), Obj.testGetSetPrivDate(d).getFullYear() );

			// ********************************************
			// Access Private virtual method
			// ********************************************
//			T.AddTestCase( "*** Access private virtual method of a class ***", 1, 1 );
//			T.AddTestCase( "Obj.setPrivVirtualDate(date), Obj.getPrivVirtualDate()", d2.getFullYear(), Obj.testGetSetPrivVirtualDate(d2).getFullYear() );

			// ********************************************
			// Access Private Static method
			// ********************************************
			T.AddTestCase( "*** Access private static method of a class ***", 1, 1 );
			T.AddTestCase( "Obj.setPrivStatString(s), Obj.getPrivStatString", str, Obj.testGetSetPrivStatString(str) );

			// ********************************************
			// Access Private Final method
			// ********************************************
			T.AddTestCase( "*** Access private final method of a class ***", 1, 1 );
			T.AddTestCase( "Obj.setPrivFinalString(s), Obj.getPrivFinalString", str, Obj.testGetSetPrivFinalString(str) );

			// ********************************************
			// Access Public method
			// ********************************************
			T.AddTestCase( "*** Access public method of a class ***", 1, 1 );
			Obj.setPubBoolean(true);
			T.AddTestCase( "Obj.setPubBoolean(b), Obj.getPubBoolean()", true, Obj.getPubBoolean() );

			// ********************************************
			// Access Public virtual method
			// ********************************************
			T.AddTestCase( "*** Access public virtual method of a class ***", 1, 1 );
			Obj.setPubBoolean(false);
			T.AddTestCase( "Obj.setPubVirtualBoolean(b), Obj.getPubVirtualBoolean()", false, Obj.getPubBoolean() );

			// ********************************************
			// Access Public Static method
			// ********************************************
			T.AddTestCase( "*** Access public static method of a class ***", 1, 1 );
			PublicClass.setPubStatObject(ob);
			T.AddTestCase( "PublicClass.setPubStatObject(ob), PublicClass.getPubStatObject()", ob, PublicClass.getPubStatObject() );

			// ********************************************
			// Access Public Final method
			// ********************************************
			T.AddTestCase( "*** Access public final method of a class ***", 1, 1 );
			Obj.setPubFinArray(arr);
			T.AddTestCase( "Obj.setPubFinArray(arr), Obj.getPubFinArray()", arr, Obj.getPubFinArray() );

			// ********************************************
			// Access Public property
			// ********************************************
			T.AddTestCase( "*** Access public property of a class ***", 1, 1 );
			Obj.pubBoolean = true;
			T.AddTestCase( "Obj.pubBoolean = true, Obj.pubBoolean", true, Obj.pubBoolean );

			// ********************************************
			// Access Public Static property
			// ********************************************
			T.AddTestCase( "*** Access public satic property of a class ***", 1, 1 );
			PublicClass.pubStatObject = ob;
			T.AddTestCase( "PublicClass.pubStatObject = ob, PublicClass.pubStatObject", ob, PublicClass.pubStatObject );

			// ********************************************
			// Access Public Final property
			// ********************************************
			T.AddTestCase( "*** Access public final property of a class ***", 1, 1 );
			Obj.pubFinArray = arr;
			T.AddTestCase( "Obj.pubFinArray = arr, Obj.pubFinArray", arr, Obj.pubFinArray );

			// ********************************************
			// Access Public Final Static property
			// ********************************************
			T.AddTestCase( "*** Access public final static property of a class ***", 1, 1 );
			PublicClass.pubFinalStaticNumber = 10;
			T.AddTestCase( "PublicClass.pubFinalStaticNumber = 10, PublicClass.pubFinalStaticNumber", 10, PublicClass.pubFinalStaticNumber );

			T.test ();
		}
	}
}