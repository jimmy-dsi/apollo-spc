namespace SpcProgram;

using Jimbl.Graphics;

public static partial class CliMain {
	static double[] timerFreqs = [
		8000, 8000, 64000
	];
	
	static void showSMPViewer(EmuDataBuffer buffer) {
		var smp = buffer.SMP_State!;
		var dsp = buffer.DSP_RegisterMem!;
		
		var boxX = 1;
		var boxY = 0;
		
		var firstLine = "┌───────────────── 00F0 ────────────────┐";
		var boxWidth  = firstLine.Length;
		
		Display.WriteBox(
			[
				firstLine,
				"│  Timers disabled                      │",
				"│  RAM writes enabled                   │",
				"│  RAM disabled                         │",
				"│  Timers enabled                       │",
				"│  RAM wait states                      │",
				"│  Internal wait states                 │",
				"├───────────────── 00F1 ────────────────┤",
				"│  Boot ROM enabled                     │",
				"├────────────── 00F2-00F3 ──────────────┤",
				"│  Current DSP address                  │",
				"│  Current DSP data                     │",
				"├────────────── 00F4-00F7 ──────────────┤",
				"│               APU<-5A22    APU->5A22  │",
				"│  IO port 0                            │",
				"│  IO port 1                            │",
				"│  IO port 2                            │",
				"│  IO port 3                            │",
				"├────────────── 00F8-00F9 ──────────────┤",
				"│  Auxiliary port 0                     │",
				"│  Auxiliary port 1                     │",
				"├────────────── 00FA-00FC ──────────────┤",
				"│  Timer 0 period                       │",
				"│  Timer 1 period                       │",
				"│  Timer 2 period                       │",
				"├────────────── 00FD-00FF ──────────────┤",
				"│  Timer 0 output                       │",
				"│  Timer 1 output                       │",
				"│  Timer 2 output                       │",
				"└───────────────────────────────────────┘",
			],
			boxX, boxY
		);
		
		Display.Write("               APU<-5A22    APU->5A22  ", boxX + 1, boxY + 13, col: AnsiColor.Magenta);
		var headerColor = AnsiColor.Green;
		
		Display.Write(" 00F0 ",      boxX + 18, boxY,      col: headerColor);
		Display.Write(" 00F1 ",      boxX + 18, boxY +  7, col: headerColor);
		Display.Write(" 00F2-00F3 ", boxX + 15, boxY +  9, col: headerColor);
		Display.Write(" 00F4-00F7 ", boxX + 15, boxY + 12, col: headerColor);
		Display.Write(" 00F8-00F9 ", boxX + 15, boxY + 18, col: headerColor);
		Display.Write(" 00FA-00FC ", boxX + 15, boxY + 21, col: headerColor);
		Display.Write(" 00FD-00FF ", boxX + 15, boxY + 25, col: headerColor);
		
		Display.WriteBox(
			[
				"┌─────────────── Timer 0 ───────────────┐",
				"│  Enabled                              │",
				"│  Internal counter 1                   │",
				"│  Internal counter 2                   │",
				"├─────────────── Timer 1 ───────────────┤",
				"│  Enabled                              │",
				"│  Internal counter 1                   │",
				"│  Internal counter 2                   │",
				"├─────────────── Timer 2 ───────────────┤",
				"│  Enabled                              │",
				"│  Internal counter 1                   │",
				"│  Internal counter 2                   │",
				"└───────────────────────────────────────┘",
			],
			boxX + boxWidth + 2, boxY
		);
		
		Display.Write(" Timer 0 ", boxX + boxWidth + 2 + 16, boxY,     col: headerColor);
		Display.Write(" Timer 1 ", boxX + boxWidth + 2 + 16, boxY + 4, col: headerColor);
		Display.Write(" Timer 2 ", boxX + boxWidth + 2 + 16, boxY + 8, col: headerColor);
		
		int valueRegionX = 0, y = 0;
			
		void writeBool(bool value, int alignment = 15) {
			if (alignment == 15) {
				Display.Write($"{value,15}", valueRegionX, y, col: value ? AnsiColor.Cyan : AnsiColor.Yellow); y++;
			}
			else {
				Display.Write($"{value,10}", valueRegionX, y, col: value ? AnsiColor.Cyan : AnsiColor.Yellow); y++;
			}
		}
		
		{
			valueRegionX = boxX + boxWidth - 18;
			y            = boxY + 1;
		
			// Test register values
			writeBool(smp.GlobalTimerDisable);
			writeBool(smp    .RAMWriteEnable);
			writeBool(smp        .RAMDisable);
			writeBool(smp .GlobalTimerEnable);
			Display.Write($"{smp.RAMWaitstates,15}", valueRegionX, y); y++;
			Display.Write($"{smp.IOWaitstates ,15}", valueRegionX, y); y++;
			y++;
		
			// Control
			writeBool(smp.UseBootROM);
			y++;
		
			// DSP
			Display.Write($"{smp.DSPAddress     ,15:X2}", valueRegionX, y); y++;
			if (heatMapEnabled) {
				Display.Write("  ", valueRegionX + 10, y - 1, col: HeatMapColor(BusSize.Bit8, signed: false, scale: 2.0, smp.DSPAddress));
			}
			
			var data = dsp[smp.DSPAddress];
			Display.Write($"{data,15:X2}", valueRegionX, y); y++;
			if (heatMapEnabled) {
				Display.Write("  ", valueRegionX + 10, y - 1, col: HeatMapColor(BusSize.Bit8, signed: false, scale: 1.0, data));
			}
			
			y++;
		
			// IO ports
			y++;
			for (var i = 0; i < 4; i++) {
				var input  = smp.InputPorts[i];
				var output = smp.OutputPorts[i];
				
				var io = $"{input:X2}           {output:X2}";
				Display.Write($"{io,15}", valueRegionX, y); y++;
				
				if (heatMapEnabled) {
					Display.Write("  ", valueRegionX -  3, y - 1, col: HeatMapColor(BusSize.Bit8, signed: false, scale: 1.0,  input));
					Display.Write("  ", valueRegionX + 10, y - 1, col: HeatMapColor(BusSize.Bit8, signed: false, scale: 1.0, output));
				}
			}
			y++;
		
			// Aux ports
			Display.Write($"{smp.Aux[0],15:X2}", valueRegionX, y); y++;
			if (heatMapEnabled) {
				Display.Write("  ", valueRegionX + 10, y - 1, col: HeatMapColor(BusSize.Bit8, signed: false, scale: 1.0, smp.Aux[0]));
			}
			
			Display.Write($"{smp.Aux[1],15:X2}", valueRegionX, y); y++;
			if (heatMapEnabled) {
				Display.Write("  ", valueRegionX + 10, y - 1, col: HeatMapColor(BusSize.Bit8, signed: false, scale: 1.0, smp.Aux[1]));
			}
			
			y++;
		
			// Timer periods
			for (var i = 0; i < 3; i++) {
				var div = smp.Timer[i].Divider;
				var val = $"[{timerFreqs[i] / div:F2} Hz]";
				Display.Write($"{val,12}", valueRegionX - 3,  y, col: AnsiColor.BrightBlue);
				Display.Write($"{div:X2}", valueRegionX + 13, y); y++;
				
				if (heatMapEnabled) {
					Display.Write("  ", valueRegionX + 10, y - 1, col: HeatMapColor(BusSize.Bit8, signed: false, scale: 1.0, div));
				}
			}
			y++;
		
			// Timer outputs
			for (var i = 0; i < 3; i++) {
				var dt = smp.Timer[i].Output & 0xF;
				
				Display.Write($"{dt,15:X1}", valueRegionX, y); y++;
				if (heatMapEnabled) {
					Display.Write("  ", valueRegionX + 10, y - 1, col: HeatMapColor(BusSize.Bit8, signed: false, scale: 16.0, dt));
				}
			}
			y++;
		}

		{
			valueRegionX = boxX + boxWidth * 2 + 2 - 13;
			y            = boxY + 1;
		
			// Timer periods
			for (var i = 0; i < 3; i++) {
				var div     = smp.Timer[i].Divider;
				var target  = (int) (2048000 / timerFreqs[i]);
				
				var stage_1_val = smp.Timer[i].Stage0 + target * smp.Timer[i].Stage1 / 2;
				var stage_2_val = smp.Timer[i].Stage2;
				
				var stage_1 = $"{stage_1_val:X2} / {target:X3}";
				var stage_2 = $"{stage_2_val:X2} /  {div:X2}";
				
				writeBool(smp.Timer[i].Enabled, 10);
				
				Display.Write($"{stage_1,10}", valueRegionX, y); y++;
				if (heatMapEnabled) {
					Display.Write("  ", valueRegionX - 1, y - 1, col: HeatMapColor(BusSize.Bit8, signed: false, scale: 256.0 / target, stage_1_val));
				}
				
				Display.Write($"{stage_2,10}", valueRegionX, y); y++;
				if (heatMapEnabled) {
					var d = div == 255 ? 256 : div == 0 ? 1 : div;
					Display.Write("  ", valueRegionX - 1, y - 1, col: HeatMapColor(BusSize.Bit8, signed: false, scale: 256.0 / d, stage_2_val));
				}
				
				y++;
			}
		}
		
		Display.Write("MMIO region", boxWidth + 2, 13);
		
		MemCellProperties[]? properties = null;
		
		memDisplayRows(
			BusSize.Bit16,
			0xF,
			0xF,
			buffer.SMP_BusData![0x1F0..],
			bootRomEnabled: smp.UseBootROM,
			readDisabled: smp.RAMDisable,
			writeDisabled: smp.RAMDisable || smp.RAMWriteEnable,
			useHeatMap: heatMapEnabled,
			xOffset: boxWidth + 1,
			yOffset: 14
		);
		
		Display.Write("FFC0-FFFF region", boxWidth + 2, 15);
		if (smp.UseBootROM) {
			Display.Write("[Boot ROM]", boxWidth + 19, 15, col: AnsiColor.Magenta);
		}
		else {
			Display.Write("[ARAM]    ", boxWidth + 19, 15, col: AnsiColor.Cyan);
			
			var echoStartAddr = buffer.DSP_State!.EchoStartPage << 8;
			var echoDelaySize = buffer.DSP_State!.EchoDelay * 0x800;
		
			var dirStartAddr = buffer.DSP_State!.SourceStartPage << 8;
		
			if (heatMapEnabled && heatMapMemMode == HeatMapMode.TypeAware) {
				if (echoStartAddr + echoDelaySize >= 0x1_0000) {
					properties = new MemCellProperties[0x40];
			
					for (var i = 0; i < 0x40; i++) {
						properties[i].DataSize = BusSize.Bit16;
						properties[i].Signed   = true;
					}
				}
				else if (dirStartAddr >= 0xFC00) {
					properties = new MemCellProperties[0x40];
			
					for (var i = 0; i < 0x40; i++) {
						properties[i].DataSize = BusSize.Bit16;
						properties[i].Signed   = false;
					}
				}
			}
		}
		
		memDisplayRows(
			BusSize.Bit16,
			0xFFC,
			0xFFF,
			buffer.SMP_BusData![0xC0..],
			memCellProperties: properties,
			bootRomEnabled: smp.UseBootROM,
			readDisabled: smp.RAMDisable,
			writeDisabled: smp.RAMDisable || smp.RAMWriteEnable,
			useHeatMap: heatMapEnabled,
			xOffset: boxWidth + 1,
			yOffset: 16
		);
		
		Display.Write("  ", boxWidth + 1, 14);
		
		for (var i = 0; i < 4; i++) {
			Display.Write("  ", boxWidth + 1, 16 + i);
		}
		
		Display.Write("DSP map", boxWidth + 2, 21);
		showDSPMem(buffer, smp.DSPAddress, boxWidth + 3, 22);
	}
}