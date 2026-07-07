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
		public SampleEntry[] SampleEntries = new SampleEntry[0x100];
		
		public Container Clone() {
			Container c = new();
			c.SampleEntries = (SampleEntry[]) SampleEntries.Clone();
			return c;
		}
		
		object ICloneable.Clone() => Clone();
	}
	
	public static void TrackSampleUsage(this Emulator emu) {
		var container = emu.AdditionalState as Container;
		var prevSampleDir = container?.SampleEntries;
		
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
	
	public static (char, AnsiColor)?[][] DisplayWaveform(Int16[] input, int canvasWidth, int canvasHeight, double xscale = 1.0, int? cursorIndex = null) {
		if (CliMain.WaveInsideColor is null) {
			CliMain.WaveInsideColor = new(Color.FromLCh(10, 30, 280), isBG: true);
		}
		
		var eqInsideRGB = CliMain.WaveInsideColor.BackgroundRGB!;
		
		int cellPrecision;
		#if LINUX // By default, Windows terminal emulators do not seem to support unicode char display - make bars more coarse for those
			cellPrecision = 8;
		#else
			cellPrecision = 2;
		#endif
		
		var waveCanvasPos = new int[canvasWidth];
		var waveCanvasNeg = new int[canvasWidth];
		
		var xratio = (double) canvasWidth  / input.Length;
		var yratio = (double) canvasHeight * cellPrecision / 0x8000;
		
		int? cursorLine = null;
		
		if (cursorIndex is int ci) {
			cursorLine = JMath.Floor(ci * xratio);
		}
		
		foreach (var (x, y) in input.Enum()) {
			var cx = JMath.Floor(xratio * x);
			var cy = JMath.Round(yratio * y);
			
			if (cy > 0 && cy > waveCanvasPos[cx]) {
				waveCanvasPos[cx] = cy;
			}
			else if (cy < 0 && cy < waveCanvasNeg[cx]) {
				waveCanvasNeg[cx] = cy;
			}
		}
		
		(char, AnsiColor)?[][] canvas = new (char, AnsiColor)?[canvasHeight][];
		for (var y = 0; y < canvas.Length; y++) {
			canvas[y] = new (char, AnsiColor)?[canvasWidth];
		}
		
		var yZero = canvasHeight / 2;
		
		for (var x = 0; x < canvasWidth; x++) {
			if (cursorLine is int cl && cl == x) {
				for (var y = 0; y < canvasHeight; y++) {
					canvas[y][x] = (' ', AnsiColor.BGWhite);
				}
				
				continue;
			}
			
			var yMaxIn = yZero * cellPrecision - waveCanvasPos[x];
			var yMinIn = yZero * cellPrecision - waveCanvasNeg[x];
			
			yMaxIn = Math.Max(yMaxIn, 0);
			yMinIn = Math.Max(yMinIn, 0);
			
			var yMax = Math.Clamp(yMaxIn, 0, (canvasHeight - 1) * cellPrecision) / cellPrecision;
			var yMin = Math.Clamp(yMinIn, 0, (canvasHeight - 1) * cellPrecision) / cellPrecision;
			
			var yMaxRem = yMaxIn % cellPrecision;
			var yMinRem = yMinIn % cellPrecision;
			
			var col = CliMain.HeatMapColor(CliMain.BusSize.Bit8, true, 1.0, 0x40);
			
			for (var y = yMax + 1; y < yMin; y++) {
				canvas[y][x] = (' ', col);
			}
			
			var c = '▄';
			
			if (yMax < yMin) {
				if (yMaxRem == 0) {
					canvas[yMax][x] = (' ', col);
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
				
					canvas[yMax][x] = (c, new(col.BackgroundRGB!, eqInsideRGB));
				}
			
				if (yMinRem == 0) {
					canvas[yMin][x] = (' ', new(eqInsideRGB, isBG: true));
				}
				else {
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
				
					canvas[yMin][x] = (c, new(eqInsideRGB, col.BackgroundRGB!));
				}
			}
		}
		
		return canvas;
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