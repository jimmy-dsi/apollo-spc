namespace SpcProgram;

using Jimbl;

public static partial class CliMain {
	static void showScript700Viewer(EmuDataBuffer buffer) {
		Display.WriteBox([
			"Running?    :",
			"Port In (Q) :",
			"Port In     :",
			"Work 0-3    :",
			"     4-7    :",
			"Cmp Param   :",
			"Wait Until  :",
			"Script Size :",
			"Data Size   :",
			"Cur. Cycle  :",
			"Begin Cycle :",
			"Sync Point  :"
		]);
		
		var x = 14;
		var y = 0;
		
		var s700 = buffer.Script700_State!;
		var smp  = buffer.SMP_State!;
		
		Display.Write($"{s700.IsRunning}", x, y); y++;
		Display.Write($"{s700.InputPorts[0]:X2} {s700.InputPorts[1]:X2} {s700.InputPorts[2]:X2} {s700.InputPorts[3]:X2}", x, y); y++;
		Display.Write($"{smp. InputPorts[0]:X2} {smp .InputPorts[1]:X2} {smp .InputPorts[2]:X2} {smp .InputPorts[3]:X2}", x, y);
		Display.Write($"    Out : ");
		Display.Write($"{smp.OutputPorts[0]:X2} {smp.OutputPorts[1]:X2} {smp.OutputPorts[2]:X2} {smp.OutputPorts[3]:X2}"); y++;
		
		Display.Write($"{s700.Work[0]:X8} {s700.Work[1]:X8} {s700.Work[2]:X8} {s700.Work[3]:X8}", x, y); y++;
		Display.Write($"{s700.Work[4]:X8} {s700.Work[5]:X8} {s700.Work[6]:X8} {s700.Work[7]:X8}", x, y); y++;
		Display.Write($"{s700 .Cmp[0]:X8} {s700. Cmp[1]:X8}", x, y); y++;
		
		if (s700.WaitUntil > 0) {
			Display.Write($"{s700.WaitUntil:X16} ({s700.WaitUntil})".PadRight(x + 36), x, y);
		}
		else {
			Display.Write($"---------------- (none)".PadRight(x + 36), x, y);
		}
		y++;
		
		Display.Write($"{s700.BytecodeLength:X6}", x, y); y++;
		Display.Write($"{s700.DataLength:X6} (PC={s700.PC:X6} SP={s700.SP:X2} ST={s700.SPTop:X2})", x, y); y++;
		
		Display.Write($"{s700.CurCycle  :X16} ({s700  .CurCycle})".PadRight(x + 36), x, y); y++;
		Display.Write($"{s700.BeginCycle:X16} ({s700.BeginCycle})".PadRight(x + 36), x, y); y++;
		Display.Write($"{s700.SyncPoint :X16} ({s700 .SyncPoint})".PadRight(x + 36), x, y); y++;
		
		if (heatMapEnabled) {
			//x += 36;
			y = 1;
			
			Display.Highlight(11, x, y, col: heatMapZero());
			
			for (var i = 0; i < 4; i++) {
				Display.Highlight(2, x + 3 * i, y, col: HeatMapColor(BusSize.Bit8, signed: false, scale: 1, s700.InputPorts[i]).BackgroundRGB);
			}
			y++;
			
			Display.Highlight(11, x, y, col: heatMapZero());
			
			for (var i = 0; i < 4; i++) {
				Display.Highlight(2, x + 3 * i, y, col: HeatMapColor(BusSize.Bit8, signed: false, scale: 1, smp.InputPorts[i]).BackgroundRGB);
			}
			
			Display.Highlight(11, x + 21, y, col: heatMapZero());
			
			for (var i = 0; i < 4; i++) {
				Display.Highlight(2, x + 3 * i + 21, y, col: HeatMapColor(BusSize.Bit8, signed: false, scale: 1, smp.OutputPorts[i]).BackgroundRGB);
			}
			y++;
			
			Display.Highlight(35, x, y, col: heatMapZero());
			
			for (var i = 0; i < 4; i++) {
				displayHeatMap32(s700.Work[i], x + 9 * i, y);
			}
			y++;
			
			Display.Highlight(35, x, y, col: heatMapZero());
			
			for (var i = 4; i < 8; i++) {
				displayHeatMap32(s700.Work[i], x + 9 * (i % 4), y);
			}
			y++;
			
			Display.Highlight(17, x, y, col: heatMapZero());
			
			for (var i = 0; i < 2; i++) {
				displayHeatMap32(s700.Cmp[i], x + 9 * i, y);
			}
			y++;
			
			if (s700.WaitUntil > 0) {
				displayHeatMap64((ulong) s700.WaitUntil.SafeSigned(), x, y);
			}
			y++;
			
			displayHeatMap24((uint) s700.BytecodeLength, x, y); y++;
			
			displayHeatMap24((uint) s700.DataLength, x,      y);
			displayHeatMap24(       s700.PC,         x + 11, y);
			
			Display.Highlight(2, x + 21, y, col: HeatMapColor(BusSize.Bit8, signed: false, scale: 1, s700.SP   ).BackgroundRGB);
			Display.Highlight(2, x + 27, y, col: HeatMapColor(BusSize.Bit8, signed: false, scale: 1, s700.SPTop).BackgroundRGB);
			y++;
			
			displayHeatMap64((ulong) s700.CurCycle.  SafeSigned(), x, y); y++;
			displayHeatMap64((ulong) s700.BeginCycle.SafeSigned(), x, y); y++;
			displayHeatMap64((ulong) s700.SyncPoint .SafeSigned(), x, y); y++;
		}
		
		showColorCoding();
	}
}