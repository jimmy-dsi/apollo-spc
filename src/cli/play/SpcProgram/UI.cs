namespace SpcProgram;

using System.Diagnostics;
using System.Text;

using Apollo;
using Jimbl;
using Jimbl.Graphics;

public static partial class CliMain {
	const int ScrollAreaRows = 0x1E;
	
	class EndAppException: Exception { }
	
	public enum State {
		Init, Normal, Paused, Break, NonFatalError
	}
	
	enum View {
		Metadata,
		Help,
		ASMViewer,
		MemoryViewer,
		DSPViewer1,
		DSPViewer2,
		DSPViewer3,
		Script700Viewer,
	}
	
	enum StatusMSG {
		Default,
		HeatMapOff, HeatMapOn, BusSizeChanged,
		
		ChannelX_Enabled,  MainChannelX_Enabled,  EchoChannelX_Enabled,
		ChannelX_Disabled, MainChannelX_Disabled, EchoChannelX_Disabled,
		
		LPF_Enabled, LPF_Disabled,
		
		AllChannelsEnabled,  AllMainChannelsEnabled,  AllEchoChannelsEnabled,
		AllChannelsDisabled, AllMainChannelsDisabled, AllEchoChannelsDisabled,
		
		SeekFwd, SeekBack, SeekFwdFar, SeekBackFar, SeekPos,
		SteppedCycles, Paused, BreakExec, CycleDisplayChanged,
		BreakpointHit, BreakpointsOff, BreakpointsOn,
		
		Script700_Error, Continue
	}
	
	static State  uiState          = State.Init;
	static State  realUiState      = State.Init;
	static bool   disableScript700 = false;
	static object uiStateLock      = new();
	
	static bool ignoreStepDisplay = false;
	
	static View       realView       = View.Metadata;
	static View       currentView    = View.Metadata;
	static View       nextView       = View.Metadata;
	static string     menuBarMsg     = "Press CTRL+L for help menu";
	static string?    tempMenuBarMsg = null;
	static bool       menuBarError   = false;
	static Stopwatch? tempMsgTime    = null;
	
	static int     channelToToggle    = 0;
	static int     seekPosition       = 1;
	static long    stepCycles         = 0;
	static UInt16  execBreakpointAddr = 0;
	static BusSize heatMapDataSize    = BusSize.Bit32;
	
	static int viewIndex = 0;
	static View[] views = [View.Metadata, View.DSPViewer1, View.DSPViewer2, View.DSPViewer3, View.MemoryViewer, View.Script700Viewer, View.ASMViewer];
	static int asmViewerIndex = views.IndexOf(View.ASMViewer);
	
	static bool heatMapEnabled     = false;
	static bool cyclesInSpcClocks  = false;
	static bool breakpointsEnabled = true;
	
	static object breakpointToggleLock = new();
	
	static Transfer.Requests requests = Transfer.Requests.CycleCountOnly;
	
	static long   curCycle     = 0;
	static long   barCycle     = 0;
	static object barCycleLock = new();
	
	static bool instrStepInTransit = false;
	
	static long frame     = 0;
	static long prevFrame = 0;
			
	static (int X, int Y, int W, int H)[] menuRegions = [
		(22 + 2,                 0, 18, 2),
		(Display.Width /  2 - 8, 0, 17, 2),
		(Display.Width - 22 - 8, 1,  4, 1),
	];
			
	static string[][] menuOptions = [
		["Continue Script700",   "    execution"],
		["Disable Script700",   "  and continue"],
		["Quit"],
	];
	
	static int selectedItem = 0;
	
	public static State UI_State {
		get {
			lock (uiStateLock) {
				return uiState;
			}
		}
		set {
			lock (uiStateLock) {
				var s = uiState;
				uiState = value;
				
				if (uiState == State.NonFatalError && s != State.NonFatalError) {
					realUiState = s;
				}
			}
		}
	}
	
	public static State RealUI_State {
		get {
			lock (uiStateLock) {
				return realUiState;
			}
		}
	}
	
	public static bool UI_StateIsPaused {
		get {
			lock (uiStateLock) {
				return uiState is State.Paused or State.NonFatalError;
			}
		}
	}
	
	public static bool DisableScript700 {
		get {
			lock (uiStateLock) {
				return disableScript700;
			}
		}
		set {
			lock (uiStateLock) {
				disableScript700 = value;
			}
		}
	}

	public static bool InstrStepInTransit {
		get {
			lock (uiStateLock) {
				return instrStepInTransit;
			}
		}
		set {
			lock (uiStateLock) {
				instrStepInTransit = value;
			}
		}
	}

	public static bool BreakpointsEnabled {
		get {
			lock (breakpointToggleLock) {
				return breakpointsEnabled;
			}
		}
		set {
			lock (breakpointToggleLock) {
				breakpointsEnabled = value;
			}
		}
	}
	
	public static bool ToggleBreakpoints() {
		lock (breakpointToggleLock) {
			breakpointsEnabled = !breakpointsEnabled;
			return breakpointsEnabled;
		}
	}

	public static void FlagStepInTransit() {
		lock (uiStateLock) {
			var s = uiState;
			uiState = State.NonFatalError;
				
			if (s != State.NonFatalError) {
				realUiState = s;
				instrStepInTransit = true;
			}
		}
	}

	public static State RestoreUIState() {
		lock (uiStateLock) {
			if (uiState == State.NonFatalError && realUiState != State.NonFatalError) {
				uiState = realUiState;
			}
			return uiState;
		}
	}
	
	public static void ToggleBreak() {
		TogglePause();
	}
	
	public static void TogglePause() {
		lock (uiStateLock) {
			if (uiState == State.Normal) {
				uiState = State.Paused;
			}
			else if (uiState == State.Paused) {
				uiState = State.Normal;
			}
		}
	}
	
