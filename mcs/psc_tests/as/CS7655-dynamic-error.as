// Compiler options: -dynamic- -newdynamic-
//
// test-ps-issue-18.as(12,13): error CS7655: Illegal use of dynamic: 'GetMember,Invoke'
// test-ps-issue-18.as(14,20): error CS7655: Illegal use of dynamic: 'Convert,GetIndex'
//
package blah {

    public class Program {

        public static function Main():int {

            var o:Object;

            // If dynamic are not allowed, 
            // the following code should generate errors
            o.blah("foo");

            var qq:Object = o[100];

            return 0;
        }
    }
}
