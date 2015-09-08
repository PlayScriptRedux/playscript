# To produce a debugging parser, use the version that says "-cvt"
JAY_FLAGS=-c
# JAY_FLAGS=-cvt

thisdir := psc
SUBDIRS := 
include ../build/rules.make

EXTRA_DISTFILES = \
	psc.csproj		\
	psc.sln		\
	../../cs-parser.jay		\
	../../ps-parser.jay		\
	psc.exe.sources

ifeq (basic, $(PROFILE))
PROGRAM = psc.exe
sourcefile = psc.exe.sources
else
PROGRAM_USE_INTERMEDIATE_FILE = true
PROGRAM = psc.exe
the_libdir = $(topdir)/class/lib/net_4_5/
LOCAL_MCS_FLAGS += -lib:$(topdir)/class/lib/net_4_5 -debug
endif

LOCAL_MCS_FLAGS += -d:STATIC,NO_SYMBOL_WRITER,NO_AUTHENTICODE,PLAYSCRIPT

PROGRAM_INSTALL_DIR = $(mono_libdir)/mono/4.5

PROGRAM_COMPILE = $(BOOT_COMPILE)

BUILT_SOURCES = ../mcs/cs-parser.cs ../mcs/ps-parser.cs

CLEAN_FILES += y.output

cs-parser.cs: cs-parser.jay $(topdir)/jay/skeleton.cs
	$(topdir)/jay/jay $(JAY_FLAGS) < $(topdir)/jay/skeleton.cs $< > jay-tmp.out && mv jay-tmp.out $@

ps-parser.cs: ps-parser.jay $(topdir)/jay/skeleton.cs
	$(topdir)/jay/jay $(JAY_FLAGS) < $(topdir)/jay/skeleton.cs $< > jay-tmp.out && mv jay-tmp.out $@

KEEP_OUTPUT_FILE_COPY = yes

include ../build/executable.make

debug-parser:
	rm cs-parser.cs
	rm ps-parser.cs
	$(MAKE) JAY_FLAGS=-cvt

