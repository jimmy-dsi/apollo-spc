namespace Play;

using System.Diagnostics;
using Jimbl;

public static class KnownHashes {
	static string[] windows = [
		"e8f1e9f0eb11b571336d6a16d7585e91c90bced734d47122810ac173589d20ff" // v0.1.0 (x86-64)
	];
	
	static string[] linux = [
		"5733859762265cfb29f9314f7df747027f12e6913f7dd44800f64ab3c3ce39fe" // v0.1.0 (x86-64)
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