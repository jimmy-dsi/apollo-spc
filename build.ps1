# Build zig app
try {
    src/zig-build.bat
}
catch {
    exit 1
}

# Cleanup unnecessary files
Remove-Item -Path "bin/*.pdb" -Force 2> $null
Remove-Item -Path "bin/*.obj" -Force 2> $null

$source = "src/cli/play/Play/_AdditionalHashes-Template.txt"
$target = "src/cli/play/Play/AdditionalHashes.cs"
$content = '"[[[C#___play___apollo-spc-program___C735A0F9___!GenFromCode!]]]"'

# Generate additional hash from the output of zig build
try {
    $hashval = (Get-FileHash -Path "bin/apollo-spc-program.exe" -Algorithm SHA256).Hash
    Copy-Item -Path $source -Destination $target -Force
    (Get-Content $target).Replace($content, """$hashval""") | Set-Content $target
}
catch {
    exit 1
}

# Build dotnet app
try {
    dotnet publish src/cli/play/Play/Play.csproj -r win-x64 -p:PublishAot=true --output bin/
}
catch {
    exit 1
}
finally {
    # Clean up
    Copy-Item -Path $source -Destination $target -Force 2> $null
}

Write-Host ""
Write-Host "build.ps1: Build successful"
Write-Host "To run:"
Write-Host ".\bin\play.exe <your-spc-file>"