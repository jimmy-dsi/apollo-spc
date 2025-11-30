namespace SpcProgram;

using Apollo;

public class EmuDataContainer {
	public EmuDataBuffer? Buffer { get; set; } = null;
	
	public EmuDataContainer(EmuDataBuffer? buffer) {
		Buffer = buffer;
	}
}

public class EmuDataBuffer: ICloneable {
	public long DSPCycle { get; private set; }
	
	public byte[]? ARAM_Data   { get; private set; }
	public byte[]? SMP_BusData { get; private set; }
	
	public EmuDataBuffer(long dspCycle) {
		DSPCycle = dspCycle;
	}
	
	public void RequestPopulate(Emulator emu, Transfer.Requests requests, UInt16 startAddr = 0, UInt16 length = 0x100) {
		resetToNull();
		
		if ((requests & Transfer.Requests.ARAM) != 0) {
			ARAM_Data = new byte[length];
			
			for (var a = startAddr; a < startAddr + length; a++) {
				if (startAddr + length > 0xFFFF) {
					break;
				}
				
				ARAM_Data[a - startAddr] = emu.DSP.ARAM[a];
			}
		}
		
		if ((requests & Transfer.Requests.SMP_Bus) != 0) {
			// TODO: Optimize when only a tiny amount of data is requested
			SMP_BusData = new byte[length];
			
			var startPage = startAddr & 0xFF00;
			var lastPage  = Math.Clamp(startAddr + length - 1, 0, 0xFFFF) & 0xFF00;
			
			var srcData = new byte[lastPage + 0x100 - startPage];
			
			for (var p = startPage >> 8; p <= lastPage >> 8; p++) {
				var baseAddr = p << 8;
				var buf = emu.SMP.DebugReadPage((UInt16) baseAddr);
				for (var i = 0; i < 0x100; i++) {
					srcData[baseAddr - startPage + i] = buf[i];
				}
			}
			
			var startOffset = startAddr & 0xFF;
			for (var i = 0; i < length; i++) {
				SMP_BusData[i] = srcData[startOffset + i];
			}
		}
	}
	
	public bool ExpectData(Transfer.Requests requests) {
		if ((requests & Transfer.Requests.ARAM)    != 0) return ARAM_Data   is not null;
		if ((requests & Transfer.Requests.SMP_Bus) != 0) return SMP_BusData is not null;
		return true;
	}
	
	public EmuDataBuffer Clone() {
		EmuDataBuffer clone = new(DSPCycle);
		
		if (ARAM_Data is not null) {
			clone.ARAM_Data = ARAM_Data.ToArray();
		}
		
		if (SMP_BusData is not null) {
			clone.SMP_BusData = SMP_BusData.ToArray();
		}
		
		return clone;
	}
	
	object ICloneable.Clone() => Clone();
	
	void resetToNull() {
		ARAM_Data   = null;
		SMP_BusData = null;
	}
}