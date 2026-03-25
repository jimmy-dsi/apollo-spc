namespace Apollo;

using System.Globalization;
using Jimbl;

public partial class Script700 {
	public static byte[] Compile(string scriptText) {
		Parser parser = new();
		var test = parser.Compile(scriptText);
		return test;
	}
	
	public class Parser {
		int tokenParseEnd = 0;
		
		enum ParseMode {
			Script, Data
		}
	
		public byte[] Compile(string scriptText) {
			List<byte> binData = [];
		
			var lines = Simplify(scriptText).Split('\n').Select(x => x.TrimEnd());
		
			var mode = ParseMode.Script;
		
			// Pre-initialize with label address table
			for (var i = 0; i < 1024; i++) {
				for (var j = 0; j < 4; j++) binData.Add(0xFF);
			}
		
			var    scriptSizeOffset = binData.Count;
			UInt32 dataSizeOffset   = 0;
		
			// Allocate 4 bytes for where the script size will be stored
			for (var i = 0; i < 4; i++) {
				binData.Add(0x00);
			}
		
			byte[][] bytes = [ new byte[4], new byte[4], new byte[4], new byte[4] ];
		
			var instrSize = 1;
			var firstIter = true;
		
			uint pc = 0;
			uint dc = 0;
		
			var ignore = false;
		
			foreach (var str in lines) {
				tokenParseEnd = 0;
				
				if (str == ";@line") {
					ignore = false;
					continue;
				}
				
				if (str.Length == 0) {
					if (mode == ParseMode.Script) {
						if (!ignore) {
							foreach (var (k, bb) in bytes.Enum()) {
								if (instrSize <= k) {
									break;
								}
								for (var i = 0; i < 4; i++) {
									binData.Add(bb[i]);
								}
							}
						}
					
						var sizeBytes = BitConverter.GetBytes(pc);
						for (var x = 0; x < 4; x++) {
							binData[scriptSizeOffset + x] = sizeBytes[x];
						}
						
						dataSizeOffset = (uint) binData.Count;
						
						// Allocate 4 bytes for where the data size will be stored
						for (var i = 0; i < 4; i++) {
							binData.Add(0x00);
						}
						
						dc = 0;
						
						firstIter = true;
						mode = ParseMode.Data;
						
						if (ignore) {
							ignore = false;
						}
					}
					else if (mode == ParseMode.Data) {
						break;
					}
				}
                
				if (ignore) {
					continue;
				}
				
				var mnemonic = peekToken(str);
				
				if (mnemonic is not null && mnemonic.Length >= 2 && mnemonic[0] == ':') {
					nextToken(str);
					var labelNum = int.TryParse(mnemonic[1..], out var result) ? result : -1;
					
					if (labelNum < 0) {
						ignore = true;
						continue;
					}
					
					labelNum %= 1024;
					var labelStart = labelNum * 4;
					
					for (var i = 0; i < 4; i++) {
						if (mode == ParseMode.Script) {
							binData[labelStart + i] = (byte) (pc >> i * 8);
						}
						else {
							binData[labelStart + i] = (byte) ((0x8000_0000 + dc) >> i * 8);
						}
					}
					
					continue;
				}
				else if (mode == ParseMode.Script) {
					nextToken(str);
					
					if (!firstIter && (mnemonic is null || mnemonic.Length < 2 || mnemonic.Length >= 2 && mnemonic[0] != ':')) {
						foreach (var (k, bb) in bytes.Enum()) {
							if (instrSize <= k) {
								break;
							}
							for (var i = 0; i < 4; i++) {
								binData.Add(bb[i]);
							}
						}
					}
					
					instrSize = 1;
					
					if (mnemonic is null) {
						ignore = true;
						continue;
					}
					
					firstIter = false;
					
					UInt32[] wslc;
					
					var prefixValue = nextToken(str);
					
					if (prefixValue is null) {
						try {
							wslc = CompileInstruction(mnemonic);
						}
						catch (Script700CompileError) {
							ignore = true;
							continue;
						}
						
						foreach (var (i, word) in wslc.Enum()) {
							bytes[i] = BitConverter.GetBytes(word);
						}
						
						instrSize = wslc.Length;
						
						pc += (uint) wslc.Length;
						
						continue;
					}
					
					var operand = splitToken(prefixValue);
					
					var next = nextToken(str);
					if (next is null) {
						try {
							wslc = CompileInstruction(mnemonic, operand);
						}
						catch (Script700CompileError) {
							ignore = true;
							continue;
						}
						
						foreach (var (i, word) in wslc.Enum()) {
							bytes[i] = BitConverter.GetBytes(word);
						}
						
						instrSize = wslc.Length;
						
						pc += (uint) wslc.Length;
						
						continue;
					}
					
					char? infixOp = null;
					
					if (next.Length == 1) {
						infixOp = next[0] switch {
							'+' or '-' or '*' or '/' or '\\' or '%' or '$' or '&' or '|' or '^' or '<' or '_' or '>' or '!' => next[0],
							_ => null
						};
					}
					
					string? prefixValue2 = null;
					
					if (infixOp is null) {
						prefixValue2 = next;
					}
					else {
						prefixValue2 = nextToken(str);
					}
					
					var operand2 = splitToken(prefixValue2!);
					
					try {
						wslc = CompileInstruction(mnemonic, operand, infixOp, operand2);
					}
					catch (Script700CompileError) {
						ignore = true;
						continue;
					}
						
					foreach (var (i, word) in wslc.Enum()) {
						bytes[i] = BitConverter.GetBytes(word);
					}
						
					instrSize = wslc.Length;
						
					pc += (uint) wslc.Length;
				}
				else {
					var dataByteStr = nextToken(str);
					
					while (dataByteStr is not null) {
						// TODO: Figure out what to do with unparsable data sections
						var dataByte = byte.Parse(dataByteStr, NumberStyles.HexNumber);
						binData.Add(dataByte);
						dc++;
						
						dataByteStr = nextToken(str);
					}
				}
			}
			
			var dataBytes = BitConverter.GetBytes(dc);
			for (var x = 0; x < 4; x++) {
				binData[(dataSizeOffset + (UInt32) x).SafeSigned()] = dataBytes[x];
			}
		
			return binData.ToArray();
		}
		
