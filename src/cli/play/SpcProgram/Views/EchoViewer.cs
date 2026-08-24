namespace SpcProgram;

using Jimbl.Graphics;
using Jimbl.JMath;

using Apollo;
using Jimbl;

public static partial class CliMain {
	enum EchoView {
		All, LeftOnly, RightOnly, Mixed
	}
	
	static EchoView currentEchoView = EchoView.All;
	
	static double[] scaleTable = [
		0.25, 0.5, 1,
		2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16,
		18, 20, 22, 24, 26, 28, 30, 32,
		36, 40, 44, 48, 52, 56, 60
	];
	
	static int fitZoomIndex   = scaleTable.Length - 1;
	static int echoZoomIndex  = fitZoomIndex;
	static bool echoAtFitZoom = true;
	
	static int prevEdl = 0;
	
	static int echoScrollOffset = 0;
	static int echoBufferLength = 0;
	
	static int prevEchoOffsetStart = 0;
	static int prevEchoOffsetEnd   = 0;
	
	static void showEchoViewer(EmuDataBuffer buffer) {
		Display.UseBlending = false;
		var ds = buffer.DSP_DebugState!;
		
		var echoStart    = ds.EchoPage << 8;
		var bufferLength = ds.EchoLength / 4;
		var edl          = ds.EchoLength / 2048;
		
		echoBufferLength = bufferLength;
		
		if (edl == 0) {
			fitZoomIndex = 0;
		}
		else {
			fitZoomIndex = scaleTable.IndexOf(edl * 4.0);
		}
		
		if (echoZoomIndex > fitZoomIndex) {
			echoZoomIndex = fitZoomIndex;
			echoAtFitZoom = true;
		}
		else if (prevEdl != edl && echoAtFitZoom) {
			echoZoomIndex = fitZoomIndex;
		}
		
		echoAtFitZoom = echoZoomIndex == fitZoomIndex;
		
		prevEdl = edl;
		
		var echoSampRatio = scaleTable[echoZoomIndex];
		var echoZoomLevel = bufferLength / (echoSampRatio * 128);
		
		var segmentLength = (int) (bufferLength / echoZoomLevel);
		
		if (bufferLength == 0) {
			bufferLength = 1;
			segmentLength = 1;
		}
		
		var cursorAddr = ds.EchoAddress / 4 * 4;
		if (cursorAddr < echoStart) {
			cursorAddr += 0x1_0000;
		}
		
		var prevSegmentLength = prevEchoOffsetEnd + 1 - prevEchoOffsetStart;
		
		if (segmentLength < prevSegmentLength) {
			var startAddr = cursorAddr - segmentLength * 4 / 2;
			
			var start = (startAddr - echoStart) / 4;
			var end   = start + segmentLength - 1;
			
			if (start < prevEchoOffsetStart) {
				start = prevEchoOffsetStart;
				end   = start + segmentLength - 1;
			}
			else if (end > prevEchoOffsetEnd) {
				end   = Math.Min(bufferLength - 1, prevEchoOffsetEnd);
				start = end - segmentLength + 1;
			}
			
			echoScrollOffset = start;
		}
		else if (segmentLength > prevSegmentLength) {
			var prevCenter = prevEchoOffsetStart + prevSegmentLength / 2;
			echoScrollOffset = prevCenter - segmentLength / 2;
		}
		
		if (echoScrollOffset < 0) {
			echoScrollOffset = 0;
		}
		else if (echoScrollOffset + segmentLength > bufferLength) {
			echoScrollOffset = bufferLength - segmentLength;
		}
		
		var segmentStart = echoStart + echoScrollOffset * 4;
		var segmentEnd = (UInt16) (segmentStart + segmentLength * 4 - 1);
		
		prevEchoOffsetStart = (segmentStart - echoStart) / 4;
		prevEchoOffsetEnd   = prevEchoOffsetStart + segmentLength - 1;
		
		List<Int16>  leftBuf = new();
		List<Int16> rightBuf = new();
		
		var snapStart = segmentStart;
		if (echoSampRatio > 1) {
			var er  = (int) echoSampRatio;
			var off = echoScrollOffset / er * er;
			snapStart = echoStart + off * 4;
		}
		
		var cursorPos = (cursorAddr - snapStart) / 4;
		
		for (var i = 0; i < segmentLength; i++) {
			var addr = (UInt16) (snapStart + i * 4);
			
			var leftLo  = buffer.ARAM_Data![addr];
			var leftHi  = buffer.ARAM_Data![addr + 1];
			
			var rightLo = buffer.ARAM_Data![addr + 2];
			var rightHi = buffer.ARAM_Data![addr + 3];
			
			var left  = (Int16) ( leftLo |  leftHi << 8);
			var right = (Int16) (rightLo | rightHi << 8);
			
			leftBuf .Add( left);
			rightBuf.Add(right);
		}
		
		var x = 2;
		var width = 128;
		
		switch (currentEchoView) {
			case EchoView.All: {
				Display.ClearLine(0);
				Display.ClearLine(13);
				Display.Write("Echo left",  0, 0);
				Display.Write("Echo right", 0, 13);
			
				displayWaveform(leftBuf.ToArray(),  x, 1,  width, 12, cursorPos);
				displayWaveform(rightBuf.ToArray(), x, 14, width, 12, cursorPos);
				
				break;
			}
			
			case EchoView.LeftOnly: {
				Display.ClearLine(0);
				Display.ClearLine(13);
				Display.Write("Echo left", 0, 0);
				displayWaveform(leftBuf.ToArray(), x, 1, width, 25, cursorPos);
				break;
			}
			
			case EchoView.RightOnly: {
				Display.ClearLine(0);
				Display.ClearLine(13);
				Display.Write("Echo right", 0, 0);
				displayWaveform(rightBuf.ToArray(), x, 1, width, 25, cursorPos);
				break;
			}
			
			case EchoView.Mixed: {
				Display.ClearLine(0);
				Display.ClearLine(13);
				Display.Write("Echo left/right mixed", 0, 0);
				displayWaveform(leftBuf.ToArray(), rightBuf.ToArray(), x, 1, width, 25, cursorPos);
				break;
			}
		}
		
		var barWidth = JMath.Round(width * segmentLength / (double) bufferLength);
		var barStart = width * echoScrollOffset / bufferLength;
		
		Display.Write(new string('▀', width),    2,            26, AnsiColor.DarkGrey);
		Display.Write(new string('▀', barWidth), 2 + barStart, 26, AnsiColor.White);
		
		if (echoScrollOffset + segmentLength == bufferLength) {
			Display.Write("▀", 2 + width - 1, 26, AnsiColor.White);
		}
		
		Display.Write($"{segmentStart:X4}", x,             27);
		Display.Write($"{segmentEnd  :X4}", x + width - 4, 27);
		
		var tabX = 80;
		var tabY = 27;
		
		var    fullLength = (int) (width * echoZoomLevel);
		string zoomText;
		
		if (bufferLength == 1) {
			zoomText = $" 1 sample  : 4 chars";
		}
		else if (fullLength < bufferLength) {
			zoomText = $"{bufferLength / fullLength,2} samples : 1 char ";
		}
		else if (fullLength == bufferLength) {
			zoomText = $" 1 sample  : 1 char ";
		}
		else {
			zoomText = $" 1 sample  : {fullLength / bufferLength} chars";
		}
		
		Display.Write($"Current addr: [{(UInt16) cursorAddr:X4}]", 48, 29);
		
		Display.Write($"Scale: {zoomText}", 6, 29);
		
		Display.Write($"                     Addr  Left Right",                                                                 tabX, tabY);
		Display.Write($"Last sample read:  [{ds.LastEchoReadAddr :X4}]  {ds.LastEchoReadLeft :X4}  {ds.LastEchoReadRight :X4}", tabX, tabY + 1);
		Display.Write($"Last sample write: [{ds.LastEchoWriteAddr:X4}]  {ds.LastEchoWriteLeft:X4}  {ds.LastEchoWriteRight:X4}", tabX, tabY + 2);
	}
	
	public static AnsiColor? WaveInsideColor = null;
	
	static void displayWaveform(Int16[] buf, int x, int y, int width, int height, int cursorPos, int loopPos = -1, bool isMuted = false) {
		displayWaveform(buf, null, x, y, width, height, cursorPos, loopPos: loopPos, isMuted: isMuted);
	}
	
	static void displayWaveform(Int16[] input, Int16[]? input_2, int x, int y, int width, int height, int cursor, bool writeToScrollBuf = false, int loopPos = -1, bool isMuted = false) {
		if (WaveInsideColor is null) {
			WaveInsideColor = new(Color.FromLCh(10, 30, 280), isBG: true);
		}
		
		var eqInsideRGB = WaveInsideColor.BackgroundRGB! * (isMuted ? 0.5 : 1.0);
		
		Display.ClearBox(width, height, x, y, new(eqInsideRGB, isBG: true), writeToScrollBuf);
		
		var waveCanvas = Analysis.DisplayWaveform(input, input_2, width, height, cursorIndex: cursor, loopIndex: loopPos, isMuted: isMuted);
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