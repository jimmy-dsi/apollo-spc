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
	// This unit value is carefully picked to represent the base-10 precision of decimal values (10^28)
	// as well as the base-2 precision of double values (2^53). It is also a multiple of common small integers for better fractional accuracy (2-24)
	public static readonly BigInteger UnitPrecision = BigInteger.Parse("16533182789382915930193920000000000000000000000000000");
	
	static BigNumber one              = new( UnitPrecision);
	static BigNumber negativeOne      = new(-UnitPrecision);
	static BigNumber zero             = new((BigInteger) 0);
	static BigNumber e                = new(BigInteger.Parse( "44941850342971410491504784020845001571447288814329989"));
	static BigNumber pi               = new(BigInteger.Parse( "51940525591582574509458869567679175027653994297991570"));
	static BigNumber tau              = new(BigInteger.Parse("103881051183165149018917739135358350055307988595983140"));
	static BigNumber nan              = new(double.NaN);
	static BigNumber positiveInfinity = new(double.PositiveInfinity);
	static BigNumber negativeInfinity = new(double.NegativeInfinity);
	
	static BigInteger float64ThresholdBigInt = BigInteger.Parse("7002075272792542790512015926651");
	static BigNumber  float64ThresholdBigNum = new(float64ThresholdBigInt);
	static double     float64Threshold       = 4.235164736271502e-22;
	static double     nonFloatIndicator      = BitConverter.Int64BitsToDouble(0);
	static long       doubleUnit             = 9007199254740992;
	static BigInteger doubleToBigIntUnit     = BigInteger.Parse("1835552020310928337275981903076171875");
	
	public static BigNumber One         => one;
	public static BigNumber NegativeOne => negativeOne;
	public static int       Radix       => 2;
	public static BigNumber Zero        => zero;
	public static BigNumber E           => e;
	public static BigNumber Pi          => pi;
	public static BigNumber Tau         => tau;
	
	public static BigNumber AdditiveIdentity       => zero;
	public static BigNumber MultiplicativeIdentity => one;
	
	public static BigNumber MaxValue => positiveInfinity;
	public static BigNumber MinValue => negativeInfinity;
	
	public static BigNumber NaN              => nan;
	public static BigNumber PositiveInfinity => positiveInfinity;
	public static BigNumber NegativeInfinity => negativeInfinity;
	
	// Fields
	readonly BigInteger value;
	readonly double     doubleValue = nonFloatIndicator;
	
	// Properties
	internal bool IsDouble => BitConverter.DoubleToInt64Bits(doubleValue) != 0;
	
	// Constructors
	public BigNumber() { }
	
	internal BigNumber(BigInteger value) {
		this.value = value;
	}
	
	internal BigNumber(double doubleValue) {
		this.doubleValue = doubleValue;
	}
	
	// Compare
	public int CompareTo(object? obj) {
		throw new NotImplementedException();
	}
	
	public int CompareTo(double other) {
		if (IsDouble) {
			return doubleValue.CompareTo(other);
		}
		else if (double.IsNaN(other)) {
			return 1;
		}
		else if (value >= 0 && double.IsNegative(other)) {
			if (value == 0 && other == 0) { // Account for negative zero
				return 0;
			}
			else {
				return 1;
			}
		}
		else if (value < 0 && double.IsPositive(other)) {
			return -1;
		}
		else if (double.IsInfinity(other)) {
			return value >= 0 ? -1 : 1;
		}
		else if (value >= 0) { // Both positive
			return 0; // TODO
		}
		else { // Both negative
			return 0; // TODO
		}
	}
	
	public int CompareTo(BigNumber other) {
		if (IsDouble && other.IsDouble) {
			return doubleValue.CompareTo(other.doubleValue);
		}
		else if (IsNaN(this)) {
			return -1;
		}
		else if (IsNaN(other)) {
			return 1;
		}
		else if (IsPositiveInfinity(this)) {
			return 1;
		}
		else if (IsPositiveInfinity(other)) {
			return -1;
		}
		else if (IsNegativeInfinity(this)) {
			return -1;
		}
		else if (IsNegativeInfinity(other)) {
			return 1;
		}
		else if (IsPositive(this) && IsNegative(other)) {
			return 1;
		}
		if (IsNegative(this) && IsPositive(other)) {
			return -1;
		}
		else if (IsDouble && !other.IsDouble) {
			if (IsPositive(this) && IsPositive(other)) {
				return IsZero(other) ? 1 : -1;
			}
			else {
				return IsZero(other) ? -1 : 1;
			}
		}
		else if (!IsDouble && other.IsDouble) {
			// Special double value assumed to be subnormal here
			if (IsPositive(this) && IsPositive(other)) {
				return IsZero(this) ? -1 : 1;
			}
			else {
				return IsZero(this) ? 1 : -1;
			}
		}
		else {
			return value.CompareTo(other.value);
		}
	}
	
	// ???
	public TypeCode GetTypeCode() {
		return Type.GetTypeCode(typeof(BigNumber));
	}
	
	// Conversions (to)
	public bool ToBoolean(IFormatProvider? provider) {
		throw new NotImplementedException();
	}
	
	public char ToChar(IFormatProvider? provider) {
		throw new NotImplementedException();
	}
	
	public byte ToByte(IFormatProvider? provider) {
		throw new NotImplementedException();
	}
	
	public sbyte ToSByte(IFormatProvider? provider) {
		throw new NotImplementedException();
	}
	
	public ushort ToUInt16(IFormatProvider? provider) {
		throw new NotImplementedException();
	}
	
	public short ToInt16(IFormatProvider? provider) {
		throw new NotImplementedException();
	}
	
	public uint ToUInt32(IFormatProvider? provider) {
		throw new NotImplementedException();
	}
	
	public int ToInt32(IFormatProvider? provider) {
		throw new NotImplementedException();
	}
	
	public ulong ToUInt64(IFormatProvider? provider) {
		throw new NotImplementedException();
	}
	
	public long ToInt64(IFormatProvider? provider) {
		throw new NotImplementedException();
	}
	
	public float ToSingle(IFormatProvider? provider) {
		throw new NotImplementedException();
	}
	
	public double ToDouble(IFormatProvider? provider) {
		if (IsDouble) {
			return doubleValue;
		}
		else {
			return fixedToDouble(value);
		}
	}
	
	public decimal ToDecimal(IFormatProvider? provider) {
		throw new NotImplementedException();
	}
	
	public string ToString(IFormatProvider? provider) {
		throw new NotImplementedException();
	}
	
	public DateTime ToDateTime(IFormatProvider? provider) {
		throw new NotImplementedException();
	}
	
	public object ToType(Type conversionType, IFormatProvider? provider) {
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
		if (IsDouble && other.IsDouble) {
			return doubleValue.Equals(other.doubleValue);
		}
		else if (IsNaN(this) || IsNaN(other)) {
			return false;
		}
		else {
			return CompareTo(other) != 0;
		}
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
		return value;
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
		if (value.IsDouble) {
			return new(double.Abs(value.doubleValue));
		}
		else {
			return new(BigInteger.Abs(value.value));
		}
	}
	
	public static bool IsCanonical(BigNumber value) {
		return !value.IsDouble || isCanonical(value.doubleValue);
	}
	
	public static bool IsFinite(BigNumber value) {
		return !value.IsDouble || double.IsFinite(value.doubleValue);
	}
	
	public static bool IsInteger(BigNumber value) {
		return !value.IsDouble && value == value.truncate();
	}
	
	public static bool IsEvenInteger(BigNumber value) {
		return IsInteger(value) /*&& value % (BigNumber) 2 == 0*/;
	}
	
	public static bool IsOddInteger(BigNumber value) {
		return IsInteger(value) /*&& value % (BigNumber) 2 != 0*/;
	}
	
	public static bool IsPositive(BigNumber value) {
		return !value.IsDouble && value.value >= 0 || value.IsDouble && double.IsPositive(value.doubleValue);
	}
	
	public static bool IsNegative(BigNumber value) {
		return !value.IsDouble && value.value < 0 || value.IsDouble && double.IsNegative(value.doubleValue);
	}
	
	public static bool IsNormal(BigNumber value) {
		return !value.IsDouble || double.IsNormal(value.doubleValue);
	}
	
	public static bool IsSubnormal(BigNumber value) {
		return value.IsDouble && double.IsSubnormal(value.doubleValue);
	}
	
	public static bool IsInfinity(BigNumber value) {
		return value.IsDouble && double.IsInfinity(value.doubleValue);
	}
	
	public static bool IsPositiveInfinity(BigNumber value) {
		return value.IsDouble && double.IsPositiveInfinity(value.doubleValue);
	}
	
	public static bool IsNegativeInfinity(BigNumber value) {
		return value.IsDouble && double.IsNegativeInfinity(value.doubleValue);
	}
	
	public static bool IsNaN(BigNumber value) {
		return value.IsDouble && double.IsNaN(value.doubleValue);
	}
	
	public static bool IsRealNumber(BigNumber value) {
		return !value.IsDouble || double.IsRealNumber(value.doubleValue);
	}
	
	public static bool IsImaginaryNumber(BigNumber value) {
		return false;
	}
	
	public static bool IsComplexNumber(BigNumber value) {
		return false;
	}
	
	public static bool IsZero(BigNumber value) {
		return !value.IsDouble && value.value == 0;
	}
	
	public static BigNumber MaxMagnitude(BigNumber x, BigNumber y) {
		if (x.IsDouble && y.IsDouble) {
			return new(double.MaxMagnitude(x.doubleValue, y.doubleValue));
		}
		else if (IsNaN(x)) {
			return x;
		}
		else if (IsNaN(y)) {
			return y;
		}
		else if (IsInfinity(y)) {
			return y;
		}
		else if (IsInfinity(x)) {
			return x;
		}
		else if (x.IsDouble && !y.IsDouble) {
			return IsZero(y) ? x : y;
		}
		else if (!x.IsDouble && y.IsDouble) {
			return IsZero(x) ? y : x;
		}
		else {
			return new(BigInteger.MaxMagnitude(x.value, y.value));
		}
	}
	
	public static BigNumber MaxMagnitudeNumber(BigNumber x, BigNumber y) {
		if (x.IsDouble && y.IsDouble) {
			return new(double.MaxMagnitudeNumber(x.doubleValue, y.doubleValue));
		}
		else if (IsNaN(x)) {
			return y;
		}
		else if (IsNaN(y)) {
			return x;
		}
		else if (IsInfinity(y)) {
			return y;
		}
		else if (IsInfinity(x)) {
			return x;
		}
		else if (x.IsDouble && !y.IsDouble) {
			return IsZero(y) ? x : y;
		}
		else if (!x.IsDouble && y.IsDouble) {
			return IsZero(x) ? y : x;
		}
		else {
			return new(BigInteger.MaxMagnitude(x.value, y.value));
		}
	}
	
	public static BigNumber MinMagnitude(BigNumber x, BigNumber y) {
		if (x.IsDouble && y.IsDouble) {
			return new(double.MinMagnitude(x.doubleValue, y.doubleValue));
		}
		else if (IsNaN(x)) {
			return x;
		}
		else if (IsNaN(y)) {
			return y;
		}
		else if (IsInfinity(y)) {
			return x;
		}
		else if (IsInfinity(x)) {
			return y;
		}
		else if (x.IsDouble && !y.IsDouble) {
			return IsZero(y) ? y : x;
		}
		else if (!x.IsDouble && y.IsDouble) {
			return IsZero(x) ? x : y;
		}
		else {
			return new(BigInteger.MinMagnitude(x.value, y.value));
		}
	}
	
	public static BigNumber MinMagnitudeNumber(BigNumber x, BigNumber y) {
		if (x.IsDouble && y.IsDouble) {
			return new(double.MinMagnitudeNumber(x.doubleValue, y.doubleValue));
		}
		else if (IsNaN(x)) {
			return y;
		}
		else if (IsNaN(y)) {
			return x;
		}
		else if (IsInfinity(y)) {
			return x;
		}
		else if (IsInfinity(x)) {
			return y;
		}
		else if (x.IsDouble && !y.IsDouble) {
			return IsZero(y) ? y : x;
		}
		else if (!x.IsDouble && y.IsDouble) {
			return IsZero(x) ? x : y;
		}
		else {
			return new(BigInteger.MinMagnitude(x.value, y.value));
		}
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
		if (IsDouble) {
			return new((BigInteger) (long) doubleValue * UnitPrecision);
		}
		else {
			return new(value / UnitPrecision * UnitPrecision);
		}
	}
	
	BigNumber roundAwayFromZero() {
		if (IsDouble) {
			var truncated = (long) doubleValue;
			
			if (truncated == doubleValue) {
				return new((BigInteger) truncated * UnitPrecision);
			}
			else if (double.IsPositive(doubleValue)) {
				return new(((BigInteger) truncated + 1) * UnitPrecision);
			}
			else {
				return new(((BigInteger) truncated - 1) * UnitPrecision);
			}
		}
		else {
			var truncated = value / UnitPrecision * UnitPrecision;
			
			if (value == truncated) {
				return this;
			}
			else if (value >= 0) {
				return new(truncated + UnitPrecision);
			}
			else {
				return new(truncated - UnitPrecision);
			}
		}
	}
	
	static bool isCanonical<T>(T value) where T: INumber<T> {
		return T.IsCanonical(value);
	}
	
	static void normalize(ref BigNumber n) {
		if (n.IsDouble && !IsNaN(n) && !IsInfinity(n)) {
			if (!IsSubnormal(n) && n.doubleValue is >= 6.048442170748684e-53 or <= -6.048442170748684e-53) {
				
			}
		}
	}
	
	static (object, object) upcastPair(BigInteger b, double d) {
		if (tryDoubleToFixed(d, out var result)) {
			return (b, result);
		}
		else {
			return (fixedToDouble(b), d);
		}
	}
	
	static bool tryDoubleToFixed(double d, out BigInteger result) {
		result = 0;
		
		if (double.IsNaN(d) || double.IsInfinity(d) || double.IsSubnormal(d) || Math.Abs(d) < float64Threshold) {
			return false;
		}
		else {
			result = doubleToFixed(d);
			return true;
		}
	}
	
	static BigInteger doubleToFixed(double d) {
		// Assumes non-NaN, non-Infinity, and non-Zero
		var sign     = d.GetSignBit();
		var exponent = d.GetExponentBits();
		var fraction = d.GetFractionBits();
		
		BigInteger fixedPoint = fraction;
		
		if (exponent == 0) { // Handle subnormals
			fixedPoint *=  UnitPrecision; // Multiply by fixed point "one"
			fixedPoint >>= 1022;
		}
		else { // Handle normals
			var trueExponent = exponent - 1023;
			
			fixedPoint |= 0x10_0000_0000_0000; // Include mantissa
			fixedPoint *= UnitPrecision;       // Multiply by fixed point "one"
			
			if (trueExponent > 0) {
				fixedPoint <<= trueExponent;
			}
			else if (trueExponent < 0) {
				fixedPoint >>= trueExponent;
			}
		}
			
		fixedPoint >>= 52; // Adjust for fraction bits
		if (sign) {
			fixedPoint *= -1;
		}
		
		return fixedPoint;
	}
	
	static double fixedToDouble(BigInteger b) {
		// In this case, I think it'd be easier to leverage the built-in int->double conversion with the integer and fractional parts separately
		var sign = false;
		
		if (b < 0) {
			b *= -1;
			sign = true;
		}
		
		var trueIntTrunc = b / UnitPrecision; // Integer portion
		var fractional   = b % UnitPrecision;
		
		fractional /= doubleToBigIntUnit;
		
		var trueFractional = (double) trueIntTrunc + (double) fractional / doubleUnit;
		return trueFractional;
	}
}