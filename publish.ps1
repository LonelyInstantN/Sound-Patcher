$ErrorActionPreference = 'Stop'
Remove-Item -Recurse -Force "$PSScriptRoot\publish" -ErrorAction SilentlyContinue
dotnet publish "$PSScriptRoot\SoundSwitcher.csproj" -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeAllContentForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o "$PSScriptRoot\publish"
Write-Host "Done: $PSScriptRoot\publish\SoundSwitcher.exe"
