# Apollo

**Apollo** is an S-SMP/S-DSP emulator core written in Zig for Windows and Linux. Its primary purpose is to be used for playing SPC files, but can also be used for general-purpose SNES APU emulation.

## Releases

The latest stable version is available from the [releases on GitHub](https://github.com/jimmy-dsi/apollo-spc/releases).

## Running the Application

Running the player is as simple as invoking the **apollo-spc-program** app with the path of your SPC file as the argument.

### Usage (Windows)

```powershell
.\apollo-spc-program.exe "<path-to-your-spc-file.spc>"
```

### Usage (Linux)

```bash
./apollo-spc-program "<path-to-your-spc-file.spc>"
```

## Compiling

See [COMPILING-Windows.md](COMPILING-Windows.md) or [COMPILING-Linux.md](COMPILING-Linux.md)

## Other Useful Information
- [Apollo extended Script700 commands](./doc/apollo-specific-script700-commands.md)
- [Loading Script700 and the 7sb file format](./doc/the-7sb-format.md)
- [Script700 commands and bytecode format](./doc/script700-bytecode.txt)

## Licensing

**Apollo** is licensed under the **Mozilla Public License**, version 2.0.

This license applies to all source files included in this repo, with the exception of the ones under the [Jimbl](src/cli/play/Jimbl) directory, which are licensed under MIT, as well as SDL2.cs — which is written by Ethan Lee and also licensed under MIT.