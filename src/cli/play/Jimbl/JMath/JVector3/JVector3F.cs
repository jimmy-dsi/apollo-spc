namespace Jimbl.JMath;

public struct JVector3F: JVector3<float> {
	public float X { get; set; }
	public float Y { get; set; }
	public float Z { get; set; }
	
	public static explicit operator JVector2B (JVector3F vec) => new((byte) vec.X, (byte) vec.Y);
	public static explicit operator JVector2I (JVector3F vec) => new((int)  vec.X, (int)  vec.Y);
	public static explicit operator JVector2L (JVector3F vec) => new((long) vec.X, (long) vec.Y);
	public static explicit operator JVector2F (JVector3F vec) => new(       vec.X,        vec.Y);
	public static explicit operator JVector2D (JVector3F vec) => new(       vec.X,        vec.Y);
	
	public static explicit operator JVector3B (JVector3F vec) => new((byte) vec.X, (byte) vec.Y, (byte) vec.Z);
	public static explicit operator JVector3I (JVector3F vec) => new((int)  vec.X, (int)  vec.Y, (int)  vec.Z);
	public static explicit operator JVector3L (JVector3F vec) => new((long) vec.X, (long) vec.Y, (long) vec.Z);
	public static implicit operator JVector3D (JVector3F vec) => new(       vec.X,        vec.Y,        vec.Z);
	
	public JVector3F(float x, float y, float z) {
		X = x;
		Y = y;
		Z = z;
	}

	public float this[int itemIndex] {
		get => ((JVector3<float>) this)[itemIndex];
		set => ((JVector3<float>) this)[itemIndex] = value;
	}
	
	public double Magnitude => ((JVector3<float>) this).Magnitude;
	
	JVector3 JVector3.Negate() => Negate();
	
	public JVector3F Negate() {
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
		if (other is JVector2F v2f) {
			return Add(v2f);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.Add(b);
		}
	}
	
	public JVector3 Add(JVector3 other) {
		if (other is JVector3F v3f) {
			return Add(v3f);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.Add(b);
		}
	}
	
	public JVector3F Add(JVector2F other) {
		return Add((JVector3F) other);
	}
	
	public JVector3F Add(JVector3F other) {
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
		if (other is JVector2F v2f) {
			return Subtract(v2f);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.Subtract(b);
		}
	}
	
	public JVector3 Subtract(JVector3 other) {
		if (other is JVector3F v3f) {
			return Subtract(v3f);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.Subtract(b);
		}
	}
	
	public JVector3F Subtract(JVector2F other) {
		return Subtract((JVector3F) other);
	}
	
	public JVector3F Subtract(JVector3F other) {
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
		if (other is JVector2F v2f) {
			return SubtractFrom(v2f);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.SubtractFrom(b);
		}
	}
	
	public JVector3 SubtractFrom(JVector3 other) {
		if (other is JVector3F v3f) {
			return SubtractFrom(v3f);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.SubtractFrom(b);
		}
	}
	
	public JVector3F SubtractFrom(JVector2F other) {
		return SubtractFrom((JVector3F) other);
	}
	
	public JVector3F SubtractFrom(JVector3F other) {
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
		if (other is JVector2F v2f) {
			return Multiply(v2f);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.Multiply(b);
		}
	}
	
	public double Multiply(JVector3 other) {
		if (other is JVector3F v3f) {
			return Multiply(v3f);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.Multiply(b);
		}
	}
	
	public double Multiply(JVector2F other) {
		return Multiply((JVector3F) other);
	}
	
	public double Multiply(JVector3F other) {
		return X * other.X + Y * other.Y + Z * other.Z;
	}
	
	JVector3 JVector3.Multiply(double other) => Multiply(other);
	
	public JVector3D Multiply(double other) {
		return new(X * other, Y * other, Z * other);
	}
	
	public JVector3D Multiply(int other) {
		return new(X * other, Y * other, Z * other);
	}
	
	public JVector3F Multiply(float other) {
		return new(X * other, Y * other, Z * other);
	}
	
	JVector3 JVector3.Divide(double other) => Divide(other);
	
	public JVector3D Divide(double other) {
		return new(X / other, Y / other, Z / other);
	}
	
	public JVector3D Divide(int other) {
		return new(X / other, Y / other, Z / other);
	}
	
	public JVector3F Divide(float other) {
		return new(X / other, Y / other, Z / other);
	}
	
	JVector3 JVector3.DivideFrom(double other) => DivideFrom(other);
	
	public JVector3D DivideFrom(double other) {
		return new(other / X, other / Y, other / Z);
	}
	
	public JVector3D DivideFrom(int other) {
		return new(other / X, other / Y, other / Z);
	}
	
	public JVector3F DivideFrom(float other) {
		return new(other / X, other / Y, other / Z);
	}
	
	// Unary
	public static JVector3F operator + (JVector3F self) => self;
	public static JVector3F operator - (JVector3F self) => self.Negate();
	
