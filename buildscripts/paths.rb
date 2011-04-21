root_folder = File.expand_path("#{File.dirname(__FILE__)}/..")

Folders = {
  :root => root_folder,
  :src => File.join(root_folder, "src"),
  :outdir => File.join(root_folder, "build"),
  :tests => File.join(root_folder, "build", "tests"),
  :binaries => "placeholder - specify build environment",
  :tools => File.join(root_folder, "tools"),
  :nunit => File.join(root_folder, "tools", "NUnit", "bin"),
  :packages => File.join(root_folder, "packages"),
  :nuspec_tx => File.join(root_folder, "packages", "Castle.Services.Transaction"),
  :nuspec_autotx => File.join(root_folder, "packages", "Castle.Facilities.AutoTx"),
  :nuget_out => File.join(root_folder, "nuget")
}

Commands = {
  :nunit => File.join(Folders[:nunit], "nunit-console.exe"),
  :nupack => File.join(Folders[:tools], "NuPack.exe"),
  :nuget => File.join(Folders[:tools], "NuGet.exe"),
  :ilmerge => File.join(Folders[:tools], "ILMerge.exe")
}

Files = {
  :sln => File.join(Folders[:root], "Castle.Services.Transaction.sln"),
  :nuspec_tx => File.join(Folders[:packages], "Castle.Services.Transaction.nuspec"),
  :nuspec_autotx => File.join(Folders[:packages], "Castle.Facilities.AutoTx.nuspec"),
  :version => File.join(Folders[:root], "VERSION")
}