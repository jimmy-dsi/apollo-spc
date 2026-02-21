namespace SpcProgram;

using System.Diagnostics;
using System.Text;

using Apollo;
using Jimbl;

public static partial class CliMain {
	const int ScrollAreaRows = 0x1E;
	
	class EndAppException: Exception { }
	
	public enum State {
		Init, Normal, Paused, NonFatalError
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
		HeatMapOff, HeatMapOn,
		
		ChannelX_Enabled,  MainChannelX_Enabled,  EchoChannelX_Enabled,
		ChannelX_Disabled, MainChannelX_Disabled, EchoChannelX_Disabled,
		
		AllChannelsEnabled,  AllMainChannelsEnabled,  AllEchoChannelsEnabled,
		AllChannelsDisabled, AllMainChannelsDisabled, AllEchoChannelsDisabled,
		
		SeekFwd, SeekBack, SeekFwdFar, SeekBackFar, SeekPos,
		
		Script700_Error, Continue
	}
	
	static State  uiState          = State.Init;
	static bool   disableScript700 = false;
	static object uiStateLock      = new();
	
	static View       realView       = View.Metadata;
	static View       currentView    = View.Metadata;
	static View       nextView       = View.Metadata;
	static string     menuBarMsg     = "Press CTRL+L for help menu";
	static string?    tempMenuBarMsg = null;
	static bool       menuBarError   = false;
	static Stopwatch? tempMsgTime    = null;
	
	static int channelToToggle = 0;
	static int seekPosition    = 1;
	
	static int viewIndex = 0;
	static View[] views = [View.Metadata, View.DSPViewer1, View.DSPViewer2, View.DSPViewer3, View.MemoryViewer, View.Script700Viewer];
	
	static bool heatMapEnabled    = false;
	static bool cyclesInSpcClocks = false;
	
	static Transfer.Requests requests = Transfer.Requests.CycleCountOnly;
	
	static long curCycle = 0;
	
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
				uiState = value;
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
									var curIndex = getSnapshotIndex(curCycle);
				
									foreach (var idx in indexes) {
										if (idx > 0 && idx >= curIndex) {
											seekBarSnapshots.Remove(idx);
										}
									}
								