	// With vec2
	public static JVector3F operator + (JVector3F lhs, JVector2F rhs) => lhs.Add(rhs);
	public static JVector3F operator + (JVector3F lhs, JVector2B rhs) => lhs.Add(rhs);
	public static JVector3  operator + (JVector3F lhs, JVector2  rhs) => lhs.Add(rhs);
	public static JVector3F operator + (JVector2F lhs, JVector3F rhs) => rhs.Add(lhs);
	public static JVector3F operator + (JVector2B lhs, JVector3F rhs) => rhs.Add(lhs);
	public static JVector3  operator + (JVector2  lhs, JVector3F rhs) => rhs.Add(lhs);
	public static JVector3F operator - (JVector3F lhs, JVector2F rhs) => lhs.Subtract(rhs);
	public static JVector3F operator - (JVector3F lhs, JVector2B rhs) => lhs.Subtract(rhs);
	public static JVector3  operator - (JVector3F lhs, JVector2  rhs) => lhs.Subtract(rhs);
	public static JVector3F operator - (JVector2F lhs, JVector3F rhs) => rhs.SubtractFrom(lhs);
	public static JVector3F operator - (JVector2B lhs, JVector3F rhs) => rhs.SubtractFrom(lhs);
	public static JVector3  operator - (JVector2  lhs, JVector3F rhs) => rhs.SubtractFrom(lhs);
	public static double    operator * (JVector3F lhs, JVector2F rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector3F lhs, JVector2B rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector3F lhs, JVector2  rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector2F lhs, JVector3F rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector2B lhs, JVector3F rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector2  lhs, JVector3F rhs) => rhs.Multiply(lhs);
	
	// With vec3
	public static JVector3F operator + (JVector3F lhs, JVector3F rhs) => lhs.Add(rhs);
	public static JVector3F operator + (JVector3F lhs, JVector3B rhs) => lhs.Add(rhs);
	public static JVector3  operator + (JVector3F lhs, JVector3  rhs) => lhs.Add(rhs);
	public static JVector   operator + (JVector3F lhs, JVector   rhs) => lhs.Add(rhs);
	public static JVector3F operator + (JVector3B lhs, JVector3F rhs) => rhs.Add(lhs);
	public static JVector3  operator + (JVector3  lhs, JVector3F rhs) => rhs.Add(lhs);
	public static JVector   operator + (JVector   lhs, JVector3F rhs) => rhs.Add(lhs);
	public static JVector3F operator - (JVector3F lhs, JVector3F rhs) => lhs.Subtract(rhs);
	public static JVector3F operator - (JVector3F lhs, JVector3B rhs) => lhs.Subtract(rhs);
	public static JVector3  operator - (JVector3F lhs, JVector3  rhs) => lhs.Subtract(rhs);
	public static JVector   operator - (JVector3F lhs, JVector   rhs) => lhs.Subtract(rhs);
	public static JVector3F operator - (JVector3B lhs, JVector3F rhs) => rhs.SubtractFrom(lhs);
	public static JVector3  operator - (JVector3  lhs, JVector3F rhs) => rhs.SubtractFrom(lhs);
	public static JVector   operator - (JVector   lhs, JVector3F rhs) => rhs.SubtractFrom(lhs);
	public static double    operator * (JVector3F lhs, JVector3F rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector3F lhs, JVector3B rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector3F lhs, JVector3  rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector3F lhs, JVector   rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector3B lhs, JVector3F rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector3  lhs, JVector3F rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector   lhs, JVector3F rhs) => rhs.Multiply(lhs);
	
	public static JVector3D operator * (JVector3F lhs, double    rhs) => lhs.Multiply(rhs);
	public static JVector3D operator * (double    lhs, JVector3F rhs) => rhs.Multiply(lhs);
	public static JVector3D operator / (JVector3F lhs, double    rhs) => lhs.Divide(rhs);
	public static JVector3D operator / (double    lhs, JVector3F rhs) => rhs.DivideFrom(lhs);
	
	public static JVector3D operator * (JVector3F lhs, int       rhs) => lhs.Multiply(rhs);
	public static JVector3D operator * (int       lhs, JVector3F rhs) => rhs.Multiply(lhs);
	public static JVector3D operator / (JVector3F lhs, int       rhs) => lhs.Divide(rhs);
	public static JVector3D operator / (int       lhs, JVector3F rhs) => rhs.DivideFrom(lhs);
	
	public static JVector3F operator * (JVector3F lhs, float     rhs) => lhs.Multiply(rhs);
	public static JVector3F operator * (float     lhs, JVector3F rhs) => rhs.Multiply(lhs);
	public static JVector3F operator / (JVector3F lhs, float     rhs) => lhs.Divide(rhs);
	public static JVector3F operator / (float     lhs, JVector3F rhs) => rhs.DivideFrom(lhs);
}