# copyright Henrik Feldt 2011

$: << './'
require 'albacore'
require 'buildscripts/albacore_mods'
begin
  require 'version_bumper'  
rescue LoadError
  puts 'version bumper not available!'
end
require 'rake/clean'
require 'buildscripts/project_data'
require 'buildscripts/paths'
require 'buildscripts/utils'
require 'buildscripts/environment'

# profile time: "PS \> $start = [DateTime]::UtcNow ; rake ; $end = [DateTime]::UtcNow ; $diff = $end-$start ; "Started: $start to $end, a diff of $diff"
task :default => [:release]

desc "prepare the version info files to get ready to start coding!"
task :prepare => ["castle:assembly_infos"]

desc "build in release mode"
task :release => ["env:release", "castle:build", "castle:nuget"]

desc "build in debug mode"
task :debug => ["env:debug", "castle:build"]

task :ci => ["clobber", "castle:build"]

desc "Run all unit and integration tests in debug mode"
task :test_all => ["env:debug", "castle:test_all"]

desc "prepare alpha version for being published"
task :alpha => ["env:release"] do
  puts %q{
    Basically what the script should do;
    1. Verify no pending changes
    2. Verify on develop branch
    3. Ask for alpha number
    4. Verify alpha number is greater than the last alpha number
    5. Verify we're not above alpha, e.g. in beta.
    6. git add . -A ; git commit -m "Automatic alpha" ; rake release castle:test_all
       This ensures we have passing tests and a build with a matching git commit hash.
    7. git checkout alpha
    8. git merge --no-ff -m "Alpha [version here] commit." develop
    9. git tag -a "v[VERSION]"
    10. git push
    11. git push --tags
        This means that the tag is now publically browsable.
    
    Now, TeamCity till take over and run the compile process on the server and then
    upload the artifacts to be downloaded at https://github.com/haf/Castle.Services.Transaction/downloads

}
  branch_to = "alpha"

  # 1. check status
  status = `git status`
  status.include? "nothing to commit" or fail "---> Commit your dirty files:\n\n#{status}\n"
  status.include? "# On branch develop" or fail "---> Commit the alpha on your develop branch (this rule might change)"
  
  # 2. check version
  max_ver = versions(`git tag`)[-1]                          # get max tag version
  if max_ver == nil then fail "no tags available in your repository, exiting. nothing done." end
  build_type = (max_ver[3] / 1000) * 1000                    # e.g. 1000, 2000 or 3000
  next_alpha = (max_ver[3] - build_type) + 1                 # get its alpha/beta/rc-number, e.g. 1, 2, ..., n
  curr_ver = version(VERSION)                                # call utility function with constant
  
  puts " :: Max tag version: #{max_ver}, current version: #{curr_ver}. Please state alpha number > max tag (CTRL+C to interrupt) [#{next_alpha}]: "
  alpha_ver = STDIN.gets.chomp
  alpha_ver = alpha_ver.length == 0 ? next_alpha : alpha_ver.to_i
  
  # 3. calculate new version and verify it
  new_ver = [curr_ver[0], curr_ver[1], curr_ver[2], 1000+alpha_ver]
  if (new_ver <=> max_ver || alpha_ver) == -1 then puts "---> #{new_ver} less than maximum: #{max_ver}" end
  
  # 4, 5. Verify it's an alpha
  if (next_alpha > 1000) then puts "---> no more than a thousand allowed" end
  
  # 6. we can do this optionally, but for now, let's assume someone put a good message in.
  # sh "git commit --amend -m \"Alpha #{new_ver.join('.')}\"" do |ok, status|
    # ok or fail "---> could not perform commit:\n#{status}"
  # end
  
  flags = (`git branch`.include? "  #{branch_to}") ? "" : "-b "
  puts "creating new #{branch_to}-branch, because none exists" if flags.length > 0
  
  # 7.
  sh "git checkout #{flags}#{branch_to}" do |ok, status|
    ok or fail "---> could not checkout alpha. do you have such a branch?"
  end
  
  # 8.
  sh "git merge --no-ff -m \"Alpha #{new_ver.join('.')} commit.\" develop" do |ok, status|
    ok or fail "---> failed merge. recommending a 'git merge --abort'. you are on #{branch_to} currently."
  end
  
  sh "git tag -a \"v#{new_ver.join('.')}\" -m \"Alpha #{new_ver.join('.')}\""
  
  # 9.
  puts " :: Confirm push!"
  sh "git status"
  sh "git log -1"
  
  ok = ""
  puts "\n\nEverything OK? cmd: \"git push origin #{branch_to}\" (yes/no)?"
  until ok == "yes" || ok == "no"
    ok = STDIN.gets.chomp
  end
  
  if ok == "no" then fail %Q{
    
    Remember what has changed in your repository! (now that you aborted ;))
        
        1. tags (git tag -d v#{new_ver.join('.')}),
        2. you have merged to the alpha branch (git reset --hard HEAD~1)
        3. previous branch commit message (git commit --amend ...) or the same as above
	
    ---> NOTE THAT YOU ARE CURRENTLY ON YOUR #{branch-to} BRANCH -- and everything is ready to push.
	
	You can for example change your commit message by 'git commit --amend -m "..."'.

} else
    sh "git push origin #{branch_to}"
  end
  
