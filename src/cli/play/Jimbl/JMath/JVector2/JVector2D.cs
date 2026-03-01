namespace Jimbl.JMath;

public struct JVector2D: JVector2<double> {
	public double X { get; set; }
	public double Y { get; set; }
	
	public static explicit operator JVector2B (JVector2D vec) => new((byte)  vec.X, (byte)  vec.Y);
	public static explicit operator JVector2I (JVector2D vec) => new((int)   vec.X, (int)   vec.Y);
	public static explicit operator JVector2L (JVector2D vec) => new((long)  vec.X, (long)  vec.Y);
	public static explicit operator JVector2F (JVector2D vec) => new((float) vec.X, (float) vec.Y);
	
	public static explicit operator JVector3B (JVector2D vec) => new((byte)  vec.X, (byte)  vec.Y, 0);
	public static explicit operator JVector3I (JVector2D vec) => new((int)   vec.X, (int)   vec.Y, 0);
	public static explicit operator JVector3L (JVector2D vec) => new((long)  vec.X, (long)  vec.Y, 0);
	public static explicit operator JVector3F (JVector2D vec) => new((float) vec.X, (float) vec.Y, 0);
	public static implicit operator JVector3D (JVector2D vec) => new(        vec.X,         vec.Y, 0);
	
	public JVector2D(double x, double y) {
		X = x;
		Y = y;
	}

	public double this[int itemIndex] {
		get => ((JVector2<double>) this)[itemIndex];
		set => ((JVector2<double>) this)[itemIndex] = value;
	}
	
	public double Magnitude => ((JVector2<double>) this).Magnitude;
	
	JVector2 JVector2.Negate() => Negate();
	
	public JVector2D Negate() {
		return new(-X, -Y);
	}
	
	public JVector Add(JVector other) {
		if (other is JVector2 v2) {
			return Add(v2);
		}
		else {
			return other.Add(this);
		}
	}
	
	public JVector2 Add(JVector2 other) {
		if (other is JVector2D v2d) {
			return Add(v2d);
		}
		else {
			var (a, b) = JVector2.Promote(this, other);
			return a.Add(b);
		}
	}
	
	public JVector2D Add(JVector2D other) {
		return new(X + other.X, Y + other.Y);
	}
	
	public JVector Subtract(JVector other) {
		if (other is JVector2 v2) {
			return Subtract(v2);
		}
		else {
			return other.SubtractFrom(this);
		}
	}
	
	public JVector2 Subtract(JVector2 other) {
		if (other is JVector2D v2d) {
			return Subtract(v2d);
		}
		else {
			var (a, b) = JVector2.Promote(this, other);
			return a.Subtract(b);
		}
	}
	
	public JVector2D Subtract(JVector2D other) {
		return new(X - other.X, Y - other.Y);
	}
	
	public JVector SubtractFrom(JVector other) {
		if (other is JVector2 v2) {
			return SubtractFrom(v2);
		}
		else {
			return other.Subtract(this);
		}
	}
	
	public JVector2 SubtractFrom(JVector2 other) {
		if (other is JVector2D v2d) {
			return SubtractFrom(v2d);
		}
		else {
			var (a, b) = JVector2.Promote(this, other);
			return a.SubtractFrom(b);
		}
	}
	
	public JVector2D SubtractFrom(JVector2D other) {
		return new(other.X - X, other.Y - Y);
	}
	
	public double Multiply(JVector other) {
		if (other is JVector2 v2) {
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
			var (a, b) = JVector2.Promote(this, other);
			return a.Multiply(b);
		}
	}
	
	public double Multiply(JVector2D other) {
		return X * other.X + Y * other.Y;
	}
	
	JVector2 JVector2.Multiply(double other) => Multiply(other);
	
	public JVector2D Multiply(double other) {
		return new(X * other, Y * other);
	}
	
	JVector2 JVector2.Divide(double other) => Divide(other);
	
	public JVector2D Divide(double other) {
		return new(X / other, Y / other);
	}
	
	JVector2 JVector2.DivideFrom(double other) => DivideFrom(other);
	
	public JVector2D DivideFrom(double other) {
		return new(other / X, other / Y);
	}
	
	// Operators
	public static JVector2D operator + (JVector2D self) => self;
	public static JVector2D operator - (JVector2D self) => self.Negate();
	
	public static JVector2D operator + (JVector2D lhs, JVector2D rhs) => lhs.Add(rhs);
	public static JVector2D operator + (JVector2D lhs, JVector2F rhs) => lhs.Add(rhs);
	public static JVector2D operator + (JVector2D lhs, JVector2I rhs) => lhs.Add(rhs);
	public static JVector2D operator + (JVector2D lhs, JVector2B rhs) => lhs.Add(rhs);
	public static JVector2  operator + (JVector2D lhs, JVector2  rhs) => lhs.Add(rhs);
	public static JVector   operator + (JVector2D lhs, JVector   rhs) => lhs.Add(rhs);
	public static JVector2D operator + (JVector2F lhs, JVector2D rhs) => rhs.Add(lhs);
	public static JVector2D operator + (JVector2I lhs, JVector2D rhs) => rhs.Add(lhs);
	public static JVector2D operator + (JVector2B lhs, JVector2D rhs) => rhs.Add(lhs);
	public static JVector2  operator + (JVector2  lhs, JVector2D rhs) => rhs.Add(lhs);
	public static JVector   operator + (JVector   lhs, JVector2D rhs) => rhs.Add(lhs);
	public static JVector2D operator - (JVector2D lhs, JVector2D rhs) => lhs.Subtract(rhs);
	public static JVector2D operator - (JVector2D lhs, JVector2F rhs) => lhs.Subtract(rhs);
	public static JVector2D operator - (JVector2D lhs, JVector2I rhs) => lhs.Subtract(rhs);
	public static JVector2D operator - (JVector2D lhs, JVector2B rhs) => lhs.Subtract(rhs);
	public static JVector2  operator - (JVector2D lhs, JVector2  rhs) => lhs.Subtract(rhs);
	public static JVector   operator - (JVector2D lhs, JVector   rhs) => lhs.Subtract(rhs);
	public static JVector2D operator - (JVector2F lhs, JVector2D rhs) => rhs.SubtractFrom(lhs);
	public static JVector2D operator - (JVector2I lhs, JVector2D rhs) => rhs.SubtractFrom(lhs);
	public static JVector2D operator - (JVector2B lhs, JVector2D rhs) => rhs.SubtractFrom(lhs);
	public static JVector2  operator - (JVector2  lhs, JVector2D rhs) => rhs.SubtractFrom(lhs);
	public static JVector   operator - (JVector   lhs, JVector2D rhs) => rhs.SubtractFrom(lhs);
	public static double    operator * (JVector2D lhs, JVector2D rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector2D lhs, JVector2F rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector2D lhs, JVector2I rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector2D lhs, JVector2B rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector2D lhs, JVector2  rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector2D lhs, JVector   rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector2F lhs, JVector2D rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector2I lhs, JVector2D rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector2B lhs, JVector2D rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector2  lhs, JVector2D rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector   lhs, JVector2D rhs) => rhs.Multiply(lhs);
	
	public static JVector2D operator * (JVector2D lhs, double    rhs) => lhs.Multiply(rhs);
	public static JVector2D operator * (double    lhs, JVector2D rhs) => rhs.Multiply(lhs);
	public static JVector2D operator / (JVector2D lhs, double    rhs) => lhs.Divide(rhs);
	public static JVector2D operator / (double    lhs, JVector2D rhs) => rhs.DivideFrom(lhs);
}