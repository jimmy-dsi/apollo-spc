namespace Apollo;

using Jimbl;

using System.Text;

public partial class Script700 {
	static Dictionary<string, int> mnemonicTable = new() {
		["a"]   = 2,
		["bp"]  = 1,
		["c"]   = 2,
		["d"]   = 2,
		["f"]   = 0,
		["f0"]  = 0,
		["f1"]  = 0,
		["i"]   = 0,
		["ib"]  = 0,
		["iv"]  = 1,
		["iw"]  = 1,
		["m"]   = 2,
		["n"]   = 3,
		["nop"] = 0,
		["r"]   = 0,
		["r0"]  = 0,
		["r1"]  = 0,
		["q"]   = 0,
		["s"]   = 2,
		["sw"]  = 0,
		["u"]   = 2,
		["w"]   = 1,
		["wi"]  = 1,
		["wo"]  = 1,
		["bra"] = 1,
		["beq"] = 1,
		["bne"] = 1,
		["bge"] = 1,
		["ble"] = 1,
		["bgt"] = 1,
		["blt"] = 1,
		["bcc"] = 1,
		["blo"] = 1,
		["bhi"] = 1,
		["bcs"] = 1,
	};
	
	public class Instruction {
		internal string   mnemonic;
		internal string[] parameters;
		internal bool     lineMarker;
		
		internal bool IsLabel => mnemonic.StartsWith(':');
		
		internal string Compile() {
			if (lineMarker) {
				return ";@line";
			}
			
			if (IsLabel) {
				return mnemonic;
			}
			
			if (mnemonic == "n") {
				var op = parameters[1];
				if (op == "+") {
					return $"a {parameters[0]} {parameters[2]}";
				}
				else if (op == "-") {
					return $"s {parameters[0]} {parameters[2]}";
				}
				else if (op == "*") {
					return $"u {parameters[0]} {parameters[2]}";
				}
				else if (op == "/") {
					return $"d {parameters[0]} {parameters[2]}";
				}
			}
			
			return $"{mnemonic} {string.Join(' ', parameters)}".Trim();
		}
		
		internal static Instruction[] Parse(string line) {
			var commentIndex = line.IndexOf(';');
			if (commentIndex >= 0) {
				line = line[..commentIndex];
			}
			
			line = line.Trim();
			if (line == "") {
				return [];
			}
			
			List<Instruction> instructions = new() {
				new() {
					mnemonic   = "",
					parameters = [],
					lineMarker = true
				}
			};
			
			var argIndex = 0;
			
			var    args = line.Trim().Split(new char[] {' ', '\t'}, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			string mnemonic;
			
			while (argIndex < args.Length) {
				mnemonic = args[argIndex];
				
				if (mnemonic.StartsWith(':')) {
					Instruction label = new() {
						mnemonic   = mnemonic,
						parameters = []
					};
				
					instructions.Add(label);
					argIndex++;
					
					continue;
				}
				
				if (!mnemonicTable.ContainsKey(mnemonic)) {
					return instructions.ToArray();
				}
			
				var expectedParams = mnemonicTable[mnemonic];
				
				if (args.Length < argIndex + expectedParams + 1) {
					return instructions.ToArray();
				}
				
				Instruction instr = new() {
					mnemonic   = mnemonic,
					parameters = args[(argIndex + 1) .. (argIndex + expectedParams + 1)]
				};
				
				instructions.Add(instr);
				argIndex += expectedParams + 1;
			}
			
			return instructions.ToArray();
		}
	}
	
	class NybbleStream {
		bool parity = false;
		
		public List<List<byte>> Bytes  = new();
		public List<(int, int)> Labels = new();
		
		public void AppendNybble(byte value) {
			var lastBytes = Bytes[^1];
			
			if (parity) {
				var loNybble = (byte) (value & 0xF);
				lastBytes[^1] |= loNybble;
			}
			else {
				var hiNybble = (value & 0xF) << 4;
				lastBytes.Add((byte) hiNybble);
			}
			
			parity = !parity;
		}
		
