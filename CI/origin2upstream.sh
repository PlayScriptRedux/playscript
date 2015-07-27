# https://github.com/PlayScriptRedux/playscriptredux.git

echo "For org members, all changes come through pull-requests, never direct push\n\n"

export regex=
# Export as the foreach via git is a new shell
export GITHUBREDUX=${GITHUBREDUX:=PlayScriptRedux}
export GITHUBUSERNAME=${GITHUBUSERNAME:=`git config --global user.name`}

# Disable pushing to upstream
git remote rename origin upstream
git remote set-url --push upstream DISABLE

# Create new origins based on the github forks that you should have ;-)
GITHUBURI=`git config remote.upstream.url`
NEWGITHUBURI=${GITHUBURI/${GITHUBREDUX}/${GITHUBUSERNAME}}
git remote add origin ${NEWGITHUBURI}

# no recursive for each
git submodule foreach --quiet ' \
  echo "\nUpstream change: ${name}"; \
  git remote rename origin upstream; \
  GITHUBURI=`git config remote.upstream.url`; \
  echo "    Upstream URI:\t ${GITHUBURI}"; \
  echo "  Disabling push:\t ${GITHUBURI}"; \
  git remote set-url --push upstream DISABLE; \
  echo "Set new origin info:"; \
  echo "  Upstream URI:\t ${GITHUBURI}"; \
  foo=${GITHUBURI#https://*/}; ORGUSER=${foo%/*}; \ 
  NEWGITHUBURI=${GITHUBURI/${ORGUSER}/${GITHUBUSERNAME}}; \
  echo "    Origin URI:\t ${NEWGITHUBURI}"; \
  git remote add origin ${NEWGITHUBURI}; \
  # the following is to fixup failed runs...and to exit with a return of zero \
  git remote set-url origin ${NEWGITHUBURI}; \
'

git submodule foreach --quiet ' \
  git remote -v
'

