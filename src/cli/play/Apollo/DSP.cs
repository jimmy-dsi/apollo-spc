namespace Apollo;

using System.Collections;

public class DSP {
	public enum EnvelopeMode {
		KeyOff = 0, Attack = 1, Decay = 2, Release = 3
	}
	
	public unsafe class Properties {
		public class EchoProps {
			public class FirBuf: IEnumerable<sbyte> {
				public sbyte this[byte index] {
					get {
						emu.MaybeAcquireLock();
						try     { return *((sbyte*) state.EchoFIR + (index & 7)); }
						finally { emu.MaybeReleaseLock(); }
					}
					set {
						emu.MaybeAcquireLock();
						try     { *((sbyte*) state.EchoFIR + (index & 7)) = value; }
						finally { emu.MaybeReleaseLock(); }
					}
				}
				
				Emulator emu;
				DLL.DspGlobalState state;
				
				internal FirBuf(Emulator emu, DLL.DspGlobalState state) {
					this.emu   = emu;
					this.state = state;
				}
				
				public IEnumerator<sbyte> GetEnumerator() {
					for (byte i = 0; i < 8; i++) {
						yield return this[i];
					}
				}
				
				IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
			}
			
			Emulator emu;
			DLL.DspGlobalState state;
			DLL.DspDebugGlobalState debugState;
			
