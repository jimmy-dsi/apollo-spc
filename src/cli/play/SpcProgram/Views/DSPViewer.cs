namespace SpcProgram;

using Apollo;
using Jimbl;

public static partial class CliMain {
	static byte[]? progressiveBuffer = null;
	
	static void showDSPViewer1(EmuDataBuffer buffer) {
		if (progressiveBuffer is null) {
			progressiveBuffer = buffer.DSP_RegisterMem!.ToArray();
		}
		else {
			softFadeHeatmap(buffer.DSP_RegisterMem!, progressiveBuffer);
		}
		
		var coloring = new Color?[0x80];
		var color = Color.DarkGrey;
		
		for (var c = 0; c < 8; c++) {
			coloring[c * 0x10 + 0xA] = color;
			coloring[c * 0x10 + 0xB] = color;
			coloring[c * 0x10 + 0xE] = color;
			coloring[0x1D] = color;
		}
		
		memDisplayRows(AddressBusSize.Bit8, 0, 7, buffer.DSP_RegisterMem!, progressiveBuffer, coloring, useHeatMap: heatMapEnabled);
	}
}