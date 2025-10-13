# First, ensure that dotnet telemetry is disabled
$telemetry_off = 0
if (Test-Path env:DOTNET_CLI_TELEMETRY_OPTOUT) {
    if ($env:DOTNET_CLI_TELEMETRY_OPTOUT -eq 1) {
        $telemetry_off = 1
    }
}

$telemetry_verified_off = 0
if ($telemetry_off -ne 1) {
    Write-Host "$([char]27)[1;93mwarning: it appears that you have not yet disabled telemetry in dotnet$([char]27)[0m"
    Write-Host "Disabling..."
    [Environment]::SetEnvironmentVariable('DOTNET_CLI_TELEMETRY_OPTOUT', 1, 'User')
    try {
        [Environment]::SetEnvironmentVariable('DOTNET_CLI_TELEMETRY_OPTOUT', 1, 'Machine')
    }
    catch {

    }

    $env:DOTNET_CLI_TELEMETRY_OPTOUT = 1 # Reflect updated value for the rest of the current session, in addition to new sessions

    if (Test-Path env:DOTNET_CLI_TELEMETRY_OPTOUT) {
        if ($env:DOTNET_CLI_TELEMETRY_OPTOUT -eq 1) {
            $telemetry_verified_off = 1
            Write-Host "$([char]27)[1;32mDisabling of dotnet telemetry succeeded $([Char]::ConvertFromUtf32(0x1F4CE))$([char]27)[0m"
        }
    }

    if ($telemetry_verified_off -ne 1) {
        Write-Host "$([char]27)[1;91merror: unable to disable dotnet telemetry$([char]27)[0m"
        Write-Host "Please set the DOTNET_CLI_TELEMETRY_OPTOUT environment variable equal to 1 and then re-run this script"
        Write-Host "(https://learn.microsoft.com/en-us/dotnet/core/tools/telemetry#how-to-opt-out)"
        exit 1
    }
}

# Move to the directory where this script resides
try {
    pushd "$PSScriptRoot"

    # Make sure bin directory exists before attempting to store files there
    New-Item -Path "bin" -ItemType Directory -Force

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
}
finally {
    popd
}

Write-Host ""
Write-Host "build.ps1: Build successful"
Write-Host "To run:"
Write-Host ".\bin\play.exe <your-spc-file>"