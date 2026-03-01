namespace Jimbl.JMath;

public struct JVector3D: JVector3<double> {
	public double X { get; set; }
	public double Y { get; set; }
	public double Z { get; set; }
	
	public static explicit operator JVector2B (JVector3D vec) => new((byte)  vec.X, (byte)  vec.Y);
	public static explicit operator JVector2I (JVector3D vec) => new((int)   vec.X, (int)   vec.Y);
	public static explicit operator JVector2L (JVector3D vec) => new((long)  vec.X, (long)  vec.Y);
	public static explicit operator JVector2F (JVector3D vec) => new((float) vec.X, (float) vec.Y);
	public static explicit operator JVector2D (JVector3D vec) => new(        vec.X,         vec.Y);
	
	public static explicit operator JVector3B (JVector3D vec) => new((byte)  vec.X, (byte)  vec.Y, (byte)  vec.Z);
	public static explicit operator JVector3I (JVector3D vec) => new((int)   vec.X, (int)   vec.Y, (int)   vec.Z);
	public static explicit operator JVector3L (JVector3D vec) => new((long)  vec.X, (long)  vec.Y, (long)  vec.Z);
	public static explicit operator JVector3F (JVector3D vec) => new((float) vec.X, (float) vec.Y, (float) vec.Z);
	
	public JVector3D(double x, double y, double z) {
		X = x;
		Y = y;
		Z = z;
	}

	public double this[int itemIndex] {
		get => ((JVector3<double>) this)[itemIndex];
		set => ((JVector3<double>) this)[itemIndex] = value;
	}
	
	public double Magnitude => ((JVector3<double>) this).Magnitude;
	
	JVector3 JVector3.Negate() => Negate();
	
	public JVector3D Negate() {
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
		if (other is JVector2D v2d) {
			return Add(v2d);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.Add(b);
		}
	}
	
	public JVector3 Add(JVector3 other) {
		if (other is JVector3D v3d) {
			return Add(v3d);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.Add(b);
		}
	}
	
	public JVector3D Add(JVector2D other) {
		return Add((JVector3D) other);
	}
	
	public JVector3D Add(JVector3D other) {
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
		if (other is JVector2D v2d) {
			return Subtract(v2d);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.Subtract(b);
		}
	}
	
	public JVector3 Subtract(JVector3 other) {
		if (other is JVector3D v3d) {
			return Subtract(v3d);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.Subtract(b);
		}
	}
	
	public JVector3D Subtract(JVector2D other) {
		return Subtract((JVector3D) other);
	}
	
	public JVector3D Subtract(JVector3D other) {
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
		if (other is JVector2D v2d) {
			return SubtractFrom(v2d);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.SubtractFrom(b);
		}
	}
	
	public JVector3 SubtractFrom(JVector3 other) {
		if (other is JVector3D v3d) {
			return SubtractFrom(v3d);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.SubtractFrom(b);
		}
	}
	
	public JVector3D SubtractFrom(JVector2D other) {
		return SubtractFrom((JVector3D) other);
	}
	
	public JVector3D SubtractFrom(JVector3D other) {
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
		if (other is JVector2D v2d) {
			return Multiply(v2d);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.Multiply(b);
		}
	}
	
	public double Multiply(JVector3 other) {
		if (other is JVector3D v3d) {
			return Multiply(v3d);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.Multiply(b);
		}
	}
	
	public double Multiply(JVector2D other) {
		return Multiply((JVector3D) other);
	}
	
	public double Multiply(JVector3D other) {
		return X * other.X + Y * other.Y + Z * other.Z;
	}
	
	JVector3 JVector3.Multiply(double other) => Multiply(other);
	
	public JVector3D Multiply(double other) {
		return new(X * other, Y * other, Z * other);
	}
	
	JVector3 JVector3.Divide(double other) => Divide(other);
	
	public JVector3D Divide(double other) {
		return new(X / other, Y / other, Z / other);
	}
	
	JVector3 JVector3.DivideFrom(double other) => DivideFrom(other);
	
	public JVector3D DivideFrom(double other) {
		return new(other / X, other / Y, other / Z);
	}
	
	// Unary
	public static JVector3D operator + (JVector3D self) => self;
	public static JVector3D operator - (JVector3D self) => self.Negate();
	
