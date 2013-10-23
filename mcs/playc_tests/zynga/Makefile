# Makefile
#
# A build system for executing PlayScript tests on GNU Make (v3.81+)
#

# sh is the default $(SHELL) for GNU Make.
#
# bash is a credible alternative which I often leverage in Makefiles
#
#   echo -e
#   [[
#
SHELL := bash

# If you're not doing an out-of-the-box c/flex/bison textbook project,
# I find that the automatic suffix targets produce confusion.
#
# For larger-scale and sometimes-confusing projects, I prefer to code
# it all by hand than use the invisible magic.
#
.SUFFIXES:

DESTDIR := bin

# Add newline define for pretty printing where needed
define \n


endef

.PHONY: all  # default target, does nothing
all:

.PHONY: clean  # delete all build artifacts
clean:
	rm -rf $(DESTDIR)

THIS_MAKEFILE		:= $(lastword $(MAKEFILE_LIST))
THIS_MAKEFILE_ROOT	:= $(shell dirname $(THIS_MAKEFILE))

PS_HOME			:= $(THIS_MAKEFILE_ROOT)/../../..
PS_LIB			:= $(PS_HOME)/mcs/class/lib/net_4_5
MCS				:= $(PS_HOME)/mcs/class/lib/build/mcs.exe
MONO_WRAPPER	:= $(PS_HOME)/runtime/mono-wrapper

MCS_LIB_NAMES	:= mscorlib.dll
MCS_LIB_NAMES	+= System.dll
MCS_LIB_NAMES	+= System.Core.dll
MCS_LIB_NAMES	+= System.Drawing.dll
MCS_LIB_NAMES	+= System.Xml.dll
MCS_LIB_NAMES	+= ICSharpCode.SharpZipLib.dll
MCS_LIB_NAMES	+= System.Json.dll
MCS_LIB_NAMES	+= System.Web.dll
MCS_LIB_NAMES	+= Playscript.Dynamic_aot.dll
MCS_LIB_NAMES 	+= pscorlib_aot.dll

MCS_LIBS		:= $(MCS_LIB_NAMES:%=$(PS_LIB)/%)

MCS_OPTS		:= $(MCS_LIBS:%=/r:%)
MCS_OPTS		+= /noconfig
MCS_OPTS		+= /nostdlib
MCS_OPTS		+= /sdk:4
MCS_OPTS		+= /nologo
MCS_OPTS		+= /debug:full
MCS_OPTS		+= /optimize-
MCS_OPTS		+= /codepage:utf8
MCS_OPTS		+= /unsafe
MCS_OPTS		+= /psstrict-
MCS_OPTS		+= /define:DEBUG
MCS_OPTS		+= /t:exe

ALL_AS_TEST_FILES	?= $(shell find tests -name '*.as')
BAD_AS_TEST_FILES	:= 
AS_TEST_FILES		:= $(filter-out $(BAD_AS_TEST_FILES),$(ALL_AS_TEST_FILES))
AS_TEST_FILE_NAMES	:= $(AS_TEST_FILES:%.as=%)

ALL_PS_TEST_FILES	?= $(shell find tests -name '*.play')
BAD_PS_TEST_FILES	:= tests/test-ps-004.play
BAD_PS_TEST_FILES	+= tests/test-ps-006.play
BAD_PS_TEST_FILES	+= tests/test-ps-010.play # this test passes locally but fails for Jenkins
BAD_PS_TEST_FILES	+= tests/test-ps-016.play
BAD_PS_TEST_FILES	+= tests/test-ps-017.play
BAD_PS_TEST_FILES	+= tests/test-ps-018.play
BAD_PS_TEST_FILES	+= tests/test-ps-019.play
BAD_PS_TEST_FILES	+= tests/test-ps-020.play
BAD_PS_TEST_FILES	+= tests/test-ps-021.play
BAD_PS_TEST_FILES	+= tests/test-ps-022.play
BAD_PS_TEST_FILES	+= tests/test-ps-023.play
BAD_PS_TEST_FILES	+= tests/test-ps-024.play
BAD_PS_TEST_FILES	+= tests/test-ps-025.play
BAD_PS_TEST_FILES	+= tests/test-ps-026.play
BAD_PS_TEST_FILES	+= tests/test-ps-027.play
BAD_PS_TEST_FILES	+= tests/test-ps-029.play
BAD_PS_TEST_FILES	+= tests/test-ps-030.play
BAD_PS_TEST_FILES	+= tests/test-ps-033.play
BAD_PS_TEST_FILES	+= tests/test-ps-034.play
BAD_PS_TEST_FILES	+= tests/test-ps-036.play
BAD_PS_TEST_FILES	+= tests/test-ps-037.play
BAD_PS_TEST_FILES	+= tests/test-ps-038.play
BAD_PS_TEST_FILES	+= tests/test-ps-039.play
BAD_PS_TEST_FILES	+= tests/test-ps-040.play
PS_TEST_FILES		:= $(filter-out $(BAD_PS_TEST_FILES),$(ALL_PS_TEST_FILES))
PS_TEST_FILE_NAMES	:= $(PS_TEST_FILES:%.play=%)

