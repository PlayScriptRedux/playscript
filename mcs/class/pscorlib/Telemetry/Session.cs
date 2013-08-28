using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using Amf;
using System.Net.Sockets;

namespace Telemetry
{
	public static class Session
	{
		// global enable flag for telemetry, if this is false no connections or recordings will be made
		public static bool 	 Enabled = true;

		// default hostname used for starting network sessions if none is provided to Connect()
		public static string DefaultHostName = "localhost";

		// default port used for starting sessions
		public static int 	 DefaultPort = 7934;

		// configuration file name
		public const string  ConfigFileName = "telemetry.cfg";

		// frequency for all timing values (microseconds)
		public const int 	Frequency = 1000000;

		// minimum span length that can be written (any spans shorter than this will be discarded)
		public const int 	MinTimeSpan = 5000;

		// telemetry version
		public const string Version = "3,2";

		// telemetry meta (?)
		public const int 	Meta = 293228;


		// returns true if a session is active
		public static bool Connected
		{
			get { return (sOutput != null);}
		}

		public static long BeginSpan()
		{
			return Stopwatch.GetTimestamp();
		}

		public static void EndSpan(object name, long beginTime)
		{
			if (!Connected) return;

			// get current time (in nano-seconds)
			long time = Stopwatch.GetTimestamp();

			// get span length (in nano-seconds)
			long span = (time - beginTime);

			// skip spans that are too short
			if (span < MinTimeSpan) {
				return;
			}

			// write entry
			AddEntry(time, span, name, null);
		}

		public static void EndSpanValue(object name, long beginTime, object value)
		{
			if (!Connected) return;

			// get current time (in nano-seconds)
			long time = Stopwatch.GetTimestamp();

			// get span length (in nano-seconds)
			long span = (time - beginTime);

			// skip spans that are too short
			if (span < MinTimeSpan) {
				return;
			}

			// write entry
			AddEntry(time, span, name, value);
		}

		public static void WriteTime(object name)
		{
			if (!Connected) return;

			// get current time (in nano-seconds)
			long time = Stopwatch.GetTimestamp();

			// write entry
			AddEntry(time, LogTime, name, null);
		}

		public static void WriteValue(object name, object value)
		{
			if (!Connected) return;

			// write entry
			AddEntry(0, LogValue, name, value);
		}

		public static void WriteTrace(string trace)
		{
			if (!Connected) return;

			long time = Stopwatch.GetTimestamp();
			AddEntry(time, 0, sNameTrace, trace);
		}


		public static void WriteSWFStats(string name, int width, int height, int frameRate, int version, int size)
		{
			if (!Connected) return;

			WriteValue(".swf.name", name);
			WriteValue(".swf.rate", (int)(Frequency / frameRate));
			WriteValue(".swf.vm", 3);
			WriteValue(".swf.width", width);
			WriteValue(".swf.height", height);
			WriteValue(".swf.playerversion", version);
			WriteValue(".swf.size", size);
		}
		
