namespace Apollo;

using System.Runtime.InteropServices;

internal partial class DLL {
	[LibraryImport("apollo", EntryPoint = "emu_create")]
	public static partial Emulator.Handle EmuCreate([MarshalAs(UnmanagedType.I1)] bool setAsMainInstance);
	
	[LibraryImport("apollo", EntryPoint = "emu_get_main_instance")]
	public static partial Emulator.Handle EmuGetMainInstance();
	
	[LibraryImport("apollo", EntryPoint = "emu_reassign_main_instance")]
	[return: MarshalAs(UnmanagedType.I1)]
	public static partial bool EmuReassignMainInstance(Emulator.Handle? emuPtr);
	
	[LibraryImport("apollo", EntryPoint = "emu_destroy")]
	[return: MarshalAs(UnmanagedType.I1)]
	public static partial bool EmuDestroy(Emulator.Handle emuPtr);
	
	[LibraryImport("apollo", EntryPoint = "emu_step_cycle")]
	[return: MarshalAs(UnmanagedType.I1)]
	public static partial bool EmuStepCycle(Emulator.Handle emuPtr);
	
	[LibraryImport("apollo", EntryPoint = "emu_step_cycle_fast")]
	[return: MarshalAs(UnmanagedType.I1)]
	public static partial bool EmuStepCycleFast(Emulator.Handle emuPtr);
	
	[LibraryImport("apollo", EntryPoint = "emu_step_instruction")]
	[return: MarshalAs(UnmanagedType.I1)]
	public static partial bool EmuStepInstruction(Emulator.Handle emuPtr);
	
	[LibraryImport("apollo", EntryPoint = "emu_step_n_cycles")]
	public static partial UInt32 EmuStepNCycles(UInt32 cycles, Emulator.Handle emuPtr);
	
	[LibraryImport("apollo", EntryPoint = "emu_step_n_cycles_fast")]
	public static partial UInt32 EmuStepNCyclesFast(UInt32 cycles, Emulator.Handle emuPtr);
	
	[LibraryImport("apollo", EntryPoint = "emu_get_render_buffer")]
	public static partial IntPtr EmuGetRenderBuffer(byte channel, Emulator.Handle emuPtr);
	
	[LibraryImport("apollo", EntryPoint = "emu_get_render_buffer_len")]
	public static partial UInt32 EmuGetRenderBufferLen(Emulator.Handle emuPtr);
	
	[LibraryImport("apollo", EntryPoint = "emu_get_render_position")]
	public static partial UInt32 EmuGetRenderPosition(Emulator.Handle emuPtr);
}