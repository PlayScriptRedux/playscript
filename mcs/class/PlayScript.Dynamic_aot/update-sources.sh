echo "Updating source files in PlayScript.Dynamic_aot.dll.sources"
rm -rf ./bin 
rm -rf ./obj
rm -rf ../PlayScript.Dynamic/bin 
find . ../PlayScript.Dynamic -name "*.cs" -a \! -wholename "../PlayScript.Dynamic/AssemblyInfo.cs" > PlayScript.Dynamic_aot.dll.sources
