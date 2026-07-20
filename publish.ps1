$ErrorActionPreference = 'Stop'
Remove-Item -Recurse -Force "$PSScriptRoot\publish" -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force "$PSScriptRoot\publish_dir" -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force "$PSScriptRoot\bin" -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force "$PSScriptRoot\obj" -ErrorAction SilentlyContinue
dotnet publish "$PSScriptRoot\SoundPatcher.csproj" -c Release -r win-x64 -p:SelfContained=true `
    -p:PublishSingleFile=true `
    -p:IncludeAllContentForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o "$PSScriptRoot\publish"
Write-Host "Done: $PSScriptRoot\publish\Sound Patcher.exe"
