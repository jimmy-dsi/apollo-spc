namespace Jimbl.JMath;

public struct JVector3L: JVector3<long> {
	public long X { get; set; }
	public long Y { get; set; }
	public long Z { get; set; }
	
	public static explicit operator JVector2B (JVector3L vec) => new((byte) vec.X, (byte) vec.Y);
	public static explicit operator JVector2I (JVector3L vec) => new((int)  vec.X, (int)  vec.Y);
	public static explicit operator JVector2L (JVector3L vec) => new(       vec.X,        vec.Y);
	public static explicit operator JVector2F (JVector3L vec) => new(       vec.X,        vec.Y);
	public static explicit operator JVector2D (JVector3L vec) => new(       vec.X,        vec.Y);
	
	public static explicit operator JVector3B (JVector3L vec) => new((byte) vec.X, (byte) vec.Y, (byte) vec.Z);
	public static explicit operator JVector3I (JVector3L vec) => new((int)  vec.X, (int)  vec.Y, (int)  vec.Z);
	public static explicit operator JVector3F (JVector3L vec) => new(       vec.X,        vec.Y,        vec.Z);
	public static implicit operator JVector3D (JVector3L vec) => new(       vec.X,        vec.Y,        vec.Z);
	
	public JVector3L(long x, long y, long z) {
		X = x;
		Y = y;
		Z = z;
	}

	public long this[int itemIndex] {
		get => ((JVector3<long>) this)[itemIndex];
		set => ((JVector3<long>) this)[itemIndex] = value;
	}
	
	public double Magnitude => ((JVector3<long>) this).Magnitude;
	
	JVector3 JVector3.Negate() => Negate();
	
	public JVector3L Negate() {
		return new(-X, -Y, -Z);
	}
	
	public JVector Add(JVector other) {
		if (other is JVector3 v3) {
			return Add(v3);
		}
		else if (other is JVector2 v2) {
			return Add(v2);
		}
		else {
			return other.Add(this);
		}
	}
	
	public JVector3 Add(JVector2 other) {
		if (other is JVector2L v2L) {
			return Add(v2L);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.Add(b);
		}
	}
	
	public JVector3 Add(JVector3 other) {
		if (other is JVector3L v3L) {
			return Add(v3L);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.Add(b);
		}
	}
	
	public JVector3L Add(JVector2L other) {
		return Add((JVector3L) other);
	}
	
	public JVector3L Add(JVector3L other) {
		return new(X + other.X, Y + other.Y, Z + other.Y);
	}
	
	public JVector Subtract(JVector other) {
		if (other is JVector3 v3) {
			return Subtract(v3);
		}
		else if (other is JVector2 v2) {
			return Subtract(v2);
		}
		else {
			return other.SubtractFrom(this);
		}
	}
	
	public JVector3 Subtract(JVector2 other) {
		if (other is JVector2L v2L) {
			return Subtract(v2L);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.Subtract(b);
		}
	}
	
	public JVector3 Subtract(JVector3 other) {
		if (other is JVector3L v3L) {
			return Subtract(v3L);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.Subtract(b);
		}
	}
	
	public JVector3L Subtract(JVector2L other) {
		return Subtract((JVector3L) other);
	}
	
	public JVector3L Subtract(JVector3L other) {
		return new(X - other.X, Y - other.Y, Z - other.Z);
	}
	
	public JVector SubtractFrom(JVector other) {
		if (other is JVector3 v3) {
			return SubtractFrom(v3);
		}
		else if (other is JVector2 v2) {
			return SubtractFrom(v2);
		}
		else {
			return other.Subtract(this);
		}
	}
	
	public JVector3 SubtractFrom(JVector2 other) {
		if (other is JVector2L v2L) {
			return SubtractFrom(v2L);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.SubtractFrom(b);
		}
	}
	
	public JVector3 SubtractFrom(JVector3 other) {
		if (other is JVector3L v3L) {
			return SubtractFrom(v3L);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.SubtractFrom(b);
		}
	}
	
	public JVector3L SubtractFrom(JVector2L other) {
		return SubtractFrom((JVector3L) other);
	}
	
	public JVector3L SubtractFrom(JVector3L other) {
		return new(other.X - X, other.Y - Y, other.Z - Z);
	}
	
	public double Multiply(JVector other) {
		if (other is JVector3 v3) {
			return Multiply(v3);
		}
		else if (other is JVector2 v2) {
			return Multiply(v2);
		}
		else {
			return other.Multiply(this);
		}
	}
	
	public double Multiply(JVector2 other) {
		if (other is JVector2L v2L) {
			return Multiply(v2L);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.Multiply(b);
		}
	}
	
	public double Multiply(JVector3 other) {
		if (other is JVector3L v3L) {
			return Multiply(v3L);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.Multiply(b);
		}
	}
	
	public double Multiply(JVector2L other) {
		return Multiply((JVector3L) other);
	}
	
	public double Multiply(JVector3L other) {
		return X * other.X + Y * other.Y + Z * other.Z;
	}
	
	JVector3 JVector3.Multiply(double other) => Multiply(other);
	
	public JVector3D Multiply(double other) {
		return new(X * other, Y * other, Z * other);
	}
	
	public JVector3L Multiply(long other) {
		return new(X * other, Y * other, Z * other);
	}
	
	JVector3 JVector3.Divide(double other) => Divide(other);
	
	public JVector3D Divide(double other) {
		return new(X / other, Y / other, Z / other);
	}
	
	public JVector3L Divide(long other) {
		return new(X / other, Y / other, Z / other);
	}
	
	JVector3 JVector3.DivideFrom(double other) => DivideFrom(other);
	
	public JVector3D DivideFrom(double other) {
		return new(other / X, other / Y, other / Z);
	}
	
