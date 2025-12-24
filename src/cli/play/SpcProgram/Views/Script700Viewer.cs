using Jimbl;

namespace SpcProgram;

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
			x += 36;
			y = 1;
			
			for (var i = 0; i < 4; i++) {
				Display.Write("  ", x + 2 * i, y, col: heatMapColor(BusSize.Bit8, signed: false, scale: 1, s700.InputPorts[i]));
			}
			y++;
			
			for (var i = 0; i < 4; i++) {
				Display.Write("  ", x + 2 * i, y, col: heatMapColor(BusSize.Bit8, signed: false, scale: 1, smp.InputPorts[i]));
			}
			
			for (var i = 0; i < 4; i++) {
				Display.Write("  ", x + 2 * i + 8, y, col: heatMapColor(BusSize.Bit8, signed: false, scale: 1, smp.OutputPorts[i]));
			}
			y++;
			
			for (var i = 0; i < 4; i++) {
				Display.Write("  ", x + 2 * i, y, col: heatMapColor(BusSize.Bit32, signed: false, scale: 1, s700.Work[i]));
			}
			y++;
			
			for (var i = 4; i < 8; i++) {
				Display.Write("  ", x + 2 * (i % 4), y, col: heatMapColor(BusSize.Bit32, signed: false, scale: 1, s700.Work[i]));
			}
			y++;
			
			for (var i = 0; i < 2; i++) {
				Display.Write("  ", x + 2 * i, y, col: heatMapColor(BusSize.Bit32, signed: false, scale: 1, s700.Cmp[i]));
			}
			y++;
			
			if (s700.WaitUntil > 0) {
				Display.Write("  ", x, y, col: heatMapColor(BusSize.Bit64, signed: false, scale: 1, s700.WaitUntil.SafeSigned()));
			}
			y++;
			
			Display.Write("  ", x, y, col: heatMapColor(BusSize.Bit32, signed: false, scale: 256, s700.BytecodeLength)); y++;
			
			Display.Write("  ", x, y, col: heatMapColor(BusSize.Bit32, signed: false, scale: 256, s700.DataLength));
			Display.Write("  ", col: heatMapColor(BusSize.Bit32, signed: false, scale: 256, s700.PC));
			Display.Write("  ", col: heatMapColor(BusSize.Bit8,  signed: false, scale:   1, s700.SP));
			Display.Write("  ", col: heatMapColor(BusSize.Bit8,  signed: false, scale:   1, s700.SPTop));
			y++;
			
			Display.Write("  ", x, y, col: heatMapColor(BusSize.Bit64, signed: false, scale: 1, s700.CurCycle  .SafeSigned())); y++;
			Display.Write("  ", x, y, col: heatMapColor(BusSize.Bit64, signed: false, scale: 1, s700.BeginCycle.SafeSigned())); y++;
			Display.Write("  ", x, y, col: heatMapColor(BusSize.Bit64, signed: false, scale: 1, s700.SyncPoint .SafeSigned())); y++;
		}
		
		showColorCoding();
	}
}