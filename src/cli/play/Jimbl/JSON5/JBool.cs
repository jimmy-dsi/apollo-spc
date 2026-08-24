namespace Jimbl.JSON5;

public class JBool: JItem {
	readonly bool value;
	
	internal JBool(bool value) {
		this.value = value;
	}
	
	public override string Serialize(int _) {
		return value ? "true" : "false";
	}
	
	public override string ToString()          => value.ToString();
	public override bool   Equals(object? obj) => value.Equals(obj);
	public override int    GetHashCode()       => value.GetHashCode();

	public static bool operator true  (JBool b) =>  b.value;
	public static bool operator false (JBool b) => !b.value;
	
	public static implicit operator bool  (JBool b) => b.value;
	public static implicit operator JBool (bool  b) => new(b);
	
	public static bool operator == (JBool lhs, JBool rhs) => lhs.value == rhs.value;
	public static bool operator != (JBool lhs, JBool rhs) => lhs.value != rhs.value;
	
	public static bool operator == (JBool lhs, bool rhs) => lhs.value == rhs;
	public static bool operator != (JBool lhs, bool rhs) => lhs.value != rhs;
	
	public static bool operator == (bool lhs, JBool rhs) => lhs == rhs.value;
	public static bool operator != (bool lhs, JBool rhs) => lhs != rhs.value;
}