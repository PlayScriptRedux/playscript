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
 * Portions created by the Initial Developer are Copyright (C) 2007-2008
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
/**
   File Name:    concat.es
   Description:  The static concat method collects the vector elements from object followed by the vector
    elements from the additional items, in order, into a new vector object.  All the items must be objects.
    returns a new vector object
   */

package {
public class Tester extends Test {
public static function Main():int {

var SECTION = " ";
var VERSION = "AS3";
startTest();

writeHeaderToLog( SECTION + " Vector.concat()");

var v1=new Vector.<uint>();
v1[0]=0; v1[1]=1; v1[2]=2;
var v2=new Vector.<uint>();
v2[0]=3;v2[1]=4;v2[2]=5;
var v3=v1.concat(v2);
AddTestCase(    "concat uint vector, original vector is unchanged",
		"0,1,2",
		v1.toString());
AddTestCase(	"concat uint vector, new vector concat worked",
		"0,1,2,3,4,5",
		v3.toString());

var v4:Vector.<String>=new Vector.<String>();
v4[0]="zero"; v4[1]="one"; v4[2]="two";
var v5:Vector.<int>=new Vector.<int>();
v5[0]=0; v5[1]=1; v5[2]=2;
var errormsg;
try {
  var v6=v4.concat(v5);
} catch (e:Error) {
  errormsg=e.toString();
}
AddTestCase(    "concat two differently typed vectors",
                "TypeError: Error #1034",
                parseError(errormsg,"TypeError: Error #1034".length));

var v7:Vector.<TestClass> = new Vector.<TestClass>();
v7.push(new TestClass(33));
v7.push(new TestClass(44));

var v8 = new Vector.<TestClass>();
v8.push(new TestClass(100));

var v9 = v7.concat(v8);

AddTestCase("concat custom vector class", "33,44,100", v9.toString());

AddTestCase("concat vector to itself multiple times",
            "100,100,100",
            v8.concat(v8,v8).toString()
            );

AddTestCase("concat with no parameters duplicates original vector",
            "33,44",
            v7.concat().toString()
            );

var b1 = new <Boolean>[true,false,true];
var b2 = new <Boolean>[false,true,false];
AddTestCase("concat boolean vectors", "true,false,true,false,true,false", b1.concat(b2).toString());

var xmlVector = new <XML>[];
var expectedArr:Array = [];
for (var i=0; i<80; i++) {
    xmlVector = xmlVector.concat(new <XML>[XML("<test>"+i+"</test>")]);
    expectedArr.push(i);
}

AddTestCase("concat XML vectors", expectedArr.join(','), xmlVector.toString() );

var va:Array = [];
// Concat multiple vectors
for (var i=0; i <= 10; i++) {
    va[i] = new <int>[i];
}

AddTestCase("concat multiple int vectors",
            "0,1,2,3,4,5,6,7,8,9,10",
            va[0].concat(va[1],va[2],va[3],va[4],va[5],va[6],va[7],va[8],va[9],va[10])
           );

test();
return results();
}}}


class TestClass {
	private var myVal:Object;
	public function TestClass(v:Object):void {
		myVal = v;
	}
	public function toString():String {
		return myVal.toString();
	}
}

