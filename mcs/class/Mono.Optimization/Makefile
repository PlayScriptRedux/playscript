thisdir = class/Mono.Optimization
include ../../build/rules.make

LIBRARY = Mono.Optimization.dll
LIBRARY_SNK = ../mono.snk
LIBRARY_PACKAGE = none

LIB_MCS_FLAGS = -keyfile:$(LIBRARY_SNK) -r:System.dll -unsafe

NO_TEST = yes

include ../../build/library.make
