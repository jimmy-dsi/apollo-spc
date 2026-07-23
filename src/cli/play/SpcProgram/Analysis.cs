namespace SpcProgram;

using System.Diagnostics;

using Jimbl.Graphics;
using Jimbl.JMath;

using Apollo;
using Jimbl;

using SampleRef   = (byte SampleID, UInt16 Address, UInt16 Length, bool Looped);
using SampleEntry = (UInt16 Start,  UInt16 Loop);

public static class Analysis {
	public class Container: ICloneable {
		public double FadeVolume = 1.0;
		public SampleEntry[] SampleEntries = new SampleEntry[0x100];
		
		public Container Clone() {
			Container c = new();
			
			c.FadeVolume    = FadeVolume;
			c.SampleEntries = (SampleEntry[]) SampleEntries.Clone();
			
			return c;
		}
		
		object ICloneable.Clone() => Clone();
	}
	
	public static void TrackSampleUsage(this Emulator emu) {
		var container = emu.AdditionalState as Container;
		var prevSampleDir = container?.SampleEntries;
		
		if (container is not null) {
			var fadeCycles = (long) (Emulator.MainInstance!.SpcMetadata.LengthInSeconds ?? 12 * 60) * 2048000;
			
			if (emu.DSP.CurrentCycle >= fadeCycles) {
				container.FadeVolume = Math.Pow(Math.Clamp(1 - (emu.DSP.CurrentCycle - fadeCycles) / 20480000.0, 0, 1), 1.5);
			}
			else {
				container.FadeVolume = 1;
			}
			
			emu.PrimaryMixingVol = (float) container.FadeVolume;
		}
		
		var newSampleDir = new SampleEntry[256];
		var sampleBank   = emu.DSP.Register[0x5D];
		
		for (var i = 0; i < 256; i++) {
			var entryAddr   = (sampleBank << 8) + i * 4 & 0xFFFF;
			
			var startAddrLo = emu.DSP.ARAM[entryAddr];
			var startAddrHi = emu.DSP.ARAM[entryAddr + 1];
			var loopAddrLo  = emu.DSP.ARAM[entryAddr + 2];
			var loopAddrHi  = emu.DSP.ARAM[entryAddr + 3];
			
			var startAddr = startAddrLo | startAddrHi << 8;
			var loopAddr  =  loopAddrLo |  loopAddrHi << 8;
			
			newSampleDir[i].Start = (UInt16) startAddr;
			newSampleDir[i].Loop  = (UInt16) loopAddr;
		}
		
		if (prevSampleDir is not null) {
			for (var i = 0; i < 256; i++) {
				// Reset sample usage flag if either the start or loop address had been changed from before
				if (newSampleDir[i].Start != prevSampleDir[i].Start || newSampleDir[i].Loop != prevSampleDir[i].Loop) {
					emu.DSP.ResetSampleUsage((byte) i);
				}
			}
		}
		
		//prevSampleDir = newSampleDir;
	}
	
	public static byte?[] CheckForSampleData(this Emulator snapshot, UInt16 startAddr, UInt16 length, byte maxSamples = 0xFF) {
		var sampleDirectory = snapshot.extractSampleEntries();
		
		var endAddr = Math.Min(startAddr + length - 1, 0xFFFF);
		length = (UInt16) (endAddr + 1 - startAddr);
		
		var aramSlice = snapshot.DSP.ARAM[startAddr .. (endAddr + 1)];
		
		var inSampleArr = aramSlice.Select(_ => (byte?) null).ToArray();
		var candidates  = snapshot.identify(sampleDirectory, startAddr, (UInt16) aramSlice.Length);
		    //candidates  = snapshot.filter(candidates);
		    candidates  = snapshot.partialLengths(candidates, startAddr, length).OrderBy(x => -x.Address).ToArray();
		
		// Process exact address matches first
		for (var i = 0; i < length; i++) {
			var   addr   = startAddr + i;
			byte? sampId = null;

			foreach (var s in candidates) {
				if (addr == s.Address) {
					sampId = s.SampleID;
					break;
				}
			}

			inSampleArr[i] = sampId;
		}
		
		for (var i = 0; i < length; i++) {
			if (inSampleArr[i] is not null) {
				continue;
			}
			
			var   addr   = startAddr + i;
			byte? sampId = null;
			
			foreach (var s in candidates) {
				var sampEnd = s.Address + s.Length - 1;
				
				if (addr >= s.Address && addr <= sampEnd) {
					sampId = s.SampleID;
					break;
				}
			}
			
			inSampleArr[i] = sampId;
		}
		
		return inSampleArr;
	}
	
