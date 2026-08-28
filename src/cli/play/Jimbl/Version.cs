namespace Jimbl;

public class Version {
	System.Version version;
	
	public int Major    => version.Major;
	public int Minor    => version.Minor;
	public int Build    => version.Build;
	public int Revision => version.Revision;
	
	Version(System.Version version) {
		this.version = version;
	}
	
	public static implicit operator Version (System.Version v) => new(v);
	public static implicit operator System.Version (Version v) => v.version;
	
	public static implicit operator Version ( int n)                 => new(new(n, 0));
	public static implicit operator Version ((int, int) v)           => new(new(v.Item1, v.Item2));
	public static implicit operator Version ((int, int, int) v)      => new(new(v.Item1, v.Item2, v.Item3));
	public static implicit operator Version ((int, int, int, int) v) => new(new(v.Item1, v.Item2, v.Item3, v.Item4));
	
	public static implicit operator Version (string s) => new(new(s));
	
	public static bool operator == (Version lhs, Version rhs) => lhs.version == rhs.version;
	public static bool operator != (Version lhs, Version rhs) => lhs.version != rhs.version;
	public static bool operator <  (Version lhs, Version rhs) => lhs.version <  rhs.version;
	public static bool operator <= (Version lhs, Version rhs) => lhs.version <= rhs.version;
	public static bool operator >  (Version lhs, Version rhs) => lhs.version >  rhs.version;
	public static bool operator >= (Version lhs, Version rhs) => lhs.version >= rhs.version;
	
	public static bool operator == (Version lhs, string rhs) => lhs == (Version) rhs;
	public static bool operator != (Version lhs, string rhs) => lhs != (Version) rhs;
	public static bool operator <  (Version lhs, string rhs) => lhs <  (Version) rhs;
	public static bool operator <= (Version lhs, string rhs) => lhs <= (Version) rhs;
	public static bool operator >  (Version lhs, string rhs) => lhs >  (Version) rhs;
	public static bool operator >= (Version lhs, string rhs) => lhs >= (Version) rhs;
	
	public static bool operator == (string lhs, Version rhs) => (Version) lhs == rhs;
	public static bool operator != (string lhs, Version rhs) => (Version) lhs != rhs;
	public static bool operator <  (string lhs, Version rhs) => (Version) lhs <  rhs;
	public static bool operator <= (string lhs, Version rhs) => (Version) lhs <= rhs;
	public static bool operator >  (string lhs, Version rhs) => (Version) lhs >  rhs;
	public static bool operator >= (string lhs, Version rhs) => (Version) lhs >= rhs;
	
	public static bool operator == (Version lhs, int rhs) => lhs.Major == rhs;
	public static bool operator != (Version lhs, int rhs) => lhs.Major != rhs;
	public static bool operator <  (Version lhs, int rhs) => lhs.Major <  rhs;
	public static bool operator <= (Version lhs, int rhs) => lhs.Major <= rhs;
	public static bool operator >  (Version lhs, int rhs) => lhs.Major >  rhs;
	public static bool operator >= (Version lhs, int rhs) => lhs.Major >= rhs;
	
	public static bool operator == (int lhs, Version rhs) => lhs == rhs.Major;
	public static bool operator != (int lhs, Version rhs) => lhs != rhs.Major;
	public static bool operator <  (int lhs, Version rhs) => lhs <  rhs.Major;
	public static bool operator <= (int lhs, Version rhs) => lhs <= rhs.Major;
	public static bool operator >  (int lhs, Version rhs) => lhs >  rhs.Major;
	public static bool operator >= (int lhs, Version rhs) => lhs >= rhs.Major;
	
