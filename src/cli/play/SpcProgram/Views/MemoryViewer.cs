namespace SpcProgram;

using Apollo;
using Jimbl;

public static partial class CliMain {
	static byte[]? progressiveMemBuffer = null;
	static UInt16  startAddr            = 0x0000;
	
	static void showMemoryViewer(EmuDataBuffer buffer) {
		if (progressiveMemBuffer is null) {
			progressiveMemBuffer = buffer.SMP_BusData!.ToArray();
		}
		else {
			softFadeHeatmap(buffer.SMP_BusData!, progressiveMemBuffer);
		}
		
		memDisplayRows(
			BusSize.Bit16,
			startAddr >> 4,
			(startAddr >> 4) + (ScrollAreaRows - 1),
			buffer.SMP_BusData!,
			progressiveMemBuffer,
			useHeatMap: heatMapEnabled
		);
		
		showColorCoding();
	}
}