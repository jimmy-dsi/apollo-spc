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
	
	public long Step     { get; private set; }
	public long DSPCycle { get; private set; }
	
	public byte[]?      ARAM_Data       { get; private set; }
	public byte[]?      SMP_BusData     { get; private set; }
	public byte[]?      DSP_RegisterMem { get; private set; }
	public DSPVoice1[]? DSP_Voice       { get; private set; }
	public DSP2State?   DSP_State       { get; private set; }
	public DSP3State?   DSP_DebugState  { get; private set; }
	
	static long nextStep = 0;
	
	public EmuDataBuffer(long dspCycle) {
		DSPCycle = dspCycle;
		Step = nextStep;
		nextStep++;
	}
	
	public void RequestPopulate(Emulator emu, Transfer.Requests requests, UInt16 startAddr = 0, UInt16 length = 0x100) {
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
	}
	
	public bool ExpectData(Transfer.Requests requests) {
		var result = true;
		
		if ((requests & Transfer.Requests.ARAM)            != 0) result = result && ARAM_Data       is not null;
		if ((requests & Transfer.Requests.SMP_Bus)         != 0) result = result && SMP_BusData     is not null;
		if ((requests & Transfer.Requests.DSP_RegisterMem) != 0) result = result && DSP_RegisterMem is not null;
		if ((requests & Transfer.Requests.DSP_1)           != 0) result = result && DSP_Voice       is not null;
		if ((requests & Transfer.Requests.DSP_2)           != 0) result = result && DSP_State       is not null;
		if ((requests & Transfer.Requests.DSP_3)           != 0) result = result && DSP_DebugState  is not null;
		
		return result;
	}
	
	public EmuDataBuffer Clone() {
		EmuDataBuffer clone = new(DSPCycle);
		
		if (ARAM_Data is not null) {
			clone.ARAM_Data = ARAM_Data.ToArray();
		}
		
		if (SMP_BusData is not null) {
			clone.SMP_BusData = SMP_BusData.ToArray();
		}
		
		if (DSP_RegisterMem is not null) {
			clone.DSP_RegisterMem = DSP_RegisterMem.ToArray();
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
		
		return clone;
	}
	
	object ICloneable.Clone() => Clone();
	
	void resetToNull() {
		ARAM_Data       = null;
		SMP_BusData     = null;
		DSP_RegisterMem = null;
		DSP_Voice       = null;
		DSP_State       = null;
		DSP_DebugState  = null;
	}
}