	static void handleUI(EmuDataBuffer? buffer) {
		PrevStartAddr = StartAddr;
		
		KeyBindings.Action? action = null;
		var state = UI_State;
		
		prevFrame = frame;
		frame     = Driver.Frame;
		
		if (state == State.NonFatalError) {
			// Handle error menu controls
			var keyInfo = KeyListener.GetKeyInfo();
			if (keyInfo is not null) {
				var ki = keyInfo.Value;
				
				if (ki.IsRightArrow()) {
					selectedItem = Math.Clamp(selectedItem + 1, 0, 2);
				}
				else if (ki.IsLeftArrow()) {
					selectedItem = Math.Clamp(selectedItem - 1, 0, 2);
				}
				else if (ki.IsTab()) {
					selectedItem++;
					selectedItem %= 3;
				}
				else if (ki.IsChar('C')) {
					selectedItem = 0;
				}
				else if (ki.IsChar('D')) {
					selectedItem = 1;
				}
				else if (ki.IsChar('Q')) {
					selectedItem = 2;
				}
				else if (ki.IsEnter() || ki.IsChar(' ')) {
					switch (selectedItem) {
						case 0: {
							// Remove all snapshots after current timestamp if Script700 is disabled->enabled
							if (DisableScript700) {
								lock (seekBarLock) {
									var indexes  = seekBarSnapshots.Select(x => x.Key).ToArray();
									var curIndex = GetSnapshotIndex(curCycle);
				
									foreach (var idx in indexes) {
										if (idx > 0 && idx >= curIndex) {
											seekBarSnapshots.Remove(idx);
										}
									}
								
									RunAheadEmu = PrimaryEmu.SaveState();
								}
							}
							
							DisableScript700 = false;
							state = RestoreUIState();
							
							setStatusMsg(StatusMSG.Default);
							setTempStatusMsg(StatusMSG.Continue);
							Display.Clear();
							
							break;
						}
						case 1: {
							PrimaryEmu.Script700.Disable();
							
							// Remove all snapshots after current timestamp if Script700 is enabled->disabled
							if (!DisableScript700) {
								lock (seekBarLock) {
									var indexes  = seekBarSnapshots.Select(x => x.Key).ToArray();
									var curIndex = GetSnapshotIndex(curCycle);
				
									foreach (var idx in indexes) {
										if (idx > 0 && idx >= curIndex) {
											seekBarSnapshots.Remove(idx);
										}
									}
								
									RunAheadEmu = PrimaryEmu.SaveState();
								}
							}
							
							DisableScript700 = true;
							state = RestoreUIState();
							
							setStatusMsg(StatusMSG.Default);
							setTempStatusMsg(StatusMSG.Continue);
							Display.Clear();
							
							break;
						}
						case 2: {
							throw new EndAppException();
						}
					}
				}
			}
		}
		
		if (state is State.Break) {
			if (buffer?.BreakPC is UInt16 pc) {
				execBreakpointAddr = pc;
				
				if (HideFirstBreakAddr) {
					ignoreStepDisplay = true;
					resetTraceLog();
				}
				else {
					setStatusMsg(StatusMSG.BreakpointHit);
				}
				
				forceTraceLoggerView();
		
				if (nextView != currentView) {
					if (buffer?.ExpectData(requests) ?? false) {
						commitCurrentView();
					}
				}
			
				UI_State = State.Paused;
				ignoreStepDisplay = true;
				lastInstructionCycle = -1;
			}
		}
		else if (state is not State.NonFatalError and not State.Init) {
			action = KeyBindings.GetAction();
		
			var framesSinceLastDisplay = Math.Max(1, frame - prevFrame);
			var stepInTransit = InstrStepInTransit;
		
			if (action is not null && !stepInTransit) {
				doAction(action!.Value);
			}
			else if (stepInTransit) {
				stepInstruction(log: true);
			}
			
			if (buffer is not null) {
				if (!ignoreStepDisplay) {
					if (state == State.Paused && lastInstructionCycle >= 0) {
						stepCycles = buffer.DSPCycle - lastInstructionCycle;
						if (stepCycles > 0) {
							setTempStatusMsg(StatusMSG.SteppedCycles);
						}
					}
			
					lastInstructionCycle = buffer.DSPCycle;
				}
				
				ignoreStepDisplay = false;
			}
		
			if (nextView != currentView) {
				if (buffer?.ExpectData(requests) ?? false) {
					commitCurrentView();
				}
			}
		}
		
		switch (currentView) {
			case View.Metadata: {
				showMetadata();
				break;
			}
			
			case View.Help: {
				showHelpMenu();
				break;
			}
			
			case View.ASMViewer: {
				if (Display.CurrentBufferId == "trace") {
					showTraceLogger(buffer!);
				}
				break;
			}
			
			case View.MemoryViewer: {
				if (Display.CurrentBufferId == "aram") {
					showMemoryViewer(buffer!);
				}
				
				break;
			}
			
			case View.DSPViewer1: {
				showDSPViewer1(buffer!);
				break;
			}
			
			case View.DSPViewer2: {
				showDSPViewer2(buffer!);
				break;
			}
			
			case View.DSPViewer3: {
				showDSPViewer3(buffer!);
				break;
			}
			
			case View.Script700Viewer: {
				showScript700Viewer(buffer!);
				break;
			}
			
			default: {
				break;
			}
		}
		
		// Display Seek Bar
		if (buffer is not null || state == State.Init) {
			curCycle = buffer?.DSPCycle ?? 0;
			lock (barCycleLock) {
				barCycle = curCycle;
			}
			
			Display.ClearLine(Display.Height - 3);
			Display.Write(formatTime(curCycle / 32, TimeUnit.Timer2s), 0, Display.Height - 3, AnsiColor.Cyan);
			
			var fullTimeInCycles = (long) (PrimaryEmu.SpcMetadata.LengthInSeconds ?? 60 * 12)  * 2048000;
			var barLength        = Display.Width - 1 - 14;
			
			var cursorPos  = (int) ((double)      curCycle / fullTimeInCycles * barLength);
			var cursorPos2 = (int) ((double) RunAheadCycle / fullTimeInCycles * barLength);
			
			cursorPos  = Math  .Min(cursorPos,                          Display.Width - 1);
			cursorPos2 = Math.Clamp(cursorPos2, Math.Max(1, cursorPos), Display.Width);
			
			Display.Write(new string('=', cursorPos) + '|',                         14,                 Display.Height - 3, AnsiColor    .Cyan);
			Display.Write(new string('=', Math.Max(0, cursorPos2 - cursorPos - 1)), 14 + cursorPos + 1, Display.Height - 3, AnsiColor.DarkGrey);
		}
		
		Display.Write("[", 13,                Display.Height - 3, AnsiColor.Cyan);
		Display.Write("]", Display.Width - 1, Display.Height - 3, AnsiColor.Cyan);
		
		// Display Menu Bar
		AnsiColor barColor;
		
		if (menuBarError) {
			barColor = AnsiColor.BGRed;
		}
		else if (lastSetMsg is StatusMSG.BreakpointHit) {
			barColor = AnsiColor.BGDarkGrey;
		}
		else {
			barColor = AnsiColor.BGBlue;
		}
		
		Display.ClearLine(Display.Height - 1, barColor);
		
		if (tempMsgTime is not null && tempMsgTime.ElapsedMilliseconds >= 3000) {
			resetMenuBar();
		}
		else if (tempMenuBarMsg is not null) {
			Display.Write(tempMenuBarMsg, 0, Display.Height - 1, barColor);
		}
		else {
			Display.Write(menuBarMsg,     0, Display.Height - 1, barColor);
		}
		
		if (buffer is not null || state == State.Init) {
			var cycle = buffer?.DSPCycle ?? 0;
			var cycleCounter = cyclesInSpcClocks ? $"SPC Cycle: {cycle / 2}" : $"DSP Cycle: {cycle}";
			Display.Write(cycleCounter, Display.Width - 1 - cycleCounter.Length, Display.Height - 1, barColor);
		}
		
		// Display error menu
		if (state == State.NonFatalError) {
			setStatusMsg(StatusMSG.Script700_Error, error: true);
			
			var winLeft   = 20;
			var winTop    = 8;
			var winWidth  = Display.Width  - 40;
			var winHeight = Display.Height - 18;
			
			Display.EnableCutout(winLeft, winTop, winWidth, winHeight);
			
			Display.DrawOutline(winLeft, winTop, winWidth, winHeight, AnsiColor.Yellow);
			Display.ClearBox(winWidth - 2, winHeight - 2, winLeft + 1, winTop + 1);
			
			Display.Write(" Error ", Display.Width / 2 - 3, 8, AnsiColor.Yellow);
			
			var errorText = Display.WordWrap("A non-fatal error has occurred during the processing of Script700 code.", Display.Width - 36, 3);
			Display.WriteBox(errorText, winLeft + 3, winTop + 2, AnsiColor.Yellow);
			Display.Y++;
			Display.WriteBox([
				"Error reason: ",
				"    Script700 execution timed out",
				""
			], col: AnsiColor.Yellow);
			
			var explainText = Display.WordWrap(
				"This error can occur from either an infinite loop, or the execution of a long stretch of non-yielding Script700 code " +
				"(i.e. no `w` command)",
				winWidth - 2 - 4, 3
			);
			Display.Write("Explanation: ", col: AnsiColor.Yellow);
			Display.Y++;
			Display.WriteBox(explainText, x_: winLeft + 2 + 4, col: AnsiColor.Yellow);
			Display.Y += 2;
			
			var displayY = Display.Y;
			
			for (var i = 0; i < 3; i++) {
				var region = menuRegions[i];
				var option = menuOptions[i];
				
				Display.ClearBox(region.W + 2, region.H, region.X - 1, region.Y + displayY, col: selectedItem == i ? AnsiColor.BGBlue : AnsiColor.Cyan);
				Display.WriteBox(option, region.X, region.Y + displayY, col: selectedItem == i ? AnsiColor.BGBlue : AnsiColor.Cyan);
			}
		}
		else {
			Display.DisableCutout();
		}
		
		if (state == State.Init) {
			UI_State = State.Normal;
		}
		
		Console.Write(Display.Flush());
	}
	
