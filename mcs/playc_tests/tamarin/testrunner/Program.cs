using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using System.IO;

namespace TestRunner
{
	public class TestResults
	{
		public string TestName;
		public int Passed;
		public int Failed;
		public int ExpectedFailed;
		public int Skipped;
		public bool TestPassed;
	}

	public class Program
	{
		public static XmlDocument TestConfig = new XmlDocument();

		public static string TestXmlFile = "tests.xml";
		public static string BinFolder = "./bin";
		public static string MonoPath = "../../mono/mini/mono";
		public static string LibsPath = "../class/lib/net_4_0/";
		public static string McsPath = "../class/lib/build/mcs.exe";

		public static string[] ReferencedLibs = {
			"mscorlib.dll",
			"System.dll",
			"System.Core.dll",
			"System.Drawing.dll",
			"System.Xml.dll",
			"ICSharpCode.SharpZipLib.dll",
			"System.Json.dll",
			"System.Web.dll",
			"Playscript.Dynamic_aot.dll",
			"pscorlib_aot.dll"
		};
		public static string References;

		public static int Verbosity = 0; // 0=summary only, 1=summary+failures, 2=all results, 3=everything + commands

		public static string Output;

		public const int OK = 0;
		public const int ERROR = 1;

		public static List<TestResults> Results = new List<TestResults> ();
		public static int TotalPassed;
		public static int TotalFailed;

		public static int Main (string[] args)
		{
			Console.WriteLine ("PlayScript TestRunner");

			// Process arguments
			for (int arg = 0; arg < args.Length; arg++) {
				if (args [arg].StartsWith("-v:")) 
					int.TryParse(args[arg].Substring(3), out Verbosity);
				else if (args [arg].ToLower ().EndsWith (".xml"))
					TestXmlFile = args [arg];
			}

			// Set the current working directory to the directory the test.xml file is in
			string fullTestXmlPath = Path.GetFullPath (TestXmlFile);
			string testDir = Path.GetDirectoryName (fullTestXmlPath);
			Directory.SetCurrentDirectory (testDir);

			if (!SetupBinFolder ()) {
				return ERROR;
			}

			TestConfig.Load (fullTestXmlPath);

			var childNodes = TestConfig ["tests"].ChildNodes;
			foreach (XmlNode folderNode in childNodes) {
				if (folderNode is XmlElement && ((XmlElement)folderNode).Name == "folder") {
					XmlElement folderElem = (XmlElement)folderNode;
					string path = folderElem.Attributes ["path"].Value;
					if (path == null) {
						Console.WriteLine ("ERROR: No 'path' attribute found in <folder> tag.");
					}
					Console.WriteLine ("Folder: {0}", path);
					var folderChildNodes = folderElem.ChildNodes;
					foreach (XmlNode testNode in folderChildNodes) {
						if (testNode is XmlElement && ((XmlElement)testNode).Name == "test") {
							XmlElement testElem = (XmlElement)testNode;
							TestResults results = RunTest (path, testElem);
							string passFail = results.TestPassed ? "[PASSED]" : "[FAILED]";
							if (results.TestPassed) {
								TotalPassed++;
							} else {
								TotalFailed++;
							}
							Console.WriteLine ("   {0} Test '{1}': Passed {2} Failed {3} ExpectedFailed {4} Skipped {5}",
							                   passFail, results.TestName, results.Passed, results.Failed, results.ExpectedFailed, results.Skipped);
						} else {
							Console.WriteLine("Error, unrecognized XML tag " + testNode.Name);
						}
					}
				} else {
					Console.WriteLine("Error, unrecognized XML tag " + folderNode.Name);
				}
			}

			Console.WriteLine ("Total Passed {0} Total Failed {1}", TotalPassed, TotalFailed);

			if (TotalFailed > 0) {
				Console.WriteLine ("RESULT: FAILED!");
			} else {
				Console.WriteLine ("RESULT: PASSED!");
			}

			return TotalFailed > 0 ? ERROR : OK;
		}

