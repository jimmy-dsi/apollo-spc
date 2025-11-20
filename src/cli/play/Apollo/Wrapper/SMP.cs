namespace Apollo;

using System.Runtime.InteropServices;

internal partial class DLL {
	[LibraryImport("apollo", EntryPoint = "smp_read_byte")]
	public static partial byte SmpReadByte(UInt16 address, Emulator.Handle? emuPtr);
	
	[LibraryImport("apollo", EntryPoint = "smp_read_word")]
	public static partial UInt16 SmpReadWord(UInt16 address, Emulator.Handle? emuPtr);
	
	[LibraryImport("apollo", EntryPoint = "smp_read_page")]
	public static partial SmpMemoryPage SmpReadPage(UInt16 address, Emulator.Handle? emuPtr);
	
	[LibraryImport("apollo", EntryPoint = "smp_debug_read_byte")]
	public static partial byte SmpDebugReadByte(UInt16 address, Emulator.Handle? emuPtr);
	
	[LibraryImport("apollo", EntryPoint = "smp_debug_read_word")]
	public static partial UInt16 SmpDebugReadWord(UInt16 address, Emulator.Handle? emuPtr);
	
	[LibraryImport("apollo", EntryPoint = "smp_debug_read_page")]
	public static partial SmpMemoryPage SmpDebugReadPage(UInt16 address, Emulator.Handle? emuPtr);
	
	[LibraryImport("apollo", EntryPoint = "smp_write_byte")]
	[return: MarshalAs(UnmanagedType.I1)]
	public static partial bool SmpWriteByte(UInt16 address, byte value, Emulator.Handle? emuPtr);
	
	[LibraryImport("apollo", EntryPoint = "smp_write_word")]
	[return: MarshalAs(UnmanagedType.I1)]
	public static partial bool SmpWriteWord(UInt16 address, UInt16 value, Emulator.Handle? emuPtr);
	
	[LibraryImport("apollo", EntryPoint = "smp_get_boot_rom_ptr")]
	public static partial IntPtr SmpGetBootRomPtr(Emulator.Handle? emuPtr);
	
	[LibraryImport("apollo", EntryPoint = "smp_get_state")]
	public static partial SmpState SmpGetState(Emulator.Handle? emuPtr);
	
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	internal unsafe struct SmpMemoryPage {
		public byte _IsError;
		public fixed byte Array[256];
		
		public bool IsError => _IsError != 0;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	internal unsafe struct SmpState {
		public IntPtr Timer0Stage0;
		public IntPtr Timer0Stage1;
		public IntPtr Timer0Stage2;
		
		public IntPtr Timer1Stage0;
		public IntPtr Timer1Stage1;
		public IntPtr Timer1Stage2;
		
		public IntPtr Timer2Stage0;
		public IntPtr Timer2Stage1;
		public IntPtr Timer2Stage2;
		
		public IntPtr GlobalTimerDisable;
		public IntPtr RamWriteEnable;
		public IntPtr RamDisable;
		public IntPtr GlobalTimerEnable;
		public IntPtr RamWaitstates;
		public IntPtr IoWaitstates;
		
		public IntPtr TimerOnFlags;
		public IntPtr UseBootRom;
		
		public IntPtr DspAddress;
		
		public IntPtr InputPorts;
		public IntPtr OutputPorts;
		
		public IntPtr Aux;
		
		public IntPtr TimerDividers;
		public IntPtr TimerOutputs;
	}
}