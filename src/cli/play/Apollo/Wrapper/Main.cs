namespace Apollo;

using System.Runtime.InteropServices;

internal partial class DLL {
	[LibraryImport("apollo", EntryPoint = "init")]
	[return: MarshalAs(UnmanagedType.I1)]
	public static partial bool Init();
	
	[LibraryImport("apollo", EntryPoint = "deinit")]
	[return: MarshalAs(UnmanagedType.I1)]
	public static partial bool Deinit();
	
	[LibraryImport("apollo", EntryPoint = "get_last_result")]
	public static partial UInt32 GetLastResult();
	
	[LibraryImport("apollo", EntryPoint = "get_last_error")]
	public static partial UInt32 GetLastError();
}