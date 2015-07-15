thisdir = class/Mono.PlayScript
SUBDIRS = 
include ../../build/rules.make

LIBRARY = Mono.PlayScript.dll

LIB_MCS_FLAGS = -r:System.Core.dll -r:System.Xml.dll -r:System.dll 
LIB_MCS_FLAGS += -d:PLAYSCRIPT_REPL

TEST_MCS_FLAGS = -r:System.Core.dll

include ../../build/library.make