		public void Parse(string line) {
			var commentIndex = line.IndexOf(';');
			if (commentIndex >= 0) {
				line = line[..commentIndex];
			}
			
			line = line.Trim();
			if (line == "") {
				return;
			}
			
			Bytes.Add(new());
			var isLabel  = false;
			var labelStr = "";
			
			foreach (var c in line) {
				if (!isLabel && ('0' <= c && c <= '9' || 'a' <= c.ToLower() && c.ToLower() <= 'f')) {
					var nybble = byte.Parse(c.ToString(), System.Globalization.NumberStyles.HexNumber);
					AppendNybble(nybble);
				}
				else if (isLabel && ('0' <= c && c <= '9')) {
					labelStr += c;
				}
				else if (!isLabel && c == ':') {
					isLabel = true;
				}
				else if (c is not ' ' and not '\t' and not '\r' and not '\n') {
					return; // Prematurely stop parsing line if we hit an invalid character
				}
				else if (isLabel) {
					var currentByteIndex = 0;
					foreach (var bytes in Bytes) {
						currentByteIndex += bytes.Count;
					}
					
					Labels.Add((currentByteIndex, int.Parse(labelStr)));
					
					isLabel  = false;
					labelStr = "";
				}
			}
		}
		
		public string Compile() {
			StringBuilder sb = new();
			var currentByte = 0;
			var labelIndex  = 0;
			
			foreach (var bytes in Bytes) {
				sb.Append(";@line").Append('\n');
				
				foreach (var (i, b) in bytes.Enum()) {
					if (labelIndex < Labels.Count && currentByte == Labels[^1].Item1) {
						if (sb[^1] != '\n') {
							sb.Append('\n');
						}
						sb.Append(':').Append(Labels[^1].Item2).Append('\n');
					}
					
					sb.Append(b.ToString("X2"));
					
					if (i % 8 == 7) {
						sb.Append('\n');
					}
					else {
						sb.Append(' ');
					}
					
					currentByte++;
				}
			}
			
			return sb.ToString();
		}
	}
	
	public enum WaitDevice {
		None = 0, Input = 1, Output = 2
	}
	
	public unsafe class Properties {
		Emulator           emu;
		DLL.Script700State state;
		
		public UInt8Buffer  PortIn    { get; }
		   
		public UInt32Buffer Work      { get; }
		public UInt32Buffer Cmp       { get; }
		
		public UInt32Buffer Callstack { get; }
		
		public byte SP {
			get {
				emu.MaybeAcquireLock();
				try     { return *((byte*) state.SP); }
				finally { emu.MaybeReleaseLock(); }
			}
		}
		
		public byte SPTop {
			get {
				emu.MaybeAcquireLock();
				try     { return *((byte*) state.SPTop); }
				finally { emu.MaybeReleaseLock(); }
			}
		}
		
		public bool CallstackOn {
			get {
				emu.MaybeAcquireLock();
				try     { return *((bool*) state.CallstackOn); }
				finally { emu.MaybeReleaseLock(); }
			}
		}
		
		public bool PortQueueOn {
			get {
				emu.MaybeAcquireLock();
				try     { return *((bool*) state.PortQueueOn); }
				finally { emu.MaybeReleaseLock(); }
			}
		}
		
		public UInt32 PC {
			get {
				emu.MaybeAcquireLock();
				try     { return *((UInt32*) state.PC); }
				finally { emu.MaybeReleaseLock(); }
			}
		}
		
		public UInt32 Step {
			get {
				emu.MaybeAcquireLock();
				try     { return *((UInt32*) state.Step); }
				finally { emu.MaybeReleaseLock(); }
			}
		}
		
		public UInt64 CurCycle {
			get {
				emu.MaybeAcquireLock();
				try     { return *((UInt64*) state.CurCycle); }
				finally { emu.MaybeReleaseLock(); }
			}
		}
		
		public UInt64 BeginCycle {
			get {
				emu.MaybeAcquireLock();
				try     { return *((UInt64*) state.BeginCycle); }
				finally { emu.MaybeReleaseLock(); }
			}
		}
		
		public UInt64 SyncPoint {
			get {
				emu.MaybeAcquireLock();
				try     { return *((UInt64*) state.SyncPoint); }
				finally { emu.MaybeReleaseLock(); }
			}
		}
		
