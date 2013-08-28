using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;

#if PLATFORM_MONOMAC
using MonoMac.OpenGL;
#elif PLATFORM_MONOTOUCH
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using OpenTK.Graphics;
using OpenTK.Graphics.ES20;
#elif PLATFORM_MONODROID
using OpenTK.Graphics;
using OpenTK.Graphics.ES20;
#endif

namespace PlayScript
{
	#if PLATFORM_MONOTOUCH
	// Somehow MonoTouch does not give access to the HardwareProperty
	// Code from this: http://stackoverflow.com/questions/10889695/how-do-i-get-the-ios-device-and-version-in-monotouch
	public class IOSDeviceHardware
	{
		public const string HardwareProperty = "hw.machine";

		public enum IOSHardware {
			iPhone,
			iPhone3G,
			iPhone3GS,
			iPhone4,
			iPhone4RevA,
			iPhone4CDMA,
			iPhone4S,
			iPhone5GSM,
			iPhone5CDMAGSM,
			iPodTouch1G,
			iPodTouch2G,
			iPodTouch3G,
			iPodTouch4G,
			iPodTouch5G,
			iPad,
			iPad3G,
			iPad2,
			iPad2GSM,
			iPad2CDMA,
			iPad2RevA,
			iPadMini,
			iPadMiniGSM,
			iPadMiniCDMAGSM,
			iPad3,
			iPad3CDMA,
			iPad3GSM,
			iPad4,
			iPad4GSM,
			iPad4CDMAGSM,
			iPhoneSimulator,
			iPhoneRetinaSimulator,
			iPadSimulator,
			iPadRetinaSimulator,
			Unknown
		}

		[DllImport(MonoTouch.Constants.SystemLibrary)]
		static internal extern int sysctlbyname([MarshalAs(UnmanagedType.LPStr)] string property, IntPtr output, IntPtr oldLen, IntPtr newp, uint newlen);

