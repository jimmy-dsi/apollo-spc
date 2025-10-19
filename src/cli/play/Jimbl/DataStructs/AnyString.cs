namespace Jimbl.DataStructs;

using System.Diagnostics.CodeAnalysis;
using System.Collections;
using System.Text;

using SRange = System.Range;

public abstract class AnyString:
	IComparable,
	IEnumerable<AnyChar>,
	IComparable<AnyString?>,
	IEquatable<AnyString?>,
	ICloneable
{
	public static implicit operator AnyString (string     str) => new AnyString_string (str);
	//public static implicit operator AnyString (char[]     str) => new AnyString<char[]>(str);
	//public static implicit operator AnyString (List<char> str) => new AnyString<List<char>>(str);
	
	public abstract int Length { get; }
	
	public abstract AnyChar   this[int    index] { get; }
	public virtual  AnyString this[SRange range] {
		get {
			var start = range.Start.Normalize(Length);
			var end   = range.End  .Normalize(Length);
			
			return Substring(start, end - start);
		}
	}
	
	object ICloneable.Clone() => Clone();
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	
	// Abstracts - Must be overridden
	public abstract int                    CompareTo(AnyString? strB);
	public abstract bool                   Contains(AnyString value, StringComparison comparisonType);
	public abstract bool                   EndsWith(AnyString value, StringComparison comparisonType);
	public abstract StringRuneEnumerator   EnumerateRunes();
	public abstract bool                   Equals(AnyString? strB, StringComparison comparisonType);
	public abstract int                    GetHashCode(StringComparison comparisonType);
	public abstract int                    IndexOf(AnyString value, Int32 startIndex, Int32 count, StringComparison comparisonType);
	public abstract int                    IndexOfAny(AnyChar[] value, Int32 startIndex, Int32 count);
	public abstract AnyString              Insert(Int32 index, AnyString value);
	public abstract bool                   IsNormalized(NormalizationForm normalizationForm);
	public abstract int                    LastIndexOf(AnyString value, Int32 startIndex, Int32 count, StringComparison comparisonType);
	public abstract int                    LastIndexOfAny(AnyChar[] value, Int32 startIndex, Int32 count);
	public abstract AnyString              Normalize(NormalizationForm normalizationForm);
	public abstract AnyString              PadLeft(Int32 count, AnyChar value);
	public abstract AnyString              PadRight(Int32 count, AnyChar value);
	public abstract AnyString              Remove(Int32 startIndex, Int32 count);
	public abstract AnyString              Replace(AnyString oldValue, AnyString newValue, StringComparison comparisonType);
	public abstract AnyString              ReplaceLineEndings(AnyString value);
	public abstract IEnumerable<AnyString> Split(AnyString[] separator, Int32 count, StringSplitOptions options);
	public abstract bool                   StartsWith(AnyString value, StringComparison comparisonType);
	public abstract AnyString              Substring(Int32 startIndex, Int32 length);
	public abstract IEnumerable<AnyChar>   ToCharArray(Int32 startIndex, Int32 length);
	public abstract AnyString              ToLower();
	public abstract AnyString              ToUpper();
	public abstract AnyString              TrimEnd(params AnyChar[]? trimChars);
	public abstract AnyString              TrimStart(params AnyChar[]? trimChars);
	
	protected abstract IEnumerable Enumerable();
	
	// Virtuals - Optionally overrideable
	public virtual AnyString Clone() => this;
	
	public virtual int CompareTo(object? value) => CompareTo(value as AnyString);
	
	public virtual bool Contains(AnyChar   value, StringComparison comparisonType) => Contains(value.ToAnyString(), comparisonType);
	public virtual bool Contains(AnyChar   value)                                  => Contains(value.ToAnyString());
	public virtual bool Contains(AnyString value)                                  => Contains(value, StringComparison.Ordinal);
	
	public virtual bool EndsWith(AnyChar   value) => EndsWith(value.ToAnyString());
	public virtual bool EndsWith(AnyString value) => EndsWith(value, StringComparison.Ordinal);
	public virtual bool EndsWith(AnyString value, bool ignoreCase) =>
		EndsWith(value, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
	
	public override bool Equals(object? obj)     => Equals(obj as AnyString);
	public virtual  bool Equals(AnyString? strB) => Equals(strB, StringComparison.Ordinal);
	
	public IEnumerator<AnyChar> GetEnumerator() {
		foreach (var item in Enumerable()) {
			yield return (AnyChar) item;
		}
	}
	
	public override int GetHashCode() => GetHashCode(StringComparison.Ordinal);
	
	public virtual int IndexOf(AnyChar value) => IndexOf(value, 0, Length, StringComparison.Ordinal);
	public virtual int IndexOf(AnyChar value, Int32 startIndex) => IndexOf(value, startIndex, Length - startIndex, StringComparison.Ordinal);
	public virtual int IndexOf(AnyChar value, Int32 startIndex, Int32 count) => IndexOf(value, startIndex, count, StringComparison.Ordinal);
	public virtual int IndexOf(AnyChar value, Int32 startIndex, StringComparison comparisonType) =>
		IndexOf(value, startIndex, Length - startIndex, comparisonType);
	public virtual int IndexOf(AnyChar value, Int32 startIndex, Int32 count, StringComparison comparisonType) =>
		IndexOf(value.ToAnyString(), startIndex, count, comparisonType);
	
	public virtual int IndexOf(AnyString value) => IndexOf(value, 0, Length, StringComparison.Ordinal);
	public virtual int IndexOf(AnyString value, Int32 startIndex) => IndexOf(value, startIndex, Length - startIndex, StringComparison.Ordinal);
	public virtual int IndexOf(AnyString value, Int32 startIndex, Int32 count) => IndexOf(value, startIndex, count, StringComparison.Ordinal);
	public virtual int IndexOf(AnyString value, Int32 startIndex, StringComparison comparisonType) =>
		IndexOf(value, startIndex, Length - startIndex, comparisonType);
	
	public virtual int IndexOfAny(AnyChar[] value) => IndexOfAny(value, 0, Length);
	public virtual int IndexOfAny(AnyChar[] value, Int32 startIndex) => IndexOfAny(value, startIndex, Length - startIndex);
	
	public virtual bool IsNormalized() => IsNormalized(NormalizationForm.FormC);
	
	public virtual int LastIndexOf(AnyChar value) => LastIndexOf(value, 0, Length, StringComparison.Ordinal);
	public virtual int LastIndexOf(AnyChar value, Int32 startIndex) => LastIndexOf(value, startIndex, Length - startIndex, StringComparison.Ordinal);
	public virtual int LastIndexOf(AnyChar value, Int32 startIndex, Int32 count) => LastIndexOf(value, startIndex, count, StringComparison.Ordinal);
	public virtual int LastIndexOf(AnyChar value, Int32 startIndex, StringComparison comparisonType) =>
		LastIndexOf(value, startIndex, Length - startIndex, comparisonType);
	public virtual int LastIndexOf(AnyChar value, Int32 startIndex, Int32 count, StringComparison comparisonType) =>
		LastIndexOf(value.ToAnyString(), startIndex, count, comparisonType);
	
	public virtual int LastIndexOf(AnyString value) => LastIndexOf(value, 0, Length, StringComparison.Ordinal);
	public virtual int LastIndexOf(AnyString value, Int32 startIndex) => LastIndexOf(value, startIndex, Length - startIndex, StringComparison.Ordinal);
	public virtual int LastIndexOf(AnyString value, Int32 startIndex, Int32 count) => LastIndexOf(value, startIndex, count, StringComparison.Ordinal);
	public virtual int LastIndexOf(AnyString value, Int32 startIndex, StringComparison comparisonType) =>
		LastIndexOf(value, startIndex, Length - startIndex, comparisonType);
	
	public virtual int LastIndexOfAny(AnyChar[] value)                   => LastIndexOfAny(value, 0,          Length);
	public virtual int LastIndexOfAny(AnyChar[] value, Int32 startIndex) => LastIndexOfAny(value, startIndex, Length - startIndex);
	
	public virtual AnyString Normalize() => Normalize(NormalizationForm.FormC);
	
	public virtual AnyString PadLeft (Int32 count) => PadLeft(count, ' ');
	public virtual AnyString PadRight(Int32 count) => PadRight(count, ' ');
	
	public virtual AnyString Remove(Int32 startIndex) => Remove(startIndex, Length - startIndex);
	
	public virtual AnyString Replace(AnyChar   oldValue, AnyChar   newValue) => Replace(oldValue.ToAnyString(), newValue.ToAnyString());
	public virtual AnyString Replace(AnyString oldValue, AnyString newValue) => Replace(oldValue,               newValue, StringComparison.Ordinal);
	public virtual AnyString Replace(AnyString oldValue, AnyString newValue, bool ignoreCase) =>
		Replace(oldValue, newValue, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
	
	public virtual AnyString ReplaceLineEndings() => ReplaceLineEndings(Env.NewLine);
	
	public virtual IEnumerable<AnyString> Split(params AnyChar[] separator) {
		if (separator.Length == 0) {
			return Split(new AnyChar[] {' ', '\t', '\r', '\n'}, Int32.MaxValue, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		}
		else {
			return Split(separator, Int32.MaxValue, StringSplitOptions.None);
		}
	}
	
	public virtual IEnumerable<AnyString> Split(AnyChar separator, Int32 count) =>
		Split(separator, count, StringSplitOptions.None);
	public virtual IEnumerable<AnyString> Split(AnyChar separator, StringSplitOptions options) =>
		Split(separator, Int32.MaxValue, StringSplitOptions.None);
	public virtual IEnumerable<AnyString> Split(AnyChar separator, Int32 count, StringSplitOptions options) =>
		Split(new AnyChar[] {separator}, count, options);
	
	public virtual IEnumerable<AnyString> Split(AnyString separator) =>
		Split(separator, Int32.MaxValue, StringSplitOptions.None);
	public virtual IEnumerable<AnyString> Split(AnyString separator, Int32 count) =>
		Split(separator, count, StringSplitOptions.None);
	public virtual IEnumerable<AnyString> Split(AnyString separator, StringSplitOptions options) =>
		Split(separator, Int32.MaxValue, options);
	public virtual IEnumerable<AnyString> Split(AnyString separator, Int32 count, StringSplitOptions options) =>
		Split([separator], count, options);
	
	public virtual IEnumerable<AnyString> Split(AnyChar[] separator, Int32 count) =>
		Split(separator, count, StringSplitOptions.None);
	public virtual IEnumerable<AnyString> Split(AnyChar[] separator, StringSplitOptions options) =>
		Split(separator, Int32.MaxValue, options);
	public virtual IEnumerable<AnyString> Split(AnyChar[] separator, Int32 count, StringSplitOptions options) =>
		Split(separator.Select(x => x.ToAnyString()).ToArray(), count, options);
	
	public virtual IEnumerable<AnyString> Split(AnyString[] separator) =>
		Split(separator, Int32.MaxValue, StringSplitOptions.None);
	public virtual IEnumerable<AnyString> Split(AnyString[] separator, Int32 count) =>
		Split(separator, count, StringSplitOptions.None);
	public virtual IEnumerable<AnyString> Split(AnyString[] separator, StringSplitOptions options) =>
		Split(separator, Int32.MaxValue, options);
	
	public virtual bool StartsWith(AnyChar   value) => StartsWith(value.ToAnyString());
	public virtual bool StartsWith(AnyString value) => StartsWith(value, StringComparison.Ordinal);
	public virtual bool StartsWith(AnyString value, bool ignoreCase) =>
		StartsWith(value, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
	
	public virtual AnyString Substring(Int32 startIndex) => Substring(startIndex, Length - startIndex);
	
	public virtual IEnumerable<AnyChar> ToCharArray() => ToCharArray(0, Length);
	
	public virtual AnyString Trim(params AnyChar[]? trimChars) => TrimStart(trimChars).TrimEnd(trimChars);
	
	[return: NotNull]
	public override string ToString() => throw new NotImplementedException();
}

class AnyString_string: AnyString {
	string innerValue;
	
	public override int Length => innerValue.Length;
	
	public AnyString_string(string value) {
		innerValue = value;
	}
	
	public override AnyChar this[int index] {
		get => innerValue[index];
	}
	
	public override int CompareTo(AnyString? strB) =>
		innerValue.CompareTo(strB);
	public override bool Contains(AnyString value, StringComparison comparisonType) =>
		innerValue.Contains(value.ToString(), comparisonType);
	public override bool EndsWith(AnyString value, StringComparison comparisonType) =>
		innerValue.EndsWith(value.ToString(), comparisonType);
	public override StringRuneEnumerator EnumerateRunes() =>
		innerValue.EnumerateRunes();
	public override bool Equals(AnyString? strB, StringComparison comparisonType) =>
		innerValue.Equals(strB?.ToString(), comparisonType);
	public override int GetHashCode(StringComparison comparisonType) =>
		innerValue.GetHashCode(comparisonType);
	public override int IndexOf(AnyString value, Int32 startIndex, Int32 count, StringComparison comparisonType) =>
		innerValue.IndexOf(value.ToString(), startIndex, count, comparisonType);
	
	public override int IndexOfAny(AnyChar[] value, Int32 startIndex, Int32 count) {
		var firstMatchingIndex = -1;
			
		foreach (var valueChar in value) {
			var str = valueChar.ToString();
			var idx = innerValue.IndexOf(str, startIndex, count, StringComparison.Ordinal);
			
			if (idx >= 0 && (firstMatchingIndex < 0 || idx < firstMatchingIndex)) {
				firstMatchingIndex = idx;
			}
		}
		
		return firstMatchingIndex;
	}
	
	public override AnyString Insert(Int32 index, AnyString value) =>
		innerValue.Insert(index, value.ToString());
	public override bool IsNormalized(NormalizationForm normalizationForm) =>
		innerValue.IsNormalized(normalizationForm);
	public override int LastIndexOf(AnyString value, Int32 startIndex, Int32 count, StringComparison comparisonType) =>
		innerValue.LastIndexOf(value.ToString(), startIndex, count, comparisonType);
	
	public override int LastIndexOfAny(AnyChar[] value, Int32 startIndex, Int32 count) {
		var lastMatchingIndex = -1;
			
		foreach (var valueChar in value) {
			var str = valueChar.ToString();
			var idx = innerValue.LastIndexOf(str, startIndex, count, StringComparison.Ordinal);
			
			if (idx >= 0 && (lastMatchingIndex < 0 || idx > lastMatchingIndex)) {
				lastMatchingIndex = idx;
			}
		}
		
		return lastMatchingIndex;
	}
	
	public override AnyString Normalize(NormalizationForm normalizationForm) =>
		innerValue.Normalize(normalizationForm);
	
	public override AnyString PadLeft(Int32 count, AnyChar value) {
		var str = value.ToString();
		
		StringBuilder sb = new();
		while (sb.Length + innerValue.Length < count) {
			sb.Append(str);
		}
		
		return sb.Append(innerValue).ToString();
	}
	
	public override AnyString PadRight(Int32 count, AnyChar value) {
		var str = value.ToString();
		
		StringBuilder sb = new(innerValue);
		while (sb.Length < count) {
			sb.Append(str);
		}
		
		return sb.ToString();
	}
	
	public override AnyString Remove(Int32 startIndex, Int32 count) =>
		innerValue.Remove(startIndex, count);
	public override AnyString Replace(AnyString oldValue, AnyString newValue, StringComparison comparisonType) =>
		innerValue.Replace(oldValue.ToString(), newValue.ToString(), comparisonType);
	public override AnyString ReplaceLineEndings(AnyString value) =>
		innerValue.ReplaceLineEndings(value.ToString());
	public override IEnumerable<AnyString> Split(AnyString[] separator, Int32 count, StringSplitOptions options) =>
		innerValue.Split(separator.Select(x => x.ToString()).ToArray(), count, options).Select(x => (AnyString) x);
	public override bool StartsWith(AnyString value, StringComparison comparisonType) =>
		innerValue.StartsWith(value.ToString(), comparisonType);
	public override AnyString Substring(Int32 startIndex, Int32 length) =>
		innerValue.Substring(startIndex, length);
	public override IEnumerable<AnyChar> ToCharArray(Int32 startIndex, Int32 length) =>
		innerValue.ToCharArray(startIndex, length).Select(x => (AnyChar) x);
	public override AnyString ToLower() =>
		innerValue.ToLower();
	public override AnyString ToUpper() =>
		innerValue.ToUpper();
	public override AnyString TrimEnd(params AnyChar[]? trimChars) =>
		innerValue.TrimEnd();
	public override AnyString TrimStart(params AnyChar[]? trimChars) =>
		innerValue.TrimStart();
	
	protected override IEnumerable Enumerable() {
		foreach (var value in innerValue) {
			yield return value;
		}
	}
}

//class AnyString<T>: AnyString where T: IEnumerable<char> {
//	T innerValue;
//	
//	public AnyString(T value) {
//		innerValue = value;
//	}
//}