end

CLOBBER.include(Folders[:out])
CLOBBER.include(Folders[:packages])

Albacore.configure do |config|
  config.nunit.command = Commands[:nunit]
  config.assemblyinfo.namespaces = "System", "System.Reflection", "System.Runtime.InteropServices", "System.Security"
end

desc "Builds Debug + Release of Tx + AutoTx"
task :build_all do
  ["env:release", "env:debug"].each{ |t| build t }
end

def build(conf)
  puts "BUILD ALL CONF #{conf}"
  Rake::Task.tasks.each{ |t| t.reenable }
  Rake::Task[conf].invoke # these will only be invoked once each
  Rake::Task["castle:build"].invoke
  Rake::Task["castle:test_all"].invoke
end

namespace :castle do

  desc "build + tx unit tests + output"
  task :build => ['src/TxAssemblyInfo.cs', 'src/AutoTxAssemblyInfo.cs', :msbuild, :tx_test, :output]
 
  desc "generate the assembly infos you need to compile with VS"
  task :assembly_infos => [:tx_version, :autotx_version]
  
  desc "prepare Tx Services and AutoTx Facility nuspec + nuget package"
  task :nuget => ["#{Folders[:nuget]}", :tx_nuget, :autotx_nuget]
  
  task :test_all => [:tx_test, :autotx_test]
  
  #                    BUILDING
  # ===================================================
  
  msbuild :msbuild do |msb, args|
    # msb.use = :args[:framework] || :net40
    config = "#{FRAMEWORK.upcase}-#{CONFIGURATION}"
    puts "Building config #{config} with MsBuild"
    msb.properties :Configuration => config
    msb.targets :Clean, :Build
    msb.solution = Files[:sln]
  end
  
  #                    VERSIONING
  #        http://support.microsoft.com/kb/556041
  # ===================================================
  
  file 'src/TxAssemblyInfo.cs' => "castle:tx_version"
  file 'src/AutoTxAssemblyInfo.cs' => "castle:autotx_version"
  
  assemblyinfo :tx_version do |asm|
    data = commit_data() #hash + date
    asm.product_name = asm.title = Projects[:tx][:title]
    asm.description = Projects[:tx][:description] + " #{data[0]} - #{data[1]}"
    # This is the version number used by framework during build and at runtime to locate, link and load the assemblies. When you add reference to any assembly in your project, it is this version number which gets embedded.
    asm.version = VERSION
    # Assembly File Version : This is the version number given to file as in file system. It is displayed by Windows Explorer. Its never used by .NET framework or runtime for referencing.
    asm.file_version = VERSION_INFORMAL
    asm.custom_attributes :AssemblyInformationalVersion => "#{VERSION}", # disposed as product version in explorer
      :CLSCompliantAttribute => false,
      :AssemblyConfiguration => "#{CONFIGURATION}",
      :Guid => Projects[:tx][:guid]
    asm.com_visible = false
    asm.copyright = Projects[:tx][:copyright]
    asm.output_file = 'src/TxAssemblyInfo.cs'
  end
  
  assemblyinfo :autotx_version do |asm|
    asm.product_name = asm.title = Projects[:autotx][:title]
    asm.description = Projects[:autotx][:description]
    # This is the version number used by framework during build and at runtime to locate, link and load the assemblies. When you add reference to any assembly in your project, it is this version number which gets embedded.
    asm.version = VERSION
    # Assembly File Version : This is the version number given to file as in file system. It is displayed by Windows Explorer. Its never used by .NET framework or runtime for referencing.
    asm.file_version = VERSION_INFORMAL
    asm.custom_attributes :AssemblyInformationalVersion => "#{VERSION}", # disposed as product version in explorer
      :CLSCompliantAttribute => false,
      :AssemblyConfiguration => "#{CONFIGURATION}",
      :Guid => Projects[:autotx][:guid]
    asm.copyright = Projects[:autotx][:copyright]
    asm.output_file = 'src/AutoTxAssemblyInfo.cs'
  end
  
  #                    OUTPUTTING
  # ===================================================
  task :output => [:tx_output, :autotx_output] do
    Dir.glob(File.join(Folders[:binaries], "*.txt")){ | fn | File.delete(fn) } # remove old commit marker files
	data = commit_data() # get semantic data
    File.open File.join(Folders[:binaries], "#{data[0]} - #{data[1]}.txt"), "w" do |f|
      f.puts %Q{aa
    This file's name gives you the specifics of the commit.
    
    Commit hash:		#{data[0]}
    Commit date:		#{data[1]}
}
    end
  end
  
  task :tx_output => :msbuild do
    target = File.join(Folders[:binaries], Projects[:tx][:dir])
    copy_files Folders[:tx_out], "*.{xml,dll,pdb,config}", target
    CLEAN.include(target)
  end
  
  task :autotx_output => :msbuild do
    target = File.join(Folders[:binaries], Projects[:autotx][:dir])
    copy_files Folders[:autotx_out], "*.{xml,dll,pdb,config}", target
    CLEAN.include(target)
  end
  
  
  #                     TESTING
  # ===================================================
  directory "#{Folders[:tests]}"
  
  task :tx_test => [:msbuild, "#{Folders[:tests]}", :tx_nunit, :tx_test_publish_artifacts]
  task :autotx_test => [:msbuild, "#{Folders[:tests]}", :autotx_nunit, :autotx_test_publish_artifacts]
  
  nunit :tx_nunit do |nunit|
    nunit.command = Commands[:nunit]
    nunit.options '/framework v4.0', "/out #{Files[:tx][:test_log]}", "/xml #{Files[:tx][:test_xml]}"
    nunit.assemblies Files[:tx][:test]
	CLEAN.include(Folders[:tests])
  end
  
  task :tx_test_publish_artifacts => :tx_nunit do
	puts "##teamcity[importData type='nunit' path='#{Files[:tx][:test_xml]}']"
	puts "##teamcity[publishArtifacts '#{Files[:tx][:test_log]}']"
  end
    
  nunit :autotx_nunit do |nunit|
    nunit.command = Commands[:nunit]
    nunit.options '/framework v4.0', "/out #{Files[:autotx][:test_log]}", "/xml #{Files[:autotx][:test_xml]}"
    nunit.assemblies Files[:autotx][:test]
	CLEAN.include(Folders[:tests])
  end
  
  task :autotx_test_publish_artifacts => :autotx_nunit do
	puts "##teamcity[publishArtifacts path='#{Files[:autotx][:test_xml]}']"
	puts "##teamcity[publishArtifacts '#{Files[:autotx][:test_log]}']"
  end
  
  #                      NUSPEC
  # ===================================================
  
  # copy from the key's data using the glob pattern
  def nuspec_copy(key, glob)
    puts "key: #{key}, glob: #{glob}, proj dir: #{Projects[key][:dir]}"
    FileList[File.join(Folders[:binaries], Projects[key][:dir], glob)].collect{ |f|
      to = File.join( Folders[:"#{key}_nuspec"], "lib", FRAMEWORK )
      FileUtils.mkdir_p to
      cp f, to
	  # return the file name and its extension:
	  File.join(FRAMEWORK, File.basename(f))
    }
  end
  
  file "#{Files[:tx][:nuspec]}"
  
  nuspec :tx_nuspec => [:output, :tx_nuget_dirs] do |nuspec|
    nuspec.id = Projects[:tx][:id]
    nuspec.version = VERSION
    nuspec.authors = Projects[:tx][:authors]
    nuspec.description = Projects[:tx][:description]
    nuspec.title = Projects[:tx][:title]
    nuspec.projectUrl = "https://github.com/haf/Castle.Services.Transaction"
    nuspec.language = "en-US"
    nuspec.licenseUrl = "https://github.com/haf/Castle.Services.Transaction/raw/master/License.txt"	
    nuspec.requireLicenseAcceptance = "true"
    nuspec.dependency "Castle.Core", "2.5.2"
	nuspec.dependency "log4net", "1.2.10"
	nuspec.dependency "Rx-Core", "1.0.2856.0"
	nuspec.dependency "Rx-Main", "1.0.2856.0"
	nuspec.dependency "Rx-Interactive", "1.0.2856.0"
	nuspec.framework_assembly "System.Transactions", FRAMEWORK
    nuspec.output_file = Files[:tx][:nuspec]
    #nuspec.working_directory = Folders[:tx_nuspec]

    nuspec_copy(:tx, "*Transaction.{dll,xml,pdb}")
    # right now, we'll go with the conventions.each{ |ff| nuspec.file ff }

    #CLEAN.include(Folders[:tx][:nuspec])
  end
  
  file "#{Files[:autotx][:nuspec]}"
  
  nuspec :autotx_nuspec => [:output, :autotx_nuget_dirs] do |nuspec|
    nuspec.id = Projects[:autotx][:id]
    nuspec.version = VERSION
    nuspec.authors = Projects[:autotx][:authors]
    nuspec.description = Projects[:autotx][:description]
    nuspec.title = Projects[:autotx][:title]
    nuspec.projectUrl = "https://github.com/haf/Castle.Services.Transaction"
    nuspec.language = "en-US"
    nuspec.licenseUrl = "https://github.com/haf/Castle.Services.Transaction/raw/master/License.txt"
    nuspec.requireLicenseAcceptance = "true"
    nuspec.dependency "Castle.Core", "2.5.2"
    nuspec.dependency "Castle.Windsor", "2.5.2"
    nuspec.dependency Projects[:tx][:id], VERSION # might require <VERSION sometimes
	nuspec.dependency "log4net", "1.2.10"
	nuspec.dependency "Rx-Core", "1.0.2856.0"
	nuspec.dependency "Rx-Main", "1.0.2856.0"
	nuspec.dependency "Rx-Interactive", "1.0.2856.0"
	nuspec.framework_assembly "System.Transactions", FRAMEWORK
    nuspec.output_file = Files[:autotx][:nuspec]
    #nuspec.working_directory = Folders[:autotx_nuspec]
    
    nuspec_copy(:autotx, "*AutoTx.{dll,xml,pdb}")
	# right now, we'll go with the conventions
	#.each{ |ff| nuspec.file ff }
	
    CLEAN.include(Folders[:autotx_nuspec])
  end
  
  #                       NUGET
  # ===================================================
  
  directory "#{Folders[:nuget]}"
  
  # creates directory tasks for all nuspec-convention based directories
  def nuget_directory(key)
    dirs = FileList.new([:lib, :content, :tools].collect{ |dir|
      File.join(Folders[:"#{key}_nuspec"], "#{dir}")
    }).each{ |d| directory d }
    task :"#{key}_nuget_dirs" => dirs # NOTE: here a new dynamic task is defined
  end
  
  nuget_directory(:tx)
  
  desc "generate nuget package for tx services"
  nugetpack :tx_nuget => [:output, :tx_nuspec] do |nuget|
    nuget.command     = Commands[:nuget]
    nuget.nuspec      = Files[:tx][:nuspec]
    nuget.output      = Folders[:nuget]
  end
  
  nuget_directory(:autotx)
  
  desc "generate nuget package for autotx facility"
  nugetpack :autotx_nuget => [:output, :autotx_nuspec] do |nuget|
	nuget.command     = Commands[:nuget]
    nuget.nuspec      = Files[:autotx][:nuspec]
    nuget.output      = Folders[:nuget]
  end