	static void doAction(KeyBindings.Action action) {
		switch (action) {
			case KeyBindings.Action.ExitCurrentMenu: {
				if (currentView != View.Help && tracePrevView is not null && currentView != tracePrevView && nextView != tracePrevView) {
					viewIndex = tracePrevViewIndex;
					changeCurrentView(tracePrevView!.Value, setAsRealView: false);
						
					tracePrevView      = null;
					tracePrevViewIndex = 0;
				}
				else if (currentView == View.Help) {
					changeCurrentView(realView, setAsRealView: false);
				}
				
				break;
			}
			
			case KeyBindings.Action.ToggleHelpMenu: {
				if (currentView == View.Help) {
					changeCurrentView(realView, setAsRealView: false);
				}
				else {
					realView = currentView;
					changeCurrentView(View.Help, setAsRealView: false);
				}
				
				break;
			}
			
			case KeyBindings.Action.NavNextView: {
				tracePrevView = null;
				viewIndex++;
				viewIndex %= views.Length;
				changeCurrentView(views[viewIndex], setAsRealView: false);
				break;
			}
			
			case KeyBindings.Action.NavPrevView: {
				tracePrevView = null;
				viewIndex--;
				viewIndex += views.Length;
				viewIndex %= views.Length;
				changeCurrentView(views[viewIndex], setAsRealView: false);
				break;
			}
			
			case KeyBindings.Action.EnableAllChannels: {
				for (var i = 0; i < 8; i++) {
					PrimaryEmu.EnableVoice(i);
				}
				
				setTempStatusMsg(StatusMSG.AllChannelsEnabled);
				break;
			}
			
			case KeyBindings.Action.ToggleChannel_1: {
				toggleChannel(0);
				break;
			}
			
			case KeyBindings.Action.ToggleChannel_2: {
				toggleChannel(1);
				break;
			}
			
			case KeyBindings.Action.ToggleChannel_3: {
				toggleChannel(2);
				break;
			}
			
			case KeyBindings.Action.ToggleChannel_4: {
				toggleChannel(3);
				break;
			}
			
			case KeyBindings.Action.ToggleChannel_5: {
				toggleChannel(4);
				break;
			}
			
			case KeyBindings.Action.ToggleChannel_6: {
				toggleChannel(5);
				break;
			}
			
			case KeyBindings.Action.ToggleChannel_7: {
				toggleChannel(6);
				break;
			}
			
			case KeyBindings.Action.ToggleChannel_8: {
				toggleChannel(7);
				break;
			}
			
			case KeyBindings.Action.ToggleMainChannel_1: {
				toggleMainChannel(0);
				break;
			}
			
			case KeyBindings.Action.ToggleMainChannel_2: {
				toggleMainChannel(1);
				break;
			}
			
			case KeyBindings.Action.ToggleMainChannel_3: {
				toggleMainChannel(2);
				break;
			}
			
			case KeyBindings.Action.ToggleMainChannel_4: {
				toggleMainChannel(3);
				break;
			}
			
			case KeyBindings.Action.ToggleMainChannel_5: {
				toggleMainChannel(4);
				break;
			}
			
			case KeyBindings.Action.ToggleMainChannel_6: {
				toggleMainChannel(5);
				break;
			}
			
			case KeyBindings.Action.ToggleMainChannel_7: {
				toggleMainChannel(6);
				break;
			}
			
			case KeyBindings.Action.ToggleMainChannel_8: {
				toggleMainChannel(7);
				break;
			}
			
			case KeyBindings.Action.ToggleEchoChannel_1: {
				toggleEchoChannel(0);
				break;
			}
			
			case KeyBindings.Action.ToggleEchoChannel_2: {
				toggleEchoChannel(1);
				break;
			}
			
			case KeyBindings.Action.ToggleEchoChannel_3: {
				toggleEchoChannel(2);
				break;
			}
			
			case KeyBindings.Action.ToggleEchoChannel_4: {
				toggleEchoChannel(3);
				break;
			}
			
			case KeyBindings.Action.ToggleEchoChannel_5: {
				toggleEchoChannel(4);
				break;
			}
			
			case KeyBindings.Action.ToggleEchoChannel_6: {
				toggleEchoChannel(5);
				break;
			}
			
			case KeyBindings.Action.ToggleEchoChannel_7: {
				toggleEchoChannel(6);
				break;
			}
			
			case KeyBindings.Action.ToggleEchoChannel_8: {
				toggleEchoChannel(7);
				break;
			}
			
			case KeyBindings.Action.ToggleLPF: {
				toggleLPF();
				break;
			}
			
			case KeyBindings.Action.ScrollRowUp: {
				if (currentView == View.MemoryViewer) {
					if (StartAddr >= 0x10) {
						StartAddr -= 0x10;
						requestEmuData(requests);
					}
				}
				else if (currentView == View.ASMViewer) {
					if (ScrollOffset < getScrollTopOffset()) {
						ScrollOffset++;
					}
				}
				
				break;
			}
			
			case KeyBindings.Action.ScrollRowDown: {
				if (currentView == View.MemoryViewer) {
					if (StartAddr <= 0x1_0000 - ScrollAreaRows * 0x10 - 0x10) {
						StartAddr += 0x10;
						requestEmuData(requests);
					}
				}
				else if (currentView == View.ASMViewer) {
					//scrollSignal = true;
					if (ScrollOffset > 0) {
						ScrollOffset--;
					}
				}
				
				break;
			}
			
			case KeyBindings.Action.ScrollPageUp: {
				if (currentView == View.MemoryViewer) {
					if (StartAddr >= 0x100) {
						StartAddr -= 0x100;
						requestEmuData(requests);
					}
					else if (StartAddr > 0) {
						//scrollSignal = true;
						StartAddr    = 0;
						requestEmuData(requests);
					}
				}
				else if (currentView == View.ASMViewer) {
					if (ScrollOffset + 16 <= getScrollTopOffset()) {
						ScrollOffset += 16;
					}
					else {
						ScrollOffset = getScrollTopOffset();
					}
				}
				
				break;
			}
			
			case KeyBindings.Action.ScrollPageDown: {
				if (currentView == View.MemoryViewer) {
					if (StartAddr <= 0xFF00 - ScrollAreaRows * 0x10) {
						StartAddr += 0x100;
					}
					else if (StartAddr < 0x1_0000 - ScrollAreaRows * 0x10) {
						StartAddr = 0x1_0000 - ScrollAreaRows * 0x10;
					}
					requestEmuData(requests);
				}
				else if (currentView == View.ASMViewer) {
					if (ScrollOffset - 16 >= 0) {
						ScrollOffset -= 16;
					}
					else {
						ScrollOffset = 0;
					}
				}
				
				break;
			}
			
			case KeyBindings.Action.ScrollStart: {
				if (currentView == View.MemoryViewer) {
					if (StartAddr > 0) {
						StartAddr = 0;
						requestEmuData(requests);
					}
				}
				else if (currentView == View.ASMViewer) {
					ScrollOffset = getScrollTopOffset();
				}
				
				break;
			}
			
			case KeyBindings.Action.ScrollEnd: {
				if (currentView == View.MemoryViewer) {
					if (StartAddr < 0x1_0000 - ScrollAreaRows * 0x10) {
						StartAddr = 0x1_0000 - ScrollAreaRows * 0x10;
						requestEmuData(requests);
					}
				}
				else if (currentView == View.ASMViewer) {
					ScrollOffset = 0;
				}
				
				break;
			}
			
			case KeyBindings.Action.ToggleHeatMap: {
				if (currentView is View.MemoryViewer or View.DSPViewer1 or View.DSPViewer2 or View.DSPViewer3 or View.Script700Viewer) {
					heatMapEnabled = !heatMapEnabled;
					Display.Clear();
					
					if (heatMapEnabled) {
						setTempStatusMsg(StatusMSG.HeatMapOn);
					}
					else {
						setTempStatusMsg(StatusMSG.HeatMapOff);
					}
				}
				break;
			}
			
			case KeyBindings.Action.ToggleCycleUnit: {
				cyclesInSpcClocks = !cyclesInSpcClocks;
				setTempStatusMsg(StatusMSG.CycleDisplayChanged);
				break;
			}
			
			case KeyBindings.Action.SeekFwd: {
				seek(+5);
				setTempStatusMsg(StatusMSG.SeekFwd);
				break;
			}
			
			case KeyBindings.Action.SeekBack: {
				seek(-5);
				setTempStatusMsg(StatusMSG.SeekBack);
				break;
			}
			
			case KeyBindings.Action.SeekFwdFar: {
				seek(+30);
				setTempStatusMsg(StatusMSG.SeekFwdFar);
				break;
			}
			
			case KeyBindings.Action.SeekBackFar: {
				seek(-30);
				setTempStatusMsg(StatusMSG.SeekBackFar);
				break;
			}
			
			case KeyBindings.Action.SeekPos_0: {
				seekPos(0);
				setTempStatusMsg(StatusMSG.SeekPos);
				break;
			}
			
			case KeyBindings.Action.SeekPos_1: {
				seekPos(1);
				setTempStatusMsg(StatusMSG.SeekPos);
				break;
			}
			
			case KeyBindings.Action.SeekPos_2: {
				seekPos(2);
				setTempStatusMsg(StatusMSG.SeekPos);
				break;
			}
			
			case KeyBindings.Action.SeekPos_3: {
				seekPos(3);
				setTempStatusMsg(StatusMSG.SeekPos);
				break;
			}
			
			case KeyBindings.Action.SeekPos_4: {
				seekPos(4);
				setTempStatusMsg(StatusMSG.SeekPos);
				break;
			}
			
			case KeyBindings.Action.SeekPos_5: {
				seekPos(5);
				setTempStatusMsg(StatusMSG.SeekPos);
				break;
			}
			
			case KeyBindings.Action.SeekPos_6: {
				seekPos(6);
				setTempStatusMsg(StatusMSG.SeekPos);
				break;
			}
			
			case KeyBindings.Action.SeekPos_7: {
				seekPos(7);
				setTempStatusMsg(StatusMSG.SeekPos);
				break;
			}
			
			case KeyBindings.Action.SeekPos_8: {
				seekPos(8);
				setTempStatusMsg(StatusMSG.SeekPos);
				break;
			}
			
			case KeyBindings.Action.SeekPos_9: {
				seekPos(9);
				setTempStatusMsg(StatusMSG.SeekPos);
				break;
			}
			
			case KeyBindings.Action.ToggleBreak: {
				ToggleBreak();
				
				if (UI_State == State.Paused) {
					setTempStatusMsg(StatusMSG.BreakExec);
					forceTraceLoggerView();
				}
				else {
					setTempStatusMsg(StatusMSG.Continue);
					resetTraceLog();
					
					if (tracePrevView is not null && currentView != tracePrevView && nextView != tracePrevView) {
						viewIndex = tracePrevViewIndex;
						changeCurrentView(tracePrevView!.Value, setAsRealView: false);
						
						tracePrevView      = null;
						tracePrevViewIndex = 0;
					}
				}
				
				break;
			}
			
			case KeyBindings.Action.TogglePause: {
				TogglePause();
				
				if (UI_State == State.Paused) {
					setTempStatusMsg(StatusMSG.Paused);
				}
				else {
					setTempStatusMsg(StatusMSG.Continue);
					resetTraceLog();
				}
				
				break;
			}
			
			case KeyBindings.Action.StepInstruction: {
				stepInstruction();
				break;
			}
			
			case KeyBindings.Action.ToggleBreakpoints: {
				var enabled = ToggleBreakpoints();
				
				if (enabled) {
					setTempStatusMsg(StatusMSG.BreakpointsOn);
				}
				else {
					setTempStatusMsg(StatusMSG.BreakpointsOff);
				}
				
				break;
			}
			
			case KeyBindings.Action.IncHeatMapDataSize: {
				heatMapDataSize = heatMapDataSize.Next();
				setTempStatusMsg(StatusMSG.BusSizeChanged);
				break;
			}
			
			case KeyBindings.Action.DecHeatMapDataSize: {
				heatMapDataSize = heatMapDataSize.Prev();
				setTempStatusMsg(StatusMSG.BusSizeChanged);
				break;
			}
		}
	}
	
