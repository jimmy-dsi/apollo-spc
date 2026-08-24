namespace Apollo;

using System.Collections;
using Jimbl;

internal unsafe class Buffer {
	byte* ptr;
	uint  size;
	
	internal byte this[int index] {
		get {
			if (index < 0 || index >= size) {
				throw new IndexOutOfRangeException();
			}
			return *(ptr + index);
		}
		set {
			if (index < 0 || index >= size) {
				throw new IndexOutOfRangeException();
			}
			*(ptr + index) = value;
		}
	}
	
	internal int Length => (int) size;
	internal IntPtr Ptr => (IntPtr) ptr;
	
	internal Buffer(int numBytes) {
		var iptr = DLL.BufferCreate(numBytes.SafeUnsigned());
		if (iptr == IntPtr.Zero) {
			throw new AllocError(); // TODO: or StateError
		}
		
		ptr  = (byte*) iptr;
		size = numBytes.SafeUnsigned();
	}
	
	~Buffer() {
		var result = DLL.BufferDestroy((IntPtr) ptr, size);
		if (!result) {
			throw new StateError();
		}
	}
}

public unsafe class UInt8Buffer: IEnumerable<byte> {
	protected byte* ptr;
	protected uint  size;
	protected bool  isReadonly;
	
	public int Length => (int) size;
	
	internal UInt8Buffer(byte* ptr, int size, bool isReadonly = false) {
		this.ptr        = ptr;
		this.size       = size.SafeUnsigned();
		this.isReadonly = isReadonly;
	}
	
	public virtual byte this[int index] {
		get => *(ptr + index % size);
		set {
			if (isReadonly) {
				throw new InvalidOperationException("Cannot write to readonly buffer");
			}
			*(ptr + index % size) = value;
		}
	}
	
	public virtual byte[] this[Range range] {
		get {
			var start = range.Start.Normalize(Length);
			var end   = range  .End.Normalize(Length);
			
			var result = new byte[end - start];
			
			for (var i = start; i < end; i++) {
				result[i - start] = this[i];
			}
			
			return result;
		}
	}

	public IEnumerator<byte> GetEnumerator() {
		for (var i = 0; i < size; i++) {
			yield return this[i];
		}
	}
	
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public unsafe class UInt8BufferShared: UInt8Buffer {
	Emulator emu;
	
	internal UInt8BufferShared(Emulator emu, byte* ptr, int size, bool isReadonly = false): base(ptr, size, isReadonly) {
		this.emu = emu;
	}
	
	public override byte this[int index] {
		get {
			emu.AcquireLock();
			try     { return *(ptr + index % size); }
			finally { emu.ReleaseLock(); }
		}
		set {
			if (isReadonly) {
				throw new InvalidOperationException("Cannot write to readonly buffer");
			}
			
			emu.AcquireLock();
			try     { *(ptr + index % size) = value; }
			finally { emu.ReleaseLock(); }
		}
	}
	
	public override byte[] this[Range range] {
		get {
			emu.AcquireLock();
			try {
				var start = range.Start.Normalize(Length);
				var end   = range  .End.Normalize(Length);
			
				var result = new byte[end - start];
			
				for (var i = start; i < end; i++) {
					result[i - start] = *(ptr + i % size);
				}
			
				return result;
			}
			finally {
				emu.ReleaseLock();
			}
		}
	}
}

public unsafe class UInt32Buffer: IEnumerable<UInt32> {
	protected UInt32* ptr;
	protected uint    size;
	protected bool    isReadonly;
	
	public int Length => (int) size;
	
	internal UInt32Buffer(UInt32* ptr, int size, bool isReadonly = false) {
		this.ptr        = ptr;
		this.size       = size.SafeUnsigned();
		this.isReadonly = isReadonly;
	}
	
	public virtual UInt32 this[int index] {
		get => *(ptr + index % size);
		set {
			if (isReadonly) {
				throw new InvalidOperationException("Cannot write to readonly buffer");
			}
			*(ptr + index % size) = value;
		}
	}

	public IEnumerator<UInt32> GetEnumerator() {
		for (var i = 0; i < size; i++) {
			yield return this[i];
		}
	}
	
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public unsafe class UInt32BufferShared: UInt32Buffer {
	Emulator emu;
	
	internal UInt32BufferShared(Emulator emu, UInt32* ptr, int size, bool isReadonly = false): base(ptr, size, isReadonly) {
		this.emu = emu;
	}
	
	public override UInt32 this[int index] {
		get {
			emu.AcquireLock();
			try     { return *(ptr + index % size); }
			finally { emu.ReleaseLock(); }
		}
		set {
			if (isReadonly) {
				throw new InvalidOperationException("Cannot write to readonly buffer");
			}
			
			emu.AcquireLock();
			try     { *(ptr + index % size) = value; }
			finally { emu.ReleaseLock(); }
		}
	}
}