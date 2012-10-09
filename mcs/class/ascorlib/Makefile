thisdir = class/ascorlib
include ../../build/rules.make

LIBRARY = ascorlib.dll
LIBRARY_SNK = ../mono.snk
LIBRARY_PACKAGE = none

LIB_MCS_FLAGS = -d:DYNAMIC_SUPPORT -keyfile:$(LIBRARY_SNK) -r:System.dll -r:System.Core.dll -r:Microsoft.CSharp.dll -r:System.Dynamic.dll -r:Mono.CSharp.dll

NO_TEST = yes

include ../../build/library.make
