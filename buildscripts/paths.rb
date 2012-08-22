root_folder = File.expand_path("#{File.dirname(__FILE__)}/..")

Folders = {
  :root => root_folder,
  :src => "src",
  :out => "build",
  :tests => File.join("build", "tests"),
  :tools => "tools",
  :nunit => File.join("tools", "NUnit", "bin"),
  
  :packages => "packages",
  :tx_nuspec => File.join("build", "nuspec", Projects[:tx][:dir]),
  :autotx_nuspec => File.join("build", "nuspec", Projects[:autotx][:dir]),
  :io_nuspec => File.join("build", "nuspec", Projects[:io][:dir]),
  :io_autofac_nuspec => File.join("build", "nuspec", Projects[:io_autofac][:dir]),
  :io_windsor_nuspec => File.join("build", "nuspec", Projects[:io_windsor][:dir]),
  :tx_autofac_nuspec => File.join("build", "nuspec", Projects[:tx_autofac][:dir]),
  :tx_fsharpapi_nuspec => File.join("build", "nuspec", Projects[:tx_fsharpapi][:dir]),
  :tx_io_nuspec => File.join("build", "nuspec", Projects[:tx_io][:dir]),

  :nuget => File.join("build", "nuget"),
  
  :tx_out => 'placeholder - specify build environment',
  :tx_test_out => 'placeholder - specify build environment',
  :autotx_out => 'placeholder - specify build environment',
  :autotx_test_out => 'placeholder - specify build environment',
  :io_out => 'placeholder - specify build environment',
  :io_test_out => 'placeholder - specify build environment',
  :io_autofac_out => 'placeholder - specify build environment',
  :io_windsor_out => 'placeholder - specify build environment',
  :tx_autofac_out => 'placeholder - specify build environment',
  :tx_fsharpapi_out => 'placeholder - specify build environment',
  :tx_io_out => 'placeholder - specify build environment',
  :tx_io_test_out => 'placeholder - specify build environment',
  :binaries => "placeholder - specify build environment"
}

Files = {
  :sln => "Castle.Transactions.sln",
  :version => "VERSION",
  :nuget_private_key => "NUGET_KEY",
  
  :tx => {
    :nuspec => File.join(Folders[:tx_nuspec], "#{Projects[:tx][:id]}.nuspec"),
	:test_log => File.join(Folders[:tests], "Castle.Transactions.Tests.log"),
	:test_xml => File.join(Folders[:tests], "Castle.Transactions.Tests.xml"),
	
	:test => 'ex: Castle.Transactions.Tests.dll'
  },
  
  :autotx => {
    :nuspec => File.join(Folders[:autotx_nuspec], "#{Projects[:autotx][:id]}.nuspec"),
	:test_log => File.join(Folders[:tests], "Castle.Facilities.AutoTx.Tests.log"),
	:test_xml => File.join(Folders[:tests], "Castle.Facilities.AutoTx.Tests.xml"),
	
	:test => 'ex: build/.../Castle.Facilities.AutoTx.Tests.dll'
  },

  :io => {
    :nuspec => File.join(Folders[:io_nuspec], "#{Projects[:io][:id]}.nuspec"),
	:test_log => File.join(Folders[:tests], "Castle.IO.Tests.log"),
	:test_xml => File.join(Folders[:tests], "Castle.IO.Tests.xml"),
	
	:test => 'ex: build/.../Castle.IO.Tests.dll'
  },

  :io_autofac => {
    :nuspec => File.join(Folders[:io_autofac_nuspec], "#{Projects[:io_autofac][:id]}.nuspec"),
  },

  :io_windsor => {
    :nuspec => File.join(Folders[:io_windsor_nuspec], "#{Projects[:io_windsor][:id]}.nuspec"),
  },

  :tx_autofac => {
    :nuspec => File.join(Folders[:tx_autofac_nuspec], "#{Projects[:tx_autofac][:id]}.nuspec"),
  },

  :tx_fsharpapi => {
    :nuspec => File.join(Folders[:tx_fsharpapi_nuspec], "#{Projects[:tx_fsharpapi][:id]}.nuspec"),
  },

  :tx_io => {
    :nuspec => File.join(Folders[:tx_io_nuspec], "#{Projects[:tx_io][:id]}.nuspec"),
	:test_log => File.join(Folders[:tests], "Castle.Transactions.IO.Tests.log"),
	:test_xml => File.join(Folders[:tests], "Castle.Transactions.IO.Tests.xml"),
	
	:test => 'ex: build/.../Castle.Transactions.IO.Tests.dll'
  }

}

Commands = {
  :nunit => File.join(Folders[:nunit], "nunit-console.exe"),
  :nuget => File.join(Folders[:tools], "NuGet.exe"),
  :ilmerge => File.join(Folders[:tools], "ILMerge.exe")
}

Uris = {
  :nuget_offical => "http://packages.nuget.org/v1/"
}