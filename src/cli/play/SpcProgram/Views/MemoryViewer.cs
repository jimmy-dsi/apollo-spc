namespace SpcProgram;

using Apollo;
using Jimbl;

public static partial class CliMain {
	public static UInt16     StartAddr  = 0x0000;
	public static UInt16 PrevStartAddr1 = 0x0000;
	public static UInt16 PrevStartAddr2 = 0x0000;
	public static UInt16 PrevStartAddr3 = 0x0000;
	public static UInt16 PrevStartAddr4 = 0x0000;
	
	static void showMemoryViewer(EmuDataBuffer buffer) {
		memDisplayRows(
			BusSize.Bit16,
			StartAddr >> 4,
			(StartAddr >> 4) + (ScrollAreaRows - 1),
			buffer.SMP_BusData!,
			useHeatMap: heatMapEnabled
		);
		
		showColorCoding();
	}
}