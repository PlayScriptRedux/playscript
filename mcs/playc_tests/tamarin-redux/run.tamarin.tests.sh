# Tamarin AVMSHELL
export AVM=${PWD}/bin/`uname -s`-`uname -p`-avmshell
# Apache Flex ActionScript Compiler
export ASC=${PWD}/bin/asc.jar
# ActionScript Byte Code used by AVMSHELL
export SHELLABC=${PWD}/generated-abc/shell_toplevel.abc
export BUILTINABC=${PWD}/generated-abc/builtin.abc

export JAVABIN=`which java`

mkdir bin-aot
export AOTOUT=${PWD}/bin-aot

pushd tests/acceptance

# abcasm
# as3
# e4x
# ecma3
# generated
# misc
# mmgc
# mops
# recursion
# regress
# spidermonkey
# versioning
export EXCLUDEDIRS='abcasm,generated,misc,mmgc,mops,recursion,regress,spidermonkey,versioning,e4x'
export EXCLUDEDIRS='abcasm,generated,misc,mmgc,mops,recursion,regress,spidermonkey,versioning,e4x,ecma3'

#   --aotsdk        location of the AOT sdk used to compile tests to standalone executables.
#   --ascoutput     output the asc commands to ascoutput.log. Does not compile or run any tests.
# 	--aotout ${AOTOUT} \

python ./runtests.py \
 --exclude ${EXCLUDEDIRS} \
 --html \
 --threads 2
popd


