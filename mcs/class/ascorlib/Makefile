thisdir = class/ascorlib
include ../../build/rules.make

LIBRARY = ascorlib.dll
LIBRARY_SNK = ../mono.snk
LIBRARY_PACKAGE = none

LIB_MCS_FLAGS = -keyfile:$(LIBRARY_SNK) -r:System.Core.dll -r:System.dll -r:System.Dynamic.dll -r:Mono.CSharp

NO_TEST = yes

include ../../build/library.make
