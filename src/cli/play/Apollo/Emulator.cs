namespace Apollo;

using Jimbl;
using System.Runtime.InteropServices;

public class Emulator {
	internal class Handle: SafeHandle {
		static Dictionary<IntPtr, (Handle, Emulator)> cache = new();
		
		Handle? keepPrimaryAlive = null; // References the first creation of matching IntPtr handle - Can only be destroyed if all references to it are
		bool    trueOwnsHandle   = true;
		
		public object Lock = new();
		
		public Handle(): base(IntPtr.Zero, true) { }
		
		public void PostConstruct() {
			lock (Lock) {
				if (cache.ContainsKey(handle)) {
					trueOwnsHandle   = false;
					keepPrimaryAlive = cache[handle].Item1;
				}
			}
		}
		
		public void AddToCache(Emulator emu) {
			lock (Lock) {
				cache[handle] = (this, emu);
			}
		}
		
		public Emulator? GetCachedEmu() {
			lock (Lock) {
				if (!cache.ContainsKey(handle)) {
					return null;
				}
				return cache[handle].Item2;
			}
		}
		
		public override bool IsInvalid => handle == IntPtr.Zero;
		
		protected override bool ReleaseHandle() {
			if (trueOwnsHandle) {
				var status = DLL.EmuDestroy(this);
				if (!status) {
					var errorCode = DLL.GetLastError();
					Error.Throw(errorCode);
				}
				
				lock (Lock) {
					if (ReferenceEquals(cache[handle].Item1, this)) {
						cache.Remove(handle);
					}
				}
			}
				
			handle = IntPtr.Zero;
			return true;
		}
	}
	
	uint lastRenderPosition = 0;
	bool makeShared         = false;
	
	bool[] mainVoiceOnStates = new bool[8] {
		true, true, true, true,
		true, true, true, true
	};
	
	bool[] echoVoiceOnStates = new bool[8] {
		true, true, true, true,
		true, true, true, true
	};
	
	SPC.Metadata? metadata = null;
	
	public DSP       DSP       { get; init; }
	public SMP       SMP       { get; init; }
	public SPC       SPC       { get; init; }
	public Script700 Script700 { get; init; }
	
	internal Handle handle { get; set; }

	public bool MakeShared => makeShared;
	
	public (bool Main, bool Echo)[] VoiceOnStates {
		get {
			MaybeAcquireLock();
			try {
				return new[] {
					(Main: mainVoiceOnStates[0], Echo: echoVoiceOnStates[0]), (Main: mainVoiceOnStates[1], Echo: echoVoiceOnStates[1]),
					(Main: mainVoiceOnStates[2], Echo: echoVoiceOnStates[2]), (Main: mainVoiceOnStates[3], Echo: echoVoiceOnStates[3]),
					(Main: mainVoiceOnStates[4], Echo: echoVoiceOnStates[4]), (Main: mainVoiceOnStates[5], Echo: echoVoiceOnStates[5]),
					(Main: mainVoiceOnStates[6], Echo: echoVoiceOnStates[6]), (Main: mainVoiceOnStates[7], Echo: echoVoiceOnStates[7]),
				};
			}
			finally {
				MaybeReleaseLock();
			}
		}
	}
	
	public bool[] MainVoiceOnStates {
		get {
			MaybeAcquireLock();
			try {
				return (bool[]) mainVoiceOnStates.Clone();
			}
			finally {
				MaybeReleaseLock();
			}
		}
	}
	
	public bool[] EchoVoiceOnStates {
		get {
			MaybeAcquireLock();
			try {
				return (bool[]) echoVoiceOnStates.Clone();
			}
			finally {
				MaybeReleaseLock();
			}
		}
	}

	public int LastRenderPosition => (int) lastRenderPosition;
	public int RenderPosition {
		get {
			MaybeAcquireLock();
			try {
				var result = (int) DLL.EmuGetRenderPosition(handle);
				CheckForError();
				return result;
			}
			finally {
				MaybeReleaseLock();
			}
		}
	}
	