		public static IOSHardware Version {
			get {
				var pLen = Marshal.AllocHGlobal(sizeof(int));
				sysctlbyname(IOSDeviceHardware.HardwareProperty, IntPtr.Zero, pLen, IntPtr.Zero, 0);

				var length = Marshal.ReadInt32(pLen);

				if (length == 0) {
					Marshal.FreeHGlobal(pLen);

					return IOSHardware.Unknown;
				}

				var pStr = Marshal.AllocHGlobal(length);
				sysctlbyname(IOSDeviceHardware.HardwareProperty, pStr, pLen, IntPtr.Zero, 0);

				var hardwareStr = Marshal.PtrToStringAnsi(pStr);

				Marshal.FreeHGlobal(pLen);
				Marshal.FreeHGlobal(pStr);

				if (hardwareStr == "iPhone1,1") return IOSHardware.iPhone;
				if (hardwareStr == "iPhone1,2") return IOSHardware.iPhone3G;
				if (hardwareStr == "iPhone2,1") return IOSHardware.iPhone3GS;
				if (hardwareStr == "iPhone3,1") return IOSHardware.iPhone4;
				if (hardwareStr == "iPhone3,2") return IOSHardware.iPhone4RevA;
				if (hardwareStr == "iPhone3,3") return IOSHardware.iPhone4CDMA;
				if (hardwareStr == "iPhone4,1") return IOSHardware.iPhone4S;
				if (hardwareStr == "iPhone5,1") return IOSHardware.iPhone5GSM;
				if (hardwareStr == "iPhone5,2") return IOSHardware.iPhone5CDMAGSM;

				if (hardwareStr == "iPad1,1") return IOSHardware.iPad;
				if (hardwareStr == "iPad1,2") return IOSHardware.iPad3G;
				if (hardwareStr == "iPad2,1") return IOSHardware.iPad2;
				if (hardwareStr == "iPad2,2") return IOSHardware.iPad2GSM;
				if (hardwareStr == "iPad2,3") return IOSHardware.iPad2CDMA;
				if (hardwareStr == "iPad2,4") return IOSHardware.iPad2RevA;
				if (hardwareStr == "iPad2,5") return IOSHardware.iPadMini;
				if (hardwareStr == "iPad2,6") return IOSHardware.iPadMiniGSM;
				if (hardwareStr == "iPad2,7") return IOSHardware.iPadMiniCDMAGSM;
				if (hardwareStr == "iPad3,1") return IOSHardware.iPad3;
				if (hardwareStr == "iPad3,2") return IOSHardware.iPad3CDMA;
				if (hardwareStr == "iPad3,3") return IOSHardware.iPad3GSM;
				if (hardwareStr == "iPad3,4") return IOSHardware.iPad4;
				if (hardwareStr == "iPad3,5") return IOSHardware.iPad4GSM;
				if (hardwareStr == "iPad3,6") return IOSHardware.iPad4CDMAGSM;

				if (hardwareStr == "iPod1,1") return IOSHardware.iPodTouch1G;
				if (hardwareStr == "iPod2,1") return IOSHardware.iPodTouch2G;
				if (hardwareStr == "iPod3,1") return IOSHardware.iPodTouch3G;
				if (hardwareStr == "iPod4,1") return IOSHardware.iPodTouch4G;
				if (hardwareStr == "iPod5,1") return IOSHardware.iPodTouch5G;

				if (hardwareStr == "i386" || hardwareStr=="x86_64")
				{
					if (UIDevice.CurrentDevice.Model.Contains("iPhone"))
					{
						if(UIScreen.MainScreen.Scale > 1.5f)
							return IOSHardware.iPhoneRetinaSimulator;
						else
							return IOSHardware.iPhoneSimulator;
					}
					else
					{
						if(UIScreen.MainScreen.Scale > 1.5f)
							return IOSHardware.iPadRetinaSimulator;
						else
							return IOSHardware.iPadSimulator;
					}
				}

				return IOSHardware.Unknown;
			}
		}
	}

	#endif

	public static class Profiler
	{
		public static bool Enabled = true;
		public static bool ProfileGPU = false;

		static Profiler()
		{
			// do this to ensure that frame is the first section
			Begin("frame");
			End("frame");
		}

		public static void Begin(string name)
		{
			if (!Enabled)
				return;

			Section section;
			if (!sSections.TryGetValue(name, out section)) {
				section = new Section();
				section.Name = name;
				sSections[name] = section;
				// keep ordered list of sections
				sSectionList.Add(section);
			}

			section.Timer.Start();
			section.Stats.Subtract(PlayScript.Stats.CurrentInstance);
			section.GCCount -= System.GC.CollectionCount(System.GC.MaxGeneration);
		}
		
		public static void End(string name)
		{
			if (!Enabled)
				return;

			Section section;
			if (!sSections.TryGetValue(name, out section)) {
				return;
			}

			section.Timer.Stop();
			section.Stats.Add(PlayScript.Stats.CurrentInstance);
			section.GCCount += System.GC.CollectionCount(System.GC.MaxGeneration);
		}

		public static void Reset()
		{
			if (!Enabled)
				return;

			// reset all sections
			foreach (Section section in sSectionList) {
				section.TotalTime = new TimeSpan();
				section.Timer.Reset();
				section.Stats.Reset();
				section.History.Clear();
			}

			// reset all counters
			sFrameCount = 0;
		}

