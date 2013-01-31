package com.zynga.zengine.classes
{
	public class Test 
	{
		public static var _field1:int = 100;
		public static var _field2:String = "Zaaa";
	
		public function Test() {
			trace("Test");
		}
		
		public function f():void {
			f();
		}
	
        public static function Main() : int {
        	var t:Test;
        	t.f();
        	var s:String = "Hello World!", j:int = 100, k:Number = 200.0, l:uint = 300;
        	for (var q:int = 0; q < 122; q++) {
        		trace("Goo!");
        		continue;
        	}
        	var a = [100, 200, 300];
        	a.push(400);
        	var o = {blah:100, joo:false, blee:null};
        	trace(o.blah);
        	j = 1 * 300 / 2 + 5;
        	j = j + k / l;
        	j = (j + k) / l;
        	switch (j) {
        		case 0:
        		case 1:
        			trace("flah");
        			break;
        		case 2:
        			trace("boo");
        			break;
        		default:
        			trace("google!");
        			break;
        	}
        	if (s == null) {
        		trace("Foo!" + "Bar" + "Blah!");
        	} else {
        		trace("Bar!");
        	}
        	while (j == 4) {
        		trace ("blah");
        		break;
        	}
        	do {
        		trace ("blah");
        	} while (j == 50);
        	trace(s + "Blah!");
        	return j + 100;
		}
	}
}
