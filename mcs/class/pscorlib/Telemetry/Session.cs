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
		public const int 	MinTimeSpan = 5;

		// telemetry version
		public const string Version = "3,2";

		// telemetry meta (?)
		public const int 	Meta = 293228;


		// returns the time in frequency (microseconds)
		public static int GetTime()
		{
			return ((int)Stopwatch.GetTimestamp()) / sDivisor;
		}

		// returns true if a session is active
		public static bool Connected
		{
			get { return (sOutput != null);}
		}

		public static int BeginSpan()
		{
			return GetTime();
		}

		public static void EndSpan(Amf3String name, int beginTime)
		{
			if (!Connected) return;

			// compute delta and span
			int span;
			int delta = TimeDelta(beginTime, out span);
			if (delta < 0)
				return;

			// write span
			sOutput.WriteObjectHeader(Protocol.Span.ClassDef);
			sOutput.Write(name);
			sOutput.Write(span);
			sOutput.Write(delta);
		}

		public static void EndSpan(string name, int beginTime)
		{
			if (!Connected) return;

			// compute delta and span
			int span;
			int delta = TimeDelta(beginTime, out span);
			if (delta < 0)
				return;

			// write span
			sOutput.WriteObjectHeader(Protocol.Span.ClassDef);
			sOutput.Write(name);
			sOutput.Write(span);
			sOutput.Write(delta);
		}

		public static void EndSpanValue(Amf3String name, int beginTime, object value)
		{
			if (!Connected) return;

			// compute delta and span
			int span;
			int delta = TimeDelta(beginTime, out span);
			if (delta < 0)
				return;

			// write span
			sOutput.WriteObjectHeader(Protocol.SpanValue.ClassDef);
			sOutput.Write(name);
			sOutput.Write(span);
			sOutput.Write(delta);
			sOutput.Write(value);
		}

		public static void EndSpanValue(string name, int beginTime, object value)
		{
			if (!Connected) return;

			// compute delta and span
			int span;
			int delta = TimeDelta(beginTime, out span);
			if (delta < 0)
				return;

			// write span
			sOutput.WriteObjectHeader(Protocol.SpanValue.ClassDef);
			sOutput.Write(name);
			sOutput.Write(span);
			sOutput.Write(delta);
			sOutput.Write(value);
		}

		public static void WriteTime(Amf3String name)
		{
			if (!Connected) return;

			// compute delta
			int delta = TimeDelta();

			sOutput.WriteObjectHeader(Protocol.Time.ClassDef);
			sOutput.Write(name);
			sOutput.Write(delta);
		}

		public static void WriteTime(string name)
		{
			if (!Connected) return;

			// compute delta
			int delta = TimeDelta();

			sOutput.WriteObjectHeader(Protocol.Time.ClassDef);
			sOutput.Write(name);
			sOutput.Write(delta);
		}

		public static void WriteValue(Amf3String name, int value)
		{
			if (!Connected) return;

			sOutput.WriteObjectHeader(Protocol.Value.ClassDef);
			sOutput.Write(name);
			sOutput.Write(value);
		}

		public static void WriteValue(string name, int value)
		{
			if (!Connected) return;

			sOutput.WriteObjectHeader(Protocol.Value.ClassDef);
			sOutput.Write(name);
			sOutput.Write(value);
		}

		public static void WriteValue(Amf3String name, string value)
		{
			if (!Connected) return;

			sOutput.WriteObjectHeader(Protocol.Value.ClassDef);
			sOutput.Write(name);
			sOutput.Write(value);
		}

		public static void WriteValue(string name, string value)
		{
			if (!Connected) return;

			sOutput.WriteObjectHeader(Protocol.Value.ClassDef);
			sOutput.Write(name);
			sOutput.Write(value);
		}

		public static void WriteValue(Amf3String name, object value)
		{
			if (!Connected) return;

			sOutput.WriteObjectHeader(Protocol.Value.ClassDef);
			sOutput.Write(name);
			sOutput.Write(value);
		}

		public static void WriteValue(string name, object value)
		{
			if (!Connected) return;

			sOutput.WriteObjectHeader(Protocol.Value.ClassDef);
			sOutput.Write(name);
			sOutput.Write(value);
		}

		public static void WriteTrace(string trace)
		{
			if (!Connected) return;

			WriteValue(sNameTrace, trace);
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
			TimeDelta();

			var appName = PlayScript.Player.ApplicationClass.Name;
			var swfVersion = 21;
			var swfSize = 4 * 1024 * 1024;

			// write telemetry version
			WriteValue(".tlm.version", Session.Version);
			WriteValue(".tlm.meta", Session.Meta);

			// write player info
			WriteValue(".player.version", "11,8,800,94");
//			WriteValue(".player.airversion", "3.8.0.910");
			WriteValue(".player.type", "Air");
			WriteValue(".player.debugger", flash.system.Capabilities.isDebugger); 
			WriteValue(".player.global.date", new _root.Date().getTime());
			WriteValue(".player.instance", 0);
			WriteValue(".player.scriptplayerversion", swfVersion);

			// write platform info
			WriteValue(".platform.capabilities", flash.system.Capabilities.serverString);
			WriteValue(".platform.cpucount", 4);

			// write gpu info (this is faked)
			WriteValue(".platform.gpu.kind", "opengles2");
			WriteValue(".platform.gpu.vendor", "Imagination Technologies");
			WriteValue(".platform.gpu.renderer", "PowerVR SGX 535");
			WriteValue(".platform.gpu.version", "OpenGL ES 2.0 IMGSGX535-63.24");
			WriteValue(".platform.gpu.shadinglanguageversion", "OpenGL ES GLSL ES 1.0");
			WriteValue(".platform.3d.driverinfo", "OpenGL Vendor=Imagination Technologies Version=OpenGL ES 2.0 IMGSGX535-63.24 Renderer=PowerVR SGX 535 GLSL=OpenGL ES GLSL ES 1.0");

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

			// 	add any additional telemetry processing here which will be counted as "overhead"
//			sSpanTlmDoPlay.Begin();
//			sSpanTlmDoPlay.End();

			Flush();
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
				// flush output stream
				sOutput.Stream.Flush();
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

		// computes the delta time since the last marker and updates the marker position
		private static int TimeDelta()
		{
			// get current ticks
			int time = GetTime();

			// get delta since our last marker
			int delta = time - sLastMarkerTime;

			// update marker
			sLastMarkerTime = time;

			// return delta
			return delta;
		}

		// computes the delta time since the last marker and updates the marker position
		// spanLength will contain the time since beginTime (in microseconds)
		private static int TimeDelta(int beginTime, out int spanLength)
		{
			// get current ticks
			int time = GetTime();

			// get delta since our last marker
			int delta = time - sLastMarkerTime;

			// get span length (elapsed time since begin)
			spanLength = time - beginTime;

			// skip spans that are too short
			if (spanLength < MinTimeSpan) {
				return -1;
			}

			// update marker
			sLastMarkerTime = time;

			// return delta
			return delta;
		}

		private static Amf3Writer 	  sOutput;
		private static MemoryStream   sRecording;

		private static int  		  sLastMarkerTime;
		private static int  		  sDivisor = (int)(Stopwatch.Frequency / Frequency);

		private static readonly Amf3String     sNameTrace   = new Amf3String(".trace");
		private static readonly Amf3String     sNameSwfFrame  = new Amf3String(".swf.frame");
		private static readonly Amf3String     sNameEnter  = new Amf3String(".enter");
//		private static readonly Span 		   sSpanAsActions = new Span(".as.actions");
		private static readonly Span 		   sSpanExit      = new Span(".exit");
//		private static readonly Span 		   sSpanTlmDoPlay = new Span(".tlm.doplay");
		#endregion
	}
}

