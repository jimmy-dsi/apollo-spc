namespace Apollo;

using System.Diagnostics;

using System.Text;
using Jimbl;

public class SPC {
	public class Metadata {
		// ID666 Main
		public string Title    { get; set; }
		public string Artist   { get; set; }
		public string Game     { get; set; }
		public string Dumper   { get; set; }
		public string Comments { get; set; }
		
		public UInt32? Month { get; set; }
		public UInt32? Day   { get; set; }
		public UInt32? Year  { get; set; }
		
		public string DateOther { get; set; }
		
		public UInt32? LengthInSeconds { get; set; }
		public UInt32? FadeLengthInMS  { get; set; }
		
		public bool[] ChannelsDisabled { get; set; }
		
		public byte? EmulatorID { get; set; }
		
		// ID666 Extended
		public string  OSTTitle { get; set; }
		public byte?   OSTDisc  { get; set; }
		public byte[]? OSTTrack { get; set; }
		
		public string  Publisher     { get; set; }
		public UInt32? CopyrightYear { get; set; }
		
		public UInt32? IntroLengthInTimer2Steps { get; set; }
		public UInt32? LoopLengthInTimer2Steps  { get; set; }
		public UInt32? EndLengthInTimer2Steps   { get; set; }
		public byte?   LoopTimes                { get; set; }
		
		public byte? MixingLevel { get; set; }
		
		internal unsafe Metadata(DLL.SpcMetadata metadata) {
			Title    = getString(metadata.Title,    257);
			Artist   = getString(metadata.Artist,   257);
			Game     = getString(metadata.Game,     257);
			Dumper   = getString(metadata.Dumper,   257);
			Comments = getString(metadata.Comments, 257);
			
			Month = metadata.Month >= 0 ? (UInt32) metadata.Month : null;
			Day   = metadata.Month >= 0 ? (UInt32) metadata.Day   : null;
			Year  = metadata.Year  >= 0 ? (UInt32) metadata.Year  : null;
			
			DateOther = getString(metadata.DateOther, 12);
			
			LengthInSeconds = metadata.LengthInSeconds >= 0 ? (UInt32) metadata.LengthInSeconds : null;
			FadeLengthInMS  = metadata.FadeLengthInMS  >= 0 ? (UInt32) metadata.FadeLengthInMS  : null;
			
			ChannelsDisabled = new bool[8];
			for (var i = 0; i < 8; i++) {
				ChannelsDisabled[i] = metadata.ChannelsDisabled[i] != 0;
			}
			
			EmulatorID = metadata.EmulatorId >= 0 ? (byte) metadata.EmulatorId : null;
			
			OSTTitle = getString(metadata.OstTitle, 257);
			OSTDisc  = metadata.OstDisc >= 0 ? (byte) metadata.OstDisc : null;
			OSTTrack = metadata.HasOstTrack != 0 ? new Span<byte>(metadata.OstTrack, 2).ToArray() : null;
			
			Publisher = getString(metadata.Publisher, 257);
			CopyrightYear = metadata.CopyrightYear >= 0 ? (UInt32) metadata.CopyrightYear : null;
			
			IntroLengthInTimer2Steps = metadata.IntroLengthInTimer2Steps >= 0 ? (UInt32) metadata.IntroLengthInTimer2Steps : null;
			LoopLengthInTimer2Steps  = metadata.LoopLengthInTimer2Steps  >= 0 ? (UInt32) metadata.LoopLengthInTimer2Steps  : null;
			EndLengthInTimer2Steps   = metadata.EndLengthInTimer2Steps   >= 0 ? (UInt32) metadata.EndLengthInTimer2Steps   : null;
			LoopTimes                = metadata.LoopTimes                >= 0 ? (byte)   metadata.LoopTimes                : null;
			
			MixingLevel = metadata.MixingLevel >= 0 ? (byte) metadata.MixingLevel : null;
		}
	}
	
