thisdir = class/PlayScript.Dynamic_aot
include ../../build/rules.make

LIBRARY = PlayScript.Dynamic_aot.dll
LIBRARY_SNK = ../mono.snk
LIBRARY_PACKAGE = none

LIB_MCS_FLAGS = -d:PLATFORM_GENERIC -keyfile:$(LIBRARY_SNK) -r:System.dll -r:System.Core.dll

NO_TEST = yes

include ../../build/library.make
