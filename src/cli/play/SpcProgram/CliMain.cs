namespace SpcProgram;

using Apollo;
using Jimbl;

public static partial class CliMain {
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
			//Termios.EnableRawMode();
			
			var keyListener = new Thread(KeyListener.Run);
			keyListener.Start();
			
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
	
	static Menu   realMenu    = Menu.Metadata;
	static Menu   currentMenu = Menu.Metadata;
	static string menuBarMsg  = "Press CTRL+H for help menu";
	
	static void handleUI(EmuDataBuffer? buffer) {
		var isHelp = false;
		
		var key = KeyListener.GetKeyInfo();
		
		if (key is not null) {
			var kv = key.Value;
			
			if (kv.HasCtrl() && kv.IsChar('H')) {
				if (currentMenu == Menu.Help) {
					currentMenu = realMenu;
					Display.Clear();
				}
				else {
					currentMenu = Menu.Help;
					Display.Clear();
				}
			}
			else if (kv.IsEscape()) {
				currentMenu = realMenu;
				Display.Clear();
			}
		}
		
		switch (currentMenu) {
			case Menu.Metadata: {
				showMetadata();
				break;
			}
			
			case Menu.Help: {
				showHelpMenu();
				break;
			}
			
			default: {
				break;
			}
		}
		
		// Display Seek Bar
		if (buffer is not null) {
			if (isHelp) {
				Display.ClearBox(Display.Width - 16, 1, 16, Display.Height - 16);
				Display.Write($"Last help press: {buffer.DSPCycle}", 0, Display.Height - 5);
			}
			
			Display.ClearLine(Display.Height - 2);
			Display.Write(formatTime((int) (buffer.DSPCycle / 32), TimeUnit.Timer2s), 0, Display.Height - 3, Color.Cyan);
			
			var fullTimeInCycles = (long) (emu.SpcMetadata.LengthInSeconds ?? 600) * 2048000;
			var barLength = Display.Width - 1 - 14;
			
			var cursorPos = (int) ((double) buffer.DSPCycle / fullTimeInCycles * barLength);
			Display.Write(new string('=', cursorPos) + '|', 14, Display.Height - 3, Color.Cyan);
		}
		
		Display.Write("[", 13,                Display.Height - 3, Color.Cyan);
		Display.Write("]", Display.Width - 1, Display.Height - 3, Color.Cyan);
		
		// Display Menu Bar
		Display.ClearLine(Display.Height - 1, Color.BGBlue);
		Display.Write(menuBarMsg, 0, Display.Height - 1, Color.BGBlue);
		
		if (buffer is not null) {
			var cycleCounter = $"DSP Cycle: {buffer.DSPCycle}";
			Display.Write(cycleCounter, Display.Width - 1 - cycleCounter.Length, Display.Height - 1, Color.BGBlue);
		}
		
		Console.Write(Display.Flush());
	}
	
	enum TimeUnit {
		Seconds, MS, Timer2s
	}
	
	static string formatTime(int input, TimeUnit unit) {
		TimeSpan length = new();
		
		switch (unit) {
			case TimeUnit.Seconds: {
				length = new(hours: 0, minutes: 0, seconds: input);
				break;
			}
				
			case TimeUnit.MS: {
				length = new(days: 0, hours: 0, minutes: 0, seconds: 0, milliseconds: input);
				break;
			}
				
			case TimeUnit.Timer2s: {
				const long NS_InTimer2 = 15625;
				var ns = input * NS_InTimer2;
				length = new(ticks: ns / 100);
				break;
			}
		}
		
		return $"{(int) length.TotalHours:D2}:{length.Minutes:D2}:{length.Seconds:D2}.{length.Milliseconds:D3}";
	}
	
	static (string, Color?) drawTextField(string? text, Color? col = null) {
		if ((text ?? "") == "") {
			return ("<none>", new(ansiCode: 32));
		}
		else {
			return (text!, col);
		}
	}
}