	public JVector3L DivideFrom(long other) {
		return new(other / X, other / Y, other / Z);
	}
	
	// Unary
	public static JVector3L operator + (JVector3L self) => self;
	public static JVector3L operator - (JVector3L self) => self.Negate();
	
	// With vec2
	public static JVector3L operator + (JVector3L lhs, JVector2L rhs) => lhs.Add(rhs);
	public static JVector3L operator + (JVector3L lhs, JVector2I rhs) => lhs.Add(rhs);
	public static JVector3L operator + (JVector3L lhs, JVector2B rhs) => lhs.Add(rhs);
	public static JVector3  operator + (JVector3L lhs, JVector2  rhs) => lhs.Add(rhs);
	public static JVector3L operator + (JVector2L lhs, JVector3L rhs) => rhs.Add(lhs);
	public static JVector3L operator + (JVector2I lhs, JVector3L rhs) => rhs.Add(lhs);
	public static JVector3L operator + (JVector2B lhs, JVector3L rhs) => rhs.Add(lhs);
	public static JVector3  operator + (JVector2  lhs, JVector3L rhs) => rhs.Add(lhs);
	public static JVector3L operator - (JVector3L lhs, JVector2L rhs) => lhs.Subtract(rhs);
	public static JVector3L operator - (JVector3L lhs, JVector2I rhs) => lhs.Subtract(rhs);
	public static JVector3L operator - (JVector3L lhs, JVector2B rhs) => lhs.Subtract(rhs);
	public static JVector3  operator - (JVector3L lhs, JVector2  rhs) => lhs.Subtract(rhs);
	public static JVector3L operator - (JVector2L lhs, JVector3L rhs) => rhs.SubtractFrom(lhs);
	public static JVector3L operator - (JVector2I lhs, JVector3L rhs) => rhs.SubtractFrom(lhs);
	public static JVector3L operator - (JVector2B lhs, JVector3L rhs) => rhs.SubtractFrom(lhs);
	public static JVector3  operator - (JVector2  lhs, JVector3L rhs) => rhs.SubtractFrom(lhs);
	public static double    operator * (JVector3L lhs, JVector2L rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector3L lhs, JVector2I rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector3L lhs, JVector2B rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector3L lhs, JVector2  rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector2L lhs, JVector3L rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector2I lhs, JVector3L rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector2B lhs, JVector3L rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector2  lhs, JVector3L rhs) => rhs.Multiply(lhs);
	
	// With vec3
	public static JVector3L operator + (JVector3L lhs, JVector3L rhs) => lhs.Add(rhs);
	public static JVector3L operator + (JVector3L lhs, JVector3I rhs) => lhs.Add(rhs);
	public static JVector3L operator + (JVector3L lhs, JVector3B rhs) => lhs.Add(rhs);
	public static JVector3  operator + (JVector3L lhs, JVector3  rhs) => lhs.Add(rhs);
	public static JVector   operator + (JVector3L lhs, JVector   rhs) => lhs.Add(rhs);
	public static JVector3L operator + (JVector3I lhs, JVector3L rhs) => rhs.Add(lhs);
	public static JVector3L operator + (JVector3B lhs, JVector3L rhs) => rhs.Add(lhs);
	public static JVector3  operator + (JVector3  lhs, JVector3L rhs) => rhs.Add(lhs);
	public static JVector   operator + (JVector   lhs, JVector3L rhs) => rhs.Add(lhs);
	public static JVector3L operator - (JVector3L lhs, JVector3L rhs) => lhs.Subtract(rhs);
	public static JVector3L operator - (JVector3L lhs, JVector3I rhs) => lhs.Subtract(rhs);
	public static JVector3L operator - (JVector3L lhs, JVector3B rhs) => lhs.Subtract(rhs);
	public static JVector3  operator - (JVector3L lhs, JVector3  rhs) => lhs.Subtract(rhs);
	public static JVector   operator - (JVector3L lhs, JVector   rhs) => lhs.Subtract(rhs);
	public static JVector3L operator - (JVector3I lhs, JVector3L rhs) => rhs.SubtractFrom(lhs);
	public static JVector3L operator - (JVector3B lhs, JVector3L rhs) => rhs.SubtractFrom(lhs);
	public static JVector3  operator - (JVector3  lhs, JVector3L rhs) => rhs.SubtractFrom(lhs);
	public static JVector   operator - (JVector   lhs, JVector3L rhs) => rhs.SubtractFrom(lhs);
	public static double    operator * (JVector3L lhs, JVector3L rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector3L lhs, JVector3I rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector3L lhs, JVector3B rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector3L lhs, JVector3  rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector3L lhs, JVector   rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector3I lhs, JVector3L rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector3B lhs, JVector3L rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector3  lhs, JVector3L rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector   lhs, JVector3L rhs) => rhs.Multiply(lhs);
	
	public static JVector3D operator * (JVector3L lhs, double    rhs) => lhs.Multiply(rhs);
	public static JVector3D operator * (double    lhs, JVector3L rhs) => rhs.Multiply(lhs);
	public static JVector3D operator / (JVector3L lhs, double    rhs) => lhs.Divide(rhs);
	public static JVector3D operator / (double    lhs, JVector3L rhs) => rhs.DivideFrom(lhs);
	
	public static JVector3L operator * (JVector3L lhs, long      rhs) => lhs.Multiply(rhs);
	public static JVector3L operator * (long      lhs, JVector3L rhs) => rhs.Multiply(lhs);
	public static JVector3L operator / (JVector3L lhs, long      rhs) => lhs.Divide(rhs);
	public static JVector3L operator / (long      lhs, JVector3L rhs) => rhs.DivideFrom(lhs);
}