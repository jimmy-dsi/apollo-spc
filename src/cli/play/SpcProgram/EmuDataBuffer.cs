namespace SpcProgram;

public class EmuDataContainer {
	public EmuDataBuffer? Buffer { get; set; } = null;
	
	public EmuDataContainer(EmuDataBuffer? buffer) {
		Buffer = buffer;
	}
}

public abstract class EmuDataBuffer: ICloneable {
	public readonly long DSPCycle;
	
	protected EmuDataBuffer(long dspCycle) {
		DSPCycle = dspCycle;
	}
	
	public abstract EmuDataBuffer Clone();
	object ICloneable.Clone() => Clone();
}

public class EmuGenericBuffer: EmuDataBuffer {
	public EmuGenericBuffer(long dspCycle): base(dspCycle) { }
	
	public override EmuGenericBuffer Clone() {
		return new(DSPCycle);
	}
}