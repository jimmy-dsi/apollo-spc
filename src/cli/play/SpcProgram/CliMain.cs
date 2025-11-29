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
			
			// Register Key Bindings
			KeyBindings.Register(KeyBindings.Key.Escape,     KeyBindings.Action.ExitCurrentMenu);
			KeyBindings.Register(KeyBindings.Key.Char('H'),  KeyBindings.Action.ToggleHelpMenu, ctrl: true);
			KeyBindings.Register(KeyBindings.Key.ArrowRight, KeyBindings.Action.NavNextView);
			KeyBindings.Register(KeyBindings.Key.ArrowLeft,  KeyBindings.Action.NavPrevView);
			
			Console.Clear();
			
			Thread keyListener = new(KeyListener.Run);
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
		var action = KeyBindings.GetAction();
		
		if (action is not null) {
			doAction(action!.Value);
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
	
	static void doAction(KeyBindings.Action action) {
		switch (action) {
			case KeyBindings.Action.ExitCurrentMenu: {
				changeCurrentMenu(realMenu, setAsRealMenu: false);
				break;
			}
			
			case KeyBindings.Action.ToggleHelpMenu: {
				if (currentMenu == Menu.Help) {
					changeCurrentMenu(realMenu, setAsRealMenu: false);
				}
				else {
					realMenu = currentMenu;
					changeCurrentMenu(Menu.Help, setAsRealMenu: false);
				}
				
				break;
			}
		}
	}
	
	static void changeCurrentMenu(Menu newMenu, bool setAsRealMenu = true) {
		currentMenu = newMenu;
		
		if (setAsRealMenu) {
			realMenu = newMenu;
		}
		
		Display.Clear();
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