		string? nextToken(string buffer) {
			var buf    = buffer[tokenParseEnd ..];
			var result = peekRange(buf);
			
			if (result is null) {
				return null;
			}
			
			var (start, end) = result!.Value;
			tokenParseEnd += end;
			
			return buf[start .. end];
		}
		
		string? peekToken(string buffer) {
			var buf = buffer[tokenParseEnd ..];
			var result = peekRange(buf);
			
			if (result is null) {
				return null;
			}
			
			var (start, end) = result!.Value;
			
			return buf[start .. end];
		}
		
		(int, int)? peekRange(string buf) {
			var startFound = false;
			
			var start = 0;
			var end   = 0;
			
			foreach (var (i, b) in buf.Enum()) {
				if (b is not ' ' and not '\t' and not '\r' and not '\n') {
					start      = i;
					startFound = true;
					break;
				}
			}
			
			if (!startFound) {
				return null;
			}
			
			for (var i = start; i < buf.Length; i++) {
				if (buf[i] is ' ' or '\t' or '\r' or '\n') {
					end = i;
					break;
				}
			}
			
			if (end == 0) {
				end = buf.Length;
			}
			
			return (start, end);
		}
		
		Operand splitToken(string str) {
			string? prefix = null;
			UInt32? value  = null;
			
			var splitIndex = 0;
			var numFound = false;
			
			foreach (var (i, c) in str.Enum()) {
				if (c is '$' or >= '0' and <= '9') {
					splitIndex = i;
					numFound = true;
					break;
				}
			}
			
			if (!numFound) {
				splitIndex = str.Length;
			}
			
			prefix = str[..splitIndex];
			var numIndex = splitIndex;
			
			var isHex = false;
			
			if (numFound) {
				if (str[splitIndex] == '$') {
					numIndex += 1;
					isHex = true;
				}
				else if (str.Length >= splitIndex + 2 && str[splitIndex .. (splitIndex + 2)] == "0x") {
					numIndex += 2;
					isHex = true;
				}
				
				if (isHex) {
					value = uint.TryParse(str[numIndex..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var res) ? res : null;
				}
				else {
					value = uint.TryParse(str[numIndex..], out var res) ? res : null;
				}
			}
			
			return Operand.New(
				prefix,
				str[numIndex..].Length == 0 ? null : value
			);
		}
	}
}