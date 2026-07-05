namespace SpcProgram;

using Apollo;
using Jimbl;
using Jimbl.Graphics;

public static partial class CliMain {
	public static Emulator PrimaryEmu  { get; private set; }
	public static Emulator RunAheadEmu { get; private set; }
	public static bool     TerminateRequest { get; set; } = false;
	
	public static object EmuRestoreLock = new();
	
	static string spcFilePath;
	
	static Dictionary<int, Emulator> seekBarSnapshots = new();
	static object seekBarLock = new();
	
	static Thread? runAheadThread = null;
	
	static bool   killAllThreads     = false;
	static object killAllThreadsLock = new();
	
	static bool   startInDebugMode   = false;
	static bool   hideFirstBreakAddr = false;
	static object debugModeLock      = new();

	public static bool StartInDebugMode {
		get {
			lock (debugModeLock) {
				var res = startInDebugMode;
				startInDebugMode = false;
				return res;
			}
		}
		set {
			lock (debugModeLock) {
				startInDebugMode   = value;
				hideFirstBreakAddr = value;
			}
		}
	}

	public static bool HideFirstBreakAddr {
		get {
			lock (debugModeLock) {
				var res = hideFirstBreakAddr;
				hideFirstBreakAddr = false;
				return res;
			}
		}
	}

	public static bool KillAllThreads {
		get {
			lock (killAllThreadsLock) {
				return killAllThreads;
			}
		}
		set {
			lock (killAllThreadsLock) {
				killAllThreads = value;
			}
		}
	}

