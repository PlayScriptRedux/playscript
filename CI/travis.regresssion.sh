#!/bin/bash

build_exit=0
play_exit=0
mono_exit=0

./CI/play.${TRAVIS_OS_NAME}.build.small.i386.sh
build_exit=$?
if [ $build_exit -eq 0 ]
then
  echo "Build passed, running regressions"
  ./CI/mono.regression.sh
  mono_exit=$? 
  ./CI/play.regression.sh
  play_exit=$?
else
  echo "~~~~~~~~~~~~~~~~~~~~~~~~~~~~"
  echo "     Mono Build Failed      "
  echo "~~~~~~~~~~~~~~~~~~~~~~~~~~~~"
fi

if [ $mono_exit -eq 1 ]
then
  echo "~~~~~~~~~~~~~~~~~~~~~~~~~~~~"
  echo "   Mono Regression Failed   "
  echo "~~~~~~~~~~~~~~~~~~~~~~~~~~~~"
fi

if [ $play_exit -eq 1 ]
then
  echo "~~~~~~~~~~~~~~~~~~~~~~~~~~~~"
  echo "   Play Regression Failed   "
  echo "~~~~~~~~~~~~~~~~~~~~~~~~~~~~"
fi

exit_code=var=$(( build_exit + play_exit + mono_exit  ))

exit $(( exit_code ))
