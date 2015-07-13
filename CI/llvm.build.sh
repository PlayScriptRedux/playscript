#!/bin/bash

CURDIR=${PWD}
LLVMDIR=mono-llvm

if [ -z ${PlayInstallPath+x} ]
then
  PlayInstallPath=$HOME/mono/play
else
  echo ${PlayInstallPath}
fi

if [ ! -f ${PWD}../${LLVMDIR} ]; then
  git clone git://github.com/mono/llvm.git ${PWD}/../${LLVMDIR}
fi

pushd ../${LLVMDIR}

if [ "${PWD##$i{CURDIR/}}" == "${LLVMDIR}" ]
  git reset --hard
  git clean -d -f
  git checkout --force mono-3-2-4
fi

./configure \
   --prefix=${PlayInstallPath} \
   --enable-optimized \
   --enable-targets="x86 x86_64"

make 
make install

popd

