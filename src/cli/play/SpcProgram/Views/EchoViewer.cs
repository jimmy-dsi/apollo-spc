namespace SpcProgram;

using Jimbl.Graphics;

using Apollo;
using Jimbl;

public static partial class CliMain {
	enum EchoView {
		All, LeftOnly, RightOnly, Mixed
	}
	
	static EchoView currentEchoView = EchoView.All;
	
	static void showEchoViewer(EmuDataBuffer buffer) {
		Display.UseBlending = false;
		
		var echoStart    = buffer.DSP_DebugState!.EchoPage << 8;
		var bufferLength = buffer.DSP_DebugState!.EchoLength / 4;
		
		if (bufferLength == 0) {
			bufferLength = 1;
		}
		
		List<Int16>  leftBuf = new();
		List<Int16> rightBuf = new();
		
		for (var i = 0; i < bufferLength; i++) {
			var addr = (UInt16) (echoStart + i * 4);
			
			var leftLo  = buffer.ARAM_Data![addr];
			var leftHi  = buffer.ARAM_Data![addr + 1];
			
			var rightLo = buffer.ARAM_Data![addr + 2];
			var rightHi = buffer.ARAM_Data![addr + 3];
			
			var left  = (Int16) ( leftLo |  leftHi << 8);
			var right = (Int16) (rightLo | rightHi << 8);
			
			leftBuf .Add( left);
			rightBuf.Add(right);
		}
		
		var width = 131;
		
		var echoEnd = (UInt16) (echoStart + bufferLength * 4 - 1);
		
		var cursorPos = buffer.DSP_DebugState!.EchoOffset / 4;
		
		switch (currentEchoView) {
			case EchoView.All: {
				Display.Write("Echo left",  0, 0);
				Display.Write("Echo right", 0, 13);
			
				displayWaveform(leftBuf.ToArray(),  0, 1,  width, 12, cursorPos);
				displayWaveform(rightBuf.ToArray(), 0, 14, width, 12, cursorPos);
				
				break;
			}
			
			case EchoView.LeftOnly: {
				Display.Write("Echo left", 0, 0);
				displayWaveform(leftBuf.ToArray(), 0, 1, width, 25, cursorPos);
				break;
			}
			
			case EchoView.RightOnly: {
				Display.Write("Echo right", 0, 0);
				displayWaveform(rightBuf.ToArray(), 0, 1, width, 25, cursorPos);
				break;
			}
			
			case EchoView.Mixed: {
				Display.Write("Echo left/right mixed", 0, 0);
				displayWaveform(leftBuf.ToArray(), rightBuf.ToArray(), 0, 1, width, 25, cursorPos);
				break;
			}
		}
		
		Display.Write($"{echoStart:X4}", 0,   26);
		Display.Write($"{echoEnd  :X4}", 126, 26);
	}
	
	public static AnsiColor? WaveInsideColor = null;
	
	static void displayWaveform(Int16[] buf, int x, int y, int width, int height, int cursorPos) {
		displayWaveform(buf, null, x, y, width, height, cursorPos);
	}
	
	static void displayWaveform(Int16[] input, Int16[]? input_2, int x, int y, int width, int height, int cursor, bool writeToScrollBuf = false) {
		if (WaveInsideColor is null) {
			WaveInsideColor = new(Color.FromLCh(10, 30, 280), isBG: true);
		}
		
		var eqInsideRGB = WaveInsideColor.BackgroundRGB!;
		
		for (var yy = y; yy < y + height; yy++) {
			Display.ClearLine(yy, new(eqInsideRGB, isBG: true), writeToScrollBuf);
		}
		
		var waveCanvas = Analysis.DisplayWaveform(input, input_2, width, height, cursorIndex: cursor);
		for (var yy = 0; yy < height; yy++) {
			for (var xx = 0; xx < width; xx++) {
				var c = waveCanvas[yy][xx];
				if (c is not null) {
					Display.Write($"{c.Value.Item1}", x + xx, y + yy, c.Value.Item2, writeToScrollBuf);
				}
			}
		}
	}
}