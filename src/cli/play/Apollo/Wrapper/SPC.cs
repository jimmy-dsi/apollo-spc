namespace Apollo;

using System.Runtime.InteropServices;

internal partial class DLL {
	[LibraryImport("apollo", EntryPoint = "spc_load")]
	[return: MarshalAs(UnmanagedType.I1)]
	public static partial bool SpcLoad(IntPtr fileData, UInt64 length, Emulator.Handle? emuPtr);
}