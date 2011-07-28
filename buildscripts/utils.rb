require 'fileutils'

def commit_data
  begin  
    commit = `git log -1 --pretty=format:%H`
    git_date = `git log -1 --date=iso --pretty=format:%ad`
    commit_date = DateTime.parse( git_date ).strftime("%Y-%m-%d %H%M%S")
  rescue
    commit = "git unavailable"
    commit_date = Time.new.strftime("%Y-%m-%d %H%M%S")
  end
  [commit, commit_date]
end


def copy_files(from_dir, file_pattern, out_dir)
	FileUtils.mkdir_p out_dir unless FileTest.exists?(out_dir)
	Dir.glob(File.join(from_dir, file_pattern)){|file|
		copy(file, out_dir) if File.file?(file)
	}
end

def versions(str)
  str.split(/\r\n|\n/).map{|s|version(s)}.compact.sort
end

def version(str)
  ver = /v?(\d+)\.(\d+)\.(\d+)\.?(\d+)?/i.match(str).to_a()
  ver[1,4].map{|s|s.to_i} unless ver == nil or ver.empty?
end

def verify_release_branch_number(build_number, bottom)
  bottom == 4000 ? (build_number == 4000) : (build_number > bottom and build_number < (bottom+1000))
end

def resolve_type(branch)
  case branch
  when "alpha"
    1000
  when "beta"
    2000
  when "rc"
    3000
  when "ga"
    4000  
  end
end

def release_branch(branch_to)

  # 1. check status
  status = `git status`
  status.include? "nothing to commit" or fail "---> Commit your dirty files:\n\n#{status}\n"
  status.include? "# On branch develop" or fail "---> Commit to the #{branch_to} branch from your develop branch!"
  
  # 2. check version
  max_ver = versions(`git tag`)[-1]                          # get max tag version
  if max_ver == nil then fail "no tags available in your repository, exiting. nothing done." end
  build_type = resolve_type(branch_to)                       # e.g. 1000, 2000 or 3000: (max_ver[3] / 1000) * 1000 
  next_build = (max_ver[3] - build_type) + 1                 # get its alpha/beta/rc-number, e.g. 1, 2, ..., n
  curr_ver = version(VERSION)                                # call utility function with constant
  
  puts " :: Max tag version: #{max_ver}, current version: #{curr_ver}. Please state #{branch_to.capitalize} number > max tag (CTRL+C to interrupt) [#{next_build}]: "
  target_revision = STDIN.gets.chomp
  target_revision = target_revision.length == 0 ? next_build : target_revision.to_i
  
  # 3. calculate new version and verify it
  new_ver = [curr_ver[0], curr_ver[1], curr_ver[2], build_type+target_revision]
  if (new_ver <=> max_ver || target_revision) == -1 then puts "---> #{new_ver} less than maximum: #{max_ver}" end
  
  # 4, 5. Verify it's a correct number for our release type
  fail "---> invalid build number #{new_ver[3]} for #{branch_to}-branch" unless verify_release_branch_number(new_ver[3], build_type)
  
  # 6. we can do this optionally, but for now, let's assume someone put a good message in.
  # sh "git commit --amend -m \"v#{new_ver.join('.')}. #{branch_to.capitalize} #{target_revision}\"" do |ok, status|
    # ok or fail "---> could not perform commit:\n#{status}"
  # end
  
  # is it a new branch?
  flags = (`git branch`.include? "  #{branch_to}") ? "" : "-b "
  puts "creating new #{branch_to}-branch, because none exists" if flags.length > 0
  
  # 7. move to the branch we wish to release
  sh "git checkout #{flags}#{branch_to}"
  
  tagname = "v#{new_ver.join('.')}"
  
  # 8. Merge from the develop branch into the current branch with a custom commit message stating it's a special merge. You want a recursive theirs-merge, because you don't care about modifications to your local alpha branch.
  sh "git merge -s recursive -Xtheirs --no-ff -m \"#{tagname}. #{branch_to.capitalize} #{target_revision} commit.\" develop" do |ok, status|
    ok or fail "---> failed merge. Recommending a 'git merge --abort'. you are on #{branch_to} currently. You can also merge manually and commit those changes manually. Read the buildscripts/utils.rb file to get an idea of the next steps."
  end
  
  # no need to jump to another branch, we're fine here.
  sh "git tag -a \"#{tagname}\" -m \"#{branch_to.capitalize} #{tagname}\""
  
  # 9. Ready with merge, let's push and give the option to abort!
  puts " :: Confirm push! Printing status and your push message:\n"
  sh "git status"
  sh "git log -1"
  
  ok = ""
  puts "\n\nEverything OK? You can enter 'no' and then run \"git push --dry-run origin #{branch_to}\" if you are unsure. Run command: \"git push origin #{branch_to}\" (yes/no)?"
  until ok == "yes" || ok == "no"
    ok = STDIN.gets.chomp
  end
  
  if ok == "no" then fail %Q{
    
    Remember what has changed in your repository now that you have aborted the push. You can undo
	almost anything as long as you don't push. Here are some commands you might consider for that:
        
        1. tags (git tag -d #{tagname}),
        2. you have merged to the alpha branch (git reset --hard HEAD~1)
        3. previous branch commit message (git commit --amend ...) or the same as above
	
    ---> NOTE THAT YOU ARE CURRENTLY ON YOUR #{branch_to} BRANCH -- and everything is ready to push.
	
	You can for example change your commit message by 'git commit --amend -m "..."'.

} else
    sh "git push origin #{branch_to}"
	
	puts "Type your password below if you would like to push a tag for it (you can do this later also)."
	sh "git push origin \"refs/tags/#{tagname}:refs/tags/#{tagname}\""
  end
  
  # the rest is book-keeping to keep branches up to speed with each other
  # in the end develop == master and alpha is whatever we had in develop at the time we said commit.
  # Hopefully master is a ff-only merge.
  sh "git checkout develop"
  sh "git merge #{branch_to}"
  sh "git checkout master"
  sh "git merge develop"
  sh "git checkout develop"
  sh "git merge master" # --ff-only?
end