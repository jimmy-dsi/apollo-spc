namespace Apollo;

using Jimbl;

using System.Runtime.InteropServices;

public class Emulator {
	internal class Handle: SafeHandle {
		static Dictionary<IntPtr, (Handle, Emulator)> cache = new();
		
		Handle? keepPrimaryAlive = null; // References the first creation of matching IntPtr handle - Can only be destroyed if all references to it are
		bool    trueOwnsHandle   = true;
		
		public Handle(): base(IntPtr.Zero, true) {
			if (cache.ContainsKey(handle)) {
				trueOwnsHandle   = false;
				keepPrimaryAlive = cache[handle].Item1;
			}
		}
		
		public void AddToCache(Emulator emu) {
			cache[handle] = (this, emu);
		}
		
		public Emulator? GetCachedEmu() {
			if (!cache.ContainsKey(handle)) {
				return null;
			}
			return cache[handle].Item2;
		}
		
		public override bool IsInvalid => handle == IntPtr.Zero;
		
		protected override bool ReleaseHandle() {
			if (trueOwnsHandle) {
				var status = DLL.EmuDestroy(this);
				if (!status) {
					throw new StateError();
				}
				
				if (ReferenceEquals(cache[handle].Item1, this)) {
					cache.Remove(handle);
				}
			}
				
			handle = IntPtr.Zero;
			return true;
		}
	}
	
	uint lastRenderPosition = 0;
	
	public   DSP    DSP    { get; init; }
	public   SMP    SMP    { get; init; }
	internal Handle handle { get;  set; }

	public int LastRenderPosition => (int) lastRenderPosition;
	public int RenderPosition     => (int) DLL.EmuGetRenderPosition(handle);
	
	public int SamplesQueued {
		get {
			var samples = RenderPosition - LastRenderPosition;
			if (samples < 0) {
				samples += (int) DLL.EmuGetRenderBufferLen(handle);
			}
			return samples;
		}
	}

	public static Emulator? MainInstance {
		get {
			var mainHandle = DLL.EmuGetMainInstance();
			if (mainHandle.IsInvalid) {
				return null;
			}
			
			return mainHandle.GetCachedEmu();
		}
		set {
			var result = DLL.EmuReassignMainInstance(value?.handle);
			if (!result) {
				throw new StateError(); // TODO: or NullError or AllocError
			}
		}
	}
	
	public bool IsMainInstance => ReferenceEquals(this, MainInstance);
	
	public Emulator(bool setAsMain = false) {
		handle = DLL.EmuCreate(setAsMain);
		if (handle.IsInvalid) {
			throw new StateError(); // TODO: or AllocError
		}
		
		DSP = new(this);
		SMP = new(this);
		handle.AddToCache(this);
		
		if (setAsMain) {
			lastRenderPosition = DLL.EmuGetRenderPosition(handle);
		}
	}
	
	public void LoadSpcFile(string filePath) {
		var data = File.ReadAllBytes(filePath);
		LoadSpcFile(data);
	}
	
	public void LoadSpcFile(byte[] fileData) {
		Buffer buffer = new(fileData.Length);
		for (var i = 0; i < fileData.Length; i++) {
			buffer[i] = fileData[i];
		}
		
		var result = DLL.SpcLoad(buffer.Ptr, buffer.Length.SafeUnsigned(), handle);
		if (!result) {
			throw new SpcLoadError(); // TODO: or StateError or NullError
		}
	}
	
	public void StepCycle() {
		var result = DLL.EmuStepCycle(handle);
		if (!result) {
			throw new StateError(); // TODO: or Script700Timeout
		}
	}
	
	public void StepCycleFast() {
		var result = DLL.EmuStepCycleFast(handle);
		if (!result) {
			throw new StateError();
		}
	}
	
	public int StepNCycles(int cycles) {
		return (int) DLL.EmuStepNCycles(cycles.SafeUnsigned(), handle);
	}
	
	public int StepNCyclesFast(int cycles) {
		return (int) DLL.EmuStepNCyclesFast(cycles.SafeUnsigned(), handle);
	}
	
	public Int16[] GetBufferedSamples() {
		unsafe {
			var bufferLeft  = (Int16*) DLL.EmuGetRenderBuffer(0, handle);
			var bufferRight = (Int16*) DLL.EmuGetRenderBuffer(1, handle);
			var fullBufLen  = (int)    DLL.EmuGetRenderBufferLen(handle);
		
			var current     = DLL.EmuGetRenderPosition(handle);
			var sampleCount = current - (int) lastRenderPosition;
		
			if (sampleCount < 0) {
				sampleCount += (int) fullBufLen;
			}
		
			var bufferedSamples = new Int16[sampleCount * 2];
			for (var i = 0; i < sampleCount; i++) {
				bufferedSamples[i * 2]     = *(bufferLeft  + (lastRenderPosition + i) % fullBufLen);
				bufferedSamples[i * 2 + 1] = *(bufferRight + (lastRenderPosition + i) % fullBufLen);
			}
			
			lastRenderPosition = current;
			
			return bufferedSamples;
		}
	}
}