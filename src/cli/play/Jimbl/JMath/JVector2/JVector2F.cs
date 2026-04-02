namespace Jimbl.JMath;

using System.Numerics;

public struct JVector2F: JVector2<float>, IEquatable<JVector2F> {
	float x;
	float y;
	Action<int, float>? setterHook = null;
	
	public Type InnerType => JVector<float>.Defaults.InnerType();
	
	public (float, float) AsTuple => JVector2<float>.Defaults.AsTuple(this);
	public float[]        AsArray => JVector2<float>.Defaults.AsArray(this);
	
	public float X {
		get => x;
		set {
			x = value;
			setterHook?.Invoke(0, value);
		}
	}

	public float Y {
		get => y;
		set {
			y = value;
			setterHook?.Invoke(1, value);
		}
	}
	
	internal Action<int, float>? SetterHook {
		get => setterHook;
		set => setterHook = value;
	}
	
	public static explicit operator JVector2B (JVector2F vec) => new((byte) vec.X, (byte) vec.Y);
	public static explicit operator JVector2I (JVector2F vec) => new((int)  vec.X, (int)  vec.Y);
	public static explicit operator JVector2L (JVector2F vec) => new((long) vec.X, (long) vec.Y);
	public static implicit operator JVector2D (JVector2F vec) => new(       vec.X,        vec.Y);
	
	public static explicit operator JVector3B (JVector2F vec) => new((byte) vec.X, (byte) vec.Y, 0);
	public static explicit operator JVector3I (JVector2F vec) => new((int)  vec.X, (int)  vec.Y, 0);
	public static explicit operator JVector3L (JVector2F vec) => new((long) vec.X, (long) vec.Y, 0);
	public static implicit operator JVector3F (JVector2F vec) => new(       vec.X,        vec.Y, 0);
	public static implicit operator JVector3D (JVector2F vec) => new(       vec.X,        vec.Y, 0);
	
	public static explicit operator JVector4B (JVector2F vec) => new((byte) vec.X, (byte) vec.Y, 0, 0);
	public static explicit operator JVector4I (JVector2F vec) => new((int)  vec.X, (int)  vec.Y, 0, 0);
	public static explicit operator JVector4L (JVector2F vec) => new((long) vec.X, (long) vec.Y, 0, 0);
	public static implicit operator JVector4F (JVector2F vec) => new(       vec.X,        vec.Y, 0, 0);
	public static implicit operator JVector4D (JVector2F vec) => new(       vec.X,        vec.Y, 0, 0);
	
	public static implicit operator JVector2F ((float, float) tup) => new(tup.Item1, tup.Item2);
	public static explicit operator JVector2F (float[] arr) => new(arr[0], arr[1]);
	
	public JVector2F(float x, float y) {
		X = x;
		Y = y;
	}
	
	JVector2 JVector2<float>.Copy<R>(Func<float, R> transformation) {
		JVector2 result;
		
		if (typeof(R) == typeof(byte)) {
			result = Copy(x => (byte) (object) transformation(x));
		}
		else if (typeof(R).FitsInt32()) {
			result = Copy(x => (Int32) (object) transformation(x));
		}
		else if (typeof(R).FitsInt64()) {
			result = Copy(x => (Int64) (object) transformation(x));
		}
		else if (typeof(R).FitsFloat32()) {
			result = Copy(x => (float) (object) transformation(x));
		}
		else if (typeof(R).FitsFloat64()) {
			result = Copy(x => (double) (object) transformation(x));
		}
		else {
			throw new ArgumentException();
		}
		
		return result;
	}
	
	public JVector2B Copy(Func<float, byte> transformation) {
		JVector2B result = new();
		for (var i = 0; i < 2; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public JVector2I Copy(Func<float, Int32> transformation) {
		JVector2I result = new();
		for (var i = 0; i < 2; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public JVector2L Copy(Func<float, Int64> transformation) {
		JVector2L result = new();
		for (var i = 0; i < 2; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public JVector2F Copy(Func<float, float> transformation) {
		JVector2F result = new();
		for (var i = 0; i < 2; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public JVector2D Copy(Func<float, double> transformation) {
		JVector2D result = new();
		for (var i = 0; i < 2; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public void Transform(Func<float, float> transformation) => JVector<float>.Defaults.Transform(ref this, transformation);

	public float this[int itemIndex] {
		get => JVector2<float>.Defaults.GetThis(this, itemIndex);
		set => JVector2<float>.Defaults.SetThis(ref this, itemIndex, value);
	}
	
	public double Magnitude => JVector2<float>.Defaults.Magnitude(this);
	
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

	public override int GetHashCode() {
		return (X, Y).GetHashCode();
	}

	bool IEquatable<JVector2F>.Equals(JVector2F other) => Equals(other);
	
	public override bool Equals(object? other) {
		if (other is JVector2 v2) {
			return Equals(v2);
		}
		else {
			return false;
		}
	}
	
	public bool Equals(JVector2 other) {
		if (!InnerType.IsNumeric() || !InnerType.IsNumeric()) {
			return (object) X == other.X
			    && (object) Y == other.Y;
		}
		
		var (v1, v2) = JVector2.Promote(this, other);
		
		if (v2.InnerType.FitsFloat32()) {
			return Equals((JVector2<float>) v2);
		}
		else if (v2.InnerType.FitsFloat64()) {
			return Equals((JVector2<double>) v2);
		}
		else {
			throw new ArgumentException("Cannot compare JVector2F to JVector2");
		}
	}
	
	public bool Equals<T>(JVector2<T> other) where T: INumber<T> {
		var type = JVector.PromoteType(this, other);
		
		if (type == typeof(float)) {
			return X == (float) (object) other.X
			    && Y == (float) (object) other.Y;
		}
		else if (type == typeof(double)) {
			return X == (double) (object) other.X
			    && Y == (double) (object) other.Y;
		}
		else if (type == typeof(Complex)) {
			return X == (Complex) (object) other.X
			    && Y == (Complex) (object) other.Y;
		}
		else {
			return (object) X == (object) other.X
			    && (object) Y == (object) other.Y;
		}
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
	
	// Equality operator
	public static bool operator == (JVector2F lhs, JVector2F rhs) =>  ((IEquatable<JVector2F>) lhs).Equals(rhs);
	public static bool operator == (JVector2F lhs, JVector2  rhs) =>  lhs.Equals(rhs);
	public static bool operator == (JVector2  lhs, JVector2F rhs) =>  rhs.Equals(lhs);
	public static bool operator != (JVector2F lhs, JVector2F rhs) => !((IEquatable<JVector2F>) lhs).Equals(rhs);
	public static bool operator != (JVector2F lhs, JVector2  rhs) => !lhs.Equals(rhs);
	public static bool operator != (JVector2  lhs, JVector2F rhs) => !rhs.Equals(lhs);
}