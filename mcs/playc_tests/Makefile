#
# use make run-test PROFILE=net_2_0
#

thisdir = tests
SUBDIRS =
include ../build/rules.make

DISTFILES = $(wildcard dlls/**/*.cs) $(wildcard dlls/*.cs) $(wildcard dlls/*.inc)
DISTFILES += $(wildcard *.cs) $(wildcard *.il) $(wildcard *.xml) $(wildcard *.inc) $(wildcard *.snk)
DISTFILE += (wildcard play-issues-*) (wildcard as-issues-*)

with_mono_path = MONO_PATH="$(topdir)/class/lib/$(PROFILE)$(PLATFORM_PATH_SEPARATOR)$$MONO_PATH"/

ilasm = $(topdir)/class/lib/$(PROFILE)/ilasm.exe
ILASM = MONO_PATH="$(topdir)/class/lib/$(PROFILE)$(PLATFORM_PATH_SEPARATOR)$$MONO_PATH" $(RUNTIME) $(RUNTIME_FLAGS) $(ilasm)

USE_MCS_FLAGS :=

# mention all targets
all-local $(STD_TARGETS:=-local):

VALID_PROFILE := $(filter net_4_5, $(PROFILE))
ifdef VALID_PROFILE
# casts
bootstrap-cast.exe: gen-cast-test.cs
	$(BOOT_COMPILE) -target:exe /out:$@ $<

casts.cs: bootstrap-cast.exe
	$(with_mono_path) $(RUNTIME) $(RUNTIME_FLAGS) $< >$@

casts-mcs.exe: casts.cs
	$(CSCOMPILE) -target:exe /out:$@ $<

casts-boot.exe: casts.cs
	$(BOOT_COMPILE) -target:exe /out:$@ $<

boot-casts.out: casts-boot.exe
	$(with_mono_path) $(RUNTIME) $(RUNTIME_FLAGS) $< >$@

mcs-casts.out: casts-mcs.exe
	$(with_mono_path) $(RUNTIME) $(RUNTIME_FLAGS) $< >$@

test-casts: boot-casts.out mcs-casts.out
	cmp $^
	-rm -f bootstrap-cast.exe casts.cs casts-boot.exe casts-mcs.exe boot-casts.out mcs-casts.out

ifeq (net_4_5, $(PROFILE))
TEST_PATTERN = '*.as'
DEFINES = -compiler-options:"-d:NET_4_0;NET_4_5 -debug -r:../class/lib/net_4_5/PlayScript.Dynamic_aot.dll -r:../class/lib/net_4_5/pscorlib_aot.dll"
endif

LOCAL_RUNTIME_FLAGS =
COMPILER = $(topdir)/class/lib/$(BUILD_TOOLS_PROFILE)/mcs.exe
TESTER = MONO_RUNTIME='$(RUNTIME)' $(TEST_RUNTIME) $(RUNTIME_FLAGS) $(LOCAL_RUNTIME_FLAGS) $(topdir)/class/lib/$(PROFILE)/playc-tester.exe $(UPDATEIL) $(VERBOSE) $(UPDATEREF)

TEST_ILS := $(wildcard *-lib.il)

# Make mcs first in-order to generate .jay to .cs
build-compiler-lib:
	cd ../mcs && $(MAKE) NO_DIR_CHECK=yes
	cd ../class/Mono.CSharp && $(MAKE) NO_DIR_CHECK=yes
	cd ../class/pscorlib_aot && $(MAKE) NO_DIR_CHECK=yes
	cd ../class/pscorlib && $(MAKE) NO_DIR_CHECK=yes
	cd ../class/PlayScript.Dynamic_aot && $(MAKE) NO_DIR_CHECK=yes
	cd ../class/PlayScript.Dynamic && $(MAKE) NO_DIR_CHECK=yes

qcheck: build-compiler-lib setup qcheck2

as/Assert.dll:
	$(CSCOMPILE) -t:library -d:"NET_4_0;NET_4_5" -r:../class/lib/net_4_5/pscorlib_aot.dll -r:../class/lib/net_4_5/PlayScript.Dynamic_aot.dll -psstrict- -out:as/Assert.dll com.adobe.test/Assert.as com.adobe.test/Utils.as com.adobe.test/print.as

play/Assert.dll:
	$(CSCOMPILE) -t:library -d:"NET_4_0;NET_4_5" -r:../class/lib/net_4_5/pscorlib_aot.dll -r:../class/lib/net_4_5/PlayScript.Dynamic_aot.dll -psstrict- -out:play/Assert.dll com.adobe.test/Assert.as com.adobe.test/Utils.as com.adobe.test/print.as

%.play: setup
	$(TESTER) -mode:pos -files:$@ -compiler:$(COMPILER) $(DEFINES) $(TOPTIONS) -log:play-$(PROFILE).log -il:play-il-$(PROFILE).xml $(DEFINES) $(TOPTIONS)

%.as: setup 
	$(TESTER) -mode:pos -files:$@ -compiler:$(COMPILER) $(DEFINES) $(TOPTIONS) -log:play-$(PROFILE).log -il:play-il-$(PROFILE).xml $(DEFINES) $(TOPTIONS)

astest: setup
	$(TESTER) -mode:pos -files:"as" -compiler:$(COMPILER) -issues:play-issues-$(PROFILE) -log:play-$(PROFILE).log -il:play-il-$(PROFILE).xml $(DEFINES) $(TOPTIONS) 

playtest: setup
	$(TESTER) -mode:pos -files:"play" -compiler:$(COMPILER) -issues:play-issues-$(PROFILE) -log:play-$(PROFILE).log -il:play-il-$(PROFILE).xml $(DEFINES) $(TOPTIONS) 

playscript: setup
	$(TESTER) -mode:pos -files:"playscript" -compiler:$(COMPILER) -issues:play-issues-$(PROFILE) -log:play-$(PROFILE).log -il:play-il-$(PROFILE).xml $(DEFINES) $(TOPTIONS) 

qcheck2: playscript

test-local:
	@:

run-test-local: qcheck

check: run-test-local

endif

clean-local:
	rm -R */*.exe */*.dll */*.mdb *.log

dist-local: dist-default
	rm -f $(distdir)/casts.cs

csproj-local:

%-il.dll: %-il.il
	$(ILASM) /dll $<

%-lib.dll: %-lib.il
	$(ILASM) /dll /out:$@ $<

setup: as/Assert.dll play/Assert.dll

#	$(CSCOMPILE) -t:library 
