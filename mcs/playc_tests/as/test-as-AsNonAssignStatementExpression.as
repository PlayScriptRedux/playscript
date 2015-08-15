package {

    public class Flow {

protected var _name:String;

public interface IAnimator
	{
}

public function play(name : String, transition : IAnimator = null, offset : Number = NaN) : void
{

}

        public static function Main():int {

		var o:Flow = new Flow();
		o.play("foobar", null, NaN);
                return 0;
        }
    }

}

