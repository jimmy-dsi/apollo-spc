namespace SpcProgram;

using Apollo;

public class EmuDataContainer {
	public EmuDataBuffer? Buffer { get; set; } = null;
	
	public EmuDataContainer(EmuDataBuffer? buffer) {
		Buffer = buffer;
	}
}

public class EmuDataBuffer: ICloneable {
	public class DSP2State: ICloneable {
		public sbyte  MainVolumeLeft  { get; internal set; }
		public sbyte  MainVolumeRight { get; internal set; }
		public sbyte  EchoVolumeLeft  { get; internal set; }
		public sbyte  EchoVolumeRight { get; internal set; }
		
		public sbyte  EchoFeedback    { get; internal set; }
		public byte   EchoStartPage   { get; internal set; }
		public byte   SourceStartPage { get; internal set; }
		public byte   EchoDelay       { get; internal set; }
		
		public byte   NoiseClock      { get; internal set; }
		public bool   ReadonlyEcho    { get; internal set; }
		public bool   Mute            { get; internal set; }
		public bool   Reset           { get; internal set; }
		
		public sbyte[] FIR            { get; internal set; } = new sbyte[8];
		public DSPVoice2[] Voice      { get; internal set; } = new DSPVoice2[8];
		
		public DSP2State Clone() {
			var clone = (DSP2State) MemberwiseClone();
			
			clone.FIR   = FIR.ToArray();
			clone.Voice = new DSPVoice2[8];
			
			for (var v = 0; v < 8; v++) {
				clone.Voice[v] = Voice[v].Clone();
			}
			
			return clone;
		}
		
		object ICloneable.Clone() => Clone();
	}
	
	public class DSP3State: ICloneable {
		public DSPVoice3[] Voice { get; internal set; } = new DSPVoice3[8];
		
		public DSP3State Clone() {
			var clone = (DSP3State) MemberwiseClone();
			
			clone.Voice = new DSPVoice3[8];
			
			for (var v = 0; v < 8; v++) {
				clone.Voice[v] = Voice[v].Clone();
			}
			
			return clone;
		}
		
		object ICloneable.Clone() => Clone();
	}
	
	public class DSPVoice1: ICloneable {
		public sbyte  VolumeLeft  { get; internal set; }
		public sbyte  VolumeRight { get; internal set; }
		public UInt16 Pitch       { get; internal set; }
		public byte   Source      { get; internal set; }
		public byte   ADSR0       { get; internal set; }
		public byte   ADSR1       { get; internal set; }
		public byte   Gain        { get; internal set; }
		public byte   ENVX        { get; internal set; }
		
		public DSPVoice1 Clone() {
			return (DSPVoice1) MemberwiseClone();
		}
		
		object ICloneable.Clone() => Clone();
	}
	
	public class DSPVoice2: ICloneable {
		public bool KeyOn      { get; internal set; }
		public bool KeyOff     { get; internal set; }
		public bool PitchModOn { get; internal set; }
		public bool NoiseOn    { get; internal set; }
		public bool EchoOn     { get; internal set; }
		public bool End        { get; internal set; }
		
		public DSPVoice2 Clone() {
			return (DSPVoice2) MemberwiseClone();
		}
		
		object ICloneable.Clone() => Clone();
	}
	
	public class DSPVoice3: ICloneable {
		public Int16[]          Buffer         { get; internal set; } = new Int16[8];
		public byte             BufferOffset   { get; internal set; }
		public UInt16           GaussianOffset { get; internal set; }
		public UInt16           BRRAddress     { get; internal set; }
		public byte             BRROffset      { get; internal set; }
		public byte             KeyOnDelay     { get; internal set; }
		public DSP.EnvelopeMode EnvMode        { get; internal set; }
		public UInt16           EnvLevel       { get; internal set; }
		
