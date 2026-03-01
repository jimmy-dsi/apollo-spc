namespace Jimbl.JMath;

public struct JVector3B: JVector3<byte> {
	public byte X { get; set; }
	public byte Y { get; set; }
	public byte Z { get; set; }
	
	public static explicit operator JVector2B (JVector3B vec) => new(vec.X, vec.Y);
	public static explicit operator JVector2I (JVector3B vec) => new(vec.X, vec.Y);
	public static explicit operator JVector2L (JVector3B vec) => new(vec.X, vec.Y);
	public static explicit operator JVector2F (JVector3B vec) => new(vec.X, vec.Y);
	public static explicit operator JVector2D (JVector3B vec) => new(vec.X, vec.Y);
	
	public static implicit operator JVector3I (JVector3B vec) => new(vec.X, vec.Y, vec.Z);
	public static implicit operator JVector3L (JVector3B vec) => new(vec.X, vec.Y, vec.Z);
	public static implicit operator JVector3F (JVector3B vec) => new(vec.X, vec.Y, vec.Z);
	public static implicit operator JVector3D (JVector3B vec) => new(vec.X, vec.Y, vec.Z);
	
	public JVector3B(byte x, byte y, byte z) {
		X = x;
		Y = y;
		Z = z;
	}

	public byte this[int itemIndex] {
		get => ((JVector3<byte>) this)[itemIndex];
		set => ((JVector3<byte>) this)[itemIndex] = value;
	}
	
	public double Magnitude => ((JVector3<byte>) this).Magnitude;

	JVector3 JVector3.Negate() => Negate();
	
	public JVector3I Negate() {
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
		if (other is JVector2B v2b) {
			return Add(v2b);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.Add(b);
		}
	}
	
	public JVector3 Add(JVector3 other) {
		if (other is JVector3B v3b) {
			return Add(v3b);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.Add(b);
		}
	}
	
	public JVector3I Add(JVector2B other) {
		return Add((JVector3B) other);
	}
	
	public JVector3I Add(JVector3B other) {
		return new(X + other.X, Y + other.Y, Z + other.Z);
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
		if (other is JVector2B v2b) {
			return Subtract(v2b);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.Subtract(b);
		}
	}
	
	public JVector3 Subtract(JVector3 other) {
		if (other is JVector3B v3b) {
			return Subtract(v3b);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.Subtract(b);
		}
	}
	
	public JVector3I Subtract(JVector2B other) {
		return Subtract((JVector3B) other);
	}
	
	public JVector3I Subtract(JVector3B other) {
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
		if (other is JVector2B v2b) {
			return SubtractFrom(v2b);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.SubtractFrom(b);
		}
	}
	
	public JVector3 SubtractFrom(JVector3 other) {
		if (other is JVector3B v3b) {
			return SubtractFrom(v3b);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.SubtractFrom(b);
		}
	}
	
	public JVector3I SubtractFrom(JVector2B other) {
		return SubtractFrom((JVector3B) other);
	}
	
	public JVector3I SubtractFrom(JVector3B other) {
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
		if (other is JVector2B v2b) {
			return Multiply(v2b);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.Multiply(b);
		}
	}
	
	public double Multiply(JVector3 other) {
		if (other is JVector3B v3b) {
			return Multiply(v3b);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.Multiply(b);
		}
	}
	
	public double Multiply(JVector2B other) {
		return Multiply((JVector3B) other);
	}
	
	public double Multiply(JVector3B other) {
		return X * other.X + Y * other.Y + Z * other.Z;
	}
	
	JVector3 JVector3.Multiply(double other) => Multiply(other);
	
	public JVector3D Multiply(double other) {
		return new(X * other, Y * other, Z * other);
	}
	
	public JVector3F Multiply(float other) {
		return new(X * other, Y * other, Z * other);
	}
	
	public JVector3I Multiply(int other) {
		return new(X * other, Y * other, Z * other);
	}
	
	public JVector3L Multiply(long other) {
		return new(X * other, Y * other, Z * other);
	}
	
	JVector3 JVector3.Divide(double other) => Divide(other);
	
	public JVector3D Divide(double other) {
		return new(X / other, Y / other, Z / other);
	}
	
	public JVector3F Divide(float other) {
		return new(X / other, Y / other, Z / other);
	}
	
	public JVector3I Divide(int other) {
		return new(X / other, Y / other, Z / other);
	}
	
	public JVector3L Divide(long other) {
		return new(X / other, Y / other, Z / other);
	}
	
	JVector3 JVector3.DivideFrom(double other) => DivideFrom(other);
	
	public JVector3D DivideFrom(double other) {
		return new(other / X, other / Y, other / Z);
	}
	
