thisdir = class/PlayScript.Dynamic
include ../../build/rules.make

LIBRARY = PlayScript.Dynamic.dll
LIBRARY_SNK = ../mono.snk
LIBRARY_PACKAGE = none

LIB_MCS_FLAGS = -d:DYNAMIC_SUPPORT -keyfile:$(LIBRARY_SNK) -r:System.dll -r:System.Core.dll -r:Mono.CSharp.dll

NO_TEST = yes

include ../../build/library.make