			public sbyte Feedback {
				get {
					emu.MaybeAcquireLock();
					try     { return *((sbyte*) state.EchoFeedback); }
					finally { emu.MaybeReleaseLock(); }
				}
				set {
					emu.MaybeAcquireLock();
					try     { *((sbyte*) state.EchoFeedback) = value; }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public sbyte VolumeLeft {
				get {
					emu.MaybeAcquireLock();
					try     { return *((sbyte*) state.EchoVolLeft); }
					finally { emu.MaybeReleaseLock(); }
				}
				set {
					emu.MaybeAcquireLock();
					try     { *((sbyte*) state.EchoVolLeft) = value; }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public sbyte VolumeRight {
				get {
					emu.MaybeAcquireLock();
					try     { return *((sbyte*) state.EchoVolRight); }
					finally { emu.MaybeReleaseLock(); }
				}
				set {
					emu.MaybeAcquireLock();
					try     { *((sbyte*) state.EchoVolRight) = value; }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public FirBuf FIR { get; }
			
			public byte StartPage {
				get {
					emu.MaybeAcquireLock();
					try     { return *((byte*) state.EsaPage); }
					finally { emu.MaybeReleaseLock(); }
				}
				set {
					emu.MaybeAcquireLock();
					try     { *((byte*) state.EsaPage) = value; }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public byte Delay {
				get {
					emu.MaybeAcquireLock();
					try     { return (byte) (*((byte*) state.EchoDelay) & 15); }
					finally { emu.MaybeReleaseLock(); }
				}
				set {
					emu.MaybeAcquireLock();
					try     { *((byte*) state.EchoDelay) = (byte) (value & 15); }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public bool Readonly {
				get {
					emu.MaybeAcquireLock();
					try     { return *((bool*) state.EchoReadonly); }
					finally { emu.MaybeReleaseLock(); }
				}
				set {
					emu.MaybeAcquireLock();
					try     { *((bool*) state.EchoReadonly) = value; }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public UInt16 Offset {
				get {
					emu.MaybeAcquireLock();
					try     { return *((UInt16*) debugState.EchoOffset); }
					finally { emu.MaybeReleaseLock(); }
				}
				set {
					emu.MaybeAcquireLock();
					try     { *((UInt16*) debugState.EchoOffset) = value; }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public UInt16 Address {
				get {
					emu.MaybeAcquireLock();
					try     { return *((UInt16*) debugState.EchoAddress); }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public byte Page {
				get {
					emu.MaybeAcquireLock();
					try     { return *((byte*) debugState.EchoPage); }
					finally { emu.MaybeReleaseLock(); }
				}
				set {
					emu.MaybeAcquireLock();
					try     { *((byte*) debugState.EchoPage) = value; }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public UInt16 Length {
				get {
					emu.MaybeAcquireLock();
					try     { return *((UInt16*) debugState.EchoLength); }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public UInt64 LastReadCycle {
				get {
					emu.MaybeAcquireLock();
					try     { return *((UInt64*) debugState.LastEchoReadCycle); }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public UInt16 LastReadAddr {
				get {
					emu.MaybeAcquireLock();
					try     { return *((UInt16*) debugState.LastEchoReadAddr); }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public Int16 LastReadLeft {
				get {
					emu.MaybeAcquireLock();
					try     { return *((Int16*) debugState.LastEchoReadLeft); }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public Int16 LastReadRight {
				get {
					emu.MaybeAcquireLock();
					try     { return *((Int16*) debugState.LastEchoReadRight); }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public UInt64 LastWriteCycle {
				get {
					emu.MaybeAcquireLock();
					try     { return *((UInt64*) debugState.LastEchoWriteCycle); }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public UInt16 LastWriteAddr {
				get {
					emu.MaybeAcquireLock();
					try     { return *((UInt16*) debugState.LastEchoWriteAddr); }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public Int16 LastWriteLeft {
				get {
					emu.MaybeAcquireLock();
					try     { return *((Int16*) debugState.LastEchoWriteLeft); }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public Int16 LastWriteRight {
				get {
					emu.MaybeAcquireLock();
					try     { return *((Int16*) debugState.LastEchoWriteRight); }
					finally { emu.MaybeReleaseLock(); }
				}
			}
				
			internal EchoProps(Emulator emu, DLL.DspGlobalState state, DLL.DspDebugGlobalState debugState) {
				this.emu        = emu;
				this.state      = state;
				this.debugState = debugState;
				
				FIR = new(emu, state);
			}
		}
		
		public class VoiceProps {
			Emulator emu;
			DLL.DspVoiceState state;
			
			public sbyte VolumeLeft {
				get {
					emu.MaybeAcquireLock();
					try     { return *((sbyte*) state.VolLeft); }
					finally { emu.MaybeReleaseLock(); }
				}
				set {
					emu.MaybeAcquireLock();
					try     { *((sbyte*) state.VolLeft) = value; }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public sbyte VolumeRight {
				get {
					emu.MaybeAcquireLock();
					try     { return *((sbyte*) state.VolRight); }
					finally { emu.MaybeReleaseLock(); }
				}
				set {
					emu.MaybeAcquireLock();
					try     { *((sbyte*) state.VolRight) = value; }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public UInt16 Pitch {
				get {
					emu.MaybeAcquireLock();
					try     { return (UInt16) (*((UInt16*) state.Pitch) & 0x3FFF); }
					finally { emu.MaybeReleaseLock(); }
				}
				set {
					emu.MaybeAcquireLock();
					try     { *((UInt16*) state.Pitch) = (UInt16) (value & 0x3FFF); }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public byte Source {
				get {
					emu.MaybeAcquireLock();
					try     { return *((byte*) state.Source); }
					finally { emu.MaybeReleaseLock(); }
				}
				set {
					emu.MaybeAcquireLock();
					try     { *((byte*) state.Source) = value; }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public byte ADSR0 {
				get {
					emu.MaybeAcquireLock();
					try     { return *((byte*) state.Adsr0); }
					finally { emu.MaybeReleaseLock(); }
				}
				set {
					emu.MaybeAcquireLock();
					try     { *((byte*) state.Adsr0) = value; }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public byte ADSR1 {
				get {
					emu.MaybeAcquireLock();
					try     { return *((byte*) state.Adsr1); }
					finally { emu.MaybeReleaseLock(); }
				}
				set {
					emu.MaybeAcquireLock();
					try     { *((byte*) state.Adsr1) = value; }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public byte Gain {
				get {
					emu.MaybeAcquireLock();
					try     { return *((byte*) state.Gain); }
					finally { emu.MaybeReleaseLock(); }
				}
				set {
					emu.MaybeAcquireLock();
					try     { *((byte*) state.Gain) = value; }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public byte ENVX {
				get {
					emu.MaybeAcquireLock();
					try     { return *((byte*) state.Envx); }
					finally { emu.MaybeReleaseLock(); }
				}
				set {
					emu.MaybeAcquireLock();
					try     { *((byte*) state.Envx) = value; }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public bool KeyOn {
				get {
					emu.MaybeAcquireLock();
					try     { return *((bool*) state.KeyOn); }
					finally { emu.MaybeReleaseLock(); }
				}
				set {
					emu.MaybeAcquireLock();
					try     { *((bool*) state.KeyOn) = value; }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public bool KeyOff {
				get {
					emu.MaybeAcquireLock();
					try     { return *((bool*) state.KeyOff); }
					finally { emu.MaybeReleaseLock(); }
				}
				set {
					emu.MaybeAcquireLock();
					try     { *((bool*) state.KeyOff) = value; }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public bool PitchModOn {
				get {
					emu.MaybeAcquireLock();
					try     { return *((bool*) state.PitchModOn); }
					finally { emu.MaybeReleaseLock(); }
				}
				set {
					emu.MaybeAcquireLock();
					try     { *((bool*) state.PitchModOn) = value; }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public bool NoiseOn {
				get {
					emu.MaybeAcquireLock();
					try     { return *((bool*) state.NoiseOn); }
					finally { emu.MaybeReleaseLock(); }
				}
				set {
					emu.MaybeAcquireLock();
					try     { *((bool*) state.NoiseOn) = value; }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public bool EchoOn {
				get {
					emu.MaybeAcquireLock();
					try     { return *((bool*) state.EchoOn); }
					finally { emu.MaybeReleaseLock(); }
				}
				set {
					emu.MaybeAcquireLock();
					try     { *((bool*) state.EchoOn) = value; }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public bool End {
				get {
					emu.MaybeAcquireLock();
					try     { return *((bool*) state.End); }
					finally { emu.MaybeReleaseLock(); }
				}
				set {
					emu.MaybeAcquireLock();
					try     { *((bool*) state.End) = value; }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			internal VoiceProps(Emulator emu, DLL.DspVoiceState state) {
				this.emu   = emu;
				this.state = state;
			}
		}
		
		public class DebugVoiceProps {
			Emulator emu;
			DLL.DspDebugVoiceState state;
			
			public Int16[] Buffer {
				get {
					emu.MaybeAcquireLock();
					try     { return new Span<Int16>((Int16*) state.Buffer, 12).ToArray(); }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public byte BufferOffset {
				get {
					emu.MaybeAcquireLock();
					try     { return (byte) (*((byte*) state.BufferOffset) & 0xF); }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public UInt16 GaussianOffset {
				get {
					emu.MaybeAcquireLock();
					try     { return *((UInt16*) state.GaussianOffset); }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public UInt16 BRRAddress {
				get {
					emu.MaybeAcquireLock();
					try     { return *((UInt16*) state.BrrAddress); }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public byte BRROffset {
				get {
					emu.MaybeAcquireLock();
					try     { return (byte) (*((byte*) state.BrrOffset) & 0xF); }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public byte KeyOnDelay {
				get {
					emu.MaybeAcquireLock();
					try     { return (byte) (*((byte*) state.KeyOnDelay) & 7); }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public EnvelopeMode EnvMode {
				get {
					emu.MaybeAcquireLock();
					try     { return (EnvelopeMode) (byte) (*((byte*) state.EnvMode) % 4); }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public UInt16 EnvLevel {
				get {
					emu.MaybeAcquireLock();
					try     { return (UInt16) (*((UInt16*) state.EnvLevel) & 0x7FF); }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public Int16 GAINEnvLevel {
				get {
					emu.MaybeAcquireLock();
					try     { return *((Int16*) state.GainEnvLevel); }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public bool KeyLatch {
				get {
					emu.MaybeAcquireLock();
					try     { return *((bool*) state.KeyLatch); }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public bool KeyOn {
				get {
					emu.MaybeAcquireLock();
					try     { return *((bool*) state.KeyOn); }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public bool KeyOff {
				get {
					emu.MaybeAcquireLock();
					try     { return *((bool*) state.KeyOff); }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public bool PitchModOn {
				get {
					emu.MaybeAcquireLock();
					try     { return *((bool*) state.PitchModOn); }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public bool NoiseOn {
				get {
					emu.MaybeAcquireLock();
					try     { return *((bool*) state.NoiseOn); }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public bool EchoOn {
				get {
					emu.MaybeAcquireLock();
					try     { return *((bool*) state.EchoOn); }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public bool End {
				get {
					emu.MaybeAcquireLock();
					try     { return *((bool*) state.End); }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public bool Looped {
				get {
					emu.MaybeAcquireLock();
					try     { return *((bool*) state.End); }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public byte QueuedSRCN {
				get {
					emu.MaybeAcquireLock();
					try     { return *((byte*) state.QueuedSRCN); }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public byte CurrentSRCN {
				get {
					emu.MaybeAcquireLock();
					try     { return *((byte*) state.CurrentSRCN); }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public UInt16 TruePitch {
				get {
					emu.MaybeAcquireLock();
					try     { return *((UInt16*) state.TruePitch); }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			internal DebugVoiceProps(Emulator emu, DLL.DspDebugVoiceState state) {
				this.emu   = emu;
				this.state = state;
			}
		}
		
		Emulator emu;
		DLL.DspGlobalState state;
			
		public bool Reset {
			get {
				emu.MaybeAcquireLock();
				try     { return *((bool*) state.Reset); }
				finally { emu.MaybeReleaseLock(); }
			}
			set {
				emu.MaybeAcquireLock();
				try     { *((bool*) state.Reset) = value; }
				finally { emu.MaybeReleaseLock(); }
			}
		}

		public bool Mute {
			get {
				emu.MaybeAcquireLock();
				try     { return *((bool*) state.Mute); }
				finally { emu.MaybeReleaseLock(); }
			}
			set {
				emu.MaybeAcquireLock();
				try     { *((bool*) state.Mute) = value; }
				finally { emu.MaybeReleaseLock(); }
			}
		}
			
		public byte NoiseRate {
			get {
				emu.MaybeAcquireLock();
				try     { return (byte) (*((byte*) state.NoiseRate) & 0x1F); }
				finally { emu.MaybeReleaseLock(); }
			}
			set {
				emu.MaybeAcquireLock();
				try     { *((byte*) state.NoiseRate) = (byte) (value & 0x1F); }
				finally { emu.MaybeReleaseLock(); }
			}
		}
			
		public sbyte MainVolumeLeft {
			get {
				emu.MaybeAcquireLock();
				try     { return *((sbyte*) state.MainVolLeft); }
				finally { emu.MaybeReleaseLock(); }
			}
			set {
				emu.MaybeAcquireLock();
				try     { *((sbyte*) state.MainVolLeft) = value; }
				finally { emu.MaybeReleaseLock(); }
			}
		}
			
		public sbyte MainVolumeRight {
			get {
				emu.MaybeAcquireLock();
				try     { return *((sbyte*) state.MainVolRight); }
				finally { emu.MaybeReleaseLock(); }
			}
			set {
				emu.MaybeAcquireLock();
				try     { *((sbyte*) state.MainVolRight) = value; }
				finally { emu.MaybeReleaseLock(); }
			}
		}
		
		public byte SourceTablePage {
			get {
				emu.MaybeAcquireLock();
				try     { return *((byte*) state.BrrBank); }
				finally { emu.MaybeReleaseLock(); }
			}
			set {
				emu.MaybeAcquireLock();
				try     { *((byte*) state.BrrBank) = value; }
				finally { emu.MaybeReleaseLock(); }
			}
		}
		
		public EchoProps         Echo       { get; }
		public VoiceProps[]      Voice      { get; }
		public DebugVoiceProps[] VoiceDebug { get; }
		
		internal Properties(Emulator emu,
		                    DLL.DspGlobalState state,
		                    DLL.DspDebugGlobalState debugState,
		                    DLL.DspVoiceState[] voiceStates,
		                    DLL.DspDebugVoiceState[] debugVoiceStates)
		{
			this.emu   = emu;
			this.state = state;
			
			Echo       = new(emu, state, debugState);
			Voice      = new VoiceProps[8];
			VoiceDebug = new DebugVoiceProps[8];
			
			for (var i = 0; i < 8; i++) {
				if (i >= voiceStates.Length) {
					break;
				}
				Voice[i]      = new(emu,      voiceStates[i]);
				VoiceDebug[i] = new(emu, debugVoiceStates[i]);
			}
		}
	}
	
	public Emulator Emulator { get; init; }
	
	public UInt8Buffer ARAM     { get; }
	public UInt8Buffer Register { get; }
	
	public Properties State { get; }
	
	public long CurrentCycle {
		get {
			Emulator.MaybeAcquireLock();
			
			try {
				var result = (long) DLL.DspGetCurrentCycle(Emulator.handle);
				if (result == -1) {
					var errorCode = DLL.EmuGetLastError(Emulator.handle);
					Error.Throw(errorCode);
				}
			
				return result;
			}
			finally {
				Emulator.MaybeReleaseLock();
			}
		}
	}
		
	public bool[] SampleUsageFlags {
		get {
			Emulator.MaybeAcquireLock();
			try {
				unsafe {
					var rawFlags = DLL.DspGetSampleUsageFlags(Emulator.handle);
					if (rawFlags.Flags == IntPtr.Zero) {
						var errorCode = DLL.EmuGetLastError(Emulator.handle);
						Error.Throw(errorCode);
					}
					
					var flags = new bool[256];
					
					for (var i = 0; i < 256; i++) {
						var u8 = *((byte*) rawFlags.Flags + i);
						flags[i] = u8 != 0;
					}
					
					return flags;
				}
			}
			finally {
				Emulator.MaybeReleaseLock();
			}
		}
	}
	
	internal DSP(Emulator emulator) {
		unsafe {
			Emulator = emulator;
			
			var aramPtr = DLL.DspGetAramPtr(Emulator.handle);
			if (aramPtr == IntPtr.Zero) { throw new StateError(); }
			
			var regMapPtr = DLL.DspGetRegMapPtr(Emulator.handle);
			if (regMapPtr == IntPtr.Zero) { throw new StateError(); }
			
			if (emulator.MakeShared) {
				ARAM     = new UInt8BufferShared(emulator, (byte*) aramPtr, 65536);
				Register = new UInt8BufferShared(emulator, (byte*) regMapPtr, 128);
			}
			else {
				ARAM     = new((byte*) aramPtr, 65536);
				Register = new((byte*) regMapPtr, 128);
			}
			
			var globalState = DLL.DspGetGlobalState(Emulator.handle);
			if (globalState.EchoFeedback == IntPtr.Zero) {
				var errorCode = DLL.EmuGetLastError(Emulator.handle);
				Error.Throw(errorCode);
			}
			
			var debugGlobalState = DLL.DspGetGlobalDebugState(Emulator.handle);
			if (debugGlobalState.EchoOffset == IntPtr.Zero) {
				var errorCode = DLL.EmuGetLastError(Emulator.handle);
				Error.Throw(errorCode);
			}
			
			List<DLL.DspVoiceState> voiceStates = new();
			
			for (var i = 0; i < 8; i++) {
				var v = DLL.DspGetVoiceState((byte) i, Emulator.handle);
				if (v.VolLeft == IntPtr.Zero) {
					var errorCode = DLL.EmuGetLastError(Emulator.handle);
					Error.Throw(errorCode);
				}
				voiceStates.Add(v);
			}
			
			List<DLL.DspDebugVoiceState> debugVoiceStates = new();
			
			for (var i = 0; i < 8; i++) {
				var v = DLL.DspGetVoiceDebugState((byte) i, Emulator.handle);
				if (v.Buffer == IntPtr.Zero) {
					var errorCode = DLL.EmuGetLastError(Emulator.handle);
					Error.Throw(errorCode);
				}
				debugVoiceStates.Add(v);
			}
			
			State = new(emulator, globalState, debugGlobalState, voiceStates.ToArray(), debugVoiceStates.ToArray());
		}
	}
	
	public void ResetSampleUsage(byte sampleId) {
		Emulator.MaybeAcquireLock();
		try {
			var result = DLL.DspResetSampleUsage(sampleId, Emulator.handle);
			if (!result) {
				var errorCode = DLL.EmuGetLastError(Emulator.handle);
				Error.Throw(errorCode);
			}
		}
		finally {
			Emulator.MaybeReleaseLock();
		}
	}
	
	public static (Int16[] Decoded, bool Looped) DecodeBrrFromBuffer(byte[] input, UInt16 offset, uint maxSize, Int16 oldDecoded = 0, Int16 olderDecoded = 0) {
		var inPtr  = DLL.BufferCreate((uint) input.Length);
		var resPtr = DLL.BufferCreate(maxSize * 2);
		
		try {
			unsafe {
				Span<byte> inSpan = new((byte*) inPtr, input.Length);
				input.CopyTo(inSpan);
				
				var result = DLL.DspDecodeBrrFromBuffer(inPtr, (UInt16) input.Length, offset, resPtr, maxSize, oldDecoded, olderDecoded);
				if (result == 0) {
					var errorCode = DLL.GetLastError();
					Error.Throw(errorCode);
				}
			
				unsafe {
					if (result > 0) {
						Span<Int16> span = new((Int16*) resPtr, result);
						return (span.ToArray(), false);
					}
					else {
						Span<Int16> span = new((Int16*) resPtr, Math.Abs(result));
						return (span.ToArray(), true);
					}
				}
			}
		}
		finally {
			DLL.BufferDestroy(resPtr, maxSize * 2);
			DLL.BufferDestroy(inPtr,  (uint) input.Length);
		}
	}
	
	public (Int16[] Decoded, bool Looped) DecodeBrrAtAddr(UInt16 aramAddr, uint maxSize, Int16 oldDecoded = 0, Int16 olderDecoded = 0) {
		var ptr = DLL.BufferCreate(maxSize * 2);
		
		Emulator.MaybeAcquireLock();
		try {
			var result = DLL.DspDecodeBrrAtAddress(aramAddr, ptr, maxSize, oldDecoded, olderDecoded, Emulator.handle);
			if (result == 0) {
				var errorCode = DLL.EmuGetLastError(Emulator.handle);
				Error.Throw(errorCode);
			}
			
			unsafe {
				if (result > 0) {
					Span<Int16> span = new((Int16*) ptr, result);
					return (span.ToArray(), false);
				}
				else {
					Span<Int16> span = new((Int16*) ptr, Math.Abs(result));
					return (span.ToArray(), true);
				}
			}
		}
		finally {
			DLL.BufferDestroy(ptr, maxSize * 2);
			Emulator.MaybeReleaseLock();
		}
	}
}