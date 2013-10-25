echo "Updating source files in pscorlib.dll.sources"
rm -rf ./bin 
rm -rf ./obj
find . -name "*.cs" -o -name "*.play" -o -name "*.as" > pscorlib.dll.sources
