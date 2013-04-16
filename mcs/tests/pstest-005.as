package
{
	// Test expressions.
	
	public class Foo 
	{
		public function Foo() {
			cl = Foo;
			f = this;
		}
	
		public var cl:Class = Foo;
		public var f:Foo;
	}
	
	public class Test extends Foo
	{
		public static function Main():void {
	
			var t:Test = new Test();	
			t.test();
			
		}
		
		public function test():void {
		
			// Test basic numeric operators.
			var i:int = 0;
			i = i + i;
			i = i - i;
			i = i / i;
			i = i * i;
			i = i << i;
			i = i >>> i;
			i = i >> i;
			i = i % i;
			i = i | i;
			i = i & i;
			i = i ^ i;
			i = ~i;
			
			// Test bool operators.
			var b:Boolean = false;
			b = !b;
			b = b && b;
			b = b || b;
			b = b == b;
			b = b != b;
			b = i < i;
			b = i > i;
			b = i <= i;
			b = i >= i;	
		
			// Test new expressions.
		
			var f:Foo = new Foo();
			var f2:Foo;
			
			f2 = new Foo();
			f2 = new Foo;
			f2 = new cl();
			f2 = new this.cl();
			f2 = new this.f.f.cl;
			f2 = new super.f.cl();
			f2 - new super.f.f.cl();
			
		}
	}

}