		public static void OnFrame()
		{
			if (!Enabled)
				return;

			Profiler.End("frame");

#if PLATFORM_MONOMAC || PLATFORM_MONOTOUCH || PLATFORM_MONODROID
			if (ProfileGPU) {
				// this stalls and waits for the gpu to finish 
				PlayScript.Profiler.Begin("gpu");
				GL.Finish();
				PlayScript.Profiler.End("gpu");
			}
#endif

			// update all sections
			foreach (Section section in sSectionList) {
				section.TotalTime += section.Timer.Elapsed;
				if (sDoReport) 
				{
					// pad with zeros if necessary
					while (section.History.Count < sFrameCount) {
						section.History.Add(new SectionHistory());
					}

					var history = new SectionHistory();
					history.Time = section.Timer.Elapsed;
					history.GCCount = section.GCCount;
					section.History.Add(history);
				}
				section.GCCount = 0;
				section.Timer.Reset();
			}

			sFrameCount++;
			if (!sDoReport) {
				// normal profiling, just print out every so often
				if ((sPrintFrameCount!=0) && (sFrameCount >= sPrintFrameCount)) {
					PrintTimes(System.Console.Out);
					Reset();
				}
			} else {
				// report generation, accumulate a specified number of frames and then print report
				if (sFrameCount >= sReportFrameCount) {
					// print out report
					OnEndReport();
					Reset();
					sDoReport = false;
				}
			}

			// check start report countdown
			if (sReportStartDelay > 0) {
				if (--sReportStartDelay == 0) {
					OnStartReport();
				}
			}

			Profiler.Begin("frame");
		}

		/// <summary>
		/// This should be invoked when a loading milestone has occurred
		/// </summary>
		public static void LoadMilestone(string name)
		{
			// store load complete time
			sLoadMilestones[name] = sGlobalTimer.Elapsed;
		}

		public static void StartSession(string reportName, int frameCount, int reportStartDelay = 5)
		{
			if (!Enabled)
				return;

			sReportName = reportName;
			sReportFrameCount = frameCount;
			sReportStartDelay = reportStartDelay;

			Console.WriteLine("Starting profiling session: {0} frames:{1} frameDelay:", reportName, frameCount, reportStartDelay);
		}

		
		public static void PrintTimes(TextWriter tw)
		{
			var str = "profiler: ";
			foreach (Section section in sSectionList) {
				str += section.Name + ":";
				str += (section.TotalTime.TotalMilliseconds / sFrameCount).ToString("0.00");
				str += " ";
			}
			tw.WriteLine(str);
		}

		public static void PrintFullTimes(TextWriter tw)
		{
			foreach (Section section in sSectionList) {

				var total = section.TotalTime;
				var average = total.TotalMilliseconds / sFrameCount;

				// do we have a history?
				if (section.History.Count > 0) {
					// get history in milliseconds, sorted
					var history = section.History.Select(a => a.Time.TotalMilliseconds).OrderBy(a => a).ToList();
					// get min/max/median
					var minTime = history.First();
					var maxTime = history.Last();
					var medianTime = history[history.Count / 2];
					
					tw.WriteLine("{0,-12} total:{1,6} average:{2,6:0.00}ms min:{3,6:0.00}ms max:{4,6:0.00}ms median:{5,6:0.00}ms ",
					             section.Name,
					             total,
					             average,
					             minTime,
					             maxTime,
					             medianTime
					             );

				} else {

					tw.WriteLine("{0,-12} total:{1,6} average:{2,6:0.00}ms",
				             section.Name,
				             total,
				             average
				             );
				}
			}
		}

		public static void PrintHistory(TextWriter tw)
		{
			tw.Write("{0,4} ", "");
			foreach (Section section in sSectionList) 
			{
				tw.Write("{0,12}{1} ", section.Name, ' ');

				// pad history with zeros if necessary
				while (section.History.Count < sFrameCount) {
					section.History.Add(new SectionHistory());
				}
			}
			tw.WriteLine();

			tw.WriteLine("---------------------------");
			for (int frame=0; frame < sFrameCount; frame++)
			{
				tw.Write("{0,4}:", frame);
				int gcCount = 0;
				foreach (Section section in sSectionList) 
				{
					var history = section.History[frame];
					tw.Write("{0,12:0.00}{1} ", history.Time.TotalMilliseconds, (history.GCCount > 0) ? '*' : ' ' );
					gcCount += history.GCCount;
				}
				if (gcCount > 0) {
					tw.Write("    <=== GC occurred");
				}
				tw.WriteLine();
			}
		}

