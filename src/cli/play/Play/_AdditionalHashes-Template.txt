namespace Play;

public static class AdditionalHashes {
	static string[] set = [
		// Placeholder value to be replaced by build script with generated hash
		// A little ridiculous looking, yes... but this way there is absolutely no mistaking it
		// And it is probably more secure or something... Paranoid programming yay!
		"[[[C#___play___apollo-spc-program___C735A0F9___!GenFromCode!]]]"
	];
	
	public static string[] Set => set.ToArray();
}