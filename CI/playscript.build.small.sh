#!/usr/bin/bash
if [ -z ${PlayInstallPath+x} ]
then 
  echo "var is unset"
  PlayInstallPath=$HOME/mono/play-small
else 
  echo ${PlayInstallPath}
fi

echo "Building a limited PlayScript mono framework to ${PlayInstallPath} folder"
#rm libgc/acinclude.m4
./autogen.sh --with-mcs-docs=no \
	 --with-profile2=no \
	 --with-profile4=no \
	 --with-profile4_5=yes \
	 --with-moonlight=no \
	 --with-tls=posix \
	 --enable-nls=no \
	 --prefix=${PlayInstallPath}

if [ ! -f ${PWD}mcs/class/lib/monolite/gmcs.exe ]; then
    make get-monolite-latest
fi

make EXTERNAL_MCS=${PWD}/mcs/class/lib/monolite/gmcs.exe
make install