	public JVector3F DivideFrom(float other) {
		return new(other / X, other / Y, other / Z);
	}
	
	public JVector3I DivideFrom(int other) {
		return new(other / X, other / Y, other / Z);
	}
	
	public JVector3L DivideFrom(long other) {
		return new(other / X, other / Y, other / Z);
	}
	
	// Unary
	public static JVector3B operator + (JVector3B self) => self;
	public static JVector3I operator - (JVector3B self) => self.Negate();
	
	// With vec2
	public static JVector3I operator + (JVector3B lhs, JVector2B rhs) => lhs.Add(rhs);
	public static JVector3  operator + (JVector3B lhs, JVector2  rhs) => lhs.Add(rhs);
	public static JVector3I operator + (JVector2B lhs, JVector3B rhs) => rhs.Add(lhs);
	public static JVector3  operator + (JVector2  lhs, JVector3B rhs) => rhs.Add(lhs);
	public static JVector3I operator - (JVector3B lhs, JVector2B rhs) => lhs.Subtract(rhs);
	public static JVector3  operator - (JVector3B lhs, JVector2  rhs) => lhs.Subtract(rhs);
	public static JVector3I operator - (JVector2B lhs, JVector3B rhs) => rhs.SubtractFrom(lhs);
	public static JVector3  operator - (JVector2  lhs, JVector3B rhs) => rhs.SubtractFrom(lhs);
	public static double    operator * (JVector3B lhs, JVector2B rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector3B lhs, JVector2  rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector2B lhs, JVector3B rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector2  lhs, JVector3B rhs) => rhs.Multiply(lhs);
	
	// With vec3
	public static JVector3I operator + (JVector3B lhs, JVector3B rhs) => lhs.Add(rhs);
	public static JVector3  operator + (JVector3B lhs, JVector3  rhs) => lhs.Add(rhs);
	public static JVector   operator + (JVector3B lhs, JVector   rhs) => lhs.Add(rhs);
	public static JVector3  operator + (JVector3  lhs, JVector3B rhs) => rhs.Add(lhs);
	public static JVector   operator + (JVector   lhs, JVector3B rhs) => rhs.Add(lhs);
	public static JVector3I operator - (JVector3B lhs, JVector3B rhs) => lhs.Subtract(rhs);
	public static JVector3  operator - (JVector3B lhs, JVector3  rhs) => lhs.Subtract(rhs);
	public static JVector   operator - (JVector3B lhs, JVector   rhs) => lhs.Subtract(rhs);
	public static JVector3  operator - (JVector3  lhs, JVector3B rhs) => rhs.SubtractFrom(lhs);
	public static JVector   operator - (JVector   lhs, JVector3B rhs) => rhs.SubtractFrom(lhs);
	public static double    operator * (JVector3B lhs, JVector3B rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector3B lhs, JVector3  rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector3B lhs, JVector   rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector3  lhs, JVector3B rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector   lhs, JVector3B rhs) => rhs.Multiply(lhs);
	
	public static JVector3D operator * (JVector3B lhs, double    rhs) => lhs.Multiply(rhs);
	public static JVector3D operator * (double    lhs, JVector3B rhs) => rhs.Multiply(lhs);
	public static JVector3D operator / (JVector3B lhs, double    rhs) => lhs.Divide(rhs);
	public static JVector3D operator / (double    lhs, JVector3B rhs) => rhs.DivideFrom(lhs);
	
	public static JVector3F operator * (JVector3B lhs, float     rhs) => lhs.Multiply(rhs);
	public static JVector3F operator * (float     lhs, JVector3B rhs) => rhs.Multiply(lhs);
	public static JVector3F operator / (JVector3B lhs, float     rhs) => lhs.Divide(rhs);
	public static JVector3F operator / (float     lhs, JVector3B rhs) => rhs.DivideFrom(lhs);
	
	public static JVector3I operator * (JVector3B lhs, int       rhs) => lhs.Multiply(rhs);
	public static JVector3I operator * (int       lhs, JVector3B rhs) => rhs.Multiply(lhs);
	public static JVector3I operator / (JVector3B lhs, int       rhs) => lhs.Divide(rhs);
	public static JVector3I operator / (int       lhs, JVector3B rhs) => rhs.DivideFrom(lhs);
	
	public static JVector3L operator * (JVector3B lhs, long      rhs) => lhs.Multiply(rhs);
	public static JVector3L operator * (long      lhs, JVector3B rhs) => rhs.Multiply(lhs);
	public static JVector3L operator / (JVector3B lhs, long      rhs) => lhs.Divide(rhs);
	public static JVector3L operator / (long      lhs, JVector3B rhs) => rhs.DivideFrom(lhs);
}