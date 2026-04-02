namespace Apollo;

using System.Runtime.InteropServices;

internal partial class DLL {
	[LibraryImport("apollo", EntryPoint = "spc_load")]
	[return: MarshalAs(UnmanagedType.I1)]
	public static partial bool SpcLoad(IntPtr fileData, UInt64 length, Emulator.Handle? emuPtr);
	
	[LibraryImport("apollo", EntryPoint = "spc_get_metadata")]
	public static partial SpcMetadata SpcGetMetadata(Emulator.Handle? emuPtr);
	
	[LibraryImport("apollo", EntryPoint = "spc_get_cpu_state")]
	public static partial SpcCpuState SpcGetCpuState(Emulator.Handle? emuPtr);
	
	[StructLayout(LayoutKind.Sequential)]
	internal unsafe struct SpcMetadata {
		public byte IsValid;
		
		public fixed byte    Title[257];
		public fixed byte   Artist[257];
		public fixed byte     Game[257];
		public fixed byte   Dumper[257];
		public fixed byte Comments[257];
		
		public Int64 Month;
		public Int64 Day;
		public Int64 Year;
		
		public fixed byte DateOther[12];
		
		public Int64 LengthInSeconds;
		public Int64 FadeLengthInMS;
		
		public fixed byte ChannelsDisabled[8];
		
		public Int64 EmulatorId;
		
		public       byte  HasOstTrack;
		public fixed byte  OstTitle[257];
		public       Int64 OstDisc;
		public fixed byte  OstTrack[2];
		
		public fixed byte  Publisher[257];
		public       Int64 CopyrightYear;
		
		public Int64 IntroLengthInTimer2Steps;
		public Int64 LoopLengthInTimer2Steps;
		public Int64 EndLengthInTimer2Steps;
		public Int64 LoopTimes;
		
		public Int64 MixingLevel;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	internal struct SpcCpuState {
		public IntPtr A;
		public IntPtr X;
		public IntPtr Y;
		
		public IntPtr SP;
		public IntPtr PC;
		
		public IntPtr PSW;
		public IntPtr Mode;
		
		public IntPtr InstructionStartPC;
		public IntPtr InstructionStartCycle;
	}
}