		public static void PrintStats(TextWriter tw)
		{
			foreach (Section section in sSectionList) {
				var dict = section.Stats.ToDictionary(true);
				if (dict.Count > 0) {
					tw.WriteLine("---------- {0} ----------", section.Name);

					// create sorted list of stats results
					var list = dict.ToList();
					list.Sort((a,b) => {return b.Value.CompareTo(a.Value);} );
					foreach (var entry in list) {
						tw.WriteLine(" {0}: {1}", entry.Key, entry.Value);
					}
				}
			}
		}

		public static void PrintLoading(TextWriter tw)
		{
			tw.WriteLine("Offline Mode:  {0}", PlayScript.Player.Offline);
			foreach (var milestone in sLoadMilestones) {
				tw.WriteLine(" {0,-20} {1}", milestone.Key, milestone.Value);
			}
		}


		private static void PrintHistogram(TextWriter tw, double bucketSize, List<double> history, double minRange, double maxRange, double splitThreshold)
		{
			var counts = new List<int>();
			foreach (var time in history) {
				if (time >= minRange && time <= maxRange) {
					// find bucket
					int i = (int)Math.Floor( (time - minRange) / bucketSize);

					// resize counts
					while (i >= counts.Count)
						counts.Add(0);

					// increment histogram
					counts[i]++;
				}
			}

			// print
			double startTime = minRange;
			double endTime = startTime + bucketSize;
			for (int i=0; i < counts.Count; i++) {
				if (counts[i] > 0)
				{
					double percent = (counts[i] * 100.0 / history.Count);
					if ((percent <= splitThreshold) || (bucketSize <= 0.1))
					{
						// print counts for this range
						tw.WriteLine("{0,4}ms->{1,4}ms {2,5} {3,5:0.0}% {4}", 
						             startTime, endTime, counts[i], percent, new string('=', (int)Math.Ceiling(percent) ) );
					}
					else
					{
						// split histogram here if this range has too many data points
						PrintHistogram(tw, bucketSize / 10, history, startTime, endTime, splitThreshold);
					}
				}

				// next range
				startTime += bucketSize;
				endTime   += bucketSize;
			}
		}

		public static void PrintHistograms(TextWriter tw)
		{
			foreach (var section in sSectionList)
			{
				var history = section.History.Select(h => h.Time.TotalMilliseconds).OrderBy(h=>h).ToList();
				if (history.Count == 0)
					continue;

				tw.WriteLine(" --- {0} ---", section.Name);

				if (section.Name == "frame") {
					// hardcode this for "frame"
					double mid = 17 * 2;
					PrintHistogram(tw,  1.0, history, 0.0, mid, 100.0);
					PrintHistogram(tw, 10.0, history, mid, double.MaxValue, 40.0);
				} else {
					PrintHistogram(tw, 10.0, history, 0.0, double.MaxValue, 40.0);
				}
			}
		}

		#region Private
		private static double GetFpsFromMs(double value)
		{
			return (value <= 0.0) ? double.NaN : (1000.0 / value);
		}

		private static void PrintAverageClamped(TextWriter tw, string key, double minimumClampedValue)
		{
			Section section;
			if (sSections.TryGetValue(key, out section) == false)
			{
				return;
			}
			double sum = 0;
			for (int frame=0; frame < sFrameCount; frame++)
			{
				var history = section.History[frame];
				double milliseconds = Math.Max(history.Time.TotalMilliseconds, minimumClampedValue);
				sum += milliseconds;
			}
			sum /= sFrameCount;
			tw.WriteLine("Avg (clamped):{0,6:0.00}ms - {1, 12:0.0} fps        min {2:0.00}ms", sum, GetFpsFromMs(sum), minimumClampedValue);
		}

