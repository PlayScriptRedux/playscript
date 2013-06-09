namespace Test
{
	using System;
	using System.Diagnostics;
	using Mono.Optimization;

	// Test instrinsic support.


	public class Test 
	{
		public const int REPEATS = 10000000;
		public const int COUNT = 100;

		unsafe public static void Main() {

			var stopwatch = new Stopwatch ();

			int[] a = new int[COUNT];
			int[] b = new int[COUNT];

			for (var i = 0; i < COUNT; i++) {
				a[i] = i;
			}


			// ------------------------------------------------------------------------------------------------------------------------------

			stopwatch.Restart ();

			stopwatch.Start ();

			for (var c = 0; c < REPEATS; c++) {

				for (var i = 0; i < COUNT; i++) {
					b [i] = a [i];
				}

			}

			stopwatch.Stop ();

			Console.WriteLine ("for() copy Time " + stopwatch.ElapsedMilliseconds + " " + stopwatch.ElapsedTicks);


			// ------------------------------------------------------------------------------------------------------------------------------

			byte* s = null;
			byte* d = null;

			Msil.LoadAddr (a[0]);
			Msil.Store (s);
			Msil.LoadAddr (b[0]);
			Msil.Store (d);

			stopwatch.Restart ();

			stopwatch.Start ();

			for (var c = 0; c < REPEATS; c++) {

				for (var i = 0; i < COUNT * 4; i += 8) {

					Msil.Load (d);
					Msil.Load (i);
					Msil.Emit (Op.Add);

					Msil.Load (s);
					Msil.Load (i);
					Msil.Emit (Op.Add);

					Msil.LoadInd (typeof(System.Int64));

					Msil.StoreInd (typeof(System.Int64));

				}

			}

			stopwatch.Stop ();

			Console.WriteLine ("Msil copy Time " + stopwatch.ElapsedMilliseconds + " " + stopwatch.ElapsedTicks);


			// ------------------------------------------------------------------------------------------------------------------------------


			stopwatch.Restart ();

			stopwatch.Start ();

			for (var c = 0; c < REPEATS; c++) {

				Array.Copy (a, b, COUNT);

			}

			stopwatch.Stop ();

			Console.WriteLine ("Array.Copy() Time " + stopwatch.ElapsedMilliseconds + " " + stopwatch.ElapsedTicks);



			Console.ReadKey ();
		}

	}

}
