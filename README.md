# Apollo

**Apollo** is an S-SMP/S-DSP emulator core written in Zig for Windows and Linux. Its primary purpose is to be used for playing SPC files, but can also be used for general-purpose SNES APU emulation.

In its current state, Apollo comes included with a debugging SPC player using a command-line interface, but the long-term goal is for this to function more as a library rather than a standalone application.

## Releases

The latest stable version is available from the releases on GitHub [TODO: Create initial release].

## Prerequesites Before Running

### Windows

It is **highly** recommended that you install either the [Windows Terminal](https://learn.microsoft.com/en-us/windows/terminal/install) or [msys2](https://www.msys2.org/) if you haven't already. Other terminals such as **cmd** or **Windows Powershell** will ***not*** display the app correctly (Essentially, you need a terminal emulator which supports coloring and cursor re-positioning via ANSI codes).

You will also need to download a copy of [ffplay](https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v4.4.1/ffplay-4.4.1-win-64.zip) ([main download page](https://ffbinaries.com/downloads)) and add it to your system PATH variable. You can do this via a sequence of Powershell commands:

```powershell
$ffplayPath = "C:\path\to\your\ffplay\folder"
$env:Path = $ffplayPath + ";" + $env:Path
$newPath = $ffplayPath + ";" + [System.Environment]::GetEnvironmentVariable("PATH", "User")
[System.Environment]::SetEnvironmentVariable("PATH", $newPath, "User")
```

(If you prefer not to use Powershell for this, you can also open "Edit the system environment variables" from the Windows taskbar search menu and modify the value for `Path` under user variables.)

### Linux

For Linux, you will need to make sure you have *at least one* of the following binaries installed:
- **paplay**
- **aplay**
- **ffplay**

On most Linux distributions, it is highly likely that you already have **paplay** or **aplay** pre-installed on your system, but if that is not the case, you can install them via your package manager:

```bash
sudo apt update
sudo apt install pulseaudio-utils # For paplay
sudo apt install alsa-utils       # For aplay
```

## Running the Application

Assuming you have already met the prerequesites for your system above, running the player is as simple as invoking the **play** app with the path of your SPC file as the argument.

### Usage (Windows)

```powershell
.\play.exe "<path-to-your-spc-file.spc>"
```

### Usage (Linux)

```bash
./play "<path-to-your-spc-file.spc>"
```

You may also notice that the provided release folder contains a total of two programs: **play** and **apollo-spc-program**. It's worth noting that **apollo-spc-program** is the actual SPC player—**play** is just the launcher for it.

If you are interested in knowing how to run **apollo-spc-program** directly without requiring the **play** launcher, check out the [Advanced Usage](#advanced-usage) section.

## Compiling

See [COMPILING-Windows.md](COMPILING-Windows.md) or [COMPILING-Linux.md](COMPILING-Linux.md)

## Advanced Usage

TODO

## Licensing

**Apollo** is licensed under the **Mozilla Public License**, version 2.0.

This license applies to all source files included in this repo, with the exception of the ones under the [Jimbl](src/cli/play/Jimbl) directory, which are licensed under MIT.