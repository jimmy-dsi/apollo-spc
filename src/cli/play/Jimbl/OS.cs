namespace Jimbl;

using System.Runtime.InteropServices;

public static class OS {
	public enum Platform {
		Windows,
		Linux
	}
	
	public const Platform Windows = Platform.Windows;
	public const Platform Linux   = Platform.Linux;
	
	static Platform curPlatform;
	
	public static Platform Get() {
		return curPlatform;
	}
	
	static OS() {
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
			curPlatform = Platform.Windows;
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
			curPlatform = Platform.Linux;
		}
		else {
			throw new Exception("OS not supported");
		}
	}
}