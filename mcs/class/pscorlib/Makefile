thisdir = class/pscorlib
include ../../build/rules.make

LIBRARY = pscorlib.dll
LIBRARY_SNK = ../mono.snk
LIBRARY_PACKAGE = none

LIB_MCS_FLAGS = -unsafe+ -d:DYNAMIC_SUPPORT -d:PLATFORM_GENERIC -keyfile:$(LIBRARY_SNK) -r:System.dll -r:System.Core.dll -r:System.Drawing.dll -r:PlayScript.Dynamic.dll -r:System.Xml.dll -r:ICSharpCode.SharpZipLib.dll -r:System.Json.dll  -r:System.Web.dll

NO_TEST = yes

include ../../build/library.make
