echo "Updating source files in pscorlib_aot.dll.sources"
find . ../pscorlib \( -name "*.cs" -o -name "*.play" -o -name "*.as" \) -a \! -wholename "../pscorlib/AssemblyInfo.cs" > pscorlib_aot.dll.sources
