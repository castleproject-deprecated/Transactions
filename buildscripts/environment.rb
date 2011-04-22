
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
	Files[:tx_test] = File.join(Folders[:tx_test_out], "#{Projects[:tx][:test_dir]}.dll")
	CLEAN.include(Folders[:tx_test_out])
	
    Folders[:autotx_test_out] = File.join(Folders[:src], Projects[:autotx][:test_dir], 'bin', CONFIGURATION)
	Files[:autotx_test] = File.join(Folders[:autotx_test_out], "#{Projects[:autotx][:test_dir]}.dll")
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
end
