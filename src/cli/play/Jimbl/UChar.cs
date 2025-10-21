namespace Jimbl;

using DataStructs;
using System.Globalization;

public class UChar: AnyChar {
	Int32 value;
	
	public override int Value => value;
	
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
	
	public static explicit operator   Byte (UChar value) => (  Byte) value.value;
	public static explicit operator  SByte (UChar value) => ( SByte) value.value;
	public static explicit operator UInt16 (UChar value) => (UInt16) value.value;
	public static explicit operator  Int16 (UChar value) => ( Int16) value.value;
	public static implicit operator UInt32 (UChar value) => (UInt32) value.value;
	public static implicit operator  Int32 (UChar value) =>          value.value;
	
	public override UnicodeCategory GetUnicodeCategory() => CharUnicodeInfo.GetUnicodeCategory(value);
	
	public override bool IsAscii()         => value <=   0x7F && ((char) value).IsAscii();
	public override bool IsAsciiDigit()    => value <=   0x7F && ((char) value).IsAsciiDigit();
	public override bool IsAsciiHexDigit() => value <=   0x7F && ((char) value).IsAsciiHexDigit();
	public override bool IsAsciiLetter()   => value <=   0x7F && ((char) value).IsAsciiLetter();
	public override bool IsControl()       => value <= 0xFFFF && ((char) value).IsControl();
	public override bool IsDigit()         => value <= 0xFFFF && ((char) value).IsDigit();
	public override bool IsLetter()        => value <= 0xFFFF && ((char) value).IsLetter();
	public override bool IsLower()         => value <= 0xFFFF && ((char) value).IsLower();
	public override bool IsNumber()        => value <= 0xFFFF && ((char) value).IsNumber();
	public override bool IsPunctuation()   => value <= 0xFFFF && ((char) value).IsPunctuation();
	public override bool IsSeparator()     => value <= 0xFFFF && ((char) value).IsSeparator();
	public override bool IsSymbol()        => value <= 0xFFFF && ((char) value).IsSymbol();
	public override bool IsUpper()         => value <= 0xFFFF && ((char) value).IsUpper();
	public override bool IsWhiteSpace()    => value <= 0xFFFF && ((char) value).IsWhiteSpace();
	
	public override AnyChar ToLower() => value <= 0xFFFF ? ((char) value).ToLower() : new UChar(value);
	public override AnyChar ToUpper() => value <= 0xFFFF ? ((char) value).ToUpper() : new UChar(value);
	
	public override string    ToString()    => char.ConvertFromUtf32(value);
	public override AnyString ToAnyString() => ToUString();
	
	public UString ToUString() {
		return new(ToString());
	}
}