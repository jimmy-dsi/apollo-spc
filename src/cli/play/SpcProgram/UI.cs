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
	
	static View   realView    = View.Metadata;
	static View   currentView = View.Metadata;
	static View   nextView    = View.Metadata;
	static string menuBarMsg  = "Press CTRL+H for help menu";
	
	static int viewIndex = 0;
	static View[] views = [View.Metadata, View.MemoryViewer];
	
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
			
			default: {
				break;
			}
		}
		
		// Display Seek Bar
		if (buffer is not null) {
			Display.ClearLine(Display.Height - 2);
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
		}
	}
	
	static void changeCurrentView(View newView, bool setAsRealView = true) {
		nextView = newView;
		
		// Make requests
		switch (nextView) {
			case View.MemoryViewer: {
				requestEmuData(Transfer.Requests.SMP_Bus);
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
	
	enum AddressBusSize {
		Bit8, Bit16, Bit24, Bit32
	}
	
	static string[] memDisplayRows(AddressBusSize busSize, int startRow, int endRow, byte[] data, bool useHeatMap = false) {
		List<string> rows = new();
		
		for (var i = startRow; i <= endRow; i++) {
			StringBuilder sb = new();
			
			var startAddr = (uint) i * 16;
			switch (busSize) {
				case AddressBusSize.Bit8: {
					sb.Append($"{startAddr:X2} | ");
					break;
				}
				case AddressBusSize.Bit16: {
					sb.Append($"{startAddr:X4} | ");
					break;
				}
				case AddressBusSize.Bit24: {
					sb.Append($"{startAddr:X6} | ");
					break;
				}
				case AddressBusSize.Bit32: {
					sb.Append($"{startAddr:X8} | ");
					break;
				}
			}
			
			for (var c = 0; c < 16; c++) {
				sb.Append($"{data[(i - startRow) * 16 + c]:X2} ");
			}
			sb.Append("| ");
			
			if (useHeatMap) {
				// TODO
			}
			else {
				for (var c = 0; c < 16; c++) {
					var val = data[(i - startRow) * 16 + c];
					sb.Append($"{(val is >= 0x20 and <= 0x7E ? (char) val : '.')}");
				}
			}
			
			rows.Add(sb.ToString());
		}
		
		return rows.ToArray();
	}
	
	static void requestEmuData(Transfer.Requests reqs) {
		requests = reqs;
		Transfer.RequestEmuData(reqs, startAddr, ScrollAreaRows * 0x10);
	}
}