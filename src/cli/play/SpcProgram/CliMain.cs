namespace SpcProgram;

using Apollo;
using Jimbl;

public static partial class CliMain {
	public static Emulator PrimaryEmu { get; private set; }
	
	static string spcFilePath;
	
	public static int Start(string[] args) {
		Lib.Init();
		try {
			if (args.Length == 0) {
				Console.Error.WriteLine($"error: SPC file not provided");
				return 1;
			}
			
			spcFilePath = args[0];
			
			//emu = LibTest.Test(spcFilePath);
			PrimaryEmu = new(setAsMain: true, makeShared: true);
			PrimaryEmu.LoadSpcFile(spcFilePath);
			PrimaryEmu.SMP.LoggingEnabled = true;
			
			// Register Key Bindings
			KeyBindings.Register(KeyBindings.Key.Escape,     KeyBindings.Action.ExitCurrentMenu);
			KeyBindings.Register(KeyBindings.Key.Char('H'),  KeyBindings.Action.ToggleHelpMenu, ctrl: true);
			KeyBindings.Register(KeyBindings.Key.ArrowRight, KeyBindings.Action.NavNextView);
			KeyBindings.Register(KeyBindings.Key.ArrowLeft,  KeyBindings.Action.NavPrevView);
			KeyBindings.Register(KeyBindings.Key.ArrowUp,    KeyBindings.Action.ScrollRowUp);
			KeyBindings.Register(KeyBindings.Key.ArrowDown,  KeyBindings.Action.ScrollRowDown);
			KeyBindings.Register(KeyBindings.Key.PageUp,     KeyBindings.Action.ScrollPageUp);
			KeyBindings.Register(KeyBindings.Key.PageDown,   KeyBindings.Action.ScrollPageDown);
			KeyBindings.Register(KeyBindings.Key.Home,       KeyBindings.Action.ScrollStart);
			KeyBindings.Register(KeyBindings.Key.End,        KeyBindings.Action.ScrollEnd);
			
			Console.Clear();
			
			Thread keyListener = new(KeyListener.Run);
			keyListener.Start();
			
			AudioOutput.Setup(handleUI);
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
	
	public static void MainLoop(Action<EmuDataBuffer?> uiCallback) {
		var lastCycle = 0L;
		
		while (true) {
			while (!Transfer.Signal) {
				Thread.Sleep(1); // 1 millisecond sleep to reduce CPU load
			}
				
			EmuDataBuffer? buffer = null;
				
			// Retrieve whichever data processed by emu in audio callback is available - Signal when done
			Transfer.Comm.UseBufferUI(container => {
				if (container.Buffer is null) return;
				buffer = container.Buffer.Clone();
			});
				
			Transfer.Signal = false;
			
			if (buffer is null || buffer.DSPCycle < lastCycle) {
				continue;
			}
			
			lastCycle = buffer!.DSPCycle;
			
			// Do UI display
			uiCallback(buffer);
		}
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