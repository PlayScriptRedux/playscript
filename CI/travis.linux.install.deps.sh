#!/bin/bash
set -ev

if [ -z ${TRAVIS+x} ]
then
  # Ensure that all required packages are installed.
  sudo apt-get update -qq
  sudo apt-get install -qq git autoconf libtool automake build-essential mono-devel gettext
fi

