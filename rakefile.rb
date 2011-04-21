$: << './'
require 'albacore'
require 'version_bumper'
require 'rake/clean'
require 'buildscripts/paths'
require 'buildscripts/utils'
require 'buildscripts/project_data'

task :default => [:release]
task :release => ["env:release", "castle:build"]
task :debug => ["env:debug", "castle:build"]

desc "display rake task help"  
task :help do
  puts ""
  puts " Castle Transaction Services & AutoTx Facility (c)Henrik Feldt 2011"
  puts " =================================================================="
  puts ""
  puts " Quick Start: Type 'rake' and look in '#{Folders[:outdir]}/'."
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

CLOBBER.include(Folders[:outdir])

Albacore.configure do |config|
  config.nunit.command = Commands[:nunit]
  config.nugetpack.command = Commands[:nuget]
  config.assemblyinfo.namespaces = "System", "System.Reflection", "System.Runtime.InteropServices", "System.Security"
end

desc "Builds both a Debug and a Release build of both Transaction Services and AutoTx Facility"
task :build_all do
  ["env:release", "env:debug"].each{ |t| build t }
end

def build(conf)
  Rake::Task[conf].invoke # these will only be invoked once each
  Rake::Task["castle:build"].reenable
  Rake::Task["castle:build"].invoke
end

namespace :env do

  task :common do
	File.open( Files[:version] , "r") do |f|
		ENV['VERSION_BASE'] = VERSION_BASE = f.gets
	end
	
	# version management
	official = ENV['OFFICIAL_RELEASE'] || "0"
	build = ENV['BUILD_NUMBER'] || Time.now.strftime('%j%H') # (day of year 0-265)(hour 00-24)
    ENV['VERSION'] = VERSION = "#{VERSION_BASE}.#{official}"
	ENV['VERSION_INFORMAL'] = VERSION_INFORMAL = "#{VERSION_BASE}.#{build}"
	puts "Assembly Version: #{VERSION}."
	puts "##teamcity[buildNumber '#{VERSION_INFORMAL}']"
	
	# configuration management
	ENV['FRAMEWORK'] = FRAMEWORK = ENV['FRAMEWORK'] || (Rake::Win32::windows? ? "net40" : "mono28")
	puts "Framework: #{FRAMEWORK}"
  end
  
  desc "set GA envionment variables"
  task :ga do
	ENV['OFFICIAL_RELEASE'] = OFFICIAL_RELEASE = "4000"
  end
  
  desc "set release candidate environment variables"
  task :rc, [:number] do |t, args|
    num = args[:number].to_i || 1
	ENV['OFFICIAL_RELEASE'] = OFFICIAL_RELEASE = "#{3000 + num}"
  end
  
  desc "set beta-environment variables"
  task :beta, [:number] do |t, args|
	num = args[:number].to_i || 1
    ENV['OFFICIAL_RELEASE'] = OFFICIAL_RELEASE = "#{2000 + num}"
  end
  
  desc "set alpha environment variables"
  task :alpha, [:number] do |t, args|
	num = args[:number].to_i || 1
    ENV['OFFICIAL_RELEASE'] = OFFICIAL_RELEASE = "#{1000 + num}"
  end
  
  desc "set debug environment variables"
  task :debug => :common do
    ENV['CONFIGURATION'] = CONFIGURATION = 'Debug'
    Folders[:binaries] = File.join(Folders[:outdir], "debug", FRAMEWORK)
	CLEAN.include("*", Folders[:binaries])
  end
  
  desc "set release environment variables"
  task :release => :common do
	ENV['CONFIGURATION'] = CONFIGURATION = 'Release'
    Folders[:binaries] = File.join(Folders[:outdir], "release", FRAMEWORK)
	CLEAN.include("*", Folders[:binaries])
  end
end

namespace :castle do

  desc "build Castle Transaction Services and AutoTx Facility"
  msbuild :build => ['src/TxAssemblyInfo.cs', 'src/AutoTxAssemblyInfo.cs'] do |msb, args|
    # msb.use = :args[:framework] || :net40
	config = "#{FRAMEWORK.upcase}-#{CONFIGURATION}"
	puts "Building config #{config} with MsBuild"
	msb.properties :Configuration => config
    msb.targets :Build
    msb.solution = Files[:sln]
  end
  
  file 'src/TxAssemblyInfo.cs' => "castle:tx_version"
  file 'src/AutoTxAssemblyInfo.cs' => "castle:autotx_version"
  
  # versioning: http://support.microsoft.com/kb/556041
  desc 'build Transaction Services assembly info file'
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
  
  desc 'build AutoTx Facility assembly info file'
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
  
  desc "nuget package for Transaction Services"
  nuspec :nuspec_tx do |nuspec|
    nuspec.id = "Castle.Services.Transaction"
    nuspec.version = File.read(Files[:version])
    nuspec.authors = Projects[:tx][:authors]
    nuspec.description = Projects[:tx][:description]
    nuspec.working_directory = Folders[:nuspec_tx]
    nuspec.title = Projects[:tx][:title]
	nuspec.projectUrl = "https://github.com/haf/Castle.Services.Transaction"
    nuspec.language = "en-US"
    nuspec.licenseUrl = "https://github.com/haf/Castle.Services.Transaction/raw/master/License.txt"	
    nuspec.dependency "Castle.Core", "2.5.1"
    nuspec.output_file = Files[:nuspec_tx]
  end
  
  directory "#{Folders[:packages]}}"
  
  desc "nuget package for AutoTx Facility"
  nuspec :nuspec_autotx do |nuspec|
    nuspec.id = "Castle.Facilities.AutoTx"
    nuspec.version = File.read(Files[:version])
    nuspec.authors = Projects[:autotx][:authors]
    nuspec.description = Projects[:autotx][:description]
    nuspec.working_directory = Folders[:nuspec_autotx]
    nuspec.title = Projects[:autotx][:title]
    nuspec.projectUrl = "https://github.com/haf/Castle.Services.Transaction"
    nuspec.language = "en-US"
    nuspec.licenseUrl = "https://github.com/haf/Castle.Services.Transaction/raw/master/License.txt"
	nuspec.dependency "Castle.Core", "2.5.1"
	nuspec.dependency "Castle.Windsor", "2.5.1"
	nuspec.dependency "Castle.Services.Transaction", "2.5.1"
    nuspec.output_file = Files[:nuspec_autotx]
  end
  
  desc "create AutoTx nuget package"
  nugetpack :nuget_autotx => :nuspec_autotx do |nuget|
   nuget.nuspec      = Files[:nuspec_autotx]
   nuget.base_folder = Folders[:nuspec_autotx]
   nuget.output      = Folders[:nuget_out]
  end
  
  desc "create Tx Services nuget package"
  nugetpack :nuget_tx => :nuspec_tx do |nuget|
   nuget.nuspec      = Files[:nuspec_tx]
   nuget.base_folder = Folders[:nuspec_tx]
   nuget.output      = Folders[:nuget_out]
  end
end