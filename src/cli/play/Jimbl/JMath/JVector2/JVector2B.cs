namespace Jimbl.JMath;

public struct JVector2B: JVector2<byte> {
	public byte X { get; set; }
	public byte Y { get; set; }
	
	public static implicit operator JVector2I (JVector2B vec) => new(vec.X, vec.Y);
	public static implicit operator JVector2L (JVector2B vec) => new(vec.X, vec.Y);
	public static implicit operator JVector2F (JVector2B vec) => new(vec.X, vec.Y);
	public static implicit operator JVector2D (JVector2B vec) => new(vec.X, vec.Y);
	
	public static implicit operator JVector3B (JVector2B vec) => new(vec.X, vec.Y, 0);
	public static implicit operator JVector3I (JVector2B vec) => new(vec.X, vec.Y, 0);
	public static implicit operator JVector3L (JVector2B vec) => new(vec.X, vec.Y, 0);
	public static implicit operator JVector3F (JVector2B vec) => new(vec.X, vec.Y, 0);
	public static implicit operator JVector3D (JVector2B vec) => new(vec.X, vec.Y, 0);
	
	public JVector2B(byte x, byte y) {
		X = x;
		Y = y;
	}

	public byte this[int itemIndex] {
		get => ((JVector2<byte>) this)[itemIndex];
		set => ((JVector2<byte>) this)[itemIndex] = value;
	}
	
	public double Magnitude => ((JVector2<byte>) this).Magnitude;
	
	JVector2 JVector2.Negate() => Negate();
	
	public JVector2I Negate() {
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
		if (other is JVector2B v2b) {
			return Add(v2b);
		}
		else {
			var (a, b) = JVector2.Promote(this, other);
			return a.Add(b);
		}
	}
	
	public JVector2I Add(JVector2B other) {
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
		if (other is JVector2B v2b) {
			return Subtract(v2b);
		}
		else {
			var (a, b) = JVector2.Promote(this, other);
			return a.Subtract(b);
		}
	}
	
	public JVector2I Subtract(JVector2B other) {
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
		if (other is JVector2B v2b) {
			return SubtractFrom(v2b);
		}
		else {
			var (a, b) = JVector2.Promote(this, other);
			return a.SubtractFrom(b);
		}
	}
	
	public JVector2I SubtractFrom(JVector2B other) {
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
		if (other is JVector2B v2b) {
			return Multiply(v2b);
		}
		else {
			var (a, b) = JVector2.Promote(this, other);
			return a.Multiply(b);
		}
	}
	
	public double Multiply(JVector2B other) {
		return X * other.X + Y * other.Y;
	}
	
	JVector2 JVector2.Multiply(double other) => Multiply(other);
	
	public JVector2D Multiply(double other) {
		return new(X * other, Y * other);
	}
	
	public JVector2F Multiply(float other) {
		return new(X * other, Y * other);
	}
	
	public JVector2I Multiply(int other) {
		return new(X * other, Y * other);
	}
	
	public JVector2L Multiply(long other) {
		return new(X * other, Y * other);
	}
	
	JVector2 JVector2.Divide(double other) => Divide(other);
	
	public JVector2D Divide(double other) {
		return new(X / other, Y / other);
	}
	
	public JVector2F Divide(float other) {
		return new(X / other, Y / other);
	}
	
	public JVector2I Divide(int other) {
		return new(X / other, Y / other);
	}
	
	public JVector2L Divide(long other) {
		return new(X / other, Y / other);
	}
	
	JVector2 JVector2.DivideFrom(double other) => DivideFrom(other);
	
	public JVector2D DivideFrom(double other) {
		return new(other / X, other / Y);
	}
	
	public JVector2F DivideFrom(float other) {
		return new(other / X, other / Y);
	}
	
	public JVector2I DivideFrom(int other) {
		return new(other / X, other / Y);
	}
	
	public JVector2L DivideFrom(long other) {
		return new(other / X, other / Y);
	}
	
	// Operators
	public static JVector2B operator + (JVector2B self) => self;
	public static JVector2I operator - (JVector2B self) => self.Negate();
	
	public static JVector2I operator + (JVector2B lhs, JVector2B rhs) => lhs.Add(rhs);
	public static JVector2  operator + (JVector2B lhs, JVector2  rhs) => lhs.Add(rhs);
	public static JVector   operator + (JVector2B lhs, JVector   rhs) => lhs.Add(rhs);
	public static JVector2  operator + (JVector2  lhs, JVector2B rhs) => rhs.Add(lhs);
	public static JVector   operator + (JVector   lhs, JVector2B rhs) => rhs.Add(lhs);
	public static JVector2I operator - (JVector2B lhs, JVector2B rhs) => lhs.Subtract(rhs);
	public static JVector2  operator - (JVector2B lhs, JVector2  rhs) => lhs.Subtract(rhs);
	public static JVector   operator - (JVector2B lhs, JVector   rhs) => lhs.Subtract(rhs);
	public static JVector2  operator - (JVector2  lhs, JVector2B rhs) => rhs.SubtractFrom(lhs);
	public static JVector   operator - (JVector   lhs, JVector2B rhs) => rhs.SubtractFrom(lhs);
	public static double    operator * (JVector2B lhs, JVector2B rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector2B lhs, JVector2  rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector2B lhs, JVector   rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector2  lhs, JVector2B rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector   lhs, JVector2B rhs) => rhs.Multiply(lhs);
	
	public static JVector2D operator * (JVector2B lhs, double    rhs) => lhs.Multiply(rhs);
	public static JVector2D operator * (double    lhs, JVector2B rhs) => rhs.Multiply(lhs);
	public static JVector2D operator / (JVector2B lhs, double    rhs) => lhs.Divide(rhs);
	public static JVector2D operator / (double    lhs, JVector2B rhs) => rhs.DivideFrom(lhs);
	
	public static JVector2F operator * (JVector2B lhs, float     rhs) => lhs.Multiply(rhs);
	public static JVector2F operator * (float     lhs, JVector2B rhs) => rhs.Multiply(lhs);
	public static JVector2F operator / (JVector2B lhs, float     rhs) => lhs.Divide(rhs);
	public static JVector2F operator / (float     lhs, JVector2B rhs) => rhs.DivideFrom(lhs);
	
	public static JVector2I operator * (JVector2B lhs, int       rhs) => lhs.Multiply(rhs);
	public static JVector2I operator * (int       lhs, JVector2B rhs) => rhs.Multiply(lhs);
	public static JVector2I operator / (JVector2B lhs, int       rhs) => lhs.Divide(rhs);
	public static JVector2I operator / (int       lhs, JVector2B rhs) => rhs.DivideFrom(lhs);
	
	public static JVector2L operator * (JVector2B lhs, long      rhs) => lhs.Multiply(rhs);
	public static JVector2L operator * (long      lhs, JVector2B rhs) => rhs.Multiply(lhs);
	public static JVector2L operator / (JVector2B lhs, long      rhs) => lhs.Divide(rhs);
	public static JVector2L operator / (long      lhs, JVector2B rhs) => rhs.DivideFrom(lhs);
}