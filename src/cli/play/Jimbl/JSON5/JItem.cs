namespace Jimbl.JSON5;

public abstract class JItem {
	public static JObject Load(string fileData) {
		return new JSON5Parser(fileData).Parse();
	}
	
	public abstract string Serialize(int level = 0);
	
	// Implicit indexing
	public JItem? this[string propName] => ((JObject) this)[propName];
	
	public JItem? this[int   index] => ((JArray) this)[index];
	public JItem? this[Index index] => ((JArray) this)[index];
	public JArray this[Range range] => ((JArray) this)[range];
	
	// Up-casts (mostly implicit)
	public static implicit operator JItem (Dictionary<string, JItem?> obj) => new JObject(obj);
	
	public static implicit operator JItem (List<JItem?>  list) => new JArray(list);
	public static implicit operator JItem (JItem?[]     array) => new JArray(array);
	
	public static implicit operator JItem (string str) => new JString(str);
	
	public static implicit operator JItem (bool b) => new JBool(b);
	
	public static implicit operator JItem (double  n) => new JNumber(n);
	public static explicit operator JItem (decimal n) => new JNumber((double) n);
	public static implicit operator JItem (float   n) => new JNumber(n);
	public static explicit operator JItem (Int64   n) => new JNumber(n);
	public static explicit operator JItem (UInt64  n) => new JNumber(n);
	public static implicit operator JItem (Int32   n) => new JNumber(n);
	public static implicit operator JItem (UInt32  n) => new JNumber(n);
	public static implicit operator JItem (Int16   n) => new JNumber(n);
	public static implicit operator JItem (UInt16  n) => new JNumber(n);
	public static implicit operator JItem (sbyte   n) => new JNumber(n);
	public static implicit operator JItem (byte    n) => new JNumber(n);
	
	// Down-casts (all explicit)
	public static explicit operator Dictionary<string, JItem?> (JItem obj) => (JObject) obj;
	
	public static explicit operator List<JItem?> (JItem array) => (JArray) array;
	public static explicit operator JItem?[]     (JItem array) => (JArray) array;
	
	public static explicit operator string (JItem str) => (JString) str;
	
	public static explicit operator bool (JItem b) => (JBool) b;
	
	public static explicit operator double  (JItem n) =>          (JNumber) n;
	public static explicit operator decimal (JItem n) =>          (JNumber) n;
	public static explicit operator float   (JItem n) => (float ) (JNumber) n;
	public static explicit operator Int64   (JItem n) => (Int64 ) (JNumber) n;
	public static explicit operator UInt64  (JItem n) => (UInt64) (JNumber) n;
	public static explicit operator Int32   (JItem n) => (Int32 ) (JNumber) n;
	public static explicit operator UInt32  (JItem n) => (UInt32) (JNumber) n;
	public static explicit operator Int16   (JItem n) => (Int16 ) (JNumber) n;
	public static explicit operator UInt16  (JItem n) => (UInt16) (JNumber) n;
	public static explicit operator sbyte   (JItem n) => (sbyte ) (JNumber) n;
	public static explicit operator byte    (JItem n) => (byte  ) (JNumber) n;
}