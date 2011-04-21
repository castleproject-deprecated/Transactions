root_folder = File.expand_path("#{File.dirname(__FILE__)}/..")

Folders = {
  :root => root_folder,
  :src => "src",
  :outdir => "build",
  :tests => File.join("build", "tests"),
  :binaries => "placeholder - specify build environment",
  :tools => "tools",
  :nunit => File.join("tools", "NUnit", "bin"),
  :packages => "packages",
  :nuspec_tx => File.join("packages", "Castle.Services.Transaction"),
  :nuspec_autotx => File.join("packages", "Castle.Facilities.AutoTx"),
  :nuget_out => "nuget"
}

Commands = {
  :nunit => File.join(Folders[:nunit], "nunit-console.exe"),
  :nupack => File.join(Folders[:tools], "NuPack.exe"),
  :nuget => File.join(Folders[:tools], "NuGet.exe"),
  :ilmerge => File.join(Folders[:tools], "ILMerge.exe")
}

Files = {
  :sln => "Castle.Services.Transaction.sln",
  :nuspec_tx => File.join(Folders[:nuspec_tx], "Castle.Services.Transaction.nuspec"),
  :nuspec_autotx => File.join(Folders[:nuspec_autotx], "Castle.Facilities.AutoTx.nuspec"),
  :version => "VERSION"
}