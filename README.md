 ![logo](https://raw.githubusercontent.com/PlayScriptRedux/playscript/playscript/PlayscriptLogo.png) 

# PLAYSCRIPT

## Current project status

The CI builds are generously hosted and run on [Travis][travis]

| Git Branch |  Mac OS X |
| :------ | :------: | :------: |
| **playscript** | [![master nix][master-nix-badge]][master-nix] |



## What is PlayScript?

PlayScript is an open source Adobe ActionScript compatible compiler and Flash compatible runtime that runs in the Mono .NET environment, targeting mobile devices through the Xamarin platform.   With a combination of Adobe FlashBuilder for Web and Xamarin Studio for mobile complex large scale cross-mobile-web projects can be developed with full IDE, source debugging and intellisense support on all platforms, with access to the full native mobile API's on the mobile platform.

In addition to accurate ActionScript language support, the PlayScript compiler also supports a new language - PlayScript - which is derived from both C# and ActionScript.  This new language supports all of the features of C#, including generics, properties, events, value types, operator overloading, async programming, linq, while at the same time being upwards compatible with ActionScript.  The PlayScript language can be used to target both desktop and mobile (via Xamarin), and existing Flash/ActionScript code can easily be converted to PlayScript code by simply renaming files from .as to .play, and fixing a few issues related to the stricter syntax and semantics of the PlayScript language.

Finally, the PlayScript runtime supports a full Stage3D compatible implementation of the Flash runtime allowing games that are Stage3D compliant to run with very minor modifications on mobile via the Xamarin/Mono runtime.  A subset of the "display" library is implemented to support Stage3D libraries such as Starling, Away3D, and Feathers, *though there are no plans at the present time to implement the full Flash display system*.  

The PlayScript compiler and runtime provides a complete toolset for building and running ActionScript based games on mobile via the Xamarin Mono runtime or the web via Adobe Flash.

# How is PlayScript Implemented?

The PlayScript compiler is implemented as an additional front end to the Mono MCS compiler.   Installing the PlayScript version of the Mono framework allows you to compile, with the MCS compiler all three langauges: C#, ActionScript, and PlayScript simply by adding files with .cs, .as, and .play file extensions to the MCS command line.

Likewise with the Xamarin Studio IDE, pointing the Xamarin Studio ".NET Frameworks" preferences page selection to the PlayScript Mono framework allows you to simply add .as or .play files to any C# project, and compile them directly into your Xamarin.iOS or Xamarin.Android project.  You can then compile ActionScript or PlayScript code and debug it on the device just as you would any C# code.  ActionScript code can directly call C# code, and vice versa.

# How is the Stage3D Flash Runtime Implemented?

PlayScript includes two libraries: PlayScript.Dynamic_aot.dll, and pscorlib.dll, which implement the basic flash runtime and Stage3D over OpenGL.  Referencing these libraries or the iOS (Monotouch) or Android (MonoDroid) versions of them in your project in Xamarin Studio allows you to run existing Flash Stage3D code with no or little modifications.  (NOTE: A stubbed version of the flash "display" library is included, but is non functional except for various functionality in Bitmap, BitmapData, and TextField).

# Current Status

The PlayScript and ActionScript compiler front ends are fairly stable at this point (given that they are built on top of the very mature Mono compiler and runtime), but there are still several ActionScript language features that are not fully implemented, and certain constructs that are not parsed or compiled as they are in ActionScript.  Work is ongoing to eliminate these last remaining issues and deliver full ActionScript compatibility and any help with [testing](https://github.com/PlayScriptRedux/playscript/issues), writing tests, and/or project [pull-requests](https://github.com/PlayScriptRedux/playscript/blob/playscript/CONTRIBUTING.md) are welcome.

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

TODO: Add Wiki to describe setup/building. For now the best thing to do is review the Travis CI build scripts as if it works for CI it should work for you... ;-)

Windows:

***We don't currently have specific instructions for building on Windows. However, PlayScript is simply part of the regular Mono build and the MCS compiler build by Mono will compile ActionScript and PlayScript .as and .play files. See the MONO build instructions below.***

There is an [open AppVeyor CI enhancement](https://github.com/PlayScriptRedux/playscript/issues/46) to add Window's builds to parallel the Travis CI OS-X and Linux builds, would love some help in getting this done.

http://www.mono-project.com/docs/compiling-mono/windows/

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

Original code contributed to this project by Zynga was released under the Apache open source license and additional code after that has also been released under the same license.

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

## PlayScript Forum

Please post questions/issues on Github [Issues](https://github.com/PlayScriptRedux/playscript/issues)

FYI: The old [Google Group forum](https://groups.google.com/forum/?fromgroups#!forum/playscript) is still available, but we do not have admin access to it and do not track any postings on it.

If enough interest is seen, a new forum can be created, +1 and/or comment on existing [question](https://github.com/PlayScriptRedux/playscript/issues/67) concerning this.


 [travis]: https://travis-ci.org/

 [master-nix-badge]: https://travis-ci.org/PlayScriptRedux/playscript.svg?branch=playscript
 [master-nix]: https://travis-ci.org/PlayScriptRedux/playscript/branches
