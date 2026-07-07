namespace SpcProgram;

using Jimbl.Graphics;

using Apollo;
using Jimbl;

public static partial class CliMain {
	static void showEchoViewer(EmuDataBuffer buffer) {
		Display.UseBlending = false;
		
		var echoStart    = buffer.DSP_State!.EchoStartPage << 8;
		var bufferLength = buffer.DSP_State!.EchoDelay * 0x800 / 4;
		
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
		
		var cursorPos = buffer.DSP_DebugState!.EchoOffset / 4;
			
		displayWaveform(leftBuf.ToArray(),  1, 1,  130, 12, cursorPos);
		displayWaveform(rightBuf.ToArray(), 1, 14, 130, 12, cursorPos);
	}
	
	public static AnsiColor? WaveInsideColor = null;
	
	static void displayWaveform(Int16[] input, int x, int y, int width, int height, int cursor, bool writeToScrollBuf = false) {
		if (WaveInsideColor is null) {
			WaveInsideColor = new(Color.FromLCh(10, 30, 280), isBG: true);
		}
		
		var eqInsideRGB = WaveInsideColor.BackgroundRGB!;
		
		for (var yy = y; yy < y + height; yy++) {
			Display.ClearLine(yy, new(eqInsideRGB, isBG: true), writeToScrollBuf);
		}
		
		var waveCanvas = Analysis.DisplayWaveform(input, width, height, cursorIndex: cursor);
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