	public static bool operator == (Version lhs, (int, int) rhs) => lhs.Major == rhs.Item1 && lhs.Minor == rhs.Item2;
	public static bool operator != (Version lhs, (int, int) rhs) => !(lhs == rhs);
	public static bool operator <  (Version lhs, (int, int) rhs) => lhs.Major < rhs.Item1 || lhs.Major == rhs.Item1 && lhs.Minor < rhs.Item2;
	public static bool operator <= (Version lhs, (int, int) rhs) => lhs < rhs || lhs == rhs;
	public static bool operator >  (Version lhs, (int, int) rhs) => !(lhs < rhs);
	public static bool operator >= (Version lhs, (int, int) rhs) => lhs > rhs || lhs == rhs;
	
	public static bool operator == ((int, int) lhs, Version rhs) => lhs.Item1 == rhs.Major && lhs.Item2 == rhs.Minor;
	public static bool operator != ((int, int) lhs, Version rhs) => !(lhs == rhs);
	public static bool operator <  ((int, int) lhs, Version rhs) => rhs > lhs;
	public static bool operator <= ((int, int) lhs, Version rhs) => lhs < rhs || lhs == rhs;
	public static bool operator >  ((int, int) lhs, Version rhs) => !(lhs < rhs);
	public static bool operator >= ((int, int) lhs, Version rhs) => lhs > rhs || lhs == rhs;
	
	public static bool operator == (Version lhs, (int, int, int) rhs) => lhs.Major == rhs.Item1 && lhs.Minor == rhs.Item2 && lhs.Build == rhs.Item3;
	public static bool operator != (Version lhs, (int, int, int) rhs) => !(lhs == rhs);
	public static bool operator <  (Version lhs, (int, int, int) rhs) => lhs.Major < rhs.Item1 || lhs.Major == rhs.Item1 && (lhs.Minor < rhs.Item2 || lhs.Minor == rhs.Item2 && lhs.Build < rhs.Item3);
	public static bool operator <= (Version lhs, (int, int, int) rhs) => lhs < rhs || lhs == rhs;
	public static bool operator >  (Version lhs, (int, int, int) rhs) => !(lhs < rhs);
	public static bool operator >= (Version lhs, (int, int, int) rhs) => lhs > rhs || lhs == rhs;
	
	public static bool operator == ((int, int, int) lhs, Version rhs) => lhs.Item1 == rhs.Major && lhs.Item2 == rhs.Minor && lhs.Item3 == rhs.Build;
	public static bool operator != ((int, int, int) lhs, Version rhs) => !(lhs == rhs);
	public static bool operator <  ((int, int, int) lhs, Version rhs) => rhs > lhs;
	public static bool operator <= ((int, int, int) lhs, Version rhs) => lhs < rhs || lhs == rhs;
	public static bool operator >  ((int, int, int) lhs, Version rhs) => !(lhs < rhs);
	public static bool operator >= ((int, int, int) lhs, Version rhs) => lhs > rhs || lhs == rhs;
	
	public static bool operator == (Version lhs, (int, int, int, int) rhs) => lhs == (Version) rhs;
	public static bool operator != (Version lhs, (int, int, int, int) rhs) => lhs != (Version) rhs;
	public static bool operator <  (Version lhs, (int, int, int, int) rhs) => lhs <  (Version) rhs;
	public static bool operator <= (Version lhs, (int, int, int, int) rhs) => lhs <= (Version) rhs;
	public static bool operator >  (Version lhs, (int, int, int, int) rhs) => lhs >  (Version) rhs;
	public static bool operator >= (Version lhs, (int, int, int, int) rhs) => lhs >= (Version) rhs;
	
	public static bool operator == ((int, int, int, int) lhs, Version rhs) => (Version) lhs == rhs;
	public static bool operator != ((int, int, int, int) lhs, Version rhs) => (Version) lhs != rhs;
	public static bool operator <  ((int, int, int, int) lhs, Version rhs) => (Version) lhs <  rhs;
	public static bool operator <= ((int, int, int, int) lhs, Version rhs) => (Version) lhs <= rhs;
	public static bool operator >  ((int, int, int, int) lhs, Version rhs) => (Version) lhs >  rhs;
	public static bool operator >= ((int, int, int, int) lhs, Version rhs) => (Version) lhs >= rhs;
}