	public unsafe class CpuState {
		public byte A {
			get {
				emu.MaybeAcquireLock();
				try     { return *(byte*) state.A; }
				finally { emu.MaybeReleaseLock(); }
			}
			set {
				emu.MaybeAcquireLock();
				try     { *(byte*) state.A = value; }
				finally { emu.MaybeReleaseLock(); }
			}
		}
		
		public byte X {
			get {
				emu.MaybeAcquireLock();
				try     { return *(byte*) state.X; }
				finally { emu.MaybeReleaseLock(); }
			}
			set {
				emu.MaybeAcquireLock();
				try     { *(byte*) state.X = value; }
				finally { emu.MaybeReleaseLock(); }
			}
		}
		
		public byte Y {
			get {
				emu.MaybeAcquireLock();
				try     { return *(byte*) state.Y; }
				finally { emu.MaybeReleaseLock(); }
			}
			set {
				emu.MaybeAcquireLock();
				try     { *(byte*) state.Y = value; }
				finally { emu.MaybeReleaseLock(); }
			}
		}
		
		public UInt16 YA {
			get {
				emu.MaybeAcquireLock();
				try     { return (UInt16) (*(byte*) state.Y << 8 | *(byte*) state.A); }
				finally { emu.MaybeReleaseLock(); }
			}
			set {
				emu.MaybeAcquireLock();
				try {
					*(byte*) state.Y = (byte) (value >>   8);
					*(byte*) state.A = (byte) (value & 0xFF);
				}
				finally { emu.MaybeReleaseLock(); }
			}
		}
		
		public byte SP {
			get {
				emu.MaybeAcquireLock();
				try     { return *(byte*) state.SP; }
				finally { emu.MaybeReleaseLock(); }
			}
			set {
				emu.MaybeAcquireLock();
				try     { *(byte*) state.SP = value; }
				finally { emu.MaybeReleaseLock(); }
			}
		}
		
		public UInt16 PC {
			get {
				emu.MaybeAcquireLock();
				try     { return *(UInt16*) state.PC; }
				finally { emu.MaybeReleaseLock(); }
			}
			set {
				emu.MaybeAcquireLock();
				try     { *(UInt16*) state.PC = value; }
				finally { emu.MaybeReleaseLock(); }
			}
		}
		
		public byte PSW {
			get {
				emu.MaybeAcquireLock();
				try     { return *(byte*) state.PSW; }
				finally { emu.MaybeReleaseLock(); }
			}
			set {
				emu.MaybeAcquireLock();
				try     { *(byte*) state.PSW = value; }
				finally { emu.MaybeReleaseLock(); }
			}
		}
		
		public bool N {
			get {
				emu.MaybeAcquireLock();
				try     { return (*(byte*) state.PSW).GetBit(7); }
				finally { emu.MaybeReleaseLock(); }
			}
			set {
				emu.MaybeAcquireLock();
				try     { (*(byte*) state.PSW).SetBit(7, value); }
				finally { emu.MaybeReleaseLock(); }
			}
		}
		
		public bool V {
			get {
				emu.MaybeAcquireLock();
				try     { return (*(byte*) state.PSW).GetBit(6); }
				finally { emu.MaybeReleaseLock(); }
			}
			set {
				emu.MaybeAcquireLock();
				try     { (*(byte*) state.PSW).SetBit(6, value); }
				finally { emu.MaybeReleaseLock(); }
			}
		}
		
		public bool P {
			get {
				emu.MaybeAcquireLock();
				try     { return (*(byte*) state.PSW).GetBit(5); }
				finally { emu.MaybeReleaseLock(); }
			}
			set {
				emu.MaybeAcquireLock();
				try     { (*(byte*) state.PSW).SetBit(5, value); }
				finally { emu.MaybeReleaseLock(); }
			}
		}
		
		public bool B {
			get {
				emu.MaybeAcquireLock();
				try     { return (*(byte*) state.PSW).GetBit(4); }
				finally { emu.MaybeReleaseLock(); }
			}
			set {
				emu.MaybeAcquireLock();
				try     { (*(byte*) state.PSW).SetBit(4, value); }
				finally { emu.MaybeReleaseLock(); }
			}
		}
		
