using System;
using System.Drawing;

namespace PlayScript
{
	public class ProfilerData
	{
		public class ScreenStruct
		{
			public RectangleF Bounds { get; set; }
			public float Scale { get; set; }
		}

		public string Name { get; set; }
		public string Device { get; set; }
		public string Model { get; set; }
		public string SystemVersion { get; set; }
		public ScreenStruct Screen { get; set; }

		public int FrameCount { get; set; }
		public TimeSpan ElapsedTime { get; set; }
	}
}

