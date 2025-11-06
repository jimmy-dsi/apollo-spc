namespace Play;

using System.Diagnostics;
using Jimbl;

public static class KnownHashes {
	static string[] windows = [
		"e8f1e9f0eb11b571336d6a16d7585e91c90bced734d47122810ac173589d20ff", // v0.1.0 (x86-64)
		"35e9d392f7b5b859c5821f5a15d9c48da2e2fe8c8af323a70d7ae73cc25bc729", // v0.1.1 (x86-64)
		"8c08bde90ecccb5daecd82019a1cac164f574bac0eb2621f81d917e8e0a13f30", // v0.1.2 (x86-64)
		"3935805820990b7103a37bf27be5f7d58dbc18a0baff7491c57713d5b6b22e4a", // v0.2.0 (x86-64)
	];
	
	static string[] linux = [
		"5733859762265cfb29f9314f7df747027f12e6913f7dd44800f64ab3c3ce39fe", // v0.1.0 (x86-64)
		"8c2e566e58692bebbc62d99fbdd3c995ff65d0079fe38772a740317470f54704", // v0.1.1 (x86-64)
		"11c81e5e13120dcde833dc674542704c4537c08eda369ff9da23882b19ce2f34", // v0.1.2 (x86-64)
		"c71a274dc355167a8b70ea613e1c957161b8ff452716fee2f3eadf4c749aeb9a", // v0.2.0 (x86-64)
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