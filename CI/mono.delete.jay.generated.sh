#!/bin/bash

find . -name "*.jay" |  while read f; do dn="$(dirname "$f")"; fn="$(basename "$f" ".jay")"; echo $dn/$fn.cs | xargs -I genfile rm genfile ; done

