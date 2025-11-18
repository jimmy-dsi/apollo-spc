namespace Apollo;

using System.Runtime.InteropServices;

internal partial class DLL {
	[LibraryImport("apollo", EntryPoint = "init")]
	[return: MarshalAs(UnmanagedType.I1)]
	public static partial bool Init();
	
	[LibraryImport("apollo", EntryPoint = "deinit")]
	[return: MarshalAs(UnmanagedType.I1)]
	public static partial bool Deinit();
}