end

desc "display rake task help"  
task :help do
  puts ""
  puts " Castle Transaction Services & AutoTx Facility (c)Henrik Feldt 2011"
  puts " =================================================================="
  puts ""
  puts " Quick Start: Type 'rake' and look in '#{Folders[:out]}/'."
  puts ""	
  puts ""
  puts " How-to:"
  puts " -------"
  puts " JUST BUILD IT:           'rake'"
  puts " See available tasks:     'rake -T'"
  puts " Complete major GA:       'rake bump:major  env:release castle:build castle:nuget'"
  puts " Complete minor GA:       'rake bump:minor env:release castle:build castle:nuget'"
  puts " Build release yourself:  'rake' or 'rake release'"
  puts " Build debug yourself:    'rake debug'"
  puts " RC 1 build:              'rake env:rc[1] env:release castle:build castle:nuget'"
  puts " RC 2 build:              'rake env:rc[2] env:release castle:build castle:nuget'"
  puts " Beta 1 build:            'rake env:beta[1] env:release castle:build castle:nuget'"
  puts " Alpha 1 build:           'rake env:alpha[1] env:release castle:build castle:nuget'"
  puts " Alpha 2 build:           'rake env:alpha[2] env:release castle:build castle:nuget'"
  puts ""
  puts " Informational:"
  puts " --------------"
  puts " See version if rc 3:      'rake env:rc[3] env:release'"
  puts ""
  puts " Maintainance"
  puts " ------------"
  puts " Remove build/ dir         'rake clobber'"
end
