#!/bin/bash
ROOT=${PWD}
./runtime/mono-wrapper --version
cd mcs/playc_tests
make clean
make check
exit_code=$?
echo "Exit code :" $exit_code
exit $exit_code