		public static bool SetupBinFolder()
		{
			if (Directory.Exists (BinFolder)) {
				Directory.Delete (BinFolder, true);
			}

			Directory.CreateDirectory (BinFolder);

			References = "";

			foreach (string refLib in ReferencedLibs) {
				string srcPath = Path.GetFullPath (Path.Combine (LibsPath, refLib));
				string dstPath = Path.GetFullPath (Path.Combine (BinFolder, refLib));
				if (!File.Exists (srcPath)) {
					Console.WriteLine ("Unable to copy library file: " + srcPath + " to test folder " + dstPath);
					return false;
				}
				File.Copy (srcPath, dstPath);
				References = References + " -r:" + dstPath;
			}

			return true;
		}

		public static TestResults RunTest(string path, XmlElement test) 
		{
			TestResults results = new TestResults ();
			Results.Add (results);

			string testName = test.Attributes ["testName"].Value;
			results.TestName = testName;

			if (test.Attributes["expectedFailed"] != null) {
				results.ExpectedFailed = Convert.ToInt32 (test.Attributes["expectedFailed"].Value, 10);
			}

			string extraFilesPath = Path.GetFullPath(Path.Combine (path, testName));
			string[] extraFiles = {};

			if (Directory.Exists (extraFilesPath)) {
				extraFiles = Directory.GetFiles (extraFilesPath, "*.*");
			}

			string outputPath = Path.GetFullPath (Path.Combine (BinFolder, "test.exe"));

			if (BuildTest (path, testName, extraFiles, outputPath) == ERROR) {
				results.TestPassed = false;
				return results;
			}

			if (RunCommand (Path.GetFullPath(MonoPath), outputPath) == ERROR) {
				results.TestPassed = false;
			} else {
				results.TestPassed = true;
			}

			if (Output != null) {
				int passedPos = Output.LastIndexOf ("Passed: ");
				if (passedPos != -1) {
					int.TryParse (Output.Substring (passedPos + 8, 3), out results.Passed);
				}
				int failedPos = Output.LastIndexOf ("Failed: ");
				if (failedPos != -1) {
					int.TryParse (Output.Substring (failedPos + 8, 3), out results.Failed);
				}
				int skippedPos = Output.LastIndexOf ("Skipped: ");
				if (skippedPos != -1) {
					int.TryParse (Output.Substring (skippedPos + 9, 3), out results.Skipped);
				}

				// Allow failures if we expect them..
				if (results.Failed > 1 && results.Failed < results.ExpectedFailed)
					results.TestPassed = true;
			}

			if (((Verbosity > 0 && results.TestPassed == false) || (Verbosity > 1)) && Output != null) {
				Console.WriteLine (Output);
			}

			return results;
		}

		public static int BuildTest(string path, string testName, string[] extraFiles, string outputPath) 
		{
			string testPath = Path.GetFullPath( Path.Combine (path, testName + ".as") );
			string shellPath = Path.GetFullPath ("./shell.as");
			string mcsPath = Path.GetFullPath (McsPath);

			int result = RunCommand (Path.GetFullPath(MonoPath), 
			                   mcsPath + " -noconfig -nostdlib -sdk:4 " + References + " " + shellPath + " " + 
			                   testPath + " " + string.Join (" ", extraFiles) + " -out:" + outputPath);

			if (((Verbosity > 0 && result == ERROR) || (Verbosity > 1)) && Output != null) {
				Console.WriteLine (Output);
			}

			return result;
		}

		public static int RunCommand(string command, string arguments)
		{
			if (Verbosity > 2) {
				Console.WriteLine (command + " " + arguments);
			}
			// Start the child process.
			Output = null;
			Process p = new Process();
			// Redirect the output stream of the child process.
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.RedirectStandardOutput = true;
			p.StartInfo.RedirectStandardError = true;
			p.StartInfo.FileName = command;
			p.StartInfo.Arguments = arguments;
			p.Start();
			// Do not wait for the child process to exit before
			// reading to the end of its redirected stream.
			// p.WaitForExit();
			// Read the output stream first and then wait.
			Output = p.StandardOutput.ReadToEnd();
			p.WaitForExit();
			return p.ExitCode;
		}
	}
}
