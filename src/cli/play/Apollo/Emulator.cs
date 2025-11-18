using Jimbl;

namespace Apollo;

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
	
	public   DSP    DSP    { get; init; }
	internal Handle handle { get;  set; }
	
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
	
	public Emulator(bool setAsMain = false) {
		handle = DLL.EmuCreate(setAsMain);
		if (handle.IsInvalid) {
			throw new StateError(); // TODO: or AllocError
		}
		
		DSP = new(this);
		handle.AddToCache(this);
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
}