	public static SampleEntry LookupSampleInfo(this Emulator snapshot, byte sampleID) {
		var directoryStart = snapshot.DSP.Register[0x5D] << 8;
		var addr = (directoryStart + sampleID * 4) & 0xFFFF;
			
		var startLo = snapshot.DSP.ARAM[addr];
		var startHi = snapshot.DSP.ARAM[addr + 1];
		var loopLo  = snapshot.DSP.ARAM[addr + 2];
		var loopHi  = snapshot.DSP.ARAM[addr + 3];
			
		var startAddr = startLo | startHi << 8;
		var loopAddr  =  loopLo |  loopHi << 8;
		
		return (Start: (UInt16) startAddr, Loop: (UInt16) loopAddr);
	}
	
	public static (char, AnsiColor)?[][] DisplayWaveform(Int16[] input,
	                                                     Int16[]? input_2,
	                                                     int canvasWidth,
	                                                     int canvasHeight,
	                                                     double xscale = 1.0,
	                                                     int? cursorIndex = null,
	                                                     int? loopIndex = null,
	                                                     bool isMuted = false)
	{
		if (CliMain.WaveInsideColor is null) {
			CliMain.WaveInsideColor = new(Color.FromLCh(10, 30, 280), isBG: true);
		}
		
		var eqInsideRGB = CliMain.WaveInsideColor.BackgroundRGB! * (isMuted ? 0.5 : 1.0);
		AnsiColor shadowColor = new(Color.FromLCh(30, 30, 280) * (isMuted ? 0.5 : 1.0), isBG: true);
		
		int cellPrecision;
		#if LINUX // By default, Windows terminal emulators do not seem to support unicode char display - make bars more coarse for those
			cellPrecision = 8;
		#else
			cellPrecision = 2;
		#endif
		
		var waveCanvasPos = new int[canvasWidth];
		var waveCanvasNeg = new int[canvasWidth];
		
		var waveCanvasPosMax = new int[canvasWidth];
		var waveCanvasNegMax = new int[canvasWidth];
		
		var xratio = (double) canvasWidth  / input.Length;
		var yratio = (double) canvasHeight * cellPrecision / 0x10000;
		
		int? cursorLine = null;
		
		if (cursorIndex is int ci) {
			cursorLine = JMath.Floor(ci * xratio);
		}
		
		int? loopLine = null;
		
		if (loopIndex is int li && li >= 0) {
			loopLine = JMath.Floor(li * xratio);
		}
		
		var zeroDelay = input.Length == 1;
		
		foreach (var (x, y) in input.Enum()) {
			var cx  = JMath.Floor(xratio * x);
			var cy  = JMath.Round(yratio * y);
			var cy2 = input_2 is null ? 0 : JMath.Round(yratio * input_2[x]);
			
			int val;
			if (input_2 is not null) {
				val = (cy + cy2) / 2;
				
				var max = Math.Max(cy, cy2);
				var min = Math.Min(cy, cy2);
				
				if (max > 0 && max > waveCanvasPosMax[cx]) {
					waveCanvasPosMax[cx] = max;
				}
				
				if (min < 0 && min < waveCanvasNegMax[cx]) {
					waveCanvasNegMax[cx] = min;
				}
			}
			else {
				val = cy;
			}
			
			if (val > 0 && val > waveCanvasPos[cx]) {
				waveCanvasPos[cx] = val;
			}
			else if (val < 0 && val < waveCanvasNeg[cx]) {
				waveCanvasNeg[cx] = val;
			}
		}
		
		if (zeroDelay) {
			for (var i = 1; i < 4; i++) {
				waveCanvasPos[i] = waveCanvasPos[0];
				waveCanvasNeg[i] = waveCanvasNeg[0];
				waveCanvasPosMax[i] = waveCanvasPosMax[0];
				waveCanvasNegMax[i] = waveCanvasNegMax[0];
			}
		}
		
		(char, AnsiColor)?[][] canvas = new (char, AnsiColor)?[canvasHeight][];
		for (var y = 0; y < canvas.Length; y++) {
			canvas[y] = new (char, AnsiColor)?[canvasWidth];
		}
		
		var yZero = canvasHeight / 2;
		
		for (var x = 0; x < canvasWidth; x++) {
			var col_0 = shadowColor;
			var col   = CliMain.HeatMapColor(CliMain.BusSize.Bit8, true, 1.0, 0x2C);
			var col_2 = CliMain.HeatMapColor(CliMain.BusSize.Bit8, true, 1.0, 0x48);
			col   = new(col  .BackgroundRGB! * (isMuted ? 0.5 : 1.0), isBG: true);
			col_2 = new(col_2.BackgroundRGB! * (isMuted ? 0.5 : 1.0), isBG: true);
			
			int yMax_0 = 0, yMin_0 = 0;
			int yMaxRem_0 = 0, yMinRem_0 = 0;
			
			// 0
			if (input_2 is not null) {
				var yMaxIn_0 = yZero * cellPrecision - waveCanvasPosMax[x];
				var yMinIn_0 = yZero * cellPrecision - waveCanvasNegMax[x];
			
				yMaxIn_0 = Math.Clamp(yMaxIn_0, 0, canvasHeight * cellPrecision);
				yMinIn_0 = Math.Clamp(yMinIn_0, 0, canvasHeight * cellPrecision);
			
				yMax_0 = yMaxIn_0 / cellPrecision;
				yMin_0 = yMinIn_0 / cellPrecision;
			
				yMaxRem_0 = yMaxIn_0 % cellPrecision;
				yMinRem_0 = yMinIn_0 % cellPrecision;
			
				colorWavePoint(canvas, eqInsideRGB, col_0, cellPrecision, x, yZero, yMin_0, yMax_0, yMinRem_0, yMaxRem_0);
			}
			
			// 1
			var yMaxIn = yZero * cellPrecision - waveCanvasPos[x];
			var yMinIn = yZero * cellPrecision - waveCanvasNeg[x];
			
			yMaxIn = Math.Clamp(yMaxIn, 0, canvasHeight * cellPrecision);
			yMinIn = Math.Clamp(yMinIn, 0, canvasHeight * cellPrecision);
			
			var yMax = yMaxIn / cellPrecision;
			var yMin = yMinIn / cellPrecision;
			
			var yMaxRem = yMaxIn % cellPrecision;
			var yMinRem = yMinIn % cellPrecision;
			
			var displayShadow = true;
			
			if (input_2 is not null) {
				if (yMax == yMax_0 && yMin == yMin_0) {
					if (Math.Abs(yMaxRem - yMaxRem_0) < cellPrecision / 2 && Math.Abs(yMinRem - yMinRem_0) < cellPrecision / 2) {
						displayShadow = false;
					}
				}
			}
			
			Color back = displayShadow ? col_0.BackgroundRGB! : eqInsideRGB;
			
			colorWavePoint(canvas, input_2 is null ? eqInsideRGB : back, col, cellPrecision, x, yZero, yMin, yMax, yMinRem, yMaxRem);
			
			// 2
			var yMaxIn_2 = yZero * cellPrecision - waveCanvasPos[x] / 2;
			var yMinIn_2 = yZero * cellPrecision - waveCanvasNeg[x] / 2;
			
			yMaxIn_2 = Math.Clamp(yMaxIn_2, 0, canvasHeight * cellPrecision);
			yMinIn_2 = Math.Clamp(yMinIn_2, 0, canvasHeight * cellPrecision);
			
			var yMax_2 = yMaxIn_2 / cellPrecision;
			var yMin_2 = yMinIn_2 / cellPrecision;
			
			var yMaxRem_2 = yMaxIn_2 % cellPrecision;
			var yMinRem_2 = yMinIn_2 % cellPrecision;
			
			if (yMax_2 == yMax) {
				yMax_2    = Math.Min(yZero, yMax + 1);
				yMaxRem_2 = 0;
			}
			
			if (yMin_2 == yMin) {
				yMin_2 = Math.Max(yZero, yMin - 1);
				yMinRem_2 = 0;
			}
			
			colorWavePoint(canvas, col.BackgroundRGB!, col_2, cellPrecision, x, yZero, yMin_2, yMax_2, yMinRem_2, yMaxRem_2);
			var white    = Color.FromRGB(1.0, 1.0, 1.0);
			var offWhite = Color.FromRGB(0.6, 0.6, 1.0);
			var grey     = Color.FromRGB(0.5, 0.5, 0.5);
			
			var bright   = isMuted ? grey : white;
			var bright_2 = isMuted ? grey : offWhite;
			
			var mul_1 = 0.75;
			var mul_2 = 0.88;
			
			var mul_1_com = 1 - mul_1;
			var mul_2_com = 1 - mul_2;
			
			var loopBGCol_1 = eqInsideRGB * mul_1 + bright_2 * mul_1_com;
			var loopBGCol_2 = eqInsideRGB * mul_2 + bright_2 * mul_2_com;
			
			if (loopLine is int LL) {
				if (x >= LL) {
					for (var y = 0; y < canvasHeight; y++) {
						canvas[y][x] ??= (' ', new(eqInsideRGB, isBG: true));
					
						var cc = canvas[y][x]!.Value;
						if (cc.Item2.IsFG) {
							cc.Item2 = x == LL ?
								new(cc.Item2.ForegroundRGB! * mul_1 + bright_2 * mul_1_com)
							:
								new(cc.Item2.ForegroundRGB! * mul_2 + bright_2 * mul_2_com);
						}
						else if (cc.Item2.IsBG) {
							cc.Item2 = x == LL ?
								new(cc.Item2.BackgroundRGB! * mul_1 + bright_2 * mul_1_com, isBG: true)
							:
								new(cc.Item2.BackgroundRGB! * mul_2 + bright_2 * mul_2_com, isBG: true);
						}
						else {
							var colFG = x == LL ?
								cc.Item2.ForegroundRGB! * mul_1 + bright_2 * mul_1_com
							:
								cc.Item2.ForegroundRGB! * mul_2 + bright_2 * mul_2_com;
							
							var colBG = x == LL ?
								cc.Item2.BackgroundRGB! * mul_1 + bright_2 * mul_1_com
							:
								cc.Item2.BackgroundRGB! * mul_2 + bright_2 * mul_2_com;
						
							cc.Item2 = new(colFG, colBG);
						}
					
						canvas[y][x] = cc;
					}
				}
			}
			
			if (cursorLine is int cl && cl == x) {
				for (var y = 0; y < canvasHeight; y++) {
					canvas[y][x] ??= (' ', new(eqInsideRGB, isBG: true));
					
					var cc = canvas[y][x]!.Value;
					if (cc.Item2.IsFG) {
						cc.Item2 = new(bright - cc.Item2.ForegroundRGB!);
					}
					else if (cc.Item2.IsBG) {
						if (cc.Item2.BackgroundRGB! == eqInsideRGB || cc.Item2.BackgroundRGB! == loopBGCol_1 || cc.Item2.BackgroundRGB! == loopBGCol_2) {
							cc.Item1 = '│';
							cc.Item2 = new(bright - cc.Item2.BackgroundRGB!, cc.Item2.BackgroundRGB!);
						}
						else {
							cc.Item2 = new(bright - cc.Item2.BackgroundRGB!, isBG: true);
						}
					}
					else {
						var colFG = cc.Item2.ForegroundRGB!;
						var colBG = cc.Item2.BackgroundRGB!;
						
						if (colFG != eqInsideRGB && colFG != loopBGCol_1 && colFG != loopBGCol_2) {
							colFG = bright - colFG;
						}
						
						if (colBG != eqInsideRGB && colBG != loopBGCol_1 && colBG != loopBGCol_2) {
							colBG = bright - colBG;
						}
						
						cc.Item2 = new(colFG, colBG);
					}
					
					canvas[y][x] = cc;
				}
			}
		}
		
		return canvas;
	}
	
