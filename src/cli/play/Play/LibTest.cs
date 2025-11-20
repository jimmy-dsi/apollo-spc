namespace Play;

using Apollo;

public static class LibTest {
	public static void Test(string spcFilePath) {
		Lib.Init();
		
		try {
			Emulator emu = new(setAsMain: true);
			emu.LoadSpcFile(spcFilePath);
			
			Console.WriteLine($"reset: {emu.DSP.State.Reset}");
			Console.WriteLine($"mute:  {emu.DSP.State.Mute }");
			Console.WriteLine($"readonly echo: {emu.DSP.State.Echo.Readonly}");
			Console.WriteLine($"noise rate:    {emu.DSP.State.NoiseRate}");
			
			Console.WriteLine($"random ARAM: {emu.DSP.ARAM[0x0000]}");
			Console.WriteLine($"random Reg:  {emu.DSP.Register[0x00]}");
			
			Console.WriteLine("SMP regs:");
			Console.Write("    ");
			
			for (ushort a = 0xF0; a < 0x100; a++) {
				var val = emu.SMP.DebugReadByte(a);
				Console.Write(val.ToString("X2"));
				Console.Write(' ');
			}
			
			Console.WriteLine();
			Console.WriteLine();
			
			//emu.SMP.BootROM[0] = 0xFF;
			
			for (var i = 0; i < 0x40; i++) {
				var val = emu.SMP.BootROM[i];
				Console.Write(val.ToString("X2"));
				Console.Write(' ');
				if (i % 16 == 15) {
					Console.WriteLine();
				}
			}
			
			Console.WriteLine();
			
			var page0 = emu.SMP.DebugReadPage(0x0000);
			
			for (var i = 0; i < 0x100; i++) {
				var val = page0[i];
				Console.Write(val.ToString("X2"));
				Console.Write(' ');
				if (i % 16 == 15) {
					Console.WriteLine();
				}
			}
			
			Console.WriteLine();
			
			Console.Write("$F0      ");
			Console.WriteLine(         $"global timer disable: {emu.SMP.State.GlobalTimerDisable}");
			Console.WriteLine($"         ram write enable:     {emu.SMP.State.RAMWriteEnable    }");
			Console.WriteLine($"         ram disable:          {emu.SMP.State.RAMDisable        }");
			Console.WriteLine($"         global timer enable:  {emu.SMP.State.GlobalTimerEnable }");
			Console.WriteLine($"         ram waitstates:       {emu.SMP.State.RAMWaitstates     }");
			Console.WriteLine($"         io waitstates:        {emu.SMP.State .IOWaitstates     }");
			Console.Write("$F1      ");
			Console.WriteLine(         $"timer on flags:       {string.Join(' ', emu.SMP.State.Timer.Select(x => x.Enabled))}");
			Console.WriteLine($"         use boot rom:         {emu.SMP.State.UseBootROM}");
			Console.Write("$F2-$F3  ");
			Console.WriteLine(         $"dsp address:          {emu.SMP.State.DSPAddress:X2}");
			Console.WriteLine($"         dsp data:             {emu.SMP.State.DSPData   :X2}");
			Console.Write("$F4-$F7  ");
			Console.WriteLine(         $"input  ports:         {string.Join(' ', emu.SMP.State.IO .Input.Select(x => x.ToString("X2")))}");
			Console.WriteLine($"         output ports:         {string.Join(' ', emu.SMP.State.IO.Output.Select(x => x.ToString("X2")))}");
			Console.Write("$F8-$F9  ");
			Console.WriteLine(         $"aux:                  {string.Join(' ', emu.SMP.State.Aux.Select(x => x.ToString("X2")))}");
			Console.Write("$FA-$FC  ");
			Console.WriteLine(         $"timer dividers:       {string.Join(' ', emu.SMP.State.Timer.Select(x => x.Divider.ToString("X2")))}");
			Console.Write("$FD-$FF  ");
			Console.WriteLine(         $"timer outputs:        {string.Join(' ', emu.SMP.State.Timer.Select(x => x.Output.ToString("X1")))}");
			Console.WriteLine();
			
			for (var i = 0; i < 3; i++) {
				Console.WriteLine($"timer {i}");
				Console.WriteLine($"         enabled:              {emu.SMP.State.Timer[i].Enabled   }");
				Console.WriteLine($"         divider:              {emu.SMP.State.Timer[i].Divider:X2}");
				Console.WriteLine($"         stage 0:              {emu.SMP.State.Timer[i].Stage0 :X2}");
				Console.WriteLine($"         stage 1:              {emu.SMP.State.Timer[i].Stage1    }");
				Console.WriteLine($"         stage 2:              {emu.SMP.State.Timer[i].Stage2 :X2}");
				Console.WriteLine($"         stage 3:              {emu.SMP.State.Timer[i].Stage3 :X1}");
				Console.WriteLine();
			}
			
			emu.SMP.State.RAMDisable = true;
			
			page0 = emu.SMP.DebugReadPage(0x0000);
			
			for (var i = 0; i < 0x100; i++) {
				var val = page0[i];
				Console.Write(val.ToString("X2"));
				Console.Write(' ');
				if (i % 16 == 15) {
					Console.WriteLine();
				}
			}
			
			Console.WriteLine();
			
			//var cycles = 0L;
			//while (true) {
			//	if (cycles % 2048000 == 0) {
			//		Console.WriteLine($"{cycles / 2048000} seconds processed");
			//	}
			//	emu.StepCycle();
			//	cycles++;
			//}
		}
		finally {
			Lib.Deinit();
		}
	}
}