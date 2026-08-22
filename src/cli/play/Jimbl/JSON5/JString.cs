namespace Jimbl.JSON5;

using System.Text;
using System.Collections;

public class JString: JItem, IEquatable<JString?>, IComparable<JString?>, IEnumerable<char> {
	readonly string value;
	
	public int Length => value.Length;
	
	internal JString(string value) {
		this.value = value;
	}
	
	public override string Serialize(int _) {
		return Escape(value);
	}
	
	public static string Escape(JString str) => Escape((string) str);
	public static string Escape(string str) {
		StringBuilder sb = new();
		sb.Append('"');
		foreach (var c in str) {
			if (c == '"') sb.Append("\\\"");
			else if (c == '\\') sb.Append("\\\\");
			else if (c == '\b') sb.Append("\\b");
			else if (c == '\f') sb.Append("\\f");
			else if (c == '\n') sb.Append("\\n");
			else if (c == '\r') sb.Append("\\r");
			else if (c == '\t') sb.Append("\\t");
			else if ((int) c is >= 0x20 and <= 0x7E) sb.Append(c);
			else sb.Append($"\\u{(int) c :X4}");
		}
		sb.Append('"');
		return sb.ToString();
	}
	
	public static implicit operator string  (JString str) => str.value;
	public static implicit operator JString (string  str) => new(str);
	
	public char    this[int index]   => value[index];
	public char    this[Index index] => value[index];
	public JString this[Range range] => value[range];

	public bool Equals(JString? other)    => value == other?.value;
	public int  CompareTo(JString? other) => value.CompareTo(other?.value);

	public IEnumerator<char> GetEnumerator() => value.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator()  => value.GetEnumerator();
	
	public override string ToString()          => value;
	public override bool   Equals(object? obj) => value.Equals(obj);
	public override int    GetHashCode()       => value.GetHashCode();
	
	public static string operator + (JString lhs, JString rhs) => lhs.value + rhs.value;
	public static string operator + (JString lhs, string  rhs) => lhs.value + rhs;
	public static string operator + (string  lhs, JString rhs) => lhs       + rhs.value;
	
	public static bool operator == (JString? lhs, JString? rhs) => lhs is null && rhs is null ? true  :   lhs?.Equals(rhs) ?? false;
	public static bool operator != (JString? lhs, JString? rhs) => lhs is null && rhs is null ? false : !(lhs?.Equals(rhs) ?? false);
	
	public static bool operator == (JString? lhs, string? rhs) => lhs is null && rhs is null ? true  :   lhs?.Equals(rhs) ?? false;
	public static bool operator != (JString? lhs, string? rhs) => lhs is null && rhs is null ? false : !(lhs?.Equals(rhs) ?? false);
	
	public static bool operator == (string?  lhs, JString? rhs) => lhs is null && rhs is null ? true  :   lhs?.Equals(rhs) ?? false;
	public static bool operator != (string?  lhs, JString? rhs) => lhs is null && rhs is null ? false : !(lhs?.Equals(rhs) ?? false);
	
	public static bool operator <  (JString lhs, JString rhs) => lhs.CompareTo(rhs) <  0;
	public static bool operator >  (JString lhs, JString rhs) => lhs.CompareTo(rhs) >  0;
	public static bool operator <= (JString lhs, JString rhs) => lhs.CompareTo(rhs) <= 0;
	public static bool operator >= (JString lhs, JString rhs) => lhs.CompareTo(rhs) >= 0;
	
	public static bool operator <  (JString lhs, string rhs) => lhs.CompareTo(rhs) <  0;
	public static bool operator >  (JString lhs, string rhs) => lhs.CompareTo(rhs) >  0;
	public static bool operator <= (JString lhs, string rhs) => lhs.CompareTo(rhs) <= 0;
	public static bool operator >= (JString lhs, string rhs) => lhs.CompareTo(rhs) >= 0;
	
	public static bool operator <  (string lhs, JString rhs) => lhs.CompareTo(rhs) <  0;
	public static bool operator >  (string lhs, JString rhs) => lhs.CompareTo(rhs) >  0;
	public static bool operator <= (string lhs, JString rhs) => lhs.CompareTo(rhs) <= 0;
	public static bool operator >= (string lhs, JString rhs) => lhs.CompareTo(rhs) >= 0;
}