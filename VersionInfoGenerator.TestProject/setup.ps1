if (Test-Path "$PSScriptRoot/nuget-pkgs") {
  Remove-Item -Recurse -Force -ErrorAction Stop "$PSScriptRoot/nuget-pkgs"
}

dotnet build -c Release "$PSScriptRoot/../VersionInfoGenerator"
dotnet pack "$PSScriptRoot/../VersionInfoGenerator" `
  -o "$PSScriptRoot/nuget-pkgs/src" `
  -c Release `
  -p:DebugType=pdbonly `
  -p:DebugSymbols=true
