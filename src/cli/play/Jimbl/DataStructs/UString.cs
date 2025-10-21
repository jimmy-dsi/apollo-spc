namespace Jimbl.DataStructs;

using System.Diagnostics;
using System.Collections;
using System.Text;

using VarArray = ArrayUnion<Byte, UInt16, UInt32>;
using VarList  =  ListUnion<Byte, UInt16, UInt32>;

public class UString: AnyString, IEnumerable<UChar> {
	VarArray data;
	int      length;
	
	public override int Length => length;
	
	public UString(string s) {
		VarList buffer = new List<Byte>();
		
		foreach (var rune in s.EnumerateRunes()) {
			if (!buffer.Is<List<UInt32>>(out _) && (UInt32) rune.Value >= 65536) {
				buffer = resize<UInt32>(buffer);
			}
			else if (buffer.Is<List<Byte>>(out _) && (UInt32) rune.Value >= 256) {
				buffer = resize<UInt16>(buffer);
			}
			
			append(buffer, rune.Value);
		}
		
		data = buffer.Match<VarArray>(
			b => b.ToArray(),
			b => b.ToArray(),
			b => b.ToArray()
		);
		
		length = data.Length;
	}
	
	public override UChar this[int index] => data.Match(d => d[index],
	                                                    d => d[index],
	                                                    d => d[index]);
	
	public static implicit operator UString ( string str) => new(str);
	public static explicit operator  string (UString str) => str.ToString();
	
	public override int CompareTo(AnyString? strB) {
		if (strB is null) {
			throw new NullReferenceException();
		}
		
		switch (strB) {
			case AnyString_string s: {
				return compareTo((string) s);
			}
			case UString s: {
				return compareTo(s);
			}
			default: {
				return -strB.CompareTo(this);
			}
		}
	}
	
	public override bool Contains(AnyString value, StringComparison comparisonType) {
		bool ignoreCase;
		
		switch (comparisonType) {
			case StringComparison.Ordinal: {
				ignoreCase = false;
				break;
			}
			case StringComparison.OrdinalIgnoreCase: {
				ignoreCase = true;
				break;
			}
			default: {
				return ToString().Contains(value.ToString(), comparisonType);
			}
		}
		
		switch (value) {
			case AnyString_string s: {
				return contains((string) s, ignoreCase);
			}
			case UString s: {
				return contains(s, ignoreCase);
			}
			default: {
				return ToString().Contains(value.ToString(), comparisonType);
			}
		}
	}
	
	public override bool EndsWith(AnyString value, StringComparison comparisonType) {
		switch (value) {
			case AnyString_string s: {
				return endsWith((string) s, comparisonType);
			}
			case UString: {
				break;
			}
			default: {
				return ToString().EndsWith(value.ToString(), comparisonType);
			}
		}
		
		switch (comparisonType) {
			case StringComparison.Ordinal: {
				return endsWith((UString) value, false);
			}
			case StringComparison.OrdinalIgnoreCase: {
				return endsWith((UString) value, true);
			}
			default: {
				return ToString().EndsWith(value.ToString(), comparisonType);
			}
		}
	}
	
	public override IEnumerable<Rune> EnumerateRunes() {
		foreach (var c in this) {
			yield return new(c);
		}
	}
	
	public override bool Equals(AnyString? strB, StringComparison comparisonType) {
		bool ignoreCase;
		
		if (strB is null) {
			return false;
		}
		
		switch (comparisonType) {
			case StringComparison.Ordinal: {
				ignoreCase = false;
				break;
			}
			case StringComparison.OrdinalIgnoreCase: {
				ignoreCase = true;
				break;
			}
			default: {
				return ToString().Equals(strB.ToString(), comparisonType);
			}
		}
		
		switch (strB) {
			case AnyString_string s: {
				return equals((string) s, ignoreCase);
			}
			case UString s: {
				return equals(s, ignoreCase);
			}
			default: {
				return ToString().Equals(strB.ToString(), comparisonType);
			}
		}
	}
	
	public override int GetHashCode(StringComparison comparisonType) => ToString().GetHashCode(comparisonType);
	
	public override int IndexOf(AnyString value, Int32 startIndex, Int32 count, StringComparison comparisonType) =>
		throw new NotImplementedException();
	
