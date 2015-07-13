#!/bin/bash
if [ ! -f ${PWD}/packages/nuget.exe ]; then
  curl https://api.nuget.org/downloads/nuget.exe -o packages/nuget.exe
fi
