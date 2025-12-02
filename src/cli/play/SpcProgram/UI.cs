using System.Diagnostics;

namespace SpcProgram;

using System.Text;

using Apollo;
using Jimbl;

public static partial class CliMain {
	const int ScrollAreaRows = 0x1E;
	
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
	}
	
	static View       realView       = View.Metadata;
	static View       currentView    = View.Metadata;
	static View       nextView       = View.Metadata;
	static string     menuBarMsg     = "Press CTRL+L for help menu";
	static string?    tempMenuBarMsg = null;
	static Stopwatch? tempMsgTime    = null;
	
	static int viewIndex = 0;
	static View[] views = [View.Metadata, View.DSPViewer1, View.DSPViewer2, View.MemoryViewer];
	
	static bool heatMapEnabled = false;
	
	static Transfer.Requests requests = Transfer.Requests.CycleCountOnly;
	
	static void handleUI(EmuDataBuffer? buffer) {
		var action = KeyBindings.GetAction();
		
		if (action is not null) {
			doAction(action!.Value);
		}
		
		if (nextView != currentView) {
			if (buffer?.ExpectData(requests) ?? false) {
				commitCurrentView();
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
			
			default: {
				break;
			}
		}
		
		// Display Seek Bar
		if (buffer is not null) {
			Display.ClearLine(Display.Height - 3);
			Display.Write(formatTime((int) (buffer.DSPCycle / 32), TimeUnit.Timer2s), 0, Display.Height - 3, Color.Cyan);
			
			var fullTimeInCycles = (long) (PrimaryEmu.SpcMetadata.LengthInSeconds ?? 600) * 2048000;
			var barLength = Display.Width - 1 - 14;
			
			var cursorPos = (int) ((double) buffer.DSPCycle / fullTimeInCycles * barLength);
			Display.Write(new string('=', cursorPos) + '|', 14, Display.Height - 3, Color.Cyan);
		}
		
		Display.Write("[", 13,                Display.Height - 3, Color.Cyan);
		Display.Write("]", Display.Width - 1, Display.Height - 3, Color.Cyan);
		
		// Display Menu Bar
		Display.ClearLine(Display.Height - 1, Color.BGBlue);
		
		if (tempMsgTime is not null && tempMsgTime.ElapsedMilliseconds >= 3000) {
			resetMenuBar();
		}
		else if (tempMenuBarMsg is not null) {
			Display.Write(tempMenuBarMsg, 0, Display.Height - 1, Color.BGBlue);
		}
		else {
			Display.Write(menuBarMsg,     0, Display.Height - 1, Color.BGBlue);
		}
		
		if (buffer is not null) {
			var cycleCounter = $"DSP Cycle: {buffer.DSPCycle}";
			Display.Write(cycleCounter, Display.Width - 1 - cycleCounter.Length, Display.Height - 1, Color.BGBlue);
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
			
			case KeyBindings.Action.ScrollRowUp: {
				if (currentView == View.MemoryViewer) {
					if (startAddr >= 0x10) {
						startAddr -= 0x10;
						requestEmuData(requests);
					}
				}
				break;
			}
			
			case KeyBindings.Action.ScrollRowDown: {
				if (currentView == View.MemoryViewer) {
					if (startAddr <= 0x1_0000 - ScrollAreaRows * 0x10 - 0x10) {
						startAddr += 0x10;
						requestEmuData(requests);
					}
				}
				break;
			}
			
			case KeyBindings.Action.ScrollPageUp: {
				if (currentView == View.MemoryViewer) {
					if (startAddr >= 0x100) {
						startAddr -= 0x100;
						requestEmuData(requests);
					}
					else if (startAddr > 0) {
						startAddr = 0;
						requestEmuData(requests);
					}
				}
				break;
			}
			
			case KeyBindings.Action.ScrollPageDown: {
				if (currentView == View.MemoryViewer) {
					if (startAddr <= 0xFF00 - ScrollAreaRows * 0x10) {
						startAddr += 0x100;
					}
					else if (startAddr < 0x1_0000 - ScrollAreaRows * 0x10) {
						startAddr = 0x1_0000 - ScrollAreaRows * 0x10;
					}
					requestEmuData(requests);
				}
				break;
			}
			
			case KeyBindings.Action.ScrollStart: {
				if (currentView == View.MemoryViewer) {
					if (startAddr > 0) {
						startAddr = 0;
						requestEmuData(requests);
					}
				}
				break;
			}
			
			case KeyBindings.Action.ScrollEnd: {
				if (currentView == View.MemoryViewer) {
					if (startAddr < 0x1_0000 - ScrollAreaRows * 0x10) {
						startAddr = 0x1_0000 - ScrollAreaRows * 0x10;
						requestEmuData(requests);
					}
				}
				break;
			}
			
			case KeyBindings.Action.ToggleHeatMap: {
				if (currentView is View.MemoryViewer or View.DSPViewer1 or View.DSPViewer2) {
					heatMapEnabled = !heatMapEnabled;
					
					if (heatMapEnabled) {
						setTempStatusMsg(StatusMSG.HeatMapOn);
					}
					else {
						setTempStatusMsg(StatusMSG.HeatMapOff);
					}
				}
				break;
			}
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
	
	static void setStatusMsg(StatusMSG msg) {
		menuBarMsg = statusMsg(msg);
	}
	
	static void setTempStatusMsg(StatusMSG msg) {
		tempMenuBarMsg = statusMsg(msg);
		tempMsgTime    = new();
		
		tempMsgTime.Start();
	}
	
	static string statusMsg(StatusMSG msg) {
		return msg switch {
			StatusMSG.Default    => "Press CTRL+L for help menu",
			StatusMSG.HeatMapOff => "Heat map disabled",
			StatusMSG.HeatMapOn  => "Heat map enabled",
			_ => throw new NotImplementedException()
		};
	}
	
	static void resetMenuBar() {
		tempMenuBarMsg = null;
		tempMsgTime    = null;
	}
	
	enum AddressBusSize {
		Bit8, Bit16, Bit24, Bit32
	}
	
	static void memDisplayRows(AddressBusSize busSize,
	                           int startRow,
	                           int endRow,
	                           byte[] data,
	                           byte[]? dataForHeatmap = null,
	                           Color?[]? colorData = null,
	                           bool useHeatMap = false)
	{
		Display.X = 0;
		Display.Y = 0;
		
		for (var i = startRow; i <= endRow; i++) {
			var startAddr = (uint) i * 16;
			switch (busSize) {
				case AddressBusSize.Bit8: {
					Display.Write($"{startAddr:X2} | ");
					break;
				}
				case AddressBusSize.Bit16: {
					Display.Write($"{startAddr:X4} | ");
					break;
				}
				case AddressBusSize.Bit24: {
					Display.Write($"{startAddr:X6} | ");
					break;
				}
				case AddressBusSize.Bit32: {
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
					var val = (byte) ((dataForHeatmap ?? data)[idx] * 5 / 9 + 40);
					Display.Write("  ", col: new(val, val, val, bg: true));
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
		Transfer.RequestEmuData(reqs, startAddr, ScrollAreaRows * 0x10);
	}
}