		private static void BeginSession(Stream stream)
		{
			// create AMF writer from stream
			sOutput = new Amf3Writer(stream);

			// reset time marker
			ResetLog();

			var appName = PlayScript.Player.ApplicationClass.Name;
			var swfVersion = 21;
			var swfSize = 4 * 1024 * 1024;

			// write telemetry version
			WriteValue(".tlm.version", Session.Version);
			WriteValue(".tlm.meta", Session.Meta);

			// write player info
			WriteValue(".player.version", "11,8,800,94");
//			WriteValue(".player.airversion", "3.8.0.910");
			WriteValue(".player.type", "PlayScript");
			WriteValue(".player.debugger", flash.system.Capabilities.isDebugger); 
			WriteValue(".player.global.date", new _root.Date().getTime());
			WriteValue(".player.instance", 0);
			WriteValue(".player.scriptplayerversion", swfVersion);

#if PLATFORM_MONOMAC
			// write platform info (this is faked)
			WriteValue(".platform.capabilities", "&M=Adobe%20Macintosh&R=1920x1200&COL=color&AR=1.0&OS=Mac%20OS%2010.7.4&ARCH=x86&L=en&PR32=t&PR64=t&LS=en;ja;fr;de;es;it;pt;pt-PT;nl;sv;nb;da;fi;ru;pl;zh-Hans;zh-Hant;ko;ar;cs;hu;tr");
			WriteValue(".platform.cpucount", 4);

			// write gpu info (this is faked)
			WriteValue(".platform.gpu.kind", "opengl");

#else
			// write platform info (this is faked)
			WriteValue(".platform.capabilities", "&M=Adobe iOS&R=640x960&COL=color&AR=1&OS=iPhone OS 6.1 iPhone5,1&ARCH=ARM&L=en&IME=false&PR32=true&PR64=false&LS=en;fr;de;ja;nl;it;es;pt;pt-PT;da;fi;nb;sv;ko;zh-Hans;zh-Hant;ru;pl;tr;uk;ar;hr;cs;el;he;ro;sk;th;id;ms;en-GB;ca;hu;vi");
			WriteValue(".platform.cpucount", 2);

			// write gpu info (this is faked)
			WriteValue(".platform.gpu.kind", "opengles2");
			WriteValue(".platform.gpu.vendor", "Imagination Technologies");
			WriteValue(".platform.gpu.renderer", "PowerVR SGX 535");
			WriteValue(".platform.gpu.version", "OpenGL ES 2.0 IMGSGX535-63.24");
			WriteValue(".platform.gpu.shadinglanguageversion", "OpenGL ES GLSL ES 1.0");
			WriteValue(".platform.3d.driverinfo", "OpenGL Vendor=Imagination Technologies Version=OpenGL ES 2.0 IMGSGX535-63.24 Renderer=PowerVR SGX 535 GLSL=OpenGL ES GLSL ES 1.0");
#endif


			// write memory stats
			WriteValue(".mem.total", 8 * 1024);
			WriteValue(".mem.used", 4 * 1024);
			WriteValue(".mem.managed", 0);
			WriteValue(".mem.managed.used", 0);
			WriteValue(".mem.telemetry.overhead", 0);

			// write telemetry categories
			WriteValue(".tlm.category.disable",  "3D");
			WriteValue(".tlm.category.disable",  "sampler");
			WriteValue(".tlm.category.disable",  "displayobjects");
			WriteValue(".tlm.category.disable",  "alloctraces");
			WriteValue(".tlm.category.disable",  "allalloctraces");
			WriteValue(".tlm.category.enable",   "customMetrics");

			WriteValue(".network.loadmovie", "app:/" + appName );
			WriteValue(".rend.display.mode", "auto");

			// SWF startup timestamp
			WriteTime(".swf.start");

			// write swf stats
			WriteSWFStats(appName, 800, 600, 60, swfVersion, swfSize);

			// write memory stats
			WriteMemoryStats();

			// start detailed metrics
			WriteValue(".tlm.category.start", "customMetrics");

			// enable 'advanced telemetry'
			WriteValue(".tlm.detailedMetrics.start", true);

			Flush();
		}

		private static void EndSession()
		{
			if (!Connected)	return;

			WriteValue(".tlm.date", new _root.Date().getTime());
			WriteValue(".tlm.optimize.exit3DStandbyModeTime", 0);
			WriteValue(".tlm.optimize.selectionEnd", false);
			WriteValue(".tlm.optimize.selectionStart", false);
			WriteValue(".tlm.optimize.startedIn3DStandbyMode", false);
			WriteValue(".tlm.optimize.threeDStandbyModeHasExited", false);

			Flush();

			// close session stream
			if (sOutput!= null && (sOutput.Stream != sRecording)) {
				sOutput.Stream.Close();
			}

			sOutput = null;
		}

		public static void OnBeginFrame()
		{
			if (!Connected) return;

			// emit a timecode for '.enter'
			WriteTime(sNameEnter);

			// swf frame must be written here
			WriteValue(sNameSwfFrame, 0);

			// begin a .exit span
			sSpanExit.Begin();
		}

		public static void OnEndFrame()
		{
			if (!Connected) return;

			// end .exit
			if (sSpanExit.IsInSpan) {
				sSpanExit.End();
			}

			sSpanTlmDoPlay.Begin();
			Flush();
			sSpanTlmDoPlay.End();
		}

		public static void OnResize(int width, int height)
		{
			if (!Connected)	return;

			var rect = new Telemetry.Protocol.Rect();
			rect.xmin = 0;
			rect.ymin = 0;
			rect.xmax = width;
			rect.ymax = height;
			WriteValue(".player.view.resize", rect);
		}

		public static void WriteMemoryStats()
		{
			if (!Connected)	return;

			// memory stats
			// TODO: figure these out
			WriteValue(".mem.total", 8 * 1024);
			WriteValue(".mem.used", 4 * 1024);
			WriteValue(".mem.managed", 0);
			WriteValue(".mem.managed.used", 0);
			WriteValue(".mem.bitmap", 0);
			WriteValue(".mem.bitmap.display", 0);
			WriteValue(".mem.script", 0);
			WriteValue(".mem.network.shared", 0);
			WriteValue(".mem.telemetry.overhead", 0);
		}

		// starts a telemetry session and writes telemetry data over the network
		public static bool Connect()
		{
			return Connect(DefaultHostName, DefaultPort);
		}

