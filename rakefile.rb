$: << './'
require 'albacore'
require 'buildscripts/albacore_mods'
require 'version_bumper'
require 'rake/clean'
require 'buildscripts/project_data'
require 'buildscripts/paths'
require 'buildscripts/utils'
require 'buildscripts/environment'

# profile time: "PS \> $start = [DateTime]::UtcNow ; rake ; $end = [DateTime]::UtcNow ; $diff = $end-$start ; "Started: $start to $end, a diff of $diff"
task :default => [:release]

desc "prepare the version info files to get ready to start coding!"
task :prepare => ["castle:assembly_infos"]

desc "runner for continuous integration"
task :ci => ["env:release", "castle:build", "castle:test_all", "castle:nuget"]

desc "build in release mode"
task :release => ["env:release", "clean", "castle:build"]

desc "build in debug mode"
task :debug => ["env:debug", "clean", "castle:build"]

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
  
  desc "run all tests, also for AutoTx"
  task :test_all => [:tx_test, :autotx_test]
  
  msbuild :msbuild do |msb, args|
    # msb.use = :args[:framework] || :net40
    config = "#{FRAMEWORK.upcase}-#{CONFIGURATION}"
    puts "Building config #{config} with MsBuild"
    msb.properties :Configuration => config
    msb.targets :Clean, :Build
    msb.solution = Files[:sln]
  end
  
  directory "#{Folders[:tests]}"
  
  nunit :tx_test => [:msbuild, "#{Folders[:tests]}"] do |nunit|
    nunit.command = Commands[:nunit]
    nunit.options '/framework v4.0', 
      "/out #{File.join(Folders[:tests], Projects[:tx][:dir])}.log",
      "/xml #{File.join(Folders[:tests], Projects[:tx][:dir])}.xml"
    nunit.assemblies Files[:tx_test]
  end
  
  desc "AutoTx unit + integration tests"
  nunit :autotx_test => [:msbuild, "#{Folders[:tests]}"] do |nunit|
    nunit.command = Commands[:nunit]
    nunit.options '/framework v4.0', 
      "/out #{File.join(Folders[:tests], Projects[:autotx][:dir])}.log",
      "/xml #{File.join(Folders[:tests], Projects[:autotx][:dir])}.xml"
    nunit.assemblies Files[:autotx_test]
  end
  
  task :output => [:tx_output, :autotx_output] do
    data = commit_data()
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
  
  file 'src/TxAssemblyInfo.cs' => "castle:tx_version"
  file 'src/AutoTxAssemblyInfo.cs' => "castle:autotx_version"
  
  task :assembly_infos => [:tx_version, :autotx_version]
  
  # versioning: http://support.microsoft.com/kb/556041
  assemblyinfo :tx_version do |asm|
    data = commit_data() #hash + date
    asm.product_name = asm.title = Projects[:tx][:title]
    asm.description = Projects[:tx][:description] + " #{data[0]} - #{data[1]}"
    # This is the version number used by framework during build and at runtime to locate, link and load the assemblies. When you add reference to any assembly in your project, it is this version number which gets embedded.
    asm.version = VERSION
    # Assembly File Version : This is the version number given to file as in file system. It is displayed by Windows Explorer. Its never used by .NET framework or runtime for referencing.
    asm.file_version = VERSION_INFORMAL
    asm.custom_attributes :AssemblyInformationalVersion => "#{VERSION_INFORMAL}",
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
    asm.custom_attributes :AssemblyInformationalVersion => "#{VERSION_INFORMAL}",
      :CLSCompliantAttribute => false,
      :AssemblyConfiguration => "#{CONFIGURATION}",
      :Guid => Projects[:autotx][:guid]
    asm.copyright = Projects[:autotx][:copyright]
    asm.output_file = 'src/AutoTxAssemblyInfo.cs'
  end
  
  directory "#{Folders[:nuget]}"
  desc "prepare Tx Services and AutoTx Facility nuspec + nuget package"
  task :nuget => ["#{Folders[:nuget]}", :tx_nuget, :autotx_nuget]
  
  nugetpack :tx_nuget => [:msbuild, :tx_nuspec] do |nuget|
    nuget.command     = Commands[:nuget]
    nuget.nuspec      = Files[:tx_nuspec]
    nuget.output      = Folders[:nuget]
  end
  
  # creates directory tasks for all nuspec-convention based directories
  def nuget_directory(key)
    dirs = FileList.new([:lib, :content, :tools].collect{ |dir|
      File.join(Folders[:"#{key}_nuspec"], "#{dir}")
    }).each{ |d| directory d }
    task :"#{key}_nuget_dirs" => dirs # NOTE: here a new dynamic task is defined
  end
  
  nuget_directory(:tx)
  file "#{Files[:tx_nuspec]}"
  
  nuspec :tx_nuspec => :tx_nuget_dirs do |nuspec|
    nuspec.id = "Castle.Services.Transaction"
    nuspec.version = File.read(Files[:version])
    nuspec.authors = Projects[:tx][:authors]
    nuspec.description = Projects[:tx][:description]
    nuspec.title = Projects[:tx][:title]
    nuspec.projectUrl = "https://github.com/haf/Castle.Services.Transaction"
    nuspec.language = "en-US"
    nuspec.licenseUrl = "https://github.com/haf/Castle.Services.Transaction/raw/master/License.txt"	
    nuspec.dependency "Castle.Core", "2.5.1"
	nuspec.dependency "Rx-Core", "1.0.2856.0"
	nuspec.dependency "Rx-Main", "1.0.2856.0"
	nuspec.dependency "Rx-Interactive", "1.0.2856.0"
	nuspec.framework_assembly "System.Transactions", FRAMEWORK
    nuspec.output_file = Files[:tx_nuspec]
    #nuspec.working_directory = Folders[:tx_nuspec]

    nuspec_copy(:tx, "*Transaction.{dll,xml,pdb}")
    # right now, we'll go with the conventions.each{ |ff| nuspec.file ff }

    CLEAN.include(Folders[:tx_nuspec])
  end
  
  nugetpack :autotx_nuget => [:msbuild, :autotx_nuspec] do |nuget|
	nuget.command     = Commands[:nuget]
    nuget.nuspec      = Files[:autotx_nuspec]
    nuget.output      = Folders[:nuget]
  end
  
  nuget_directory(:autotx)
  file "#{Files[:autotx_nuspec]}"
  
  nuspec :autotx_nuspec => :autotx_nuget_dirs do |nuspec|
    nuspec.id = "Castle.Facilities.AutoTx"
    nuspec.version = File.read(Files[:version])
    nuspec.authors = Projects[:autotx][:authors]
    nuspec.description = Projects[:autotx][:description]
    nuspec.title = Projects[:autotx][:title]
    nuspec.projectUrl = "https://github.com/haf/Castle.Services.Transaction"
    nuspec.language = "en-US"
    nuspec.licenseUrl = "https://github.com/haf/Castle.Services.Transaction/raw/master/License.txt"
    nuspec.dependency "Castle.Core", "2.5.1"
    nuspec.dependency "Castle.Windsor", "2.5.1"
    nuspec.dependency "Castle.Services.Transaction", VERSION # might require <VERSION sometimes
	nuspec.dependency "Rx-Core", "1.0.2856.0"
	nuspec.dependency "Rx-Main", "1.0.2856.0"
	nuspec.dependency "Rx-Interactive", "1.0.2856.0"
	nuspec.framework_assembly "System.Transactions", FRAMEWORK
    nuspec.output_file = Files[:autotx_nuspec]
    #nuspec.working_directory = Folders[:autotx_nuspec]
    
    nuspec_copy(:autotx, "*AutoTx.{dll,xml,pdb}")
	# right now, we'll go with the conventions
	#.each{ |ff| nuspec.file ff }
	
    CLEAN.include(Folders[:autotx_nuspec])
  end
  
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
  puts " Complete major GA:       'rake bump:major env:ga release'"
  puts " Complete minor GA:       'rake bump:minor env:ga release'"
  puts " Build release yourself:  'rake' or 'rake release'"
  puts " Build debug yourself:    'rake debug'"
  puts " GA release build:        'rake env:ga release'"
  puts " RC 1 build:              'rake env:rc[1] release'"
  puts " RC 2 build:              'rake env:rc[2] release'"
  puts " Beta 1 build:            'rake env:beta[1] release'"
  puts " Alpha 1 build:           'rake env:alpha[1] release'"
  puts " Alpha 2 build:           'rake env:alpha[2] release'"
  puts ""
  puts " Informational:"
  puts " --------------"
  puts " See version if rc 3:      'rake env:rc[3] env:release'"
  puts ""
  puts " Maintainance"
  puts " ------------"
  puts " Remove build/ dir         'rake clobber'"
end
