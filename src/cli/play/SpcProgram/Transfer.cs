namespace SpcProgram;

using Apollo;

public static class Transfer {
	[Flags]
	public enum Requests {
		CycleCountOnly = 0,
		
		ARAM      = 1 << 0,
		SMP_Bus   = 1 << 1,
		SMP_State = 1 << 2,
		
		SPC_Regs = 1 << 3,
		MemLogs  = 1 << 4,
		
		DSP_RegisterMem = 1 << 5,
		
		DSP_1 = 1 << 6,
		DSP_2 = 1 << 7,
		DSP_3 = 1 << 8,
		
		Script700       = 1 << 9,
		Script700_Break = 1 << 10,
		Script700_Data  = 1 << 11,
	}
	
	public static AutoResetEvent Signal = new(false);
	
	static object requestLock = new();
	
	static Requests requests         = Requests.CycleCountOnly;
	static UInt16   memRequestStart  = 0;
	static UInt16   memRequestLength = 0x100;
	
	static Emulator emu => CliMain.PrimaryEmu;
	
	public static class Comm {
		static object commLock = new();
		
		static bool uiUsingBufferA = false;
		static bool uiUsingBufferB = false;
		
		static bool emuUsingBufferA = false;
		static bool emuUsingBufferB = false;
		
		static EmuDataContainer[] buffers = new EmuDataContainer[] { new(null), new(null) };
		
		public static void UseBufferUI(Action<EmuDataContainer> uiCallback) {
			var buffer = uiObtainBuffer();
			try     { uiCallback(buffer); }
			finally { uiReleaseBuffers(); }
		}
		
		public static void UseBufferEmu(Action<EmuDataContainer> emuCallback) {
			var buffer = emuObtainBuffer();
			try     { emuCallback(buffer); }
			finally { emuReleaseBuffers(); }
		}
		
		static EmuDataContainer uiObtainBuffer() {
			lock (commLock) {
				if (!emuUsingBufferA && !emuUsingBufferB) {
					if (buffers[0].Buffer is null) {
						uiUsingBufferB = true;
						return buffers[1];
					}
					else if (buffers[1].Buffer is null) {
						uiUsingBufferA = true;
						return buffers[0];
					}
					else {
						// Select the most recently computed buffer
						if (buffers[0].Buffer!.Step >= buffers[1].Buffer!.Step) {
							uiUsingBufferA = true;
							return buffers[0];
						}
						else {
							uiUsingBufferB = true;
							return buffers[1];
						}
					}
				}
				else if (!emuUsingBufferA) {
					uiUsingBufferA = true;
					return buffers[0];
				}
				else {
					uiUsingBufferB = true;
					return buffers[1];
				}
			}
		}
		
		static void uiReleaseBuffers() {
			lock (commLock) {
				uiUsingBufferA = false;
				uiUsingBufferB = false;
			}
		}
		
		static EmuDataContainer emuObtainBuffer() {
			lock (commLock) {
				if (!uiUsingBufferA && !uiUsingBufferB) {
					if (buffers[0].Buffer is null) {
						emuUsingBufferB = true;
						return buffers[1];
					}
					else if (buffers[1].Buffer is null) {
						emuUsingBufferA = true;
						return buffers[0];
					}
					else {
						// Select the least recently computed buffer for overwriting
						if (buffers[0].Buffer!.Step <= buffers[1].Buffer!.Step) {
							emuUsingBufferA = true;
							return buffers[0];
						}
						else {
							emuUsingBufferB = true;
							return buffers[1];
						}
					}
				}
				else if (!uiUsingBufferA) {
					emuUsingBufferA = true;
					return buffers[0];
				}
				else {
					emuUsingBufferB = true;
					return buffers[1];
				}
			}
		}
		
		static void emuReleaseBuffers() {
			lock (commLock) {
				emuUsingBufferA = false;
				emuUsingBufferB = false;
			}
		}
	}
	
	public static void SendEmuData() {
		// Transfer data from emu to main thread which the UI requests
		Comm.UseBufferEmu(container => {
			container.Buffer = new(emu.DSP.CurrentCycle);
			container.Buffer.RequestPopulate(emu, GetRequests(), memRequestStart, memRequestLength);
		});
		
		// After that's done, give the main thread the go ahead signal to display said data
		Signal.Set();
	}
	
	public static void RequestEmuData(Requests reqs, UInt16 startAddress, UInt16 length) {
		lock (requestLock) {
			requests = reqs;
			memRequestStart  = startAddress;
			memRequestLength = length;
		}
	}
	
	public static Requests GetRequests() {
		lock (requestLock) {
			return requests;
		}
	}
}