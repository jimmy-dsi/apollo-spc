namespace SPC;

using Jimbl.DataStructs;

public static class Script700 {
	public static string Simplify(string str) { // TODO: Make these UString
		// TODO: Implement
		return str;
	}
	
	public static string? ScriptFile(string spcFilePath) {
		if (spcFilePath.ToLower().EndsWith(".spc")) {
			var s700 = spcFilePath[..^4] + ".700";
			if (File.Exists(s700)) {
				return s700;
			}
			
			if (File.Exists("65816.700")) {
				return "65816.700";
			}
		}
		
		return null;
	}
}