		public bool H {
			get {
				emu.MaybeAcquireLock();
				try     { return (*(byte*) state.PSW).GetBit(3); }
				finally { emu.MaybeReleaseLock(); }
			}
			set {
				emu.MaybeAcquireLock();
				try     { (*(byte*) state.PSW).SetBit(3, value); }
				finally { emu.MaybeReleaseLock(); }
			}
		}
		
		public bool I {
			get {
				emu.MaybeAcquireLock();
				try     { return (*(byte*) state.PSW).GetBit(2); }
				finally { emu.MaybeReleaseLock(); }
			}
			set {
				emu.MaybeAcquireLock();
				try     { (*(byte*) state.PSW).SetBit(2, value); }
				finally { emu.MaybeReleaseLock(); }
			}
		}
		
		public bool Z {
			get {
				emu.MaybeAcquireLock();
				try     { return (*(byte*) state.PSW).GetBit(1); }
				finally { emu.MaybeReleaseLock(); }
			}
			set {
				emu.MaybeAcquireLock();
				try     { (*(byte*) state.PSW).SetBit(1, value); }
				finally { emu.MaybeReleaseLock(); }
			}
		}
		
		public bool C {
			get {
				emu.MaybeAcquireLock();
				try     { return (*(byte*) state.PSW).GetBit(0); }
				finally { emu.MaybeReleaseLock(); }
			}
			set {
				emu.MaybeAcquireLock();
				try     { (*(byte*) state.PSW).SetBit(0, value); }
				finally { emu.MaybeReleaseLock(); }
			}
		}
		
		public UInt16 InstructionStartPC {
			get {
				emu.MaybeAcquireLock();
				try     { return *(UInt16*) state.InstructionStartPC; }
				finally { emu.MaybeReleaseLock(); }
			}
			set {
				emu.MaybeAcquireLock();
				try     { *(UInt16*) state.InstructionStartPC = value; }
				finally { emu.MaybeReleaseLock(); }
			}
		}
		
		Emulator        emu;
		DLL.SpcCpuState state;
		
		internal CpuState(Emulator emu, DLL.SpcCpuState state) {
			this.emu   = emu;
			this.state = state;
		}
	}
	
	public Emulator Emulator { get; init; }
	public CpuState State    { get; }
	
	internal SPC(Emulator emulator) {
		unsafe {
			Emulator = emulator;
			
			var state = DLL.SpcGetCpuState(Emulator.handle);
			if (state.A == IntPtr.Zero) {
				var errorCode = DLL.EmuGetLastError(Emulator.handle);
				Error.Throw(errorCode);
			}
			
			State = new(emulator, state);
		}
	}
	
	enum Format {
		A, X, Y, YA, PSW, SP, C,
		Dp, DpX, DpY, Abs, AbsX, AbsY, AbsXPtr, Relative, Imm,
		XPtr, YPtr, XPtrPlus, DpXPtr, DpPtrY,
		
		TCall, PCall, MemBit, NotMemBit, DpBit
	}
	
	class DecodeState {
		byte prefix;
		
		public UInt16 BasePC { get; init; }
		public int    Length { get; private set; }
		
		public DecodeState(byte prefix) {
			this.prefix = prefix;
			var format = instructionFormat(prefix);
			
			Length = determineLength(format);
		}
	