		public UInt64 LastCycle {
			get {
				emu.MaybeAcquireLock();
				try     { return *((UInt64*) state.LastCycle); }
				finally { emu.MaybeReleaseLock(); }
			}
		}
		
		public UInt64 WaitUntil {
			get {
				emu.MaybeAcquireLock();
				try {
					var waitUntil = DLL.Script700GetWaitUntilCycle(emu.handle);
					if (waitUntil == 0) {
						emu.CheckForError();
					}
					
					return waitUntil;
				}
				finally { emu.MaybeReleaseLock(); }
			}
		}
		
		public WaitDevice WaitDevice {
			get {
				emu.MaybeAcquireLock();
				try     { return (WaitDevice) (*((byte*) state.WaitDevice)); }
				finally { emu.MaybeReleaseLock(); }
			}
		}
		
		public byte WaitPort {
			get {
				emu.MaybeAcquireLock();
				try     { return *((byte*) state.WaitPort); }
				finally { emu.MaybeReleaseLock(); }
			}
		}
		
		internal Properties(Emulator emu, DLL.Script700State state) {
			this.emu   = emu;
			this.state = state;
			
			if (emu.MakeShared) {
				PortIn    = new  UInt8BufferShared(emu, (byte*)   state.PortIn,     4);
				
				Work      = new UInt32BufferShared(emu, (UInt32*) state.Work,       8);
				Cmp       = new UInt32BufferShared(emu, (UInt32*) state.Cmp,        2);
				
				Callstack = new UInt32BufferShared(emu, (UInt32*) state.Callstack, 64, isReadonly: true);
			}
			else {
				PortIn    = new((byte*)   state.PortIn,     4);
				
				Work      = new((UInt32*) state.Work,       8);
				Cmp       = new((UInt32*) state.Cmp,        2);
				
				Callstack = new((UInt32*) state.Callstack, 64, isReadonly: true);
			}
			
			PortIn = new((byte*) state.PortIn, 4);
		}
	}
	
	enum Mode {
		ScriptArea, ExtendCMD, DataArea
	}
	
	Buffer? scriptBytecode = null;
	Buffer? dataArea       = null;
	
	public Emulator Emulator { get; init; }
	
	public bool IsRunning {
		get {
			Emulator.MaybeAcquireLock();
			try {
				var isRunning = DLL.Script700IsRunning(Emulator.handle);
				if (!isRunning) {
					Emulator.CheckForError();
				}
				
				return isRunning;
			}
			finally { Emulator.MaybeReleaseLock(); }
		}
	}
	
	public int ScriptLength {
		get {
			Emulator.MaybeAcquireLock();
		
			try {
				unsafe {
					var size = DLL.Script700GetScriptBytecodeLength(Emulator.handle);
					if (size == 0) {
						Emulator.CheckForError();
					}
			
					return size.SafeSigned();
				}
			}
			finally {
				Emulator.MaybeReleaseLock();
			}
		}
	}
	
	public UInt32[] ScriptBytecode {
		get {
			Emulator.MaybeAcquireLock();
		
			try {
				unsafe {
					var ptr = DLL.Script700GetScriptBytecode(Emulator.handle);
					if (ptr == IntPtr.Zero) {
						var errorCode = DLL.EmuGetLastError(Emulator.handle);
						Error.Throw(errorCode);
					}
			
					var size = DLL.Script700GetScriptBytecodeLength(Emulator.handle);
					if (size == 0) {
						Emulator.CheckForError();
					}
			
					return new Span<UInt32>((void*) ptr, (int) size).ToArray();
				}
			}
			finally {
				Emulator.MaybeReleaseLock();
			}
		}
	}
	
	public int DataLength {
		get {
			Emulator.MaybeAcquireLock();
		
			try {
				unsafe {
					var size = DLL.Script700GetDataLength(Emulator.handle);
					if (size == 0) {
						Emulator.CheckForError();
					}
			
					return size.SafeSigned();
				}
			}
			finally {
				Emulator.MaybeReleaseLock();
			}
		}
	}
	
