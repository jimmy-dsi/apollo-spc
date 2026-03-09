using System.Diagnostics;

namespace Jimbl;

using System.Numerics;
using System.Globalization;

using  Range = Jimbl.DataStructs.Range;
using SRange = System.Range;

public static class Exts {
	public static bool Truthy(this object? obj) {
		if (obj is bool b) {
			return b;
		}
		else if (obj is null) {
			return false;
		}
		else if (obj is Exception) {
			return false;
		}
		else {
			return true;
		}
	}
	
	public static bool Truthy<T>(this object? obj, out T successVal) {
		var result = Truthy(obj);
		if (result) {
			successVal = (T) obj!;
		}
		else {
			successVal = default(T);
		}
		return result;
	}
	
	public static bool Falsy(this object? obj) => !Truthy(obj);
	
	public static bool Falsy(this object? obj, out Exception ex) {
		var result = Falsy(obj);
		if (result) {
			ex = (Exception) (obj ?? new NullReferenceException());
		}
		else {
			ex = default;
		}
		return result;
	}
	
	public class Null { }
	
	public static Type Type(this object? obj) {
		if (obj is Union un) {
			return un.GetType();
		}
		else {
			return obj?.GetType() ?? typeof(Null);
		}
	}
	
	/// <summary>
	/// Checks whether an object of a built-in C# type can be converted to another built-in C# type.
	/// Does not work for user-defined conversions.
	/// </summary>
	public static bool ConvertsTo(this object obj, Type targetType) {
		return Try.Catch(() => {
			_ = Convert.ChangeType(obj, targetType);
			return true;
		},
		(InvalidCastException _) => false);
	}
	
	// Range/Index
	public static int Normalize(this Index index, int length) {
		if (index.IsFromEnd) {
			return length - index.Value;
		}
		else {
			return index.Value;
		}
	}
	
	public static Range Iter(this SRange range, int step = 1) {
		if (range.Start.IsFromEnd || range.End.IsFromEnd) {
			throw new ArgumentException("Ranges with indexes specified from end cannot be enumerated");
		}
		
		return new(range.Start.Value, range.End.Value, step);
	}
	
	// Enumerable Extension Methods
	public static IEnumerable<(int, T)> Enum<T>(this IEnumerable<T> iterable) {
		var index = 0;
		foreach (var item in iterable) {
			yield return (index, item);
			index++;
		}
	}
	
	// Integer Extension Methods
	public static byte   SafeUnsigned(this sbyte value) => (byte)   sbyte.Clamp(value, 0, sbyte.MaxValue);
	public static UInt16 SafeUnsigned(this Int16 value) => (UInt16) Int16.Clamp(value, 0, Int16.MaxValue);
	public static UInt32 SafeUnsigned(this Int32 value) => (UInt32) Int32.Clamp(value, 0, Int32.MaxValue);
	public static UInt64 SafeUnsigned(this Int64 value) => (UInt64) Int64.Clamp(value, 0, Int64.MaxValue);
	
	public static sbyte SafeSigned(this byte   value) => value > sbyte.MaxValue ? sbyte.MaxValue : (sbyte) value;
	public static Int16 SafeSigned(this UInt16 value) => value > Int16.MaxValue ? Int16.MaxValue : (Int16) value;
	public static Int32 SafeSigned(this UInt32 value) => value > Int32.MaxValue ? Int32.MaxValue : (Int32) value;
	public static Int64 SafeSigned(this UInt64 value) => value > Int64.MaxValue ? Int64.MaxValue : (Int64) value;
	
	public static bool GetBit(this byte v, int b) {
		return (v & (1 << b)) != 0;
	}
	
	public static void SetBit(this ref byte v, int b, bool value) {
		if (value) {
			v |= (byte) (1 << b);
		}
		else {
			v &= (byte) ~(1 << b);
		}
	}
	
	public static bool GetBit<T>(this T v, int b) where T: IBinaryInteger<T> {
		return (v.ToUInt64() & (1uL << b)) != 0;
	}
	
	public static void SetBit<T>(this ref T v, int b, bool value) where T: struct, IBinaryInteger<T> {
		if (value) {
			v = (T)(object) (v.ToUInt64() | (1uL << b));
		}
		else {
			v = (T)(object) (v.ToUInt64() & ~(1uL << b));
		}
	}
	
