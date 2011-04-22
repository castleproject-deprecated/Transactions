root_folder = File.expand_path("#{File.dirname(__FILE__)}/..")

Folders = {
  :root => root_folder,
  :src => "src",
  :out => "build",
  :tests => File.join("build", "tests"),
  :tools => "tools",
  :nunit => File.join("tools", "NUnit", "bin"),
  
  :packages => "packages",
  :nuspec_tx => File.join("packages", Projects[:tx][:dir]),
  :nuspec_autotx => File.join("packages", Projects[:autotx][:dir]),
  :nuget_out => "nuget",
  
  :tx_out => 'placeholder - specify build environment',
  :tx_test_out => 'placeholder - specify build environment',
  :autotx_out => 'placeholder - specify build environment',
  :autotx_test_out => 'placeholder - specify build environment',
  :binaries => "placeholder - specify build environment"
}

Commands = {
  :nunit => File.join(Folders[:nunit], "nunit-console.exe"),
  :nupack => File.join(Folders[:tools], "NuPack.exe"),
  :nuget => File.join(Folders[:tools], "NuGet.exe"),
  :ilmerge => File.join(Folders[:tools], "ILMerge.exe")
}

Files = {
  :sln => "Castle.Services.Transaction.sln",
  :nuspec_tx => File.join(Folders[:nuspec_tx], "#{Projects[:tx][:dir]}.nuspec"),
  :nuspec_autotx => File.join(Folders[:nuspec_autotx], "#{Projects[:autotx][:dir]}.nuspec"),
  :version => "VERSION",
  
  :tx_test => 'placeholder - specify build environment',
  :autotx_test => 'placeholder - specify build environment'
}