	public static int Start(string[] args) {
		try {
			ConsoleWin32.EnableCmdAnsiCodes();
		}
		catch (Exception) {
			// Allow program to continue even if Win32 call fails
		}
		
		Lib.Init();
		try {
			var debugMode = args.Any(x => x is "--debug" or "-d");
			args = args.Where(x => x is not "--debug" and not "-d").ToArray();
			
			if (args.Length == 0) {
				Console.Error.WriteLine($"error: SPC file not provided");
				return 1;
			}
			
			StartInDebugMode = debugMode;
			spcFilePath      = args[0];
			
			//emu = LibTest.Test(spcFilePath);
			PrimaryEmu = new(setAsMain: true, makeShared: true);
			PrimaryEmu.LoadSpcFile(spcFilePath);
			PrimaryEmu.SMP.LoggingEnabled = true;
			
			// Enable Script700 if binary or source file is present
			var scriptBinary = Script700.BinaryFile(spcFilePath);
			var scriptSource = Script700.ScriptFile(spcFilePath);
			
			if (scriptSource is not null &&
			   (scriptBinary is null || Env.DateModified(scriptSource) >= Env.DateModified(scriptBinary)))
			{
				var binaryData = Script700.Compile(File.ReadAllText(scriptSource));
				
				scriptBinary = Env.StripExtension(scriptSource) + ".7sb";
				File.WriteAllBytes(scriptBinary, binaryData);
				
				PrimaryEmu.Script700.LoadBinaryFile(binaryData);
			}
			else if (scriptBinary is not null) {
				PrimaryEmu.Script700.LoadBinaryFile(File.ReadAllBytes(scriptBinary));
			}
			
			Emulator.BurstAction = Analysis.TrackSampleUsage;
			
			RunAheadEmu = PrimaryEmu.SaveState();
			seekBarSnapshot(0, RunAheadEmu);
			
			// Register Key Bindings
			KeyBindings.Register(KeyBindings.Key.Escape,     KeyBindings.Action.ExitCurrentMenu);
			KeyBindings.Register(KeyBindings.Key.Char('L'),  KeyBindings.Action.ToggleHelpMenu, ctrl: true);
			KeyBindings.Register(KeyBindings.Key.ArrowRight, KeyBindings.Action.NavNextView);
			KeyBindings.Register(KeyBindings.Key.ArrowLeft,  KeyBindings.Action.NavPrevView);
			KeyBindings.Register(KeyBindings.Key.Char('0'),  KeyBindings.Action.EnableAllChannels);
			KeyBindings.Register(KeyBindings.Key.Char('1'),  KeyBindings.Action.ToggleChannel_1);
			KeyBindings.Register(KeyBindings.Key.Char('2'),  KeyBindings.Action.ToggleChannel_2);
			KeyBindings.Register(KeyBindings.Key.Char('3'),  KeyBindings.Action.ToggleChannel_3);
			KeyBindings.Register(KeyBindings.Key.Char('4'),  KeyBindings.Action.ToggleChannel_4);
			KeyBindings.Register(KeyBindings.Key.Char('5'),  KeyBindings.Action.ToggleChannel_5);
			KeyBindings.Register(KeyBindings.Key.Char('6'),  KeyBindings.Action.ToggleChannel_6);
			KeyBindings.Register(KeyBindings.Key.Char('7'),  KeyBindings.Action.ToggleChannel_7);
			KeyBindings.Register(KeyBindings.Key.Char('8'),  KeyBindings.Action.ToggleChannel_8);
			KeyBindings.Register(KeyBindings.Key.Char('!'),  KeyBindings.Action.ToggleMainChannel_1);
			KeyBindings.Register(KeyBindings.Key.Char('@'),  KeyBindings.Action.ToggleMainChannel_2);
			KeyBindings.Register(KeyBindings.Key.Char('#'),  KeyBindings.Action.ToggleMainChannel_3);
			KeyBindings.Register(KeyBindings.Key.Char('$'),  KeyBindings.Action.ToggleMainChannel_4);
			KeyBindings.Register(KeyBindings.Key.Char('%'),  KeyBindings.Action.ToggleMainChannel_5);
			KeyBindings.Register(KeyBindings.Key.Char('^'),  KeyBindings.Action.ToggleMainChannel_6);
			KeyBindings.Register(KeyBindings.Key.Char('&'),  KeyBindings.Action.ToggleMainChannel_7);
			KeyBindings.Register(KeyBindings.Key.Char('*'),  KeyBindings.Action.ToggleMainChannel_8);
			KeyBindings.Register(KeyBindings.Key.F1,         KeyBindings.Action.ToggleEchoChannel_1, ctrl: true);
			KeyBindings.Register(KeyBindings.Key.F2,         KeyBindings.Action.ToggleEchoChannel_2, ctrl: true);
			KeyBindings.Register(KeyBindings.Key.F3,         KeyBindings.Action.ToggleEchoChannel_3, ctrl: true);
			KeyBindings.Register(KeyBindings.Key.F4,         KeyBindings.Action.ToggleEchoChannel_4, ctrl: true);
			KeyBindings.Register(KeyBindings.Key.F5,         KeyBindings.Action.ToggleEchoChannel_5, ctrl: true);
			KeyBindings.Register(KeyBindings.Key.F6,         KeyBindings.Action.ToggleEchoChannel_6, ctrl: true);
			KeyBindings.Register(KeyBindings.Key.F7,         KeyBindings.Action.ToggleEchoChannel_7, ctrl: true);
			KeyBindings.Register(KeyBindings.Key.F8,         KeyBindings.Action.ToggleEchoChannel_8, ctrl: true);
			KeyBindings.Register(KeyBindings.Key.F1,         KeyBindings.Action.ToggleEchoChannel_1);
			KeyBindings.Register(KeyBindings.Key.F2,         KeyBindings.Action.ToggleEchoChannel_2);
			KeyBindings.Register(KeyBindings.Key.F3,         KeyBindings.Action.ToggleEchoChannel_3);
			KeyBindings.Register(KeyBindings.Key.F4,         KeyBindings.Action.ToggleEchoChannel_4);
			KeyBindings.Register(KeyBindings.Key.Char('P'),  KeyBindings.Action.ToggleLPF, ctrl: true);
			KeyBindings.Register(KeyBindings.Key.ArrowUp,    KeyBindings.Action.ScrollRowUp);
			KeyBindings.Register(KeyBindings.Key.ArrowDown,  KeyBindings.Action.ScrollRowDown);
			KeyBindings.Register(KeyBindings.Key.PageUp,     KeyBindings.Action.ScrollPageUp);
			KeyBindings.Register(KeyBindings.Key.PageDown,   KeyBindings.Action.ScrollPageDown);
			KeyBindings.Register(KeyBindings.Key.Home,       KeyBindings.Action.ScrollStart);
			KeyBindings.Register(KeyBindings.Key.End,        KeyBindings.Action.ScrollEnd);
			KeyBindings.Register(KeyBindings.Key.Char('E'),  KeyBindings.Key.Char('T'),          KeyBindings.Action.ToggleHeatMap);
			KeyBindings.Register(KeyBindings.Key.Char('D'),  KeyBindings.Action.ToggleCycleUnit, ctrl: true);
			KeyBindings.Register(KeyBindings.Key.ArrowRight, KeyBindings.Action.SeekFwd,         ctrl: true);
			KeyBindings.Register(KeyBindings.Key.ArrowLeft,  KeyBindings.Action.SeekBack,        ctrl: true);
			KeyBindings.Register(KeyBindings.Key.Char('>'),  KeyBindings.Action.SeekFwdFar);
			KeyBindings.Register(KeyBindings.Key.Char('<'),  KeyBindings.Action.SeekBackFar);
			KeyBindings.Register(KeyBindings.Key.Char('A'),  KeyBindings.Key.Char(')'), KeyBindings.Action.SeekPos_0, ctrl: false);
			KeyBindings.Register(KeyBindings.Key.Char('A'),  KeyBindings.Key.Char('!'), KeyBindings.Action.SeekPos_1, ctrl: false);
			KeyBindings.Register(KeyBindings.Key.Char('A'),  KeyBindings.Key.Char('@'), KeyBindings.Action.SeekPos_2, ctrl: false);
			KeyBindings.Register(KeyBindings.Key.Char('A'),  KeyBindings.Key.Char('#'), KeyBindings.Action.SeekPos_3, ctrl: false);
			KeyBindings.Register(KeyBindings.Key.Char('A'),  KeyBindings.Key.Char('$'), KeyBindings.Action.SeekPos_4, ctrl: false);
			KeyBindings.Register(KeyBindings.Key.Char('A'),  KeyBindings.Key.Char('%'), KeyBindings.Action.SeekPos_5, ctrl: false);
			KeyBindings.Register(KeyBindings.Key.Char('A'),  KeyBindings.Key.Char('^'), KeyBindings.Action.SeekPos_6, ctrl: false);
			KeyBindings.Register(KeyBindings.Key.Char('A'),  KeyBindings.Key.Char('&'), KeyBindings.Action.SeekPos_7, ctrl: false);
			KeyBindings.Register(KeyBindings.Key.Char('A'),  KeyBindings.Key.Char('*'), KeyBindings.Action.SeekPos_8, ctrl: false);
			KeyBindings.Register(KeyBindings.Key.Char('A'),  KeyBindings.Key.Char('('), KeyBindings.Action.SeekPos_9, ctrl: false);
			KeyBindings.Register(KeyBindings.Key.Char(' '),  KeyBindings.Action.TogglePause);
			KeyBindings.Register(KeyBindings.Key.F5,         KeyBindings.Action.ToggleBreak);
			KeyBindings.Register(KeyBindings.Key.F6,         KeyBindings.Action.StepInstruction);
			KeyBindings.Register(KeyBindings.Key.Char('B'),  KeyBindings.Action.ToggleBreakpoints,  ctrl: true);
			KeyBindings.Register(KeyBindings.Key.ArrowUp,    KeyBindings.Action.IncHeatMapDataSize, ctrl: true);
			KeyBindings.Register(KeyBindings.Key.ArrowDown,  KeyBindings.Action.DecHeatMapDataSize, ctrl: true);
			KeyBindings.Register(KeyBindings.Key.F9,         KeyBindings.Action.IncHeatMapDataSize);
			KeyBindings.Register(KeyBindings.Key.F10,        KeyBindings.Action.DecHeatMapDataSize);
			
			// Create RAM memory view buffer and Trace logger view buffer
			Display.CurrentBufferId = "aram";
			Display.ResetWindowBuffer(110, 0x1000, 0, 0, 110, ScrollAreaRows);
			Display.CurrentBufferId = "trace";
			Display.ResetWindowBuffer(Display.Width, ScrollAreaRows, 0, 0, Display.Width, ScrollAreaRows);
			Display.SetWindowProps(0, 0, 91, ScrollAreaRows);
			
			Display.HideWindow();
			
			Console.Clear();
			Console.CursorVisible = false;
			
			runAheadThread = new(RunAheadLoop);
			runAheadThread.Start();
			
			Thread keyListener = new(KeyListener.Run);
			keyListener.Start();
			
			Driver.Setup(handleUI);
		}
		catch (SpcMissingHeaderError) {
			Console.Error.WriteLine($"error: An unknown error occurred while attempting to process SPC metadata");
			return 1;
		}
		catch (IOException) {
			Console.Error.WriteLine($"error: The SPC file '{spcFilePath}' was not found or could not be loaded");
			return 1;
		}
		catch (EndAppException) {
			// Catch and return 0
			return 0;
		}
		finally {
			KillAllThreads = true; // Send signal to run-ahead thread to terminate
			runAheadThread?.Join();
			Lib.Deinit();
			Console.CursorVisible = true;
		}
		
		return 0;
	}
	
