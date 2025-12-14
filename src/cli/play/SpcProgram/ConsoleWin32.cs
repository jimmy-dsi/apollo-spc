namespace SpcProgram;

using System.Runtime.InteropServices;

public static partial class ConsoleWin32 {
	const int  StdOutputHandle                 = -11;
	const uint EnableVirtualTerminalProcessing = 0x0004;
	
	#if LINUX
		// No Win32 library imports for Linux build
	#else
		[LibraryImport("kernel32.dll", SetLastError = true)]
		private static partial IntPtr GetStdHandle(int nStdHandle);
		
		[LibraryImport("kernel32.dll")]
		[return: MarshalAs(UnmanagedType.I1)]
		private static partial bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);
		
		[LibraryImport("kernel32.dll")]
		[return: MarshalAs(UnmanagedType.I1)]
		private static partial bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
	#endif
	
	public static void EnableCmdAnsiCodes() {
		#if LINUX
			// Do nothing if we are on Linux
		#else
			var handle = GetStdHandle(StdOutputHandle);
			GetConsoleMode(handle, out var mode);
			mode |= EnableVirtualTerminalProcessing;
			SetConsoleMode(handle, mode);
		#endif
	}
}