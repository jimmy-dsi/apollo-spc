namespace SpcProgram;

using System.Runtime.InteropServices;
using Jimbl;

public static partial class ConsoleOS {
	const int  StdInputHandle  = -10;
	const int  StdOutputHandle = -11;
	
	const uint EnableProcessedInput            = 0x0001;
	const uint EnableLineInput                 = 0x0002;
	const uint EnableEchoInput                 = 0x0004;
	
	const uint EnableVirtualTerminalProcessing = 0x0004;
	const uint EnableQuickEditMode             = 0x0040;
	const uint EnableExtendedFlags             = 0x0080;
	const uint EnableVirtualTerminalInput      = 0x0200;
	
	const uint EnableICanon = 0x00000002;
	const uint EnableEcho   = 0x00000008;
	
	#if LINUX || OSX
		[LibraryImport("libc", EntryPoint = "tcgetattr", SetLastError = true)]
		private static unsafe partial int TCGetAttr(int fd, Termios* termios);

		[LibraryImport("libc", EntryPoint = "tcsetattr", SetLastError = true)]
		private static unsafe partial int TCSetAttr(int fd, int optional_actions, Termios* termios);
	
		[StructLayout(LayoutKind.Explicit, Size = 64)]
		public struct Termios {
			[FieldOffset(8)]
			public uint MacLocalFlags;
			[FieldOffset(12)]
			public uint LinuxLocalFlags;

			public uint LocalFlags {
				get => OS.Get() == OS.Platform.OSX ? MacLocalFlags : LinuxLocalFlags;
				set {
					if (OS.Get() == OS.Platform.OSX) {
						MacLocalFlags = value;
					}
					else {
						LinuxLocalFlags = value;
					}
				}
			}
		}
	
		static Termios termiosSettings;
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
		#if LINUX || OSX
			// Do nothing if we are on Linux or Mac
		#else
			var handle = GetStdHandle(StdOutputHandle);
			GetConsoleMode(handle, out var mode);
			mode |= EnableVirtualTerminalProcessing;
			SetConsoleMode(handle, mode);
		#endif
	}
	
	static uint? inputMode = null;
	
	public static void EnableRawMode() {
		#if LINUX || OSX
			// Do nothing if we are on Linux or Mac
		#else
			var handle = GetStdHandle(StdInputHandle);
			GetConsoleMode(handle, out var mode);
			inputMode ??= mode;
			mode |= EnableVirtualTerminalInput | EnableExtendedFlags;
			mode &= ~EnableLineInput & ~EnableEchoInput & ~EnableProcessedInput & ~EnableQuickEditMode;
			SetConsoleMode(handle, mode);
		#endif
	}
	
	public static void RestoreInputMode() {
		#if LINUX || OSX
			// Do nothing if we are on Linux or Mac
		#else
			var handle = GetStdHandle(StdInputHandle);
			if (inputMode is uint mode) {
				SetConsoleMode(handle, mode);
			}
		#endif
	}
	
	public static void SetRawMode() {
		unsafe {
			#if LINUX || OSX
				fixed (Termios* t = &termiosSettings) {
					if (TCGetAttr(0, t) != 0) throw new Exception("Could not enable raw mode for an unknown reason");
					
					var flags = termiosSettings.LocalFlags;
					flags &= ~EnableICanon;
					flags &= ~EnableEcho;
					termiosSettings.LocalFlags = flags;

					*t = termiosSettings;
					
					TCSetAttr(0, 0, t);
				}
			#else
				// TODO
			#endif
		}
	}
}