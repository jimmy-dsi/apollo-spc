namespace SpcProgram;

using System.Text;
using System.Diagnostics;

using Jimbl;
using Jimbl.Graphics;

using Apollo;

public static partial class CliMain {
	public static int InstructionsSinceTrace = 0;
	public static int NextInstructionsSinceTrace = 0;
	
	public static int ScrollOffset = 0;
	
	static long lastInstructionCycle = -1;
	static int  refreshSignal = 0;
	static bool initScrollAdjust = false;
	
	static View? tracePrevView      = null;
	static int   tracePrevViewIndex = 0;
	
	static long lastEmuInstrStep  = 0;
	static long lastEmuInstrCycle = -1;
	
	public static UIElement[] ASMViewerUIElements = [
		new(UIElement.Type.ScrollableArea,
		    KeyBindings.Action.ScrollWheelUp, KeyBindings.Action.ScrollWheelDown,
		    0, 0, Display.Width, Display.Height - 4, highlightOnHover: false),
	];
	
	static void showTraceLogger(EmuDataBuffer buffer, EmuDataBuffer[]? prevBuffers = null, int recurseLevel = 1) {
		Display.UseBufferBlending = false;
		
		if (UI_StateIsPaused) {
			if (InstructionsSinceTrace == 0) {
				Display.ClearBox(Display.Width, 2, 0, 0, writeToScrollBuf: true);
				InstructionsSinceTrace = 1;
			}
		}
		else {
			InstructionsSinceTrace = 0;
		}
		
		if (InstructionsSinceTrace == 0) {
			lastInstructionCycle = buffer.DSPCycle;
			
			Display.ClearBox(Display.Width, ScrollAreaRows, 0, 0, writeToScrollBuf: true);
			Display.ScrollTop = 0;
			
			if (Display.ColorBuffer.Count > ScrollAreaRows) {
				Display. CharBuffer.RemoveRange(ScrollAreaRows, Math.Min(Display. CharBuffer.Count, Display.MaxBufferRows) - ScrollAreaRows);
				Display.ColorBuffer.RemoveRange(ScrollAreaRows, Math.Min(Display.ColorBuffer.Count, Display.MaxBufferRows) - ScrollAreaRows);
			}
			
			Display.Write("SPC700 trace log will appear here whenever execution is paused.", 0, 0, writeToScrollBuf: true);
			Display.Write("There is nothing to display at this moment.", 0, 1, writeToScrollBuf: true);
		}
		else {
			if (ScrollOffset != 0) {
				Display.ScrollTop = Math.Max(0, InstructionsSinceTrace - ScrollAreaRows - ScrollOffset);
				initScrollAdjust  = false;
			}
			else if (!initScrollAdjust) {
				Display.ScrollTop = Math.Max(0, InstructionsSinceTrace - ScrollAreaRows);
				initScrollAdjust  = true;
			}
			
			if (InstructionsSinceTrace > 1 && refreshSignal < 2) {
				if (refreshSignal > 0) {
					refreshSignal++;
				}
				return;
			}
			
			if (InstructionsSinceTrace > 1 && lastEmuInstrStep == buffer.InstrStep) {
				return;
			}
			
			if (InstructionsSinceTrace == 1 && lastEmuInstrStep == buffer.InstrStep) {
				lastEmuInstrStep = buffer.InstrStep - 1;
			}
			
			// Recurse once if we have skipped one instruction
			if (buffer.InstrStep - lastEmuInstrStep > 1 && recurseLevel > 0) {
				EmuDataBuffer? backBuffer = null;
				prevBuffers ??= EmuDataBuffer.GenBufferQueue;
				
				var earliestAfterThis = buffer.InstrStep;
				
				foreach (var buf in prevBuffers) {
					if (buf.InstrStep < earliestAfterThis && buf.InstrStep > lastEmuInstrStep) {
						earliestAfterThis = buf.InstrStep;
						backBuffer = buf;
					}
				}
				
				if (backBuffer is not null) {
					showTraceLogger(backBuffer, prevBuffers, recurseLevel: (int) (buffer.InstrStep - lastEmuInstrStep));
					InstructionsSinceTrace++;
				}
			}
			
			refreshSignal = 0;
			
			var spc = buffer.SPC_State!;
			
			var highlightCode = AnsiColor.Code.Magenta;
			
			AnsiColor? highlight     = new(highlightCode, isBG: true);
			AnsiColor? highlightName = new(highlightCode, isBG: true, isBold: true);
			
			Display.ScrollTop = Math.Max(0, InstructionsSinceTrace - ScrollAreaRows - ScrollOffset);
			
			var x = 0;
			var y = InstructionsSinceTrace - 1;
			
			while (y >= Display.VirtualSize) {
				Display.AddBufferRow();
			}
			
			unhighlight(y - 1);
			
			if (y > 0) {
				var logX = 38;
			
				UInt16? prevReadAddress = null;
				byte?   prevReadData    = null;
				
				var smpState = buffer.SMP_State!;
				
				foreach (var log in logsSinceLastExec(buffer)) {
					switch (log.Type) {
						case SMP.MemAccessLog.LogType.Read: {
							AnsiColor col;
							
							if (log.Address is >= 0x00F0 and <= 0x00FC) {
								col = new(AnsiColor.Code.Yellow);
							}
							else if (log.Address is >= 0x00FD and <= 0x00FF) {
								col = new(250, 125, 25);
							}
							else if (log.Address >= 0xFFC0 && smpState.UseBootROM) {
								col = new(AnsiColor.Code.BrightGreen);
							}
							else if (smpState.RAMDisable) {
								col = new(AnsiColor.Code.Red);
							}
							else {
								col = new(AnsiColor.Code.Green);
							}
							
							Display.Write($"[{log.Address:X4}]={log.ReadData:X2} ", logX, y - 1, col: col, writeToScrollBuf: true);
						
							prevReadAddress = log.Address;
							prevReadData    = log.ReadData;
						
							logX += 10;
							break;
						}
					
						case SMP.MemAccessLog.LogType.Write: {
							AnsiColor col;
							
							if (log.Address is >= 0x00F0 and <= 0x00FF) {
								col = new(AnsiColor.Code.BrightMagenta);
							}
							else if (smpState.RAMDisable || !smpState.RAMWriteEnable) {
								col = new(AnsiColor.Code.BrightRed);
							}
							else if (log.Address >= 0xFFC0 && smpState.UseBootROM) {
								col = new(AnsiColor.Code.BrightBlue);
							}
							else {
								col = new(AnsiColor.Code.Cyan);
							}

							var preData = log.PreData;
							if (prevReadAddress == log.Address) {
								preData =  prevReadData!.Value;
								logX    -= 10;
							}
						
							Display.Write($"[{log.Address:X4}]={preData:X2}->{log.PostData:X2} ", logX, y - 1, col: col, writeToScrollBuf: true);
						
							prevReadAddress = null;
							prevReadData    = null;
						
							logX += 14;
							break;
						}
					
						default: {
							throw new UnreachableException();
						}
					}
				}
			}
			
			Display.ClearLine(y, col: highlight, writeToScrollBuf: true);
			
			var pc = spc.InstrStartPC;
			
			if (spc.Mode == SPC.ExecMode.Asleep) {
				spc.ExecData[0] = 0xEF;
			}
			else if (spc.Mode == SPC.ExecMode.Stopped) {
				spc.ExecData[0] = 0xFF;
			}
			
			var instructionBytes = SPC.GetInstructionLength(spc.ExecData[0]);
			var insParts         = SPC.DecodeInstruction(pc, spc.ExecData[0], spc.ExecData[1..]).Split(' ', 2);
			
			var insName  = insParts[0];
			var operands = string.Join(' ', insParts[1..]);
			
			var c = spc.Mode == SPC.ExecMode.Interrupt ? '*' : ' ';
			
			Display.Write($"{pc:X4} │ {c}", x,                      y, col: highlight,     writeToScrollBuf: true);
			Display.Write($"{insName} ",    x + 8,                  y, col: highlightName, writeToScrollBuf: true);
			Display.Write($"{operands}",    x + 9 + insName.Length, y, col: highlight,     writeToScrollBuf: true);
			
			var byteString = string.Join(' ', spc.ExecData.Take(instructionBytes).Select(x => $"{x:X2}"));
			
			x = 28;
			Display.Write($"{byteString}", x, y, col: highlight, writeToScrollBuf: true);
			
			var flagsString = "nvpbhizc".ToArray();
			flagsString = Enumerable.Range(0, 8).Select(b => spc.PSW.GetBit(7 - b) ? flagsString[b].ToUpper() : flagsString[b]).ToArray();
				
			Display.Write(
				$"A:{spc.A:X2} X:{spc.X:X2} Y:{spc.Y:X2} SP:{spc.SP:X2} {string.Join("", flagsString)}",
				Display.Width - 30, y,
				col: highlight,
				writeToScrollBuf: true
			);
			
			lastEmuInstrStep = buffer.InstrStep;
			
			if (Math.Max(0, spc.InstrStartCycle) == lastEmuInstrCycle) {
				return;
			}
			
			lastEmuInstrCycle = spc.InstrStartCycle;
			NextInstructionsSinceTrace = InstructionsSinceTrace + 1;
		}
	}
	
