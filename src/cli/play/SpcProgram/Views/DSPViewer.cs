namespace SpcProgram;

using Apollo;
using Jimbl;

public static partial class CliMain {
	static byte[]? progressiveBuffer = null;
	
	static void showDSPViewer1(EmuDataBuffer buffer) {
		showDSPMem(buffer);
		var yBase = Display.Y + 1;
		
		var y = 0;
		var x = 0;
		
		var voiceOnStates = PrimaryEmu.VoiceOnStates;
		
		for (var v = 0; v < 8; v++) {
			x = 27 * (v % 4);
			y = yBase + 10 * (v / 4);
			
			Display.Write($"V{v + 1}", x, y, col: voiceOnStates[v] ? null : Color.DarkGrey);
			
			Display.WriteBox([
				"left volume:",
				"right volume:",
				"pitch:",
				"srcn:",
				"adsr 1:",
				"adsr 2:",
				"gain:",
				"envx:",
				"outx:",
			], x + 4, y, col: voiceOnStates[v] ? null : Color.DarkGrey);
		
			var xhm = x;
		
			if (heatMapEnabled) {
				var colMult = voiceOnStates[v] ? 1.0 : 0.125;
				
				Display.Write("  ", x + 18, y    , col: heatMapColor(BusSize.Bit8,  signed: true,  scale: 1 * colMult, buffer.DSP_Voice![v].VolumeLeft));
				Display.Write("  ", x + 18, y + 1, col: heatMapColor(BusSize.Bit8,  signed: true,  scale: 1 * colMult, buffer.DSP_Voice![v].VolumeRight));
				Display.Write("  ", x + 18, y + 2, col: heatMapColor(BusSize.Bit16, signed: false, scale: 4 * colMult, buffer.DSP_Voice![v].Pitch));
				Display.Write("  ", x + 18, y + 3, col: heatMapColor(BusSize.Bit8,  signed: false, scale: 1 * colMult, buffer.DSP_Voice![v].Source));
				Display.Write("  ", x + 18, y + 4, col: heatMapColor(BusSize.Bit8,  signed: false, scale: 1 * colMult, buffer.DSP_Voice![v].ADSR0));
				Display.Write("  ", x + 18, y + 5, col: heatMapColor(BusSize.Bit8,  signed: false, scale: 1 * colMult, buffer.DSP_Voice![v].ADSR1));
				Display.Write("  ", x + 18, y + 6, col: heatMapColor(BusSize.Bit8,  signed: false, scale: 1 * colMult, buffer.DSP_Voice![v].Gain));
				Display.Write("  ", x + 18, y + 7, col: heatMapColor(BusSize.Bit8,  signed: false, scale: 2 * colMult, buffer.DSP_Voice![v].ENVX));
				Display.Write("  ", x + 18, y + 8, col: heatMapColor(BusSize.Bit8,  signed: true,  scale: 1 * colMult, buffer.DSP_RegisterMem![v << 4 | 9]));
			}
			
			xhm += 3;
			
			Display.WriteBox([
				$"{(byte) buffer.DSP_Voice![v].VolumeLeft :X2}",
				$"{(byte) buffer.DSP_Voice![v].VolumeRight:X2}",
				$"{buffer.DSP_Voice![v].Pitch:X4}",
				$"{buffer.DSP_Voice![v].Source:X2}",
				$"{buffer.DSP_Voice![v].ADSR0:X2}",
				$"{buffer.DSP_Voice![v].ADSR1:X2}",
				$"{buffer.DSP_Voice![v].Gain:X2}",
				$"{buffer.DSP_Voice![v].ENVX:X2}",
				$"{buffer.DSP_RegisterMem![v << 4 | 9]:X2}",
			], xhm + 18, y, col: voiceOnStates[v] ? null : Color.DarkGrey);
		}
		
		showColorCoding();
	}
	
