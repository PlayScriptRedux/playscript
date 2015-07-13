#!/bin/bash
git checkout master
git fetch upstream
git merge upstream/master
git push origin master

