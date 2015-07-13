#!/bin/bash
ROOT=${PWD}
pushd mcs/tests
make qcheck 2>&1 | tee -a ${ROOT}/regressions/mono.`git log -n 1 --date=short --pretty=format:"%cd-%h"`.log