	static HashSet<string> ctrlFlowInstrs = new() {
		"bbc", "bbs", "bcc", "bcs", "beq", "bmi", "bne", "bpl", "bra", "bvc", "bvs",
		"call", "cbne",
		"dbnz",
		"jmp",
		"ret", "reti"
	};
	
	static HashSet<string> vecInstrs = new() {
		"brk",
		"pcall",
		"tcall"
	};
	
	static HashSet<string> stackFlagInstrs = new() {
		"and1",
		"clrc", "clrp", "clrv",
		"di",
		"ei", "eor1",
		"mov1",
		"notc",
		"or1",
		"pop", "push",
		"setc", "setp"
	};
	
	static HashSet<string> nopInstrs = new() {
		"nop",
		"sleep"
	};
	
	static AnsiColor defaultInsColor   = new(AnsiColor.Code.BrightBlue,    isBold: true);
	static AnsiColor ctrlFlowInsColor  = new(AnsiColor.Code.BrightMagenta, isBold: true);
	static AnsiColor vecInsColor       = new(AnsiColor.Code.Yellow,        isBold: true);
	static AnsiColor stackFlagInsColor = new(AnsiColor.Code.Green,         isBold: true);
	static AnsiColor nopInsColor       = new(AnsiColor.Code.DarkGrey,      isBold: true);
	static AnsiColor stopInsColor      = new(AnsiColor.Code.Red,           isBold: true);
	