	static void forceTraceLoggerView() {
		if (currentView != View.ASMViewer && nextView != View.ASMViewer) {
			tracePrevView      = nextView;
			tracePrevViewIndex = viewIndex;
						
			viewIndex = asmViewerIndex;
			changeCurrentView(View.ASMViewer, setAsRealView: false);
		}
	}
	
	static void stepInstruction(bool log = true) {
		if (UI_State == State.Paused) {
			if (log) {
				ScrollOffset = 0;
				InstructionsSinceTrace = NextInstructionsSinceTrace;
				refreshSignal++;
			}
					
			if (currentView != View.ASMViewer) {
				resetTraceLog();
			}
					
			Transfer.StepSignal.Set(); // Signal emulating thread to step one single instruction
		}
		else {
			TogglePause();
			setTempStatusMsg(StatusMSG.Paused);
		}
	}
	
	static void toggleChannel(int channelIndex) {
		var newOnState = PrimaryEmu.ToggleVoice(channelIndex);
		channelToToggle = channelIndex + 1;
		
		setTempStatusMsg(newOnState ? StatusMSG.ChannelX_Enabled : StatusMSG.ChannelX_Disabled);
	}
	
	static void toggleMainChannel(int channelIndex) {
		var newOnState = PrimaryEmu.ToggleMainVoice(channelIndex);
		channelToToggle = channelIndex + 1;
		
		setTempStatusMsg(newOnState ? StatusMSG.MainChannelX_Enabled : StatusMSG.MainChannelX_Disabled);
	}
	
