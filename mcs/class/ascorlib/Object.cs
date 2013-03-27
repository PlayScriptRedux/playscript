using System;

namespace _root
{
	public class Object
	{
		public virtual string toString()
		{
			return base.ToString();
		}

		public override string ToString ()
		{
			return toString ();
		}
		
		public virtual bool hasOwnProperty(object v = null)
		{
			var t = GetType ();
			var name = v as string;
			
			if (name != null) {
				return t.GetProperty(name) != null ||
					t.GetField (name) != null;
			}
			
			return false;
		}
		
		public virtual dynamic valueOf()
		{
			return toString();
		}

	}
}