	public byte[] Data {
		get {
			Emulator.MaybeAcquireLock();
		
			try {
				unsafe {
					var ptr = DLL.Script700GetData(Emulator.handle);
					if (ptr == IntPtr.Zero) {
						var errorCode = DLL.EmuGetLastError(Emulator.handle);
						Error.Throw(errorCode);
					}
			
					var size = DLL.Script700GetDataLength(Emulator.handle);
					if (size == 0) {
						Emulator.CheckForError();
					}
			
					return new Span<byte>((void*) ptr, (int) size).ToArray();
				}
			}
			finally {
				Emulator.MaybeReleaseLock();
			}
		}
	}
	
	public UInt32[] LabelAddresses {
		get {
			Emulator.MaybeAcquireLock();
			
			try {
				unsafe {
					var ptr = DLL.Script700GetLabelAddresses(Emulator.handle);
					if (ptr == IntPtr.Zero) {
						var errorCode = DLL.EmuGetLastError(Emulator.handle);
						Error.Throw(errorCode);
					}
			
					return new Span<UInt32>((void*) ptr, 1024).ToArray();
				}
			}
			finally {
				Emulator.MaybeReleaseLock();
			}
		}
	}
	
	public Properties State { get; }
	
	internal Script700(Emulator emulator) {
		unsafe {
			Emulator = emulator;
			
			var state = DLL.Script700GetState(Emulator.handle);
			if (state.PortIn == IntPtr.Zero) {
				var errorCode = DLL.EmuGetLastError(Emulator.handle);
				Error.Throw(errorCode);
			}
			
			State = new(emulator, state);
		}
	}
	
	public void Disable() {
		Emulator.MaybeAcquireLock();
		
		try {
			DLL.Script700Disable(Emulator.handle);
		}
		finally {
			Emulator.MaybeReleaseLock();
		}
	}
	
	public void LoadBinaryFile(byte[] binaryData) {
		Emulator.MaybeAcquireLock();
		
		try {
			unsafe {
				Buffer? dataBuffer = new(binaryData.Length);
				// Copy buffer
				for (var i = 0; i < binaryData.Length; i++) {
					dataBuffer[i] = binaryData[i];
				}
				
				var result = DLL.Script700LoadBinaryFile(Emulator.handle, dataBuffer.Ptr, binaryData.Length.SafeUnsigned());
				if (!result) {
					var errorCode = DLL.EmuGetLastError(Emulator.handle);
					Error.Throw(errorCode);
				}
			}
		}
		finally {
			Emulator.MaybeReleaseLock();
		}
	}
	
	public void LoadBytecode(UInt32[] scriptBytecode) {
		Emulator.MaybeAcquireLock();
		
		try {
			unsafe {
				this.scriptBytecode = new(scriptBytecode.Length * sizeof(UInt32));
				// Copy buffer
				for (var i = 0; i < this.scriptBytecode.Length; i += 4) {
					var value = scriptBytecode[i];
				
					this.scriptBytecode[i]     = (byte) (value       & 0xFF);
					this.scriptBytecode[i + 1] = (byte) (value >>  8 & 0xFF);
					this.scriptBytecode[i + 2] = (byte) (value >> 16 & 0xFF);
					this.scriptBytecode[i + 3] = (byte) (value >> 24 & 0xFF);
				}
			
				DLL.Script700LoadBytecode(Emulator.handle, this.scriptBytecode.Ptr, scriptBytecode.Length.SafeUnsigned());
			}
		}
		finally {
			Emulator.MaybeReleaseLock();
		}
	}
	
	public void LoadData(byte[] data) {
		Emulator.MaybeAcquireLock();
		
		try {
			unsafe {
				dataArea = new(data.Length);
				// Copy buffer
				for (var i = 0; i < data.Length; i++) {
					dataArea[i] = data[i];
				}
				
				DLL.Script700LoadData(Emulator.handle, dataArea.Ptr, dataArea.Length.SafeUnsigned());
			}
		}
		finally {
			Emulator.MaybeReleaseLock();
		}
	}
	
