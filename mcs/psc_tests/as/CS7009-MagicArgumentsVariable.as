//  error CS7009: The `arguments' magic variable is not currently supported in PlayScript
package magic {

    public class Program {

        //The basic usefulness of arguments is less useful now that we have var args. Here’s how it went in AS2:
        public function noVarArgs(a:Number, b:Number): void
        {
            arguments[0]; // a, or you could just use a
            arguments[1]; // b, or you could just use b
            arguments[2]; // third argument
            arguments[3]; // fourth argument
        }

        //Now you can do this a lot more simply and cleanly in AS3:
        public function varArgs(a:Number, b:Number, ... args)
        {
            a; // a
            b; // b
            args[0]; // third argument
            args[1]; // fourth argument
        }

        public static function Main():int {
            return 0;
        }

    }
}