	static void toggleEchoChannel(int channelIndex) {
		var newOnState = PrimaryEmu.ToggleEchoVoice(channelIndex);
		channelToToggle = channelIndex + 1;
		
		setTempStatusMsg(newOnState ? StatusMSG.EchoChannelX_Enabled : StatusMSG.EchoChannelX_Disabled);
	}
	
	static void toggleLPF() {
		var newLpfEnabled = !PrimaryEmu.LowpassEnabled;
		PrimaryEmu.LowpassEnabled = newLpfEnabled;
		
		if (newLpfEnabled) {
			Driver.ChangeSampleRate(96000);
		}
		else {
			Driver.ChangeSampleRate(32000);
		}
		
		setTempStatusMsg(newLpfEnabled ? StatusMSG.LPF_Enabled : StatusMSG.LPF_Disabled);
	}
	
	static void seek(int offsetInSeconds) {
		var targetCycle = Math.Max(0, curCycle + offsetInSeconds * 2048000);
		var targetSnapshotIndex = GetSnapshotIndex(targetCycle);
		
		if (offsetInSeconds >= 0 && targetCycle > 2048000L * (60 * 12 + offsetInSeconds - 1)) {
			return;
		}
		
		loadSnapshot(targetSnapshotIndex);
	}
	
	static void seekPos(int position) {
		position = Math.Clamp(position, 0, 9);
		seekPosition = position;
		seekAbsolute(position / 10.0);
	}
	
	static void seekAbsolute(double songtimeRatio) {
		songtimeRatio = Math.Clamp(songtimeRatio, 0, 1);
		
		var targetCycle         = Math.Max(0, (long) (songtimeRatio * 2048000 * (PrimaryEmu.SpcMetadata.LengthInSeconds ?? 60 * 12)));
		var targetSnapshotIndex = GetSnapshotIndex(targetCycle);
		
		loadSnapshot(targetSnapshotIndex);
	}
	
	static void loadSnapshot(int targetSnapshotIndex) {
		ignoreStepDisplay = true;
		resetTraceLog();
		
		Emulator? snapshot = null;
		
		lock (seekBarLock) {
			while (!seekBarSnapshots.ContainsKey(targetSnapshotIndex)) {
				targetSnapshotIndex--;
				if (targetSnapshotIndex == 0) {
					break;
				}
			}
			
			seekBarSnapshots.TryGetValue(targetSnapshotIndex, out snapshot);
		}
		
		if (snapshot is null) {
			throw new UnreachableException($"No viable seekbar snapshot found (this should never happen)");
		}
		
		lock (EmuRestoreLock) {
			PrimaryEmu.LoadStateFrom(snapshot);
			lastInstructionCycle = PrimaryEmu.DSP.CurrentCycle;
		}
	}
	
	static void changeCurrentView(View newView, bool setAsRealView = true) {
		if (currentView != nextView) return;
		nextView = newView;
		
		// Make requests
		switch (nextView) {
			case View.ASMViewer: {
				requestEmuData(Transfer.Requests.SPC_Regs | Transfer.Requests.MemLogs | Transfer.Requests.SMP_State);
				Display.CurrentBufferId = "trace";
				Display.ScrollTop = Math.Max(0, InstructionsSinceTrace - ScrollAreaRows - ScrollOffset);
				Display.SetWindowProps(0, 0, Display.Width, ScrollAreaRows);
				Display.EnableWindow();
				
				if (UI_State != State.Paused) {
					resetTraceLog();
				}
				
				break;
			}
			
			case View.MemoryViewer: {
				resetStatusMsg();
				requestEmuData(Transfer.Requests.SMP_Bus
				               | Transfer.Requests.DSP_2
				               | Transfer.Requests.SPC_Regs
				               | Transfer.Requests.MemLogs
				               | Transfer.Requests.SMP_State);
				Display.CurrentBufferId = "aram";
				Display.SetWindowProps(0, 0, 110, ScrollAreaRows);
				Display.EnableWindow();
				break;
			}
			
			case View.DSPViewer1: {
				resetStatusMsg();
				requestEmuData(Transfer.Requests.DSP_RegisterMem | Transfer.Requests.DSP_1 | Transfer.Requests.MemLogs | Transfer.Requests.SMP_State);
				Display.HideWindow();
				break;
			}
			
			case View.DSPViewer2: {
				resetStatusMsg();
				requestEmuData(Transfer.Requests.DSP_RegisterMem | Transfer.Requests.DSP_2 | Transfer.Requests.MemLogs | Transfer.Requests.SMP_State);
				Display.HideWindow();
				break;
			}
			
			case View.DSPViewer3: {
				resetStatusMsg();
				requestEmuData(Transfer.Requests.DSP_3);
				Display.HideWindow();
				break;
			}
			
			case View.Script700Viewer: {
				resetStatusMsg();
				requestEmuData(Transfer.Requests.Script700 | Transfer.Requests.SMP_State);
				Display.HideWindow();
				break;
			}
			
			default: {
				resetStatusMsg();
				requestEmuData(Transfer.Requests.CycleCountOnly);
				Display.HideWindow();
				break;
			}
		}
		
		if (setAsRealView) {
			realView = newView;
		}
	}
	
	static void commitCurrentView() {
		currentView = nextView;
		Display.Clear();
	}
	
	static void setStatusMsg(StatusMSG msg, bool error = false) {
		tempMenuBarMsg = null;
		menuBarError   = false;
		tempMsgTime    = null;
		
		menuBarMsg   = statusMsg(msg);
		menuBarError = error;
	}
	
	static void resetStatusMsg() {
		if (tempMenuBarMsg is null) {
			setStatusMsg(StatusMSG.Default);
		}
	}
	
	static void setTempStatusMsg(StatusMSG msg, bool error = false) {
		setStatusMsg(StatusMSG.Default);
		
		tempMenuBarMsg = statusMsg(msg);
		tempMsgTime    = new();
		menuBarError   = error;
		
		tempMsgTime.Start();
	}
	
	static StatusMSG lastSetMsg = StatusMSG.Default;
	