									RunAheadEmu = PrimaryEmu.SaveState();
								}
							}
							
							DisableScript700 = false;
							UI_State         = State.Normal;
							state            = State.Normal;
							
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
									var curIndex = getSnapshotIndex(curCycle);
				
									foreach (var idx in indexes) {
										if (idx > 0 && idx >= curIndex) {
											seekBarSnapshots.Remove(idx);
										}
									}
								
									RunAheadEmu = PrimaryEmu.SaveState();
								}
							}
							
							DisableScript700 = true;
							UI_State         = State.Normal;
							state            = State.Normal;
							
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
		
		if (state is not State.NonFatalError and not State.Init) {
			action = KeyBindings.GetAction();
		
			var framesSinceLastDisplay = Math.Max(1, frame - prevFrame);
		
			for (var _ = 0; _ < framesSinceLastDisplay; _++) {
				PrevStartAddr4 = PrevStartAddr3;
				PrevStartAddr3 = PrevStartAddr2;
				PrevStartAddr2 = PrevStartAddr1;
				PrevStartAddr1 = StartAddr;
			}
		
			if (action is not null) {
				doAction(action!.Value);
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
			
			case View.MemoryViewer: {
				showMemoryViewer(buffer!);
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
			
			Display.ClearLine(Display.Height - 3);
			Display.Write(formatTime((int) (curCycle / 32), TimeUnit.Timer2s), 0, Display.Height - 3, Color.Cyan);
			
			var fullTimeInCycles = (long) (PrimaryEmu.SpcMetadata.LengthInSeconds ?? 60 * 12)  * 2048000;
			var barLength        = Display.Width - 1 - 14;
			
			var cursorPos  = (int) ((double)      curCycle / fullTimeInCycles * barLength);
			var cursorPos2 = (int) ((double) RunAheadCycle / fullTimeInCycles * barLength);
			
			cursorPos  = Math  .Min(cursorPos,                          Display.Width - 1);
			cursorPos2 = Math.Clamp(cursorPos2, Math.Max(1, cursorPos), Display.Width);
			
			Display.Write(new string('=', cursorPos) + '|',                         14,                 Display.Height - 3, Color    .Cyan);
			Display.Write(new string('=', Math.Max(0, cursorPos2 - cursorPos - 1)), 14 + cursorPos + 1, Display.Height - 3, Color.DarkGrey);
		}
		
		Display.Write("[", 13,                Display.Height - 3, Color.Cyan);
		Display.Write("]", Display.Width - 1, Display.Height - 3, Color.Cyan);
		
		// Display Menu Bar
		var barColor = menuBarError ? Color.BGRed : Color.BGBlue;
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
			
			Display.DrawOutline(20, 8, Display.Width - 40, Display.Height - 18, Color.Yellow);
			Display.ClearBox(Display.Width - 42, Display.Height - 20, 21, 9);
			
			Display.Write(" Error ", Display.Width / 2 - 3, 8, Color.Yellow);
			
			var errorText = Display.WordWrap("A non-fatal error has occurred during the processing of Script700 code.", Display.Width - 36, 3);
			Display.WriteBox(errorText, 23, 10, Color.Yellow);
			Display.Y++;
			Display.WriteBox([
				"Error reason: ",
				"    Script700 execution timed out",
				""
			], col: Color.Yellow);
			
			var explainText = Display.WordWrap(
				"This error can occur from either an infinite loop, or the execution of a long stretch of non-yielding Script700 code " +
				"(i.e. no `w` command)",
				Display.Width - 42 - 4, 3
			);
			Display.Write("Explanation: ", col: Color.Yellow);
			Display.Y++;
			Display.WriteBox(explainText, x_: 22 + 4, col: Color.Yellow);
			Display.Y += 2;
			
			var displayY = Display.Y;
			
			for (var i = 0; i < 3; i++) {
				var region = menuRegions[i];
				var option = menuOptions[i];
				
				Display.ClearBox(region.W + 2, region.H, region.X - 1, region.Y + displayY, col: selectedItem == i ? Color.BGBlue : Color.Cyan);
				Display.WriteBox(option, region.X, region.Y + displayY, col: selectedItem == i ? Color.BGBlue : Color.Cyan);
			}
		}
		
		if (state == State.Init) {
			UI_State = State.Normal;
		}
		
		Console.Write(Display.Flush());
	}
	
	static void doAction(KeyBindings.Action action) {
		switch (action) {
			case KeyBindings.Action.ExitCurrentMenu: {
				changeCurrentView(realView, setAsRealView: false);
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
				viewIndex++;
				viewIndex %= views.Length;
				changeCurrentView(views[viewIndex], setAsRealView: false);
				break;
			}
			
			case KeyBindings.Action.NavPrevView: {
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
			
			case KeyBindings.Action.ScrollRowUp: {
				if (currentView == View.MemoryViewer) {
					if (StartAddr >= 0x10) {
						StartAddr -= 0x10;
						requestEmuData(requests);
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
				break;
			}
			
			case KeyBindings.Action.ScrollPageUp: {
				if (currentView == View.MemoryViewer) {
					if (StartAddr >= 0x100) {
						StartAddr -= 0x100;
						requestEmuData(requests);
					}
					else if (StartAddr > 0) {
						StartAddr = 0;
						requestEmuData(requests);
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
				break;
			}
			
			case KeyBindings.Action.ScrollStart: {
				if (currentView == View.MemoryViewer) {
					if (StartAddr > 0) {
						StartAddr = 0;
						requestEmuData(requests);
					}
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
			
			case KeyBindings.Action.TogglePause: {
				TogglePause();
				break;
			}
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
	
	static void seek(int offsetInSeconds) {
		var targetCycle = Math.Max(0, curCycle + offsetInSeconds * 2048000);
		var targetSnapshotIndex = getSnapshotIndex(targetCycle);
		
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
		var targetSnapshotIndex = getSnapshotIndex(targetCycle);
		
		loadSnapshot(targetSnapshotIndex);
	}
	
	static void loadSnapshot(int targetSnapshotIndex) {
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
		}
	}
	
	static void changeCurrentView(View newView, bool setAsRealView = true) {
		if (currentView != nextView) return;
		nextView = newView;
		
		// Make requests
		switch (nextView) {
			case View.MemoryViewer: {
				requestEmuData(Transfer.Requests.SMP_Bus);
				break;
			}
			
			case View.DSPViewer1: {
				requestEmuData(Transfer.Requests.DSP_RegisterMem | Transfer.Requests.DSP_1);
				break;
			}
			
			case View.DSPViewer2: {
				requestEmuData(Transfer.Requests.DSP_RegisterMem | Transfer.Requests.DSP_2);
				break;
			}
			
			case View.DSPViewer3: {
				requestEmuData(Transfer.Requests.DSP_3);
				break;
			}
			
			case View.Script700Viewer: {
				requestEmuData(Transfer.Requests.Script700 | Transfer.Requests.SMP_State);
				break;
			}
			
			default: {
				requestEmuData(Transfer.Requests.CycleCountOnly);
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
	
	static void setTempStatusMsg(StatusMSG msg, bool error = false) {
		tempMenuBarMsg = statusMsg(msg);
		tempMsgTime    = new();
		menuBarError   = error;
		
		tempMsgTime.Start();
	}
	
	static string statusMsg(StatusMSG msg) {
		return msg switch {
			StatusMSG.Default               => "Press CTRL+L for help menu",
			StatusMSG.HeatMapOff            => "Heat map disabled",
			StatusMSG.HeatMapOn             => "Heat map enabled",
			StatusMSG.ChannelX_Disabled     => $"Channel {channelToToggle} disabled      {showActiveChannels()}",
			StatusMSG.ChannelX_Enabled      => $"Channel {channelToToggle} enabled       {showActiveChannels()}",
			StatusMSG.MainChannelX_Disabled => $"Main channel {channelToToggle} disabled {showActiveChannels()}",
			StatusMSG.MainChannelX_Enabled  => $"Main channel {channelToToggle} enabled  {showActiveChannels()}",
			StatusMSG.EchoChannelX_Disabled => $"Echo channel {channelToToggle} disabled {showActiveChannels()}",
			StatusMSG.EchoChannelX_Enabled  => $"Echo channel {channelToToggle} enabled  {showActiveChannels()}",
			StatusMSG.AllChannelsEnabled    => $"All channels enabled    {showActiveChannels()}",
			StatusMSG.SeekFwd               => $"Seek +5 seconds",
			StatusMSG.SeekBack              => $"Seek -5 seconds",
			StatusMSG.SeekFwdFar            => $"Seek +30 seconds",
			StatusMSG.SeekBackFar           => $"Seek -30 seconds",
			StatusMSG.SeekPos               => $"Seek to position {seekPosition}",
			StatusMSG.Script700_Error       => $"Script700 error occurred",
			StatusMSG.Continue              => $"Resuming...",
			_ => throw new NotImplementedException()
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
	
	enum BusSize {
		Bit8, Bit16, Bit24, Bit32, Bit64
	}
	
	static void memDisplayRows(BusSize addrBusSize,
	                           int startRow,
	                           int endRow,
	                           byte[] data,
	                           Color?[]? colorData = null,
	                           bool useHeatMap = false)
	{
		Display.X = 0;
		Display.Y = 0;
		
		for (var i = startRow; i <= endRow; i++) {
			var startAddr = (uint) i * 16;
			switch (addrBusSize) {
				case BusSize.Bit8: {
					Display.Write($"{startAddr:X2} | ");
					break;
				}
				case BusSize.Bit16: {
					Display.Write($"{startAddr:X4} | ");
					break;
				}
				case BusSize.Bit24: {
					Display.Write($"{startAddr:X6} | ");
					break;
				}
				case BusSize.Bit32: {
					Display.Write($"{startAddr:X8} | ");
					break;
				}
			}
			
			for (var c = 0; c < 16; c++) {
				var idx = (i - startRow) * 16 + c;
				Display.Write($"{data[idx]:X2} ", col: colorData?[idx]);
			}
			Display.Write("| ");
			
			if (useHeatMap) {
				for (var c = 0; c < 16; c++) {
					var idx = (i - startRow) * 16 + c;
					var val = data[idx];
					var col = heatMapColor(BusSize.Bit8, signed: false, scale: 1.0, val);
					Display.Write("  ", col: col);
				}
			}
			else {
				for (var c = 0; c < 16; c++) {
					var idx = (i - startRow) * 16 + c;
					var val = data[idx];
					Display.Write($"{(val is >= 0x20 and <= 0x7E ? (char) val : '.')}", col: colorData?[idx]);
				}
				Display.Write(new string(' ', 16));
			}
			
			Display.Write("\n");
		}
	}
	
	static byte[] heatValues = [0x50, 0x68, 0xA0, 0xE0, 0xA0, 0xA0, 0xA0, 0xA0];
	
	static void showBar(double value, int displayHeight, int x, int y) {
		var color  = Color.CRed;
		var height = (int) Math.Round(value * displayHeight * 8);
		
		var refColor = heatMapColor(BusSize.Bit8, signed: false, scale: 1, 0xFF);
		
		x -= 1;
		
		if (height > 0) {
			for (var i = 0; i < displayHeight; i++) {
				Color bgcol;
				Color fgCol;
				
				if (i >= displayHeight / 2) {
					var interp = (double) (displayHeight - i) / (displayHeight / 2 + 1);
					interp = Math.Pow(interp, 2);
					
					bgcol = heatMapColor(BusSize.Bit8, signed: true,  scale: 1, heatValues[i]);
					fgCol = new(bgcol.Red, bgcol.Green, bgcol.Blue, bg: false);
					fgCol = Color.FromLCH(
						refColor.L * interp + fgCol.L * (1 - interp),
						refColor.C * interp + fgCol.C * (1 - interp),
						refColor.H * interp + fgCol.H * (1 - interp)
					);
				}
				else {
					bgcol = heatMapColor(BusSize.Bit8, signed: false, scale: 1, heatValues[i]);
					fgCol = new(bgcol.Red, bgcol.Green, bgcol.Blue, bg: false);
				}
				
				#if LINUX // By default, Windows terminal emulators do not seem to support unicode char display - make bars more coarse for those
					if (i < height / 8) {
						Display.Write("███", x, y - i - 1, fgCol);
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
						Display.Write(barString, x, y - i - 1, fgCol);
					}
				#else
					if (i < height / 8) {
						Display.Write("███", x, y - i - 1, fgCol);
					}
					else if (i == height / 8 && height % 8 >= 4) {
						var barString = (height % 8) switch {
							4 => "▄▄▄",
							5 => "▄▄▄",
							6 => "▄▄▄",
							7 => "▄▄▄",
							_ => throw new UnreachableException()
						};
						Display.Write(barString, x, y - i - 1, fgCol);
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
	
	static Color heatMapColor(BusSize dataSize, bool signed, double scale, long value) {
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
		
		Color zero = Color.FromLCH(0.1, 70, (280 - 360) * 2 * Math.PI / 360); //new(0, 31, 82);
		
		Color maxUns = Color.FromLCH(85.5, 47, 4.8     * Math.PI /   3); //new(242, 222, 255);
		Color maxPos = Color.FromLCH(94.0, 97, 125 * 2 * Math.PI / 360); //new(225, 249, 122);
		Color maxNeg = Color.FromLCH(89.0, 87, 2.0     * Math.PI /   6); //new(255, 201,  93);
		
		double L;
		double C;
		double H;
		
		double rm;
		double gm;
		double bm;
		
		if (signed) {
			if (interp >= 0) {
				var h = maxPos.H;
				
				if (maxPos.H - zero.H > Math.PI) {
					h -= 2 * Math.PI;
				}
				else if (zero.H - maxPos.H < -Math.PI) {
					h += 2 * Math.PI;
				}
				
				L = maxPos.L *  interp + zero.L * (1 -  interp);
				C = maxPos.C *  interp + zero.C * (1 -  interp);
				H = h        *  interp + zero.H * (1 -  interp);
				
				rm = 1.0; //1.0 + origInterp * 0.4;
				gm = 1.0; //1.1;
				bm = 1.0; //1.0 + origInterp * 0.45;
			}
			else {
				var h = maxNeg.H;
				
				if (maxNeg.H - zero.H > Math.PI) {
					h -= 2 * Math.PI;
				}
				else if (zero.H - maxNeg.H < -Math.PI) {
					h += 2 * Math.PI;
				}
				
				L = maxNeg.L * -interp + zero.L * (1 - -interp);
				C = maxNeg.C * -interp + zero.C * (1 - -interp);
				H = h        * -interp + zero.H * (1 - -interp);
				
				rm = 1.0; //1.0 + -origInterp * 0.4;
				gm = 1.0; //1.1;
				bm = 1.0; //1.0 + -origInterp * 0.45;
			}
		}
		else {
			var h = maxUns.H;
				
			if (maxUns.H - zero.H > Math.PI) {
				h -= 2 * Math.PI;
			}
			else if (zero.H - maxUns.H < -Math.PI) {
				h += 2 * Math.PI;
			}
			
			L = maxUns.L * interp + zero.L * (1 - interp);
			C = maxUns.C * interp + zero.C * (1 - interp);
			H = h        * interp + zero.H * (1 - interp);
				
			rm = 1.0; //1.0 + origInterp * 0.3;
			gm = 1.0; //1.1;
			bm = 1.0; //1.1; //1.0 + origInterp * 0.35;
		}
		
		var col = Color.FromLCH(L, C, H);
		
		return new(
			(byte) Math.Clamp(col.Red   * rm, 0, 255),
			(byte) Math.Clamp(col.Green * gm, 0, 255),
			(byte) Math.Clamp(col.Blue  * bm, 0, 255),
			bg: true
		);
	}
	
	static void softFadeHeatmap(byte[] dataBuffer, byte[] progBuffer) {
		const int FadeStep = 72;
		
		// Smooth transition to avoid rapid flashing
		for (var i = 0; i < progBuffer.Length; i++) {
			var progVal = progBuffer[i];
			var target  = dataBuffer[i];
				
			if (target > progVal + FadeStep) {
				progBuffer[i] += FadeStep;
			}
			else if (target < progVal - FadeStep) {
				progBuffer[i] -= FadeStep;
			}
			else if (target != progVal) {
				progBuffer[i] = target;
			}
		}
	}
	
	static void requestEmuData(Transfer.Requests reqs) {
		requests = reqs;
		Transfer.RequestEmuData(reqs, StartAddr, ScrollAreaRows * 0x10);
	}
}