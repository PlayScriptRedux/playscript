thisdir = class/ascorlib
include ../../build/rules.make

LIBRARY = ascorlib.dll
LIBRARY_SNK = ../mono.snk
LIBRARY_PACKAGE = none

LIB_MCS_FLAGS = -d:DYNAMIC_SUPPORT -keyfile:$(LIBRARY_SNK) -r:System.dll -r:System.Core.dll -r:ActionScript.Dynamic.dll -r:System.Xml.dll -r:../zlib.NET_104/bin/zlib.net.dll

NO_TEST = yes

include ../../build/library.make