		public Int16            GAINEnvLevel   { get; internal set; }
		public bool             KeyLatch       { get; internal set; }
		public bool             KeyOn          { get; internal set; }
		public bool             KeyOff         { get; internal set; }
		public bool             PitchModOn     { get; internal set; }
		public bool             NoiseOn        { get; internal set; }
		public bool             EchoOn         { get; internal set; }
		public bool             End            { get; internal set; }
		public bool             Looped         { get; internal set; }
		
		public DSPVoice3 Clone() {
			var clone = (DSPVoice3) MemberwiseClone();
			clone.Buffer = Buffer.ToArray();
			return clone;
		}
		
		object ICloneable.Clone() => Clone();
	}
	
	public class SPCState: ICloneable {
		public byte         A            { get; internal set; }
		public byte         X            { get; internal set; }
		public byte         Y            { get; internal set; }
		
		public byte         SP           { get; internal set; }
		public UInt16       PC           { get; internal set; }
		
		public byte         PSW          { get; internal set; }
		public SPC.ExecMode Mode         { get; internal set; }
		
		public UInt16       InstrStartPC { get; internal set; }
		
		public byte[]       ExecData     { get; internal set; } = new byte[3];
		
		public SPCState Clone() {
			var clone = (SPCState) MemberwiseClone();
			clone.ExecData = ExecData.ToArray();
			return clone;
		}
		
		object ICloneable.Clone() => Clone();
	}
	
	public class SMPState: ICloneable {
		public struct TimerState {
			bool Enabled;
			
			byte Stage0;
			byte Stage1;
			byte Stage2;
			
			byte Divider;
			byte Output;
			
			internal TimerState(SMP.Properties.TimerProps timerProps) {
				Enabled = timerProps.Enabled;
				
				Stage0  = timerProps.Stage0;
				Stage1  = timerProps.Stage1;
				Stage2  = timerProps.Stage2;
				
				Divider = timerProps.Divider;
				Output  = timerProps.Output;
			}
		}
		
		public TimerState[] Timer              { get; internal set; } = new TimerState[3];
		
		public bool         GlobalTimerDisable { get; internal set; }
		public bool         RAMWriteEnable     { get; internal set; }
		public bool         RAMDisable         { get; internal set; }
		public bool         GlobalTimerEnable  { get; internal set; }
		public byte         RAMWaitstates      { get; internal set; }
		public byte         IOWaitstates       { get; internal set; }
		
		public bool         UseBootROM         { get; internal set; }
		
		public byte         DSPAddress         { get; internal set; }
		public byte[]       InputPorts         { get; internal set; } = new byte[4];
		public byte[]       OutputPorts        { get; internal set; } = new byte[4];
		
		public byte[]       Aux                { get; internal set; } = new byte[2];
		
		public SMPState Clone() {
			var clone = (SMPState) MemberwiseClone();
			
			clone.Timer       = Timer.ToArray();
			clone.InputPorts  = InputPorts.ToArray();
			clone.OutputPorts = OutputPorts.ToArray();
			clone.Aux         = Aux.ToArray();
			
			return clone;
		}
		
		object ICloneable.Clone() => Clone();
	}
	
	public class Script700State: ICloneable {
		public bool                 IsRunning      { get; internal set; }
		
		public byte[]               InputPorts     { get; internal set; } = new byte[4];
		
		public UInt32[]             Work           { get; internal set; } = new UInt32[8];
		public UInt32[]             Cmp            { get; internal set; } = new UInt32[2];
		
		public UInt32[]             Callstack      { get; internal set; } = new UInt32[64];
		public byte                 SP             { get; internal set; }
		public byte                 SPTop          { get; internal set; }
		
		public bool                 CallstackOn    { get; internal set; }
		public bool                 PortQueueOn    { get; internal set; }
		
		public UInt32               PC             { get; internal set; }
		public UInt32               Step           { get; internal set; }
		
		public UInt64               CurCycle       { get; internal set; }
		public UInt64               BeginCycle     { get; internal set; }
		public UInt64               SyncPoint      { get; internal set; }
		public UInt64               LastCycle      { get; internal set; }
		