	static void colorWavePoint((char, AnsiColor)?[][] canvas,
	                           Color bgCol,
	                           AnsiColor fgCol,
	                           int cellPrecision,
	                           int x,
	                           int yZero,
	                           int yMin,
	                           int yMax,
	                           int yMinRem,
	                           int yMaxRem)
	{
		for (var y = yMax + 1; y < yZero; y++) {
			canvas[y][x] = (' ', fgCol);
		}
		
		for (var y = yZero; y <= yMin - 1; y++) {
			canvas[y][x] = (' ', fgCol);
		}
			
		var c = '▄';
			
		if (yMax <= yMin) {
			if (yMaxRem == 0) {
				if (yMax != yZero) {
					canvas[yMax][x] = (' ', fgCol);
				}
			}
			else {
				if (cellPrecision == 8) {
					c = yMaxRem switch {
						1 => '▇',
						2 => '▆',
						3 => '▅',
						4 => '▄',
						5 => '▃',
						6 => '▂',
						7 => '▁',
						_ => throw new UnreachableException($"max: {yMaxRem}")
					};
				}
				
				canvas[yMax][x] = (c, new(fgCol.BackgroundRGB!, bgCol));
			}
			
			if (yMinRem > 0) {
				if (cellPrecision == 8) {
					c = yMinRem switch {
						1 => '▇',
						2 => '▆',
						3 => '▅',
						4 => '▄',
						5 => '▃',
						6 => '▂',
						7 => '▁',
						_ => throw new UnreachableException($"min: {yMinRem}")
					};
				}
				
				canvas[yMin][x] = (c, new(bgCol, fgCol.BackgroundRGB!));
			}
		}
	}
	
