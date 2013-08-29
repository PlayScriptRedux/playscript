using System;
using System.Diagnostics;
using System.Collections.Generic;
using Amf;

namespace Telemetry
{
	/// <summary>
	/// This class defines a named value for telemetry reporting.
	/// Call WriteValue() to write a value
	/// </summary>
	public sealed class Value
	{
		public string Name {get {return mName.Value;}}

		public Value(Amf3String name)
		{
			mName = name;
		}

		public Value(string name)
		{
			mName = new Amf3String(name);
		}

		public void WriteValue(int value)
		{
			Session.WriteValue(mName, value);
		}

		public void WriteValue(double value)
		{
			Session.WriteValue(mName, value);
		}

		public void WriteValue(string value)
		{
			Session.WriteValue(mName, value);
		}

		public void WriteValueObject(object value)
		{
			Session.WriteValue(mName, value);
		}

		#region Private
		// value name (as amf-ready string)
		private readonly Amf3String mName;
		#endregion
	}
}