	// Char Extension Methods
	public static UnicodeCategory GetUnicodeCategory(this char c) => char.GetUnicodeCategory(c);
	
	public static bool IsAscii(this char c)                       => char.IsAscii(c);
	public static bool IsAsciiDigit(this char c)                  => char.IsAsciiDigit(c);
	public static bool IsAsciiHexDigit(this char c)               => char.IsAsciiHexDigit(c);
	public static bool IsAsciiHexDigitLower(this char c)          => char.IsAsciiHexDigitLower(c);
	public static bool IsAsciiHexDigitUpper(this char c)          => char.IsAsciiHexDigitUpper(c);
	public static bool IsAsciiLetter(this char c)                 => char.IsAsciiLetter(c);
	public static bool IsAsciiLetterLower(this char c)            => char.IsAsciiLetterLower(c);
	public static bool IsAsciiLetterOrDigit(this char c)          => char.IsAsciiLetterOrDigit(c);
	public static bool IsAsciiLetterUpper(this char c)            => char.IsAsciiLetterUpper(c);
	public static bool IsBetween(this char c, char min, char max) => char.IsBetween(c, min, max);
	public static bool IsControl(this char c)                     => char.IsControl(c);
	public static bool IsDigit(this char c)                       => char.IsDigit(c);
	public static bool IsHighSurrogate(this char c)               => char.IsHighSurrogate(c);
	public static bool IsLetter(this char c)                      => char.IsLetter(c);
	public static bool IsLetterOrDigit(this char c)               => char.IsLetterOrDigit(c);
	public static bool IsLower(this char c)                       => char.IsLower(c);
	public static bool IsLowSurrogate(this char c)                => char.IsLowSurrogate(c);
	public static bool IsNumber(this char c)                      => char.IsNumber(c);
	public static bool IsPunctuation(this char c)                 => char.IsPunctuation(c);
	public static bool IsSeparator(this char c)                   => char.IsSeparator(c);
	public static bool IsSurrogate(this char c)                   => char.IsSurrogate(c);
	public static bool IsSymbol(this char c)                      => char.IsSymbol(c);
	public static bool IsUpper(this char c)                       => char.IsUpper(c);
	public static bool IsWhiteSpace(this char c)                  => char.IsWhiteSpace(c);
	
	public static char ToLower(this char c) => char.ToLower(c);
	public static char ToUpper(this char c) => char.ToUpper(c);
	
	public static UInt64 GetFractionBits(this double d) {
		var allBits = BitConverter.DoubleToUInt64Bits(d);
		return allBits & 0xF_FFFF_FFFF_FFFF; // Lower 52 bits
	}
	
	public static UInt16 GetExponentBits(this double d) {
		var allBits = BitConverter.DoubleToUInt64Bits(d);
		return (UInt16) (allBits >> 52 & 0x7_FF);
	}
	
	public static bool GetSignBit(this double d) {
		var allBits = BitConverter.DoubleToUInt64Bits(d);
		return allBits >> 63 != 0;
	}
	
	public static bool IsNumeric(this Type type) {
		return type.FitsInt32()   || type.FitsUInt32()
		    || type.FitsInt64()   || type.FitsUInt64()
		    || type.FitsInt128()  || type.FitsUInt128()
		    || type.FitsBigInteger()
		    || type.FitsFloat32() || type.FitsFloat64()
		    || type.FitsDecimal() || type.FitsComplex();
	}
	
	public static bool FitsInt32(this Type type) {
		return type == typeof(char)
		    || type == typeof(byte)
		    || type == typeof(sbyte)
		    || type == typeof(UInt16)
		    || type == typeof(Int16)
		    || type == typeof(Int32);
	}
	
	public static bool FitsUInt32(this Type type) {
		return type == typeof(char)
		    || type == typeof(byte)
		    || type == typeof(UInt16)
		    || type == typeof(UInt32);
	}
	
	public static bool FitsInt64(this Type type) {
		return type.FitsInt32()
		    || type == typeof(UInt32)
		    || type == typeof(Int64);
	}
	
	public static bool FitsUInt64(this Type type) {
		return type.FitsUInt32()
		    || type == typeof(UInt64);
	}
	
	public static bool FitsInt128(this Type type) {
		return type.FitsInt64()
		    || type == typeof(UInt64)
		    || type == typeof(Int128);
	}
	
	public static bool FitsUInt128(this Type type) {
		return type.FitsUInt64()
		    || type == typeof(UInt128);
	}
	