		public string DecodeInstruction(byte prefix, params byte[] suffix) {
			this.prefix = prefix;
			
			var name   =   instructionName(prefix);
			var format = instructionFormat(prefix);
			
			Length = determineLength(format);
			
			var operand1 = suffix.Length >= 1 ? suffix[0] : (byte) 0;
			var operand2 = suffix.Length >= 2 ? suffix[1] : (byte) 0;
			
			var operand16 = (UInt16) (operand1 | operand2 << 8);
			
			if (format.Length == 0) {
				return $"{name}";
			}
			else if (format.Length == 1 && is16bitOp(format[0])) {
				return $"{name} {formatOperand(format[0], operand16)}";
			}
			else if (format.Length == 2 && is16bitOp(format[0])) {
				return $"{name} {formatOperand(format[0], operand16)}, {formatOperand(format[1])}";
			}
			else if (format.Length == 2 && is16bitOp(format[1])) {
				return $"{name} {formatOperand(format[0])}, {formatOperand(format[1], operand16)}";
			}
			else if (format.Length == 1) {
				return $"{name} {formatOperand(format[0], operand1)}";
			}
			else if (format.Length == 2 && format[1] == Format.Relative) {
				return $"{name} {formatOperand(format[0], operand1)}, {formatOperand(format[1], operand2)}";
			}
			else if (format.Length == 2 && isRegister(format[1])) {
				return $"{name} {formatOperand(format[0], operand1)}, {formatOperand(format[1], operand2)}";
			}
			else {
				return $"{name} {formatOperand(format[0], operand2)}, {formatOperand(format[1], operand1)}";
			}
		}
		
		string instructionName(byte prefix) {
			var hiNyb = prefix >> 4;
			var loNyb = prefix & 0xF;
			
			if (loNyb == 0x0) {
				switch (hiNyb) {
					case 0x0: return "nop";
					case 0x1: return "bpl";
					case 0x2: return "clrp";
					case 0x3: return "bmi";
					case 0x4: return "setp";
					case 0x5: return "bvc";
					case 0x6: return "clrc";
					case 0x7: return "bvs";
					case 0x8: return "setc";
					case 0x9: return "bcc";
					case 0xA: return "ei";
					case 0xB: return "bcs";
					case 0xC: return "di";
					case 0xD: return "bne";
					case 0xE: return "clrv";
					case 0xF: return "beq";
				}
			}
			
			if (loNyb == 0x1) return "tcall";
			
			switch (prefix & 0x1F) {
				case 0x02: return "set1";
				case 0x12: return "clr1";
				case 0x03: return "bbs";
				case 0x13: return "bbc";
			}
			
			if (prefix == 0xC8) return "mov";
			
			if (loNyb is >= 0x4 and <= 0x9) {
				switch (hiNyb) {
					case 0x0 or 0x1: return "or";
					case 0x2 or 0x3: return "and";
					case 0x4 or 0x5: return "eor";
					case 0x6 or 0x7: return "cmp";
					case 0x8 or 0x9: return "adc";
					case 0xA or 0xB: return "sbc";
					default:         return "mov";
				}
			}
			
			if (loNyb == 0x0A) {
				switch (hiNyb) {
					case 0x0 or 0x2: return "or1";
					case 0x1:        return "decw";
					case 0x3:        return "incw";
					case 0x4 or 0x6: return "and1";
					case 0x5:        return "cmpw";
					case 0x7:        return "addw";
					case 0x8:        return "eor1";
					case 0x9:        return "subw";
					case 0xA or 0xC: return "mov1";
					case 0xB or 0xD: return "movw";
					case 0xE:        return "not1";
					case 0xF:        return "mov";
				}
			}
			
			if (prefix is 0xDB or 0xFB) return "mov";
			
			if (loNyb is 0x0B or 0x0C) {
				switch (hiNyb) {
					case 0x0 or 0x1:        return "asl";
					case 0x2 or 0x3:        return "rol";
					case 0x4 or 0x5:        return "lsr";
					case 0x6 or 0x7:        return "ror";
					case 0x8 or 0x9 or 0xD: return "dec";
					case 0xA or 0xB or 0xF: return "inc";
					case 0xC or 0xE:        return "mov";
				}
			}
			
			if (loNyb is 0x0D) {
				switch (hiNyb) {
					case 0x0 or 0x2 or 0x4 or 0x6: return "push";
					case 0x1:                      return "dec";
					case 0x3:                      return "inc";
					case 0xA:                      return "cmp";
					case 0xE:                      return "notc";
					default:                       return "mov";
				}
			}
			
			if (loNyb is 0x0E) {
				switch (hiNyb) {
					case 0x0:                      return "tset1";
					case 0x1 or 0x3 or 0x5 or 0x7: return "cmp";
					case 0x2 or 0xD:               return "cbne";
					case 0x4:                      return "tclr1";
					case 0x6 or 0xF:               return "dbnz";
					case 0x8 or 0xA or 0xC or 0xE: return "pop";
					case 0x9:                      return "div";
					case 0xB:                      return "das";
				}
			}
			
			// if (loNyb is 0x0F)
			switch (hiNyb) {
				case 0x0:               return "brk";
				case 0x1 or 0x5:        return "jmp";
				case 0x2:               return "bra";
				case 0x3:               return "call";
				case 0x4:               return "pcall";
				case 0x6:               return "ret";
				case 0x7:               return "reti";
				case 0x8 or 0xA or 0xB: return "mov";
				case 0x9:               return "xcn";
				case 0xC:               return "mul";
				case 0xD:               return "daa";
				case 0xE:               return "sleep";
				case 0xF:               return "stop";
				default: throw new UnreachableException();
			}
		}
		