		// starts a telemetry session and writes telemetry data over the network
		public static bool Connect(string hostname, int port)
		{
			if (!Enabled) {
				// telemetry is not enabled
				Console.WriteLine("Telemetry: did not connect, telemetry not enabled");
				return false;
			}

			if (Connected) {
				// already connected...
				Console.WriteLine("Telemetry: already connected, disconnect first");
				return true;
			}

			Console.WriteLine("Telemetry: connecting to {0}:{1}...", hostname, port);
			try
			{
				// open network connection
				var client = new TcpClient(hostname, port);
				var buffered = new BufferedStream(client.GetStream());
				// begin session
				BeginSession(buffered);
				Console.WriteLine("Telemetry: connected!");
				return true;
			}
			catch 
			{
				Console.WriteLine("Telemetry: connection failed");
				return false;
			}
		}

		// starts a telemetry session and writes telemetry data to a file
		public static bool ConnectToFile(string outputFilePath)
		{
			if (!Enabled) {
				// telemetry is not enabled
				Console.WriteLine("Telemetry: could not write to file, telemetry not enabled");
				return false;
			}

			if (Connected) {
				// already connected...
				Console.WriteLine("Telemetry: already connected, disconnect first");
				return true;
			}

			try
			{
				// open file stream
				var fs = File.Open(outputFilePath, FileMode.Create);
				var buffered = new BufferedStream(fs);
				// begin session
				BeginSession(buffered);
				Console.WriteLine("Telemetry: writing to file {0}", outputFilePath);
				return true;
			}
			catch
			{
				Console.WriteLine("Telemetry: could not open file for writing: {0}", outputFilePath);
				return false;
			}
		}

		// disconnects the current session
		public static void Disconnect()
		{
			if (!Connected) {
				return ;
			}

			EndSession();
		}

		// begins recording telemetry data to a growable memory buffer
		public static bool StartRecording(int bufferCapacity = 256 * 1024)
		{
			if (!Enabled) {
				// telemetry is not enabled
				Console.WriteLine("Telemetry: did record, telemetry not enabled");
				return false;
			}

			if (Connected) {
				// already connected...
				Console.WriteLine("Telemetry: could not record, already connected");
				return false;
			}

			// record to a new memory stream
			sRecording = new MemoryStream(bufferCapacity);

			Console.WriteLine("Telemetry: began recording to memory buffer");

			// start session
			BeginSession(sRecording);
			return true;
		}

		// ends a recording session and returns memory stream containing the data
		public static MemoryStream EndRecording()
		{
			var recording = sRecording;
			if (recording != null) {
				// stop recording
				EndSession();

				sRecording = null;
				Console.WriteLine("Telemetry: end recording");
			}
			// return recording
			return recording;
		}

		// sends a recording (stored in a stream) over the network
		public static void SendRecordingOverNetwork(Stream stream)
		{
			SendRecordingOverNetwork(stream, DefaultHostName, DefaultPort);
		}

		// sends a recording (stored in a stream) over the network
		public static void SendRecordingOverNetwork(Stream stream, string hostname, int port)
		{
			try {
				Console.WriteLine("Telemetry: network sending {0} bytes to {1}:{2}", stream.Length, hostname, port);
				using (var client = new TcpClient(hostname, port)) {
					using (var ns = client.GetStream()) {
						stream.Position = 0;

						var buffer = new byte[16 * 1024];
						int count;
						// read from input stream
						while ( (count=stream.Read(buffer, 0, buffer.Length)) > 0)
						{
							// write to network stream
							ns.Write(buffer, 0, count);
							ns.Flush();
							// we can't feed data too fast or else the server will ignore it 
							System.Threading.Thread.Sleep(250);
						}
					}
				}
				Console.WriteLine("Telemetry: network send completed!");
			} catch {
				Console.WriteLine("Telemetry: network send failed");
			}
		}

		// saves a recording (stored in a stream) to a file
		public static void SaveRecordingToFile(Stream stream, string outputFilePath)
		{
			try {
				Console.WriteLine("Telemetry: writing {0} bytes to file {1}", stream.Length, outputFilePath);
				using (var fs = File.Open(outputFilePath, FileMode.Create)) {
					stream.Position = 0;
					stream.CopyTo(fs);
				}
				Console.WriteLine("Telemetry: file write completed!");
			} catch {
				Console.WriteLine("Telemetry: file write failed");
			}		
		}

		// flushes all accumulated data to the output stream (over the network or to a file)
		public static void Flush()
		{
			if (sOutput != null) {

				try
				{
					// flush log to AMF output
					FlushLog(sOutput);

					// flush output stream
					sOutput.Stream.Flush();
				}
				catch 
				{
					// error writing to socket?
					sOutput = null;
				}
			}
		}