	public int SamplesQueued {
		get {
			var samples = RenderPosition - LastRenderPosition; // Render position may obtain lock so leave this line out of the locked region
			MaybeAcquireLock();
			
			try {
				if (samples < 0) {
					var fullBufLen = (int) DLL.EmuGetRenderBufferLen(handle);
					samples += fullBufLen;
				
					if (fullBufLen == 0) {
						var errorCode = DLL.EmuGetLastError(handle);
						Error.Throw(errorCode);
					}
				}
				
				return samples;
			}
			finally {
				MaybeReleaseLock();
			}
		}
	}

	public static Emulator? MainInstance {
		get {
			var mainHandle = DLL.EmuGetMainInstance();
			
			// TODO: Error handling here (might legitimately just be null (unset))
			if (mainHandle.IsInvalid) {
				return null;
			}
			
			mainHandle.PostConstruct();
			return mainHandle.GetCachedEmu();
		}
		set {
			var result = DLL.EmuReassignMainInstance(value?.handle);
			if (!result) {
				var errorCode = DLL.GetLastError();
				Error.Throw(errorCode);
			}
		}
	}
	
	public bool IsMainInstance => ReferenceEquals(this, MainInstance);
	
	public SPC.Metadata SpcMetadata {
		get {
			if (metadata != null) {
				return metadata;
			}
			
			MaybeAcquireLock();
		
			try {
				var md = DLL.SpcGetMetadata(handle);
				if (md.IsValid == 0) {
					var errorCode = DLL.EmuGetLastError(handle);
					Error.Throw(errorCode);
				}
			
				metadata = new(md);
				return metadata;
			}
			finally {
				MaybeReleaseLock();
			}
		}
	}
	
	public UInt32 LastResultCode => DLL.EmuGetLastResult(handle); // Get the last result code, regardless of whether it has succeeded or failed
	public UInt32 LastErrorCode  => DLL.EmuGetLastError(handle);  // Get the last result code of the previous operation which resulted in an error
	
	public Emulator(bool setAsMain = false, bool makeShared = false) {
		handle = DLL.EmuCreate(setAsMain);
		
		if (handle.IsInvalid) {
			var errorCode = DLL.GetLastError();
			Error.Throw(errorCode);
		}
		
		handle.PostConstruct();
		
		DSP       = new(this);
		SMP       = new(this);
		SPC       = new(this);
		Script700 = new(this);
		
		handle.AddToCache(this);
		
		this.makeShared = makeShared;
		
		if (IsMainInstance) {
			MaybeAcquireLock();
			try {
				lastRenderPosition = DLL.EmuGetRenderPosition(handle);
				CheckForError();
			}
			finally {
				MaybeReleaseLock();
			}
		}
	}
	
	public void LoadSpcFile(string filePath) {
		var data = File.ReadAllBytes(filePath);
		LoadSpcFile(data);
	}
	
	public void LoadSpcFile(byte[] fileData) {
		MaybeAcquireLock();
		
		try {
			Buffer buffer = new(fileData.Length);
			for (var i = 0; i < fileData.Length; i++) {
				buffer[i] = fileData[i];
			}
		
			var result = DLL.SpcLoad(buffer.Ptr, buffer.Length.SafeUnsigned(), handle);
			if (!result) {
				var errorCode = DLL.EmuGetLastError(handle);
				Error.Throw(errorCode);
			}
		}
		finally {
			MaybeReleaseLock();
		}
	}
	
	public void StepCycle() {
		MaybeAcquireLock();
		
		try {
			var result = DLL.EmuStepCycle(handle);
			if (!result) {
				var errorCode = DLL.EmuGetLastError(handle);
				Error.Throw(errorCode);
			}
		}
		finally {
			MaybeReleaseLock();
		}
	}
	
	public void StepCycleFast() {
		MaybeAcquireLock();
		
		try {
			var result = DLL.EmuStepCycleFast(handle);
			if (!result) {
				var errorCode = DLL.EmuGetLastError(handle);
				Error.Throw(errorCode);
			}
		}
		finally {
			MaybeReleaseLock();
		}
	}
	
	public void StepInstruction() {
		MaybeAcquireLock();

		try {
			var result = DLL.EmuStepInstruction(handle);
			if (!result) {
				var errorCode = DLL.EmuGetLastError(handle);
				Error.Throw(errorCode);
			}
		}
		finally {
			MaybeReleaseLock();
		}
	}
	
	public int StepNCycles(int cycles) {
		MaybeAcquireLock();
		try     { return (int) DLL.EmuStepNCycles(cycles.SafeUnsigned(), handle); }
		finally { MaybeReleaseLock(); }
	}
	