	static string statusMsg(StatusMSG msg) {
		lastSetMsg = msg;
		
		return msg switch {
			StatusMSG.Default               => "Press CTRL+L for help menu",
			StatusMSG.HeatMapOff            => "Heat map disabled",
			StatusMSG.HeatMapOn             => "Heat map enabled",
			StatusMSG.BusSizeChanged        => $"Heat map data size changed to {heatMapDataSize.Name()}",
			StatusMSG.ChannelX_Disabled     => $"Channel {channelToToggle} disabled      {showActiveChannels()}",
			StatusMSG.ChannelX_Enabled      => $"Channel {channelToToggle} enabled       {showActiveChannels()}",
			StatusMSG.MainChannelX_Disabled => $"Main channel {channelToToggle} disabled {showActiveChannels()}",
			StatusMSG.MainChannelX_Enabled  => $"Main channel {channelToToggle} enabled  {showActiveChannels()}",
			StatusMSG.EchoChannelX_Disabled => $"Echo channel {channelToToggle} disabled {showActiveChannels()}",
			StatusMSG.EchoChannelX_Enabled  => $"Echo channel {channelToToggle} enabled  {showActiveChannels()}",
			StatusMSG.LPF_Disabled          => $"SNES Low-Pass Filter disabled",
			StatusMSG.LPF_Enabled           => $"SNES Low-Pass Filter enabled",
			StatusMSG.AllChannelsEnabled    => $"All channels enabled    {showActiveChannels()}",
			StatusMSG.SeekFwd               => $"Seek +5 seconds",
			StatusMSG.SeekBack              => $"Seek -5 seconds",
			StatusMSG.SeekFwdFar            => $"Seek +30 seconds",
			StatusMSG.SeekBackFar           => $"Seek -30 seconds",
			StatusMSG.SeekPos               => $"Seek to position {seekPosition}",
			StatusMSG.SteppedCycles         => cyclesInSpcClocks ? 
			                                         $"Stepped {stepCycles / 2} SPC cycle{(stepCycles / 2 == 1 ? "" : "s")}"
			                                       : $"Stepped {stepCycles} DSP cycles",
			StatusMSG.CycleDisplayChanged   => $"Cycle display mode changed: {(cyclesInSpcClocks ? "SPC700" : "S-DSP")}",
			StatusMSG.Paused                => $"Paused",
			StatusMSG.BreakExec             => $"Execution break",
			StatusMSG.BreakpointHit         => $"Execution breakpoint hit at ${execBreakpointAddr:X4}",
			StatusMSG.BreakpointsOn         => $"All breakpoints enabled",
			StatusMSG.BreakpointsOff        => $"All breakpoints disabled",
			StatusMSG.Script700_Error       => $"Script700 error occurred",
			StatusMSG.Continue              => $"Resuming playback",
			_                               => throw new NotImplementedException()
		};
	}
	
	static string showActiveChannels() {
		var enabled = PrimaryEmu.VoiceOnStates;
		
		StringBuilder sb = new();
		
		sb.Append('[');
		foreach (var (voice, _) in enabled) {
			sb.Append(voice ? '+' : '-');
		}
		sb.Append(']').Append(' ');
		
		sb.Append('[');
		foreach (var (_, voice) in enabled) {
			sb.Append(voice ? '+' : '-');
		}
		sb.Append(']');
		
		return sb.ToString();
	}
	
	static void resetMenuBar() {
		tempMenuBarMsg = null;
		tempMsgTime    = null;
	}
	
	public enum BusSize {
		Bit8, Bit16, Bit24, Bit32, Bit64
	}
	
	static AnsiColor writeRegColor = new(AnsiColor.Code.Black, AnsiColor.Code.White);
	static AnsiColor writeErrColor = new(AnsiColor.Code.BrightRed);
	static AnsiColor writeRomColor = new(AnsiColor.Code.BrightCyan);
	static AnsiColor writeColor    = new(AnsiColor.Code.Cyan,    isBG: true);
	static AnsiColor pcColor       = new(AnsiColor.Code.Magenta, isBG: true);
	static AnsiColor execColor     = new(AnsiColor.Code.Blue,    isBG: true);
	static AnsiColor fetchColor    = new(AnsiColor.Code.BrightBlue);
	static AnsiColor readColor     = new(AnsiColor.Code.Green,   isBG: true);
	static AnsiColor readRegColor1 = new(AnsiColor.Code.Yellow,  isBG: true);
	static AnsiColor readRegColor2 = new(250, 125, 25,           isBG: true);
	static AnsiColor readErrColor  = new(AnsiColor.Code.Red,     isBG: true);
	static AnsiColor readRomColor  = new(AnsiColor.Code.Grey,    isBG: true);
	
	static void memDisplayRows(BusSize addrBusSize,
	                           int startRow,
	                           int endRow,
	                           byte[] data,
	                           AnsiColor?[]? colorData = null,
	                           SMP.MemAccessLog[]? memLogs = null,
	                           bool readDisabled   = false,
	                           bool writeDisabled  = false,
	                           bool bootRomEnabled = false,
	                           UInt32? pc = null,
	                           bool isDSP = false,
	                           bool useHeatMap = false,
	                           int yOffset = 0,
	                           bool writeToScrollBuf = false)
	{
		Display.UpdateState(writeToScrollBuf);
		
		Display.X = 0;
		Display.Y = yOffset;
		
		for (var i = startRow; i <= endRow; i++) {
			var startAddr = (uint) i * 16;
			
			AnsiColor? getColor(int c) {
				var realAddr = startAddr + c;
				var idx      = (i - startRow) * 16 + c;
				
				var color = colorData?[idx];
				
				if (memLogs?.FirstOrDefault(x => x.Address == realAddr && x.Type == SMP.MemAccessLog.LogType.Write) is not null) {
					if (realAddr is >= 0x0F0 and <= 0x00FF) {
						color = writeRegColor;
					}
					else if (writeDisabled) {
						color = writeErrColor;
					}
					else if (realAddr >= 0xFFC0 && bootRomEnabled) {
						color = writeRomColor;
					}
					else {
						color = writeColor;
					}
				}
				else if (realAddr == pc) {
					color = pcColor;
				}
				else if (memLogs?.FirstOrDefault()?.Type == SMP.MemAccessLog.LogType.Exec && memLogs[0].Address == realAddr) {
					color = execColor;
				}
				else if (memLogs?.FirstOrDefault(x => x.Address == realAddr && x.Type == SMP.MemAccessLog.LogType.Fetch) is not null) {
					color = fetchColor;
				}
				else if (memLogs?.FirstOrDefault(x => x.Address == realAddr && x.Type == SMP.MemAccessLog.LogType.Fetch) is not null) {
					color = fetchColor;
				}
				else if (memLogs?.FirstOrDefault(x => x.Address == realAddr && x.Type == SMP.MemAccessLog.LogType.Read) is not null) {
					if (realAddr is >= 0x0F0 and <= 0x00FC) {
						color = readRegColor1;
					}
					else if (realAddr is >= 0x0FD and <= 0x00FF) {
						color = readRegColor2;
					}
					else if (realAddr >= 0xFFC0 && bootRomEnabled) {
						color = readRomColor;
					}
					else if (readDisabled) {
						color = readErrColor;
					}
					else {
						color = readColor;
					}
				}
				
				return color;
			}

			switch (addrBusSize) {
				case BusSize.Bit8: {
					Display.Write($"{startAddr:X2} | ", writeToScrollBuf: writeToScrollBuf);
					break;
				}
				case BusSize.Bit16: {
					Display.Write($"{startAddr:X4} | ", writeToScrollBuf: writeToScrollBuf);
					break;
				}
				case BusSize.Bit24: {
					Display.Write($"{startAddr:X6} | ", writeToScrollBuf: writeToScrollBuf);
					break;
				}
				case BusSize.Bit32: {
					Display.Write($"{startAddr:X8} | ", writeToScrollBuf: writeToScrollBuf);
					break;
				}
			}
			
			for (var c = 0; c < 16; c++) {
				var idx = (i - startRow) * 16 + c;
				
				if (idx >= 0 && idx < data.Length) {
					var color = getColor(c);
					
					Display.Write($"{data[idx]:X2}", col: color, writeToScrollBuf: writeToScrollBuf);
					Display.Write(" ", writeToScrollBuf: writeToScrollBuf);
				}
			}
			Display.Write("| ", writeToScrollBuf: writeToScrollBuf);
			
			if (useHeatMap) {
				for (var c = 0; c < 16; c++) {
					var idx = (i - startRow) * 16 + c;
					
					if (idx >= 0 && idx < data.Length) {
						var val = data[idx];
						var col = heatMapColor(BusSize.Bit8, signed: false, scale: 1.0, val);
						Display.Write("  ", col: col, writeToScrollBuf: writeToScrollBuf);
					}
				}
			}
			else {
				for (var c = 0; c < 16; c++) {
					var idx = (i - startRow) * 16 + c;
					
					if (idx >= 0 && idx < data.Length) {
						var val   = data[idx];
						var color = getColor(c);

						Display.Write($"{(val is >= 0x20 and <= 0x7E ? (char) val : '.')}", col: color, writeToScrollBuf: writeToScrollBuf);
					}
				}
				Display.Write(new string(' ', 16), writeToScrollBuf: writeToScrollBuf);
			}
			
			Display.Write("\n", writeToScrollBuf: writeToScrollBuf);
		}
	}
	
