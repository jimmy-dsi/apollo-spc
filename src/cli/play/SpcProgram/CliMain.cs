namespace SpcProgram;

using Apollo;

public static class CliMain {
	static Emulator emu;
	static string spcFilePath;
	
	public static int Main(string[] args) {
		Lib.Init();
		try {
			if (args.Length == 0) {
				Console.Error.WriteLine($"error: SPC file not provided");
				return 1;
			}
			
			spcFilePath = args[0];
			
			emu = new(setAsMain: true);
			emu.LoadSpcFile(spcFilePath);
			
			AudioOutput.Setup(emu);
		}
		catch (SpcLoadError) {
			Console.Error.WriteLine($"error: An unknown error occurred while attempting to process SPC metadata");
			return 1;
		}
		catch (IOException) {
			Console.Error.WriteLine($"error: The SPC file '{spcFilePath}' was not found or could not be loaded");
			return 1;
		}
		finally {
			Lib.Deinit();
		}
		
		return 0;
	}
}