	public void LoadLabelAddresses(UInt32[] labelAddresses) {
		Emulator.MaybeAcquireLock();
		
		try {
			unsafe {
				Buffer? labelBuffer = new(labelAddresses.Length * sizeof(UInt32));
				// Copy buffer
				for (var i = 0; i < labelAddresses.Length; i += 4) {
					var value = labelAddresses[i];
					
					labelBuffer[i]     = (byte) (value       & 0xFF);
					labelBuffer[i + 1] = (byte) (value >>  8 & 0xFF);
					labelBuffer[i + 2] = (byte) (value >> 16 & 0xFF);
					labelBuffer[i + 3] = (byte) (value >> 24 & 0xFF);
				}
				
				DLL.Script700LoadLabelAddresses(Emulator.handle, labelBuffer.Ptr, labelBuffer.Length.SafeUnsigned());
			}
		}
		finally {
			Emulator.MaybeReleaseLock();
		}
	}
	
	public void LoadLabelRemappings(UInt32[] labelRemappings) {
		Emulator.MaybeAcquireLock();
		
		try {
			unsafe {
				Buffer? labelBuffer = new(labelRemappings.Length * sizeof(UInt32));
				// Copy buffer
				for (var i = 0; i < labelRemappings.Length; i += 4) {
					var value = labelRemappings[i];
					
					labelBuffer[i]     = (byte) (value       & 0xFF);
					labelBuffer[i + 1] = (byte) (value >>  8 & 0xFF);
					labelBuffer[i + 2] = (byte) (value >> 16 & 0xFF);
					labelBuffer[i + 3] = (byte) (value >> 24 & 0xFF);
				}
				
				DLL.Script700LoadLabelRemappings(Emulator.handle, labelBuffer.Ptr, labelBuffer.Length.SafeUnsigned());
			}
		}
		finally {
			Emulator.MaybeReleaseLock();
		}
	}
	
	public class Operand {
		public string? Prefix { get; internal init; }
		public UInt32? Value  { get; internal init; }
		
		internal static Operand New(string? prefix, UInt32? value) {
			return new() {
				Prefix = prefix,
				Value  = value
			};
		}
		
		internal Operand() { }
		
		public Operand(string prefix) {
			Prefix = prefix;
			Value  = null;
		}
		
		public Operand(UInt32 value) {
			Prefix = null;
			Value  = value;
		}
		
		public Operand(string prefix, UInt32 value) {
			Prefix = prefix;
			Value  = value;
		}
	}
	
	public static uint[] CompileInstruction(string mnemonic) =>
		compileInstruction(mnemonic, null, null, null, null, null);
	
	public static uint[] CompileInstruction(string mnemonic, Operand operand) =>
		compileInstruction(mnemonic, operand.Prefix, operand.Value, null, null, null);
	
	public static uint[] CompileInstruction(string mnemonic, Operand operandA, Operand operandB) =>
		compileInstruction(mnemonic, operandA.Prefix, operandA.Value, null, operandB.Prefix, operandB.Value);
	
	public static uint[] CompileInstruction(string mnemonic, Operand operandA, char? infixOp, Operand operandB) =>
		compileInstruction(mnemonic, operandA.Prefix, operandA.Value, infixOp, operandB.Prefix, operandB.Value);
	
	public static uint[] CompileInstruction(string mnemonic, string opPrefix) =>
		compileInstruction(mnemonic, opPrefix, null, null, null, null);
	
	public static uint[] CompileInstruction(string mnemonic, UInt32 opValue) =>
		compileInstruction(mnemonic, null, opValue, null, null, null);
	
	public static uint[] CompileInstruction(string mnemonic, string opPrefix, UInt32 opValue) =>
		compileInstruction(mnemonic, opPrefix, opValue, null, null, null);
	
	public static uint[] CompileInstruction(string mnemonic, string? opPrefixA, UInt32? opValueA, string? opPrefixB, UInt32? opValueB) =>
		compileInstruction(mnemonic, opPrefixA, opValueA, null, opPrefixB, opValueB);
	
	public static uint[] CompileInstruction(string mnemonic, string? opPrefixA, UInt32? opValA, char? infixOp, string? opPrefixB, UInt32? opValB) =>
		compileInstruction(mnemonic, opPrefixA, opValA, infixOp, opPrefixB, opValB);
	