		Format[] instructionFormat(byte prefix) {
			var hiNyb = prefix >> 4;
			var loNyb = prefix & 0xF;
			
			if (loNyb == 0x0) return (hiNyb & 1) == 0 ? [] : [Format.Relative];
			if (loNyb == 0x1) return [Format.TCall];
			if (loNyb == 0x2) return [Format.DpBit];
			if (loNyb == 0x3) return [Format.DpBit, Format.Relative];
			
			if (loNyb is >= 0x4 and <= 0x9) {
				if (hiNyb <= 0xB) {
					switch (prefix & 0x1F) {
						case 0x04: return [Format.A, Format.Dp];
						case 0x05: return [Format.A, Format.Abs];
						case 0x06: return [Format.A, Format.XPtr];
						case 0x07: return [Format.A, Format.DpXPtr];
						case 0x08: return [Format.A, Format.Imm];
						case 0x09: return [Format.Dp, Format.Dp];
						
						case 0x14: return [Format.A, Format.DpX];
						case 0x15: return [Format.A, Format.AbsX];
						case 0x16: return [Format.A, Format.AbsY];
						case 0x17: return [Format.A, Format.DpPtrY];
						case 0x18: return [Format.Dp, Format.Imm];
						case 0x19: return [Format.XPtr, Format.YPtr];
					}
				}
				else {
					switch (prefix & 0x3F) {
						case 0x04: return [Format.Dp, Format.A];
						case 0x05: return [Format.Abs, Format.A];
						case 0x06: return [Format.XPtr, Format.A];
						case 0x07: return [Format.DpXPtr, Format.A];
						case 0x08: return [Format.X, Format.Imm];
						case 0x09: return [Format.Abs, Format.X];
						
						case 0x14: return [Format.DpX, Format.A];
						case 0x15: return [Format.AbsX, Format.A];
						case 0x16: return [Format.AbsY, Format.A];
						case 0x17: return [Format.DpPtrY, Format.A];
						case 0x18: return [Format.Dp, Format.X];
						case 0x19: return [Format.DpY, Format.X];
						
						case 0x24: return [Format.A, Format.Dp];
						case 0x25: return [Format.A, Format.Abs];
						case 0x26: return [Format.A, Format.XPtr];
						case 0x27: return [Format.A, Format.DpXPtr];
						case 0x28: return [Format.A, Format.Imm];
						case 0x29: return [Format.X, Format.Abs];
						
						case 0x34: return [Format.A, Format.DpX];
						case 0x35: return [Format.A, Format.AbsX];
						case 0x36: return [Format.A, Format.AbsY];
						case 0x37: return [Format.A, Format.DpPtrY];
						case 0x38: return [Format.X, Format.Dp];
						case 0x39: return [Format.X, Format.DpY];
					}
				}
			}
			
			if (loNyb == 0xA) {
				switch (hiNyb) {
					case 0x0 or 0x4 or 0x8 or 0xA: return [Format.C, Format.MemBit];
					case 0x1 or 0x3:               return [Format.Dp];
					case 0x2 or 0x6:               return [Format.C, Format.NotMemBit];
					case 0x5 or 0x7 or 0x9 or 0xB: return [Format.YA, Format.Dp];
					case 0xC:                      return [Format.MemBit, Format.C];
					case 0xD:                      return [Format.Dp, Format.YA];
					case 0xE:                      return [Format.MemBit];
					case 0xF:                      return [Format.Dp, Format.Dp];
				}
			}
			
			if (loNyb is 0xB or 0xC) {
				if (hiNyb <= 0xB) {
					switch (prefix & 0x1F) {
						case 0x0B: return [Format.Dp];
						case 0x0C: return [Format.Abs];
						case 0x1B: return [Format.DpX];
						case 0x1C: return [Format.A];
					}
				}
				else {
					switch (prefix & 0x3F) {
						case 0x0B:         return [Format.Dp, Format.Y];
						case 0x0C:         return [Format.Abs, Format.Y];
						case 0x1B:         return [Format.DpX, Format.Y];
						case 0x1C or 0x3C: return [Format.Y];
						case 0x2B:         return [Format.Y, Format.Dp];
						case 0x2C:         return [Format.Y, Format.Abs];
						case 0x3B:         return [Format.Y, Format.DpX];
					}
				}
			}
			
			if (loNyb == 0xD) {
				switch (hiNyb) {
					case 0x0:               return [Format.PSW];
					case 0x1 or 0x3 or 0x4: return [Format.X];
					case 0x2:               return [Format.A];
					case 0x5:               return [Format.X, Format.A];
					case 0x6:               return [Format.Y];
					case 0x7:               return [Format.A, Format.X];
					case 0x8 or 0x0A:       return [Format.Y, Format.Imm];
					case 0x9:               return [Format.X, Format.SP];
					case 0xB:               return [Format.SP, Format.X];
					case 0xC:               return [Format.X, Format.Imm];
					case 0xD:               return [Format.A, Format.Y];
					case 0xE:               return [];
					case 0xF:               return [Format.Y, Format.A];
				}
			}
			
			if (loNyb == 0xE) {
				switch (hiNyb) {
					case 0x0 or 0x4: return [Format.Abs];
					case 0x1:        return [Format.X, Format.Abs];
					case 0x2 or 0x6: return [Format.Dp, Format.Relative];
					case 0x3:        return [Format.X, Format.Dp];
					case 0x5:        return [Format.Y, Format.Abs];
					case 0x7:        return [Format.Y, Format.Dp];
					case 0x8:        return [Format.PSW];
					case 0x9:        return [Format.YA, Format.X];
					case 0xA or 0xB: return [Format.A];
					case 0xC:        return [Format.X];
					case 0xD:        return [Format.DpX, Format.Relative];
					case 0xE:        return [Format.Y];
					case 0xF:        return [Format.Y, Format.Relative];
				}
			}
			
			// if (loNyb == 0xF)
			switch (hiNyb) {
				case 0x0 or 0x6 or 0x7 or 0xE or 0xF: return [];
				case 0x1:                             return [Format.AbsXPtr];
				case 0x2:                             return [Format.Relative];
				case 0x3 or 0x5:                      return [Format.Abs];
				case 0x4:                             return [Format.PCall];
				case 0x8:                             return [Format.Dp, Format.Imm];
				case 0x9 or 0xD:                      return [Format.A];
				case 0xA:                             return [Format.XPtrPlus, Format.A];
				case 0xB:                             return [Format.A, Format.XPtrPlus];
				case 0xC:                             return [Format.YA];
				default: throw new UnreachableException();
			}
		}
		