	public static bool FitsBigInteger(this Type type) {
		return type.FitsInt128()
		    || type == typeof(UInt128)
		    || type == typeof(BigInteger);
	}
	
	public static bool FitsFloat32(this Type type) {
		return type == typeof(byte)
		    || type == typeof(sbyte)
		    || type == typeof(UInt16)
		    || type == typeof(Int16)
		    || type == typeof(float);
	}
	
	public static bool FitsFloat64(this Type type) {
		return type.FitsInt32()
		    || type.FitsFloat32()
		    || type == typeof(double);
	}
	
	public static bool FitsDecimal(this Type type) {
		return type.FitsInt64()
		    || type == typeof(UInt64)
		    || type == typeof(decimal);
	}
	
	public static bool FitsComplex(this Type type) {
		return type.FitsFloat64()
		    || type == typeof(Complex);
	}
	
	static HashSet<Type> nativeNumTypes = new() {
		typeof(byte),   typeof(sbyte),  typeof(UInt16), typeof(Int16),
		typeof(UInt32), typeof(Int32),  typeof(UInt64), typeof(Int64),
		typeof(nuint),  typeof(nint),   typeof(char),
		typeof(float),  typeof(double), typeof(decimal)
	};
	
	public static T UnboxCast<T>(this object obj) {
		if (nativeNumTypes.Contains(typeof(T))) {
			return obj.toNumType<T>();
		}
		else {
			// TODO: Handle user-defined conversions
			return (T) obj;
		}
	}
	
	public static    byte    ToByte(this object obj) =>    toByte(obj);
	public static   sbyte   ToSByte(this object obj) =>   toSByte(obj);
	public static  UInt16  ToUInt16(this object obj) =>  toUInt16(obj);
	public static   Int16   ToInt16(this object obj) =>   toInt16(obj);
	public static  UInt32  ToUInt32(this object obj) =>  toUInt32(obj);
	public static   Int32   ToInt32(this object obj) =>   toInt32(obj);
	public static  UInt64  ToUInt64(this object obj) =>  toUInt64(obj);
	public static   Int64   ToInt64(this object obj) =>   toInt64(obj);
	public static   nuint   ToNUInt(this object obj) =>   toNUInt(obj);
	public static    nint    ToNInt(this object obj) =>    toNInt(obj);
	public static    char    ToChar(this object obj) =>    toChar(obj);
	public static   float   ToFloat(this object obj) =>   toFloat(obj);
	public static  double  ToDouble(this object obj) =>  toDouble(obj);
	public static decimal ToDecimal(this object obj) => toDecimal(obj);
	
	static T toNumType<T>(this object obj) {
		T dummy = default(T);
		
		object result = dummy switch {
			   byte =>    toByte(obj),
			  sbyte =>   toSByte(obj),
			 UInt16 =>  toUInt16(obj),
			  Int16 =>   toInt16(obj),
			 UInt32 =>  toUInt32(obj),
			  Int32 =>   toInt32(obj),
			 UInt64 =>  toUInt64(obj),
			  Int64 =>   toInt64(obj),
			  nuint =>   toNUInt(obj),
			   nint =>    toNInt(obj),
			   char =>    toChar(obj),
			  float =>   toFloat(obj),
			 double =>  toDouble(obj),
			decimal => toDecimal(obj),
			_ => throw new UnreachableException()
		};
		
		return (T) result;
	}
	
	static byte toByte(object obj) {
		switch (obj) {
			case    byte n: return (byte) n;  case   sbyte n: return (byte) n;
			case  UInt16 n: return (byte) n;  case   Int16 n: return (byte) n;
			case  UInt32 n: return (byte) n;  case   Int32 n: return (byte) n;
			case  UInt64 n: return (byte) n;  case   Int64 n: return (byte) n;
			case   nuint n: return (byte) n;  case    nint n: return (byte) n;
			case    char n: return (byte) n;  case   float n: return (byte) n;
			case  double n: return (byte) n;  case decimal n: return (byte) n;
		}
		
		return (byte) obj;
	}
	
