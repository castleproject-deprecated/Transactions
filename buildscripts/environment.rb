namespace :env do

  # Semantic Versioning! Thanks Seb!
  # major.minor.build.revision
  task :common do

  s = SemVer.find
  ENV['VERSION_BASE'] = VERSION_BASE = s.to_s
	
	# version management
	fv = version(VERSION_BASE)
	build = ENV['BUILD_NUMBER'] || fv[2]
	revision = ENV['OFFICIAL_RELEASE'] || (fv[3] == 0 ? Time.now.strftime('%j%H') : fv[3]) #  (day of year 0-265)(hour 00-24)
	
	real_version = [fv[0], fv[1], build, revision]
	
    ENV['VERSION'] = VERSION = real_version.join(".")
	ENV['VERSION_INFORMAL'] = VERSION_INFORMAL = real_version.join(".")
	puts "Assembly Version: #{VERSION}."
	puts "##teamcity[buildNumber '#{VERSION}']" # print the version (revision) and build number to ci
	
	# configuration management
	ENV['FRAMEWORK'] = FRAMEWORK = ENV['FRAMEWORK'] || (Rake::Win32::windows? ? "net40" : "mono28")
	puts "Framework: #{FRAMEWORK}"
  end
  
  # configure the output directories
  task :configure, [:str] do |t, args|
    ENV['CONFIGURATION'] = CONFIGURATION = args[:str]
    Folders[:binaries] = File.join(Folders[:out], FRAMEWORK, args[:str].downcase)
	CLEAN.include(File.join(Folders[:binaries], "*"))
  end
  
  task :set_dirs do
	Folders[:tx_out] = File.join(Folders[:src], Projects[:tx][:dir], 'bin', CONFIGURATION)
	CLEAN.include(Folders[:tx_out])
	
    Folders[:autotx_out] = File.join(Folders[:src], Projects[:autotx][:dir], 'bin', CONFIGURATION)
	CLEAN.include(Folders[:autotx_out])
	
	# for tests
	Folders[:tx_test_out] = File.join(Folders[:src], Projects[:tx][:test_dir], 'bin', CONFIGURATION)
	Files[:tx][:test] = File.join(Folders[:tx_test_out], "#{Projects[:tx][:test_dir]}.dll")
	CLEAN.include(Folders[:tx_test_out])
	
    Folders[:autotx_test_out] = File.join(Folders[:src], Projects[:autotx][:test_dir], 'bin', CONFIGURATION)
	Files[:autotx][:test] = File.join(Folders[:autotx_test_out], "#{Projects[:autotx][:test_dir]}.dll")
	CLEAN.include(Folders[:autotx_test_out])
  end
  
  desc "set debug environment variables"
  task :debug => [:common] do
    Rake::Task["env:configure"].invoke('Debug')
	Rake::Task["env:set_dirs"].invoke
  end
  
  desc "set release environment variables"
  task :release => [:common] do
    Rake::Task["env:configure"].invoke('Release')
	Rake::Task["env:set_dirs"].invoke
  end
  
  desc "set GA envionment variables"
  task :ga do
    puts "##teamcity[progressMessage 'Setting environment variables for GA']"
	ENV['OFFICIAL_RELEASE'] = OFFICIAL_RELEASE = "4000"
  end
  
  desc "set release candidate environment variables"
  task :rc, [:number] do |t, args|
    puts "##teamcity[progressMessage 'Setting environment variables for Release Candidate']"
    arg_num = args[:number].to_i
	num = arg_num != 0 ? arg_num : 1
	ENV['OFFICIAL_RELEASE'] = OFFICIAL_RELEASE = "#{3000 + num}"
  end
  
  desc "set beta-environment variables"
  task :beta, [:number] do |t, args|
    puts "##teamcity[progressMessage 'Setting environment variables for Beta']"
	arg_num = args[:number].to_i
	num = arg_num != 0 ? arg_num : 1
    ENV['OFFICIAL_RELEASE'] = OFFICIAL_RELEASE = "#{2000 + num}"
  end
  
  desc "set alpha environment variables"
  task :alpha, [:number] do |t, args|
    puts "##teamcity[progressMessage 'Setting environment variables for Alpha']"
    arg_num = args[:number].to_i
	num = arg_num != 0 ? arg_num : 1
    ENV['OFFICIAL_RELEASE'] = OFFICIAL_RELEASE = "#{1000 + num}"
  end
end
