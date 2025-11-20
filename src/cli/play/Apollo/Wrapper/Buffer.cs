namespace Apollo;

using System.Runtime.InteropServices;

internal partial class DLL {
	[LibraryImport("apollo", EntryPoint = "buffer_create")]
	public static partial IntPtr BufferCreate(UInt32 numBytes);
	
	[LibraryImport("apollo", EntryPoint = "buffer_destroy")]
	[return: MarshalAs(UnmanagedType.I1)]
	public static partial bool BufferDestroy(IntPtr bufPtr, UInt32 numBytes);
}