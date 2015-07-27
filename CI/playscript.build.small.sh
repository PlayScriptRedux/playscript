#!/bin/bash
if [ -z ${PlayInstallPath+x} ]
then 
  PlayInstallPath=$HOME/mono/play
else 
  echo ${PlayInstallPath}
fi

# use ccache
export PATH=/usr/local/opt/ccache/libexec:${PATH}

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


# If rebuilding and you fail, you can not do a make clean as it trys to rebuild 
# the basic.exe bootstrap compiler which causes it just to fail again..., 
# so manually delete the jay generated .cs files to prevent other build failures
find . -name "*.jay" |  while read f; do dn="$(dirname "$f")"; fn="$(basename "$f" ".jay")"; echo $dn/$fn.cs | xargs -I genfile rm genfile ; done

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

make EXTERNAL_MCS=${PWD}/mcs/class/lib/monolite/basic.exe

