namespace Apollo;

using System.Collections;

public class SMP {
	public unsafe class Properties {
		public class TimerProps {
			public byte Stage0 {
				get {
					if      (index == 0) return *((byte*) state.Timer0Stage0);
					else if (index == 1) return *((byte*) state.Timer1Stage0);
					else                 return *((byte*) state.Timer2Stage0);
				}
				set {
					if      (index == 0) *((byte*) state.Timer0Stage0) = value;
					else if (index == 1) *((byte*) state.Timer1Stage0) = value;
					else                 *((byte*) state.Timer2Stage0) = value;
				}
			}
			
			public byte Stage1 {
				get {
					if      (index == 0) return (byte) (*((byte*) state.Timer0Stage1) & 1);
					else if (index == 1) return (byte) (*((byte*) state.Timer1Stage1) & 1);
					else                 return (byte) (*((byte*) state.Timer2Stage1) & 1);
				}
				set {
					if      (index == 0) *((byte*) state.Timer0Stage1) = (byte) (value & 1);
					else if (index == 1) *((byte*) state.Timer1Stage1) = (byte) (value & 1);
					else                 *((byte*) state.Timer2Stage1) = (byte) (value & 1);
				}
			}
			
			public byte Stage2 {
				get {
					if      (index == 0) return *((byte*) state.Timer0Stage2);
					else if (index == 1) return *((byte*) state.Timer1Stage2);
					else                 return *((byte*) state.Timer2Stage2);
				}
				set {
					if      (index == 0) *((byte*) state.Timer0Stage2) = value;
					else if (index == 1) *((byte*) state.Timer1Stage2) = value;
					else                 *((byte*) state.Timer2Stage2) = value;
				}
			}
			
			public byte Stage3 {
				get => Output;
				set => Output = value;
			}
			
			public bool Enabled {
				get => (*((byte*) (state.TimerOnFlags) + index % 3) & 1) != 0;
				set =>  *((byte*) (state.TimerOnFlags) + index % 3) = (byte) (value ? 1 : 0);
			}
			
			public byte Divider {
				get => *((byte*) (state.TimerDividers) + index % 3);
				set => *((byte*) (state.TimerDividers) + index % 3) = value;
			}
			
			public byte Output {
				get => (byte) (*((byte*) (state.TimerOutputs) + index % 3) & 0xF);
				set => *((byte*) (state.TimerOutputs) + index % 3) = (byte) (value & 0xF);
			}
			
			DLL.SmpState state;
			int          index;
		
			internal TimerProps(DLL.SmpState state, int index) {
				this.state = state;
				this.index = index;
			}
		}
		
		public class APUIO {
			public class Ports: IEnumerable<byte> {
				public byte this[int index] {
					get => *((byte*) basePtr + (index & 3));
					set => *((byte*) basePtr + (index & 3)) = value;
				}
				
				DLL.SmpState state;
				IntPtr       basePtr;
		
				internal Ports(DLL.SmpState state, IntPtr basePtr) {
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
		
			internal APUIO(DLL.SmpState state) {
				this.state = state;
				
				Input  = new(state, state .InputPorts);
				Output = new(state, state.OutputPorts);
			}
		}
		
		public TimerProps[] Timer { get; }
		public APUIO        IO    { get; }
		public UInt8Buffer  Aux   { get; }
		
		public bool GlobalTimerDisable {
			get => (*((byte*) state.GlobalTimerDisable) & 1) != 0;
			set =>  *((byte*) state.GlobalTimerDisable) = (byte) (value ? 1 : 0);
		}
		
		public bool RAMWriteEnable {
			get => (*((byte*) state.RamWriteEnable) & 1) != 0;
			set =>  *((byte*) state.RamWriteEnable) = (byte) (value ? 1 : 0);
		}
		
		public bool RAMDisable {
			get => (*((byte*) state.RamDisable) & 1) != 0;
			set =>  *((byte*) state.RamDisable) = (byte) (value ? 1 : 0);
		}
		
		public bool GlobalTimerEnable {
			get => (*((byte*) state.GlobalTimerEnable) & 1) != 0;
			set =>  *((byte*) state.GlobalTimerEnable) = (byte) (value ? 1 : 0);
		}
			
		public byte RAMWaitstates {
			get => (byte) (*((byte*) state.RamWaitstates) & 3);
			set => *((byte*) state.RamWaitstates) = (byte) (value & 3);
		}
			
		public byte IOWaitstates {
			get => (byte) (*((byte*) state.IoWaitstates) & 3);
			set => *((byte*) state.IoWaitstates) = (byte) (value & 3);
		}
		
		public bool UseBootROM {
			get => (*((byte*) state.UseBootRom) & 1) != 0;
			set =>  *((byte*) state.UseBootRom) = (byte) (value ? 1 : 0);
		}
			
		public byte DSPAddress {
			get => *((byte*) state.DspAddress);
			set => *((byte*) state.DspAddress) = value;
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
			
			Timer = new TimerProps[] { new(state, 0), new(state, 1), new(state, 2) };
			IO    = new(state);
			Aux   = new((byte*) state.Aux, 2);
		}
	}
	
	public unsafe class Buffer {
		DLL.SmpMemoryPage page;
		
		public byte this[int index] => page.Array[index & 0xFF];
		
		internal Buffer(DLL.SmpMemoryPage page) {
			this.page = page;
		}
	}
	
	public Emulator    Emulator { get; init; }
	public UInt8Buffer BootROM  { get; init; }
	
	public Properties State { get; }
	
	public byte ReadByte(UInt16 address) {
		return DLL.SmpReadByte(address, Emulator.handle);
	}
	
	public UInt16 ReadWord(UInt16 address) {
		return DLL.SmpReadWord(address, Emulator.handle);
	}
	
	public Buffer ReadPage(UInt16 address) {
		var page = DLL.SmpReadPage(address, Emulator.handle);
		if (page.IsError) {
			throw new StateError(); // TODO: or NullError
		}
		return new(page);
	}
	
	public byte DebugReadByte(UInt16 address) {
		return DLL.SmpDebugReadByte(address, Emulator.handle);
	}
	
	public UInt16 DebugReadWord(UInt16 address) {
		return DLL.SmpDebugReadWord(address, Emulator.handle);
	}
	
	public Buffer DebugReadPage(UInt16 address) {
		var page = DLL.SmpDebugReadPage(address, Emulator.handle);
		if (page.IsError) {
			throw new StateError(); // TODO: or NullError
		}
		return new(page);
	}
	
	public void WriteByte(UInt16 address, byte value) {
		var result = DLL.SmpWriteByte(address, value, Emulator.handle);
		if (!result) {
			throw new StateError(); // TODO: or NullError
		}
	}
	
	public void WriteWord(UInt16 address, UInt16 value) {
		var result = DLL.SmpWriteWord(address, value, Emulator.handle);
		if (!result) {
			throw new StateError(); // TODO: or NullError
		}
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