	static void unhighlight(int lineNumber) {
		lineNumber %= Display.MaxBufferRows;
		
		if (lineNumber < 0 || lineNumber >= Display.ColorBuffer.Count) {
			return;
		}
		
		var chars  = Display .CharBuffer[lineNumber];
		var colors = Display.ColorBuffer[lineNumber];
		
		for (var i = 0; i < colors.Length; i++) {
			colors[i] = null;
		}
		
		var insStart = 8;
		StringBuilder sb = new();
		
		for (var i = insStart; i < chars.Length; i++) {
			if (chars[i] != ' ') {
				sb.Append(chars[i]);
			}
			else {
				break;
			}
		}
		
		var insName = sb.ToString();
		if (ctrlFlowInstrs.Contains(insName)) {
			for (var i = 0; i < insName.Length; i++) {
				colors[insStart + i] = ctrlFlowInsColor;
			}
		}
		else if (vecInstrs.Contains(insName)) {
			for (var i = 0; i < insName.Length; i++) {
				colors[insStart + i] = vecInsColor;
			}
		}
		else if (stackFlagInstrs.Contains(insName)) {
			for (var i = 0; i < insName.Length; i++) {
				colors[insStart + i] = stackFlagInsColor;
			}
		}
		else if (nopInstrs.Contains(insName)) {
			for (var i = 0; i < insName.Length; i++) {
				colors[insStart + i] = nopInsColor;
			}
		}
		else if (insName == "stop") {
			for (var i = 0; i < insName.Length; i++) {
				colors[insStart + i] = stopInsColor;
			}
		}
		else {
			for (var i = 0; i < insName.Length; i++) {
				colors[insStart + i] = defaultInsColor;
			}
		}
		
		for (var i = insStart + insName.Length; i < insStart + 20; i++) {
			var ch = chars[i];
			if (ch is 'a' or 'x' or 'y' or 'p' or 's' or 'w' or 'c') {
				colors[i] = new(AnsiColor.Code.BrightCyan);
			}
			else if (ch is '$' or '#' or >= '0' and <= '9' or >= 'A' and <= 'F') {
				colors[i] = new(AnsiColor.Code.BrightYellow);
			}
			else if (ch is '[' or ']' or '(' or ')') {
				colors[i] = new(AnsiColor.Code.White);
			}
			else {
				colors[i] = new(AnsiColor.Code.Grey);
			}
		}
	}
	
