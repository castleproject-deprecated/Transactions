def commit_data
  commit_date = DateTime.parse( git_date ).strftime("%Y-%m-%d %H%M%S")
  begin  
    commit = `git log -1 --pretty=format:%H`
    git_date = `git log -1 --date=iso --pretty=format:%ad`
  rescue
    commit = "git unavailable"
  end
  [commit, commit_date]
end