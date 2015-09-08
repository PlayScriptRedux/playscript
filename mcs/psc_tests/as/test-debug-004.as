// Comiler options: -r:../class/lib/net_4_5/pscorlib.dll
package {

    import System.*;
    import System.Diagnostics.*;

    import flash.utils.Dictionary;
    import flash.display.DisplayObject;

    public class inlineAttribute extends System.Attribute {
    }

    public class Texture {
        private var mCount:int = 10;

        public function t0():int {
            return mCount++;
        }

        public function t1():int {
            return mCount++;
        }

        public function t2():int {
            return mCount++;
        }

        public function testCoalescing():void {

            var n:int;
            var n2:int;

            n = t0() && t1();
            n2 = t0() || t1();
            trace(n, n2);

            var alpha:Number = 1.0;
            var obj:Object = new Object();

            var str:String = "abc";
            str += obj.id;

            obj.texture = new Texture();

            obj.i = 5;
            obj.d = 123.4;

            var i:int = obj.i;
            var d:Number = obj.d;
            if (obj && obj.hasOwnProperty("renderLayer")) {
                var layer:int = obj.layer;
                if (obj.hasOwnProperty("alpha") && !(layer == 5 || layer == 4)) {
                    alpha = obj.hasOwnProperty("alpha") ? obj.alpha : 0.6;
                }
            }
            trace(alpha);
        }

        public function get value():int {

            return 0;
        }

        public function get value2():int {
            return 0;
        }

        public function get valueF():Number {
            return mValueF;
        }

        public function set valueF(v:Number):void {
            mValueF = v;
        }

        public function foo():void {
            var o:Object = 5;
            testInvoke3Instance(o);
            testInvoke3Static(o);
        }

        public function testInvoke3Instance(a:int, b:String = null):void {
            trace(a, b);
        }

        public static function testInvoke3Static(a:int, b:String = null):void {
            trace(a, b);
        }

        public function testInvoke(a:int, b:String):void {
            trace(a, b);
        }

        public function testInvoke2(o:Object):void {
            trace(o);
        }

        public function testFunc(a:int):int {
            return a + 1;
        }

        private var mValueF:Number;

//		public final function get entityDef():Object
//		{
//			return _entityDef;
//		}
//
//		private var _entityDef:Object = new Object();
    }

    public class Texture2 extends Texture {
        public override function foo():void {
        }

        public function inlinable():void {
        }

        public override function get value():int {
            return 0;
        }

        public function get value_inlinable():int {
            return 0;
        }

//		public function blah():Number {
//			var offsetY:Number;
//
//			offsetY = entityDef.stats.bar_offset_y;
//			return offsetY;
//		}
    }

    public class Texture3 extends Texture {
        public override function foo():void {
        }

        public function inlinable():void {
        }

        public function set value_inlinable(val:int):void {
        }

        public function Texture3(a:int, b:Number = 5.0) {
        }
    }

    public class Test {
        /*
         public static function TestSeal():void
         {
         var t:Texture = new Texture();
         var t2:Texture2 = new Texture2();
         var t3:Texture3 = new Texture3();

         t.foo();
         t2.foo();
         t2.inlinable();
         t3.foo();
         t3.inlinable();
         }
         */
        /*
         public static function TestSwitch():void
         {
         var obj:Object = "abc";
         var n:Number = 10.0;
         switch (n) {
         case 5.0: trace(5); break;
         case 10.0: trace(10); break;
         default:
         break;
         }

         switch (obj) {
         default:
         trace(5);
         break;
         }

         switch (obj) {
         case "def": trace(1); break;
         case "abc": trace(2); break;
         case "xyz": trace(3); break;
         //			case 1:  break;
         default:
         trace(4);
         break;
         }

         }
         */

//		public static implicit operator int (list:Class) {
//			throw new System.NotImplementedException();
//		}

//		public static implicit operator Test (o:Object) {
//		{
//			if ( o is string)
//			{
//				return (string)o;
//			}
//			return o.ToString();
//		}

    #if false
        public static function getNullSprite():flash.display.Sprite {
            return null;
        }

        public static function getNonNullSprite():flash.display.Sprite {
            return new flash.display.Sprite();
        }

        public static function ImplicitBooleanCastTest():void {

            var a:Object = false;
            var b:Object = true;
            var c:flash.display.Sprite = new flash.display.Sprite();
            var d:flash.display.Sprite = null;

            var e:int = 0;
            var f:Object = Object(e);
            var g:Boolean = null;

            if (getNullSprite()) {
                throw new InvalidOperationException();
            }

            if (!getNonNullSprite()) {
                throw new InvalidOperationException();
            }

            if (a) {
                throw new InvalidOperationException();
            }

            if (b) {
            } else {
                throw new InvalidOperationException();
            }

            if (c) {
            } else {
                throw new InvalidOperationException();
            }

            if (d) {
                throw new InvalidOperationException();
            } else {
            }

            if (e) {
                throw new InvalidOperationException();
            } else {
            }

            if (f) {
                throw new InvalidOperationException();
            } else {
            }

            if (g) {
                throw new InvalidOperationException();
            }
        }

#endif

	#if false
        public static function ImplicitStringCastTest():void {
            var o1:Object = 5;
            var o2:String = "abc";
            var o3:Texture = new Texture();

            var s1:String = String(o1);
            var s2:String = String(o2);
            var s3:String = String(o3);
            Debug.Assert(s1 == "5");
            Debug.Assert(s2 == "abc");
            Debug.Assert(s3 == "_root.Texture");
        }
#endif

//
//		public static function TestForEach():void {
//			var a:Array = [true, false];
//			for each (var tinted:Boolean in a)
//			{
//				trace(tinted);
//			}
//		}

        // Static table initialization
    #if false
    {
        private var ZigZag:Array = [
            0, 1, 5, 6, 14, 15, 27, 28,
            2, 4, 7, 13, 16, 26, 29, 42,
            3, 8, 12, 17, 25, 30, 41, 43,
            9, 11, 18, 24, 31, 40, 44, 53,
            10, 19, 23, 32, 39, 45, 52, 54,
            20, 22, 33, 38, 46, 51, 55, 60,
            21, 34, 37, 47, 50, 56, 59, 61,
            35, 36, 48, 49, 57, 58, 62, 63
        ];
    }

        private var YTable:Array = new Array(64);
        private var UVTable:Array = new Array(64);
        private var fdtbl_Y:Array = new Array(64);
        private var fdtbl_UV:Array = new Array(64);

        public function initQuantTables(sf:int):void {
            var i:int;
            var t:Number;
            var YQT:Array = [
                16, 11, 10, 16, 24, 40, 51, 61,
                12, 12, 14, 19, 26, 58, 60, 55,
                14, 13, 16, 24, 40, 57, 69, 56,
                14, 17, 22, 29, 51, 87, 80, 62,
                18, 22, 37, 56, 68, 109, 103, 77,
                24, 35, 55, 64, 81, 104, 113, 92,
                49, 64, 78, 87, 103, 121, 120, 101,
                72, 92, 95, 98, 112, 100, 103, 99
            ];
            for (i = 0; i < 64; i++) {
                t = Math.floor((YQT[i] * sf + 50) / 100);
                if (t < 1) {
                    t = 1;
                } else if (t > 255) {
                    t = 255;
                }
                YTable[ZigZag[i]] = t;
            }
            var UVQT:Array = [
                17, 18, 24, 47, 99, 99, 99, 99,
                18, 21, 26, 66, 99, 99, 99, 99,
                24, 26, 56, 99, 99, 99, 99, 99,
                47, 66, 99, 99, 99, 99, 99, 99,
                99, 99, 99, 99, 99, 99, 99, 99,
                99, 99, 99, 99, 99, 99, 99, 99,
                99, 99, 99, 99, 99, 99, 99, 99,
                99, 99, 99, 99, 99, 99, 99, 99
            ];
            for (i = 0; i < 64; i++) {
                t = Math.floor((UVQT[i] * sf + 50) / 100);
                if (t < 1) {
                    t = 1;
                } else if (t > 255) {
                    t = 255;
                }
                UVTable[ZigZag[i]] = t;
            }
            var aasf:Array = [
                1.0, 1.387039845, 1.306562965, 1.175875602,
                1.0, 0.785694958, 0.541196100, 0.275899379
            ];
            i = 0;
            for (var row:int = 0; row < 8; row++) {
                for (var col:int = 0; col < 8; col++) {
                    fdtbl_Y[i] = (1.0 / (YTable [ZigZag[i]] * aasf[row] * aasf[col] * 8.0));
                    fdtbl_UV[i] = (1.0 / (UVTable[ZigZag[i]] * aasf[row] * aasf[col] * 8.0));
                    i++;
                }
            }
        }

        private var YDC_HT:Array;
        private var UVDC_HT:Array;
        private var YAC_HT:Array;
        private var UVAC_HT:Array;
#endif

    public static function getSize(o:Object):int {
        return 100;
    }
#if false

        public class Virals {
            /** The name of the viral type */
            public var name:String;

            /** Global cooldown in hours before any viral of this viral type can be used again */
            public var cooldown:Number;

            /** Array of possible user-selected cooldowns for this viral type. This overrides cooldown if the user chooses one */
            public var userCooldowns:Array;

            /** Populate the class from the raw data object */
            public function fromData(name:String, data:Object):void {
                this.name = name;
//
//			// cooldown may be experiment controlled or may be simple number
//			cooldown = 0;
//			if(data.hasOwnProperty('cooldown'))
//			{
//				if( !isNaN(Number(data.cooldown)) )
//				{
//					cooldown = data.cooldown;
//				}
//				else if(data.cooldown.hasOwnProperty('experiment') && data.cooldown.hasOwnProperty('variants'))
//				{
                var variant:String = "5";
                if (data.cooldown.variants.hasOwnProperty(variant)) {
//						cooldown = data.cooldown.variants[variant];
                }
//				}
//			}

//			userCooldowns = data.userCooldowns;
            }
        }
#endif

    protected static var substituterMap:Object = new Object();
        protected static var m_locale:String;
        public static var langcode:String;

        public static function setSubstituter():void {
            if (substituterMap.hasOwnProperty(m_locale)) {
//				var cl:Class = substituterMap[this.m_locale];
//				this.m_substituter = new cl;
            } else if (substituterMap.hasOwnProperty(langcode)) {
//				var cl:Class = substituterMap[this.langCode];
//				this.m_substituter = new cl;
            } else {
//				this.m_substituter = new SubstituterSimple();
            }
        }

        public static var a:Array = null;

        public static function TestConvert():void {
//			var o:Object = 5.0;
//			var v:Number = o;
//			var s:String = o;
//			trace(o, v, s);
////
            ///
            /*
             var t:Texture = new Texture();
             var ot:Object = t;
             ot.valueF = 123.0;
             //			var v:Number = ot.valueF;
             //			t.valueF = t.valueF * ot.valueF;
             trace(ot.valueF);

             t.valueF *= ot.valueF;
             */

//			var obj:Object = new Object();
//			obj.summary = 5;

//			obj.summary++;

//			var i:int = obj.summary;
//			var d:Number = obj.summary;
//			trace(obj.summary, i, d);
//			trace(i);

//			var array:Array = new Array();
//			var texture:Texture = null;
//			array.Add(new Texture());
//
//			var t2:Texture = texture || array[0];
//			trace(t2);

            /*
             var total:int  = 0;
             // create summary as dictionary
             var dict:Dictionary = new Dictionary();
             var tracked:Array = new Array();
             for (var o:Object in tracked) {
             var t:Object = tracked[o];

             var summary:Object = new Object();

             // get object size
             var size:int = getSize(o);

             // update summary for object context
             summary.count++;
             summary.total += size;

             total += size;
             }*/

//			var a:int, b:int;
//			var summary:Object = new Object();

//			var t:Texture = new Texture();
//			var o:Object = t;
////			o.testInvoke(5, "abc");
//			o.testInvoke2({a:5, b:6});
//
//			var i:int = o.testFunc(10);
//			trace(i);

            var obj:Object = new Object();
            var str:String = obj.toString();
            var str1:String = obj.ToString();
            var t3:Texture3 = new Texture3(5);

            var f3:Function = t3.testInvoke;

            f3(5, "abc");

            trace(t3);
//			var id:String = "abc";
//			obj[id] = 0;
//			obj[id]++;

//			obj.id = 0;
//			obj.id++;

//			trace(obj.id);

//			var alpha:Number = 0.0;
//			obj.hello = null;
//			a = obj.hello;

            /*

             if (obj && obj.hasOwnProperty("renderLayer"))
             {
             var layer:int = obj.layer;
             if(obj.hasOwnProperty("alpha") || layer == 5 || layer == 4)
             {
             alpha = obj.hasOwnProperty("alpha") ? obj.alpha : 0.6;
             }
             }*/
//			trace(a);

//			var a:Object = 5.0;
//			var b:Object = 10.0;
//			var c:Number = a + b;
//			var d:Number = a - b;
//			var e:Number = a / b;
//			var f:Number = a * b;
//			var g:Number = a % b;
//			trace(a,b,c,d,e,f,g);

            /*

             var o:Object = 5.0;

             var i:int = 5;

             i += o;


             summary.total = summary.total2 = 10;
             summary.total = summary.total2;




             summary["total"] = summary["total2"] = 10;

             //			var size:int = 100;
             //			summary.total = summary.total2 = size;

             //			if (summary == null) {
             //				summary.a = "x";
             //			}

             // get object size
             var size:int = 1000;
             var factor:Number = 2.0;

             summary["xyz"] = null;

             // update summary for object context
             summary.total += size;
             //
             summary.total *= factor;
             //
             summary.size = size;

             summary.method = getSize;

             //	trace(summary.total);
             */
        }

//		public static function TestIndex():void {
//
//			var obj:Object = new Object();
//			obj["summary"] = 5;
//
//			var str:String = "summary";
//
//			var s:Object = obj[str];
//			trace(s);
//
//
//			obj[str] = 10.0;
//
//			obj.summary ++;
//
//
//
//			var i:int = obj.summary;
//			var d:Number = obj[str];
//			trace(i, d);
//		}
//
//
//		private static var mRootObject:Object = new Object();
//
//		/** Returns the root symbol name for this asset (the symbol of the root object) if it has one. */
//		public static function get rootSymbolName():String {
//			if (mRootObject != null && mRootObject.symbolName != null) {
//				return mRootObject.symbolName;
//			}
//			return null;
//		}

        public static function testIterator():void {
            var v:Vector.<int> = new Vector.<int>();
            v.push(5);
            v.push(6);
            v.push(7);
            v.push(8);

            for (var i2:int in v) {
                trace(i2);
            }

            for each (var i:int in v) {
                trace(i);
            }

            var a:Array = new Array();
            a.push("5");
            a.push(65);
            a.push("abc");
            a.push(true);

            for (var j2:int in a) {
                trace(j2);
            }

            for each (var j:Object in a) {
                trace(j);
            }

            var o:Object = {a: 5, b: 6};
            for (var k0:Object in o) {
                trace(k0);
            }

            for each (var j3:Object in o) {
                trace(j3);
            }

            var d:Dictionary = new Dictionary();
            d.a = 5;
            d.b = 6;
            for each (var j4:Object in d) {
                trace(j4);
            }

        }

        public static function Main():void {
            //var z:Boolean = getNullSprite();
            //trace(z);

//			TestSeal();

//			TestSwitch();

//			TestForEach();
            //TestIndex();

            //PlayScript.Dynamic.Test();

//			var v:Virals = new Virals();
//			v.fromData("a", null);

            testIterator();
//			TestConvert();
//			initQuantTables(10);

            //removeFromQueue("a", "b");
            /*
             var dataStream:System.IO.Stream = null; // = new System.IO.FileStream("/tmp/amt3.log", System.IO.FileMode.Open);
             var reader = new System.IO.StreamReader(dataStream, System.Text.Encoding.UTF8);
             var str:String = reader.ReadToEnd();
             trace(str);*/
//			ImplicitBooleanCastTest();
//			ImplicitStringCastTest();

            /*
             var s1:String = "1";
             var s2:String = null;
             var s3:String = "3";

             if (s1 || s2 || s3) {
             trace("yes");
             }

             var o1:String = s1 || s2 || s3;
             trace(o1);
             */

//			var a:Array = ["a", "b", "c", "d", "e"];

//			var a:flash.utils.Dictionary = new flash.utils.Dictionary;
//			a['a'] = 1;
//			a['b'] = 2;

//			var a:Array = ["a", "b", "c", "d", "e"];
//			var o:Object = a;
//
//			for each (var j:Object in o)
//			{
//				trace(j);
//			}
//
//
//			for (var i:Object in o)
//			{
//				trace(i);
//			}

//			var o1:Object = 5;
//			var o2:String = "abc";
//			var o3:Texture = new Texture();
//
//			var s1:String = String(o1);
//			var s2:String = String(o2);
//			var s3:String = String(o3);
//			trace(s1);
//			trace(s2);
//			trace(s3);

            /*
             var dict = new flash.utils.Dictionary();
             dict["a"] = 5;
             dict["b"] = 2;
             delete dict["a"];
             delete dict.b;

             var sd = new flash.display.ShaderData(null);
             sd["a"] = 5;
             sd["b"] = 2;
             delete sd["a"];
             delete sd.b;


             var sdobj:Object = sd;
             delete sdobj["x"];
             */
//			var str = "abc";
//			delete str["x"]; // should not compile

//
//			var list:Vector.<int> = new Vector.<int>;
//			var list2:Vector.<int> = new Vector.<int>();
//			trace(list);
//			trace(list2);
//
//			var list3:System.Collections.Generic.List.<int> = new System.Collections.Generic.List.<int>;
//			var list4:System.Collections.Generic.List.<int> = new System.Collections.Generic.List.<int>();
//			trace(list3);
//			trace(list4);
//

//			var a:Array = new Array();
//			var cl:Class = Array;
//			trace(a is cl);
        }

//		[inline]
//		public function set texture(texture:Texture):void { _texture = texture; }

    }

}
