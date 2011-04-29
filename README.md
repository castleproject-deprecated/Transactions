(Tutorial/Discussion)[http://www.kernel.org/pub/software/scm/git/docs/howto/using-merge-subtree.html]

```
1. $ git rm -r --cached buildscripts
2. $ git commit -m "intermediate commit to remove buildscripts from index."
3. $ git checkout -b tmp
4. $ git add . ; git commit -m "branch with buildscripts"
5. $ git checkout master

6. $ git remote add -f Releases https://haf@github.com/haf/Castle.Releases.git 
7. $ git merge -s ours --no-commit Releases/master 
8. $ git read-tree --prefix=buildscripts/ -u Releases/master 
9. $ git commit -m "Merge Releases project as our subdirectory" 

10. $ git merge tmp
11. $ git branch -d tmp

12. $ git pull -s subtree Releases master


```
 1. Remove the buildscripts from git's index.
 2. You can squash this commit with 'git rebase -i HEAD~1' and then 's'-key, if you feel up for it.
 3. Make tmp branch and switch to it to place old buildscripts in it
 4. Add buildscripts to this branch
 5. Go back to the branch you came from
 
 You're done with saving what has been in buildscripts.
 
 6. name the other project "Releases", and fetch. 
 7. prepare for the later step to record the result as a merge.
 8. read "master" branch of Releases to the subdirectory "buildscripts".
 9. record the merge result.
 
 10. merge back what you had before merging the subtree. possible 'git merge -s ours tmp
 11. remove the tmp branch.
 
 12. maintain the result with subsequent merges using "subtree"
