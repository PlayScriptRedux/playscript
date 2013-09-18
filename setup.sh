#!/usr/bin/env bash
# Run this to generate all the initial makefiles, make them and install mono to $HOME/playscript-install
# Ripped off from autogen.sh

./autogen.sh --prefix=$HOME/playscript-install --with-glib=embedded --enable-nls=no --host=x86_64-apple-darwin10
make
make install

