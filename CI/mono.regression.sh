#!/bin/bash
ROOT=${PWD}
./runtime/mono-wrapper --version

# Hack to allow two tests to not cause regressions when only building sdk 4.5
mkdir mcs/class/lib/net_2_0
cp -n mcs/class/lib/net_4_5/Mono.Cecil.* mcs/class/lib/net_2_0

cd mcs/tests
make clean
make check
exit_code=$?
echo "Exit code :" $exit_code
exit $exit_code