	static byte[] heatValues = [0x48, 0x50, 0x5C, 0x68, 0x84, 0xA0, 0xC0, 0xE0, 0xA0, 0xA0, 0xA0, 0xA0, 0xA0, 0xA0, 0xA0, 0xA0];
	
	static void showBar(double value, int displayHeight, int x, int y) {
		if (eqInsideColor is null) {
			eqInsideColor = heatMapColor(BusSize.Bit8, false, 1, 0);
		}
		
		var eqInsideRGB = eqInsideColor.BackgroundRGB!.Multiply(5.0 / 7);
		
		var color  = AnsiColor.Red;
		var height = (int) Math.Round(value * displayHeight * 8);
		
		var refColor = heatMapColor(BusSize.Bit8, signed: false, scale: 1, 0xFF).BackgroundRGB!;
		
		x -= 1;
		
		if (height > 0) {
			for (var i = 0; i < displayHeight; i++) {
				AnsiColor fgAnsi;
				Color midColor;
				
				if (i >= displayHeight / 2) {
					var interp  = (double) (displayHeight         - i) / (displayHeight / 2 + 1);
					var interp2 = (double) (displayHeight - (i + 0.5)) / (displayHeight / 2 + 1);
					interp  = Math.Pow(interp,  2);
					interp2 = Math.Pow(interp2, 2);
					
					var bgCol = heatMapColor(BusSize.Bit8, signed: true,  scale: 1, heatValues[i * 2]).BackgroundRGB!;
					var fgCol = refColor.Blend(bgCol, 1 - interp, Color.Space.LCh);
					
					midColor = refColor.Blend(bgCol, 1 - interp2, Color.Space.LCh);
					
					fgAnsi = new(fgCol, eqInsideRGB);
				}
				else {
					var bgCol = heatMapColor(BusSize.Bit8, signed: false, scale: 1, heatValues[i * 2    ]).BackgroundRGB!;
					midColor  = heatMapColor(BusSize.Bit8, signed: false, scale: 1, heatValues[i * 2 + 1]).BackgroundRGB!;
					fgAnsi    = new(bgCol, eqInsideRGB);
				}
				
				#if LINUX // By default, Windows terminal emulators do not seem to support unicode char display - make bars more coarse for those
					if (i < height / 8) {
						fgAnsi = new(fgAnsi.ForegroundRGB!, midColor);
						Display.Write("▄▄▄", x, y - i - 1, fgAnsi);
					}
					else if (i == height / 8 && height % 8 > 0) {
						var barString = (height % 8) switch {
							1 => "▁▁▁",
							2 => "▂▂▂",
							3 => "▃▃▃",
							4 => "▄▄▄",
							5 => "▅▅▅",
							6 => "▆▆▆",
							7 => "▇▇▇",
							_ => throw new UnreachableException()
						};
						Display.Write(barString, x, y - i - 1, fgAnsi);
					}
				#else
					if (i < height / 8) {
						fgAnsi = new(fgAnsi.ForegroundRGB!, midColor);
						Display.Write("▄▄▄", x, y - i - 1, fgAnsi);
					}
					else if (i == height / 8 && height % 8 >= 4) {
						var barString = (height % 8) switch {
							4 => "▄▄▄",
							5 => "▄▄▄",
							6 => "▄▄▄",
							7 => "▄▄▄",
							_ => throw new UnreachableException()
						};
						Display.Write(barString, x, y - i - 1, fgAnsi);
					}
				#endif
			}
		}
		else if (height < 0) {
			for (var i = 0; i < displayHeight; i++) {
				if (i < -height / 2) {
					Display.Write("███", x, y + i, col: color);
				}
				else if (i == -height / 2 && -height % 2 == 1) {
					Display.Write("▀▀▀", x, y + i, col: color);
				}
			}
		}
	}
	
	static void showColorCoding() {
		if (heatMapEnabled) {
			var i = 0;
			
			var legX = Display.Width  - 19;
			var legY = Display.Height - 10;
			
			Display.Write("00 ..Unsigned..  FF", legX, legY + 1);
			Display.Write("00 ..Positive.. +7F", legX, legY + 3);
			Display.Write("00 ..Negative.. -80", legX, legY + 5);
			
			for (var v = 2; v <= 0x102; v += 0x20) {
				v = Math.Clamp(v, 0, 0x101);
				
				Display.Write("  ", legX + i * 2, legY,     col: heatMapColor(BusSize.Bit8, signed: false, scale: 1,  v   - 2));
				Display.Write("  ", legX + i * 2, legY + 2, col: heatMapColor(BusSize.Bit8, signed:  true, scale: 1,  v/2 - 1));
				Display.Write("  ", legX + i * 2, legY + 4, col: heatMapColor(BusSize.Bit8, signed:  true, scale: 1, -v/2 + 1));
				
				i++;
			}
		}
	}
	
	static void displayHeatMap24(UInt32 value, int x, int y, bool isBG = true) {
		switch (heatMapDataSize) {
			case BusSize.Bit8: {
				for (var i = 1; i < 4; i++) {
					Display.Highlight(
						2, x + 2 * i - 2, y, col: heatMapColor(BusSize.Bit8, signed: false, scale: 1, value >> 24 - i * 8 & 0xFF, isBG).BackgroundRGB
					);
				}
				break;
			}
			
			case BusSize.Bit16: {
				Display.Highlight(2, x,     y, col: heatMapColor(BusSize.Bit8,  signed: false, scale: 1, (value & 0xFFFFFF) >> 16, isBG).BackgroundRGB);
				Display.Highlight(4, x + 2, y, col: heatMapColor(BusSize.Bit16, signed: false, scale: 1, value & 0xFFFF,           isBG).BackgroundRGB);
				break;
			}
			
			case BusSize.Bit32 or BusSize.Bit64: {
				Display.Highlight(
					6, x, y, col: heatMapColor(BusSize.Bit32, signed: false, scale: 1, value * 256, isBG).BackgroundRGB
				);
				break;
			}
			
			default: {
				throw new UnreachableException();
			}
		}
	}
	
