package {
// Test local constants and constant references.

    public class Foo {
        public static const i:int = 100;
        public static const s:String = "blah";
        public static const b:Boolean = true;
        public static const n:Number = 123.45;

        public static const v:Vector.<int> = new Vector.<int>();
        public static const o:Array = [10, 20, 30, 40];

        public static const cl:Class = Array;
        public static const cl2:Class;

        public const i2:int = 100;
        public const s2:String = "blah";
        public const b2:Boolean = true;
        public const n2:Number = 123.45;

        public const v2:Vector.<int> = new Vector.<int>();
        public const o2:Array = [10, 20, 30, 40];

        public const cl3:Class = Array;
        public const cl4:Class;

    }

    public class Test extends Foo {

        public static function Main():int {

            const i:int = 100;
            const s:String = "blah";
            const b:Boolean = true;
            const n:Number = 123.45;

            const v:Vector.<int> = new Vector.<int>();
            const o:Array = [10, 20, 30, 40];

            const cl:Class = Array;
//			const cl2:Class;

            return 0;
        }
    }

}
