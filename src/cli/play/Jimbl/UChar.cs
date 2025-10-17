namespace Jimbl;

public struct UChar {
	Int32 value;
	
	public UChar(int value) {
		this.value = value;
	}
	
	public UChar(char value) {
		this.value = value;
	}
	
	public static implicit operator UChar ( byte value) => new(value);
	public static implicit operator UChar (sbyte value) => new(value);
	
	public static implicit operator UChar (UInt16 value) => new(        value);
	public static implicit operator UChar (UInt32 value) => new((Int32) value);
	public static explicit operator UChar (UInt64 value) => new((Int32) value);
	
	public static implicit operator UChar (Int16 value) => new(        value);
	public static implicit operator UChar (Int32 value) => new(        value);
	public static explicit operator UChar (Int64 value) => new((Int32) value);
	
	public static implicit operator UChar (char value) => new(value);
}