		public UInt64               WaitUntil      { get; internal set; }
		public Script700.WaitDevice WaitDevice     { get; internal set; }
		public byte                 WaitPort       { get; internal set; }
		
		public int                  BytecodeLength { get; internal set; }
		public int                  DataLength     { get; internal set; }
		
		public Script700State Clone() {
			var clone = (Script700State) MemberwiseClone();
			
			clone.InputPorts = InputPorts.ToArray();
			clone.Work       = Work.ToArray();
			clone.Cmp        = Cmp.ToArray();
			clone.Callstack  = Callstack.ToArray();
			
			return clone;
		}
		
		object ICloneable.Clone() => Clone();
	}
	
	public long Step      { get; private set; }
	public long DSPCycle  { get; private set; }
	public long InstrStep { get; private set; }
	
	public byte[]?             ARAM_Data             { get; private set; }
	public byte[]?             SMP_BusData           { get; private set; }
	public byte[]?             DSP_RegisterMem       { get; private set; }
	public bool[]?             Script700_Breakpoints { get; private set; }
	public SMP.MemAccessLog[]? SMP_AccessLogs        { get; private set; }
	public DSPVoice1[]?        DSP_Voice             { get; private set; }
	public DSP2State?          DSP_State             { get; private set; }
	public DSP3State?          DSP_DebugState        { get; private set; }
	public SPCState?           SPC_State             { get; private set; }
	public SMPState?           SMP_State             { get; private set; }
	public Script700State?     Script700_State       { get; private set; }
	
	static long nextStep = 0;
	
	public EmuDataBuffer(long dspCycle, long instrStep) {
		DSPCycle = dspCycle;
		Step = nextStep;
		InstrStep = instrStep;
		nextStep++;
	}
	
	const int QueueLimit = 4;
	
	public static EmuDataBuffer[] GenBufferQueue {
		get {
			lock (genBufferLock) {
				return genBufferQueue.ToArray();
			}
		}
	}
	
	static Queue<EmuDataBuffer> genBufferQueue = new();
	static object               genBufferLock  = new();
	
