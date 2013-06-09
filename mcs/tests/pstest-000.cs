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

		public static int[] a = new int[COUNT];
		public static int[] b = new int[COUNT];

		unsafe public static void Main() {

			var stopwatch = new Stopwatch ();

			for (var i = 0; i < COUNT; i++) {
				a[i] = i;
			}


			// ------------------------------------------------------------------------------------------------------------------------------

			stopwatch.Restart ();

			stopwatch.Start ();

			ForCopy ();

			stopwatch.Stop ();

			Console.WriteLine ("for() copy Time " + stopwatch.ElapsedMilliseconds + "ms " + stopwatch.ElapsedTicks);


			// ------------------------------------------------------------------------------------------------------------------------------


			stopwatch.Restart ();

			stopwatch.Start ();

			MsilCopy ();

			stopwatch.Stop ();

			Console.WriteLine ("Msil copy Time " + stopwatch.ElapsedMilliseconds + "ms " + stopwatch.ElapsedTicks);


			// ------------------------------------------------------------------------------------------------------------------------------


			stopwatch.Restart ();

			stopwatch.Start ();

			ArrayCopy ();

			stopwatch.Stop ();

			Console.WriteLine ("Array.Copy() Time " + stopwatch.ElapsedMilliseconds + "ms " + stopwatch.ElapsedTicks);


			// Results..
			//
			// $ mono -O=all,-shared test2.exe
			// for() copy Time 1561ms 15615744
			// Msil copy Time 538ms 5381151
			// Array.Copy() Time 580ms 5802771
			//
			// $ mono -O=all,-shared --llvm test2.exe
			// for() copy Time 1562ms 15625743
			// Msil copy Time 536ms 5365352
			// Array.Copy() Time 537ms 5375153

			Console.ReadKey ();
		}

		public static void ForCopy()
		{

			for (var c = 0; c < REPEATS; c++) {

				for (var i = 0; i < COUNT; i++) {
					b [i] = a [i];
				}
			}

			// Generates this code x86..
			//
			// 00000958	movl	48(%ebx), %eax
			// 0000095e	movl	(%eax), %eax
			// 00000960	movl	20(%ebx), %ecx
			// 00000966	movl	(%ecx), %ecx
			// 00000968	cmpl	%esi, 12(%ecx)
			// 0000096b	jbe	0x9a9
			// 00000971	leal	16(%ecx,%esi,4), %ecx
			// 00000975	movl	(%ecx), %ecx
			// 00000977	cmpl	%esi, 12(%eax)
			// 0000097a	jbe	0x99d
			// 00000980	leal	16(%eax,%esi,4), %eax
			// 00000984	movl	%ecx, (%eax)
			// 00000986	incl	%esi
			// 00000987	cmpl	$100, %esi
			// 0000098a	jl	0x958
			//
			// Notice the array bounds checks at 96b and 97a.. these both seem to 
			// be costly, and also disrupt the register allocation forcing more spills
			//
		}

		unsafe public static void MsilCopy()
		{

			byte* s = null;
			byte* d = null;

			Msil.LoadAddr (a[0]);
			Msil.Store (s);
			Msil.LoadAddr (b[0]);
			Msil.Store (d);

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

			// Generates this code x86..
			//
			// 00000a08	movl	%esi, %eax
			// 00000a0a	addl	%edi, %eax
			// 00000a0c	movl	-16(%ebp), %ecx    // <-- Ran out of registers here looks like .. if this was a register this would have been optimal
			// 00000a0f	addl	%edi, %ecx
			// 00000a11	movl	4(%ecx), %edx
			// 00000a14	movl	(%ecx), %ecx
			// 00000a16	movl	%edx, 4(%eax)
			// 00000a19	movl	%ecx, (%eax)
			// 00000a1b	addl	$8, %edi
			// 00000a1e	cmpl	$400, %edi
			//
			// Not too shabby

		}

		public static void ArrayCopy()
		{
			for (var c = 0; c < REPEATS; c++) {

				Array.Copy (a, b, COUNT);

			}

			// Generates this code x86..
			//
			// 00000a68	movl	20(%ebx), %eax
			// 00000a6e	movl	(%eax), %eax
			// 00000a70	movl	48(%ebx), %ecx
			// 00000a76	movl	(%ecx), %ecx
			// 00000a78	subl	$4, %esp
			// 00000a7b	pushl	$100
			// 00000a7d	pushl	%ecx
			// 00000a7e	pushl	%eax
			// 00000a7f	calll	plt_System_Array_Copy_System_Array_System_Array_int
			// 00000a84	addl	$16, %esp
			// 00000a87	incl	%edi
			// 00000a88	cmpl	$10000000, %edi
			// 00000a8e	jl	0xa68
			//
			// Not too great, but the System_Array_Copy_System_Array_System_Array_int method calls memcpy.
			//

		}



	}

}
