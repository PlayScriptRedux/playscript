package {
    // Test basic class, method, and member declarations

    // Public non static members
    public class Foo1 {

        public var i:int;
        public var u:uint;
        public var n:Number;
        public var b:Boolean;
        public var s:String;

        public var a:Array;

        public var vi:Vector.<int>;
        public var vu:Vector.<uint>;
        public var vn:Vector.<Number>;
        public var vb:Vector.<Boolean>;
        public var vs:Vector.<String>;
        public var va:Vector.<Array>;

        private var _i:int;
        private var _u:uint;
        private var _n:Number;
        private var _b:Boolean;
        private var _s:String;

        protected var __i:int;
        protected var __u:uint;
        protected var __n:Number;
        protected var __b:Boolean;
        protected var __s:String;

        public function Foo1() {
            trace("Foo1");
        }

    }

    // Public non static members with initializers
    public class Foo2 {

        public var i:int = -100;
        public var u:uint = 100;
        public var n:Number = 100.0;
        public var b:Boolean = true;
        public var s:String = "A string";

        public var a:Array = [100, 200, 300, "blah", true, false, null, {a: 100, b: [1, 2, 3], "c": {a: 100}}];

        // Should generate a "Cannot implicitly convert A to B." error
//		public var vi:Vector.<int> = [-100, -200, -300];
//		public var vu:Vector.<uint> = [100, 200, 300];
//		public var vn:Vector.<Number> = [100.0, 200.0, 300.0];
//		public var vb:Vector.<Boolean> = [true, false];
//		public var vs:Vector.<String> = [ "yes", "no", "maybe" ];
//		public var va:Vector.<Array> = [ ["aaa", "bbb", "ccc"], [1, 2, 3], [true, false] ];

        public var _vi:Vector.<int> = new <int> [-100, -200, -300];
        public var _vu:Vector.<uint> = new <uint> [100, 200, 300];
        public var _vn:Vector.<Number> = new <Number> [100.0, 200.0, 300.0];
        public var _vb:Vector.<Boolean> = new <Boolean> [true, false];
        public var _vs:Vector.<String> = new <String> ["yes", "no", "maybe"];
        public var _va:Vector.<Array> = new <Array> [["aaa", "bbb", "ccc"], [1, 2, 3], [true, false]];

        public function Foo2() {
            trace("Foo2");
        }

    }

    // Public const members with initializers
    public class Foo3 {

        public const i:int = -100;
        public const u:uint = 100;
        public const n:Number = 100.0;
        public const b:Boolean = true;
        public const s:String = "A string";

        public const a:Array = [100, 200, 300, "blah", true, false, null, {a: 100, b: [1, 2, 3], "c": {a: 100}}];

        // Should generate a "Cannot implicitly convert A to B." error
//		public var vi:Vector.<int> = [-100, -200, -300];
//		public var vu:Vector.<uint> = [100, 200, 300];
//		public var vn:Vector.<Number> = [100.0, 200.0, 300.0];
//		public var vb:Vector.<Boolean> = [true, false];
//		public var vs:Vector.<String> = [ "yes", "no", "maybe" ];
//		public var va:Vector.<Array> = [ ["aaa", "bbb", "ccc"], [1, 2, 3], [true, false] ];

        public const _vi:Vector.<int> = new <int> [-100, -200, -300];
        public const _vu:Vector.<uint> = new <uint> [100, 200, 300];
        public const _vn:Vector.<Number> = new <Number> [100.0, 200.0, 300.0];
        public const _vb:Vector.<Boolean> = new <Boolean> [true, false];
        public const _vs:Vector.<String> = new <String> ["yes", "no", "maybe"];
        public const _va:Vector.<Array> = new <Array> [["aaa", "bbb", "ccc"], [1, 2, 3], [true, false]];

        public function Foo3() {
            trace("Foo3");
        }

    }

    // Public static members with initializers
    public class Foo4 {

        public static const i:int = -100;
        public static const u:uint = 100;
        public static const n:Number = 100.0;
        public static const b:Boolean = true;
        public static const s:String = "A string";

        public static const a:Array = [100, 200, 300, "blah", true, false, null, {a: 100, b: [1, 2, 3], "c": {a: 100}}];

        // Should generate a "Cannot implicitly convert A to B." error
        //		public var vi:Vector.<int> = [-100, -200, -300];
        //		public var vu:Vector.<uint> = [100, 200, 300];
        //		public var vn:Vector.<Number> = [100.0, 200.0, 300.0];
        //		public var vb:Vector.<Boolean> = [true, false];
        //		public var vs:Vector.<String> = [ "yes", "no", "maybe" ];
        //		public var va:Vector.<Array> = [ ["aaa", "bbb", "ccc"], [1, 2, 3], [true, false] ];

        public static const _vi:Vector.<int> = new <int> [-100, -200, -300];
        public static const _vu:Vector.<uint> = new <uint> [100, 200, 300];
        public static const _vn:Vector.<Number> = new <Number> [100.0, 200.0, 300.0];
        public static const _vb:Vector.<Boolean> = new <Boolean> [true, false];
        public static const _vs:Vector.<String> = new <String> ["yes", "no", "maybe"];
        public static const _va:Vector.<Array> = new <Array> [["aaa", "bbb", "ccc"], [1, 2, 3], [true, false]];

        public function Foo4() {
            trace("Foo4");
        }

    }

    public class Test {
        public static function Main():int {
            trace("Main");
            return 0;
        }
    }

}
