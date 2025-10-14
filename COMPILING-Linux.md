# Compiling
This section details the build process on Linux for both the **apollo-spc-program** and **play** apps from this repo.

## Required
- Zig 0.14.1 — **Later versions will not work!**

## Optional
The items in the list below are only needed if you wish to compile the **play** launcher utility. If you only want **apollo-spc-program**, these can be skipped:
- Dotnet SDK 8.0+
- Clang compiler

## Zig Installation
Download Zig 0.14.1 (Linux x86_64) from https://ziglang.org/download/0.14.1/zig-x86_64-linux-0.14.1.tar.xz

Extract the archive to your desired location, then add the directory path containing the `zig` binary to your `PATH` environment variable (Replace `/path/to/folder/with/zig/binary`) below:

```bash
echo 'export PATH="/path/to/folder/with/zig/binary:$PATH"' >> ~/.bashrc
source ~/.bashrc
```

Check to make sure Zig is installed correctly:

```bash
zig version
```

If you see `0.14.1` as your output, you are good to continue on to the next steps. If you see any other version number, or "command not found", then something went wrong during download or path setup.

## Dotnet Installation
Note: The below steps are technically optional. If you wish to see how to build and run the app without dotnet, see the [Building the CLI App Only](#building-the-cli-app-only) section below.

Firstly, before doing anything, you'll want to create a new persistent environment variable called `DOTNET_CLI_TELEMETRY_OPTOUT` with a value of `1`. This will prevent data collection by Microsoft while using the dotnet toolchain (https://learn.microsoft.com/en-us/dotnet/core/tools/telemetry#how-to-opt-out).

```bash
echo 'export DOTNET_CLI_TELEMETRY_OPTOUT=1' >> ~/.bashrc
source ~/.bashrc
```

Now you're ready to install the dotnet SDK for Linux. You can do so be running the following commands:
```bash
curl -L https://dot.net/v1/dotnet-install.sh -o dotnet-install.sh
chmod +x ./dotnet-install.sh
./dotnet-install.sh --version latest
```

The **play** app utilizes dotnet's Ahead-Of-Time (AOT) compilation features. In order to build projects which use these features, you will need to install several dependencies—namely the clang compiler, among a few others. Installation command for Ubuntu or Debian based distros:

```bash
sudo apt update
sudo apt install -y clang lld libc6-dev libicu-dev libssl-dev zlib1g-dev
```

Once these are complete, you are ready to move on to the next build steps.

## Building
To build the project in full, simply navigate to the root folder of your cloned git repo and type:

```bash
./build.sh
```

This will build the entire project as whole. This includes the Zig CLI app **apollo-spc-program**, as well as the dotnet launcher app **play**.

If the build is successful—and assuming you have already met the prerequisites for running the app itself—you can now run the freshly built app from the `bin/` folder by entering:
```bash
bin/play "<path-to-your-spc-file.spc>"
```

## Building the CLI App Only
If you do not wish to install the dotnet SDK onto your machine, you can choose to build **apollo-spc-program** only (the Zig CLI app). This is the actual program which interprets your SPC file.

You can build the CLI app using the following command:

```bash
src/zig-build.sh
```

Please be aware that without the **play** app, there will be a few differences regarding the launch command (more details in the section on running in README.md), and you may experience some visual glitching upon starting and exiting the app. 

Apart from these minor differences, the app should function pretty much the same.