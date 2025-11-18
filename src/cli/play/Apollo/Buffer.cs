namespace Apollo;

using Jimbl;

public unsafe class UInt8Buffer {
	byte* ptr;
	uint  size;
	
	internal UInt8Buffer(byte* ptr, int size) {
		this.ptr  = ptr;
		this.size = size.SafeUnsigned();
	}
	
	public byte this[int index] {
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
	
	public byte this[ushort index] {
		get {
			return *(ptr + index);
		}
		set {
			*(ptr + index) = value;
		}
	}
	
	public byte this[byte index] {
		get {
			return *(ptr + (index & 0x7F));
		}
		set {
			*(ptr + (index & 0x7F)) = value;
		}
	}
}