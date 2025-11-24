using Jimbl;

namespace Apollo;

using System.Collections;

public class SMP {
	public unsafe class Properties {
		public class TimerProps {
			public byte Stage0 {
				get {
					emu.MaybeAcquireLock();
					try {
						if      (index == 0) return *((byte*) state.Timer0Stage0);
						else if (index == 1) return *((byte*) state.Timer1Stage0);
						else                 return *((byte*) state.Timer2Stage0);
					}
					finally { emu.MaybeReleaseLock(); }
				}
				set {
					emu.MaybeAcquireLock();
					try {
						if      (index == 0) *((byte*) state.Timer0Stage0) = value;
						else if (index == 1) *((byte*) state.Timer1Stage0) = value;
						else                 *((byte*) state.Timer2Stage0) = value;
					}
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public byte Stage1 {
				get {
					emu.MaybeAcquireLock();
					try {
						if      (index == 0) return (byte) (*((byte*) state.Timer0Stage1) & 1);
						else if (index == 1) return (byte) (*((byte*) state.Timer1Stage1) & 1);
						else                 return (byte) (*((byte*) state.Timer2Stage1) & 1);
					}
					finally { emu.MaybeReleaseLock(); }
				}
				set {
					emu.MaybeAcquireLock();
					try {
						if      (index == 0) *((byte*) state.Timer0Stage1) = (byte) (value & 1);
						else if (index == 1) *((byte*) state.Timer1Stage1) = (byte) (value & 1);
						else                 *((byte*) state.Timer2Stage1) = (byte) (value & 1);
					}
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public byte Stage2 {
				get {
					emu.MaybeAcquireLock();
					try {
						if      (index == 0) return *((byte*) state.Timer0Stage2);
						else if (index == 1) return *((byte*) state.Timer1Stage2);
						else                 return *((byte*) state.Timer2Stage2);
					}
					finally { emu.MaybeReleaseLock(); }
				}
				set {
					emu.MaybeAcquireLock();
					try {
						if      (index == 0) *((byte*) state.Timer0Stage2) = value;
						else if (index == 1) *((byte*) state.Timer1Stage2) = value;
						else                 *((byte*) state.Timer2Stage2) = value;
					}
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public byte Stage3 {
				get {
					emu.MaybeAcquireLock();
					try     { return Output; }
					finally { emu.MaybeReleaseLock(); }
				}
				set {
					emu.MaybeAcquireLock();
					try     { Output = value; }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public bool Enabled {
				get {
					emu.MaybeAcquireLock();
					try     { return (*((byte*) (state.TimerOnFlags) + index % 3) & 1) != 0; }
					finally { emu.MaybeReleaseLock(); }
				}
				set {
					emu.MaybeAcquireLock();
					try     { *((byte*) (state.TimerOnFlags) + index % 3) = (byte) (value ? 1 : 0); }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public byte Divider {
				get {
					emu.MaybeAcquireLock();
					try     { return *((byte*) (state.TimerDividers) + index % 3); }
					finally { emu.MaybeReleaseLock(); }
				}
				set {
					emu.MaybeAcquireLock();
					try     { *((byte*) (state.TimerDividers) + index % 3) = value; }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			public byte Output {
				get {
					emu.MaybeAcquireLock();
					try     { return (byte) (*((byte*) (state.TimerOutputs) + index % 3) & 0xF); }
					finally { emu.MaybeReleaseLock(); }
				}
				set {
					emu.MaybeAcquireLock();
					try     { *((byte*) (state.TimerOutputs) + index % 3) = (byte) (value & 0xF); }
					finally { emu.MaybeReleaseLock(); }
				}
			}
			
			Emulator     emu;
			DLL.SmpState state;
			int          index;
		
			internal TimerProps(Emulator emu, DLL.SmpState state, int index) {
				this.emu   = emu;
				this.state = state;
				this.index = index;
			}
		}
		
		public class APUIO {
			public class Ports: IEnumerable<byte> {
				public byte this[int index] {
					get {
						emu.MaybeAcquireLock();
						try     { return *((byte*) basePtr + (index & 3)); }
						finally { emu.MaybeReleaseLock(); }
					}
					set {
						emu.MaybeAcquireLock();
						try     { *((byte*) basePtr + (index & 3)) = value; }
						finally { emu.MaybeReleaseLock(); }
					}
				}
				
				Emulator     emu;
				DLL.SmpState state;
				IntPtr       basePtr;
		
				internal Ports(Emulator emu, DLL.SmpState state, IntPtr basePtr) {
					this.emu     = emu;
					this.state   = state;
					this.basePtr = basePtr;
				}

				public IEnumerator<byte> GetEnumerator() {
					for (var i = 0; i < 4; i++) {
						yield return this[i];
					}
				}
				
				IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
			}
			
			public Ports Input  { get; }
			public Ports Output { get; }
			
			DLL.SmpState state;
		
			internal APUIO(Emulator emu, DLL.SmpState state) {
				this.state = state;
				
				Input  = new(emu, state, state .InputPorts);
				Output = new(emu, state, state.OutputPorts);
			}
		}
		
		public TimerProps[] Timer { get; }
		public APUIO        IO    { get; }
		public UInt8Buffer  Aux   { get; }
		
		public bool GlobalTimerDisable {
			get {
				emu.MaybeAcquireLock();
				try     { return (*((byte*) state.GlobalTimerDisable) & 1) != 0; }
				finally { emu.MaybeReleaseLock(); }
			}
			set {
				emu.MaybeAcquireLock();
				try     { *((byte*) state.GlobalTimerDisable) = (byte) (value ? 1 : 0); }
				finally { emu.MaybeReleaseLock(); }
			}
		}
		
		public bool RAMWriteEnable {
			get {
				emu.MaybeAcquireLock();
				try     { return (*((byte*) state.RamWriteEnable) & 1) != 0; }
				finally { emu.MaybeReleaseLock(); }
			}
			set {
				emu.MaybeAcquireLock();
				try     { *((byte*) state.RamWriteEnable) = (byte) (value ? 1 : 0); }
				finally { emu.MaybeReleaseLock(); }
			}
		}
		
		public bool RAMDisable {
			get {
				emu.MaybeAcquireLock();
				try     { return (*((byte*) state.RamDisable) & 1) != 0; }
				finally { emu.MaybeReleaseLock(); }
			}
			set {
				emu.MaybeAcquireLock();
				try     { *((byte*) state.RamDisable) = (byte) (value ? 1 : 0); }
				finally { emu.MaybeReleaseLock(); }
			}
		}
		
		public bool GlobalTimerEnable {
			get {
				emu.MaybeAcquireLock();
				try     { return (*((byte*) state.GlobalTimerEnable) & 1) != 0; }
				finally { emu.MaybeReleaseLock(); }
			}
			set {
				emu.MaybeAcquireLock();
				try     { *((byte*) state.GlobalTimerEnable) = (byte) (value ? 1 : 0); }
				finally { emu.MaybeReleaseLock(); }
			}
		}
			
		public byte RAMWaitstates {
			get {
				emu.MaybeAcquireLock();
				try     { return (byte) (*((byte*) state.RamWaitstates) & 3); }
				finally { emu.MaybeReleaseLock(); }
			}
			set {
				emu.MaybeAcquireLock();
				try     { *((byte*) state.RamWaitstates) = (byte) (value & 3); }
				finally { emu.MaybeReleaseLock(); }
			}
		}
			
		public byte IOWaitstates {
			get {
				emu.MaybeAcquireLock();
				try     { return (byte) (*((byte*) state.IoWaitstates) & 3); }
				finally { emu.MaybeReleaseLock(); }
			}
			set {
				emu.MaybeAcquireLock();
				try     { *((byte*) state.IoWaitstates) = (byte) (value & 3); }
				finally { emu.MaybeReleaseLock(); }
			}
		}
		
		public bool UseBootROM {
			get {
				emu.MaybeAcquireLock();
				try     { return (*((byte*) state.UseBootRom) & 1) != 0; }
				finally { emu.MaybeReleaseLock(); }
			}
			set {
				emu.MaybeAcquireLock();
				try     { *((byte*) state.UseBootRom) = (byte) (value ? 1 : 0); }
				finally { emu.MaybeReleaseLock(); }
			}
		}
			
		public byte DSPAddress {
			get {
				emu.MaybeAcquireLock();
				try     { return *((byte*) state.DspAddress); }
				finally { emu.MaybeReleaseLock(); }
			}
			set {
				emu.MaybeAcquireLock();
				try     { *((byte*) state.DspAddress) = value; }
				finally { emu.MaybeReleaseLock(); }
			}
		}
			
		public byte DSPData {
			get => emu.DSP.Register[DSPAddress & 0x7F];
			set => emu.DSP.Register[DSPAddress & 0x7F] = value;
		}
		
		Emulator     emu;
		DLL.SmpState state;
		
		internal Properties(DLL.SmpState state, Emulator emu) {
			this.emu   = emu;
			this.state = state;
			
			Timer = new TimerProps[] { new(emu, state, 0), new(emu, state, 1), new(emu, state, 2) };
			IO    = new(emu, state);
			
			if (emu.MakeShared) {
				Aux = new UInt8BufferShared(emu, (byte*) state.Aux, 2);
			}
			else {
				Aux = new((byte*) state.Aux, 2);
			}
		}
	}
	
	public unsafe class Buffer {
		DLL.SmpMemoryPage page;
		
		public byte this[int index] => page.Array[index & 0xFF];
		
		internal Buffer(DLL.SmpMemoryPage page) {
			this.page = page;
		}
	}
	
	public class MemAccessLog {
		public enum LogType {
			None      = 0,
			Read      = 1,
			Write     = 2,
			Exec      = 3,
			Fetch     = 4,
			DummyRead = 5
		}
		
		public LogType Type      { get; }
		public UInt64  DSPCycle  { get; }
		public UInt16  Address   { get; }
		
		public byte?   PreData   { get; }
		public byte?   WriteData { get; }
		public byte?   PostData  { get; }
		
		internal MemAccessLog(DLL.SmpLog log) {
			Type      = (LogType) log.Type;
			DSPCycle  = log.DspCycle;
			Address   = log.Address;
			
			PreData   = Type == LogType.Write ? log.PreData   : null;
			WriteData = Type == LogType.Write ? log.WriteData : null;
			PostData  = Type == LogType.Write ? log.PostData  : null;
		}
	}
	
	bool loggingEnabled = false;
	
	public Emulator    Emulator { get; init; }
	public UInt8Buffer BootROM  { get; init; }
	
	public Properties State { get; }
	
	public bool LoggingEnabled {
		get {
			Emulator.MaybeAcquireLock();
			try     { return loggingEnabled; }
			finally { Emulator.MaybeReleaseLock(); }
		}
		set {
			Emulator.MaybeAcquireLock();
			
			try {
				bool result;
				
				if (value) {
					result = DLL.SmpEnableLogging(Emulator.handle);
				}
				else {
					result = DLL.SmpDisableLogging(Emulator.handle);
				}
				
				if (!result) {
					var errorCode = DLL.EmuGetLastError(Emulator.handle);
					Error.Throw(errorCode);
				}
				
				loggingEnabled = value;
			}
			finally { Emulator.MaybeReleaseLock(); }
		}
	}
	
	public byte ReadByte(UInt16 address) {
		Emulator.MaybeAcquireLock();
		try {
			var result = DLL.SmpReadByte(address, Emulator.handle);
			Emulator.CheckForError();
			return result;
		}
		finally { Emulator.MaybeReleaseLock(); }
	}
	
	public UInt16 ReadWord(UInt16 address) {
		Emulator.MaybeAcquireLock();
		try {
			var result = DLL.SmpReadWord(address, Emulator.handle);
			Emulator.CheckForError();
			return result;
		}
		finally { Emulator.MaybeReleaseLock(); }
	}
	
	public Buffer ReadPage(UInt16 address) {
		Emulator.MaybeAcquireLock();
		try {
			var page = DLL.SmpReadPage(address, Emulator.handle);
			if (page.IsError) {
				var errorCode = DLL.EmuGetLastError(Emulator.handle);
				Error.Throw(errorCode);
			}
			return new(page);
		}
		finally { Emulator.MaybeReleaseLock(); }
	}
	
	public byte DebugReadByte(UInt16 address) {
		Emulator.MaybeAcquireLock();
		try {
			var result = DLL.SmpDebugReadByte(address, Emulator.handle);
			Emulator.CheckForError();
			return result;
		}
		finally { Emulator.MaybeReleaseLock(); }
	}
	
	public UInt16 DebugReadWord(UInt16 address) {
		Emulator.MaybeAcquireLock();
		try {
			var result = DLL.SmpDebugReadWord(address, Emulator.handle);
			Emulator.CheckForError();
			return result;
		}
		finally { Emulator.MaybeReleaseLock(); }
	}
	
	public Buffer DebugReadPage(UInt16 address) {
		Emulator.MaybeAcquireLock();
		try {
			var page = DLL.SmpDebugReadPage(address, Emulator.handle);
			if (page.IsError) {
				var errorCode = DLL.EmuGetLastError(Emulator.handle);
				Error.Throw(errorCode);
			}
			return new(page);
		}
		finally { Emulator.MaybeReleaseLock(); }
	}
	
	public void WriteByte(UInt16 address, byte value) {
		Emulator.MaybeAcquireLock();
		try {
			var result = DLL.SmpWriteByte(address, value, Emulator.handle);
			if (!result) {
				var errorCode = DLL.EmuGetLastError(Emulator.handle);
				Error.Throw(errorCode);
			}
		}
		finally { Emulator.MaybeReleaseLock(); }
	}
	
	public void WriteWord(UInt16 address, UInt16 value) {
		Emulator.MaybeAcquireLock();
		try {
			var result = DLL.SmpWriteWord(address, value, Emulator.handle);
			if (!result) {
				var errorCode = DLL.EmuGetLastError(Emulator.handle);
				Error.Throw(errorCode);
			}
		}
		finally { Emulator.MaybeReleaseLock(); }
	}
	
	public MemAccessLog[] GetAccessLogs(Int64 startCycle) {
		Emulator.MaybeAcquireLock();
		try {
			unsafe {
				var logSlice = DLL.SmpGetAccessLogs(startCycle.SafeUnsigned(), Emulator.handle);
				if (logSlice.LogArray == IntPtr.Zero) {
					var errorCode = DLL.EmuGetLastError(Emulator.handle);
					Error.Throw(errorCode);
				}
				
				try {
					var arrayPtr = (DLL.SmpLog*) logSlice.LogArray;
					var length   = (int) logSlice.Length;
				
					var logs = new MemAccessLog[length];
					Span<DLL.SmpLog> span = new(arrayPtr, length);
				
					for (var i = 0; i < length; i++) {
						var log = span[i];
						logs[i] = new(log);
					}
					
					return logs;
				}
				finally {
					var result = DLL.SmpFreeLogs(logSlice.LogArray);
					if (!result) {
						var errorCode = DLL.GetLastError();
						Error.Throw(errorCode);
					}
				}
			}
		}
		finally { Emulator.MaybeReleaseLock(); }
	}
	
	public MemAccessLog[] GetAccessLogsDeduped(Int64 startCycle) {
		var allLogs = GetAccessLogs(startCycle);
		List<MemAccessLog> deduped = new();
		
		List<MemAccessLog> curCycleLogs = new();
		var lastCycle = startCycle.SafeUnsigned();
		
		void dedupe() {
			// Check if any read logs are present on the last cycle
			var hasReads = false;
			
			foreach (var curCycleLog in curCycleLogs) {
				if (curCycleLog.Type is MemAccessLog.LogType.Fetch or MemAccessLog.LogType.DummyRead) {
					hasReads = true;
					break;
				}
			}
			
			// No need to keep 'Read' logs if another, more specific type of read was logged on the same cycle
			if (hasReads) {
				deduped.AddRange(curCycleLogs.Where(x => x.Type != MemAccessLog.LogType.Read));
			}
			else {
				deduped.AddRange(curCycleLogs);
			}
		}
		
		foreach (var log in allLogs) {
			if (log.DSPCycle != lastCycle) {
				dedupe();
			
				lastCycle = log.DSPCycle;
				curCycleLogs.Clear();
			}
			curCycleLogs.Add(log);
		}
		dedupe();
		
		return deduped.ToArray();
	}
	
	internal SMP(Emulator emulator) {
		unsafe {
			Emulator = emulator;
			
			var bootRomPtr = DLL.SmpGetBootRomPtr(Emulator.handle);
			if (bootRomPtr == IntPtr.Zero) throw new StateError();
			
			BootROM = new((byte*) bootRomPtr, 0x40, isReadonly: true);
			
			var state = DLL.SmpGetState(Emulator.handle);
			if (state.GlobalTimerDisable == IntPtr.Zero) throw new StateError();
			
			State = new(state, emulator);
		}
	}
}