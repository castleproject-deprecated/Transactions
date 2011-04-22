$: << './'
require 'albacore'
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
task :ci => [":build_all", "castle:nuget"]

desc "build in release mode"
task :release => ["env:release", "clean", "castle:build"]

desc "build in debug mode"
task :debug => ["env:debug", "clean", "castle:build"]

CLOBBER.include(Folders[:out])
CLOBBER.include(Folders[:packages])

Albacore.configure do |config|
  config.nunit.command = Commands[:nunit]
  config.nugetpack.command = Commands[:nuget]
  config.assemblyinfo.namespaces = "System", "System.Reflection", "System.Runtime.InteropServices", "System.Security"
end

desc "Builds Debug + Release of Tx + AutoTx"
task :build_all do
  ["env:release", "env:debug"].each{ |t| build t }
end

def build(conf)
  puts "BUILD ALL CONF #{conf}"
  Rake::Task[conf].invoke # these will only be invoked once each
  Rake::Task.tasks.each{ |t| t.reenable }
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
  
  task :output => [:tx_output, :autotx_output]
  
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
	asm.product_name = asm.title = Projects[:tx][:title]
    asm.description = Projects[:tx][:description]
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
  
  desc "prepare Tx Services and AutoTx Facility nuget package"
  task :nuget => [:nuget_tx, :nuget_autotx]
  
  directory "#{Folders[:nuspec_tx]}"
  file "#{Files[:nuspec_tx]}"
  
  nuspec :nuspec_tx => ["#{Folders[:nuspec_tx]}","#{Files[:nuspec_tx]}"] do |nuspec|
    nuspec.id = "Castle.Services.Transaction"
    nuspec.version = File.read(Files[:version])
    nuspec.authors = Projects[:tx][:authors]
    nuspec.description = Projects[:tx][:description]
    #nuspec.working_directory = Folders[:nuspec_tx]
    nuspec.title = Projects[:tx][:title]
	nuspec.projectUrl = "https://github.com/haf/Castle.Services.Transaction"
    nuspec.language = "en-US"
    nuspec.licenseUrl = "https://github.com/haf/Castle.Services.Transaction/raw/master/License.txt"	
    nuspec.dependency "Castle.Core", "2.5.1"
    nuspec.output_file = Files[:nuspec_tx]
  end
  
  directory "#{Folders[:nuspec_autotx]}"
  file "#{Files[:nuspec_autotx]}"
  
  nuspec :nuspec_autotx => ["#{Folders[:nuspec_autotx]}", "#{Files[:nuspec_autotx]}"] do |nuspec|
    nuspec.id = "Castle.Facilities.AutoTx"
    nuspec.version = File.read(Files[:version])
    nuspec.authors = Projects[:autotx][:authors]
    nuspec.description = Projects[:autotx][:description]
    #nuspec.working_directory = Folders[:nuspec_autotx]
    nuspec.title = Projects[:autotx][:title]
    nuspec.projectUrl = "https://github.com/haf/Castle.Services.Transaction"
    nuspec.language = "en-US"
    nuspec.licenseUrl = "https://github.com/haf/Castle.Services.Transaction/raw/master/License.txt"
	nuspec.dependency "Castle.Core", "2.5.1"
	nuspec.dependency "Castle.Windsor", "2.5.1"
	nuspec.dependency "Castle.Services.Transaction", "2.5.1"
    nuspec.output_file = Files[:nuspec_autotx]
  end
  
  nugetpack :nuget_autotx => [:nuspec_autotx, :msbuild] do |nuget|
   nuget.nuspec      = Files[:nuspec_autotx]
   nuget.base_folder = Folders[:nuspec_autotx]
   nuget.output      = Folders[:nuget_out]
  end
  
  nugetpack :nuget_tx => [:nuspec_tx, :msbuild] do |nuget|
   nuget.nuspec      = Files[:nuspec_tx]
   nuget.base_folder = Folders[:nuspec_tx]
   nuget.output      = Folders[:nuget_out]
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
