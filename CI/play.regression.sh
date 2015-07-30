#!/bin/bash
ROOT=${PWD}
./runtime/mono-wrapper --version
pushd mcs/playc_tests
make clean
make run-test-local 2>&1 | tee -a ${ROOT}/regressions/play.`git log -n 1 --date=short --pretty=format:"%cd-%h"`.log
popd