	public void RequestPopulate(Emulator emu, Transfer.Requests requests, Int32 startAddr = 0, UInt32 length = 0x100) {
		// Shift Buffer queue
		lock (genBufferLock) {
			if (genBufferQueue.Count == 0 || InstrStep != genBufferQueue.Peek().InstrStep) {
				
				genBufferQueue.Enqueue(this);
				
				if (genBufferQueue.Count > QueueLimit) {
					genBufferQueue.Dequeue();
				}
			}
		}
		
		resetToNull();
		
		if ((requests & Transfer.Requests.ARAM) != 0) {
			ARAM_Data = new byte[length];
			
			for (var a = startAddr; a < startAddr + length; a++) {
				if (startAddr + length > 0xFFFF) {
					break;
				}
				
				ARAM_Data[a - startAddr] = emu.DSP.ARAM[a];
			}
		}
		
		if ((requests & Transfer.Requests.SMP_Bus) != 0) {
			// TODO: Optimize when only a tiny amount of data is requested
			SMP_BusData = new byte[length];
			
			var startPage = startAddr & 0xFF00;
			var lastPage  = Math.Clamp(startAddr + length - 1, 0, 0xFFFF) & 0xFF00;
			
			var srcData = new byte[lastPage + 0x100 - startPage];
			
			for (var p = startPage >> 8; p <= lastPage >> 8; p++) {
				var baseAddr = p << 8;
				var buf = emu.SMP.DebugReadPage((UInt16) baseAddr);
				for (var i = 0; i < 0x100; i++) {
					srcData[baseAddr - startPage + i] = buf[i];
				}
			}
			
			var startOffset = startAddr & 0xFF;
			for (var i = 0; i < length; i++) {
				SMP_BusData[i] = srcData[startOffset + i];
			}
		}
		
		if ((requests & Transfer.Requests.DSP_RegisterMem) != 0) {
			DSP_RegisterMem = new byte[0x80];
			
			for (var i = 0; i < 0x80; i++) {
				DSP_RegisterMem[i] = emu.DSP.Register[i];
			}
		}
		
		if ((requests & Transfer.Requests.MemLogs) != 0) {
			SMP_AccessLogs = emu.SMP.GetAccessLogsDeduped(Math.Max(0, DSPCycle - 240));
		}
		
		if ((requests & Transfer.Requests.DSP_1) != 0) {
			DSP_Voice = new DSPVoice1[8];
			
			for (var v = 0; v < 8; v++) {
				DSP_Voice[v] = new() {
					VolumeLeft  = emu.DSP.State.Voice[v].VolumeLeft,
					VolumeRight = emu.DSP.State.Voice[v].VolumeRight,
					Pitch       = emu.DSP.State.Voice[v].Pitch,
					Source      = emu.DSP.State.Voice[v].Source,
					ADSR0       = emu.DSP.State.Voice[v].ADSR0,
					ADSR1       = emu.DSP.State.Voice[v].ADSR1,
					Gain        = emu.DSP.State.Voice[v].Gain,
					ENVX        = emu.DSP.State.Voice[v].ENVX,
				};
			}
		}
		
		if ((requests & Transfer.Requests.DSP_2) != 0) {
			DSP_State = new() {
				MainVolumeLeft  = emu.DSP.State.MainVolumeLeft,
				MainVolumeRight = emu.DSP.State.MainVolumeRight,
				EchoVolumeLeft  = emu.DSP.State.Echo.VolumeLeft,
				EchoVolumeRight = emu.DSP.State.Echo.VolumeRight,
				EchoFeedback    = emu.DSP.State.Echo.Feedback,
				EchoStartPage   = emu.DSP.State.Echo.StartPage,
				SourceStartPage = emu.DSP.State.SourceTablePage,
				EchoDelay       = emu.DSP.State.Echo.Delay,
				NoiseClock      = emu.DSP.State.NoiseRate,
				ReadonlyEcho    = emu.DSP.State.Echo.Readonly,
				Mute            = emu.DSP.State.Mute,
				Reset           = emu.DSP.State.Reset,
				FIR             = emu.DSP.State.Echo.FIR.ToArray(),
			};
			
			for (var v = 0; v < 8; v++) {
				DSP_State.Voice[v] = new() {
					KeyOn       = emu.DSP.State.Voice[v].KeyOn,
					KeyOff      = emu.DSP.State.Voice[v].KeyOff,
					PitchModOn  = emu.DSP.State.Voice[v].PitchModOn,
					NoiseOn     = emu.DSP.State.Voice[v].NoiseOn,
					EchoOn      = emu.DSP.State.Voice[v].EchoOn,
					End         = emu.DSP.State.Voice[v].End,
				};
			}
		}
		
		if ((requests & Transfer.Requests.DSP_3) != 0) {
			DSP_DebugState = new();
			
			for (var v = 0; v < 8; v++) {
				DSP_DebugState.Voice[v] = new() {
					Buffer         = emu.DSP.State.VoiceDebug[v].Buffer,
					BufferOffset   = emu.DSP.State.VoiceDebug[v].BufferOffset,
					GaussianOffset = emu.DSP.State.VoiceDebug[v].GaussianOffset,
					BRRAddress     = emu.DSP.State.VoiceDebug[v].BRRAddress,
					BRROffset      = emu.DSP.State.VoiceDebug[v].BRROffset,
					KeyOnDelay     = emu.DSP.State.VoiceDebug[v].KeyOnDelay,
					EnvMode        = emu.DSP.State.VoiceDebug[v].EnvMode,
					EnvLevel       = emu.DSP.State.VoiceDebug[v].EnvLevel,
					
					GAINEnvLevel   = emu.DSP.State.VoiceDebug[v].GAINEnvLevel,
					KeyLatch       = emu.DSP.State.VoiceDebug[v].KeyLatch,
					KeyOn          = emu.DSP.State.VoiceDebug[v].KeyOn,
					KeyOff         = emu.DSP.State.VoiceDebug[v].KeyOff,
					PitchModOn     = emu.DSP.State.VoiceDebug[v].PitchModOn,
					NoiseOn        = emu.DSP.State.VoiceDebug[v].NoiseOn,
					EchoOn         = emu.DSP.State.VoiceDebug[v].EchoOn,
					End            = emu.DSP.State.VoiceDebug[v].End,
					Looped         = emu.DSP.State.VoiceDebug[v].Looped,
				};
			}
		}
		
		if ((requests & Transfer.Requests.SPC_Regs) != 0) {
			SPC_State = new() {
				A            = emu.SPC.State.A,
				X            = emu.SPC.State.X,
				Y            = emu.SPC.State.Y,
				
				SP           = emu.SPC.State.SP,
				PC           = emu.SPC.State.PC,
				
				PSW          = emu.SPC.State.PSW,
				Mode         = emu.SPC.State.Mode,
				
				InstrStartPC = emu.SPC.State.InstructionStartPC,
			};
			
			var pc = SPC_State.InstrStartPC;
			
			for (var i = 0; i < 3; i++) {
				SPC_State.ExecData[i] = emu.SMP.DebugReadByte((UInt16) (pc + i));
			}
		}
		
		if ((requests & Transfer.Requests.SMP_State) != 0) {
			SMP_State = new() {
				Timer = [
					new(emu.SMP.State.Timer[0]),
					new(emu.SMP.State.Timer[1]),
					new(emu.SMP.State.Timer[2])
				],
				
				GlobalTimerDisable = emu.SMP.State.GlobalTimerDisable,
				RAMWriteEnable     = emu.SMP.State.RAMWriteEnable,
				RAMDisable         = emu.SMP.State.RAMDisable,
				GlobalTimerEnable  = emu.SMP.State.GlobalTimerEnable,
				RAMWaitstates      = emu.SMP.State.RAMWaitstates,
				IOWaitstates       = emu.SMP.State.IOWaitstates,
				UseBootROM         = emu.SMP.State.UseBootROM,
				DSPAddress         = emu.SMP.State.DSPAddress,
				
				InputPorts = [
					emu.SMP.State.IO.Input[0],
					emu.SMP.State.IO.Input[1],
					emu.SMP.State.IO.Input[2],
					emu.SMP.State.IO.Input[3],
				],
				
				OutputPorts = [
					emu.SMP.State.IO.Output[0],
					emu.SMP.State.IO.Output[1],
					emu.SMP.State.IO.Output[2],
					emu.SMP.State.IO.Output[3],
				],
				
				Aux = [emu.SMP.State.Aux[0], emu.SMP.State.Aux[1]]
			};
		}
		
		if ((requests & Transfer.Requests.Script700) != 0) {
			Script700_State = new() {
				IsRunning      = emu.Script700.IsRunning,
				
				InputPorts     = emu.Script700.State.PortIn.ToArray(),
				
				Work           = emu.Script700.State.Work.ToArray(),
				Cmp            = emu.Script700.State.Cmp .ToArray(),
				
				Callstack      = emu.Script700.State.Callstack.ToArray(),
				SP             = emu.Script700.State.SP,
				SPTop          = emu.Script700.State.SPTop,
				
				CallstackOn    = emu.Script700.State.CallstackOn,
				PortQueueOn    = emu.Script700.State.PortQueueOn,
				
				PC             = emu.Script700.State.PC,
				Step           = emu.Script700.State.Step,
				
				CurCycle       = emu.Script700.State.CurCycle,
				BeginCycle     = emu.Script700.State.BeginCycle,
				SyncPoint      = emu.Script700.State.SyncPoint,
				LastCycle      = emu.Script700.State.LastCycle,
				
				WaitUntil      = emu.Script700.State.WaitUntil,
				WaitDevice     = emu.Script700.State.WaitDevice,
				WaitPort       = emu.Script700.State.WaitPort,
				
				BytecodeLength = emu.Script700.ScriptLength,
				DataLength     = emu.Script700.DataLength,
			};
		}
	}
	