		private static void PrintPercentile(TextWriter tw, string key, int percentile)
		{
			Section section;
			if (sSections.TryGetValue(key, out section) == false)
			{
				return;
			}
			List<double> history = section.History.Select(a => a.Time.TotalMilliseconds).OrderBy(a => a).ToList();
			int index = (history.Count * percentile) / 100;
			double pTime = history[index];
			tw.WriteLine("{0, 2}p:          {1,6:0.00}ms - {2, 12:0.0} fps", percentile, pTime, GetFpsFromMs(pTime));
		}

		private static void PrintPercentageOfFrames(TextWriter tw, string key, string text, Func<double, bool> func, string additionalText)
		{
			Section section;
			if (sSections.TryGetValue(key, out section) == false)
			{
				return;
			}
			int matchingFrames = 0;
			for (int frame = 0 ; frame < sFrameCount ; frame++)
			{
				var history = section.History[frame];
				if (func(history.Time.TotalMilliseconds))
				{
					matchingFrames++;
				}
			}
			tw.WriteLine("{0}: {1,4:0.0}%                             {2}", text, (double)(100.0 * matchingFrames) / (double)sFrameCount, additionalText);
		}

		private static void PrintReport(TextWriter tw)
		{
			tw.WriteLine("******** Profiling report *********");
			tw.WriteLine("ReportName:    {0}", sReportName);

			#if PLATFORM_MONOTOUCH
			tw.WriteLine("Device:        {0}", UIDevice.CurrentDevice.Name);
			tw.WriteLine("Model:         {0}", IOSDeviceHardware.Version.ToString());
			tw.WriteLine("SystemVersion: {0}", UIDevice.CurrentDevice.SystemVersion);
			tw.WriteLine("Screen Size:   {0}", UIScreen.MainScreen.Bounds);
			tw.WriteLine("Screen Scale:  {0}", UIScreen.MainScreen.Scale);
			#endif
			tw.WriteLine("************* Loading *************");
			PrintLoading(tw);

			tw.WriteLine("************* Session *************");

			tw.WriteLine("Total Frames:  {0}", sFrameCount);
			tw.WriteLine("Total Time:    {0}", sReportTime.Elapsed);
			PerformanceFrameData performanceFrameData = GetPerformanceFrameData();
			PrintAverageClamped(tw, "frame", performanceFrameData.FastFrame);
			PrintPercentile(tw, "frame", 95);
			PrintPercentageOfFrames(tw, "frame", "% Fast Frames", a => (a <= performanceFrameData.FastFrame), string.Format("<={0:0.00}ms", performanceFrameData.FastFrame));
			PrintPercentageOfFrames(tw, "frame", "% Slow frames", a => (a >= performanceFrameData.SlowFrame), string.Format(">={0:0.00}ms", performanceFrameData.SlowFrame));
			tw.WriteLine("GC Count:      {0}", sReportGCCount);

			tw.WriteLine("*********** Timing (ms) ***********");
			PrintFullTimes(tw);

			tw.WriteLine("*********** Histogram ************");
			PrintHistograms(tw);

			tw.WriteLine("***** Dynamic Runtime Stats ******");
			PrintStats(tw);

			tw.WriteLine("************ History *************");
			PrintHistory(tw);

			tw.WriteLine("**********************************");
		}

		/// <summary>
		/// This is called when a profiling report is started
		/// </summary>
		private static void OnStartReport()
		{
			// garbage collect so that it does not impact our session
			System.GC.Collect();

			// reset counters
			Reset();

			// enable the report
			sDoReport = true;

			sReportGCCount = System.GC.CollectionCount(System.GC.MaxGeneration);

			// start global timer
			sReportTime = Stopwatch.StartNew();
		}

