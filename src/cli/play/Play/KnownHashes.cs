using System.Diagnostics;

namespace Play;

using Jimbl;

public static class KnownHashes {
	static string[] windows = [
	
	];
	
	static string[] linux = [
	
	];
	
	public static string[] Set {
		get {
			switch (OS.Get()) {
				case OS.Windows: {
					return windows.ToArray();
				}
				case OS.Linux: {
					return linux.ToArray();
				}
				default: {
					throw new UnreachableException();
				}
			}
		}
	}
}