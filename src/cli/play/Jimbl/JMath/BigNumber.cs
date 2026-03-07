using System.Runtime.InteropServices;

namespace Jimbl.JMath;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;

public readonly struct BigNumber:
	IComparable,
	IComparable<BigNumber>, 
	IConvertible,
	IEquatable<BigNumber>,
	ISpanFormattable,
	IUtf8SpanFormattable,
	IFloatingPoint<BigNumber>,
	IMinMaxValue<BigNumber>
{
	// This is the highest signed 128-bit value which is divisible by all integers from 2 to 22
	// It is also a multiple of 2^103 (and therefore, also a multiple of 2^64, 2^32... etc)
	public static readonly Int128 UnitPrecision = Int128.Parse("147549814206333053320842685369842401280");
	
	public static BigNumber One         { get; }
	public static BigNumber NegativeOne { get; }
	public static int       Radix       { get; }
	public static BigNumber Zero        { get; }
	public static BigNumber E           { get; }
	public static BigNumber Pi          { get; }
	public static BigNumber Tau         { get; }
	
	public static BigNumber AdditiveIdentity       { get; }
	public static BigNumber MultiplicativeIdentity { get; }
	
	public static BigNumber MaxValue { get; }
	public static BigNumber MinValue { get; }
	
	// Fields
	readonly BigInteger value;
	readonly double     specialValue = 0;
	
	// Properties
	internal bool IsSpecial => double.IsSubnormal(specialValue)
	                        || double.IsInfinity(specialValue)
	                        || double.IsNaN(specialValue);
	
	// Constructors
	public BigNumber() { }
	
	internal BigNumber(BigInteger value) {
		this.value = value;
	}
	
	internal BigNumber(double specialValue) {
		this.specialValue = specialValue;
	}
	
	// Compare
	public int CompareTo(object? obj) {
		throw new NotImplementedException();
	}
	
	public int CompareTo(BigNumber other) {
		throw new NotImplementedException();
	}
	
	// ???
	public TypeCode GetTypeCode() {
		throw new NotImplementedException();
	}
	
	// Conversions (to)
	public bool ToBoolean(IFormatProvider? provider) {
		throw new NotImplementedException();
	}
	
	public byte ToByte(IFormatProvider? provider) {
		throw new NotImplementedException();
	}
	
	public char ToChar(IFormatProvider? provider) {
		throw new NotImplementedException();
	}
	
	public DateTime ToDateTime(IFormatProvider? provider) {
		throw new NotImplementedException();
	}
	
	public decimal ToDecimal(IFormatProvider? provider) {
		throw new NotImplementedException();
	}
	
	public double ToDouble(IFormatProvider? provider) {
		throw new NotImplementedException();
	}
	
	public short ToInt16(IFormatProvider? provider) {
		throw new NotImplementedException();
	}
	
	public int ToInt32(IFormatProvider? provider) {
		throw new NotImplementedException();
	}
	
	public long ToInt64(IFormatProvider? provider) {
		throw new NotImplementedException();
	}
	
	public sbyte ToSByte(IFormatProvider? provider) {
		throw new NotImplementedException();
	}
	
	public float ToSingle(IFormatProvider? provider) {
		throw new NotImplementedException();
	}
	
	public string ToString(IFormatProvider? provider) {
		throw new NotImplementedException();
	}
	
	public object ToType(Type conversionType, IFormatProvider? provider) {
		throw new NotImplementedException();
	}
	
	public ushort ToUInt16(IFormatProvider? provider) {
		throw new NotImplementedException();
	}
	
	public uint ToUInt32(IFormatProvider? provider) {
		throw new NotImplementedException();
	}
	
	public ulong ToUInt64(IFormatProvider? provider) {
		throw new NotImplementedException();
	}
	
	public static bool TryConvertFromChecked<TOther>(TOther value, [MaybeNullWhen(false)] out BigNumber result) where TOther: INumberBase<TOther> {
		throw new NotImplementedException();
	}
	
	public static bool TryConvertFromSaturating<TOther>(TOther value, [MaybeNullWhen(false)] out BigNumber result) where TOther: INumberBase<TOther> {
		throw new NotImplementedException();
	}
	
	public static bool TryConvertFromTruncating<TOther>(TOther value, [MaybeNullWhen(false)] out BigNumber result) where TOther: INumberBase<TOther> {
		throw new NotImplementedException();
	}
	
	public static bool TryConvertToChecked<TOther>(BigNumber value, [MaybeNullWhen(false)] out TOther result) where TOther: INumberBase<TOther> {
		throw new NotImplementedException();
	}
	
	public static bool TryConvertToSaturating<TOther>(BigNumber value, [MaybeNullWhen(false)] out TOther result) where TOther: INumberBase<TOther> {
		throw new NotImplementedException();
	}
	
	public static bool TryConvertToTruncating<TOther>(BigNumber value, [MaybeNullWhen(false)] out TOther result) where TOther: INumberBase<TOther> {
		throw new NotImplementedException();
	}
	
	// Equals
	public bool Equals(BigNumber other) {
		throw new NotImplementedException();
	}
	
	// Formatting
	public string ToString(string? format, IFormatProvider? formatProvider) {
		throw new NotImplementedException();
	}
	
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) {
		throw new NotImplementedException();
	}
	
	public static BigNumber Parse(string s, IFormatProvider? provider) {
		throw new NotImplementedException();
	}
	
	public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out BigNumber result) {
		throw new NotImplementedException();
	}
	
	public static BigNumber Parse(ReadOnlySpan<char> s, IFormatProvider? provider) {
		throw new NotImplementedException();
	}
	
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out BigNumber result) {
		throw new NotImplementedException();
	}
	
	public static BigNumber Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider) {
		throw new NotImplementedException();
	}
	
	public static BigNumber Parse(string s, NumberStyles style, IFormatProvider? provider) {
		throw new NotImplementedException();
	}
	
	public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out BigNumber result) {
		throw new NotImplementedException();
	}
	
	public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out BigNumber result) {
		throw new NotImplementedException();
	}
	
	// Operators
	public static BigNumber operator + (BigNumber value) {
		throw new NotImplementedException();
	}
	
	public static BigNumber operator - (BigNumber value) {
		throw new NotImplementedException();
	}
	
	public static BigNumber operator + (BigNumber left, BigNumber right) {
		throw new NotImplementedException();
	}
	
	public static BigNumber operator - (BigNumber left, BigNumber right) {
		throw new NotImplementedException();
	}
	
	public static BigNumber operator * (BigNumber left, BigNumber right) {
		throw new NotImplementedException();
	}
	
	public static BigNumber operator / (BigNumber left, BigNumber right) {
		throw new NotImplementedException();
	}
	
	public static BigNumber operator % (BigNumber left, BigNumber right) {
		throw new NotImplementedException();
	}
	
	public static BigNumber operator ++ (BigNumber value) {
		throw new NotImplementedException();
	}
	
	public static BigNumber operator -- (BigNumber value) {
		throw new NotImplementedException();
	}
	
	public static bool operator == (BigNumber left, BigNumber right) {
		throw new NotImplementedException();
	}
	
	public static bool operator != (BigNumber left, BigNumber right) {
		throw new NotImplementedException();
	}
	
	public static bool operator < (BigNumber left, BigNumber right) {
		throw new NotImplementedException();
	}
	
	public static bool operator > (BigNumber left, BigNumber right) {
		throw new NotImplementedException();
	}
	
	public static bool operator <= (BigNumber left, BigNumber right) {
		throw new NotImplementedException();
	}
	
	public static bool operator >= (BigNumber left, BigNumber right) {
		throw new NotImplementedException();
	}
	
	// Statics
	public static BigNumber Abs(BigNumber value) {
		if (value.IsSpecial) {
			return new(double.Abs(value.specialValue));
		}
		else {
			return new(BigInteger.Abs(value.value));
		}
	}
	
	public static bool IsCanonical(BigNumber value) {
		return !value.IsSpecial || isCanonical(value.specialValue);
	}
	
	public static bool IsFinite(BigNumber value) {
		return !value.IsSpecial || double.IsFinite(value.specialValue);
	}
	
	public static bool IsInteger(BigNumber value) {
		return !value.IsSpecial && value == value.truncate();
	}
	
	public static bool IsEvenInteger(BigNumber value) {
		return IsInteger(value) /*&& value % (BigNumber) 2 == 0*/;
	}
	
	public static bool IsOddInteger(BigNumber value) {
		return IsInteger(value) /*&& value % (BigNumber) 2 != 0*/;
	}
	
	public static bool IsPositive(BigNumber value) {
		return !value.IsSpecial && value.value >= 0 || value.IsSpecial && double.IsPositive(value.specialValue);
	}
	
	public static bool IsNegative(BigNumber value) {
		return !value.IsSpecial && value.value < 0 || value.IsSpecial && double.IsNegative(value.specialValue);
	}
	
	public static bool IsNormal(BigNumber value) {
		return !value.IsSpecial || double.IsNormal(value.specialValue);
	}
	
	public static bool IsSubnormal(BigNumber value) {
		return value.IsSpecial && double.IsSubnormal(value.specialValue);
	}
	
	public static bool IsInfinity(BigNumber value) {
		return value.IsSpecial && double.IsInfinity(value.specialValue);
	}
	
	public static bool IsPositiveInfinity(BigNumber value) {
		return value.IsSpecial && double.IsPositiveInfinity(value.specialValue);
	}
	
	public static bool IsNegativeInfinity(BigNumber value) {
		return value.IsSpecial && double.IsNegativeInfinity(value.specialValue);
	}
	
	public static bool IsNaN(BigNumber value) {
		return value.IsSpecial && double.IsNaN(value.specialValue);
	}
	
	public static bool IsRealNumber(BigNumber value) {
		return !value.IsSpecial || double.IsRealNumber(value.specialValue);
	}
	
	public static bool IsImaginaryNumber(BigNumber value) {
		return false;
	}
	
	public static bool IsComplexNumber(BigNumber value) {
		return false;
	}
	
	public static bool IsZero(BigNumber value) {
		return value.value == 0;
	}
	
	public static BigNumber MaxMagnitude(BigNumber x, BigNumber y) {
		throw new NotImplementedException();
	}
	
	public static BigNumber MaxMagnitudeNumber(BigNumber x, BigNumber y) {
		throw new NotImplementedException();
	}
	
	public static BigNumber MinMagnitude(BigNumber x, BigNumber y) {
		throw new NotImplementedException();
	}
	
	public static BigNumber MinMagnitudeNumber(BigNumber x, BigNumber y) {
		throw new NotImplementedException();
	}
	
	public static BigNumber Round(BigNumber x, int digits, MidpointRounding mode) {
		throw new NotImplementedException();
	}
	
	// Irrelevant floating point stuff, not implemented
	public int GetExponentByteCount() {
		throw new NotImplementedException();
	}
	
	public int GetExponentShortestBitLength() {
		throw new NotImplementedException();
	}
	
	public int GetSignificandBitLength() {
		throw new NotImplementedException();
	}
	
	public int GetSignificandByteCount() {
		throw new NotImplementedException();
	}
	
	public bool TryWriteExponentBigEndian(Span<byte> destination, out int bytesWritten) {
		throw new NotImplementedException();
	}
	
	public bool TryWriteExponentLittleEndian(Span<byte> destination, out int bytesWritten) {
		throw new NotImplementedException();
	}
	
	public bool TryWriteSignificandBigEndian(Span<byte> destination, out int bytesWritten) {
		throw new NotImplementedException();
	}
	
	public bool TryWriteSignificandLittleEndian(Span<byte> destination, out int bytesWritten) {
		throw new NotImplementedException();
	}
	
	// Helpers
	BigNumber truncate() {
		if (IsSpecial) {
			return new((BigInteger) (long) specialValue * UnitPrecision);
		}
		else {
			return new(value / UnitPrecision * UnitPrecision);
		}
	}
	
	static bool isCanonical<T>(T value) where T: INumber<T> {
		return T.IsCanonical(value);
	}
}