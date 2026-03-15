namespace SpcProgram;

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
	
	static void showTraceLogger(EmuDataBuffer buffer) {
		Display.UseBufferBlending = false;
		
		if (UI_State == State.Paused) {
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
			
			refreshSignal = 0;
			
			var spc = buffer.SPC_State!;
			
			AnsiColor? highlight = new(AnsiColor.Code.Magenta, isBG: true);
			
			Display.ScrollTop = Math.Max(0, InstructionsSinceTrace - ScrollAreaRows - ScrollOffset);
			
			var x = 0;
			var y = InstructionsSinceTrace - 1;
			
			if (y >= Display.ColorBuffer.Count) {
				Display.AddBufferRow();
			}
			
			unhighlight(y - 1);
			Display.ClearLine(y, col: highlight, writeToScrollBuf: true);
			
			var instructionBytes = SPC.GetInstructionLength(spc.ExecData[0]);
			var pc = spc.InstrStartPC;
			
			Display.Write(
				$"{pc:X4} |  {SPC.DecodeInstruction(pc, spc.ExecData[0], spc.ExecData[1..])}",
				x, y,
				col: highlight,
				writeToScrollBuf: true
			);
			
			var byteString = string.Join(' ', spc.ExecData.Take(instructionBytes).Select(x => $"{x:X2}"));
			
			x = 28;
			Display.Write($"{byteString}", x, y, col: highlight, writeToScrollBuf: true);
			
			var flagsString = "nvpbhizc".ToArray();
			flagsString = Enumerable.Range(0, 8).Select(b => spc.PSW.GetBit(b) ? flagsString[b].ToUpper() : flagsString[b]).ToArray();
				
			Display.Write(
				$"A:{spc.A:X2} X:{spc.X:X2} Y:{spc.Y:X2} SP:{spc.SP:X2} {string.Join("", flagsString)}",
				Display.Width - 31, y,
				col: highlight,
				writeToScrollBuf: true
			);
			
			NextInstructionsSinceTrace = InstructionsSinceTrace + 1;
		}
	}
	
	static void unhighlight(int lineNumber) {
		lineNumber %= Display.MaxBufferRows;
		
		if (lineNumber < 0 || lineNumber >= Display.ColorBuffer.Count) {
			return;
		}
		
		var line = Display.ColorBuffer[lineNumber];
		for (var i = 0; i < line.Length; i++) {
			line[i] = null;
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
}