		string formatOperand(Format opFormat, byte opValue = 0) {
			switch (opFormat) {
				// Immediate
				case Format.Imm:      return $"#${opValue:X2}";
				// Regs
				case Format.A:        return "a";
				case Format.X:        return "x";
				case Format.Y:        return "y";
				case Format.YA:       return "ya";
				case Format.PSW:      return "psw";
				case Format.SP:       return "sp";
				case Format.C:        return "c";
				// Regs (Ptr)
				case Format.XPtr:     return "(x)";
				case Format.YPtr:     return "(y)";
				case Format.XPtrPlus: return "(x)+";
				// Mem     
				case Format.Dp:       return $"${opValue:X2}";
				case Format.DpX:      return $"${opValue:X2}+x";
				case Format.DpY:      return $"${opValue:X2}+y";
				case Format.DpXPtr:   return $"[${opValue:X2}+x]";
				case Format.DpPtrY:   return $"[${opValue:X2}]+y";
				case Format.Relative: return $"${relativeTo((sbyte) opValue):X4}";
				// Misc.
				case Format.TCall:    return $"{prefix >> 4}";
				case Format.PCall:    return $"${opValue:X2}";
				case Format.DpBit:    return $"${opValue:X2}.{prefix >> 5}";
				//
				default:              throw new UnreachableException();
			}
		}
		
