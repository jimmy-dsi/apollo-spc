namespace Apollo;

public class DSP {
	public unsafe class Properties {
		public class EchoProps {
			public class FirBuf {
				public sbyte this[byte index] {
					get => *((sbyte*) state.EchoFeedback + (index & 7));
					set => *((sbyte*) state.EchoFeedback + (index & 7)) = value;
				}
				
				DLL.DspGlobalState state;
				
				internal FirBuf(DLL.DspGlobalState state) {
					this.state = state;
				}
			}
			
			DLL.DspGlobalState state;
			
			public sbyte Feedback {
				get => *((sbyte*) state.EchoFeedback);
				set => *((sbyte*) state.EchoFeedback) = value;
			}
			
			public sbyte VolumeLeft {
				get => *((sbyte*) state.EchoVolLeft);
				set => *((sbyte*) state.EchoVolLeft) = value;
			}
			
			public sbyte VolumeRight {
				get => *((sbyte*) state.EchoVolRight);
				set => *((sbyte*) state.EchoVolRight) = value;
			}
			
			public FirBuf FIR { get; }
			
			public byte StartPage {
				get => *((byte*) state.EsaPage);
				set => *((byte*) state.EsaPage) = value;
			}
			
			public byte Delay {
				get => (byte) (*((byte*) state.EchoDelay) & 15);
				set => *((byte*) state.EchoDelay) = (byte) (value & 15);
			}
			
			public bool Readonly {
				get => *((bool*) state.EchoReadonly);
				set => *((bool*) state.EchoReadonly) = value;
			}
				
			internal EchoProps(DLL.DspGlobalState state) {
				this.state = state;
				FIR = new(state);
			}
		}
		
		public class VoiceProps {
			DLL.DspVoiceState state;
			
			public sbyte VolumeLeft {
				get => *((sbyte*) state.VolLeft);
				set => *((sbyte*) state.VolLeft) = value;
			}
			
			public sbyte VolumeRight {
				get => *((sbyte*) state.VolRight);
				set => *((sbyte*) state.VolRight) = value;
			}
			
			public UInt16 Pitch {
				get => (UInt16) (*((UInt16*) state.Pitch) & 0x3FFF);
				set => *((UInt16*) state.Pitch) = (UInt16) (value & 0x3FFF);
			}
			
			public byte Source {
				get => *((byte*) state.Source);
				set => *((byte*) state.Source) = value;
			}
			
			public byte ADSR0 {
				get => *((byte*) state.Adsr0);
				set => *((byte*) state.Adsr0) = value;
			}
			
			public byte ADSR1 {
				get => *((byte*) state.Adsr1);
				set => *((byte*) state.Adsr1) = value;
			}
			
			public byte Gain {
				get => *((byte*) state.Gain);
				set => *((byte*) state.Gain) = value;
			}
			
			public byte ENVX {
				get => *((byte*) state.Envx);
				set => *((byte*) state.Envx) = value;
			}
			
			public bool KeyOn {
				get => *((bool*) state.KeyOn);
				set => *((bool*) state.KeyOn) = value;
			}
			
			public bool KeyOff {
				get => *((bool*) state.KeyOff);
				set => *((bool*) state.KeyOff) = value;
			}
			
			public bool PitchModOn {
				get => *((bool*) state.PitchModOn);
				set => *((bool*) state.PitchModOn) = value;
			}
			
			public bool NoiseOn {
				get => *((bool*) state.NoiseOn);
				set => *((bool*) state.NoiseOn) = value;
			}
			
			public bool EchoOn {
				get => *((bool*) state.EchoOn);
				set => *((bool*) state.EchoOn) = value;
			}
			
			public bool End {
				get => *((bool*) state.End);
				set => *((bool*) state.End) = value;
			}
			
			internal VoiceProps(DLL.DspVoiceState state) {
				this.state = state;
			}
		}
		
		DLL.DspGlobalState state;
			
		public bool Reset {
			get => *((bool*) state.Reset);
			set => *((bool*) state.Reset) = value;
		}
			
		public bool Mute {
			get => *((bool*) state.Mute);
			set => *((bool*) state.Mute) = value;
		}
			
		public byte NoiseRate {
			get => (byte) (*((byte*) state.NoiseRate) & 0x1F);
			set => *((byte*) state.NoiseRate) = (byte) (value & 0x1F);
		}
			
		public sbyte MainVolumeLeft {
			get => *((sbyte*) state.MainVolLeft);
			set => *((sbyte*) state.MainVolLeft) = value;
		}
			
		public sbyte MainVolumeRight {
			get => *((sbyte*) state.MainVolRight);
			set => *((sbyte*) state.MainVolRight) = value;
		}
		
		public byte SourceTablePage {
			get => *((byte*) state.BrrBank);
			set => *((byte*) state.BrrBank) = value;
		}
		
		public EchoProps    Echo  { get; }
		public VoiceProps[] Voice { get; }
				
		internal Properties(DLL.DspGlobalState state, DLL.DspVoiceState[] voiceStates) {
			this.state = state;
			Echo       = new(state);
			Voice      = new VoiceProps[8];
			
			for (var i = 0; i < 8; i++) {
				if (i >= voiceStates.Length) {
					break;
				}
				Voice[i] = new(voiceStates[i]);
			}
		}
	}
	
	public Emulator Emulator { get; init; }
	
	public UInt8Buffer ARAM     { get; }
	public UInt8Buffer Register { get; }
	
	public Properties State { get; }
	
	public long CurrentCycle {
		get {
			var result = (long) DLL.DspGetCurrentCycle(Emulator.handle);
			if (result == -1) {
				throw new StateError(); // TODO: or NullError
			}
			
			return result;
		}
	}
	
	internal DSP(Emulator emulator) {
		unsafe {
			Emulator = emulator;
			
			var aramPtr = DLL.DspGetAramPtr(Emulator.handle);
			if (aramPtr == IntPtr.Zero) { throw new StateError(); }
			
			var regMapPtr = DLL.DspGetRegMapPtr(Emulator.handle);
			if (regMapPtr == IntPtr.Zero) { throw new StateError(); }
			
			ARAM     = new((byte*) aramPtr, 65536);
			Register = new((byte*) regMapPtr, 128);
			
			var globalState = DLL.DspGetGlobalState(Emulator.handle);
			if (globalState.EchoFeedback == IntPtr.Zero) {
				throw new StateError();
			}
			
			List<DLL.DspVoiceState> voiceStates = new();
			
			for (var i = 0; i < 8; i++) {
				var v = DLL.DspGetVoiceState((byte) i, Emulator.handle);
				if (v.VolLeft == IntPtr.Zero) {
					throw new StateError();
				}
				voiceStates.Add(v);
			}
			
			State = new(globalState, voiceStates.ToArray());
		}
	}
}