	static void showDSPViewer2(EmuDataBuffer buffer) {
		showDSPMem(buffer);
		var baseY = Display.Y + 1;
		
		var y = baseY;
		var x = 0;
		
		var xo1 = 22;
		var xo2 = 25;
		
		var voices = buffer.DSP_State!.Voice;
		
		var globalPModEn  = vFlagsToByte(voices.Select(x => x.PitchModOn).ToArray());
		var globalNoiseEn = vFlagsToByte(voices.Select(x => x   .NoiseOn).ToArray());
		var globalEchoEn  = vFlagsToByte(voices.Select(x => x    .EchoOn).ToArray());
		
		// Section 1
		var xhm = xo1;
		
		if (heatMapEnabled) {
			Display.Write("  ", xo1, y    , col: heatMapColor(BusSize.Bit8, signed: true, scale: 1, buffer.DSP_State!.MainVolumeLeft ));
			Display.Write("  ", xo1, y + 1, col: heatMapColor(BusSize.Bit8, signed: true, scale: 1, buffer.DSP_State!.MainVolumeRight));
			Display.Write("  ", xo1, y + 2, col: heatMapColor(BusSize.Bit8, signed: true, scale: 1, buffer.DSP_State!.EchoVolumeLeft ));
			Display.Write("  ", xo1, y + 3, col: heatMapColor(BusSize.Bit8, signed: true, scale: 1, buffer.DSP_State!.EchoVolumeRight));
			
			Display.Write("  ", xo1, y + 5, col: heatMapColor(BusSize.Bit8, signed: false, scale: 1, globalPModEn ));
			Display.Write("  ", xo1, y + 6, col: heatMapColor(BusSize.Bit8, signed: false, scale: 1, globalNoiseEn));
			Display.Write("  ", xo1, y + 7, col: heatMapColor(BusSize.Bit8, signed: false, scale: 1, globalEchoEn ));
			
			Display.Write("  ", xo1, y + 9,  col: heatMapColor(BusSize.Bit8, signed: false, scale: 8,   buffer.DSP_State!.NoiseClock));
			Display.Write("  ", xo1, y + 10, col: heatMapColor(BusSize.Bit8, signed: false, scale: 255, buffer.DSP_State!.ReadonlyEcho ? 1 : 0));
			Display.Write("  ", xo1, y + 11, col: heatMapColor(BusSize.Bit8, signed: false, scale: 255, buffer.DSP_State!.Mute         ? 1 : 0));
			Display.Write("  ", xo1, y + 12, col: heatMapColor(BusSize.Bit8, signed: false, scale: 255, buffer.DSP_State!.Reset        ? 1 : 0));
		}
			
		xhm += 3;
		
		Display.WriteBox([
			"main volume - left:",
			"main volume - right:",
			"echo volume - left:",
			"echo volume - right:",
		], x, y);
		
		Display.WriteBox([
			$"{buffer.DSP_State!.MainVolumeLeft :X2}",
			$"{buffer.DSP_State!.MainVolumeRight:X2}",
			$"{buffer.DSP_State!.EchoVolumeLeft :X2}",
			$"{buffer.DSP_State!.EchoVolumeRight:X2}",
		], xhm, y);
		
		// Section 2
		y = Display.Y + 1;
		
		Display.WriteBox([
			"pitch modulation:",
			"noise enable:",
			"echo enable:",
		], x, y);
		
		Display.WriteBox([
			$"{globalPModEn :X2}",
			$"{globalNoiseEn:X2}",
			$"{globalEchoEn :X2}",
		], xhm, y);
		
		// Section 3
		y = Display.Y + 1;
		
		Display.WriteBox([
			"noise clock:",
			"read-only echo:",
			"mute:",
			"reset:",
		], x, y);
		
		Display.WriteBox([
			$"{buffer.DSP_State!.NoiseClock:X2}",
			$"{buffer.DSP_State!.ReadonlyEcho} ",
			$"{buffer.DSP_State! .Mute} ",
			$"{buffer.DSP_State!.Reset} ",
		], xhm, y);
		
		// Section 4
		Display.Y += 1;
		y = Display.Y;
		Display.Write("fir:  ", x, y + 1);
		Display.Y = y;
		
		var firX = Display.X;
		
		if (heatMapEnabled) {
			foreach (var val in buffer.DSP_State!.FIR) {
				Display.Write($"   ", col: heatMapColor(BusSize.Bit8, signed: true, scale: 1, val));
			}
		}
		
		Display.X = firX;
		Display.Y += 1;
		
		foreach (var val in buffer.DSP_State!.FIR) {
			Display.Write($" {(byte) val:X2}");
		}
		
		// Section 5
		y = baseY;
		x = xo1 + 11;
		
		var globalKeyOn  = vFlagsToByte(voices.Select(x => x.KeyOn ).ToArray());
		var globalKeyOff = vFlagsToByte(voices.Select(x => x.KeyOff).ToArray());
		var globalEndx   = vFlagsToByte(voices.Select(x => x   .End).ToArray());
		
		xhm = x + xo2;
		
		if (heatMapEnabled) {
			Display.Write("  ", x + xo2, y    , col: heatMapColor(BusSize.Bit8, signed: false, scale: 1, globalKeyOn ));
			Display.Write("  ", x + xo2, y + 1, col: heatMapColor(BusSize.Bit8, signed: false, scale: 1, globalKeyOff));
			Display.Write("  ", x + xo2, y + 2, col: heatMapColor(BusSize.Bit8, signed: false, scale: 1, globalEndx  ));
			Display.Write("  ", x + xo2, y + 3, col: heatMapColor(BusSize.Bit8, signed:  true, scale: 1, buffer.DSP_State!.EchoFeedback));
			
			Display.Write("  ", x + xo2, y + 5, col: heatMapColor(BusSize.Bit8, signed: false, scale: 1,  buffer.DSP_State!.EchoStartPage));
			Display.Write("  ", x + xo2, y + 6, col: heatMapColor(BusSize.Bit8, signed: false, scale: 1,  buffer.DSP_State!.SourceStartPage));
			Display.Write("  ", x + xo2, y + 7, col: heatMapColor(BusSize.Bit8, signed: false, scale: 16, buffer.DSP_State!.EchoDelay));
		}
			
		xhm += 3;
		
		Display.WriteBox([
			"key on:",
			"key off:",
			"source end (endx):",
			"echo feedback:",
		], x, y);
		
		Display.WriteBox([
			$"{globalKeyOn :X2}",
			$"{globalKeyOff:X2}",
			$"{globalEndx  :X2}",
			$"{(byte) buffer.DSP_State!.EchoFeedback:X2}",
		], xhm, y);
		
		// Section 6
		y = Display.Y + 1;
		
		Display.WriteBox([
			"echo buffer start:",
			"source directory start:",
			"echo delay:",
		], x, y);
		
		Display.WriteBox([
			$"{buffer.DSP_State!.EchoStartPage   << 8:X4}",
			$"{buffer.DSP_State!.SourceStartPage << 8:X4}",
			$"{buffer.DSP_State!.EchoDelay           :X1}",
		], xhm, y);
		
		showColorCoding();
	}
	
	static void showDSPMem(EmuDataBuffer buffer) {
		if (progressiveBuffer is null) {
			progressiveBuffer = buffer.DSP_RegisterMem!.ToArray();
		}
		else {
			softFadeHeatmap(buffer.DSP_RegisterMem!, progressiveBuffer);
		}
		
		var coloring = new Color?[0x80];
		var color    = Color.DarkGrey;
		
		for (var c = 0; c < 8; c++) {
			coloring[c * 0x10 + 0xA] = color;
			coloring[c * 0x10 + 0xB] = color;
			coloring[c * 0x10 + 0xE] = color;
			coloring[0x1D]           = color;
		}
		
		memDisplayRows(BusSize.Bit8, 0, 7, buffer.DSP_RegisterMem!, progressiveBuffer, coloring, useHeatMap: heatMapEnabled);
	}
	
	static byte vFlagsToByte(bool[] flags) {
		byte result = 0x00;
		for (var v = 0; v < flags.Length; v++) {
			result |= (byte) ((flags[v] ? 1 : 0) << v);
		}
		return result;
	}
}