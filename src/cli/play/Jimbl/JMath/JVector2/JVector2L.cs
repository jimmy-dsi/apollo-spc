namespace Jimbl.JMath;

using System.Numerics;

public struct JVector2L: JVector2<long>, IEquatable<JVector2L> {
	long x;
	long y;
	Action<int, long>? setterHook = null;
	
	public Type InnerType => JVector<long>.Defaults.InnerType();
	
	public (long, long) AsTuple => JVector2<long>.Defaults.AsTuple(this);
	public long[]       AsArray => JVector2<long>.Defaults.AsArray(this);
	
	public long X {
		get => x;
		set {
			x = value;
			setterHook?.Invoke(0, value);
		}
	}

	public long Y {
		get => y;
		set {
			y = value;
			setterHook?.Invoke(1, value);
		}
	}
	
	internal Action<int, long>? SetterHook {
		get => setterHook;
		set => setterHook = value;
	}
	
	public static explicit operator JVector2B (JVector2L vec) => new((byte) vec.X, (byte) vec.Y);
	public static explicit operator JVector2I (JVector2L vec) => new((int)  vec.X, (int)  vec.Y);
	public static explicit operator JVector2F (JVector2L vec) => new(       vec.X,        vec.Y);
	public static explicit operator JVector2D (JVector2L vec) => new(       vec.X,        vec.Y);
	
	public static explicit operator JVector3B (JVector2L vec) => new((byte) vec.X, (byte) vec.Y, 0);
	public static explicit operator JVector3I (JVector2L vec) => new((int)  vec.X, (int)  vec.Y, 0);
	public static implicit operator JVector3L (JVector2L vec) => new(       vec.X,        vec.Y, 0);
	public static explicit operator JVector3F (JVector2L vec) => new(       vec.X,        vec.Y, 0);
	public static implicit operator JVector3D (JVector2L vec) => new(       vec.X,        vec.Y, 0);
	
	public static explicit operator JVector4B (JVector2L vec) => new((byte) vec.X, (byte) vec.Y, 0, 0);
	public static explicit operator JVector4I (JVector2L vec) => new((int)  vec.X, (int)  vec.Y, 0, 0);
	public static implicit operator JVector4L (JVector2L vec) => new(       vec.X,        vec.Y, 0, 0);
	public static explicit operator JVector4F (JVector2L vec) => new(       vec.X,        vec.Y, 0, 0);
	public static implicit operator JVector4D (JVector2L vec) => new(       vec.X,        vec.Y, 0, 0);
	
	public static implicit operator JVector2L ((long, long) tup) => new(tup.Item1, tup.Item2);
	public static explicit operator JVector2L (long[] arr) => new(arr[0], arr[1]);
	
	public JVector2L(long x, long y) {
		X = x;
		Y = y;
	}
	
	JVector2 JVector2<Int64>.Copy<R>(Func<Int64, R> transformation) {
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
	
	public JVector2B Copy(Func<Int64, byte> transformation) {
		JVector2B result = new();
		for (var i = 0; i < 2; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public JVector2I Copy(Func<Int64, Int32> transformation) {
		JVector2I result = new();
		for (var i = 0; i < 2; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public JVector2L Copy(Func<Int64, Int64> transformation) {
		JVector2L result = new();
		for (var i = 0; i < 2; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public JVector2F Copy(Func<Int64, float> transformation) {
		JVector2F result = new();
		for (var i = 0; i < 2; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public JVector2D Copy(Func<Int64, double> transformation) {
		JVector2D result = new();
		for (var i = 0; i < 2; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public void Transform(Func<Int64, Int64> transformation) => JVector<Int64>.Defaults.Transform(ref this, transformation);

	public Int64 this[int itemIndex] {
		get => JVector2<Int64>.Defaults.GetThis(this, itemIndex);
		set => JVector2<Int64>.Defaults.SetThis(ref this, itemIndex, value);
	}
	
	public double Magnitude => JVector2<Int64>.Defaults.Magnitude(this);
	
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

	public override int GetHashCode() {
		return (X, Y).GetHashCode();
	}

	bool IEquatable<JVector2L>.Equals(JVector2L other) => Equals(other);
	
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
		
		if (v2.InnerType.FitsInt64()) {
			return Equals((JVector2<Int64>) v2);
		}
		else if (v2.InnerType.FitsFloat32()) {
			return Equals((JVector2<float>) v2);
		}
		else if (v2.InnerType.FitsFloat64()) {
			return Equals((JVector2<double>) v2);
		}
		else {
			throw new ArgumentException("Cannot compare JVector2L to JVector2");
		}
	}
	
	public bool Equals<T>(JVector2<T> other) where T: INumber<T> {
		var type = JVector.PromoteType(this, other);
		
		if (type == typeof(Int64)) {
			return X == (Int64) (object) other.X
			    && Y == (Int64) (object) other.Y;
		}
		else if (type == typeof(Int128)) {
			return X == (Int128) (object) other.X
			    && Y == (Int128) (object) other.Y;
		}
		else if (type == typeof(BigInteger)) {
			return (BigInteger) X == (BigInteger) (object) other.X
			    && (BigInteger) Y == (BigInteger) (object) other.Y;
		}
		else if (type == typeof(decimal)) {
			return X == (decimal) (object) other.X
			    && Y == (decimal) (object) other.Y;
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
	
	// Equality operator
	public static bool operator == (JVector2L lhs, JVector2L rhs) =>  ((IEquatable<JVector2L>) lhs).Equals(rhs);
	public static bool operator == (JVector2L lhs, JVector2  rhs) =>  lhs.Equals(rhs);
	public static bool operator == (JVector2  lhs, JVector2L rhs) =>  rhs.Equals(lhs);
	public static bool operator != (JVector2L lhs, JVector2L rhs) => !((IEquatable<JVector2L>) lhs).Equals(rhs);
	public static bool operator != (JVector2L lhs, JVector2  rhs) => !lhs.Equals(rhs);
	public static bool operator != (JVector2  lhs, JVector2L rhs) => !rhs.Equals(lhs);
}