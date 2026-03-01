namespace Jimbl.JMath;

public struct JVector2F: JVector2<float> {
	public float X { get; set; }
	public float Y { get; set; }
	
	public static explicit operator JVector2B (JVector2F vec) => new((byte) vec.X, (byte) vec.Y);
	public static explicit operator JVector2I (JVector2F vec) => new((int)  vec.X, (int)  vec.Y);
	public static explicit operator JVector2L (JVector2F vec) => new((long) vec.X, (long) vec.Y);
	public static implicit operator JVector2D (JVector2F vec) => new(       vec.X,        vec.Y);
	
	public static explicit operator JVector3B (JVector2F vec) => new((byte) vec.X, (byte) vec.Y, 0);
	public static explicit operator JVector3I (JVector2F vec) => new((int)  vec.X, (int)  vec.Y, 0);
	public static explicit operator JVector3L (JVector2F vec) => new((long) vec.X, (long) vec.Y, 0);
	public static implicit operator JVector3F (JVector2F vec) => new(       vec.X,        vec.Y, 0);
	public static implicit operator JVector3D (JVector2F vec) => new(       vec.X,        vec.Y, 0);
	
	public JVector2F(float x, float y) {
		X = x;
		Y = y;
	}

	public float this[int itemIndex] {
		get => ((JVector2<float>) this)[itemIndex];
		set => ((JVector2<float>) this)[itemIndex] = value;
	}
	
	public double Magnitude => ((JVector2<float>) this).Magnitude;
	
	JVector2 JVector2.Negate() => Negate();
	
	public JVector2F Negate() {
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
		if (other is JVector2F v2f) {
			return Add(v2f);
		}
		else {
			var (a, b) = JVector2.Promote(this, other);
			return a.Add(b);
		}
	}
	
	public JVector2F Add(JVector2F other) {
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
		if (other is JVector2F v2f) {
			return Subtract(v2f);
		}
		else {
			var (a, b) = JVector2.Promote(this, other);
			return a.Subtract(b);
		}
	}
	
	public JVector2F Subtract(JVector2F other) {
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
		if (other is JVector2F v2f) {
			return SubtractFrom(v2f);
		}
		else {
			var (a, b) = JVector2.Promote(this, other);
			return a.SubtractFrom(b);
		}
	}
	
	public JVector2F SubtractFrom(JVector2F other) {
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
		if (other is JVector2F v2f) {
			return Multiply(v2f);
		}
		else {
			var (a, b) = JVector2.Promote(this, other);
			return a.Multiply(b);
		}
	}
	
	public double Multiply(JVector2F other) {
		return X * other.X + Y * other.Y;
	}
	
	JVector2 JVector2.Multiply(double other) => Multiply(other);
	
	public JVector2D Multiply(double other) {
		return new(X * other, Y * other);
	}
	
	public JVector2D Multiply(int other) {
		return new(X * other, Y * other);
	}
	
	public JVector2F Multiply(float other) {
		return new(X * other, Y * other);
	}
	
	JVector2 JVector2.Divide(double other) => Divide(other);
	
	public JVector2D Divide(double other) {
		return new(X / other, Y / other);
	}
	
	public JVector2D Divide(int other) {
		return new(X / other, Y / other);
	}
	
	public JVector2F Divide(float other) {
		return new(X / other, Y / other);
	}
	
	JVector2 JVector2.DivideFrom(double other) => DivideFrom(other);
	
	public JVector2D DivideFrom(double other) {
		return new(other / X, other / Y);
	}
	
	public JVector2D DivideFrom(int other) {
		return new(other / X, other / Y);
	}
	
	public JVector2F DivideFrom(float other) {
		return new(other / X, other / Y);
	}
	
	// Operators
	public static JVector2F operator + (JVector2F self) => self;
	public static JVector2F operator - (JVector2F self) => self.Negate();
	
	public static JVector2F operator + (JVector2F lhs, JVector2F rhs) => lhs.Add(rhs);
	public static JVector2F operator + (JVector2F lhs, JVector2B rhs) => lhs.Add(rhs);
	public static JVector2  operator + (JVector2F lhs, JVector2  rhs) => lhs.Add(rhs);
	public static JVector   operator + (JVector2F lhs, JVector   rhs) => lhs.Add(rhs);
	public static JVector2F operator + (JVector2B lhs, JVector2F rhs) => rhs.Add(lhs);
	public static JVector2  operator + (JVector2  lhs, JVector2F rhs) => rhs.Add(lhs);
	public static JVector   operator + (JVector   lhs, JVector2F rhs) => rhs.Add(lhs);
	public static JVector2F operator - (JVector2F lhs, JVector2F rhs) => lhs.Subtract(rhs);
	public static JVector2F operator - (JVector2F lhs, JVector2B rhs) => lhs.Subtract(rhs);
	public static JVector2  operator - (JVector2F lhs, JVector2  rhs) => lhs.Subtract(rhs);
	public static JVector   operator - (JVector2F lhs, JVector   rhs) => lhs.Subtract(rhs);
	public static JVector2F operator - (JVector2B lhs, JVector2F rhs) => rhs.SubtractFrom(lhs);
	public static JVector2  operator - (JVector2  lhs, JVector2F rhs) => rhs.SubtractFrom(lhs);
	public static JVector   operator - (JVector   lhs, JVector2F rhs) => rhs.SubtractFrom(lhs);
	public static double    operator * (JVector2F lhs, JVector2F rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector2F lhs, JVector2B rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector2F lhs, JVector2  rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector2F lhs, JVector   rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector2B lhs, JVector2F rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector2  lhs, JVector2F rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector   lhs, JVector2F rhs) => rhs.Multiply(lhs);
	
	public static JVector2D operator * (JVector2F lhs, double    rhs) => lhs.Multiply(rhs);
	public static JVector2D operator * (double    lhs, JVector2F rhs) => rhs.Multiply(lhs);
	public static JVector2D operator / (JVector2F lhs, double    rhs) => lhs.Divide(rhs);
	public static JVector2D operator / (double    lhs, JVector2F rhs) => rhs.DivideFrom(lhs);
	
	public static JVector2D operator * (JVector2F lhs, int       rhs) => lhs.Multiply(rhs);
	public static JVector2D operator * (int       lhs, JVector2F rhs) => rhs.Multiply(lhs);
	public static JVector2D operator / (JVector2F lhs, int       rhs) => lhs.Divide(rhs);
	public static JVector2D operator / (int       lhs, JVector2F rhs) => rhs.DivideFrom(lhs);
	
	public static JVector2F operator * (JVector2F lhs, float     rhs) => lhs.Multiply(rhs);
	public static JVector2F operator * (float     lhs, JVector2F rhs) => rhs.Multiply(lhs);
	public static JVector2F operator / (JVector2F lhs, float     rhs) => lhs.Divide(rhs);
	public static JVector2F operator / (float     lhs, JVector2F rhs) => rhs.DivideFrom(lhs);
}