namespace Apollo;

using System.Runtime.InteropServices;

internal partial class DLL {
	[LibraryImport("apollo", EntryPoint = "dsp_get_aram_ptr")]
	public static partial IntPtr DspGetAramPtr(Emulator.Handle? emuPtr);
	
	[LibraryImport("apollo", EntryPoint = "dsp_get_reg_map_ptr")]
	public static partial IntPtr DspGetRegMapPtr(Emulator.Handle? emuPtr);
	
	[LibraryImport("apollo", EntryPoint = "dsp_get_current_cycle")]
	public static partial UInt64 DspGetCurrentCycle(Emulator.Handle? emuPtr);
	
	[LibraryImport("apollo", EntryPoint = "dsp_get_global_state")]
	public static partial DspGlobalState DspGetGlobalState(Emulator.Handle? emuPtr);
	
	[LibraryImport("apollo", EntryPoint = "dsp_get_global_debug_state")]
	public static partial DspDebugGlobalState DspGetGlobalDebugState(Emulator.Handle? emuPtr);
	
	[LibraryImport("apollo", EntryPoint = "dsp_get_voice_state")]
	public static partial DspVoiceState DspGetVoiceState(Byte voiceIdx, Emulator.Handle? emuPtr);
	
	[LibraryImport("apollo", EntryPoint = "dsp_get_voice_debug_state")]
	public static partial DspDebugVoiceState DspGetVoiceDebugState(Byte voiceIdx, Emulator.Handle? emuPtr);
	
	[LibraryImport("apollo", EntryPoint = "dsp_get_sample_usage_flags")]
	public static partial DspSampleUsageFlags DspGetSampleUsageFlags(Emulator.Handle? emuPtr);
	
	[LibraryImport("apollo", EntryPoint = "dsp_reset_sample_usage")]
	[return: MarshalAs(UnmanagedType.I1)]
	public static partial bool DspResetSampleUsage(byte sampleId, Emulator.Handle emuPtr);
	
	[LibraryImport("apollo", EntryPoint = "dsp_decode_brr_from_buffer")]
	public static partial Int32 DspDecodeBrrFromBuffer(IntPtr inputBuffer,
	                                                   UInt16 inputLen,
	                                                   UInt16 offset,
	                                                   IntPtr decodeBuffer,
	                                                   UInt32 bufferLen,
	                                                   Int16 oldDecoded,
	                                                   Int16 olderDecoded);
	
	[LibraryImport("apollo", EntryPoint = "dsp_decode_brr_at_address")]
	public static partial Int32 DspDecodeBrrAtAddress(UInt16 addr,
	                                                  IntPtr decodeBuffer,
	                                                  UInt32 bufferLen,
	                                                  Int16 oldDecoded,
	                                                  Int16 olderDecoded,
	                                                  Emulator.Handle emuPtr);
	
	[StructLayout(LayoutKind.Sequential)]
	internal struct DspGlobalState {
		public IntPtr EchoFeedback;
		public IntPtr EchoVolLeft;
		public IntPtr EchoVolRight;
		
		public IntPtr EchoFIR;
		
		public IntPtr EsaPage;
		public IntPtr EchoDelay;
		
		public IntPtr EchoReadonly;
		public IntPtr Reset;
		public IntPtr Mute;
		public IntPtr NoiseRate;
		
		public IntPtr MainVolLeft;
		public IntPtr MainVolRight;
		
		public IntPtr BrrBank;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	internal struct DspDebugGlobalState {
		public IntPtr EchoOffset;
		public IntPtr EchoAddress;
		public IntPtr EchoPage;
		public IntPtr EchoLength;
		
		public IntPtr LastEchoReadCycle;
		public IntPtr LastEchoReadAddr;
		public IntPtr LastEchoReadLeft;
		public IntPtr LastEchoReadRight;
		
		public IntPtr LastEchoWriteCycle;
		public IntPtr LastEchoWriteAddr;
		public IntPtr LastEchoWriteLeft;
		public IntPtr LastEchoWriteRight;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	internal struct DspVoiceState {
		public IntPtr VolLeft;
		public IntPtr VolRight;
		
		public IntPtr Pitch;
		public IntPtr Source;
		
		public IntPtr Adsr0;
		public IntPtr Adsr1;
		public IntPtr Gain;
		
		public IntPtr Envx;
		
		public IntPtr KeyOn;
		public IntPtr KeyOff;
		
		public IntPtr PitchModOn;
		public IntPtr NoiseOn;
		public IntPtr EchoOn;
		public IntPtr End;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	internal struct DspDebugVoiceState {
		public IntPtr Buffer;
		public IntPtr BufferOffset;
		public IntPtr GaussianOffset;
		public IntPtr BrrAddress;
		public IntPtr BrrOffset;
		public IntPtr KeyOnDelay;
		public IntPtr EnvMode;
		public IntPtr EnvLevel;
		
		public IntPtr GainEnvLevel;
		public IntPtr KeyLatch;
		public IntPtr KeyOn;
		public IntPtr KeyOff;
		public IntPtr PitchModOn;
		public IntPtr NoiseOn;
		public IntPtr EchoOn;
		public IntPtr End;
		public IntPtr Looped;
		
		public IntPtr QueuedSRCN;
		public IntPtr CurrentSRCN;
		
		public IntPtr TruePitch;
		public IntPtr BrrSubOffset;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	internal struct DspSampleUsageFlags {
		public IntPtr Flags;
	}
}