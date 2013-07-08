using System;
using System.Diagnostics;

namespace testrunner
{
	public class TestSet
	{
		public string folder;
		public string[] tests;
	}

	class MainClass
	{
		public string testsPath;

		public string[] testSets = {
			new TestSet {
				folder = "as3/Definitions/Classes",
				tests = {
					"PublicClass"
				}
			}
		};

		public static void Main (string[] args)
		{
			Console.WriteLine ("Running tests!");




		}

		public int RunTest(TestSet testSet, int test)
		{
			string testPath = System.IO.Path.Combine (testsPath, testSet.folder, testSet.tests [test]);

			string testFilesPath = System.IO.Path.Combine (testsPath, testSet.folder, testSet.tests [test], testSet.tests [test]);
			var files = System.IO.Directory.GetFiles (testFilesPath, "*.as");

			ProcessStartInfo psi = new ProcessStartInfo("../../mono/mini/mono", "../class/lib/4.5/mcs.exe " + testFilesPath);
		}
	}
}
