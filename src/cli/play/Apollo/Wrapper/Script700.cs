namespace Apollo;

using System.Runtime.InteropServices;

internal partial class DLL {
	[LibraryImport("apollo", EntryPoint = "script700_get_state")]
	public static partial Script700State Script700GetState(Emulator.Handle? emuPtr);
	
	[LibraryImport("apollo", EntryPoint = "script700_load_binary_file")]
	[return: MarshalAs(UnmanagedType.I1)]
	public static partial bool Script700LoadBinaryFile(Emulator.Handle? emuPtr, IntPtr binData, UInt64 len);
	
	[LibraryImport("apollo", EntryPoint = "script700_load_bytecode")]
	[return: MarshalAs(UnmanagedType.I1)]
	public static partial bool Script700LoadBytecode(Emulator.Handle? emuPtr, IntPtr scriptBytecode, UInt64 len);
	
	[LibraryImport("apollo", EntryPoint = "script700_load_data")]
	[return: MarshalAs(UnmanagedType.I1)]
	public static partial bool Script700LoadData(Emulator.Handle? emuPtr, IntPtr data, UInt64 len);
	
	[LibraryImport("apollo", EntryPoint = "script700_load_label_addresses")]
	[return: MarshalAs(UnmanagedType.I1)]
	public static partial bool Script700LoadLabelAddresses(Emulator.Handle? emuPtr, IntPtr labelAddresses, UInt64 len);
	
	[LibraryImport("apollo", EntryPoint = "script700_load_label_remappings")]
	[return: MarshalAs(UnmanagedType.I1)]
	public static partial bool Script700LoadLabelRemappings(Emulator.Handle? emuPtr, IntPtr labelRemappings, UInt64 len);
	
	[LibraryImport("apollo", EntryPoint = "script700_is_running")]
	[return: MarshalAs(UnmanagedType.I1)]
	public static partial bool Script700IsRunning(Emulator.Handle? emuPtr);
	
	[LibraryImport("apollo", EntryPoint = "script700_get_wait_until_cycle")]
	public static partial UInt64 Script700GetWaitUntilCycle(Emulator.Handle? emuPtr);
	
	[LibraryImport("apollo", EntryPoint = "script700_get_script_bytecode_length")]
	public static partial UInt32 Script700GetScriptBytecodeLength(Emulator.Handle? emuPtr);
	
	[LibraryImport("apollo", EntryPoint = "script700_get_script_bytecode")]
	public static partial IntPtr Script700GetScriptBytecode(Emulator.Handle? emuPtr);
	
	[LibraryImport("apollo", EntryPoint = "script700_get_data_length")]
	public static partial UInt32 Script700GetDataLength(Emulator.Handle? emuPtr);
	
	[LibraryImport("apollo", EntryPoint = "script700_get_data")]
	public static partial IntPtr Script700GetData(Emulator.Handle? emuPtr);
	
	[LibraryImport("apollo", EntryPoint = "script700_get_label_addresses")]
	public static partial IntPtr Script700GetLabelAddresses(Emulator.Handle? emuPtr);
	
	[StructLayout(LayoutKind.Sequential)]
	internal struct Script700State {
		public IntPtr PortIn;
		
		public IntPtr Work;
		public IntPtr Cmp;
		
		public IntPtr Callstack;
		public IntPtr SP;
		public IntPtr SPTop;
		
		public IntPtr CallstackOn;
		public IntPtr PortQueueOn;
		
		public IntPtr PC;
		public IntPtr Step;
		
		public IntPtr CurCycle;
		public IntPtr BeginCycle;
		public IntPtr SyncPoint;
		public IntPtr LastCycle;
		
		public IntPtr WaitDevice;
		public IntPtr WaitPort;
	}
}