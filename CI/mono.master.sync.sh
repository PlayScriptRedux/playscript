#!/bin/bash
git checkout master
git fetch http://github.com/mono/mono.git master 
git merge FETCH_HEAD
git push http://github.com/playscriptredux/playscript