	static sbyte toSByte(object obj) {
		switch (obj) {
			case    byte n: return (sbyte) n;  case   sbyte n: return (sbyte) n;
			case  UInt16 n: return (sbyte) n;  case   Int16 n: return (sbyte) n;
			case  UInt32 n: return (sbyte) n;  case   Int32 n: return (sbyte) n;
			case  UInt64 n: return (sbyte) n;  case   Int64 n: return (sbyte) n;
			case   nuint n: return (sbyte) n;  case    nint n: return (sbyte) n;
			case    char n: return (sbyte) n;  case   float n: return (sbyte) n;
			case  double n: return (sbyte) n;  case decimal n: return (sbyte) n;
		}
		
		return (sbyte) obj;
	}
	
	static UInt16 toUInt16(object obj) {
		switch (obj) {
			case    byte n: return (UInt16) n;  case   sbyte n: return (UInt16) n;
			case  UInt16 n: return (UInt16) n;  case   Int16 n: return (UInt16) n;
			case  UInt32 n: return (UInt16) n;  case   Int32 n: return (UInt16) n;
			case  UInt64 n: return (UInt16) n;  case   Int64 n: return (UInt16) n;
			case   nuint n: return (UInt16) n;  case    nint n: return (UInt16) n;
			case    char n: return (UInt16) n;  case   float n: return (UInt16) n;
			case  double n: return (UInt16) n;  case decimal n: return (UInt16) n;
		}
		
		return (UInt16) obj;
	}
	
	static Int16 toInt16(object obj) {
		switch (obj) {
			case    byte n: return (Int16) n;  case   sbyte n: return (Int16) n;
			case  UInt16 n: return (Int16) n;  case   Int16 n: return (Int16) n;
			case  UInt32 n: return (Int16) n;  case   Int32 n: return (Int16) n;
			case  UInt64 n: return (Int16) n;  case   Int64 n: return (Int16) n;
			case   nuint n: return (Int16) n;  case    nint n: return (Int16) n;
			case    char n: return (Int16) n;  case   float n: return (Int16) n;
			case  double n: return (Int16) n;  case decimal n: return (Int16) n;
		}
		
		return (Int16) obj;
	}
	
	static UInt32 toUInt32(object obj) {
		switch (obj) {
			case    byte n: return (UInt32) n;  case   sbyte n: return (UInt32) n;
			case  UInt16 n: return (UInt32) n;  case   Int16 n: return (UInt32) n;
			case  UInt32 n: return (UInt32) n;  case   Int32 n: return (UInt32) n;
			case  UInt64 n: return (UInt32) n;  case   Int64 n: return (UInt32) n;
			case   nuint n: return (UInt32) n;  case    nint n: return (UInt32) n;
			case    char n: return (UInt32) n;  case   float n: return (UInt32) n;
			case  double n: return (UInt32) n;  case decimal n: return (UInt32) n;
		}
		
		return (UInt32) obj;
	}
	
	static Int32 toInt32(object obj) {
		switch (obj) {
			case    byte n: return (Int32) n;  case   sbyte n: return (Int32) n;
			case  UInt16 n: return (Int32) n;  case   Int16 n: return (Int32) n;
			case  UInt32 n: return (Int32) n;  case   Int32 n: return (Int32) n;
			case  UInt64 n: return (Int32) n;  case   Int64 n: return (Int32) n;
			case   nuint n: return (Int32) n;  case    nint n: return (Int32) n;
			case    char n: return (Int32) n;  case   float n: return (Int32) n;
			case  double n: return (Int32) n;  case decimal n: return (Int32) n;
		}
		
		return (Int32) obj;
	}
	
	static UInt64 toUInt64(object obj) {
		switch (obj) {
			case    byte n: return (UInt64) n;  case   sbyte n: return (UInt64) n;
			case  UInt16 n: return (UInt64) n;  case   Int16 n: return (UInt64) n;
			case  UInt32 n: return (UInt64) n;  case   Int32 n: return (UInt64) n;
			case  UInt64 n: return (UInt64) n;  case   Int64 n: return (UInt64) n;
			case   nuint n: return (UInt64) n;  case    nint n: return (UInt64) n;
			case    char n: return (UInt64) n;  case   float n: return (UInt64) n;
			case  double n: return (UInt64) n;  case decimal n: return (UInt64) n;
		}
		
		return (UInt64) obj;
	}
	
