namespace Jimbl;

using System.Runtime.InteropServices;

public static class OS {
	public enum Platform {
		Windows,
		Linux,
		OSX,
	}
	
	public const Platform Windows = Platform.Windows;
	public const Platform Linux   = Platform.Linux;
	public const Platform OSX     = Platform.OSX;
	
	public static Version Version => Environment.OSVersion.Version;
	
	static Platform curPlatform;
	
	public static Platform Get() {
		return curPlatform;
	}

	public static bool IsPosix() {
		return curPlatform != OS.Windows;
	}
	
	static OS() {
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
			curPlatform = Platform.Windows;
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
			curPlatform = Platform.Linux;
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
			curPlatform = Platform.OSX;
		}
		else {
			throw new Exception("OS not supported");
		}
	}
}
