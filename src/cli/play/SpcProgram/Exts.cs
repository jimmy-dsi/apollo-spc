namespace SpcProgram;

using System.Diagnostics;

public static class BusSizeExts {
	public static string Name(this CliMain.BusSize busSize) {
		return busSize switch {
			CliMain.BusSize.Bit8  => "8-bit",
			CliMain.BusSize.Bit16 => "16-bit",
			CliMain.BusSize.Bit24 => "24-bit",
			CliMain.BusSize.Bit32 => "32-bit",
			CliMain.BusSize.Bit64 => "64-bit",
			_ => throw new UnreachableException()
		};
	}
	
	public static CliMain.BusSize Next(this CliMain.BusSize busSize) {
		return busSize switch {
			CliMain.BusSize.Bit8  => CliMain.BusSize.Bit16,
			CliMain.BusSize.Bit16 => CliMain.BusSize.Bit32,
			CliMain.BusSize.Bit32 => CliMain.BusSize.Bit64,
			CliMain.BusSize.Bit64 => CliMain.BusSize.Bit8,
			_ => throw new ArgumentException()
		};
	}
	
	public static CliMain.BusSize Prev(this CliMain.BusSize busSize) {
		return busSize switch {
			CliMain.BusSize.Bit8  => CliMain.BusSize.Bit64,
			CliMain.BusSize.Bit16 => CliMain.BusSize.Bit8,
			CliMain.BusSize.Bit32 => CliMain.BusSize.Bit16,
			CliMain.BusSize.Bit64 => CliMain.BusSize.Bit32,
			_ => throw new ArgumentException()
		};
	}
}