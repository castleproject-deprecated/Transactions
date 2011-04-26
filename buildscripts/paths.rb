root_folder = File.expand_path("#{File.dirname(__FILE__)}/..")

Folders = {
  :root => root_folder,
  :src => "src",
  :out => "build",
  :tests => File.join("build", "tests"),
  :tools => "tools",
  :nunit => File.join("tools", "NUnit", "bin"),
  
  :packages => "packages",
  :tx_nuspec => File.join("packages", Projects[:tx][:dir]),
  :autotx_nuspec => File.join("packages", Projects[:autotx][:dir]),
  :nuget => File.join("build", "nuget"),
  
  :tx_out => 'placeholder - specify build environment',
  :tx_test_out => 'placeholder - specify build environment',
  :autotx_out => 'placeholder - specify build environment',
  :autotx_test_out => 'placeholder - specify build environment',
  :binaries => "placeholder - specify build environment"
}

Files = {
  :sln => "Castle.Services.Transaction.sln",
  :version => "VERSION",
  
  :tx => {
    :nuspec => File.join(Folders[:tx_nuspec], "#{Projects[:tx][:dir]}.nuspec"),
	:test_log => File.join(Folders[:tests], "Castle.Services.Transaction.Tests.log"),
	:test_xml => File.join(Folders[:tests], "Castle.Services.Transaction.Tests.xml"),
	
	:test => 'ex: Castle.Services.Transaction.Tests.dll'
  },
  
  :autotx => {
    :nuspec => File.join(Folders[:autotx_nuspec], "#{Projects[:autotx][:dir]}.nuspec"),
	:test_log => File.join(Folders[:tests], "Castle.Facilities.AutoTx.Tests.log"),
	:test_xml => File.join(Folders[:tests], "Castle.Facilities.AutoTx.Tests.xml"),
	
	:test => 'ex: build/.../Castle.Facilities.AutoTx.Tests.dll'
  }
}

Commands = {
  :nunit => File.join(Folders[:nunit], "nunit-console.exe"),
  :nuget => File.join(Folders[:tools], "NuGet.exe"),
  :ilmerge => File.join(Folders[:tools], "ILMerge.exe")
}