		/// <summary>
		/// This is called when a profilng report is ended
		/// </summary>
		private static void OnEndReport()
		{
			sReportTime.Stop();

			sReportGCCount = System.GC.CollectionCount(System.GC.MaxGeneration) - sReportGCCount;

			#if PLATFORM_MONOTOUCH
			// dump profile to file
			var dirs = NSSearchPath.GetDirectories(NSSearchPathDirectory.DocumentDirectory, NSSearchPathDomain.User, true);
			if (dirs.Length > 0) {
				string id = DateTime.Now.ToString("u").Replace(' ', '-');
				var path = Path.Combine(dirs[0], "profile-" + id + ".log");
				Console.WriteLine("Writing profiling report to: {0}", path);
				using (var sw = new StreamWriter(path)) {
					PrintReport(sw);
				}
			}
			#endif

			// print to console 
			PrintReport(System.Console.Out);

			// pause forever
//			Console.WriteLine("Pausing...");
//			for (;;) {
//				System.Threading.Thread.Sleep(1000);
//			}
		}

		private static PerformanceFrameData GetPerformanceFrameData()
		{
			#if PLATFORM_MONOTOUCH
			switch (IOSDeviceHardware.Version)
			{
				case IOSDeviceHardware.IOSHardware.iPhone:
				case IOSDeviceHardware.IOSHardware.iPhone3G:
				case IOSDeviceHardware.IOSHardware.iPhone3GS:
				case IOSDeviceHardware.IOSHardware.iPhone4:
				case IOSDeviceHardware.IOSHardware.iPhone4RevA:
				case IOSDeviceHardware.IOSHardware.iPhone4CDMA:
				case IOSDeviceHardware.IOSHardware.iPodTouch1G:
				case IOSDeviceHardware.IOSHardware.iPodTouch2G:
				case IOSDeviceHardware.IOSHardware.iPodTouch3G:
				case IOSDeviceHardware.IOSHardware.iPodTouch4G:
				case IOSDeviceHardware.IOSHardware.iPad:
				case IOSDeviceHardware.IOSHardware.iPad3G:
					return sSlowPerformanceFrameData;
				default:
					return sDefaultPerformanceFrameData;
			}
			#else
			return sDefaultPerformanceFrameData;
			#endif
		}

		class SectionHistory
		{
			public TimeSpan Time;
			public int 		GCCount;
		};

		// info for a single section
		class Section
		{
			public string               Name;
			public Stopwatch 			Timer = new Stopwatch();
			public TimeSpan				TotalTime;
			public List<SectionHistory>	History = new List<SectionHistory>();
			public Stats 				Stats = new PlayScript.Stats();	
			public int 					GCCount;
		};

		private static Stopwatch sGlobalTimer = Stopwatch.StartNew();
		private static Dictionary<string, TimeSpan> sLoadMilestones = new Dictionary<string, TimeSpan>();

		private static Dictionary<string, Section> sSections = new Dictionary<string, Section>();
		// ordered list of sections
		private static List<Section> sSectionList = new List<Section>();
		private static int sFrameCount  = 0;

		// the frequency to print profiiling info
		private static int sPrintFrameCount  = 60;

		// report handling
		private static bool sDoReport = false;
		private static int  sReportStartDelay = 0;
		private static int  sReportFrameCount = 0;
		private static string sReportName;
		private static Stopwatch sReportTime;
		private static int  sReportGCCount;
		#endregion

		class PerformanceFrameData
		{
			public PerformanceFrameData(double fastFrame, double slowFrame)
			{
				FastFrame = fastFrame;
				SlowFrame = slowFrame;
			}
			public double	FastFrame;
			public double	SlowFrame;
		}

		private static PerformanceFrameData sDefaultPerformanceFrameData = new PerformanceFrameData(fastFrame: 1000.0/60.0, slowFrame: 1000.0/15.0);
		private static PerformanceFrameData sSlowPerformanceFrameData = new PerformanceFrameData(fastFrame: 1000.0/30.0, slowFrame: 1000.0/10.0);
	}
}

