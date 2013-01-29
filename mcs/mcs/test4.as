package com.zynga.zengine.classes
{
	public class Test 
	{
        public static function Main() : void {
        	var s:String = "Hello World!", j:int = 100, k:Number = 200.0, l:uint = 300;
        	for (var q:int = 0; q < 122; q++) {
        		trace("Goo!");
        	}
        	j = 1 * 300 / 2 + 5;
        	j = j + k / l;
        	j = (j + k) / l;
        	if (s == null) {
        		trace("Foo!" + "Bar" + "Blah!");
        	} else {
        		trace("Bar!");
        	}
        	while (j == 4) {
        		trace ("blah");
        	}
        	trace(s + "Blah!");
		}
	}
}