	static SampleEntry[] extractSampleEntries(this Emulator snapshot) {
		var directoryStart = snapshot.DSP.Register[0x5D] << 8;
		var entries = new SampleEntry[0x100];
		
		for (var i = 0; i < 0x100; i++) {
			var addr = (directoryStart + i * 4) & 0xFFFF;
			
			var startLo = snapshot.DSP.ARAM[addr];
			var startHi = snapshot.DSP.ARAM[addr + 1];
			var  loopLo = snapshot.DSP.ARAM[addr + 2];
			var  loopHi = snapshot.DSP.ARAM[addr + 3];
			
			var startAddr = startLo | startHi << 8;
			var loopAddr  =  loopLo |  loopHi << 8;
			
			entries[i].Start = (UInt16) startAddr;
			entries[i].Loop  = (UInt16)  loopAddr;
		}
		
		return entries;
	}
	
	static SampleRef[] identify(this Emulator snapshot, SampleEntry[] sampleDirectory, UInt16 startAddr, UInt16 length) {
		var used = snapshot.DSP.SampleUsageFlags;
		
		var refBins = new SampleRef?[9];
		List<SampleRef> additionalRefs = [];
		
		var endAddr = (startAddr + length - 1) & 0xFFFF;
		
		foreach (var (id, (start, loop)) in sampleDirectory.Enum()) {
			if (!used[id]) {
				continue;
			}
			
			var isLoop = false;
			
			var addr = start;
			
			for (var _ = 0; _ < 2; _++) {
				if (addr is < 0x200 or > 0xFFF7) {
					continue;
				}
				
				if (addr >= startAddr && addr <= endAddr) {
					additionalRefs.Add((SampleID: (byte) id, Address: addr, Length: 0, Looped: isLoop));
				}
				else if (addr < startAddr) {
					var mod9 = addr % 9;
					if (refBins[mod9] is null || addr > refBins[mod9]!.Value.Address) {
						refBins[mod9] = (SampleID: (byte) id, Address: addr, Length: 0, Looped: isLoop);
					}
				}
				
				addr = loop;
				isLoop = true;
			}
		}
		
		var allRefs = refBins.Where(x => x is not null).Select(x => x!.Value).ToList();
		allRefs.AddRange(additionalRefs);
		
		return allRefs.ToArray();
	}
	