	public static void MainLoop(Action<EmuDataBuffer?> uiCallback) {
		var lastCycle = 0L;
		
		while (true) {
			if (TerminateRequest) {
				return;
			}
			
			var signalReceived = false;
			while (!signalReceived) {
				signalReceived = Transfer.Signal.WaitOne(500); // Timeout of 500ms
				if (TerminateRequest) {
					return;
				}
			}
				
			EmuDataBuffer? buffer = null;
				
			// Retrieve whichever data processed by emu in audio callback is available - Signal when done
			Transfer.Comm.UseBufferUI(container => {
				if (container.Buffer is null) return;
				buffer = container.Buffer.Clone();
			});
				
			Transfer.Signal.Reset();
			
			if (buffer is null /*|| buffer.DSPCycle < lastCycle*/) {
				continue;
			}
			
			lastCycle = buffer!.DSPCycle;
			
			// Do UI display
			uiCallback(buffer);
		}
	}

	public static long RunAheadCycle {
		get {
			lock (runAheadLock) {
				return runAheadCycle;
			}
		}
		private set {
			lock (runAheadLock) {
				runAheadCycle = value;
			}
		}
	}

	static long   runAheadCycle = 0;
	static object runAheadLock = new();
	
	const int SnapsPerSecond = 1;
	