	public bool ExpectData(Transfer.Requests requests) {
		var result = true;
		
		if ((requests & Transfer.Requests.ARAM)            != 0) result = result && ARAM_Data             is not null;
		if ((requests & Transfer.Requests.SMP_Bus)         != 0) result = result && SMP_BusData           is not null;
		if ((requests & Transfer.Requests.DSP_RegisterMem) != 0) result = result && DSP_RegisterMem       is not null;
		if ((requests & Transfer.Requests.MemLogs)         != 0) result = result && SMP_AccessLogs        is not null;
		if ((requests & Transfer.Requests.DSP_1)           != 0) result = result && DSP_Voice             is not null;
		if ((requests & Transfer.Requests.DSP_2)           != 0) result = result && DSP_State             is not null;
		if ((requests & Transfer.Requests.DSP_3)           != 0) result = result && DSP_DebugState        is not null;
		if ((requests & Transfer.Requests.SPC_Regs)        != 0) result = result && SPC_State             is not null;
		if ((requests & Transfer.Requests.SMP_State)       != 0) result = result && SMP_State             is not null;
		if ((requests & Transfer.Requests.Script700)       != 0) result = result && Script700_State       is not null;
		if ((requests & Transfer.Requests.Script700_Break) != 0) result = result && Script700_Breakpoints is not null;
		
		return result;
	}
	
