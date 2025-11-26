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
			
			//emu = LibTest.Test(spcFilePath);
			emu = new(setAsMain: true, makeShared: true);
			emu.LoadSpcFile(spcFilePath);
			emu.SMP.LoggingEnabled = true;
			
			Console.Clear();
			AudioOutput.Setup(emu, handleUI);
		}
		catch (SpcMissingHeaderError) {
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
	
	enum Menu {
		Metadata,
		Help,
		ASMViewer,
		MemoryViewer,
		DSPViewer1,
		DSPViewer2,
		DSPViewer3,
		Script700Viewer,
	}
	
	static Menu currentMenu = Menu.Metadata;
	
	static void handleUI() {
		switch (currentMenu) {
			case Menu.Metadata: {
				showMetadata();
				break;
			}
			
			default: {
				break;
			}
		}
	}
	
	static void showMetadata() {
		Display.DrawOutline(0, 0, Display.Width, Display.Height - 10, removeSides: true);
		
		Display.WriteBox(["Title:", "Artist:", "Game:", "Dumper:", "Comments:"], 1, 1);
		var maxFieldWidth = Display.Width - 18;
		
		var titleField    = Display.WordWrap(emu.SpcMetadata.Title,    maxFieldWidth, 1);
		var artistField   = Display.WordWrap(emu.SpcMetadata.Artist,   maxFieldWidth, 1);
		var gameField     = Display.WordWrap(emu.SpcMetadata.Game,     maxFieldWidth, 1);
		var dumperField   = Display.WordWrap(emu.SpcMetadata.Dumper,   maxFieldWidth, 1);
		var commentsField = Display.WordWrap(emu.SpcMetadata.Comments, maxFieldWidth, 3);
		
		Display.WriteBox(titleField,    17, 1);
		Display.WriteBox(artistField,   17, 2);
		Display.WriteBox(gameField,     17, 3);
		Display.WriteBox(dumperField,   17, 4);
		Display.WriteBox(commentsField, 17, 5);
		
		Console.Write(Display.Flush());
	}
}