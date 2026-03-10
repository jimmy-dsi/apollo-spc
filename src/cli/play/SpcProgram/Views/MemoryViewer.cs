namespace SpcProgram;

using Apollo;
using Jimbl;

public static partial class CliMain {
	public static UInt16     StartAddr = 0x0000;
	public static UInt16 PrevStartAddr = 0x0000;
	
	static void showMemoryViewer(EmuDataBuffer buffer) {
		Display.ScrollTop = PrevStartAddr >> 4;
		
		var startRow = Display.ScrollTop;
		var endRow   = startRow + 0x2F;
		
		memDisplayRows(
			BusSize.Bit16,
			startRow,
			endRow,
			buffer.SMP_BusData!,
			useHeatMap: heatMapEnabled,
			yOffset: startRow,
			writeToScrollBuf: true
		);
		
		showColorCoding();
	}
}