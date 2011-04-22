require 'fileutils'

def commit_data
  begin  
    commit = `git log -1 --pretty=format:%H`
    git_date = `git log -1 --date=iso --pretty=format:%ad`
    commit_date = DateTime.parse( git_date ).strftime("%Y-%m-%d %H%M%S")
  rescue
    commit = "git unavailable"
  end
  [commit, commit_date]
end


def copy_files(from_dir, file_pattern, out_dir)
	FileUtils.mkdir_p out_dir unless FileTest.exists?(out_dir)
	Dir.glob(File.join(from_dir, file_pattern)){|file|
		copy(file, out_dir) if File.file?(file)
	}
end