	public static void RunAheadLoop() {
		while (true) {
			if (KillAllThreads) {
				return;
			}
			
			Emulator runAheadEmu;
			lock (seekBarLock) {
				runAheadEmu = RunAheadEmu;
			}
			var cycles = 2048000 / 8;
			
			if (runAheadEmu.Script700.IsRunning) {
				runAheadEmu.StepNCycles(cycles);
			}
			else {
				runAheadEmu.StepNCyclesFast(cycles);
			}
			
			runAheadEmu.BurstProcess(Emulator.BurstAction);
			
			long playCycle;
			lock (barCycleLock) {
				playCycle = barCycle;
			}
			
			var cycle = runAheadEmu.DSP.CurrentCycle;
			RunAheadCycle = cycle;
			
			var throttleFactor = 0;
			
			// Sleep for a bit if run ahead is far ahead of current play position
			if (cycle >= playCycle + 120 * 2048000) {
				throttleFactor = 8;
			}
			else if (cycle >= playCycle + 60 * 2048000) {
				throttleFactor = 5;
			}
			else if (cycle >= playCycle + 40 * 2048000) {
				throttleFactor = 3;
			}
			else if (cycle >= playCycle + 20 * 2048000) {
				throttleFactor = 2;
			}
			
			if (throttleFactor > 0) {
				Thread.Sleep(8 * throttleFactor);
			}
			
			if (runAheadEmu.DSP.CurrentCycle <= (long) 2048000 * 60 * 12) { // Hard cutoff of run-ahead snapshots after 12 minutes
				seekBarSnapshot(cycle, runAheadEmu);
			}
		}
	}
	
	public static int GetSnapshotIndex(long cycle) {
		return (int) (cycle / (2048000 / SnapsPerSecond));
	}
	
	public static Emulator GetSnapshot(long cycle) {
		var snapshotIndex = GetSnapshotIndex(cycle);
		lock (seekBarLock) {
			return seekBarSnapshots[snapshotIndex];
		}
	}
	
	static void seekBarSnapshot(long cycle, Emulator runAheadEmu) {
		var snapshotIndex = GetSnapshotIndex(cycle);
		
		lock (seekBarLock) {
			if (ReferenceEquals(runAheadEmu, RunAheadEmu) && !seekBarSnapshots.ContainsKey(snapshotIndex)) {
				seekBarSnapshots[snapshotIndex] = runAheadEmu.SaveState();
			}
		}
	}
	
	enum TimeUnit {
		Seconds, MS, Timer2s
	}
	
	static string formatTime(long input, TimeUnit unit) {
		TimeSpan length = new();
		
		switch (unit) {
			case TimeUnit.Seconds: {
				length = new(hours: 0, minutes: 0, seconds: (int) input);
				break;
			}
				
			case TimeUnit.MS: {
				length = new(days: 0, hours: 0, minutes: 0, seconds: 0, milliseconds: (int) input);
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
	
	static (string, AnsiColor?) drawTextField(string? text, AnsiColor? col = null) {
		if ((text ?? "") == "") {
			return ("<none>", AnsiColor.Green);
		}
		else {
			return (text!, col);
		}
	}
}