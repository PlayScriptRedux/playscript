using System;

namespace _root
{
	public class Error : Exception
	{
		public Error (string msg) :
			base(msg)
		{
		}
	}
}

