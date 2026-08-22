namespace Jimbl.JSON5;

public class JNumber: JItem, IComparable<JNumber> {
	readonly double value;
	
	internal JNumber(double value) {
		this.value = value;
	}
	
	public override string Serialize(int _) {
		if (double.IsNaN(value)) return "NaN";
		if (double.IsNegativeInfinity(value)) return "-Infinity";
		if (double.IsPositiveInfinity(value)) return "Infinity";
		return value.ToString();
	}
	
	public static implicit operator double  (JNumber n) =>           n.value;
	public static implicit operator decimal (JNumber n) => (decimal) n.value;
	public static explicit operator float   (JNumber n) => (float)   n.value;
	public static explicit operator Int64   (JNumber n) => (Int64)   n.value;
	public static explicit operator UInt64  (JNumber n) => (UInt64)  n.value;
	public static explicit operator Int32   (JNumber n) => (Int32)   n.value;
	public static explicit operator UInt32  (JNumber n) => (UInt32)  n.value;
	public static explicit operator Int16   (JNumber n) => (Int16)   n.value;
	public static explicit operator UInt16  (JNumber n) => (UInt16)  n.value;
	public static explicit operator sbyte   (JNumber n) => (sbyte)   n.value;
	public static explicit operator byte    (JNumber n) => (byte)    n.value;
	
	public static implicit operator JNumber (double  n) => new(n);
	public static explicit operator JNumber (decimal n) => new((double) n);
	public static implicit operator JNumber (float   n) => new(n);
	public static explicit operator JNumber (Int64   n) => new(n);
	public static explicit operator JNumber (UInt64  n) => new(n);
	public static implicit operator JNumber (Int32   n) => new(n);
	public static implicit operator JNumber (UInt32  n) => new(n);
	public static implicit operator JNumber (Int16   n) => new(n);
	public static implicit operator JNumber (UInt16  n) => new(n);
	public static implicit operator JNumber (sbyte   n) => new(n);
	public static implicit operator JNumber (byte    n) => new(n);

	public int CompareTo(JNumber? other) => value.CompareTo(other?.value);

	public override string ToString()          => value.ToString();
	public override bool   Equals(object? obj) => value.Equals(obj);
	public override int    GetHashCode()       => value.GetHashCode();
	
	public static JNumber operator + (JNumber lhs) => +lhs.value;
	public static JNumber operator - (JNumber lhs) => -lhs.value;
	
	// JNumber <-> JNumber
	public static JNumber operator + (JNumber lhs, JNumber rhs) => lhs.value + rhs.value;
	public static JNumber operator - (JNumber lhs, JNumber rhs) => lhs.value - rhs.value;
	public static JNumber operator * (JNumber lhs, JNumber rhs) => lhs.value * rhs.value;
	public static JNumber operator / (JNumber lhs, JNumber rhs) => lhs.value / rhs.value;
	public static JNumber operator % (JNumber lhs, JNumber rhs) => lhs.value % rhs.value;
	
	public static bool operator == (JNumber lhs, JNumber rhs) => lhs.value == rhs.value;
	public static bool operator != (JNumber lhs, JNumber rhs) => lhs.value != rhs.value;
	public static bool operator <  (JNumber lhs, JNumber rhs) => lhs.value <  rhs.value;
	public static bool operator <= (JNumber lhs, JNumber rhs) => lhs.value <= rhs.value;
	public static bool operator >  (JNumber lhs, JNumber rhs) => lhs.value >  rhs.value;
	public static bool operator >= (JNumber lhs, JNumber rhs) => lhs.value >= rhs.value;
	
	// JNumber <-> double
	public static JNumber operator + (JNumber lhs, double rhs) => lhs.value + rhs;
	public static JNumber operator - (JNumber lhs, double rhs) => lhs.value - rhs;
	public static JNumber operator * (JNumber lhs, double rhs) => lhs.value * rhs;
	public static JNumber operator / (JNumber lhs, double rhs) => lhs.value / rhs;
	public static JNumber operator % (JNumber lhs, double rhs) => lhs.value % rhs;
	
	public static bool operator == (JNumber lhs, double rhs) => lhs.value == rhs;
	public static bool operator != (JNumber lhs, double rhs) => lhs.value != rhs;
	public static bool operator <  (JNumber lhs, double rhs) => lhs.value <  rhs;
	public static bool operator <= (JNumber lhs, double rhs) => lhs.value <= rhs;
	public static bool operator >  (JNumber lhs, double rhs) => lhs.value >  rhs;
	public static bool operator >= (JNumber lhs, double rhs) => lhs.value >= rhs;
	
	// double <-> JNumber
	public static JNumber operator + (double lhs, JNumber rhs) => lhs + rhs.value;
	public static JNumber operator - (double lhs, JNumber rhs) => lhs - rhs.value;
	public static JNumber operator * (double lhs, JNumber rhs) => lhs * rhs.value;
	public static JNumber operator / (double lhs, JNumber rhs) => lhs / rhs.value;
	public static JNumber operator % (double lhs, JNumber rhs) => lhs % rhs.value;
	
	public static bool operator == (double lhs, JNumber rhs) => lhs == rhs.value;
	public static bool operator != (double lhs, JNumber rhs) => lhs != rhs.value;
	public static bool operator <  (double lhs, JNumber rhs) => lhs <  rhs.value;
	public static bool operator <= (double lhs, JNumber rhs) => lhs <= rhs.value;
	public static bool operator >  (double lhs, JNumber rhs) => lhs >  rhs.value;
	public static bool operator >= (double lhs, JNumber rhs) => lhs >= rhs.value;
}