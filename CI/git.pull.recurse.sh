#/bin/bash

git submodule init 
git pull --ff --recurse-submodules=yes
git submodule sync --recursive
git submodule update --recursive