	static void displayHeatMap32(UInt32 value, int x, int y, bool isBG = true) {
		switch (heatMapDataSize) {
			case BusSize.Bit8: {
				for (var i = 0; i < 4; i++) {
					Display.Highlight(
						2, x + 2 * i, y, col: heatMapColor(BusSize.Bit8, signed: false, scale: 1, value >> 24 - i * 8 & 0xFF, isBG).BackgroundRGB
					);
				}
				break;
			}
			
			case BusSize.Bit16: {
				for (var i = 0; i < 2; i++) {
					Display.Highlight(
						4, x + 4 * i, y, col: heatMapColor(BusSize.Bit16, signed: false, scale: 1, value >> 16 - i * 16 & 0xFFFF, isBG).BackgroundRGB
					);
				}
				break;
			}
			
			case BusSize.Bit32 or BusSize.Bit64: {
				Display.Highlight(
					8, x, y, col: heatMapColor(BusSize.Bit32, signed: false, scale: 1, value, isBG).BackgroundRGB
				);
				break;
			}
			
			default: {
				throw new UnreachableException();
			}
		}
	}
	
	static void displayHeatMap64(UInt64 value, int x, int y, bool isBG = true) {
		switch (heatMapDataSize) {
			case BusSize.Bit8: {
				for (var i = 0; i < 8; i++) {
					Display.Highlight(
						2, x + 2 * i, y, col: heatMapColor(BusSize.Bit8, signed: false, scale: 1, (long) (value >> 56 - i * 8 & 0xFF), isBG).BackgroundRGB
					);
				}
				break;
			}
			
			case BusSize.Bit16: {
				for (var i = 0; i < 4; i++) {
					Display.Highlight(
						4, x + 4 * i, y,
						col: heatMapColor(BusSize.Bit16, signed: false, scale: 1, (long) (value >> 48 - i * 16 & 0xFFFF), isBG).BackgroundRGB
					);
				}
				break;
			}
			
			case BusSize.Bit32: {
				for (var i = 0; i < 2; i++) {
					Display.Highlight(
						8, x + 8 * i, y,
						col: heatMapColor(BusSize.Bit32, signed: false, scale: 1, (long) (value >> 32 - i * 32 & 0xFFFFFFFF), isBG).BackgroundRGB
					);
				}
				break;
			}
			
			case BusSize.Bit64: {
				Display.Highlight(
					16, x, y, col: heatMapColor(BusSize.Bit64, signed: false, scale: 1, (long) value, isBG).BackgroundRGB
				);
				break;
			}
			
			default: {
				throw new UnreachableException();
			}
		}
	}
	
	static Color heatMapZero() {
		return Color.FromLCh(0.1, 70, 280);
	}
	
	static AnsiColor heatMapColor(BusSize dataSize, bool signed, double scale, long value, bool isBG = true) {
		double interp;
		
		switch (dataSize) {
			case BusSize.Bit8: {
				value = signed ? (sbyte) value : (byte) value;
				if (signed) {
					interp = (double) value / -sbyte.MinValue;
				}
				else {
					interp = (double) value / byte.MaxValue;
				}
				break;
			}
			
			case BusSize.Bit16: {
				value = signed ? (Int16) value : (UInt16) value;
				if (signed) {
					interp = (double) value / -Int16.MinValue;
				}
				else {
					interp = (double) value / UInt16.MaxValue;
				}
				break;
			}
			
			case BusSize.Bit24: {
				var v = (ulong) value & 0xFFFFFF;
				
				if (signed) {
					if (v < 0x800000) {
						value = (long) v;
					}
					else {
						value = (long) v - 0x1000000;
					}
				}
				
				if (signed) {
					interp = (double) value / 0x800000;
				}
				else {
					interp = (double) value / 0xFFFFFF;
				}
				
				break;
			}
			
			case BusSize.Bit32: {
				value = signed ? (Int32) value : (UInt32) value;
				if (signed) {
					interp = (double) value / -((long) Int32.MinValue);
				}
				else {
					interp = (double) value / UInt32.MaxValue;
				}
				break;
			}
			
			case BusSize.Bit64: {
				value = signed ? value : (Int64) value.SafeUnsigned();
				if (signed) {
					interp = (double) value / Int64.MaxValue;
				}
				else {
					interp = (double) value / UInt64.MaxValue;
				}
				break;
			}
			
			default: {
				throw new UnreachableException();
			}
		}
		
		interp *= scale;
		var origInterp = interp;
		
		if (interp >= 0) {
			interp = Math.Pow(interp, 1 / 1.9);
		}
		else {
			interp = -Math.Pow(-interp, 1 / 1.9);
		}
		
		var zero = Color.FromLCh(0.1, 70, 280);
		
		var maxUns = Color.FromLCh(85.5, 47, 288);
		var maxPos = Color.FromLCh(94.0, 97, 125);
		var maxNeg = Color.FromLCh(89.0, 87,  60);
		
		Color col;
		
		if (signed) {
			if (interp >= 0) {
				col = maxPos.Blend(zero, 1 - Math.Abs(interp), Color.Space.LCh);
			}
			else {
				col = maxNeg.Blend(zero, 1 - Math.Abs(interp), Color.Space.LCh);
			}
		}
		else {
			col = maxUns.Blend(zero, 1 - Math.Abs(interp), Color.Space.LCh);
		}
		
		return new(col, isBG);
	}
	
	static (char Char, AnsiColor Color)[] drawHeatMapFlags(BusSize dataSize, ulong value) {
		var zero = heatMapColor(BusSize.Bit8, signed: false, scale: 1, value:   0).BackgroundRGB!;
		var one  = heatMapColor(BusSize.Bit8, signed: false, scale: 1, value: 255).BackgroundRGB!;
		
		bool[] flags;
		int midIndex;
		
		switch (dataSize) {
			case BusSize.Bit8: {
				flags    = Enumerable.Range(0,  8).Select(i => value.GetBit(i)).ToArray();
				midIndex = 4;
				break;
			}
			
			case BusSize.Bit16: {
				flags    = Enumerable.Range(0, 16).Select(i => value.GetBit(i)).ToArray();
				midIndex = 8;
				break;
			}
			
			case BusSize.Bit24: {
				flags    = Enumerable.Range(0, 24).Select(i => value.GetBit(i)).ToArray();
				midIndex = 12;
				break;
			}
			
			case BusSize.Bit32: {
				flags    = Enumerable.Range(0, 32).Select(i => value.GetBit(i)).ToArray();
				midIndex = 16;
				break;
			}
			
			case BusSize.Bit64: {
				flags    = Enumerable.Range(0, 64).Select(i => value.GetBit(i)).ToArray();
				midIndex = 32;
				break;
			}
			
			default: {
				throw new UnreachableException();
			}
		}
		
		var result = new (char Char, AnsiColor Color)[midIndex];
		
		for (var i = 0; i < midIndex; i++) {
			var top    = flags[i];
			var bottom = flags[i + midIndex];
			
			if (top && bottom) {
				result[i].Char = '█';
			}
			else if (top && !bottom) {
				result[i].Char = '▀';
			}
			else if (!top && bottom) {
				result[i].Char = '▄';
			}
			else {
				result[i].Char = ' ';
			}
			
			result[i].Color = new(one, zero);
		}
		
		return result;
	}
	
	static void requestEmuData(Transfer.Requests reqs) {
		requestEmuData(reqs, StartAddr, ScrollAreaRows * 0x10);
	}
	
	static void requestEmuData(Transfer.Requests reqs, int startAddress, uint length) {
		requests = reqs;
		Transfer.RequestEmuData(reqs, startAddress, length);
	}
}