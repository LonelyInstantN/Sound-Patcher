$ErrorActionPreference = 'Stop'
Remove-Item -Recurse -Force "$PSScriptRoot\publish" -ErrorAction SilentlyContinue
dotnet publish "$PSScriptRoot\SoundPatcher.csproj" -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeAllContentForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o "$PSScriptRoot\publish"
Write-Host "Done: $PSScriptRoot\publish\Sound Patcher.exe"
