namespace Jimbl.JMath;

public struct JVector2L: JVector2<long> {
	public long X { get; set; }
	public long Y { get; set; }
	
	public static explicit operator JVector2B (JVector2L vec) => new((byte) vec.X, (byte) vec.Y);
	public static explicit operator JVector2I (JVector2L vec) => new((int)  vec.X, (int)  vec.Y);
	public static explicit operator JVector2F (JVector2L vec) => new(       vec.X,        vec.Y);
	public static implicit operator JVector2D (JVector2L vec) => new(       vec.X,        vec.Y);
	
	public static explicit operator JVector3B (JVector2L vec) => new((byte) vec.X, (byte) vec.Y, 0);
	public static explicit operator JVector3I (JVector2L vec) => new((int)  vec.X, (int)  vec.Y, 0);
	public static implicit operator JVector3L (JVector2L vec) => new(       vec.X,        vec.Y, 0);
	public static explicit operator JVector3F (JVector2L vec) => new(       vec.X,        vec.Y, 0);
	public static implicit operator JVector3D (JVector2L vec) => new(       vec.X,        vec.Y, 0);
	
	public JVector2L(long x, long y) {
		X = x;
		Y = y;
	}

	public long this[int itemIndex] {
		get => ((JVector2<long>) this)[itemIndex];
		set => ((JVector2<long>) this)[itemIndex] = value;
	}
	
	public double Magnitude => ((JVector2<long>) this).Magnitude;
	
	JVector2 JVector2.Negate() => Negate();
	
	public JVector2L Negate() {
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
		if (other is JVector2L v2L) {
			return Add(v2L);
		}
		else {
			var (a, b) = JVector2.Promote(this, other);
			return a.Add(b);
		}
	}
	
	public JVector2L Add(JVector2L other) {
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
		if (other is JVector2L v2L) {
			return Subtract(v2L);
		}
		else {
			var (a, b) = JVector2.Promote(this, other);
			return a.Subtract(b);
		}
	}
	
	public JVector2L Subtract(JVector2L other) {
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
		if (other is JVector2L v2L) {
			return SubtractFrom(v2L);
		}
		else {
			var (a, b) = JVector2.Promote(this, other);
			return a.SubtractFrom(b);
		}
	}
	
	public JVector2L SubtractFrom(JVector2L other) {
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
		if (other is JVector2L v2L) {
			return Multiply(v2L);
		}
		else {
			var (a, b) = JVector2.Promote(this, other);
			return a.Multiply(b);
		}
	}
	
	public double Multiply(JVector2L other) {
		return X * other.X + Y * other.Y;
	}
	
	JVector2 JVector2.Multiply(double other) => Multiply(other);
	
	public JVector2D Multiply(double other) {
		return new(X * other, Y * other);
	}
	
	public JVector2L Multiply(long other) {
		return new(X * other, Y * other);
	}
	
	JVector2 JVector2.Divide(double other) => Divide(other);
	
	public JVector2D Divide(double other) {
		return new(X / other, Y / other);
	}
	
	public JVector2L Divide(long other) {
		return new(X / other, Y / other);
	}
	
	JVector2 JVector2.DivideFrom(double other) => DivideFrom(other);
	
	public JVector2D DivideFrom(double other) {
		return new(other / X, other / Y);
	}
	
	public JVector2L DivideFrom(long other) {
		return new(other / X, other / Y);
	}
	
	// Operators
	public static JVector2L operator + (JVector2L self) => self;
	public static JVector2L operator - (JVector2L self) => self.Negate();
	
	public static JVector2L operator + (JVector2L lhs, JVector2L rhs) => lhs.Add(rhs);
	public static JVector2L operator + (JVector2L lhs, JVector2I rhs) => lhs.Add(rhs);
	public static JVector2L operator + (JVector2L lhs, JVector2B rhs) => lhs.Add(rhs);
	public static JVector2  operator + (JVector2L lhs, JVector2  rhs) => lhs.Add(rhs);
	public static JVector   operator + (JVector2L lhs, JVector   rhs) => lhs.Add(rhs);
	public static JVector2L operator + (JVector2I lhs, JVector2L rhs) => rhs.Add(lhs);
	public static JVector2L operator + (JVector2B lhs, JVector2L rhs) => rhs.Add(lhs);
	public static JVector2  operator + (JVector2  lhs, JVector2L rhs) => rhs.Add(lhs);
	public static JVector   operator + (JVector   lhs, JVector2L rhs) => rhs.Add(lhs);
	public static JVector2L operator - (JVector2L lhs, JVector2L rhs) => lhs.Subtract(rhs);
	public static JVector2L operator - (JVector2L lhs, JVector2I rhs) => lhs.Subtract(rhs);
	public static JVector2L operator - (JVector2L lhs, JVector2B rhs) => lhs.Subtract(rhs);
	public static JVector2  operator - (JVector2L lhs, JVector2  rhs) => lhs.Subtract(rhs);
	public static JVector   operator - (JVector2L lhs, JVector   rhs) => lhs.Subtract(rhs);
	public static JVector2L operator - (JVector2I lhs, JVector2L rhs) => rhs.SubtractFrom(lhs);
	public static JVector2L operator - (JVector2B lhs, JVector2L rhs) => rhs.SubtractFrom(lhs);
	public static JVector2  operator - (JVector2  lhs, JVector2L rhs) => rhs.SubtractFrom(lhs);
	public static JVector   operator - (JVector   lhs, JVector2L rhs) => rhs.SubtractFrom(lhs);
	public static double    operator * (JVector2L lhs, JVector2L rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector2L lhs, JVector2I rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector2L lhs, JVector2B rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector2L lhs, JVector2  rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector2L lhs, JVector   rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector2I lhs, JVector2L rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector2B lhs, JVector2L rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector2  lhs, JVector2L rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector   lhs, JVector2L rhs) => rhs.Multiply(lhs);
	
	public static JVector2D operator * (JVector2L lhs, double    rhs) => lhs.Multiply(rhs);
	public static JVector2D operator * (double    lhs, JVector2L rhs) => rhs.Multiply(lhs);
	public static JVector2D operator / (JVector2L lhs, double    rhs) => lhs.Divide(rhs);
	public static JVector2D operator / (double    lhs, JVector2L rhs) => rhs.DivideFrom(lhs);
	
	public static JVector2L operator * (JVector2L lhs, long      rhs) => lhs.Multiply(rhs);
	public static JVector2L operator * (long      lhs, JVector2L rhs) => rhs.Multiply(lhs);
	public static JVector2L operator / (JVector2L lhs, long      rhs) => lhs.Divide(rhs);
	public static JVector2L operator / (long      lhs, JVector2L rhs) => rhs.DivideFrom(lhs);
}