echo "Updating source files in pscorlib_aot.dll.sources"
rm -rf ./bin 
rm -rf ./obj
rm -rf ../pscorlib/bin 
find . ../pscorlib \( -name "*.cs" -o -name "*.play" -o -name "*.as" \) -a \! -wholename "../pscorlib/AssemblyInfo.cs" > pscorlib_aot.dll.sources