		string formatOperand(Format opFormat, UInt16 opValue) {
			switch (opFormat) {
				case Format.Abs:       return $"${opValue:X4}";
				case Format.AbsX:      return $"${opValue:X4}+x";
				case Format.AbsY:      return $"${opValue:X4}+y";
				case Format.AbsXPtr:   return $"[${opValue:X4}+x]";
				case Format.MemBit:    return $"${opValue & 0x1FFF :X4}.{opValue >> 13}";
				case Format.NotMemBit: return $"/${opValue & 0x1FFF :X4}.{opValue >> 13}";
				default:               throw new UnreachableException();
			}
		}
		
		UInt16 relativeTo(sbyte offset) {
			return (UInt16) (BasePC + Length + offset);
		}
		
		static int determineLength(Format[] format) {
			if (format.Length == 0) {
				return 1;
			}
			else if (format.Length == 1 && isRegister(format[0])) {
				return 1;
			}
			else if (format.Length == 1 && is16bitOp(format[0])) {
				return 3;
			}
			else if (format.Length == 1) {
				return 2;
			}
			else if (format.Length == 2 && isRegister(format[0]) && isRegister(format[1])) {
				return 1;
			}
			else if (format.Length == 2 && (isRegister(format[0]) || isRegister(format[1]))) {
				if (is16bitOp(format[0]) || is16bitOp(format[1])) {
					return 3;
				}
				else {
					return 2;
				}
			}
			else {
				return 3;
			}
		}
		
		static bool isRegister(Format format) {
			return format is Format.A    or Format.X    or Format.Y or Format.YA or Format.PSW or Format.SP or Format.C
			              or Format.XPtr or Format.YPtr or Format.XPtrPlus;
		}
		
		static bool is16bitOp(Format format) {
			return format is Format.Abs or Format.AbsX or Format.AbsXPtr or Format.AbsY or Format.MemBit or Format.NotMemBit;
		}
	}
	
	public static int GetInstructionLength(byte prefix) {
		DecodeState state = new(prefix);
		return state.Length;
	}
	
	public static string DecodeInstruction(UInt16 basePC, byte prefix, params byte[] suffix) {
		DecodeState state = new(prefix) { BasePC = basePC };
		return state.DecodeInstruction(prefix, suffix);
	}
	
	static unsafe string getString(byte* ptr, int size) {
		var adjustedSize = size;
		
		for (var i = 0; i < size; i++) {
			if (*(ptr + i) == 0) {
				adjustedSize = i;
				break;
			}
		}
		
		ReadOnlySpan<byte> span = new(ptr, adjustedSize);
		
		try {
			return Encoding.UTF8.GetString(span);
		}
		catch (ArgumentException) { // Handle the case where string is not valid UTF-8 - For now, interpret each byte as code point
			StringBuilder sb = new();
			foreach (var c in span) {
				sb.Append((char) c);
			}
			return sb.ToString();
		}
	}
}