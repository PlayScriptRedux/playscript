#!/bin/bash
if [ -z ${PlayInstallPath+x} ]
then 
  PlayInstallPath=$HOME/mono/play
else 
  echo ${PlayInstallPath}
fi

#export MONO_USE_LLVM=1
#export PATH=${PlayInstallPath}/bin:$PATH

echo "Building PlayScript mono framework to ${PlayInstallPath} folder"

# Fix for 2.2.4 vs 2.4.6 glibtool
rm libgc/aclocal.m4

# Fix for newest versions of clang compiling older monos
if [ -f configure.in ]; then
	sed -i 'bak' 's|-stack_size,0x800000||g' configure.in
fi
if [ -f configure.ac ]; then
	sed -i 'bak' 's|-stack_size,0x800000||g' configure.ac
fi

./autogen.sh \
	 --with-tls=posix \
	 --enable-nls=no \
	 --with-profile2=no \
	 --with-profile4=no \
	 --with-profile4_5=yes \
	 --with-moonlight=no \
  	 --host=x86_64-apple-darwin10 \
	 --with-glib=embedded \
	 --prefix=${PlayInstallPath}

#	 --enable-loadedllvm=yes \

rm mcs/class/System.XML/System.Xml.XPath/Parser.cs

if [ ! -f ${PWD}/mcs/class/lib/monolite/basic.exe ]; then
    make get-monolite-latest
fi

# Fix for 2.2.4 vs 2.4.6 glibtool
rm libgc/aclocal.m4

make EXTERNAL_MCS=${PWD}/mcs/class/lib/monolite/gmcs.exe
make install