	public EmuDataBuffer Clone() {
		EmuDataBuffer clone = new(DSPCycle, InstrStep);
		
		if (ARAM_Data is not null) {
			clone.ARAM_Data = ARAM_Data.ToArray();
		}
		
		if (SMP_BusData is not null) {
			clone.SMP_BusData = SMP_BusData.ToArray();
		}
		
		if (DSP_RegisterMem is not null) {
			clone.DSP_RegisterMem = DSP_RegisterMem.ToArray();
		}
		
		if (SMP_AccessLogs is not null) {
			clone.SMP_AccessLogs = SMP_AccessLogs.Select(x => x.Clone()).ToArray();
		}
		
		if (Script700_Breakpoints is not null) {
			clone.Script700_Breakpoints = Script700_Breakpoints.ToArray();
		}
		
		if (DSP_Voice is not null) {
			clone.DSP_Voice = new DSPVoice1[8];
			for (var v = 0; v < 8; v++) {
				clone.DSP_Voice[v] = DSP_Voice[v].Clone();
			}
		}
		
		if (DSP_State is not null) {
			clone.DSP_State = DSP_State.Clone();
		}
		
		if (DSP_DebugState is not null) {
			clone.DSP_DebugState = DSP_DebugState.Clone();
		}
		
		if (SPC_State is not null) {
			clone.SPC_State = SPC_State.Clone();
		}
		
		if (SMP_State is not null) {
			clone.SMP_State = SMP_State.Clone();
		}
		
		if (Script700_State is not null) {
			clone.Script700_State = Script700_State.Clone();
		}
		
		return clone;
	}
	
	object ICloneable.Clone() => Clone();
	
	void resetToNull() {
		ARAM_Data             = null;
		SMP_BusData           = null;
		DSP_RegisterMem       = null;
		SMP_AccessLogs        = null;
		Script700_Breakpoints = null;
		DSP_Voice             = null;
		DSP_State             = null;
		DSP_DebugState        = null;
		SPC_State             = null;
		SMP_State             = null;
		Script700_State       = null;
	}
}