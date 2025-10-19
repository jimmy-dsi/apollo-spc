namespace Jimbl.DataStructs;

using System.Numerics;
using System.Diagnostics.CodeAnalysis;

public abstract class AnyChar {
	public static implicit operator AnyChar (char chr) => new AnyChar<char>(chr);
	
	public abstract int Value { get; }
	
	// Abstracts - Must be overridden
	public abstract bool IsAscii();
	public abstract bool IsAsciiDigit();
	public abstract bool IsAsciiHexDigit();
	public abstract bool IsAsciiLetter();
	public abstract bool IsControl();
	public abstract bool IsDigit();
	public abstract bool IsLetter();
	public abstract bool IsLower();
	public abstract bool IsNumber();
	public abstract bool IsPunctuation();
	public abstract bool IsSeparator();
	public abstract bool IsSymbol();
	public abstract bool IsUpper();
	public abstract bool IsWhiteSpace();
	
	public abstract AnyString ToAnyString();
	[return: NotNull]
	
	// Virtuals - Optionally overrideable
	public virtual bool IsAsciiHexDigitLower() => IsAsciiHexDigit() && IsLower();
	public virtual bool IsAsciiHexDigitUpper() => IsAsciiHexDigit() && IsUpper();
	public virtual bool IsAsciiLetterLower()   => IsAsciiLetter() && IsLower();
	public virtual bool IsAsciiLetterOrDigit() => IsAsciiLetter() || IsAsciiDigit();
	public virtual bool IsAsciiLetterUpper()   => IsAsciiLetter() && IsUpper();
	public virtual bool IsLetterOrDigit()      => IsLetter() || IsDigit();
	
	public virtual bool IsBetween(AnyChar min, AnyChar max) {
		return Value >= min.Value && Value <= max.Value;
	}
	
	public override string ToString() => throw new NotImplementedException();
}

class AnyChar<T>: AnyChar where T: struct, INumber<T> {
	T innerValue;
	
	public override int Value => int.CreateTruncating(innerValue);
	
	public AnyChar(T value) {
		innerValue = value;
	}
	
	public override bool IsAscii()         => ((char) Value).IsAscii();
	public override bool IsAsciiDigit()    => ((char) Value).IsAsciiDigit();
	public override bool IsAsciiHexDigit() => ((char) Value).IsAsciiHexDigit();
	public override bool IsAsciiLetter()   => ((char) Value).IsAsciiLetter();
	public override bool IsControl()       => ((char) Value).IsControl();
	public override bool IsDigit()         => ((char) Value).IsDigit();
	public override bool IsLetter()        => ((char) Value).IsLetter();
	public override bool IsLower()         => ((char) Value).IsLower();
	public override bool IsNumber()        => ((char) Value).IsNumber();
	public override bool IsPunctuation()   => ((char) Value).IsPunctuation();
	public override bool IsSeparator()     => ((char) Value).IsSeparator();
	public override bool IsSymbol()        => ((char) Value).IsSymbol();
	public override bool IsUpper()         => ((char) Value).IsUpper();
	public override bool IsWhiteSpace()    => ((char) Value).IsWhiteSpace();
	
	public override string    ToString()    => innerValue.ToString()!;
	public override AnyString ToAnyString() => ToString();
}