ALL_CS_TEST_FILES   ?= $(shell find tests -name '*.cs')
BAD_CS_TEST_FILES   :=
CS_TEST_FILES       := $(filter-out $(BAD_CS_TEST_FILES),$(ALL_CS_TEST_FILES))
CS_TEST_FILE_NAMES  := $(CS_TEST_FILES:%.cs=%)

ALL_TEST_FILE_NAMES	:= $(AS_TEST_FILE_NAMES) $(PS_TEST_FILE_NAMES) $(CS_TEST_FILE_NAMES)

.PHONY: test  # build, but don't run, all tests
test:

.PHONY: run-test  # run all tests

# define_compile_test
#
# Define a target to compile an individual test file
#
# $(1) name of the test
# $(2) extension (as or play)
#
define define_compile_test

test: $(DESTDIR)/$(1).exe
$(DESTDIR)/$(1).exe: $(1).$(2) $(MCS_LIBS) $(MCS) $(MONO_WRAPPER)
$(DESTDIR)/$(1).exe: $(MCS_LIB_NAMES:%=$(DESTDIR)/$(dir $(1))%)
$(DESTDIR)/$(1).exe:
	@mkdir -p $$(dir $$@)
	rm -f $$@
	$(MONO_WRAPPER) $(MCS) \
$(MCS_OPTS:%=\${\n}		%) \${\n}		/out:$$@ \
		$(1).$(2) Assert.as Utils.as print.as
	test -e $$@

endef

$(foreach test,$(AS_TEST_FILE_NAMES),$(eval $(call define_compile_test,$(test),as)))
$(foreach test,$(PS_TEST_FILE_NAMES),$(eval $(call define_compile_test,$(test),play)))
$(foreach test,$(CS_TEST_FILE_NAMES),$(eval $(call define_compile_test,$(test),cs)))

# define_copy_libs
#
# Define a target to copy the libs to the build folder for each test
#
# $(1) folder to copy the libs to
#
define define_copy_libs

$(MCS_LIB_NAMES:%=$(DESTDIR)/$(1)%): $(DESTDIR)/$(1)%: $(PS_LIB)/%
	@mkdir -p $$(dir $$@)
	cp $$^ $$@

endef

$(foreach folder,$(sort $(dir $(ALL_TEST_FILE_NAMES))),$(eval $(call define_copy_libs,$(folder))))

# define_run_test
#
# Define a target to execute an individual test
#
# $(1) name of the test
#
define define_run_test

.PHONY: run-$(1)
run-test: run-$(1)
run-$(1): $(DESTDIR)/$(1).exe
run-$(1):
	mono $(DESTDIR)/$(1).exe
endef

$(foreach test,$(ALL_TEST_FILE_NAMES),$(eval $(call define_run_test,$(test))))

#
# The 'help' target is designed so any PHONY target listed with an line '#'
# delimited comment will appear in the help text.
#
.PHONY: help            # basic help
.PHONY: help_verbose    # basic help + clean targets
.PHONY: help_meta
help:         HELP_GREP:=grep -v clean_
help:         help_meta
help_verbose: HELP_GREP:=cat
help_verbose: help_meta
help_meta:
	@echo
	@echo "$(THIS_MAKEFILE): build system for PlayScript tests"
	@echo
	@echo "Supported targets:"
	@echo
	@cat $(lastword $(MAKEFILE_LIST)) | grep '^\.PHONY:' | tr '\t' ' ' | sed -e 's/\.PHONY: //g' -e 's/\.PHONY: //g' -e  's/ \{1,\}/ /g' -e 's/^/  /' | grep '#' | column -c 80 -s \# -t -x | $(HELP_GREP)
	@echo
	@echo "Most targets 'foo' also have a corresponding 'clean_foo'.  Check 'make help_verbose'." | fmt
	@echo
