namespace Jimbl;

public struct UChar {
	Int32 value;
	
	public UChar(Int32 value) {
		this.value = value;
	}
	
	public UChar(Char value) {
		this.value = value;
	}
	
	public static implicit operator UChar ( Byte value) => new(value);
	public static implicit operator UChar (SByte value) => new(value);
	
	public static implicit operator UChar (UInt16 value) => new(        value);
	public static implicit operator UChar (UInt32 value) => new((Int32) value);
	public static explicit operator UChar (UInt64 value) => new((Int32) value);
	
	public static implicit operator UChar (Int16 value) => new(        value);
	public static implicit operator UChar (Int32 value) => new(        value);
	public static explicit operator UChar (Int64 value) => new((Int32) value);
	
	public static implicit operator UChar (Char value) => new(value);
	
	public static explicit operator  Byte  (UChar value) => (  Byte) value.value;
	public static explicit operator SByte  (UChar value) => ( SByte) value.value;
	public static explicit operator UInt16 (UChar value) => (UInt16) value.value;
	public static explicit operator Int16  (UChar value) => ( Int16) value.value;
	public static implicit operator UInt32 (UChar value) => (UInt32) value.value;
	public static implicit operator Int32  (UChar value) =>          value.value;
}