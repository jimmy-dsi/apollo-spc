namespace SpcProgram;

using Apollo;
using Jimbl;

public static partial class CliMain {
	static UInt16 startAddr = 0x0000;
	
	static void showMemoryViewer(EmuDataBuffer buffer) {
		memDisplayRows(
			BusSize.Bit16,
			startAddr >> 4,
			(startAddr >> 4) + (ScrollAreaRows - 1),
			buffer.SMP_BusData!,
			useHeatMap: heatMapEnabled
		);
		
		showColorCoding();
	}
}