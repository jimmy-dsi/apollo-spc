namespace SpcProgram;

using Apollo;
using Jimbl;

public static partial class CliMain {
	static UInt16 startAddr = 0x0000;
	
	static void showMemoryViewer(EmuDataBuffer buffer) {
		var displayRows = memDisplayRows(AddressBusSize.Bit16, startAddr >> 4, (startAddr >> 4) + (ScrollAreaRows - 1), buffer.SMP_BusData!);
		Display.WriteBox(displayRows, 0, 0);
	}
}