		public static bool LoadConfig(string configPath)
		{
			try {
				string path = PlayScript.Player.TryResolveResourcePath(configPath);
				if (path == null) {
					// config not found
					return false;
				}

				// read all config lines
				var lines = File.ReadAllLines(path);
				foreach (var line in lines) {
					var split = line.Split(new char[] {'='}, 2);
					if (split.Length == 2) {
						var name = split[0].Trim();
						var value = split[1].Trim();
						switch (name) {
							case "TelemetryAddress":
								{
									var split2 = value.Split(new char[] {':'}, 2);
									// get hostname
									DefaultHostName = split2[0];
									if (split2.Length >= 2) {
										// get port
										int.TryParse(split2[1], out DefaultPort);
									}
									break;
								}

							case "SamplerEnabled":
							case "Stage3DCapture":
							case "DisplayObjectCapture":
								break;
							default:
								break;
						}
					}
				}
				// 
				return true;
			} catch {
				// exception
				return false;
			}
		}

		// the session init loads the telemetry configuration and connects
		public static void Init()
		{
			// load configuration
			if (LoadConfig(ConfigFileName)) {
				// if we have configuration, then connect
				Connect();
			}
		}

		#region Private
		struct LogEntry
		{
			public long 	Time;		// time of entry (in nano-seconds)
			public long 	Span;		// span length (in nano-seconds) for Span and SpanValue 
			public object 	Name;		// string or Amf3String
			public object	Value;		// non-null if SpanValue or Value
		};

		private const long LogValue = -2;
		private const long LogTime = -1;

		private static void WriteLogEntry(ref LogEntry entry, ref int timeBase, Amf3Writer output)
		{
			if (entry.Span == LogValue) {
				// emit Value
				output.WriteObjectHeader(Protocol.Value.ClassDef);
				output.Write(entry.Name);
				output.Write(entry.Value);
			} else 	if (entry.Span == LogTime) {
				// emit Time
				// NOTE: this requires a 64-bit division on ARM
				int time  = (int)(entry.Time / sDivisor);
				int delta = time - timeBase; 
				timeBase = time;

				output.WriteObjectHeader(Protocol.Time.ClassDef);
				output.Write(entry.Name);
				output.Write(delta);
			} else {
				// emit Span or SpanValue
				// convert times to microseconds for output
				// NOTE: this requires a 64-bit division on ARM
				int time      = (int)(entry.Time / sDivisor);
				int beginTime = (int)((entry.Time - entry.Span) / sDivisor);

				// compute span and delta in microseconds
				// this must be done this exact way to preserve rounding errors across spans
				// if not, the server may produce an error if a span exceeds its expected length
				int span  = time - beginTime;
				int delta = time - timeBase; 
				timeBase = time;

				if (entry.Value == null) {
					output.WriteObjectHeader(Protocol.Span.ClassDef);
					output.Write(entry.Name);
					output.Write(span);
					output.Write(delta);
				} else {
					output.WriteObjectHeader(Protocol.SpanValue.ClassDef);
					output.Write(entry.Name);
					output.Write(span);
					output.Write(delta);
					output.Write(entry.Value);
				}
			}
		}

		private static void FlushLog(Amf3Writer output)
		{
			// write all log entries
			for (int i=0; i < sLogCount; i++) {
				WriteLogEntry(ref sLog[i], ref sLogTimeBase, output);
			}
			// clear log
			sLogCount = 0;
		}

		private static void ResetLog()
		{
			// reset log
			sLogCount    = 0;
			// reset timebase
			sLogTimeBase = (int)(Stopwatch.GetTimestamp() / sDivisor);
		}

		private static void AddEntry(long time, long span, object name, object value)
		{
			if (sLog == null) {
				// create log
				sLog = new LogEntry[4 * 1024];
			}

			if (sLogCount >= sLog.Length) {
				// grow geometrically
				int newLength = sLog.Length * 2;
				var newLog = new LogEntry[newLength];
				Array.Copy(sLog, newLog, sLog.Length);
				sLog = newLog;
			}

			// add entry to log
			int i = sLogCount++;
			sLog[i].Time = time;
			sLog[i].Span = span;
			sLog[i].Name = name;
			sLog[i].Value = value;
		}

		// fast intermediate log used for storing data within a frame as array of packed structs
		// this gets flushed to the active AMF stream each frame
		private static LogEntry[]	  sLog;
		private static int 			  sLogCount = 0;
		private static int 			  sLogTimeBase;

		private static Amf3Writer 	  sOutput;
		private static MemoryStream   sRecording;
		private static int  		  sDivisor = (int)(Stopwatch.Frequency / Frequency);

		private static readonly Amf3String     sNameTrace   = new Amf3String(".trace");
		private static readonly Amf3String     sNameSwfFrame  = new Amf3String(".swf.frame");
		private static readonly Amf3String     sNameEnter  = new Amf3String(".enter");
		private static readonly Span 		   sSpanExit      = new Span(".exit");
		private static readonly Span 		   sSpanTlmDoPlay = new Span(".tlm.doplay");
		#endregion
	}
}

