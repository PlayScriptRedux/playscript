#!/bin/bash

# If rebuilding and you fail, you can not do a make clean as it trys to rebuild
# the basic.exe bootstrap compiler which causes it just to fail again...,
# so manually delete the jay generated .cs files to prevent other build failures
find . -name "*.jay" |  while read f; do dn="$(dirname "$f")"; fn="$(basename "$f" ".jay")"; echo $dn/$fn.cs | xargs -I genfile rm genfile ; done


