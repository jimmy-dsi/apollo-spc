namespace Jimbl.JMath;

using System.Numerics;

public struct JVector3L: JVector3<long>, IEquatable<JVector3L> {
	long x;
	long y;
	long z;
	Action<int, long>? setterHook = null;
	
	public Type InnerType => JVector<long>.Defaults.InnerType();
	
	public (long, long, long) AsTuple => JVector3<long>.Defaults.AsTuple(this);
	public long[]             AsArray => JVector3<long>.Defaults.AsArray(this);
	
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

	public long Z {
		get => z;
		set {
			z = value;
			setterHook?.Invoke(2, value);
		}
	}
	
	internal Action<int, long>? SetterHook {
		get => setterHook;
		set => setterHook = value;
	}
	
	public static explicit operator JVector2B (JVector3L vec) => new((byte) vec.X, (byte) vec.Y);
	public static explicit operator JVector2I (JVector3L vec) => new((int)  vec.X, (int)  vec.Y);
	public static explicit operator JVector2L (JVector3L vec) => new(       vec.X,        vec.Y);
	public static explicit operator JVector2F (JVector3L vec) => new(       vec.X,        vec.Y);
	public static explicit operator JVector2D (JVector3L vec) => new(       vec.X,        vec.Y);
	
	public static explicit operator JVector3B (JVector3L vec) => new((byte) vec.X, (byte) vec.Y, (byte) vec.Z);
	public static explicit operator JVector3I (JVector3L vec) => new((int)  vec.X, (int)  vec.Y, (int)  vec.Z);
	public static explicit operator JVector3F (JVector3L vec) => new(       vec.X,        vec.Y,        vec.Z);
	public static explicit operator JVector3D (JVector3L vec) => new(       vec.X,        vec.Y,        vec.Z);
	
	public static explicit operator JVector4B (JVector3L vec) => new((byte) vec.X, (byte) vec.Y, (byte) vec.Z, 0);
	public static explicit operator JVector4I (JVector3L vec) => new((int)  vec.X, (int)  vec.Y, (int)  vec.Z, 0);
	public static implicit operator JVector4L (JVector3L vec) => new(       vec.X,        vec.Y,        vec.Z, 0);
	public static explicit operator JVector4F (JVector3L vec) => new(       vec.X,        vec.Y,        vec.Z, 0);
	public static explicit operator JVector4D (JVector3L vec) => new(       vec.X,        vec.Y,        vec.Z, 0);
	
	public static implicit operator JVector3L ((long, long, long) tup) => new(tup.Item1, tup.Item2, tup.Item3);
	public static explicit operator JVector3L (long[] arr) => new(arr[0], arr[1], arr[2]);
	
	public JVector3L(long x, long y, long z) {
		X = x;
		Y = y;
		Z = z;
	}
	
	JVector3 JVector3<Int64>.Copy<R>(Func<Int64, R> transformation) {
		JVector3 result;
		
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
	
	public JVector3B Copy(Func<Int64, byte> transformation) {
		JVector3B result = new();
		for (var i = 0; i < 3; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public JVector3I Copy(Func<Int64, Int32> transformation) {
		JVector3I result = new();
		for (var i = 0; i < 3; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public JVector3L Copy(Func<Int64, Int64> transformation) {
		JVector3L result = new();
		for (var i = 0; i < 3; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public JVector3F Copy(Func<Int64, float> transformation) {
		JVector3F result = new();
		for (var i = 0; i < 3; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public JVector3D Copy(Func<Int64, double> transformation) {
		JVector3D result = new();
		for (var i = 0; i < 3; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public void Transform(Func<Int64, Int64> transformation) => JVector<Int64>.Defaults.Transform(ref this, transformation);
	
	public Int64 this[int itemIndex] {
		get => JVector3<Int64>.Defaults.GetThis(this, itemIndex);
		set => JVector3<Int64>.Defaults.SetThis(ref this, itemIndex, value);
	}
	
	public double Magnitude => JVector3<Int64>.Defaults.Magnitude(this);
	
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

	public override int GetHashCode() {
		return (X, Y, Z).GetHashCode();
	}

	bool IEquatable<JVector3L>.Equals(JVector3L other) => Equals(other);
	
	public override bool Equals(object? other) {
		if (other is JVector3 v3) {
			return Equals(v3);
		}
		else {
			return false;
		}
	}
	
	public bool Equals(JVector3 other) {
		if (!InnerType.IsNumeric() || !InnerType.IsNumeric()) {
			return (object) X == other.X
			    && (object) Y == other.Y
			    && (object) Z == other.Z;
		}
		
		var (v1, v2) = JVector3.Promote(this, other);
		
		if (v2.InnerType.FitsInt64()) {
			return Equals((JVector3<Int64>) v2);
		}
		else {
			throw new ArgumentException("Cannot compare JVector3L to JVector3");
		}
	}
	
	public bool Equals<T>(JVector3<T> other) where T: INumber<T> {
		var type = JVector.PromoteType(this, other);
		
		if (type == typeof(Int64)) {
			return X == other.X.UnboxCast<Int64>()
			    && Y == other.Y.UnboxCast<Int64>()
			    && Z == other.Z.UnboxCast<Int64>();
		}
		else if (type == typeof(Int128)) {
			return X == other.X.UnboxCast<Int128>()
			    && Y == other.Y.UnboxCast<Int128>()
			    && Z == other.Z.UnboxCast<Int128>();
		}
		else if (type == typeof(BigInteger)) {
			return (BigInteger) X == other.X.UnboxCast<BigInteger>()
			    && (BigInteger) Y == other.Y.UnboxCast<BigInteger>()
			    && (BigInteger) Z == other.Z.UnboxCast<BigInteger>();
		}
		else if (type == typeof(decimal)) {
			return X == other.X.UnboxCast<decimal>()
			    && Y == other.Y.UnboxCast<decimal>()
			    && Z == other.Z.UnboxCast<decimal>();
		}
		else {
			return (object) X == (object) other.X
			    && (object) Y == (object) other.Y
			    && (object) Z == (object) other.Z;
		}
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
	
	// Equality operator
	public static bool operator == (JVector3L lhs, JVector3L rhs) =>  ((IEquatable<JVector3L>) lhs).Equals(rhs);
	public static bool operator == (JVector3L lhs, JVector3  rhs) =>  lhs.Equals(rhs);
	public static bool operator == (JVector3  lhs, JVector3L rhs) =>  rhs.Equals(lhs);
	public static bool operator != (JVector3L lhs, JVector3L rhs) => !((IEquatable<JVector3L>) lhs).Equals(rhs);
	public static bool operator != (JVector3L lhs, JVector3  rhs) => !lhs.Equals(rhs);
	public static bool operator != (JVector3  lhs, JVector3L rhs) => !rhs.Equals(lhs);
}