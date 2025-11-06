namespace Jimbl;

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
	
	public static Range Enum(this SRange range, int step = 1) {
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
}