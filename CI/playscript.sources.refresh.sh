#!/bin/bash
#
# Stand-alone files
#

CurrectBranch=`git rev-parse --abbrev-ref HEAD`
CompareBranch=mono-3.2.6

git diff --name-only ${CompareBranch}..${CurrentBranch} |grep "playshell"
git diff --name-only ${CompareBranch}..${CurrentBranch} |grep "Mono.PlayScript"
git diff --name-only ${CompareBranch}..${CurrentBranch} |grep "PlayScript.Core"
git diff --name-only ${CompareBranch}..${CurrentBranch} |grep "PlayScript.Dynamic\/"
git diff --name-only ${CompareBranch}..${CurrentBranch} |grep "PlayScript.Dynamic_aot"
git diff --name-only ${CompareBranch}..${CurrentBranch} |grep "pscorlib"|grep -v "pscorlib_aot"
git diff --name-only ${CompareBranch}..${CurrentBranch} |grep "pscorlib_aot"
git diff --name-only ${CompareBranch}..${CurrentBranch} |grep "mcs\/playc"|grep -v "_tests"
git diff --name-only ${CompareBranch}..${CurrentBranch} |grep "mcs\/playc_tests"
#
# Makefile files
read -d '' makefile_files <<- EOF
mcs.master/mcs.master.mdw
mcs/tools/Makefile
mcs/tools/playshell/Makefile
mcs/Makefile
mcs/class/Makefile
mcs/class/Mono.Optimization/Makefile
mcs/class/Mono.PlayScript/Makefile
mcs/class/PlayScript.Core/Makefile
mcs/class/PlayScript.Dynamic/Makefile
mcs/class/PlayScript.Dynamic_aot/Makefile
mcs/class/pscorlib/Makefile
mcs/class/pscorlib_aot/Makefile
mcs/mcs/Makefile
mcs/playc/Makefile
mcs/playc_tests/zynga/Makefile
mcs/tests/Makefile
scripts/Makefile.am
EOF
echo "$makefile_files"
#
# Solution/Project files
read -d '' project_files <<- EOF
mcs/class/ICSharpCode.SharpZipLib/ICSharpCode.SharpZipLib-monotouch.csproj
mcs/class/Mono.CSharp/Mono.CSharp-net_4_0.csproj
mcs/class/Mono.Optimization/Mono.Optimization.csproj
mcs/class/Mono.PlayScript/Mono.PlayScript-net_2_0.csproj
mcs/class/Mono.PlayScript/Mono.PlayScript-net_4_0.csproj
mcs/class/Mono.PlayScript/Mono.PlayScript-net_4_5.csproj
mcs/class/Mono.PlayScript/Mono.PlayScript-tests-net_2_0.csproj
mcs/class/Mono.PlayScript/Mono.PlayScript-tests-net_4_0.csproj
mcs/class/Mono.PlayScript/Mono.PlayScript-tests-net_4_5.csproj
mcs/class/Mono.PlayScript/Mono.PlayScript.csproj
mcs/class/Mono.PlayScript/Mono.PlayScript.sln
mcs/class/Mono.PlayScript/Test/Mono.CSharp.Tests.csproj
mcs/class/Mono.PlayScript/Test/Mono.CSharp.Tests.sln
mcs/class/PlayScript.Dynamic/PlayScript.Dynamic-monotouch.csproj
mcs/class/PlayScript.Dynamic/PlayScript.Dynamic-net_4_0.csproj
mcs/class/PlayScript.Dynamic_aot/PlayScript.Dynamic_aot-monodroid.csproj
mcs/class/PlayScript.Dynamic_aot/PlayScript.Dynamic_aot-monotouch.csproj
mcs/class/PlayScript.Dynamic_aot/PlayScript.Dynamic_aot-net.csproj
mcs/class/pscorlib/pscorlib-monotouch.csproj
mcs/class/pscorlib/pscorlib-net_4_0.csproj
mcs/class/pscorlib/pscorlib-net_4_0.sln
mcs/class/pscorlib/pscorlib-net_4_0_monomac.csproj
mcs/class/pscorlib_aot/pscorlib_aot-monodroid.csproj
mcs/class/pscorlib_aot/pscorlib_aot-monotouch.csproj
mcs/class/pscorlib_aot/pscorlib_aot-net.csproj
mcs/class/pscorlib_aot/pscorlib_aot-net.sln
mcs/class/pscorlib_aot/pscorlib_aot-net_monomac.csproj
mcs/mcs/mcs-net_4_0.csproj
mcs/mcs/mcs.csproj
mcs/mcs/mcs.sln
mcs/mcs/test.csproj
mcs/playc/playc.csproj
mcs/playc/playc.sln
mcs/playc/test.csproj
mcs/playc_tests/tamarin/playc_tests.sln
mcs/playc_tests/tamarin/testrunner/testrunner.csproj
mcs/playc_tests/tamarin/testrunner/testrunner.sln
mcs/playc_tests/tamarin/tests.csproj
mcs/tools/linker/Mono.Linker.csproj
EOF
echo "$project_files"
#
# C# Shared files with Mono
# Note: hack, manually remove the two cross-merged files; see commit 6f1e4b547af11cf5c26a8846b2af8c24c0765fcd
git diff --name-only ${CompareBranch}..${CurrentBranch} |grep "mcs\/mcs\/" | grep "\.cs" |grep -v "mcs/mcs/complete.cs"|grep -v "mcs/mcs/cs-tokenizer.cs"
#
# Solution/Project files
read -d '' misc_files <<- EOF
.gitignore
PlayscriptLogo.png
README.md
configure.in
mcs/class/Microsoft.CSharp/Microsoft.CSharp.RuntimeBinder/RuntimeBinderContext.cs
mcs/class/Mono.CSharp/Mono.CSharp.dll.sources
mcs/class/Mono.Optimization/Mono.Optimization.dll.sources
mcs/class/Mono.Optimization/Mono.Optimization/InlineAttribute.cs
mcs/class/Mono.Optimization/Mono.Optimization/Msil.cs
mcs/class/Mono.Optimization/Properties/AssemblyInfo.cs
mcs/class/Mono.PlayScript/.gitignore
mcs/mcs/.gitignore
mcs/mcs/cs-parser.jay
mcs/mcs/mcs
mcs/mcs/mcs.exe.sources
mcs/mcs/options.rsp
mcs/mcs/ps-parser-auto.jay
mcs/mcs/ps-parser.jay
mcs/playc_tests/test.as
mcs/playc_tests/tests.xml
mcs/tools/linker/Mono.Linker.Steps/SweepStep.cs
playscript-full-build.sh
playscript-small-build.sh
scripts/mcs.in
scripts/playc
scripts/playc.in
scripts/script.bat.in
scripts/script.in
mcs/class/System/Microsoft.CSharp/CSharpCodeGenerator.cs
EOF
echo "$misc_files"

# Regresstion testing  files
read -d '' test_files <<- EOF
mcs/tools/compiler-tester/CompilerTester.sln
mcs/tools/compiler-tester/CompilerTester.csproj
mcs/tools/compiler-tester/compiler-tester.cs
EOF
echo "$test_files"

read -d '' license_files <<- EOF
CONTRIBUTING.md
LICENSE
LICENSE.Mono
mcs/LICENSE
mcs/LICENSE.Mono
EOF
echo "$license_files"

