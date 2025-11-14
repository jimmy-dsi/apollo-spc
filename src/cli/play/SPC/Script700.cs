using System.Reflection.Emit;
using Jimbl;

namespace SPC;

using Jimbl.DataStructs;
using System.Text;

public static class Script700 {
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
	
	enum Mode {
		ScriptArea, ExtendCMD, DataArea
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