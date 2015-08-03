#!/bin/bash
set -ev

if [ -z ${TRAVIS+x} ]
then
  brew update
  brew install autoconf automake libtool pkg-config
fi