	static SampleRef[] partialLengths(this Emulator snapshot, SampleRef[] candidates, UInt16 startAddr, UInt16 length) {
		var endAddr = startAddr + length - 1;
		
		List<SampleRef> newCandidates = [];
		
		foreach (var s in candidates) {
			var sampLen = 0;
			
			for (var addr = s.Address; addr <= endAddr; addr += 9) {
				if (addr is < 0x200 or > 0xFFF7) {
					sampLen = 0;
					break;
				}
				
				sampLen += 9;
				
				var headerByte = snapshot.DSP.ARAM[addr];
				if ((headerByte & 1) != 0) { // End flag set
					break;
				}
			}
			
			var sampEnd = s.Address + sampLen - 1;
			
			if (sampLen > 0) {
				var case_1 = s.Address >= startAddr && s.Address <= endAddr;
				var case_2 =   sampEnd >= startAddr &&   sampEnd <= endAddr;
				var case_3 = s.Address <  startAddr &&   sampEnd >  endAddr;
				
				if (case_1 || case_2 || case_3) {
					newCandidates.Add((SampleID: s.SampleID,
					                   Address:  s.Address,
					                   Length:   (UInt16) sampLen,
					                   Looped:   s.Looped));
				}
			}
		}
		
		return newCandidates.ToArray();
	}
}