	static void resetTraceLog() {
		var noReset = Display.NoResetBuffer;
		Display.NoResetBuffer = true;
		
		var bufId = Display.CurrentBufferId;
		Display.CurrentBufferId = "trace";
		
		Display.ClearBox(Display.Width, ScrollAreaRows, 0, 0, writeToScrollBuf: true);
		Display.ScrollTop = 0;
			
		if (Display.ColorBuffer.Count > ScrollAreaRows) {
			Display. CharBuffer.RemoveRange(ScrollAreaRows, Display. CharBuffer.Count - ScrollAreaRows);
			Display.ColorBuffer.RemoveRange(ScrollAreaRows, Display.ColorBuffer.Count - ScrollAreaRows);
			Display.SyncVirtualSize();
		}
		
		InstructionsSinceTrace = 1;
		lastInstructionCycle   = -1;
		lastEmuInstrCycle      = -1;
		
		Display.CurrentBufferId = bufId;
		Display.NoResetBuffer   = noReset;
	}
	
	static int getScrollTopOffset() {
		var scrollOffset = Math.Max(0, InstructionsSinceTrace - ScrollAreaRows);
		var count = Display.Buffer["trace"].CharBuffer.Count;
		
		if (InstructionsSinceTrace > count) {
			scrollOffset -= InstructionsSinceTrace - count;
		}
		
		return scrollOffset;
	}
	
	static SMP.MemAccessLog[] logsSinceLastExec(EmuDataBuffer buffer, bool filtered = true, bool inclExec = false) {
		var logs = buffer.SMP_AccessLogs;
		
		List<SMP.MemAccessLog> newLogs = new();
		
		for (var i = logs.Length - 1; i >= 0; i--) {
			var log = logs[i];
			if (log.Type == SMP.MemAccessLog.LogType.Exec) {
				if (inclExec) {
					newLogs.Add(log);
				}
				break;
			}
			
			if (log.Type is SMP.MemAccessLog.LogType.Read or SMP.MemAccessLog.LogType.Write) {
				newLogs.Add(log);
			}
			else if (!filtered && log.Type is not SMP.MemAccessLog.LogType.DummyRead) {
				newLogs.Add(log);
			}
		}
		
		return ((IEnumerable<SMP.MemAccessLog>) newLogs).Reverse().ToArray();
	}
}