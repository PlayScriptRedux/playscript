// Compiler options: -r:./as/Assert.dll
package
{
    import com.adobe.test.Assert;

	public class ConversionTest
	{
		public static function Main():int
		{
            var nanValue:Number = NaN;
            var u:* = undefined;
            var nullObj:* = null;
            var obj:Object = new Object();

            // 'as' allows numeric up conversion, but not down conversion
            Assert.expectEq("(5 as Number) == 5.0", true, (5 as Number) == 5.0);
            Assert.expectEq("(5.0 as int) == 0", true, (5.0 as int) == 0);

            // 'as' doesnt cast from boolean to numeric
            Assert.expectEq("(true as Number) == 0.0)", true, (true as Number) == 0.0);
            Assert.expectEq("(false as Number) == 0.0", true, (false as Number) == 0.0);

			return 0;
		}

//		public static function doTests():void
//		{
//			var nanValue:Number = NaN;
//			var u:* = undefined;
//			var nullObj:* = null;
//			var obj:Object = new Object();
//
//            //			Assert.expectEq("A == \"blah\"", true, A == "blah");
//
//
//            // 'as' allows numeric up conversion, but not down conversion
//            Assert.expectEq("(5 as Number) == 5.0)", true, (5 as Number) == 5.0);
//			test((5.0 as int) == 0);
//			// 'as' doesnt cast from boolean to numeric
//            Assert.expectEq("(true as Number) == 0.0)", true, (true as Number) == 0.0);
//            Assert.expectEq("test((false as Number) == 0.0)", true, (false as Number) == 0.0);
//
//			test(Number(true) == 1.0);
//			test(int(true) == 1);
//
//			test(int("1234") == 1234);
//			test(Number("1234") == 1234.0);
////			test(("1234" as int) == 0);   // <<= broken
////			test(("1234" as Number) == 0.0); // <<= broken
//
//			test(int("0x1234") == 4660);
//			test(int("0X1234") == 4660); // <<= broken
//			test(int("0xFFFFFFFF") == -1); // (or 0xFFFFFFFF)  // <<= broken
//			test(int("0xFFFFFFFF4444") == 0xFFFF4444);  // <<= broken
//			test(Number("0x1234") == 4660.0); // <<= broken
//
//			// invalid parsing (0 for int, NaN for number)
//			test(int("xyz") == 0);  // <<= broken (this throws)
//			test(isNaN(Number("xyz")));  // <<= broken (this throws)
//
//			// number conversion of undefined (NaN for explicit cast, 0.0 for 'as' cast)
//			test(isNaN(Number(u)));
////			test(u as Number == 0.0);  // <<= broken
//
//			// int conversion of undefined (0 for explicit cast, 0 for 'as' cast)
//			test(int(u) == 0);
//			// test(u as int == 0);
//
//			// null to number produces 0.0
//			test(Number(nullObj) == 0.0);
//			test(nullObj as Number == 0.0);
//
//			// int to number does proper cast
//			test(Number(5) == 5.0);
//			test((5 as Number) == 5.0); // <<= broken
//
//			// conversion of NaN to int
//			test(int(nanValue) == 0);
//			test((nanValue as int) == 0);
//
//			// casting of undefined or null produces the string "undefined" or "null", but not with as
//			test(String(u) == "undefined");     // <<= sometimes works but not always
//			test(String(null) == "null");               // <<= sometimes works but not always
//			test((u as String) == null);
//			test((nullObj as String) == null);
//
//			// casting of object to String invokes toString() on object, but not with as
//			test(String(obj) == obj.toString());
//			test((obj as String) == null);
//
//			// object as number produces 0.0, Number(object) produces NaN
//			test(isNaN(Number(obj)));
////			test((obj as Number) == 0.0); // <<= broken
////			test((obj as int) == 0);
//
//			// conversion of numerics to boolean
//			test(Boolean(0)              == false);
//			test(Boolean(1)              == true);
//			test(Boolean(0.0)            == false);
//			test(Boolean(1.0)            == true);
//
//			// conversion of strings to boolean (we had this wrong, only "" is false)
//			test(Boolean("")              == false);
//			test(Boolean("0")            == true); // <<= broken
//			test(Boolean("1")            == true);
//			test(Boolean("0x0")          == true); // <<= broken
//			test(Boolean("0x1")          == true);
//			test(Boolean("false")        == true); // <<= broken
//			test(Boolean("true")         == true);
//			test(Boolean("FALSE")        == true);
//			test(Boolean("TRUE")         == true);
//
//			test("" as Boolean          == false);
//			test("0" as Boolean         == true); // <<= broken
//			test("1"  as Boolean        == true);
//			test("0x0"  as Boolean      == true); // <<= broken
//			test("0x1"  as Boolean      == true);
//			test("false"  as Boolean    == true); // <<= broken
//			test("true"  as Boolean     == true);
//			test("FALSE"  as Boolean    == true);
//			test("TRUE"  as Boolean     == true);
//		}
	}
}
