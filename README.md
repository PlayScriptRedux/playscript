 ![logo](https://raw.githubusercontent.com/PlayScriptRedux/playscript/playscript/PlayscriptLogo.png) 

# PLAYSCRIPT

## Current project status

The CI builds are generously hosted and run on [Travis][travis]

| Git Branch |  Mac OS X |
| :------ | :------: | :------: |
| **playscript** | [![master nix][master-nix-badge]][master-nix] |



## What is PlayScript?

PlayScript is an open source Adobe ActionScript compatible compiler and Flash compatible runtime that runs in the Mono .NET environment, targeting mobile devices through the Xamarin platform.   With a combination of Adobe FlashBuilder for Web and Xamarin Studio for mobile complex large scale cross-mobile-web projects can be developed with full IDE, source debugging and intellisense support on all platforms, with access to the full native mobile API's on the mobile platform.

The PlayScript compiler also targets both C++ and JavaScript (similar to the Haxe compiler) allowing ActionScript code to be run via JavaScript on the Web, or natively on PC and mobile (with some limitations).  (NOTE: Presently the JS and C++ targets are at an experimental stage)

In addition to accurate ActionScript language support, the PlayScript compiler also supports a new language - PlayScript - which is derived from both C# and ActionScript.  This new language supports all of the features of C#, including generics, properties, events, value types, operator overloading, async programming, linq, while at the same time being upwards compatible with ActionScript.  The PlayScript language can be used to target both web and mobile (via Xamarin and JavaScript), and existing Flash code can easily be converted to PlayScript code by simply renaming files from .as to .play, and fixing a few issues related to the stricter syntax and semantics of the PlayScript language.

Finally, the PlayScript runtime supports a full Stage3D compatible implementation of the Flash runtime allowing games that are Stage3D compliant to run with very minor modifications on mobile via the Xamarin/Mono runtime.  A subset of the "display" library is implemented to support Stage3D libraries such as Starling, Away3D, and Feathers, though there are no plans at the present time to implement the full Flash display system.  

The PlayScript compiler and runtime provides a complete toolset for building and running ActionScript based games on mobile via the Xamarin Mono runtime, on the web via Adobe Flash or JavaScript/HTML5.

# How is PlayScript Implemented?

The PlayScript compiler is implemented as an additional front end to the Mono MCS compiler.   Installing the PlayScript version of the Mono framework allows you to compile, with the MCS compiler all three langauges: C#, ActionScript, and PlayScript simply by adding files with .cs, .as, and .play file extensions to the MCS command line.

Likewise with the Xamarin Studio IDE, pointing the Xamarin Studio ".NET Frameworks" preferences page selection to the PlayScript Mono framework allows you to simply add .as or .play files to any C# project, and compile them directly into your Xamarin.iOS or Xamarin.Android project.  You can then compile ActionScript or PlayScript code and debug it on the device just as you would any C# code.  ActionScript code can directly call C# code, and vice versa.

# How is the Stage3D Flash Runtime Implemented?

PlayScript includes two libraries: PlayScript.Dynamic_aot.dll, and pscorlib.dll, which implement the basic flash runtime and Stage3D over OpenGL.  Referencing these libraries (or the Monotouch or Mono for Android versions of them) in your project in Xamarin Studio allows you to run existing Flash Stage3D code with no modifications.  (NOTE: A stubbed version of the flash "display" library is included, but is non functional except for various functionality in Bitmap, BitmapData, and TextField).

# Current Status

The PlayScript and ActionScript compiler front ends are fairly stable at this point (given that they are built on top of the very mature Mono compiler and runtime), but there are still several ActionScript language features that are not fully implemented, and certain constructs that are not parsed or compiled as they are in ActionScript.  Work is ongoing to eliminate these last remaining issues and deliver full ActionScript compatibility.

### ActionScript support

1. Dynamic classes are compiled but are not implemented yet.
2. The [Embed] tag is not yet implemented.
3. The singleton guard pattern is not supported (using a private parameter to a public constructor).
4. Static and non static members of the same name are not supported.
5. Class and package level statements are not supported.
6. Variety of small bugs which require minor work arounds (these are being eliminated over time).

### PlayScript support

1. Unsafe code is not supported (conflict with the use of the * symbol in ActionScript).
2. Some issues with multi dimensional arrays.
3. JavaScript and C++ targets are experimental.

### Runtime support

1. Much work has been done on the Stage3D library support, and full AGAL to HLSL support has been implemented.
2. Starling and Away3D libraries are functional and most features are supported.
2. Very little work has been done on net, and other core libraries.


# How do I install PlayScript?

#### Binaries

Presently we are iterating very rapidly on the runtime and compiler, and binary releases are out of date almost immediately after they are posted.  

We currently recommend that if you wish to use the current alpha versions of the compiler and runtime, you be prepared to build from source and to regularly pull updates from git and rebuild.

#### Building From Source

Mac:

https://github.com/playscript/playscript-dist/wiki/Developer-Setup

Windows:

***We don't have specific instructions for building on Windows. However, PlayScript is simply part of the regular Mono build and the MCS compiler build by Mono will compile ActionScript and PlayScript .as and .play files. See the MONO build instructions below.***

http://www.mono-project.com/Compiling_Mono_on_Windows


Also, the base pscorlib.dll and PlayScript.Dynamic.dll runtime libraries (minus Stage3D support) will be pre-built and added to the GAC gache in the final mono install.  To use the "monotouch" or "monomac" or "monoandroid" versions of these libraries, use the included .csproj files in the mcs/class folder in this repository.

# How do I use PlayScript from Xamarin Studio?

1. Build the Mono framework from this repo using the Mono build instructions.   Use --prefix=/Users/myname/playscript-mono-inst to install the framework to a reasonable location on your hard disk.
2. Open Xamarin Studio, and select Preferences..
3. Select the .NET runtimes tab.
4. Click the "Add" button, and select the folder where you build the PlayScript mono framework from step 1.
5. Click the "Set as Default" button.
6. Exit Xamarin Studio, then restart.

You should now be able to add .as files and .play files to your projects and compile them.  Note that you must make sure the file is toggled to compile by selecting the "Properties" panel in Xamarin Studio and setting the "Build Action" to compile.

(NOTE: A modified version of MonoDevelop should be available in the playscript-monodevelop repository that includes full support - including syntax highlighting for both .as and .play files.)


## Features:

#### Native Performance

  * Using "unsafe" code.
  * Direct interop with native code (Cocos2D-X, other C+\+ based engines such as Page44, etc).
  * Optimized compiler for JavaScript generation.
  * Optional full C+\+ target with minimal app size and startup overhead.


#### Advanced Tools Support 

  * Complete tool support including Syntax Highlighting and Intellisense in the MonoDevelop IDE.
  * Source Debugging on all platforms (FlashBuilder for Flash).
  * Fast Release mode compiles and rapid iteration.


#### Full Platform API's

  * Complete iOS platform API via Xamarin.iOS and Xamarin.Android
  * Complete Windows/MacOSX API's.
  * Complete integration with UI builder (iOS), and Android GUI builder via Xamarin Studio.


#### Differences between PlayScript and ActionScript

  * PlayScript supports most features of C# 5.
  * PlayScript requires semicolons after all statements.
  * PlayScript uses block scoping for variables.
  * PlayScript requires breaks in switch statements.
  * PlayScript supports generics using the .<> syntax introduced in AS3 with the normal C# feature set.
  * PlayScript supports properties using the "property" keyword with syntax similar to C#.
  * PlayScript supports indexers and operator overloads using the "indexer" and "operator" keywords.
  * PlayScript implements AS3 namespaces by converting them to .NET internal.


#### Differences between PlayScript and CSharp

  * PlayScript requires the use of the "overload" keyword on addtional overload methods (allows more readable JavaScript code by only mangling overload method names).
  * PlayScript does not support using blocks.
  * PlayScript does not support checked, unchecked.
  * PlayScript does not "presently" support unsafe code (though this will be added in the future).  Currently unsafe code can be added to mobile projects via C#.
  * In PlayScript you may not directly access the base properties of Object (ToString(), GetType(), GetHashCode()) unless you cast an object to a System.Object.  Doing this however will make your code incompatible with the C++ or JavaScript target backends.


## License

Code contributed to this project by Zynga is released under the Apache open source license.

## PlayScript Sample Code

```actionscript
// Basic types
var b:byte;
var sb:sbyte;
var s:short;
var us:ushort;
var i:int;
var u:uint;
var l:long;
var ul:ulong;
var f:float;
var d:double;
 
// Conditional compilation
#if DEBUG
#else
#endif
 
// Fixed arrays
var a:int[] = new int[100];
 
// Properties
public property MyProperty:int {
   get { return _myInt; }
   set { _myInt = value; }
}
 
// Events
public event MyEvent;
 
// Delegates
public delegate MyDelegate(i:int):void;
 
// Operators
public static operator - (i:int, j:int):int {
}
 
// Indexers
public indexer this (index:int) {
   get { return _a[index]; }
   set { _a[index] = value; }
}
 
// Generics
public class Foo.<T> {
    public var _f:T;
 
    public function foo<T>(v:T):void {
    }
}

// Async
async function AccessTheWebAsync():Task.<int> 
{ 
    var client:HttpClient= new HttpClient();
    var getStringTask:Task.<String> = client.GetStringAsync("http://msdn.microsoft.com");
    var urlContents:String = await getStringTask;
    return urlContents.Length;
}

```

## PlayScript Google Group

Please join the discussion on Google Groups here:

https://groups.google.com/forum/?fromgroups#!forum/playscript


# MONO README

This is Mono.

	1. Installation
	2. Using Mono
	3. Directory Roadmap
	4. git submodules maintenance

1. Compilation and Installation
===============================

   a. Build Requirements
   ---------------------

	On Itanium, you must obtain libunwind:

		http://www.hpl.hp.com/research/linux/libunwind/download.php4

	On Solaris, make sure that you used GNU tar to unpack this package, as
	Solaris tar will not unpack this correctly, and you will get strange errors.

	On Solaris, make sure that you use the GNU toolchain to build the software.

	Optional dependencies:

		* libgdiplus

		  If you want to get support for System.Drawing, you will need to get
		  Libgdiplus.    This library in turn requires glib and pkg-config:

			* pkg-config

		    	  Available from: http://www.freedesktop.org/Software/pkgconfig

		  	* glib 2.4

		    	  Available from: http://www.gtk.org/

		* libzlib

		  This library and the development headers are required for compression
		  file support in the 2.0 profile.

    b. Building the Software
    ------------------------
  	
	If you obtained this package as an officially released tarball,
	this is very simple, use configure and make:

		./configure --prefix=/usr/local
		make
		make install

	Mono supports a JIT engine on x86, SPARC, SPARCv9, S/390,
	S/390x, AMD64, ARM and PowerPC systems.   

	If you obtained this as a snapshot, you will need an existing
	Mono installation.  To upgrade your installation, unpack both
	mono and mcs:

		tar xzf mcs-XXXX.tar.gz
		tar xzf mono-XXXX.tar.gz
		mv mono-XXX mono
		mv mcs-XXX mcs
		cd mono
		./autogen.sh --prefix=/usr/local
		make

	The Mono build system is silent for most compilation commands.
	To enable a more verbose compile (for example, to pinpoint
	problems in your makefiles or your system) pass the V=1 flag to make, like this:

		 make V=1


    c. Building the software from GIT
    ---------------------------------

	If you are building the software from GIT, make sure that you
	have up-to-date mcs and mono sources:

	   If you are an anonymous user:
		git clone git://github.com/mono/mono.git

           If you are a Mono contributor with read/write privileges:
	        git clone git@github.com:mono/mono.git


	Then, go into the mono directory, and configure:

		cd mono
		./autogen.sh --prefix=/usr/local
		make

	For people with non-standard installations of the auto* utils and of
	pkg-config (common on misconfigured OSX and windows boxes), you could get
	an error like this:

	./configure: line 19176: syntax error near unexpected token `PKG_CHECK_MODULES(BASE_DEPENDENCIES,' ...

	This means that you need to set the ACLOCAL_FLAGS environment var
	when invoking autogen.sh, like this:

		ACLOCAL_FLAGS="-I $acprefix/share/aclocal" ./autogen.sh --prefix=/usr/loca
	
	where $acprefix is the prefix where aclocal has been installed.

	This will automatically go into the mcs/ tree and build the
	binaries there.

	This assumes that you have a working mono installation, and that
	there's a C# compiler named 'mcs', and a corresponding IL
	runtime called 'mono'.  You can use two make variables
	EXTERNAL_MCS and EXTERNAL_RUNTIME to override these.  e.g., you
	can say

	  make EXTERNAL_MCS=/foo/bar/mcs EXTERNAL_RUNTIME=/somewhere/else/mono
	
	If you don't have a working Mono installation
	---------------------------------------------

	If you don't have a working Mono installation, an obvious choice
	is to install the latest released packages of 'mono' for your
	distribution and running autogen.sh; make; make install in the
	mono module directory.

	You can also try a slightly more risky approach: this may not work,
	so start from the released tarball as detailed above.

	This works by first getting the latest version of the 'monolite'
	distribution, which contains just enough to run the 'mcs'
	compiler.  You do this with:

		# Run the following line after ./autogen.sh
		make get-monolite-latest

	This will download and automatically gunzip and untar the
	tarball, and place the files appropriately so that you can then
	just run:

		make EXTERNAL_MCS=${PWD}/mcs/class/lib/monolite/gmcs.exe

	And that will use the files downloaded by 'make get-monolite-latest.

	Testing and Installation
	------------------------

	You can run (part of) the mono and mcs testsuites with the command:

		make check

	All tests should pass.  

	If you want more extensive tests, including those that test the
	class libraries, you need to re-run 'configure' with the
	'--enable-nunit-tests' flag, and try

		make -k check

	Expect to find a few testsuite failures.  As a sanity check, you
	can compare the failures you got with

		https://wrench.mono-project.com/Wrench/

	You can now install mono with:

		make install

	You can verify your installation by using the mono-test-install
	script, it can diagnose some common problems with Mono's install.

	Failure to follow these steps may result in a broken installation. 

    d. Configuration Options
    ------------------------

	The following are the configuration options that someone
	building Mono might want to use:
	
	--with-sgen=yes,no

		Generational GC support: Used to enable or disable the
		compilation of a Mono runtime with the SGen garbage collector.

		On platforms that support it, after building Mono, you
		will have both a mono binary and a mono-sgen binary.
		Mono uses Boehm, while mono-sgen uses the Simple
		Generational GC.

	--with-gc=[boehm, included, sgen, none]

		Selects the default Boehm garbage collector engine to
	  	use, the default is the "included" value.
	
		included: 
			This is the default value, and it's
	  		the most feature complete, it will allow Mono
		  	to use typed allocations and support the
		  	debugger.

			It is essentially a slightly modified Boehm GC

		boehm:
			This is used to use a system-install Boehm GC,
			it is useful to test new features available in
			Boehm GC, but we do not recommend that people
			use this, as it disables a few features.

		none:
			Disables the inclusion of a garbage
		  	collector.  

	--with-tls=__thread,pthread

		Controls how Mono should access thread local storage,
	  	pthread forces Mono to use the pthread APIs, while
	  	__thread uses compiler-optimized access to it.

	  	Although __thread is faster, it requires support from
	  	the compiler, kernel and libc.   Old Linux systems do
	  	not support with __thread.

		This value is typically pre-configured and there is no
	  	need to set it, unless you are trying to debug a
	  	problem.

	--with-sigaltstack=yes,no

		Experimental: Use at your own risk, it is known to
		cause problems with garbage collection and is hard to
	 	reproduce those bugs.

		This controls whether Mono will install a special
	  	signal handler to handle stack overflows.   If set to
	  	"yes", it will turn stack overflows into the
	  	StackOverflowException.  Otherwise when a stack
	  	overflow happens, your program will receive a
	  	segmentation fault.

		The configure script will try to detect if your
	  	operating system supports this.   Some older Linux
	  	systems do not support this feature, or you might want
	  	to override the auto-detection.

	--with-static_mono=yes,no

		This controls whether `mono' should link against a
	  	static library (libmono.a) or a shared library
	  	(libmono.so). 

		This defaults to yes, and will improve the performance
	  	of the `mono' program. 

		This only affects the `mono' binary, the shared
	  	library libmono.so will always be produced for
	  	developers that want to embed the runtime in their
	  	application.

	--with-xen-opt=yes,no

		The default value for this is `yes', and it makes Mono
	  	generate code which might be slightly slower on
	  	average systems, but the resulting executable will run
	  	faster under the Xen virtualization system.

	--with-large-heap=yes,no

		Enable support for GC heaps larger than 3GB.

		This value is set to `no' by default.

	--enable-small-config=yes,no

		Enable some tweaks to reduce memory usage and disk footprint at
		the expense of some capabilities. Typically this means that the
		number of threads that can be created is limited (256), that the
		maxmimum heap size is also reduced (256 MB) and other such limitations
		that still make mono useful, but more suitable to embedded devices
		(like mobile phones).

		This value is set to `no' by default.

	--with-ikvm-native=yes,no

		Controls whether the IKVM JNI interface library is
	  	built or not.  This is used if you are planning on
	  	using the IKVM Java Virtual machine with Mono.

		This defaults to `yes'.

	--with-profile4=yes,no

		Whether you want to build the 4.x profile libraries
		and runtime.

	  	It defaults to `yes'.

	--with-moonlight=yes,no

		Whether you want to generate the Silverlight/Moonlight
		libraries and toolchain in addition to the default
		(1.1 and 2.0 APIs).

		This will produce the `smcs' compiler which will reference
		the Silverlight modified assemblies (mscorlib.dll,
		System.dll, System.Code.dll and System.Xml.Core.dll) and turn
	  	on the LINQ extensions for the compiler.

	--with-moon-gc=boehm,sgen

		Select the GC to use for Moonlight.

		boehm:
			Selects the Boehm Garbage Collector, with the same flags
			as the regular Mono build. This is the default.

		sgen:
			Selects the new SGen Garbage Collector, which provides
			Generational GC support, using the same flags as the
			mono-sgen build.

		This defaults to `boehm'.

	--with-libgdiplus=installed,sibling,<path>

		This is used to configure where Mono should look for
	  	libgdiplus when running the System.Drawing tests.

		It defaults to `installed', which means that the
	  	library is available to Mono through the regular
	  	system setup.

		`sibling' can be used to specify that a libgdiplus
	  	that resides as a sibling of this directory (mono)
	  	should be used.

		Or you can specify a path to a libgdiplus.

	--disable-shared-memory 

		Use this option to disable the use of shared memory in
		Mono (this is equivalent to setting the MONO_DISABLE_SHM
		environment variable, although this removes the feature
		completely).

		Disabling the shared memory support will disable certain
		features like cross-process named mutexes.

	--enable-minimal=LIST

		Use this feature to specify optional runtime
	  	components that you might not want to include. This
	  	is only useful for developers embedding Mono that
	  	require a subset of Mono functionality.

		The list is a comma-separated list of components that
	  	should be removed, these are:

		aot:
			Disables support for the Ahead of Time
	  		compilation.

		attach:
			Support for the Mono.Management assembly and the
			VMAttach API (allowing code to be injected into
			a target VM)

		com:
			Disables COM support.

		debug:
			Drop debugging support.

	 	decimal:
			Disables support for System.Decimal.

		full_messages:
			By default Mono comes with a full table
			of messages for error codes.   This feature
			turns off uncommon error messages and reduces
			the runtime size.

		generics:
			Generics support.  Disabling this will not
			allow Mono to run any 2.0 libraries or
			code that contains generics.

		jit:
			Removes the JIT engine from the build, this reduces
			the executable size, and requires that all code
			executed by the virtual machine be compiled with
			Full AOT before execution.

		large_code:
			Disables support for large assemblies.

		logging:
	  		Disables support for debug logging.

		pinvoke:
			Support for Platform Invocation services,
			disabling this will drop support for any
			libraries using DllImport.

		portability:
			Removes support for MONO_IOMAP, the environment
			variables for simplifying porting applications that 
			are case-insensitive and that mix the Unix and Windows path separators.

		profiler:
			Disables support for the default profiler.

		reflection_emit:
			Drop System.Reflection.Emit support

		reflection_emit_save:
			Drop support for saving dynamically created
			assemblies (AssemblyBuilderAccess.Save) in
			System.Reflection.Emit.

		shadow_copy:
			Disables support for AppDomain's shadow copies
			(you can disable this if you do not plan on 
			using appdomains).

		simd:
			Disables support for the Mono.SIMD intrinsics
			library.

		ssa:
			Disables compilation for the SSA optimization
			framework, and the various SSA-based
		  	optimizations.

	--enable-llvm
	--enable-loadedllvm

		This enables the use of LLVM as a code generation engine
		for Mono.  The LLVM code generator and optimizer will be 
		used instead of Mono's built-in code generator for both
		Just in Time and Ahead of Time compilations.

		See the http://www.mono-project.com/Mono_LLVM for the 
		full details and up-to-date information on this feature.

		You will need to have an LLVM built that Mono can link
		against,

		The --enable-loadedllvm variant will make the llvm backend
		into a runtime-loadable module instead of linking it directly
		into the main mono binary.

	--enable-big-arrays

		This enables the use of arrays whose indexes are larger
		than Int32.MaxValue.   

		By default Mono has the same limitation as .NET on
		Win32 and Win64 and limits array indexes to 32-bit
		values (even on 64-bit systems).

		In certain scenarios where large arrays are required,
		you can pass this flag and Mono will be built to
		support 64-bit arrays.

		This is not the default as it breaks the C embedding
		ABI that we have exposed through the Mono development
		cycle.

	--enable-parallel-mark

		Use this option to enable the garbage collector to use
		multiple CPUs to do its work.  This helps performance
		on multi-CPU machines as the work is divided across CPUS.

		This option is not currently the default as we have
		not done much testing with Mono.

	--enable-dtrace

		On Solaris and MacOS X builds a version of the Mono
		runtime that contains DTrace probes and can
		participate in the system profiling using DTrace.


	--disable-dev-random

		Mono uses /dev/random to obtain good random data for
	  	any source that requires random numbers. If your
	  	system does not support this, you might want to
	  	disable it.

		There are a number of runtime options to control this
	  	also, see the man page.

	--enable-nacl

		This configures the Mono compiler to generate code
		suitable to be used by Google's Native Client:

			 http://code.google.com/p/nativeclient/

		Currently this is used with Mono's AOT engine as
		Native Client does not support JIT engines yet.

2. Using Mono
=============

	Once you have installed the software, you can run a few programs:

	* runtime engine

		mono program.exe

	* C# compiler

		mcs program.cs

	* CIL Disassembler

		monodis program.exe

	See the man pages for mono(1), mint(1), monodis(1) and mcs(2)
	for further details.

3. Directory Roadmap
====================

	docs/
		Technical documents about the Mono runtime.

	data/
		Configuration files installed as part of the Mono runtime.

	mono/
		The core of the Mono Runtime.

		metadata/
			The object system and metadata reader.

		mini/
			The Just in Time Compiler.

		dis/
			CIL executable Disassembler

		cli/
			Common code for the JIT and the interpreter.

		io-layer/
			The I/O layer and system abstraction for 
			emulating the .NET IO model.

		cil/
			Common Intermediate Representation, XML
			definition of the CIL bytecodes.

		interp/
			Interpreter for CLI executables (obsolete).

		arch/
			Architecture specific portions.

	man/

		Manual pages for the various Mono commands and programs.

	samples/

		Some simple sample programs on uses of the Mono
		runtime as an embedded library.   

	scripts/

		Scripts used to invoke Mono and the corresponding program.

	runtime/

		A directory that contains the Makefiles that link the
		mono/ and mcs/ build systems.

	../olive/

		If the directory ../olive is present (as an
		independent checkout) from the Mono module, that
		directory is automatically configured to share the
		same prefix than this module gets.


4. Git submodules maintenance
=============================

Read documentation at http://mono-project.com/Git_Submodule_Maintenance

 [travis]: https://travis-ci.org/

 [master-nix-badge]: https://travis-ci.org/PlayScriptRedux/playscript.svg?branch=playscript
 [master-nix]: https://travis-ci.org/PlayScriptRedux/playscript/branches
