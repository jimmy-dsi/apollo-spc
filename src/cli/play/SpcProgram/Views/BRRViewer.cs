namespace SpcProgram;

using Jimbl.Graphics;
using Jimbl.JMath;

using Apollo;
using Jimbl;

public static partial class CliMain {
	static void showBRRViewer(EmuDataBuffer buffer) {
		if (EQInsideColor is null) {
			EQInsideColor = HeatMapColor(BusSize.Bit8, false, 1, 0);
		}
		
		var eqInsideRGB = EQInsideColor.BackgroundRGB! * 0.75 + (0.1, 0.1, 0.1);
		
		Display.UseBlending = false;
		
		var dirStart = buffer.DSP_State!.SourceStartPage << 8;
		var ds = buffer.DSP_DebugState!;
		
		var voiceOnStates = PrimaryEmu.MainVoiceOnStates;
		
		for (var c = 0; c < 8; c++) {
			var muted = !voiceOnStates[c];
			
			var cx = c % 4;
			var cy = c / 4;
			
			var v = ds.Voice[c];
			
			var scrn = v.CurrentSRCN;
			
			var entryStart = dirStart + scrn * 4;
			var brrStart   = (UInt16) (buffer.ARAM_Data![entryStart]     | buffer.ARAM_Data![entryStart + 1] << 8);
			var loopStart  = (UInt16) (buffer.ARAM_Data![entryStart + 2] | buffer.ARAM_Data![entryStart + 3] << 8);
			
			var brrOffset = v.BRRAddress < brrStart ?
					(v.BRRAddress + 0x1_0000 - brrStart) / 9 * 16 + (v.BRROffset - 1) * 2
				: 
					(v.BRRAddress - brrStart) / 9 * 16 + (v.BRROffset - 1) * 2;
			
			var brrLoopOffset = -1;
			var loopOffset    = -1;
			
			var (decoded, looped) = DSP.DecodeBrrFromBuffer(buffer.ARAM_Data!, brrStart, 0x10_0000);
			if (looped) {
				brrLoopOffset = loopStart - brrStart;
				loopOffset = brrLoopOffset / 9 * 16;
			}
			
			if (loopOffset >= 0 && (loopOffset >= decoded.Length || brrLoopOffset % 9 != 0)) {
				loopOffset = decoded.Length;
				
				var (decodedLoop, _) = DSP.DecodeBrrFromBuffer(buffer.ARAM_Data!, loopStart, 0x10_0000, decoded[^1], decoded[^2]);
				decoded = decoded.Concat(decodedLoop).ToArray();
			
				if (v.CurrentLoopIter > 0) {
					brrOffset = v.BRRAddress < loopStart ?
						(v.BRRAddress + 0x1_0000 - loopStart) / 9 * 16 + (v.BRROffset - 1) * 2
					: 
						(v.BRRAddress - loopStart) / 9 * 16 + (v.BRROffset - 1) * 2;
					
					brrOffset += loopOffset;
				}
			}
			
			brrOffset += v.BRRSubOffset / 0x1000;
			
			AnsiColor?     fgTextWhite   = muted ? AnsiColor.DarkGrey : null;
			AnsiColor.Code fgTextYellow  = muted ? AnsiColor.Code.DarkGrey : AnsiColor.Code.Yellow;
			AnsiColor.Code fgTextMagenta = muted ? AnsiColor.Code.DarkGrey : AnsiColor.Code.Magenta;
			
			Display.Write($"V{c + 1}                          ", 33 * cx, 17 * cy, col: fgTextWhite);
				
			var bgCol = eqInsideRGB;
			if (muted) {
				bgCol *= 0.5;
			}
			
			if (v.EnvLevel > 0 || v.KeyOnDelay is > 0 and < 5) {
				AnsiColor col;
				
				if (muted) {
					col = AnsiColor.DarkGrey;
				}
				else if (v.NoiseOn) {
					col = AnsiColor.Yellow;
				}
				else if (v.KeyOnDelay is > 0 and < 5) {
					col = AnsiColor.Cyan;
				}
				else {
					col = AnsiColor.Green;
				}
				
				Display.Write("■", 33 * cx + 3, 17 * cy, col: col);
			}
			else if (v.KeyOnDelay == 5) {
				AnsiColor col;
				
				if (muted) {
					col = AnsiColor.DarkGrey;
				}
				else {
					col = AnsiColor.Blue;
				}
				
				Display.Write("■", 33 * cx + 3, 17 * cy, col: col);
				brrOffset = -1;
			}
			else {
				Display.Write("■", 33 * cx + 3, 17 * cy, col: new(bgCol));
				brrOffset = -1;
			}
			
			if (v.NoiseOn) {
				Display.Write("Noise", 33 * cx + 17, 11 + 17 * cy, col: new(fgTextYellow, isBold: true));
				brrOffset = -1;
			}
			else {
				Display.Write("     ", 33 * cx + 17, 11 + 17 * cy, col: new(fgTextYellow, isBold: true));
			}
			
			var pitch = buffer.DSP_Voice![c].Pitch;
			
			Display.Write($"P = {pitch:X4}       ", 33 * cx + 17, 12 + 17 * cy, col: fgTextWhite);
			
			if (v.PitchModOn) {
				Display.Write("PitchMod",            33 * cx + 24, 11 + 17 * cy, col: new(fgTextMagenta, isBold: true));
				Display.Write($"[{v.TruePitch:X4}]", 33 * cx + 26, 12 + 17 * cy, col: new(fgTextMagenta, isBold: true));
			}
			else {
				Display.Write("        ",            33 * cx + 24, 11 + 17 * cy, col: new(fgTextYellow, isBold: true));
			}
			
			Display.Write($"= {pitch:X4}", 33 * cx + 19, 12 + 17 * cy, col: fgTextWhite);
			
			Display.Write($"L", 33 * cx + 2, 11 + 17 * cy, col: fgTextWhite);
			Display.Write($"R", 33 * cx + 2, 12 + 17 * cy, col: fgTextWhite);
			
			var volL = buffer.DSP_Voice![c].VolumeLeft;
			var volR = buffer.DSP_Voice![c].VolumeRight;
			var env  = v.EnvLevel;
			
			var leftBarLen  = JMath.Round(Math.Abs(volL) / 8.0);
			var rightBarLen = JMath.Round(Math.Abs(volR) / 8.0);
			var envBarLen   = JMath.Round(env / 128.0);
			
			if (volL > 0 &&  leftBarLen == 0) leftBarLen  = 1;
			if (volR > 0 && rightBarLen == 0) rightBarLen = 1;
			if (env  > 0 &&   envBarLen == 0) envBarLen   = 1;
			
			//Display.Write($"V", 33 * cx + 16, 14 * cy);
			var refColor = HeatMapColor(BusSize.Bit8, signed: false, scale: 1, 0xFF).BackgroundRGB!;
			
			for (var i = 0; i < 12; i++) {
				var i2 = i * 2 / 3;
				Color fgCol;
				
				var interp = (8 - (i2 / 2.0 + 4) ) / (4 + 1);
				interp = Math.Pow(interp,  2);
				
				fgCol = HeatMapColor(BusSize.Bit8, signed: true,  scale: 1, heatValues[i2 + 8]).BackgroundRGB!;
				fgCol = refColor.Blend(fgCol, 1 - interp, Color.Space.LCh);
				
				fgCol *= (i * 2.0 / 3.0 * 0.75 + 10) / 20.0;
				if (muted) {
					fgCol *= 0.5;
				}
				
				//if (leftBarLen > i && rightBarLen > i) {
				//	Display.Write($"─", 33 * cx + 5 + i, 13 + 16 * cy, col: new(AnsiColor.Code.Black, fgCol));
				//}
				//else if (leftBarLen > i) {
				//	Display.Write($"▀", 33 * cx + 5 + i, 13 + 16 * cy, col: new(fgCol, bgCol));
				//}
				//else if (rightBarLen > i) {
				//	Display.Write($"▄", 33 * cx + 5 + i, 13 + 16 * cy, col: new(fgCol, bgCol));
				//}
				//else {
				//	Display.Write($"─", 33 * cx + 5 + i, 13 + 16 * cy, col: new(AnsiColor.Code.Black, bgCol));
				//}
				
				Display.Write($"■", 33 * cx + 4 + i, 11 + 17 * cy, col: new(bgCol));
				Display.Write($"■", 33 * cx + 4 + i, 12 + 17 * cy, col: new(bgCol));
				
				if (leftBarLen > i) {
					Display.Write($"■", 33 * cx + 4 + i, 11 + 17 * cy, col: new(fgCol));
				}
				
				if (rightBarLen > i) {
					Display.Write($"■", 33 * cx + 4 + i, 12 + 17 * cy, col: new(fgCol));
				}
				
				if (envBarLen > i) {
					Display.Write($"■", 33 * cx + 5 + i, 17 * cy, col: new(fgCol));
				}
				else {
					Display.Write($"■", 33 * cx + 5 + i, 17 * cy, col: new(bgCol));
				}
			}
			
			var srcn = buffer.DSP_Voice![c].Source;
			
			displayWaveform(decoded, 1 + 33 * cx, 1 + 17 * cy, 32, 10, brrOffset, loopPos: loopOffset, isMuted: muted);
			Display.Write($"{srcn:X2}", 31 + 33 * cx, 10 + 17 * cy, col: new(fgTextYellow));
		}
	}
}