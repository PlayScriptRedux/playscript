#!/usr/bash
mono --aot /usr/lib/mono/1.0/mscorlib.dll
for i in /usr/lib/mono/gac/*/*/*.dll
  do mono --aot $i
done

#grep "^prefix\ =" Makefile
#prefix = /Users/administrator/mono/play

