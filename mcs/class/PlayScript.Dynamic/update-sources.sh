echo "Updating source files in PlayScript.Dynamic.dll.sources"
rm -rf ./bin
rm -rf ./obj
find . -name "*.cs" | grep -v CSharpBinaryOperationBinder2 > PlayScript.Dynamic.dll.sources
