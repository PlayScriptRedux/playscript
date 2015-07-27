
pushd monodevelop
./configure --profile=mac

# Packaging for OS X
cd main/build/MacOSX 
make

popd

