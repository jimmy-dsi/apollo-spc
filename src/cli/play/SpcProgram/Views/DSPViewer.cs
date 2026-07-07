namespace SpcProgram;

using System.Diagnostics;

using Apollo;
using Jimbl;
using Jimbl.Graphics;
using Jimbl.JMath;

public static partial class CliMain {
	static void showDSPViewer1(EmuDataBuffer buffer) {
		showDSPMem(buffer);
		var yBase = Display.Y + 1;
		
		var y = 0;
		var x = 0;
		
		var voiceOnStates = PrimaryEmu.MainVoiceOnStates;
		
		for (var v = 0; v < 8; v++) {
			x = 27 * (v % 4);
			y = yBase + 10 * (v / 4);
			
			Display.Write($"V{v + 1}", x, y, col: voiceOnStates[v] ? null : AnsiColor.DarkGrey);
			
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
			], x + 4, y, col: voiceOnStates[v] ? null : AnsiColor.DarkGrey);
		
			var xhm = x;
		
			if (heatMapEnabled) {
				var colMult = voiceOnStates[v] ? 1.0 : 0.125;
				
				Display.Write("  ", x + 18, y    , col: HeatMapColor(BusSize.Bit8,  signed: true,  scale: 1 * colMult, buffer.DSP_Voice![v].VolumeLeft));
				Display.Write("  ", x + 18, y + 1, col: HeatMapColor(BusSize.Bit8,  signed: true,  scale: 1 * colMult, buffer.DSP_Voice![v].VolumeRight));
				Display.Write("  ", x + 18, y + 2, col: HeatMapColor(BusSize.Bit16, signed: false, scale: 4 * colMult, buffer.DSP_Voice![v].Pitch));
				Display.Write("  ", x + 18, y + 3, col: HeatMapColor(BusSize.Bit8,  signed: false, scale: 1 * colMult, buffer.DSP_Voice![v].Source));
				Display.Write("  ", x + 18, y + 4, col: HeatMapColor(BusSize.Bit8,  signed: false, scale: 1 * colMult, buffer.DSP_Voice![v].ADSR0));
				Display.Write("  ", x + 18, y + 5, col: HeatMapColor(BusSize.Bit8,  signed: false, scale: 1 * colMult, buffer.DSP_Voice![v].ADSR1));
				Display.Write("  ", x + 18, y + 6, col: HeatMapColor(BusSize.Bit8,  signed: false, scale: 1 * colMult, buffer.DSP_Voice![v].Gain));
				Display.Write("  ", x + 18, y + 7, col: HeatMapColor(BusSize.Bit8,  signed: false, scale: 2 * colMult, buffer.DSP_Voice![v].ENVX));
				Display.Write("  ", x + 18, y + 8, col: HeatMapColor(BusSize.Bit8,  signed: true,  scale: 1 * colMult, buffer.DSP_RegisterMem![v << 4 | 9]));
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
			], xhm + 18, y, col: voiceOnStates[v] ? null : AnsiColor.DarkGrey);
		}
		
		showColorCoding();
	}
	
	public static AnsiColor? EQInsideColor = null;
	
	static void showDSPViewer2(EmuDataBuffer buffer) {
		if (EQInsideColor is null) {
			EQInsideColor = HeatMapColor(BusSize.Bit8, false, 1, 0);
		}
		
		var eqInsideRGB = EQInsideColor.BackgroundRGB!.Multiply(5.0 / 7);
		
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
			Display.Write("  ", xo1, y    , col: HeatMapColor(BusSize.Bit8, signed: true, scale: 1, buffer.DSP_State!.MainVolumeLeft ));
			Display.Write("  ", xo1, y + 1, col: HeatMapColor(BusSize.Bit8, signed: true, scale: 1, buffer.DSP_State!.MainVolumeRight));
			Display.Write("  ", xo1, y + 2, col: HeatMapColor(BusSize.Bit8, signed: true, scale: 1, buffer.DSP_State!.EchoVolumeLeft ));
			Display.Write("  ", xo1, y + 3, col: HeatMapColor(BusSize.Bit8, signed: true, scale: 1, buffer.DSP_State!.EchoVolumeRight));
			
			var pFlags = drawHeatMapFlags(BusSize.Bit8,  globalPModEn);
			var nFlags = drawHeatMapFlags(BusSize.Bit8, globalNoiseEn);
			var eFlags = drawHeatMapFlags(BusSize.Bit8,  globalEchoEn);
			
			for (var i = 0; i < 4; i++) {
				Display.Write(new(pFlags[i].Char, 1), xo1 - 2 + i, y + 5, col: pFlags[i].Color);
				Display.Write(new(nFlags[i].Char, 1), xo1 - 2 + i, y + 6, col: nFlags[i].Color);
				Display.Write(new(eFlags[i].Char, 1), xo1 - 2 + i, y + 7, col: eFlags[i].Color);
			}
			
			Display.Write("  ", xo1, y + 9,  col: HeatMapColor(BusSize.Bit8, signed: false, scale: 8,   buffer.DSP_State!.NoiseClock));
			Display.Write("  ", xo1, y + 10, col: HeatMapColor(BusSize.Bit8, signed: false, scale: 255, buffer.DSP_State!.ReadonlyEcho ? 1 : 0));
			Display.Write("  ", xo1, y + 11, col: HeatMapColor(BusSize.Bit8, signed: false, scale: 255, buffer.DSP_State!.Mute         ? 1 : 0));
			Display.Write("  ", xo1, y + 12, col: HeatMapColor(BusSize.Bit8, signed: false, scale: 255, buffer.DSP_State!.Reset        ? 1 : 0));
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
				Display.Write($"   ", col: HeatMapColor(BusSize.Bit8, signed: true, scale: 1, val));
			}
		}
		
		Display.X = firX;
		Display.Y += 1;
		
		foreach (var val in buffer.DSP_State!.FIR) {
			Display.Write($" {(byte) val:X2}");
		}
		
		// Section 5
		var specX = firX + 37;
		var specY = Display.Y - 6;
		
		AnsiColor? darkBlue    = null; //heatMapColor(BusSize.Bit8, false, 1, 12);
		AnsiColor? boxColor    = null;
		
		AnsiColor  darkLine = new(AnsiColor.Code.DarkGrey, eqInsideRGB);
		AnsiColor lightLine = new(AnsiColor.Code.    Grey, eqInsideRGB);
		
		AnsiColor eqInsideColor2 = new(eqInsideRGB, isBG: true);
		
		Display.ClearBox(20, 8, specX + 1, specY + 1);
		Display.Write(new(' ', 21), specX, specY + 1, col: eqInsideColor2);
		Display.Write(new('_', 21), specX, specY + 2, col: darkLine);
		Display.Write(new(' ', 21), specX, specY + 3, col: eqInsideColor2);
		Display.Write(new('_', 21), specX, specY + 4, col: lightLine);
		Display.Write(new(' ', 21), specX, specY + 5, col: eqInsideColor2);
		Display.Write(new('_', 21), specX, specY + 6, col: darkLine);
		Display.Write(new(' ', 21), specX, specY + 7, col: eqInsideColor2);
		Display.Write(new(' ', 21), specX, specY + 8, col: eqInsideColor2);
		
		var firFFT = JMath.FFT_Gain(buffer.DSP_State!.FIR.Select(x => (int) x).ToArray(), 0x80);
		const double MaxDB = 16;
		
		for (var i = 0; i < firFFT.Length; i++) {
			var raw = JDSP.AmpToDB(firFFT[i]);
			var val = Math.Clamp(raw + MaxDB, 0, MaxDB * 2);
			
			var bar = val / (MaxDB * 2);
			
			showBar(bar, 8, specX + 2 + 4 * i, specY + 9);
		}
		
		Display.DrawOutline(specX - 1, specY, 23, 10, col: boxColor);
		Display.Write(new(' ', 23), specX -  1, specY, col: eqInsideColor2);
		Display.Write(new(' ', 21), specX,      specY, col: darkBlue);
		Display.Write(new(' ', 23), specX -  1, specY + 9, col: eqInsideColor2);
		Display.Write(" ",          specX -  1, specY, col: boxColor);
		Display.Write(" ",          specX + 21, specY, col: boxColor);
		
		//Display.ClearBox(4, 9, specX - 4, specY + 1, col: Color.BGMagenta);
		Display.Write("+16 _", specX - 6, specY,     col: boxColor);
		Display.Write("+12 _", specX - 6, specY + 1, col: boxColor);
		Display.Write("+8  _", specX - 6, specY + 2, col: boxColor);
		Display.Write("+4  _", specX - 6, specY + 3, col: boxColor);
		Display.Write(" 0  _", specX - 6, specY + 4, col: boxColor);
		Display.Write("-4  _", specX - 6, specY + 5, col: boxColor);
		Display.Write("-8  _", specX - 6, specY + 6, col: boxColor);
		Display.Write("-12 _", specX - 6, specY + 7, col: boxColor);
		Display.Write("-16 _", specX - 6, specY + 8, col: boxColor);
		Display.Write("     ", specX - 6, specY + 9, col: boxColor);
		Display.Write("  0k  4k  8k  12k  16k ", specX - 1, specY + 9, col: boxColor);
		
		// Section 6
		y = baseY;
		x = xo1 + 11;
		
		var globalKeyOn  = vFlagsToByte(voices.Select(x => x.KeyOn ).ToArray());
		var globalKeyOff = vFlagsToByte(voices.Select(x => x.KeyOff).ToArray());
		var globalEndx   = vFlagsToByte(voices.Select(x => x   .End).ToArray());
		
		xhm = x + xo2;
		
		if (heatMapEnabled) {
			var konFlags  = drawHeatMapFlags(BusSize.Bit8,  globalKeyOn);
			var koffFlags = drawHeatMapFlags(BusSize.Bit8, globalKeyOff);
			var endxFlags = drawHeatMapFlags(BusSize.Bit8,   globalEndx);
			
			for (var i = 0; i < 4; i++) {
				Display.Write(new( konFlags[i].Char, 1), x + xo2 - 2 + i, y    , col:  konFlags[i].Color);
				Display.Write(new(koffFlags[i].Char, 1), x + xo2 - 2 + i, y + 1, col: koffFlags[i].Color);
				Display.Write(new(endxFlags[i].Char, 1), x + xo2 - 2 + i, y + 2, col: endxFlags[i].Color);
			}
			Display.Write("  ", x + xo2, y + 3, col: HeatMapColor(BusSize.Bit8, signed:  true, scale: 1, buffer.DSP_State!.EchoFeedback));
			
			Display.Write("  ", x + xo2, y + 5, col: HeatMapColor(BusSize.Bit8, signed: false, scale: 1,  buffer.DSP_State!.EchoStartPage));
			Display.Write("  ", x + xo2, y + 6, col: HeatMapColor(BusSize.Bit8, signed: false, scale: 1,  buffer.DSP_State!.SourceStartPage));
			Display.Write("  ", x + xo2, y + 7, col: HeatMapColor(BusSize.Bit8, signed: false, scale: 16, buffer.DSP_State!.EchoDelay));
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
	
	static void showDSPViewer3(EmuDataBuffer buffer) {
		var yBase = 0;
		
		var x = 0;
		var y = yBase;
		
		var voiceOnStates = PrimaryEmu.MainVoiceOnStates;
		
		for (var v = 0; v < 8; v++) {
			x = 28 * (v % 4);
			y = yBase + 15 * (v / 4);
			
			Display.Write($"V{v + 1}", x, y, col: voiceOnStates[v] ? null : AnsiColor.DarkGrey);
			
			Display.WriteBox([
				"buff. offset:",
				"gauss offset:",
				"brr address:",
				"brr offset:",
				"key on delay:",
				"noise on:",
				"pitch mod on:",
				"env. mode:",
				"env. level:",
				"decode buffer:",
			], x + 4, y, col: voiceOnStates[v] ? null : AnsiColor.DarkGrey);
			
			var envName = buffer.DSP_DebugState!.Voice[v].EnvMode switch {
				DSP.EnvelopeMode.Attack  => "att.",
				DSP.EnvelopeMode.Decay   => "dec.",
				DSP.EnvelopeMode.Release => "rel.",
				DSP.EnvelopeMode.KeyOff  => "off",
				_ => throw new UnreachableException()
			};
		
			Display.WriteBox([
				$"{ buffer.DSP_DebugState!.Voice[v]  .BufferOffset:X1}",
				$"{ buffer.DSP_DebugState!.Voice[v].GaussianOffset:X4}",
				$"{ buffer.DSP_DebugState!.Voice[v]    .BRRAddress:X4}",
				$"{ buffer.DSP_DebugState!.Voice[v]     .BRROffset:X1}",
				$"{ buffer.DSP_DebugState!.Voice[v]    .KeyOnDelay:X1}",
				$"{ buffer.DSP_DebugState!.Voice[v]       .NoiseOn   } ",
				$"{ buffer.DSP_DebugState!.Voice[v]    .PitchModOn   } ",
				$"{ envName } ",
				$"{(buffer.DSP_DebugState!.Voice[v].EnvLevel >> 4):X2}.{(buffer.DSP_DebugState!.Voice[v].EnvLevel & 0xF):X1}",
			], x + 21, y, col: voiceOnStates[v] ? null : AnsiColor.DarkGrey);
			
			var nextY   = Display.Y;
			var colMult = voiceOnStates[v] ? 1.0 : 0.125;
		
			if (heatMapEnabled) {
				var xx = x + 18;
				
				var envModeU = (byte) buffer.DSP_DebugState!.Voice[v].EnvMode;
				var envModeS = envModeU switch {
					0 => 0,
					1 => 0x40,
					2 => 0x80,
					3 => 0xC0,
					_ => throw new UnreachableException()
				};
				
				Display.Write("  ", xx, y    , col: HeatMapColor(BusSize.Bit8,  signed: false, scale: 256.0 / 12 * colMult, buffer.DSP_DebugState!.Voice[v].BufferOffset));
				Display.Write("  ", xx, y + 1, col: HeatMapColor(BusSize.Bit16, signed: false, scale:          2 * colMult, buffer.DSP_DebugState!.Voice[v].GaussianOffset));
				Display.Write("  ", xx, y + 2, col: HeatMapColor(BusSize.Bit16, signed: false, scale:          1 * colMult, buffer.DSP_DebugState!.Voice[v].BRRAddress));
				Display.Write("  ", xx, y + 3, col: HeatMapColor(BusSize.Bit8,  signed: false, scale:         32 * colMult, buffer.DSP_DebugState!.Voice[v].BRROffset));
				Display.Write("  ", xx, y + 4, col: HeatMapColor(BusSize.Bit8,  signed: false, scale: 256.0 /  5 * colMult, buffer.DSP_DebugState!.Voice[v].KeyOnDelay));
				Display.Write("  ", xx, y + 5, col: HeatMapColor(BusSize.Bit8,  signed: false, scale:        255 * colMult, buffer.DSP_DebugState!.Voice[v].NoiseOn ? 1 : 0));
				Display.Write("  ", xx, y + 6, col: HeatMapColor(BusSize.Bit8,  signed: false, scale:        255 * colMult, buffer.DSP_DebugState!.Voice[v].PitchModOn ? 1 : 0));
				Display.Write("  ", xx, y + 7, col: HeatMapColor(BusSize.Bit8,  signed:  true, scale:          1 * colMult, envModeS));
				Display.Write("  ", xx, y + 8, col: HeatMapColor(BusSize.Bit16, signed: false, scale:         32 * colMult, buffer.DSP_DebugState!.Voice[v].EnvLevel));
			}
			
			Display.Y = nextY + 1;
			
			for (var by = 0; by < 3; by++) {
				for (var bx = 0; bx < 4; bx++) {
					var val = (UInt16) buffer.DSP_DebugState!.Voice[v].Buffer[4 * by + bx];
					Display.Write($"{val:X4} ", x + 6 + 5 * bx, col: heatMapEnabled ? HeatMapColor(BusSize.Bit16, signed: true, scale: colMult, val) : voiceOnStates[v] ? null : AnsiColor.DarkGrey);
				}
				Display.Y++;
			}
		}
		
		showColorCoding();
	}
		
	static void showDSPMem(EmuDataBuffer buffer) {
		var coloring = new AnsiColor?[0x80];
		var color    = AnsiColor.DarkGrey;
		
		for (var c = 0; c < 8; c++) {
			coloring[c * 0x10 + 0xA] = color;
			coloring[c * 0x10 + 0xB] = color;
			coloring[c * 0x10 + 0xE] = color;
			coloring[0x1D]           = color;
		}
		
		for (var v = 0; v < 8; v++) {
			var on = PrimaryEmu.MainVoiceOnStates[v];
			if (!on) {
				for (var i = 0; i <= 9; i++) {
					coloring[v * 0x10 + i] = color;
				}
			}
		}
		
		var dspLogs = getDspMemLogs(buffer);
		
		MemCellProperties[]? properties = null;
		
		if (heatMapEnabled && heatMapMemMode == HeatMapMode.TypeAware) {
			properties = new MemCellProperties[0x80];
			
			var chanProps = new MemCellProperties[] {
				new(BusSize.Bit8,  signed:  true), new(BusSize.Bit8, signed:  true), 
				new(BusSize.Bit16, signed: false, scale: 4), new(BusSize.Bit8, signed: false, scale: 4), 
				new(), new(), new(), new(),
				new(BusSize.Bit8, signed: false, scale: 2), new(BusSize.Bit8, signed: true), 
				new(), new(), new(), new(), new(),
				new(BusSize.Bit8,  signed:  true)
			};
			
			for (var c = 0; c < 8; c++) {
				for (var i = 0; i < chanProps.Length; i++) {
					properties[c * 0x10 + i] = chanProps[i];
				}
			}
			
			properties[0x0C] = new(BusSize.Bit8, signed: true);
			properties[0x1C] = new(BusSize.Bit8, signed: true);
			
			properties[0x2C] = new(BusSize.Bit8, signed: true);
			properties[0x3C] = new(BusSize.Bit8, signed: true);
			
			properties[0x0D] = new(BusSize.Bit8, signed: true);
			properties[0x7D] = new(BusSize.Bit8, signed: false, scale: 16);
		}
		
		memDisplayRows(
			BusSize.Bit8,
			0,
			7,
			buffer.DSP_RegisterMem!,
			memCellProperties: properties,
			colorData: coloring,
			memLogs: dspLogs,
			useHeatMap: heatMapEnabled
		);
	}
	
	static byte vFlagsToByte(bool[] flags) {
		byte result = 0x00;
		for (var v = 0; v < flags.Length; v++) {
			result |= (byte) ((flags[v] ? 1 : 0) << v);
		}
		return result;
	}
	
	static SMP.MemAccessLog[] getDspMemLogs(EmuDataBuffer buffer) {
		var dspAddr = buffer.SMP_State!.DSPAddress;
		
		var logs = logsSinceLastExec(buffer, filtered: false, inclExec: true)
		           .Where(x => x.Address is 0x00F2 or 0x00F3);
		
		List<SMP.MemAccessLog> dspLogs = new();
		
		foreach (var log in logs.Reverse()) {
			if (log.Type == SMP.MemAccessLog.LogType.Write) {
				if (log.Address == 0x00F2) {
					dspAddr = log.PreData!.Value;
				}
				else if (log.Address == 0x00F3 && dspAddr < 0x80) {
					SMP.MemAccessLog dspLog = new() {
						Type      = log.Type,
						DSPCycle  = log.DSPCycle,
						Address   = dspAddr,
						ReadData  = log.ReadData,
						PreData   = log.PreData,
						WriteData = log.WriteData,
						PostData  = log.PostData
					};
					
					dspLogs.Add(dspLog);
				}
			}
			else if (log.Address == 0x00F3) {
				SMP.MemAccessLog dspLog = new() {
					Type      = log.Type,
					DSPCycle  = log.DSPCycle,
					Address   = (UInt16) (dspAddr & 0x7F),
					ReadData  = log.ReadData,
					PreData   = log.PreData,
					WriteData = log.WriteData,
					PostData  = log.PostData
				};
					
				dspLogs.Add(dspLog);
			}
		}
		
		return ((IEnumerable<SMP.MemAccessLog>) dspLogs).Reverse().ToArray();
	}
}