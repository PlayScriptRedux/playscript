using System;
using System.Diagnostics;
using System.Collections.Generic;
using Amf;

namespace Telemetry
{
	/// <summary>
	/// This class defines a named span of time for telemetry reporting.
	/// Call Begin and End around the time span.
	/// The class currently is not re-entrant, so begin/end may only be called once for a span.
	/// </summary>
	public sealed class Span
	{
		public string Name 		{get {return mName.Value;}}
		public bool   IsInSpan  {get {return mIsInSpan;} }

		public Span(Amf3String name)
		{
			mName = name;
		}

		public Span(string name)
		{
			mName = new Amf3String(name);
		}

		// begins a span
		public void Begin()
		{
			if (mIsInSpan)
				throw new InvalidOperationException("Already inside span. Spans do not support recursion (yet)");

			// begin a span
			mBeginTime = Session.BeginSpan();

			// set span flag
			mIsInSpan = true;
		}

		// ends a span
		public void End()
		{
			if (!mIsInSpan)
				throw new InvalidOperationException("Span End() called without Begin()");

			// emit end span
			Session.EndSpan(mName, mBeginTime);

			// clear span flag
			mIsInSpan = false;
		}

		// ends a span with a value
		public void EndValue(object value)
		{
			if (!mIsInSpan)
				throw new InvalidOperationException("Span EndValue() called without Begin()");

			Session.EndSpanValue(mName, mBeginTime, value);
			
			// clear span flag
			mIsInSpan = false;
		}

		#region Private
		// true if we are in this span
		private bool 				mIsInSpan;

		// time that span begun
		private long 				mBeginTime;

		// span name (as amf-ready string)
		private readonly Amf3String mName;
		#endregion
	}
}

