$: << './'
require 'albacore'
require 'buildscripts/albacore_mods'
require 'buildscripts/ilmerge'
require 'semver'
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

# WARNING: do not run this locally if you have set the private nuget key file
task :ci => ["clobber", "castle:build", "castle:test_all", "castle:nuget"]

desc "Run all unit and integration tests in debug mode"
task :test_all => ["env:debug", "castle:test_all"]

desc "prepare alpha version for being published"
task :alpha => ["env:release"] do
  puts "Preparing Alpha Release"
  release_branch("alpha")
end

desc "prepare beta version for being published"
task :beta => ["env:release"] do
  puts "Preparing Beta Release"
  release_branch("beta")
end

desc "prepare rc for being published"
task :rc => ["env:release"] do
  puts "Preparing RC release"
  release_branch("rc")
end

CLOBBER.include(Folders[:out])

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
  task :build => ['src/TxAssemblyInfo.cs', 'src/AutoTxAssemblyInfo.cs', 'src/IOAssemblyInfo.cs', 'src/IOAutofacAssemblyInfo.cs', 'src/IOWindsorAssemblyInfo.cs', 'src/TxAutofacAssemblyInfo.cs', 'src/TxFSharpAPIAssemblyInfo.cs', 'src/TxIOAssemblyInfo.cs', :msbuild, :tx_test, :output]
 
  desc "generate the assembly infos you need to compile with VS"
  task :assembly_infos => [:tx_version, :autotx_version, :io_version, :io_autofac_version, :io_windsor_version, :tx_autofac_version, :tx_fsharpapi_version, :tx_io_version]
  
  desc "prepare nuspec + nuget packages"
  task :nuget => ["#{Folders[:nuget]}", :tx_nuget, :autotx_nuget, :io_nuget, :io_autofac_nuget, :io_windsor_nuget, :tx_autofac_nuget, :tx_fsharpapi_nuget, :tx_io_nuget]
  
  task :test_all => [:tx_test, :autotx_test] #,  :io_test, :tx_io_test]
  
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
  file 'src/IOAssemblyInfo.cs' => "castle:io_version"
  file 'src/IOAutofacAssemblyInfo.cs' => "castle:io_autofac_version"
  file 'src/IOWindsorAssemblyInfo.cs' => "castle:io_windsor_version"
  file 'src/TxAutofacAssemblyInfo.cs' => "castle:tx_autofac_version"
  file 'src/TxFSharpAPIAssemblyInfo.cs' => "castle:tx_fsharpapi_version"
  file 'src/TxIOAssemblyInfo.cs' => "castle:tx_io_version"

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
  
  assemblyinfo :io_version do |asm|
    asm.product_name = asm.title = Projects[:io][:title]
    asm.description = Projects[:io][:description]
    # This is the version number used by framework during build and at runtime to locate, link and load the assemblies. When you add reference to any assembly in your project, it is this version number which gets embedded.
    asm.version = VERSION
    # Assembly File Version : This is the version number given to file as in file system. It is displayed by Windows Explorer. Its never used by .NET framework or runtime for referencing.
    asm.file_version = VERSION_INFORMAL
    asm.custom_attributes :AssemblyInformationalVersion => "#{VERSION}", # disposed as product version in explorer
      :CLSCompliantAttribute => false,
      :AssemblyConfiguration => "#{CONFIGURATION}",
      :Guid => Projects[:io][:guid]
    asm.copyright = Projects[:io][:copyright]
    asm.output_file = 'src/IOAssemblyInfo.cs'
  end
  
  assemblyinfo :io_autofac_version do |asm|
    asm.product_name = asm.title = Projects[:io_autofac][:title]
    asm.description = Projects[:io_autofac][:description]
    # This is the version number used by framework during build and at runtime to locate, link and load the assemblies. When you add reference to any assembly in your project, it is this version number which gets embedded.
    asm.version = VERSION
    # Assembly File Version : This is the version number given to file as in file system. It is displayed by Windows Explorer. Its never used by .NET framework or runtime for referencing.
    asm.file_version = VERSION_INFORMAL
    asm.custom_attributes :AssemblyInformationalVersion => "#{VERSION}", # disposed as product version in explorer
      :CLSCompliantAttribute => false,
      :AssemblyConfiguration => "#{CONFIGURATION}",
      :Guid => Projects[:io_autofac][:guid]
    asm.copyright = Projects[:io_autofac][:copyright]
    asm.output_file = 'src/IOAutofacAssemblyInfo.cs'
  end
  
  assemblyinfo :io_windsor_version do |asm|
    asm.product_name = asm.title = Projects[:io_windsor][:title]
    asm.description = Projects[:io_windsor][:description]
    # This is the version number used by framework during build and at runtime to locate, link and load the assemblies. When you add reference to any assembly in your project, it is this version number which gets embedded.
    asm.version = VERSION
    # Assembly File Version : This is the version number given to file as in file system. It is displayed by Windows Explorer. Its never used by .NET framework or runtime for referencing.
    asm.file_version = VERSION_INFORMAL
    asm.custom_attributes :AssemblyInformationalVersion => "#{VERSION}", # disposed as product version in explorer
      :CLSCompliantAttribute => false,
      :AssemblyConfiguration => "#{CONFIGURATION}",
      :Guid => Projects[:io_windsor][:guid]
    asm.copyright = Projects[:io_windsor][:copyright]
    asm.output_file = 'src/IOWindsorAssemblyInfo.cs'
  end
  
  assemblyinfo :tx_autofac_version do |asm|
    asm.product_name = asm.title = Projects[:tx_autofac][:title]
    asm.description = Projects[:tx_autofac][:description]
    # This is the version number used by framework during build and at runtime to locate, link and load the assemblies. When you add reference to any assembly in your project, it is this version number which gets embedded.
    asm.version = VERSION
    # Assembly File Version : This is the version number given to file as in file system. It is displayed by Windows Explorer. Its never used by .NET framework or runtime for referencing.
    asm.file_version = VERSION_INFORMAL
    asm.custom_attributes :AssemblyInformationalVersion => "#{VERSION}", # disposed as product version in explorer
      :CLSCompliantAttribute => false,
      :AssemblyConfiguration => "#{CONFIGURATION}",
      :Guid => Projects[:tx_autofac][:guid]
    asm.copyright = Projects[:tx_autofac][:copyright]
    asm.output_file = 'src/TxAutofacAssemblyInfo.cs'
  end
  
  assemblyinfo :tx_fsharpapi_version do |asm|
    asm.product_name = asm.title = Projects[:tx_fsharpapi][:title]
    asm.description = Projects[:tx_fsharpapi][:description]
    # This is the version number used by framework during build and at runtime to locate, link and load the assemblies. When you add reference to any assembly in your project, it is this version number which gets embedded.
    asm.version = VERSION
    # Assembly File Version : This is the version number given to file as in file system. It is displayed by Windows Explorer. Its never used by .NET framework or runtime for referencing.
    asm.file_version = VERSION_INFORMAL
    asm.custom_attributes :AssemblyInformationalVersion => "#{VERSION}", # disposed as product version in explorer
      :CLSCompliantAttribute => false,
      :AssemblyConfiguration => "#{CONFIGURATION}",
      :Guid => Projects[:tx_fsharpapi][:guid]
    asm.copyright = Projects[:tx_fsharpapi][:copyright]
    asm.output_file = 'src/TxFSharpAPIAssemblyInfo.cs'
  end
  
  assemblyinfo :tx_io_version do |asm|
    asm.product_name = asm.title = Projects[:tx_io][:title]
    asm.description = Projects[:tx_io][:description]
    # This is the version number used by framework during build and at runtime to locate, link and load the assemblies. When you add reference to any assembly in your project, it is this version number which gets embedded.
    asm.version = VERSION
    # Assembly File Version : This is the version number given to file as in file system. It is displayed by Windows Explorer. Its never used by .NET framework or runtime for referencing.
    asm.file_version = VERSION_INFORMAL
    asm.custom_attributes :AssemblyInformationalVersion => "#{VERSION}", # disposed as product version in explorer
      :CLSCompliantAttribute => false,
      :AssemblyConfiguration => "#{CONFIGURATION}",
      :Guid => Projects[:tx_io][:guid]
    asm.copyright = Projects[:tx_io][:copyright]
    asm.output_file = 'src/TxIOAssemblyInfo.cs'
  end
  
  #                    OUTPUTTING
  # ===================================================
  task :output => [:tx_output, :autotx_output, :io_output, :io_autofac_output, :io_windsor_output, :tx_autofac_output, :tx_fsharpapi_output, :tx_io_output] do
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

  task :io_output => :msbuild do
    target = File.join(Folders[:binaries], Projects[:io][:dir])
    copy_files Folders[:io_out], "*.{xml,dll,pdb,config}", target
    CLEAN.include(target)
  end
  task :io_autofac_output => :msbuild do
    target = File.join(Folders[:binaries], Projects[:io_autofac][:dir])
    copy_files Folders[:io_autofac_out], "*.{xml,dll,pdb,config}", target
    CLEAN.include(target)
  end
  task :io_windsor_output => :msbuild do
    target = File.join(Folders[:binaries], Projects[:io_windsor][:dir])
    copy_files Folders[:io_windsor_out], "*.{xml,dll,pdb,config}", target
    CLEAN.include(target)
  end
  task :tx_autofac_output => :msbuild do
    target = File.join(Folders[:binaries], Projects[:tx_autofac][:dir])
    copy_files Folders[:tx_autofac_out], "*.{xml,dll,pdb,config}", target
    CLEAN.include(target)
  end
  task :tx_fsharpapi_output => :msbuild do
    target = File.join(Folders[:binaries], Projects[:tx_fsharpapi][:dir])
    copy_files Folders[:tx_fsharpapi_out], "*.{xml,dll,pdb,config}", target
    CLEAN.include(target)
  end
  task :tx_io_output => :msbuild do
    target = File.join(Folders[:binaries], Projects[:tx_io][:dir])
    copy_files Folders[:tx_io_out], "*.{xml,dll,pdb,config}", target
    CLEAN.include(target)
  end
  
  #                     ILMERGE
  # ===================================================
  
  task :ilmerge => [:tx_ilmerge]
  
  ilmerge :tx_ilmerge => :tx_output do |ilm|
    ilm.output = "#{Projects[:tx][:id]}.dll"
    ilm.internalize = File.join(File.realpath('buildscripts'), 'internalize.txt')
    ilm.working_directory = File.join(Folders[:binaries],  Projects[:tx][:dir])
    ilm.target = :library
    ilm.use :"#{FRAMEWORK}"
    ilm.log = File.join("..", 'tx-ilmerge.log')
    ilm.allow_dupes = true
    ilm.references = [ 'Castle.Transactions.dll', 'System.CoreEx.dll', 'System.Interactive.dll', 'System.Reactive.dll' ]
 end

  # ilmerge :autotx_ilmerge => :autotx_output do |ilm|
    # ilm.output = File.join(Folders[:autotx_out], "#{Projects[:autotx][:id]}.dll")
    # ilm.internalize = File.join('buildscripts', 'internalize.txt')
    # ilm.working_directory = Folders[:autotx_out]
    # ilm.target = :library
    # ilm.use FRAMEWORK
    # ilm.log = File.join( Folders[:autotx_out], "..", 'ilmerge.log' )
    # ilm.allow_dupes = true
    # ilm.references = [ "#{Projects[:autotx][:id]}.dll", 'System.CoreEx.dll', 'System.Interactive.dll', 'System.Reactive.dll' ]
 # end

  
  
  #                     TESTING
  # ===================================================
  directory "#{Folders[:tests]}"
  
  task :tx_test => [:msbuild, "#{Folders[:tests]}", :tx_nunit, :tx_test_publish_artifacts]
  task :autotx_test => [:msbuild, "#{Folders[:tests]}", :autotx_nunit, :autotx_test_publish_artifacts]
  task :io_test => [:msbuild, "#{Folders[:tests]}", :io_nunit, :io_test_publish_artifacts]
  task :tx_io_test => [:msbuild, "#{Folders[:tests]}", :tx_io_nunit, :tx_io_test_publish_artifacts]
  
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
  
  nunit :io_nunit do |nunit|
    nunit.command = Commands[:nunit]
    nunit.options '/framework v4.0', "/out #{Files[:io][:test_log]}", "/xml #{Files[:io][:test_xml]}"
    nunit.assemblies Files[:io][:test]
	  CLEAN.include(Folders[:tests])
  end
  
  task :io_test_publish_artifacts => :io_nunit do
    puts "##teamcity[publishArtifacts path='#{Files[:io][:test_xml]}']"
    puts "##teamcity[publishArtifacts '#{Files[:io][:test_log]}']"
  end
  
  nunit :tx_io_nunit do |nunit|
    nunit.command = Commands[:nunit]
    nunit.options '/framework v4.0', "/out #{Files[:tx_io][:test_log]}", "/xml #{Files[:tx_io][:test_xml]}"
    nunit.assemblies Files[:tx_io][:test]
	CLEAN.include(Folders[:tests])
  end
  
  task :tx_io_test_publish_artifacts => :tx_io_nunit do
	puts "##teamcity[publishArtifacts path='#{Files[:tx_io][:test_xml]}']"
	puts "##teamcity[publishArtifacts '#{Files[:tx_io][:test_log]}']"
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
    nuspec.projectUrl = "https://github.com/castleproject/Castle.Transactions"
    nuspec.language = "en-US"
    nuspec.licenseUrl = "http://www.apache.org/licenses/LICENSE-2.0"	
    nuspec.requireLicenseAcceptance = "true"
	nuspec.framework_assembly "System.Transactions", FRAMEWORK
    nuspec.output_file = Files[:tx][:nuspec]
    #nuspec.working_directory = Folders[:tx_nuspec]

    nuspec_copy(:tx, "Castle.Transactions.{dll,xml,pdb}")
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
    nuspec.projectUrl = "https://github.com/castleproject/Castle.Transactions"
    nuspec.language = "en-US"
    nuspec.licenseUrl = "http://www.apache.org/licenses/LICENSE-2.0"
    nuspec.requireLicenseAcceptance = "true"
    nuspec.dependency "Castle.Core", "3.0.0.4001"
    nuspec.dependency "Castle.Windsor", "3.0.0.4001"
    nuspec.dependency Projects[:tx][:id], "[#{VERSION}]" # exactly equals
    nuspec.dependency Projects[:io][:id], "[#{VERSION}]" # exactly equals
    nuspec.dependency Projects[:tx_io][:id], "[#{VERSION}]" # exactly equals
	nuspec.framework_assembly "System.Transactions", FRAMEWORK
    nuspec.output_file = Files[:autotx][:nuspec]
    #nuspec.working_directory = Folders[:autotx_nuspec]
    
    nuspec_copy(:autotx, "Castle.Facilities.AutoTx.{dll,xml,pdb}")
	# right now, we'll go with the conventions
	#.each{ |ff| nuspec.file ff }
	
    CLEAN.include(Folders[:autotx_nuspec])
  end
  
  file "#{Files[:io][:nuspec]}"
  
  nuspec :io_nuspec => [:output, :io_nuget_dirs] do |nuspec|
    nuspec.id = Projects[:io][:id]
    nuspec.version = VERSION
    nuspec.authors = Projects[:io][:authors]
    nuspec.description = Projects[:io][:description]
    nuspec.title = Projects[:io][:title]
    nuspec.projectUrl = "https://github.com/castleproject/Castle.Transactions"
    nuspec.language = "en-US"
    nuspec.licenseUrl = "http://www.apache.org/licenses/LICENSE-2.0"
    nuspec.requireLicenseAcceptance = "true"
    nuspec.output_file = Files[:io][:nuspec]
    #nuspec.working_directory = Folders[:io_nuspec]
    
    nuspec_copy(:io, "Castle.IO.{dll,xml,pdb}")
	# right now, we'll go with the conventions
	#.each{ |ff| nuspec.file ff }
	
    CLEAN.include(Folders[:io_nuspec])
  end
  
  file "#{Files[:io_autofac][:nuspec]}"
  
  nuspec :io_autofac_nuspec => [:output, :io_autofac_nuget_dirs] do |nuspec|
    nuspec.id = Projects[:io_autofac][:id]
    nuspec.version = VERSION
    nuspec.authors = Projects[:io_autofac][:authors]
    nuspec.description = Projects[:io_autofac][:description]
    nuspec.title = Projects[:io_autofac][:title]
    nuspec.projectUrl = "https://github.com/castleproject/Castle.Transactions"
    nuspec.language = "en-US"
    nuspec.licenseUrl = "http://www.apache.org/licenses/LICENSE-2.0"
    nuspec.requireLicenseAcceptance = "true"
    nuspec.dependency "Autofac", "2.5.2.830"
    nuspec.output_file = Files[:io_autofac][:nuspec]
    #nuspec.working_directory = Folders[:io_autofac_nuspec]
    
    nuspec_copy(:io_autofac, "Castle.IO.Autofac.{dll,xml,pdb}")
	# right now, we'll go with the conventions
	#.each{ |ff| nuspec.file ff }
	
    CLEAN.include(Folders[:io_autofac_nuspec])
  end
  
  file "#{Files[:io_windsor][:nuspec]}"
  
  nuspec :io_windsor_nuspec => [:output, :io_windsor_nuget_dirs] do |nuspec|
    nuspec.id = Projects[:io_windsor][:id]
    nuspec.version = VERSION
    nuspec.authors = Projects[:io_windsor][:authors]
    nuspec.description = Projects[:io_windsor][:description]
    nuspec.title = Projects[:io_windsor][:title]
    nuspec.projectUrl = "https://github.com/castleproject/Castle.Transactions"
    nuspec.language = "en-US"
    nuspec.licenseUrl = "http://www.apache.org/licenses/LICENSE-2.0"
    nuspec.requireLicenseAcceptance = "true"
    nuspec.dependency "Castle.Core", "3.0.0.4001"
    nuspec.dependency "Castle.Windsor", "3.0.0.4001"
    nuspec.dependency Projects[:io][:id], "[#{VERSION}]" # exactly equals
    nuspec.output_file = Files[:io_windsor][:nuspec]
    #nuspec.working_directory = Folders[:io_windsor_nuspec]
    
    nuspec_copy(:io_windsor, "Castle.IO.Windsor.{dll,xml,pdb}")
	# right now, we'll go with the conventions
	#.each{ |ff| nuspec.file ff }
	
    CLEAN.include(Folders[:io_windsor_nuspec])
  end
  
  file "#{Files[:tx_autofac][:nuspec]}"
  
  nuspec :tx_autofac_nuspec => [:output, :tx_autofac_nuget_dirs] do |nuspec|
    nuspec.id = Projects[:tx_autofac][:id]
    nuspec.version = VERSION
    nuspec.authors = Projects[:tx_autofac][:authors]
    nuspec.description = Projects[:tx_autofac][:description]
    nuspec.title = Projects[:tx_autofac][:title]
    nuspec.projectUrl = "https://github.com/castleproject/Castle.Transactions"
    nuspec.language = "en-US"
    nuspec.licenseUrl = "http://www.apache.org/licenses/LICENSE-2.0"
    nuspec.requireLicenseAcceptance = "true"
    nuspec.dependency "Autofac", "2.5.2.830"
    nuspec.output_file = Files[:tx_autofac][:nuspec]
    #nuspec.working_directory = Folders[:tx_autofac_nuspec]
    
    nuspec_copy(:tx_autofac, "Castle.Transactions.Autofac.{dll,xml,pdb}")
	# right now, we'll go with the conventions
	#.each{ |ff| nuspec.file ff }
	
    CLEAN.include(Folders[:tx_autofac_nuspec])
  end
  
  file "#{Files[:tx_fsharpapi][:nuspec]}"
  
  nuspec :tx_fsharpapi_nuspec => [:output, :tx_fsharpapi_nuget_dirs] do |nuspec|
    nuspec.id = Projects[:tx_fsharpapi][:id]
    nuspec.version = VERSION
    nuspec.authors = Projects[:tx_fsharpapi][:authors]
    nuspec.description = Projects[:tx_fsharpapi][:description]
    nuspec.title = Projects[:tx_fsharpapi][:title]
    nuspec.projectUrl = "https://github.com/castleproject/Castle.Transactions"
    nuspec.language = "en-US"
    nuspec.licenseUrl = "http://www.apache.org/licenses/LICENSE-2.0"
    nuspec.requireLicenseAcceptance = "true"
    nuspec.dependency Projects[:io][:id], "[#{VERSION}]" # exactly equals
    nuspec.dependency Projects[:tx][:id], "[#{VERSION}]" # exactly equals
    nuspec.dependency Projects[:tx_io][:id], "[#{VERSION}]" # exactly equals
	nuspec.framework_assembly "FSharp.Core", FRAMEWORK
    nuspec.output_file = Files[:tx_fsharpapi][:nuspec]
    #nuspec.working_directory = Folders[:tx_fsharpapi_nuspec]
    
    nuspec_copy(:tx_fsharpapi, "Castle.Transactions.FSharpAPI.{dll,xml,pdb}")
	# right now, we'll go with the conventions
	#.each{ |ff| nuspec.file ff }
	
    CLEAN.include(Folders[:tx_fsharpapi_nuspec])
  end
  
  file "#{Files[:tx_io][:nuspec]}"
  
  nuspec :tx_io_nuspec => [:output, :tx_io_nuget_dirs] do |nuspec|
    nuspec.id = Projects[:tx_io][:id]
    nuspec.version = VERSION
    nuspec.authors = Projects[:tx_io][:authors]
    nuspec.description = Projects[:tx_io][:description]
    nuspec.title = Projects[:tx_io][:title]
    nuspec.projectUrl = "https://github.com/castleproject/Castle.Transactions"
    nuspec.language = "en-US"
    nuspec.licenseUrl = "http://www.apache.org/licenses/LICENSE-2.0"
    nuspec.requireLicenseAcceptance = "true"
    nuspec.dependency "Castle.Core", "3.0.0.4001"
    nuspec.dependency Projects[:io][:id], "[#{VERSION}]" # exactly equals
    nuspec.dependency Projects[:tx][:id], "[#{VERSION}]" # exactly equals
	nuspec.framework_assembly "System.Transactions", FRAMEWORK
    nuspec.output_file = Files[:tx_io][:nuspec]
    #nuspec.working_directory = Folders[:tx_io_nuspec]
    
    nuspec_copy(:tx_io, "Castle.Transactions.IO.{dll,xml,pdb}")
	# right now, we'll go with the conventions
	#.each{ |ff| nuspec.file ff }
	
    CLEAN.include(Folders[:tx_io_nuspec])
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
  
  desc "generate nuget package for Transactions"
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
  
  nuget_directory(:io)
  
  desc "generate nuget package for io facility"
  nugetpack :io_nuget => [:output, :io_nuspec] do |nuget|
	nuget.command     = Commands[:nuget]
    nuget.nuspec      = Files[:io][:nuspec]
    nuget.output      = Folders[:nuget]
  end

  nuget_directory(:io_autofac)
  
  desc "generate nuget package for io_autofac facility"
  nugetpack :io_autofac_nuget => [:output, :io_autofac_nuspec] do |nuget|
	nuget.command     = Commands[:nuget]
    nuget.nuspec      = Files[:io_autofac][:nuspec]
    nuget.output      = Folders[:nuget]
  end

  nuget_directory(:io_windsor)
  
  desc "generate nuget package for io_windsor facility"
  nugetpack :io_windsor_nuget => [:output, :io_windsor_nuspec] do |nuget|
	nuget.command     = Commands[:nuget]
    nuget.nuspec      = Files[:io_windsor][:nuspec]
    nuget.output      = Folders[:nuget]
  end

  nuget_directory(:tx_autofac)
  
  desc "generate nuget package for tx_autofac facility"
  nugetpack :tx_autofac_nuget => [:output, :tx_autofac_nuspec] do |nuget|
	nuget.command     = Commands[:nuget]
    nuget.nuspec      = Files[:tx_autofac][:nuspec]
    nuget.output      = Folders[:nuget]
  end

  nuget_directory(:tx_fsharpapi)
  
  desc "generate nuget package for tx_fsharpapi facility"
  nugetpack :tx_fsharpapi_nuget => [:output, :tx_fsharpapi_nuspec] do |nuget|
	nuget.command     = Commands[:nuget]
    nuget.nuspec      = Files[:tx_fsharpapi][:nuspec]
    nuget.output      = Folders[:nuget]
  end

  nuget_directory(:tx_io)
  
  desc "generate nuget package for tx_io facility"
  nugetpack :tx_io_nuget => [:output, :tx_io_nuspec] do |nuget|
	nuget.command     = Commands[:nuget]
    nuget.nuspec      = Files[:tx_io][:nuspec]
    nuget.output      = Folders[:nuget]
  end

  task :nuget_push => [:tx_nuget_push, :autotx_nuget_push, :io_nuget_push, :io_autofac_nuget_push, :io_windsor_nuget_push, :tx_autofac_nuget_push, :tx_fsharpapi_nuget_push, :tx_io_nuget_push]
  
  def nuget_key()
	File.open( Files[:nuget_private_key] , "r") do |f|
		return f.gets
	end
  end
  
  task :tx_nuget_push do
	package = "#{Projects[:tx][:id]}.#{VERSION}.nupkg"
    sh "#{Commands[:nuget]} push -source #{Uris[:nuget_offical]} #{package} #{nuget_key()}"
  end
  
  task :autotx_nuget_push do
    package = "#{Projects[:autotx][:id]}.#{VERSION}.nupkg"
    sh "#{Commands[:nuget]} push -source #{Uris[:nuget_offical]} #{package} #{nuget_key()}"
  end

  task :io_nuget_push do
    package = "#{Projects[:io][:id]}.#{VERSION}.nupkg"
    sh "#{Commands[:nuget]} push -source #{Uris[:nuget_offical]} #{package} #{nuget_key()}"
  end

  task :io_autofac_nuget_push do
    package = "#{Projects[:io_autofac][:id]}.#{VERSION}.nupkg"
    sh "#{Commands[:nuget]} push -source #{Uris[:nuget_offical]} #{package} #{nuget_key()}"
  end

  task :io_windsor_nuget_push do
    package = "#{Projects[:io_windsor][:id]}.#{VERSION}.nupkg"
    sh "#{Commands[:nuget]} push -source #{Uris[:nuget_offical]} #{package} #{nuget_key()}"
  end

  task :tx_autofac_nuget_push do
    package = "#{Projects[:tx_autofac][:id]}.#{VERSION}.nupkg"
    sh "#{Commands[:nuget]} push -source #{Uris[:nuget_offical]} #{package} #{nuget_key()}"
  end

  task :tx_fsharpapi_nuget_push do
    package = "#{Projects[:tx_fsharpapi][:id]}.#{VERSION}.nupkg"
    sh "#{Commands[:nuget]} push -source #{Uris[:nuget_offical]} #{package} #{nuget_key()}"
  end

  task :tx_io_nuget_push do
    package = "#{Projects[:tx_io][:id]}.#{VERSION}.nupkg"
    sh "#{Commands[:nuget]} push -source #{Uris[:nuget_offical]} #{package} #{nuget_key()}"
  end
end

desc "display rake task help"  
task :help do
  puts ""
  puts " Castle Transactions & AutoTx Facility (c)Henrik Feldt/Castle Project 2011"
  puts " ========================================================================="
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