	static UInt32[] compileInstruction(string mnemonic, string? opPrefixA, UInt32? opValueA, char? infixOp, string? opPrefixB, UInt32? opValueB) {
		unsafe {
			DLL.InstrInfo instrInfo = new();
			
			for (var i = 0; i < 3; i++) {
				instrInfo.Mnemonic[i] = (byte) (i < mnemonic.Length ? mnemonic[i] : 0);
				
				if (opPrefixA is null) {
					instrInfo.Oper1Prefix[i] = 0;
				}
				else {
					instrInfo.Oper1Prefix[i] = (byte) (i < opPrefixA.Length ? opPrefixA[i] : i == opPrefixA.Length ? 0 : 0xFF);
				}
				
				if (opPrefixB is null) {
					instrInfo.Oper2Prefix[i] = 0;
				}
				else {
					instrInfo.Oper2Prefix[i] = (byte) (i < opPrefixB.Length ? opPrefixB[i] : i == opPrefixB.Length ? 0 : 0xFF);
				}
			}
			
			instrInfo.Oper1HasValue = (byte) (opValueA is not null ? 1 : 0);
			instrInfo.Oper2HasValue = (byte) (opValueB is not null ? 1 : 0);
			
			instrInfo.Oper1Value = opValueA is not null ? opValueA!.Value : 0;
			instrInfo.Oper2Value = opValueB is not null ? opValueB!.Value : 0;
			
			instrInfo.Operator = (byte) (infixOp is not null ? infixOp!.Value : 0);
			
			var compiled = DLL.Script700CompileInstruction(instrInfo);
			if (compiled.Length == 0) {
				var errorCode = DLL.GetLastError();
				Error.Throw(errorCode);
			}
			
			var result = new UInt32[compiled.Length];
			for (var i = 0; i < compiled.Length; i++) {
				result[i] = compiled.WordData[i];
			}
			
			return result;
		}
	}
	
	public static string Simplify(string str) { // TODO: Make these UString
		var lines = str.Split('\n').Select(x => x.Trim()).ToArray();
		StringBuilder sb = new();
		
		var mode = Mode.ScriptArea;
		
		NybbleStream nybbleStream = new();
		
		foreach (var line in lines) {
			if (line is "" or "::") {
				continue;
			}
			
			switch (mode) {
				case Mode.ScriptArea: {
					Instruction[] instrs = Instruction.Parse(line);
					
					foreach (var instr in instrs) {
						if (line.Trim() == "e") {
							mode = Mode.ExtendCMD;
							break;
						}
						
						sb.Append(instr.Compile()).Append('\n');
					}
					
					break;
				}
	
				case Mode.ExtendCMD: {
					if (line.Trim() == "e") {
						mode = Mode.DataArea;
					}
					break;
				}
	
				case Mode.DataArea: {
					nybbleStream.Parse(line);
					break;
				}
			}
		}
		
		return sb + "\n" + nybbleStream.Compile();
	}
	
	public static string? BinaryFile(string spcFilePath) {
		string dir = Env.ContainingDirectory(spcFilePath)!;
		
		if (spcFilePath.ToLower().EndsWith(".spc")) {
			var s7sb = spcFilePath[..^4] + ".7sb";
			if (File.Exists(s7sb)) {
				return s7sb;
			}
			
			var s657sb = Path.Join(dir, "65816.7sb");
			if (File.Exists(s657sb)) {
				return s657sb;
			}
		}
		
		return null;
	}
	
	public static string? ScriptFile(string spcFilePath) {
		string dir = Env.ContainingDirectory(spcFilePath)!;
		
		if (spcFilePath.ToLower().EndsWith(".spc")) {
			var s700 = spcFilePath[..^4] + ".700";
			if (File.Exists(s700)) {
				return s700;
			}
			
			var s7se = spcFilePath[..^4] + ".7se";
			if (File.Exists(s7se)) {
				return s7se;
			}
			
			var s65700 = Path.Join(dir, "65816.700");
			if (File.Exists(s65700)) {
				return s65700;
			}
			
			var s657se = Path.Join(dir, "65816.7se");
			if (File.Exists(s657se)) {
				return s657se;
			}
		}
		
		return null;
	}
}