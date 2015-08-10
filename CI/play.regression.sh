#!/bin/bash
ROOT=${PWD}
./runtime/mono-wrapper --version
cd mcs/playc_tests
make clean
#make run-test-local 2>&1 | tee -a ${ROOT}/regressions/play.`git log -n 1 --date=short --pretty=format:"%cd-%h"`.log
make run-test-local
exit_code=$?
echo "Exit code :" $exit_code
exit $exit_code