	static Int64 toInt64(object obj) {
		switch (obj) {
			case    byte n: return (Int64) n;  case   sbyte n: return (Int64) n;
			case  UInt16 n: return (Int64) n;  case   Int16 n: return (Int64) n;
			case  UInt32 n: return (Int64) n;  case   Int32 n: return (Int64) n;
			case  UInt64 n: return (Int64) n;  case   Int64 n: return (Int64) n;
			case   nuint n: return (Int64) n;  case    nint n: return (Int64) n;
			case    char n: return (Int64) n;  case   float n: return (Int64) n;
			case  double n: return (Int64) n;  case decimal n: return (Int64) n;
		}
		
		return (Int64) obj;
	}
	
	static nuint toNUInt(object obj) {
		switch (obj) {
			case    byte n: return (nuint) n;  case   sbyte n: return (nuint) n;
			case  UInt16 n: return (nuint) n;  case   Int16 n: return (nuint) n;
			case  UInt32 n: return (nuint) n;  case   Int32 n: return (nuint) n;
			case  UInt64 n: return (nuint) n;  case   Int64 n: return (nuint) n;
			case   nuint n: return (nuint) n;  case    nint n: return (nuint) n;
			case    char n: return (nuint) n;  case   float n: return (nuint) n;
			case  double n: return (nuint) n;  case decimal n: return (nuint) n;
		}
		
		return (nuint) obj;
	}
	
	static nint toNInt(object obj) {
		switch (obj) {
			case    byte n: return (nint) n;  case   sbyte n: return (nint) n;
			case  UInt16 n: return (nint) n;  case   Int16 n: return (nint) n;
			case  UInt32 n: return (nint) n;  case   Int32 n: return (nint) n;
			case  UInt64 n: return (nint) n;  case   Int64 n: return (nint) n;
			case   nuint n: return (nint) n;  case    nint n: return (nint) n;
			case    char n: return (nint) n;  case   float n: return (nint) n;
			case  double n: return (nint) n;  case decimal n: return (nint) n;
		}
		
		return (nint) obj;
	}
	
	static char toChar(object obj) {
		switch (obj) {
			case    byte n: return (char) n;  case   sbyte n: return (char) n;
			case  UInt16 n: return (char) n;  case   Int16 n: return (char) n;
			case  UInt32 n: return (char) n;  case   Int32 n: return (char) n;
			case  UInt64 n: return (char) n;  case   Int64 n: return (char) n;
			case   nuint n: return (char) n;  case    nint n: return (char) n;
			case    char n: return (char) n;  case   float n: return (char) n;
			case  double n: return (char) n;  case decimal n: return (char) n;
		}
		
		return (char) obj;
	}
	
	static float toFloat(object obj) {
		switch (obj) {
			case    byte n: return (float) n;  case   sbyte n: return (float) n;
			case  UInt16 n: return (float) n;  case   Int16 n: return (float) n;
			case  UInt32 n: return (float) n;  case   Int32 n: return (float) n;
			case  UInt64 n: return (float) n;  case   Int64 n: return (float) n;
			case   nuint n: return (float) n;  case    nint n: return (float) n;
			case    char n: return (float) n;  case   float n: return (float) n;
			case  double n: return (float) n;  case decimal n: return (float) n;
		}
		
		return (float) obj;
	}
	
	static double toDouble(object obj) {
		switch (obj) {
			case    byte n: return (double) n;  case   sbyte n: return (double) n;
			case  UInt16 n: return (double) n;  case   Int16 n: return (double) n;
			case  UInt32 n: return (double) n;  case   Int32 n: return (double) n;
			case  UInt64 n: return (double) n;  case   Int64 n: return (double) n;
			case   nuint n: return (double) n;  case    nint n: return (double) n;
			case    char n: return (double) n;  case   float n: return (double) n;
			case  double n: return (double) n;  case decimal n: return (double) n;
		}
		
		return (double) obj;
	}
	
	static decimal toDecimal(object obj) {
		switch (obj) {
			case    byte n: return (decimal) n;  case   sbyte n: return (decimal) n;
			case  UInt16 n: return (decimal) n;  case   Int16 n: return (decimal) n;
			case  UInt32 n: return (decimal) n;  case   Int32 n: return (decimal) n;
			case  UInt64 n: return (decimal) n;  case   Int64 n: return (decimal) n;
			case   nuint n: return (decimal) n;  case    nint n: return (decimal) n;
			case    char n: return (decimal) n;  case   float n: return (decimal) n;
			case  double n: return (decimal) n;  case decimal n: return (decimal) n;
		}
		
		return (decimal) obj;
	}
}