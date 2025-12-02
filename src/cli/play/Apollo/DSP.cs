namespace Apollo;

using System.Collections;

public class DSP {
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
				
			internal EchoProps(Emulator emu, DLL.DspGlobalState state) {
				this.emu   = emu;
				this.state = state;
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
		
		public EchoProps    Echo  { get; }
		public VoiceProps[] Voice { get; }
				
		internal Properties(Emulator emu, DLL.DspGlobalState state, DLL.DspVoiceState[] voiceStates) {
			this.emu   = emu;
			this.state = state;
			
			Echo  = new(emu, state);
			Voice = new VoiceProps[8];
			
			for (var i = 0; i < 8; i++) {
				if (i >= voiceStates.Length) {
					break;
				}
				Voice[i] = new(emu, voiceStates[i]);
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
			
			List<DLL.DspVoiceState> voiceStates = new();
			
			for (var i = 0; i < 8; i++) {
				var v = DLL.DspGetVoiceState((byte) i, Emulator.handle);
				if (v.VolLeft == IntPtr.Zero) {
					var errorCode = DLL.EmuGetLastError(Emulator.handle);
					Error.Throw(errorCode);
				}
				voiceStates.Add(v);
			}
			
			State = new(emulator, globalState, voiceStates.ToArray());
		}
	}
}