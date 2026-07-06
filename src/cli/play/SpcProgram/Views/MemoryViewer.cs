namespace SpcProgram;

using Jimbl.Graphics;

using Apollo;
using Jimbl;

public static partial class CliMain {
	public static UInt16     StartAddr = 0x0000;
	public static UInt16 PrevStartAddr = 0x0000;
	
	static void showMemoryViewer(EmuDataBuffer buffer) {
		Display.UseBufferBlending = true;
		Display.ScrollTop         = PrevStartAddr >> 4;
		
		var startRow = Display.ScrollTop;
		var endRow   = startRow + 0x2F;
		
		// Region locations
		var echoStartAddr = buffer.DSP_State!.EchoStartPage << 8;
		var echoStartRow  = echoStartAddr >> 4;
		
		var echoDelaySize = buffer.DSP_State!.EchoDelay * 0x800;
		var echoDelayRows = echoDelaySize >> 4;
		
		var echoEndRow = (echoStartRow + echoDelayRows - 1) & 0xFFF;
		if (echoEndRow < echoStartRow) {
			echoEndRow = echoStartRow;
		}
		
		var dirStartAddr = buffer.DSP_State!.SourceStartPage << 8;
		var dirStartRow  = dirStartAddr >> 4;
		
		var dirEndRow = (dirStartRow + 0x3F) & 0xFFF;
		
		var regionSamples = buffer.RecentSnapshot.CheckForSampleData((UInt16) (startRow << 4), 0x300);
		var rowSamples    = new byte?[0x30];
		
		// Determine which rows are samples
		for (var i = 0; i < regionSamples.Length; i++) {
			var r = i / 16;
			if (regionSamples[i] is not null) {
				rowSamples[r] = regionSamples[i];
			}
		}
		
		MemCellProperties[]? properties = null;
		
		if (heatMapEnabled && heatMapMemMode == HeatMapMode.TypeAware) {
			properties = new MemCellProperties[buffer.SMP_BusData!.Length];
			
			for (var r = startRow; r <= endRow; r++) {
				var s = r - startRow;
				
				if (r >= echoStartRow && r <= echoEndRow) {
					for (var c = 0; c < (echoStartRow == echoEndRow ? 4 : 16); c++) {
						var idx = (r - startRow) * 16 + c;
						
						if (idx < properties.Length) {
							properties[idx].DataSize = BusSize.Bit16;
							properties[idx].Signed   = true;
						}
					}
				}
				else if (rowSamples[s] is not null) {
					// Do nothing, already 8-bit unsigned by default
				}
				else if (r >= dirStartRow && r <= dirEndRow) {
					for (var c = 0; c < 16; c++) {
						var idx = (r - startRow) * 16 + c;
						
						if (idx < properties.Length) {
							properties[idx].DataSize = BusSize.Bit16;
							properties[idx].Signed   = false;
						}
					}
				}
			}
		}
		
		// Display memory cells + ASCII/Heat map
		memDisplayRows(
			BusSize.Bit16,
			startRow,
			endRow,
			buffer.SMP_BusData!,
			memCellProperties: properties,
			memLogs: logsSinceLastExec(buffer, filtered: false, inclExec: true),
			bootRomEnabled: buffer.SMP_State!.UseBootROM,
			readDisabled: buffer.SMP_State!.RAMDisable,
			writeDisabled: buffer.SMP_State!.RAMDisable || !buffer.SMP_State!.RAMWriteEnable,
			pc: buffer.SPC_State?.PC,
			useHeatMap: heatMapEnabled,
			yOffset: startRow,
			writeToScrollBuf: true
		);
		
		// Display S-DSP region markers
		byte? prevRowSamp = null;
		
		for (var r = startRow; r <= endRow; r++) {
			if (r > 0xFFF) {
				break;
			}
			var rowSamp = rowSamples[r - startRow];

			if (heatMapEnabled) {
				Display.Write("                   ", 89, r, col: null, writeToScrollBuf: true);
			}
			else {
				Display.Write("                                  ", 74, r, col: null, writeToScrollBuf: true);
			}
			
			if (r >= dirStartRow && r <= dirEndRow) {
				if (heatMapEnabled) {
					Display.Write("▒▒▒▒▒▒", 96, r, col: AnsiColor.Blue, writeToScrollBuf: true);
				}
				else {
					Display.Write("▒▒▒▒▒▒▒▒▒▒", 88, r, col: AnsiColor.Blue, writeToScrollBuf: true);
				}
				Display.Highlight(4, 0, r, col: AnsiColor.Code.Blue, writeToScrollBuf: true);
				Display.Write("▒", 4, r, col: AnsiColor.Blue, writeToScrollBuf: true);
			}
			
			if (rowSamp is not null) {
				if (heatMapEnabled) {
					Display.Write("▒▒▒▒▒▒", 102, r, col: new(readRegColor2.BackgroundRGB!), writeToScrollBuf: true);
				}
				else {
					Display.Write("▒▒▒▒▒▒▒▒▒▒", 98, r, col: new(readRegColor2.BackgroundRGB!), writeToScrollBuf: true);
				}
				
				Display.Highlight(4, 0, r, col: readRegColor2.BackgroundRGB, writeToScrollBuf: true);
				Display.Write("▒", 4, r, col: new(readRegColor2.BackgroundRGB!), writeToScrollBuf: true);
			}
			
			if (r >= echoStartRow && r <= echoEndRow) {
				if (heatMapEnabled) {
					Display.Write("▒▒▒▒▒▒", 90, r, col: AnsiColor.Magenta, writeToScrollBuf: true);
				}
				else {
					Display.Write("▒▒▒▒▒▒▒▒▒▒", 78, r, col: AnsiColor.Magenta, writeToScrollBuf: true);
				}
				Display.Highlight(4, 0, r, col: AnsiColor.Code.Magenta, writeToScrollBuf: true);
				Display.Write("▒", 4, r, col: AnsiColor.Magenta, writeToScrollBuf: true);
			}
			
			if (r == dirStartRow) {
				if (!heatMapEnabled) {
					Display.Write("                        ", 74, r, col: AnsiColor.BGBlue, writeToScrollBuf: true);
				}
				
				Display.Write(heatMapEnabled ? " Sample dir " : " Sample directory ",
				              heatMapEnabled ? 90 : 74, r,
				              col: AnsiColor.BGBlue,
				              writeToScrollBuf: true);
				
				Display.Highlight(4, 0, r, col: AnsiColor.Code.Blue, writeToScrollBuf: true);
				Display.Write("▒", 4, r, col: AnsiColor.Blue, writeToScrollBuf: true);
			}
				
			if (r == echoStartRow) {
				if (!heatMapEnabled) {
					Display.Write("              ", 74, r, col: AnsiColor.BGMagenta, writeToScrollBuf: true);
				}
				
				Display.Write(heatMapEnabled ? " Echo " : " Echo buffer ",
				              heatMapEnabled ? 90 : 74, r,
				              col: AnsiColor.BGMagenta,
				              writeToScrollBuf: true);
				
				Display.Highlight(4, 0, r, col: AnsiColor.Code.Magenta, writeToScrollBuf: true);
				Display.Write("▒", 4, r, col: AnsiColor.Magenta, writeToScrollBuf: true);
			}
			
			if (rowSamp is not null && rowSamp != prevRowSamp) {
				var sampInfo = buffer.RecentSnapshot.LookupSampleInfo(rowSamp.Value);
				
				var sampStartRow = sampInfo.Start >> 4;
				var sampLoopRow  = sampInfo.Loop  >> 4;
				
				if (r == sampStartRow || r == sampLoopRow) {
					var addr = r == sampStartRow ? sampInfo.Start : sampInfo.Loop;
					//var endAddr = addr;
					//
					//while (endAddr <= 0xFFF7) {
					//	if ((buffer.RecentSnapshot.DSP.ARAM[endAddr] & 1) != 0) {
					//		break;
					//	}
					//	
					//	endAddr += 9;
					//}
					//
					//endAddr += 8;
				
					if (!heatMapEnabled) {
						Display.Write("                                  ", 74, r, col: readRegColor2, writeToScrollBuf: true);
					}
					
					Display.Write($" Sample {rowSamp.Value:X2} [{addr:X4}] ",
					              heatMapEnabled ? 90 : 74, r,
					              col: readRegColor2,
					              writeToScrollBuf: true);
					
					Display.Highlight(4, 0, r, col: readRegColor2.BackgroundRGB, writeToScrollBuf: true);
					Display.Write("▒", 4, r, col: new(readRegColor2.BackgroundRGB!), writeToScrollBuf: true);
				}
			}
			
			prevRowSamp = rowSamp;
		}
		
		showColorCoding();
	}
}