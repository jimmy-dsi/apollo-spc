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
	public static partial Int32 EmuStepNCycles(UInt32 cycles, Emulator.Handle emuPtr, [MarshalAs(UnmanagedType.I1)] bool breakpointsEnabled);
	
	[LibraryImport("apollo", EntryPoint = "emu_step_n_cycles_fast")]
	public static partial UInt32 EmuStepNCyclesFast(UInt32 cycles, Emulator.Handle emuPtr);
	
	[LibraryImport("apollo", EntryPoint = "emu_get_render_buffer")]
	public static partial IntPtr EmuGetRenderBuffer(byte channel, Emulator.Handle emuPtr);
	
	[LibraryImport("apollo", EntryPoint = "emu_get_render_buffer_len")]
	public static partial UInt32 EmuGetRenderBufferLen(Emulator.Handle emuPtr);
	
	[LibraryImport("apollo", EntryPoint = "emu_get_render_position")]
	public static partial UInt32 EmuGetRenderPosition(Emulator.Handle emuPtr);
	
	[LibraryImport("apollo", EntryPoint = "emu_toggle_voice")]
	[return: MarshalAs(UnmanagedType.I1)]
	public static partial bool EmuToggleVoice(Emulator.Handle emuPtr, byte voiceIndex);
	
	[LibraryImport("apollo", EntryPoint = "emu_enable_voice")]
	[return: MarshalAs(UnmanagedType.I1)]
	public static partial bool EmuEnableVoice(Emulator.Handle emuPtr, byte voiceIndex);
	
	[LibraryImport("apollo", EntryPoint = "emu_disable_voice")]
	[return: MarshalAs(UnmanagedType.I1)]
	public static partial bool EmuDisableVoice(Emulator.Handle emuPtr, byte voiceIndex);
	
	[LibraryImport("apollo", EntryPoint = "emu_toggle_main_voice")]
	[return: MarshalAs(UnmanagedType.I1)]
	public static partial bool EmuToggleMainVoice(Emulator.Handle emuPtr, byte voiceIndex);
	
	[LibraryImport("apollo", EntryPoint = "emu_enable_main_voice")]
	[return: MarshalAs(UnmanagedType.I1)]
	public static partial bool EmuEnableMainVoice(Emulator.Handle emuPtr, byte voiceIndex);
	
	[LibraryImport("apollo", EntryPoint = "emu_disable_main_voice")]
	[return: MarshalAs(UnmanagedType.I1)]
	public static partial bool EmuDisableMainVoice(Emulator.Handle emuPtr, byte voiceIndex);
	
	[LibraryImport("apollo", EntryPoint = "emu_toggle_echo_voice")]
	[return: MarshalAs(UnmanagedType.I1)]
	public static partial bool EmuToggleEchoVoice(Emulator.Handle emuPtr, byte voiceIndex);
	
	[LibraryImport("apollo", EntryPoint = "emu_enable_echo_voice")]
	[return: MarshalAs(UnmanagedType.I1)]
	public static partial bool EmuEnableEchoVoice(Emulator.Handle emuPtr, byte voiceIndex);
	
	[LibraryImport("apollo", EntryPoint = "emu_disable_echo_voice")]
	[return: MarshalAs(UnmanagedType.I1)]
	public static partial bool EmuDisableEchoVoice(Emulator.Handle emuPtr, byte voiceIndex);
	
	[LibraryImport("apollo", EntryPoint = "emu_check_breakpoint")]
	[return: MarshalAs(UnmanagedType.I1)]
	public static partial bool EmuCheckBreakpoint(Emulator.Handle emuPtr);
	
	[LibraryImport("apollo", EntryPoint = "emu_consume_breakpoint")]
	[return: MarshalAs(UnmanagedType.I1)]
	public static partial bool EmuConsumeBreakpoint(Emulator.Handle emuPtr);
	
	[LibraryImport("apollo", EntryPoint = "emu_copy")]
	[return: MarshalAs(UnmanagedType.I1)]
	public static partial bool EmuCopy(Emulator.Handle destEmuPtr, Emulator.Handle srcEmuPtr);
	
	[LibraryImport("apollo", EntryPoint = "emu_acquire_lock")]
	[return: MarshalAs(UnmanagedType.I1)]
	public static partial bool EmuAcquireLock(Emulator.Handle emuPtr);
	
	[LibraryImport("apollo", EntryPoint = "emu_release_lock")]
	[return: MarshalAs(UnmanagedType.I1)]
	public static partial bool EmuReleaseLock(Emulator.Handle emuPtr);
	
	[LibraryImport("apollo", EntryPoint = "emu_get_last_result")]
	public static partial UInt32 EmuGetLastResult(Emulator.Handle emuPtr);
	
	[LibraryImport("apollo", EntryPoint = "emu_get_last_error")]
	public static partial UInt32 EmuGetLastError(Emulator.Handle emuPtr);
}