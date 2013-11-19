using System;
using System.Drawing;
using System.Collections.Generic;

namespace PlayScript
{
	public class ProfilerData
	{
		public ProfilerData ()
		{
			Timings = new List<TimingData> ();
		}

		public string ReportName { get; set; }

		public string Device { get; set; }

		public string Model { get; set; }

		public string SystemVersion { get; set; }

		public ScreenData Screen { get; set; }

		public LoadingData Loading { get; set; }

		public SessionData Session { get; set; }

		public int FrameCount { get; set; }

		public TimeSpan ElapsedTime { get; set; }

		public List<TimingData> Timings { get; set; }
	}

	public class ScreenData
	{
		public ScreenData (RectangleF bounds, float scale)
		{
			this.Bounds = bounds;
			this.Scale = scale;
		}

		public RectangleF Bounds { get; private set; }

		public float Scale { get; private set; }
	}

	public class MilestoneData
	{
		public MilestoneData (string name, TimeSpan time)
		{
			this.Name = name;
			this.Time = time;
		}

		public string Name { get; private set; }

		public TimeSpan Time { get; private set; }
	}

	public class LoadingData
	{
		public LoadingData (bool offlineMode)
		{
			this.OfflineMode = offlineMode;
			this.Milestones = new List<MilestoneData> ();
		}

		public bool OfflineMode { get; private set; }

		public List<MilestoneData> Milestones { get; private set; }

		public void AddMilestone (String name, TimeSpan time)
		{
			Milestones.Add (new MilestoneData (name, time));
		}
	}

	public class SessionData
	{
		public int TotalFrames { get; set; }

		public TimeSpan TotalTime { get; set; }

		public double AverageFrameRateInMs { get; set; }

		public double AverageFrameRateInFps { get; set; }

		public double AverageMinimumFrameRate { get; set; }

		public double NinetyFivePercentInMiliseconds { get; set; }

		public double NinetyFivePercentInFramesPerSecond { get; set; }

		public double FastFramePercentage { get; set; }

		public double SlowFramePercentage { get; set; }

		public int GarbageCollection0Count { get; set; }

		public int GarbageCollection1Count { get; set; }
	}

	public class TimingData
	{
		public String Name { get; set; }

		public TimeSpan TotalTime { get; set; }

		public double Average { get; set; }

		public double Minimum { get; set; }

		public double Maximum { get; set; }

		public double Median { get; set; }

		public int GarbageCollection0Count { get; set; }

		public float UsedMemoryInKiloBytes { get; set; }
	}
}

