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
	byte* ptr;
	uint  size;
	bool  isReadonly;
	
	public int Length => (int) size;
	
	internal UInt8Buffer(byte* ptr, int size, bool isReadonly = false) {
		this.ptr        = ptr;
		this.size       = size.SafeUnsigned();
		this.isReadonly = isReadonly;
	}
	
	public byte this[int index] {
		get => *(ptr + index % size);
		set {
			if (isReadonly) {
				throw new InvalidOperationException("Cannot write to readonly buffer");
			}
			*(ptr + index % size) = value;
		}
	}

	public IEnumerator<byte> GetEnumerator() {
		for (var i = 0; i < size; i++) {
			yield return this[i];
		}
	}
	
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}