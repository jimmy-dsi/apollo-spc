namespace SpcProgram;

using Apollo;

public static class LibTest {
	public static Emulator Test(string spcFilePath) {
		//Lib.Init();
		
		try {
			Emulator emu = new(setAsMain: true, makeShared: true);
			emu.LoadSpcFile(spcFilePath);
			emu.SMP.LoggingEnabled = true;
			
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
			
			//emu.SMP.State.RAMDisable = true;
			
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
			
			Console.WriteLine($"Title:                   {emu.SpcMetadata   .Title}");
			Console.WriteLine($"Artist:                  {emu.SpcMetadata  .Artist}");
			Console.WriteLine($"Game:                    {emu.SpcMetadata    .Game}");
			Console.WriteLine($"Dumper:                  {emu.SpcMetadata  .Dumper}");
			Console.WriteLine($"Comments:                {emu.SpcMetadata.Comments}");
			Console.WriteLine();
			
			Console.WriteLine($"Month:                   {emu.SpcMetadata.Month}");
			Console.WriteLine($"Day:                     {emu.SpcMetadata  .Day}");
			Console.WriteLine($"Year:                    {emu.SpcMetadata .Year}");
			Console.WriteLine();
			
			Console.WriteLine($"Date Other:              {emu.SpcMetadata.DateOther}");
			Console.WriteLine();
			
			Console.WriteLine($"Length (s):              {emu.SpcMetadata.LengthInSeconds}");
			Console.WriteLine($"Fade Length (ms):        {emu.SpcMetadata .FadeLengthInMS}");
			Console.WriteLine();
			
			Console.WriteLine($"Channels Disabled:       {
				string.Join(' ', emu.SpcMetadata.ChannelsDisabled ?? [false, false, false, false, false, false, false, false])
			}");
			Console.WriteLine();
			
			Console.WriteLine($"Emulator ID:             {emu.SpcMetadata.EmulatorID}");
			Console.WriteLine();
			
			Console.WriteLine($"OST Title:               {emu.SpcMetadata.OSTTitle}");
			Console.WriteLine($"OST Disc:                {emu.SpcMetadata .OSTDisc}");
			Console.WriteLine($"OST Track:               {(emu.SpcMetadata.OSTTrack != null ? string.Join(' ', emu.SpcMetadata.OSTTrack) : null)}");
			Console.WriteLine();
			
			Console.WriteLine($"Publisher:               {emu.SpcMetadata    .Publisher}");
			Console.WriteLine($"Copyright Year:          {emu.SpcMetadata.CopyrightYear}");
			Console.WriteLine();
				
			Console.WriteLine($"Intro Length (T2 steps): {emu.SpcMetadata.IntroLengthInTimer2Steps}");
			Console.WriteLine($"Loop  Length (T2 steps): {emu.SpcMetadata .LoopLengthInTimer2Steps}");
			Console.WriteLine($"End   Length (T2 steps): {emu.SpcMetadata  .EndLengthInTimer2Steps}");
			Console.WriteLine($"Loop Times:              {emu.SpcMetadata               .LoopTimes}");
			Console.WriteLine();
			
			Console.WriteLine($"Mixing Level:            {emu.SpcMetadata.MixingLevel}");
			Console.WriteLine();
			
			Console.WriteLine($"A:   {emu.SPC.State.A :X2}");
			Console.WriteLine($"X:   {emu.SPC.State.X :X2}");
			Console.WriteLine($"Y:   {emu.SPC.State.Y :X2}");
			Console.WriteLine($"YA:  {emu.SPC.State.YA:X4}");
			Console.WriteLine();
			
			Console.WriteLine($"SP:  {emu.SPC.State.SP:X2}");
			Console.WriteLine($"PC:  {emu.SPC.State.PC:X4}");
			Console.WriteLine();
			
			Console.WriteLine($"PSW: {emu.SPC.State.PSW:X2}");
			Console.WriteLine();
			
			Console.WriteLine($"N:   {emu.SPC.State.N}");
			Console.WriteLine($"V:   {emu.SPC.State.V}");
			Console.WriteLine($"P:   {emu.SPC.State.P}");
			Console.WriteLine($"B:   {emu.SPC.State.B}");
			Console.WriteLine($"H:   {emu.SPC.State.H}");
			Console.WriteLine($"I:   {emu.SPC.State.I}");
			Console.WriteLine($"Z:   {emu.SPC.State.Z}");
			Console.WriteLine($"C:   {emu.SPC.State.C}");
			Console.WriteLine();
			
			emu.SPC.State.I = true;
			
			Console.WriteLine($"PSW: {emu.SPC.State.PSW:X2}");
			Console.WriteLine();
			
			emu.SPC.State.I = false;
			
			Console.WriteLine($"PSW: {emu.SPC.State.PSW:X2}");
			Console.WriteLine();
			
			var oldYA = emu.SPC.State.YA;
			emu.SPC.State.YA = 0x4269;
			
			Console.WriteLine($"A:   {emu.SPC.State.A :X2}");
			Console.WriteLine($"Y:   {emu.SPC.State.Y :X2}");
			Console.WriteLine($"YA:  {emu.SPC.State.YA:X4}");
			Console.WriteLine();
			
			emu.SPC.State.YA = oldYA;
			
			Console.WriteLine($"A:   {emu.SPC.State.A :X2}");
			Console.WriteLine($"Y:   {emu.SPC.State.Y :X2}");
			Console.WriteLine($"YA:  {emu.SPC.State.YA:X4}");
			Console.WriteLine();
			
			var lastCycle = emu.DSP.CurrentCycle;
			for (var i = 0; i < 5; i++) {
				if (i == 3) {
					emu.SMP.LoggingEnabled = false;
				}
				
				emu.StepInstruction();
				Console.WriteLine($"Stepped instruction: {lastCycle} -> {emu.DSP.CurrentCycle}");
				Console.WriteLine();
				var logs = emu.SMP.GetAccessLogsDeduped(lastCycle);
				
				foreach (var log in logs) {
					Console.WriteLine($"    Type:       {log    .Type}");
					Console.WriteLine($"    Cycle:      {log.DSPCycle}");
					Console.WriteLine($"    Address:    {log .Address:X4}");
					
					Console.WriteLine($"    Pre-Data:   {(log.PreData   != null ? log.PreData  .Value.ToString("X2") : "")}");
					Console.WriteLine($"    Write-Data: {(log.WriteData != null ? log.WriteData.Value.ToString("X2") : "")}");
					Console.WriteLine($"    Post-Data:  {(log.PostData  != null ? log.PostData .Value.ToString("X2") : "")}");
					Console.WriteLine();
				}
				
				lastCycle = emu.DSP.CurrentCycle;
			}
		
			return emu;
		}
		finally {
			//Lib.Deinit();
		}
	}
}