	// With vec2
	public static JVector3D operator + (JVector3D lhs, JVector2D rhs) => lhs.Add(rhs);
	public static JVector3D operator + (JVector3D lhs, JVector2F rhs) => lhs.Add(rhs);
	public static JVector3D operator + (JVector3D lhs, JVector2I rhs) => lhs.Add(rhs);
	public static JVector3D operator + (JVector3D lhs, JVector2B rhs) => lhs.Add(rhs);
	public static JVector3  operator + (JVector3D lhs, JVector2  rhs) => lhs.Add(rhs);
	public static JVector3D operator + (JVector2D lhs, JVector3D rhs) => rhs.Add(lhs);
	public static JVector3D operator + (JVector2F lhs, JVector3D rhs) => rhs.Add(lhs);
	public static JVector3D operator + (JVector2I lhs, JVector3D rhs) => rhs.Add(lhs);
	public static JVector3D operator + (JVector2B lhs, JVector3D rhs) => rhs.Add(lhs);
	public static JVector3  operator + (JVector2  lhs, JVector3D rhs) => rhs.Add(lhs);
	public static JVector3D operator - (JVector3D lhs, JVector2D rhs) => lhs.Subtract(rhs);
	public static JVector3D operator - (JVector3D lhs, JVector2F rhs) => lhs.Subtract(rhs);
	public static JVector3D operator - (JVector3D lhs, JVector2I rhs) => lhs.Subtract(rhs);
	public static JVector3D operator - (JVector3D lhs, JVector2B rhs) => lhs.Subtract(rhs);
	public static JVector3  operator - (JVector3D lhs, JVector2  rhs) => lhs.Subtract(rhs);
	public static JVector3D operator - (JVector2D lhs, JVector3D rhs) => rhs.SubtractFrom(lhs);
	public static JVector3D operator - (JVector2F lhs, JVector3D rhs) => rhs.SubtractFrom(lhs);
	public static JVector3D operator - (JVector2I lhs, JVector3D rhs) => rhs.SubtractFrom(lhs);
	public static JVector3D operator - (JVector2B lhs, JVector3D rhs) => rhs.SubtractFrom(lhs);
	public static JVector3  operator - (JVector2  lhs, JVector3D rhs) => rhs.SubtractFrom(lhs);
	public static double    operator * (JVector3D lhs, JVector2D rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector3D lhs, JVector2F rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector3D lhs, JVector2I rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector3D lhs, JVector2B rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector3D lhs, JVector2  rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector2D lhs, JVector3D rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector2F lhs, JVector3D rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector2I lhs, JVector3D rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector2B lhs, JVector3D rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector2  lhs, JVector3D rhs) => rhs.Multiply(lhs);
	
	// With vec3
	public static JVector3D operator + (JVector3D lhs, JVector3D rhs) => lhs.Add(rhs);
	public static JVector3D operator + (JVector3D lhs, JVector3F rhs) => lhs.Add(rhs);
	public static JVector3D operator + (JVector3D lhs, JVector3I rhs) => lhs.Add(rhs);
	public static JVector3D operator + (JVector3D lhs, JVector3B rhs) => lhs.Add(rhs);
	public static JVector3  operator + (JVector3D lhs, JVector3  rhs) => lhs.Add(rhs);
	public static JVector   operator + (JVector3D lhs, JVector   rhs) => lhs.Add(rhs);
	public static JVector3D operator + (JVector3F lhs, JVector3D rhs) => rhs.Add(lhs);
	public static JVector3D operator + (JVector3I lhs, JVector3D rhs) => rhs.Add(lhs);
	public static JVector3D operator + (JVector3B lhs, JVector3D rhs) => rhs.Add(lhs);
	public static JVector3  operator + (JVector3  lhs, JVector3D rhs) => rhs.Add(lhs);
	public static JVector   operator + (JVector   lhs, JVector3D rhs) => rhs.Add(lhs);
	public static JVector3D operator - (JVector3D lhs, JVector3D rhs) => lhs.Subtract(rhs);
	public static JVector3D operator - (JVector3D lhs, JVector3F rhs) => lhs.Subtract(rhs);
	public static JVector3D operator - (JVector3D lhs, JVector3I rhs) => lhs.Subtract(rhs);
	public static JVector3D operator - (JVector3D lhs, JVector3B rhs) => lhs.Subtract(rhs);
	public static JVector3  operator - (JVector3D lhs, JVector3  rhs) => lhs.Subtract(rhs);
	public static JVector   operator - (JVector3D lhs, JVector   rhs) => lhs.Subtract(rhs);
	public static JVector3D operator - (JVector3F lhs, JVector3D rhs) => rhs.SubtractFrom(lhs);
	public static JVector3D operator - (JVector3I lhs, JVector3D rhs) => rhs.SubtractFrom(lhs);
	public static JVector3D operator - (JVector3B lhs, JVector3D rhs) => rhs.SubtractFrom(lhs);
	public static JVector3  operator - (JVector3  lhs, JVector3D rhs) => rhs.SubtractFrom(lhs);
	public static JVector   operator - (JVector   lhs, JVector3D rhs) => rhs.SubtractFrom(lhs);
	public static double    operator * (JVector3D lhs, JVector3D rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector3D lhs, JVector3F rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector3D lhs, JVector3I rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector3D lhs, JVector3B rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector3D lhs, JVector3  rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector3D lhs, JVector   rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector3F lhs, JVector3D rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector3I lhs, JVector3D rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector3B lhs, JVector3D rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector3  lhs, JVector3D rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector   lhs, JVector3D rhs) => rhs.Multiply(lhs);
	
	public static JVector3D operator * (JVector3D lhs, double    rhs) => lhs.Multiply(rhs);
	public static JVector3D operator * (double    lhs, JVector3D rhs) => rhs.Multiply(lhs);
	public static JVector3D operator / (JVector3D lhs, double    rhs) => lhs.Divide(rhs);
	public static JVector3D operator / (double    lhs, JVector3D rhs) => rhs.DivideFrom(lhs);
}