	public override int       IndexOfAny(AnyChar[] value, Int32 startIndex, Int32 count) => throw new NotImplementedException();
	public override AnyString Insert(Int32 index, AnyString value)                       => throw new NotImplementedException();
	public override bool      IsNormalized(NormalizationForm normalizationForm)          => throw new NotImplementedException();
	
	public override int LastIndexOf(AnyString value, Int32 startIndex, Int32 count, StringComparison comparisonType) =>
		throw new NotImplementedException();
	
	public override int       LastIndexOfAny(AnyChar[] value, Int32 startIndex, Int32 count) => throw new NotImplementedException();
	public override AnyString Normalize(NormalizationForm normalizationForm)                 => throw new NotImplementedException();
	public override AnyString PadLeft(Int32 count, AnyChar value)                            => throw new NotImplementedException();
	public override AnyString PadRight(Int32 count, AnyChar value)                           => throw new NotImplementedException();
	public override AnyString Remove(Int32 startIndex, Int32 count)                          => throw new NotImplementedException();
	
	public override AnyString Replace(AnyString oldValue, AnyString newValue, StringComparison comparisonType) =>
		throw new NotImplementedException();
	
	public override AnyString ReplaceLineEndings(AnyString value) => throw new NotImplementedException();
	
	public override IEnumerable<AnyString> Split(AnyString[] separator, Int32 count, StringSplitOptions options) =>
		throw new NotImplementedException();
	
	public override bool StartsWith(AnyString value, StringComparison comparisonType) {
		switch (value) {
			case AnyString_string s: {
				return startsWith((string) s, comparisonType);
			}
			case UString: {
				break;
			}
			default: {
				return ToString().StartsWith(value.ToString(), comparisonType);
			}
		}
		
		switch (comparisonType) {
			case StringComparison.Ordinal: {
				return startsWith((UString) value, false);
			}
			case StringComparison.OrdinalIgnoreCase: {
				return startsWith((UString) value, true);
			}
			default: {
				return ToString().StartsWith(value.ToString(), comparisonType);
			}
		}
	}
	
	public override AnyString            Substring(Int32 startIndex, Int32 length)                    => throw new NotImplementedException();
	public override IEnumerable<AnyChar> ToCharArray(Int32 startIndex, Int32 length)                  => throw new NotImplementedException();
	public override AnyString            ToLower()                                                    => throw new NotImplementedException();
	public override AnyString            ToUpper()                                                    => throw new NotImplementedException();
	public override AnyString            TrimEnd(params AnyChar[]? trimChars)                         => throw new NotImplementedException();
	public override AnyString            TrimStart(params AnyChar[]? trimChars)                       => throw new NotImplementedException();
	
	public new IEnumerator<UChar> GetEnumerator() {
		foreach (var item in Enumerable()) {
			yield return (UChar) item;
		}
	}
	
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	
	public static UString operator + (UString lhs, UString rhs) => (UString) lhs.Concat(rhs);
	public static UString operator + (UString lhs,  string rhs) =>           lhs.Concat(rhs);
	public static UString operator + ( string lhs, UString rhs) =>           rhs.ConcatFrom(lhs);
	
	protected internal override AnyString Concat(AnyString other) {
		switch (other) {
			case AnyString_string s: {
				return Concat((string) s);
			}
			case UString s: {
				// TODO: Implement properly
				return Concat((UString) s);
			}
			default: {
				return other.ConcatFrom(this);
			}
		}
	}
	
	protected internal override UString Concat(string other) {
		return this + (UString) other;
	}
	
	protected internal override UString ConcatFrom(string other) {
		return (UString) other + this;
	}
	
	protected override IEnumerable Enumerable() {
		foreach (var x in (..Length).Enum()) {
			yield return this[x];
		}
	}
	
	int compareTo(string  str) => compareTo((UString) str);
	int compareTo(UString str) {
		for (var i = 0; i < Math.Min(Length, str.Length); i++) {
			var c1 = this[i];
			var c2 =  str[i];
			
			if (c1 < c2) {
				return -1;
			}
			else if (c1 > c2) {
				return 1;
			}
		}
		
		if (Length == str.Length) {
			return 0;
		}
		else {
			return Length < str.Length ? -1 : 1;
		}
	}
	