	public int StepNCyclesFast(int cycles) {
		MaybeAcquireLock();
		try     { return (int) DLL.EmuStepNCyclesFast(cycles.SafeUnsigned(), handle); }
		finally { MaybeReleaseLock(); }
	}
	
	internal void CheckForError() {
		var resultCode = DLL.EmuGetLastResult(handle);
		if (resultCode != 0) {
			Error.Throw(resultCode);
		}
	}
	
	public Int16[] GetBufferedSamples() {
		unsafe {
			MaybeAcquireLock();
			
			try {
				var bufferLeft  = (Int16*) DLL.EmuGetRenderBuffer(0, handle);
				var bufferRight = (Int16*) DLL.EmuGetRenderBuffer(1, handle);
				var fullBufLen  = (int)    DLL.EmuGetRenderBufferLen(handle);
				
				if (fullBufLen == 0) {
					var errorCode = DLL.EmuGetLastError(handle);
					Error.Throw(errorCode);
				}
				else {
					CheckForError();
				}
		
				var current     = DLL.EmuGetRenderPosition(handle);
				var sampleCount = current - (int) lastRenderPosition;
				
				CheckForError();
		
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
			finally {
				MaybeReleaseLock();
			}
		}
	}
	
	public bool ToggleVoice(int voiceIndex) {
		MaybeAcquireLock();
		
		try {
			if (voiceIndex is < 0 or >= 8) {
				throw new ArgumentOutOfRangeException();
			}
			
			var result = DLL.EmuToggleVoice(handle, (byte) voiceIndex);
			if (!result) {
				var errorCode = DLL.EmuGetLastError(handle);
				Error.Throw(errorCode);
			}
			
			mainVoiceOnStates[voiceIndex] = !mainVoiceOnStates[voiceIndex];
			echoVoiceOnStates[voiceIndex] =  mainVoiceOnStates[voiceIndex];
			
			return mainVoiceOnStates[voiceIndex];
		}
		finally {
			MaybeReleaseLock();
		}
	}
	
	public bool EnableVoice(int voiceIndex) {
		MaybeAcquireLock();
		
		try {
			if (voiceIndex is < 0 or >= 8) {
				throw new ArgumentOutOfRangeException();
			}
			
			var result = DLL.EmuEnableVoice(handle, (byte) voiceIndex);
			if (!result) {
				var errorCode = DLL.EmuGetLastError(handle);
				Error.Throw(errorCode);
			}
			
			mainVoiceOnStates[voiceIndex] = true;
			echoVoiceOnStates[voiceIndex] = true;
			
			return true;
		}
		finally {
			MaybeReleaseLock();
		}
	}
	
	public bool DisableVoice(int voiceIndex) {
		MaybeAcquireLock();
		
		try {
			if (voiceIndex is < 0 or >= 8) {
				throw new ArgumentOutOfRangeException();
			}
			
			var result = DLL.EmuDisableVoice(handle, (byte) voiceIndex);
			if (!result) {
				var errorCode = DLL.EmuGetLastError(handle);
				Error.Throw(errorCode);
			}
			
			mainVoiceOnStates[voiceIndex] = false;
			echoVoiceOnStates[voiceIndex] = false;
			
			return true;
		}
		finally {
			MaybeReleaseLock();
		}
	}
	
	public bool ToggleMainVoice(int voiceIndex) {
		MaybeAcquireLock();
		
		try {
			if (voiceIndex is < 0 or >= 8) {
				throw new ArgumentOutOfRangeException();
			}
			
			var result = DLL.EmuToggleMainVoice(handle, (byte) voiceIndex);
			if (!result) {
				var errorCode = DLL.EmuGetLastError(handle);
				Error.Throw(errorCode);
			}
			
			mainVoiceOnStates[voiceIndex] = !mainVoiceOnStates[voiceIndex];
			return mainVoiceOnStates[voiceIndex];
		}
		finally {
			MaybeReleaseLock();
		}
	}
	
	public bool EnableMainVoice(int voiceIndex) {
		MaybeAcquireLock();
		
		try {
			if (voiceIndex is < 0 or >= 8) {
				throw new ArgumentOutOfRangeException();
			}
			
			var result = DLL.EmuEnableMainVoice(handle, (byte) voiceIndex);
			if (!result) {
				var errorCode = DLL.EmuGetLastError(handle);
				Error.Throw(errorCode);
			}
			
			mainVoiceOnStates[voiceIndex] = true;
			return true;
		}
		finally {
			MaybeReleaseLock();
		}
	}
	
	public bool DisableMainVoice(int voiceIndex) {
		MaybeAcquireLock();
		
		try {
			if (voiceIndex is < 0 or >= 8) {
				throw new ArgumentOutOfRangeException();
			}
			
			var result = DLL.EmuDisableMainVoice(handle, (byte) voiceIndex);
			if (!result) {
				var errorCode = DLL.EmuGetLastError(handle);
				Error.Throw(errorCode);
			}
			
			mainVoiceOnStates[voiceIndex] = false;
			return true;
		}
		finally {
			MaybeReleaseLock();
		}
	}
	
	public bool ToggleEchoVoice(int voiceIndex) {
		MaybeAcquireLock();
		
		try {
			if (voiceIndex is < 0 or >= 8) {
				throw new ArgumentOutOfRangeException();
			}
			
			var result = DLL.EmuToggleEchoVoice(handle, (byte) voiceIndex);
			if (!result) {
				var errorCode = DLL.EmuGetLastError(handle);
				Error.Throw(errorCode);
			}
			
			echoVoiceOnStates[voiceIndex] = !echoVoiceOnStates[voiceIndex];
			return echoVoiceOnStates[voiceIndex];
		}
		finally {
			MaybeReleaseLock();
		}
	}
	
	public bool EnableEchoVoice(int voiceIndex) {
		MaybeAcquireLock();
		
		try {
			if (voiceIndex is < 0 or >= 8) {
				throw new ArgumentOutOfRangeException();
			}
			
			var result = DLL.EmuEnableEchoVoice(handle, (byte) voiceIndex);
			if (!result) {
				var errorCode = DLL.EmuGetLastError(handle);
				Error.Throw(errorCode);
			}
			
			echoVoiceOnStates[voiceIndex] = true;
			return true;
		}
		finally {
			MaybeReleaseLock();
		}
	}
	
	public bool DisableEchoVoice(int voiceIndex) {
		MaybeAcquireLock();
		
		try {
			if (voiceIndex is < 0 or >= 8) {
				throw new ArgumentOutOfRangeException();
			}
			
			var result = DLL.EmuDisableEchoVoice(handle, (byte) voiceIndex);
			if (!result) {
				var errorCode = DLL.EmuGetLastError(handle);
				Error.Throw(errorCode);
			}
			
			echoVoiceOnStates[voiceIndex] = false;
			return true;
		}
		finally {
			MaybeReleaseLock();
		}
	}
	
	public Emulator SaveState(bool makeShared = false) {
		MaybeAcquireLock();
		
		try {
			Emulator emuCopy = new(makeShared: makeShared);
			
			var result = DLL.EmuCopy(emuCopy.handle, handle);
			if (!result) {
				var errorCode = DLL.EmuGetLastError(handle);
				Error.Throw(errorCode);
			}
			
			return emuCopy;
		}
		finally {
			MaybeReleaseLock();
		}
	}
	
	public void LoadStateFrom(Emulator other) {
		MaybeAcquireLock();
		other.MaybeAcquireLock();
		
		try {
			var result = DLL.EmuCopy(handle, other.handle);
			if (!result) {
				var errorCode = DLL.EmuGetLastError(handle);
				Error.Throw(errorCode);
			}
		}
		finally {
			other.MaybeReleaseLock();
			MaybeReleaseLock();
		}
	}
	
	internal void AcquireLock() {
		var result = DLL.EmuAcquireLock(handle);
		if (!result) {
			var errorCode = DLL.EmuGetLastError(handle);
			Error.Throw(errorCode);
		}
	}
	
	internal void ReleaseLock() {
		var result = DLL.EmuReleaseLock(handle);
		if (!result) {
			var errorCode = DLL.EmuGetLastError(handle);
			Error.Throw(errorCode);
		}
	}
	
	internal void MaybeAcquireLock() {
		if (makeShared) return;
		AcquireLock();
	}
	
	internal void MaybeReleaseLock() {
		if (makeShared) return;
		ReleaseLock();
	}
}