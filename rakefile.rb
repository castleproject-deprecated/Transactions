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

sh "dir packages -r"

# profile time: "PS \> $start = [DateTime]::UtcNow ; rake ; $end = [DateTime]::UtcNow ; $diff = $end-$start ; "Started: $start to $end, a diff of $diff"
task :default => [:release]

desc "prepare the version info files to get ready to start coding!"
task :prepare => ["castle:assembly_infos"]

desc "build in release mode"
task :release => ["env:release", "castle:build", "castle:nuget"]

desc "build in debug mode"
task :debug => ["env:debug", "castle:build"]

task :ci => ["clobber", "castle:build", "castle:test_all", "castle:nuget"]

desc "Run all unit and integration tests in debug mode"
task :test_all => ["env:debug", "castle:test_all"]

desc "prepare alpha version for being published (not for ci server)"
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

  release_branch("alpha")
  
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
    nuspec.dependency Projects[:tx][:id], "[#{VERSION}]" # exactly equals
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