	bool contains(string str, bool ignoreCase) {
		var m = 0;
		var runeEnum = str.EnumerateRunes().GetEnumerator();
		
		for (var i = 0; i < Length; i++) {
			var    c1 = this[i];
			UChar? c2 = null;
			
			if (runeEnum.MoveNext()) {
				c2 = runeEnum.Current.Value;
			}
			
			if (c2 is not null && (!ignoreCase && c1 == c2) || (ignoreCase && c1.ToLower() == c2!.ToLower())) {
				m++;
			}
			else {
				m = 0;
				((IDisposable) runeEnum).Dispose();
				runeEnum = str.EnumerateRunes().GetEnumerator();
			}
			
			if (m == str.Length) {
				((IDisposable) runeEnum).Dispose();
				return true;
			}
		}
		
		((IDisposable) runeEnum).Dispose();
		return false;
	}
	
	bool contains(UString str, bool ignoreCase) {
		var m = 0;
		
		for (var i = 0; i < Length; i++) {
			var c1 = this[i];
			var c2 = Try.CatchNull(() => str[m]);
			
			if (c2 is not null && (!ignoreCase && c1 == c2) || (ignoreCase && c1.ToLower() == c2!.ToLower())) {
				m++;
			}
			else {
				m = 0;
			}
			
			if (m == str.Length) {
				return true;
			}
		}
		
		return false;
	}
	
	bool endsWith(string str, StringComparison comparisonType) {
		var tentativeStart = Math.Max(0, Length - str.Length);
		StringBuilder sb = new();
		
		for (var i = tentativeStart; i < Length; i++) {
			sb.Append(this[i].ToString());
		}
		
		var lhs = sb.ToString();
		return lhs.EndsWith(str, comparisonType);
	}
	
	bool endsWith(UString str, bool ignoreCase) {
		if (Length < str.Length) {
			return false;
		}
		
		for (var i = 0; i < str.Length; i++) {
			var c1 = this[    Length - 1 - i];
			var c2 =  str[str.Length - 1 - i];
			
			if (ignoreCase && c1 != c2 || !ignoreCase && c1.ToLower() != c2.ToLower()) {
				return false;
			}
		}
		
		return true;
	}
	
	bool equals(string str, bool ignoreCase) {
		if (Length > str.Length) {
			return false;
		}
		else if (str.Length / 2 < Length) {
			return false;
		}
		
		var i = 0;
		
		foreach (var c in str.EnumerateRunes()) {
			var c1 = this[i];
			var c2 = (UChar) c.Value;
			
			if (!ignoreCase && c1 != c2 || ignoreCase && c1.ToLower() != c2.ToLower()) {
				return false;
			}
			
			i++;
		}
		
		return i == Length;
	}
	
	bool equals(UString str, bool ignoreCase) {
		if (Length != str.Length) {
			return false;
		}
		
		for (var i = 0; i < Length; i++) {
			var c1 = this[i];
			var c2 =  str[i];
			
			if (!ignoreCase && c1 != c2 || ignoreCase && c1.ToLower() != c2.ToLower()) {
				return false;
			}
		}
		
		return true;
	}
	
	static VarList resize<T>(VarList buffer) {
		VarList newBuffer;
		
		if (typeof(T) == typeof(UInt16)) {
			List<UInt16> buffer2 = new();
			
			foreach (var b in buffer) {
				buffer2.Add(b.As<Byte>());
			}
			
			newBuffer = buffer2;
		}
		else if (typeof(T) == typeof(UInt32)) {
			List<UInt32> buffer2 = new();
			
			foreach (var b in buffer) {
				buffer2.Add(b.Match(x => x, x => x, x => x));
			}
			
			newBuffer = buffer2;
		}
		else {
			throw new UnreachableException();
		}
		
		return newBuffer;
	}
	
	bool startsWith(string str, StringComparison comparisonType) {
		var tentativeEnd = Math.Min(Length, str.Length);
		StringBuilder sb = new();
		
		for (var i = 0; i < tentativeEnd; i++) {
			sb.Append(this[i].ToString());
		}
		
		var lhs = sb.ToString();
		return lhs.StartsWith(str, comparisonType);
	}
	
	bool startsWith(UString str, bool ignoreCase) {
		if (Length < str.Length) {
			return false;
		}
		
		for (var i = 0; i < str.Length; i++) {
			var c1 = this[i];
			var c2 =  str[i];
			
			if (ignoreCase && c1 != c2 || !ignoreCase && c1.ToLower() != c2.ToLower()) {
				return false;
			}
		}
		
		return true;
	}
	
	static void append(VarList buffer, UChar value) {
		buffer.Add(value);
	}
}