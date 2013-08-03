using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace PlayScript
{
	public static class Profiler
	{
		class Section
		{
			public string               Name;
			public Stopwatch 			Timer = new Stopwatch();
			public Stats 				Stats = new PlayScript.Stats();	
		};

		private static Dictionary<string, Section> sSections = new Dictionary<string, Section>();
		private static int sFrameCount  = 0;
		
		public static void Begin(string name)
		{
			Section section;
			if (!sSections.TryGetValue(name, out section)) {
				section = new Section();
				section.Name = name;
				sSections[name] = section;
			}

			section.Timer.Start();
			section.Stats.Subtract(PlayScript.Stats.CurrentInstance);
		}
		
		public static void End(string name)
		{
			Section section;
			if (!sSections.TryGetValue(name, out section)) {
				return;
			}

			section.Timer.Stop();
			section.Stats.Add(PlayScript.Stats.CurrentInstance);
		}


		public static void Print()
		{
			var str = "times: ";
			foreach (Section section in sSections.Values) {
				str += section.Name + ":";
				str += (section.Timer.Elapsed.TotalMilliseconds / sFrameCount).ToString("0.00");
				str += " ";
			}
			Console.WriteLine(str);

			foreach (Section section in sSections.Values) {
				Console.WriteLine("section: {0} time:{1}", section.Name, (section.Timer.Elapsed.TotalMilliseconds / sFrameCount).ToString("0.00"));
				var dict = section.Stats.ToDictionary(true);

				var list = dict.ToList();
				list.Sort((a,b) => {return b.Value.CompareTo(a.Value);} );
				foreach (var entry in list) {
					Console.WriteLine("\t{0}: {1}", entry.Key, entry.Value);
				}
			}
		}

		public static void Reset()
		{
			foreach (Section section in sSections.Values) {
				section.Timer.Reset();
				section.Stats.Reset();
			}
			sFrameCount = 0;
		}

		public static void OnFrame()
		{
			sFrameCount++;
			if (sFrameCount > 60) {
				Print();
				Reset();
			}
		}

	}
}

