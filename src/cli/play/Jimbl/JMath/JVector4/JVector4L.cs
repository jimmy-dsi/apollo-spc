namespace Jimbl.JMath;

using System.Numerics;

public struct JVector4L: JVector4<long>, IEquatable<JVector4L> {
	long x;
	long y;
	long z;
	long w;
	Action<int, long>? setterHook = null;
	
	public Type InnerType => JVector<long>.Defaults.InnerType();
	
	public (long, long, long, long) AsTuple => JVector4<long>.Defaults.AsTuple(this);
	public long[]                   AsArray => JVector4<long>.Defaults.AsArray(this);
	
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

	public long W {
		get => w;
		set {
			w = value;
			setterHook?.Invoke(3, value);
		}
	}
	
	internal Action<int, long>? SetterHook {
		get => setterHook;
		set => setterHook = value;
	}
	
	public static explicit operator JVector2B (JVector4L vec) => new((byte) vec.X, (byte) vec.Y);
	public static explicit operator JVector2I (JVector4L vec) => new((int)  vec.X, (int)  vec.Y);
	public static explicit operator JVector2L (JVector4L vec) => new(       vec.X,        vec.Y);
	public static explicit operator JVector2F (JVector4L vec) => new(       vec.X,        vec.Y);
	public static explicit operator JVector2D (JVector4L vec) => new(       vec.X,        vec.Y);
	
	public static explicit operator JVector3B (JVector4L vec) => new((byte) vec.X, (byte) vec.Y, (byte) vec.Z);
	public static explicit operator JVector3I (JVector4L vec) => new((int)  vec.X, (int)  vec.Y, (int)  vec.Z);
	public static explicit operator JVector3L (JVector4L vec) => new(       vec.X,        vec.Y,        vec.Z);
	public static explicit operator JVector3F (JVector4L vec) => new(       vec.X,        vec.Y,        vec.Z);
	public static explicit operator JVector3D (JVector4L vec) => new(       vec.X,        vec.Y,        vec.Z);
	
	public static explicit operator JVector4B (JVector4L vec) => new((byte) vec.X, (byte) vec.Y, (byte) vec.Z, (byte) vec.W);
	public static explicit operator JVector4I (JVector4L vec) => new((int)  vec.X, (int)  vec.Y, (int)  vec.Z, (int)  vec.W);
	public static explicit operator JVector4F (JVector4L vec) => new(       vec.X,        vec.Y,        vec.Z,        vec.W);
	public static explicit operator JVector4D (JVector4L vec) => new(       vec.X,        vec.Y,        vec.Z,        vec.W);
	
	public static implicit operator JVector4L ((long, long, long, long) tup) => new(tup.Item1, tup.Item2, tup.Item3, tup.Item4);
	public static explicit operator JVector4L (long[] arr)                   => new(arr[0], arr[1], arr[2], arr[3]);
	
	public JVector4L(long x, long y, long z, long w) {
		X = x;
		Y = y;
		Z = z;
		W = w;
	}
	
	JVector4 JVector4<Int64>.Copy<R>(Func<Int64, R> transformation) {
		JVector4 result;
		
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
	
	public JVector4B Copy(Func<Int64, byte> transformation) {
		JVector4B result = new();
		for (var i = 0; i < 4; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public JVector4I Copy(Func<Int64, Int32> transformation) {
		JVector4I result = new();
		for (var i = 0; i < 4; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public JVector4L Copy(Func<Int64, Int64> transformation) {
		JVector4L result = new();
		for (var i = 0; i < 4; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public JVector4F Copy(Func<Int64, float> transformation) {
		JVector4F result = new();
		for (var i = 0; i < 4; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public JVector4D Copy(Func<Int64, double> transformation) {
		JVector4D result = new();
		for (var i = 0; i < 4; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public void Transform(Func<Int64, Int64> transformation) => JVector<Int64>.Defaults.Transform(ref this, transformation);
	
	public Int64 this[int itemIndex] {
		get => JVector4<Int64>.Defaults.GetThis(this, itemIndex);
		set => JVector4<Int64>.Defaults.SetThis(ref this, itemIndex, value);
	}
	
	public double Magnitude => JVector4<Int64>.Defaults.Magnitude(this);

	JVector4 JVector4.Negate() => Negate();
	
	public JVector4L Negate() {
		return new(-X, -Y, -Z, -W);
	}
	
	public JVector Add(JVector other) {
		if (other is JVector4 v4) {
			return Add(v4);
		}
		else if (other is JVector3 v3) {
			return Add(v3);
		}
		else if (other is JVector2 v2) {
			return Add(v2);
		}
		else {
			return other.Add(this);
		}
	}
	
	public JVector4 Add(JVector2 other) {
		if (other is JVector2L v2L) {
			return Add(v2L);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.Add(b);
		}
	}
	
	public JVector4 Add(JVector3 other) {
		if (other is JVector3L v3L) {
			return Add(v3L);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.Add(b);
		}
	}
	
	public JVector4 Add(JVector4 other) {
		if (other is JVector4L v4L) {
			return Add(v4L);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.Add(b);
		}
	}
	
	public JVector4L Add(JVector2L other) {
		return Add((JVector4L) other);
	}
	
	public JVector4L Add(JVector3L other) {
		return Add((JVector4L) other);
	}
	
	public JVector4L Add(JVector4L other) {
		return new(X + other.X, Y + other.Y, Z + other.Z, W + other.W);
	}
	
	public JVector Subtract(JVector other) {
		if (other is JVector4 v4) {
			return Subtract(v4);
		}
		else if (other is JVector3 v3) {
			return Subtract(v3);
		}
		else if (other is JVector2 v2) {
			return Subtract(v2);
		}
		else {
			return other.SubtractFrom(this);
		}
	}
	
	public JVector4 Subtract(JVector2 other) {
		if (other is JVector2L v2L) {
			return Subtract(v2L);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.Subtract(b);
		}
	}
	
	public JVector4 Subtract(JVector3 other) {
		if (other is JVector3L v3L) {
			return Subtract(v3L);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.Subtract(b);
		}
	}
	
	public JVector4 Subtract(JVector4 other) {
		if (other is JVector4L v4L) {
			return Subtract(v4L);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.Subtract(b);
		}
	}
	
	public JVector4L Subtract(JVector2L other) {
		return Subtract((JVector4L) other);
	}
	
	public JVector4L Subtract(JVector3L other) {
		return Subtract((JVector4L) other);
	}
	
	public JVector4L Subtract(JVector4L other) {
		return new(X - other.X, Y - other.Y, Z - other.Z, W - other.W);
	}
	
	public JVector SubtractFrom(JVector other) {
		if (other is JVector4 v4) {
			return SubtractFrom(v4);
		}
		else if (other is JVector3 v3) {
			return SubtractFrom(v3);
		}
		else if (other is JVector2 v2) {
			return SubtractFrom(v2);
		}
		else {
			return other.Subtract(this);
		}
	}
	
	public JVector4 SubtractFrom(JVector2 other) {
		if (other is JVector2L v2L) {
			return SubtractFrom(v2L);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.SubtractFrom(b);
		}
	}
	
	public JVector4 SubtractFrom(JVector3 other) {
		if (other is JVector3L v3L) {
			return SubtractFrom(v3L);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.SubtractFrom(b);
		}
	}
	
	public JVector4 SubtractFrom(JVector4 other) {
		if (other is JVector4L v4L) {
			return SubtractFrom(v4L);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.SubtractFrom(b);
		}
	}
	
	public JVector4L SubtractFrom(JVector2L other) {
		return SubtractFrom((JVector4L) other);
	}
	
	public JVector4L SubtractFrom(JVector3L other) {
		return SubtractFrom((JVector4L) other);
	}
	
	public JVector4L SubtractFrom(JVector4L other) {
		return new(other.X - X, other.Y - Y, other.Z - Z, other.W - W);
	}
	
	public double Multiply(JVector other) {
		if (other is JVector4 v4) {
			return Multiply(v4);
		}
		else if (other is JVector3 v3) {
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
			var (a, b) = JVector4.Promote(this, other);
			return a.Multiply(b);
		}
	}
	
	public double Multiply(JVector3 other) {
		if (other is JVector3L v3L) {
			return Multiply(v3L);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.Multiply(b);
		}
	}
	
	public double Multiply(JVector4 other) {
		if (other is JVector4L v4L) {
			return Multiply(v4L);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.Multiply(b);
		}
	}
	
	public double Multiply(JVector2L other) {
		return Multiply((JVector4L) other);
	}
	
	public double Multiply(JVector3L other) {
		return Multiply((JVector4L) other);
	}
	
	public double Multiply(JVector4L other) {
		return X * other.X + Y * other.Y + Z * other.Z + W * other.W;
	}
	
	JVector4 JVector4.Multiply(double other) => Multiply(other);
	
	public JVector4D Multiply(double other) {
		return new(X * other, Y * other, Z * other, W * other);
	}
	
	public JVector4L Multiply(long other) {
		return new(X * other, Y * other, Z * other, W * other);
	}
	
	JVector4 JVector4.Divide(double other) => Divide(other);
	
	public JVector4D Divide(double other) {
		return new(X / other, Y / other, Z / other, W / other);
	}
	
	public JVector4L Divide(long other) {
		return new(X / other, Y / other, Z / other, W / other);
	}
	
	JVector4 JVector4.DivideFrom(double other) => DivideFrom(other);
	
	public JVector4D DivideFrom(double other) {
		return new(other / X, other / Y, other / Z, other / W);
	}
	
	public JVector4L DivideFrom(long other) {
		return new(other / X, other / Y, other / Z, other / W);
	}

	public override int GetHashCode() {
		return (X, Y, Z, W).GetHashCode();
	}

	bool IEquatable<JVector4L>.Equals(JVector4L other) => Equals(other);
	
	public override bool Equals(object? other) {
		if (other is JVector4 v4) {
			return Equals(v4);
		}
		else {
			return false;
		}
	}
	
	public bool Equals(JVector4 other) {
		if (!InnerType.IsNumeric() || !InnerType.IsNumeric()) {
			return (object) X == other.X
			    && (object) Y == other.Y
			    && (object) Z == other.Z
			    && (object) W == other.W;
		}
		
		var (v1, v2) = JVector4.Promote(this, other);
		
		if (v2.InnerType.FitsInt64()) {
			return Equals((JVector4<Int64>) v2);
		}
		else {
			throw new ArgumentException("Cannot compare JVector4L to JVector4");
		}
	}
	
	public bool Equals<T>(JVector4<T> other) where T: INumber<T> {
		var type = JVector.PromoteType(this, other);
		
		if (type == typeof(Int64)) {
			return X == other.X.UnboxCast<Int64>()
			    && Y == other.Y.UnboxCast<Int64>()
			    && Z == other.Z.UnboxCast<Int64>()
			    && W == other.W.UnboxCast<Int64>();
		}
		else if (type == typeof(Int128)) {
			return X == other.X.UnboxCast<Int128>()
			    && Y == other.Y.UnboxCast<Int128>()
			    && Z == other.Z.UnboxCast<Int128>()
			    && W == other.W.UnboxCast<Int128>();
		}
		else if (type == typeof(BigInteger)) {
			return (BigInteger) X == other.X.UnboxCast<BigInteger>()
			    && (BigInteger) Y == other.Y.UnboxCast<BigInteger>()
			    && (BigInteger) Z == other.Z.UnboxCast<BigInteger>()
			    && (BigInteger) W == other.W.UnboxCast<BigInteger>();
		}
		else if (type == typeof(decimal)) {
			return X == other.X.UnboxCast<decimal>()
			    && Y == other.Y.UnboxCast<decimal>()
			    && Z == other.Z.UnboxCast<decimal>()
			    && W == other.W.UnboxCast<decimal>();
		}
		else {
			return (object) X == (object) other.X
			    && (object) Y == (object) other.Y
			    && (object) Z == (object) other.Z
			    && (object) W == (object) other.W;
		}
	}
	
	// Unary
	public static JVector4L operator + (JVector4L self) => self;
	public static JVector4L operator - (JVector4L self) => self.Negate();
	
	// With vec2
	public static JVector4L operator + (JVector4L lhs, JVector2L rhs) => lhs.Add(rhs);
	public static JVector4L operator + (JVector4L lhs, JVector2I rhs) => lhs.Add(rhs);
	public static JVector4L operator + (JVector4L lhs, JVector2B rhs) => lhs.Add(rhs);
	public static JVector4  operator + (JVector4L lhs, JVector2  rhs) => lhs.Add(rhs);
	public static JVector4L operator + (JVector2L lhs, JVector4L rhs) => rhs.Add(lhs);
	public static JVector4L operator + (JVector2I lhs, JVector4L rhs) => rhs.Add(lhs);
	public static JVector4L operator + (JVector2B lhs, JVector4L rhs) => rhs.Add(lhs);
	public static JVector4  operator + (JVector2  lhs, JVector4L rhs) => rhs.Add(lhs);
	public static JVector4L operator - (JVector4L lhs, JVector2L rhs) => lhs.Subtract(rhs);
	public static JVector4L operator - (JVector4L lhs, JVector2I rhs) => lhs.Subtract(rhs);
	public static JVector4L operator - (JVector4L lhs, JVector2B rhs) => lhs.Subtract(rhs);
	public static JVector4  operator - (JVector4L lhs, JVector2  rhs) => lhs.Subtract(rhs);
	public static JVector4L operator - (JVector2L lhs, JVector4L rhs) => rhs.SubtractFrom(lhs);
	public static JVector4L operator - (JVector2I lhs, JVector4L rhs) => rhs.SubtractFrom(lhs);
	public static JVector4L operator - (JVector2B lhs, JVector4L rhs) => rhs.SubtractFrom(lhs);
	public static JVector4  operator - (JVector2  lhs, JVector4L rhs) => rhs.SubtractFrom(lhs);
	public static double    operator * (JVector4L lhs, JVector2L rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector4L lhs, JVector2I rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector4L lhs, JVector2B rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector4L lhs, JVector2  rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector2L lhs, JVector4L rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector2I lhs, JVector4L rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector2B lhs, JVector4L rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector2  lhs, JVector4L rhs) => rhs.Multiply(lhs);
	
	// With vec3
	public static JVector4L operator + (JVector4L lhs, JVector3L rhs) => lhs.Add(rhs);
	public static JVector4L operator + (JVector4L lhs, JVector3I rhs) => lhs.Add(rhs);
	public static JVector4L operator + (JVector4L lhs, JVector3B rhs) => lhs.Add(rhs);
	public static JVector4  operator + (JVector4L lhs, JVector3  rhs) => lhs.Add(rhs);
	public static JVector4L operator + (JVector3L lhs, JVector4L rhs) => rhs.Add(lhs);
	public static JVector4L operator + (JVector3I lhs, JVector4L rhs) => rhs.Add(lhs);
	public static JVector4L operator + (JVector3B lhs, JVector4L rhs) => rhs.Add(lhs);
	public static JVector4  operator + (JVector3  lhs, JVector4L rhs) => rhs.Add(lhs);
	public static JVector4L operator - (JVector4L lhs, JVector3L rhs) => lhs.Subtract(rhs);
	public static JVector4L operator - (JVector4L lhs, JVector3I rhs) => lhs.Subtract(rhs);
	public static JVector4L operator - (JVector4L lhs, JVector3B rhs) => lhs.Subtract(rhs);
	public static JVector4  operator - (JVector4L lhs, JVector3  rhs) => lhs.Subtract(rhs);
	public static JVector4L operator - (JVector3L lhs, JVector4L rhs) => rhs.SubtractFrom(lhs);
	public static JVector4L operator - (JVector3I lhs, JVector4L rhs) => rhs.SubtractFrom(lhs);
	public static JVector4L operator - (JVector3B lhs, JVector4L rhs) => rhs.SubtractFrom(lhs);
	public static JVector4  operator - (JVector3  lhs, JVector4L rhs) => rhs.SubtractFrom(lhs);
	public static double    operator * (JVector4L lhs, JVector3L rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector4L lhs, JVector3I rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector4L lhs, JVector3B rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector4L lhs, JVector3  rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector3L lhs, JVector4L rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector3I lhs, JVector4L rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector3B lhs, JVector4L rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector3  lhs, JVector4L rhs) => rhs.Multiply(lhs);
	
	// With vec4
	public static JVector4L operator + (JVector4L lhs, JVector4L rhs) => lhs.Add(rhs);
	public static JVector4L operator + (JVector4L lhs, JVector4I rhs) => lhs.Add(rhs);
	public static JVector4L operator + (JVector4L lhs, JVector4B rhs) => lhs.Add(rhs);
	public static JVector4  operator + (JVector4L lhs, JVector4  rhs) => lhs.Add(rhs);
	public static JVector4L operator + (JVector4I lhs, JVector4L rhs) => rhs.Add(lhs);
	public static JVector4L operator + (JVector4B lhs, JVector4L rhs) => rhs.Add(lhs);
	public static JVector4  operator + (JVector4  lhs, JVector4L rhs) => rhs.Add(lhs);
	public static JVector4L operator - (JVector4L lhs, JVector4L rhs) => lhs.Subtract(rhs);
	public static JVector4L operator - (JVector4L lhs, JVector4I rhs) => lhs.Subtract(rhs);
	public static JVector4L operator - (JVector4L lhs, JVector4B rhs) => lhs.Subtract(rhs);
	public static JVector4  operator - (JVector4L lhs, JVector4  rhs) => lhs.Subtract(rhs);
	public static JVector4L operator - (JVector4I lhs, JVector4L rhs) => rhs.SubtractFrom(lhs);
	public static JVector4L operator - (JVector4B lhs, JVector4L rhs) => rhs.SubtractFrom(lhs);
	public static JVector4  operator - (JVector4  lhs, JVector4L rhs) => rhs.SubtractFrom(lhs);
	public static double    operator * (JVector4L lhs, JVector4L rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector4L lhs, JVector4I rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector4L lhs, JVector4B rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector4L lhs, JVector4  rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector4I lhs, JVector4L rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector4B lhs, JVector4L rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector4  lhs, JVector4L rhs) => rhs.Multiply(lhs);
	
	public static JVector4D operator * (JVector4L lhs, double    rhs) => lhs.Multiply(rhs);
	public static JVector4D operator * (double    lhs, JVector4L rhs) => rhs.Multiply(lhs);
	public static JVector4D operator / (JVector4L lhs, double    rhs) => lhs.Divide(rhs);
	public static JVector4D operator / (double    lhs, JVector4L rhs) => rhs.DivideFrom(lhs);
	
	public static JVector4L operator * (JVector4L lhs, long      rhs) => lhs.Multiply(rhs);
	public static JVector4L operator * (long      lhs, JVector4L rhs) => rhs.Multiply(lhs);
	public static JVector4L operator / (JVector4L lhs, long      rhs) => lhs.Divide(rhs);
	public static JVector4L operator / (long      lhs, JVector4L rhs) => rhs.DivideFrom(lhs);
	
	// Equality operator
	public static bool operator == (JVector4L lhs, JVector4L rhs) =>  lhs.Equals(rhs);
	public static bool operator == (JVector4L lhs, JVector4  rhs) =>  lhs.Equals(rhs);
	public static bool operator == (JVector4  lhs, JVector4L rhs) =>  rhs.Equals(lhs);
	public static bool operator != (JVector4L lhs, JVector4L rhs) => !lhs.Equals(rhs);
	public static bool operator != (JVector4L lhs, JVector4  rhs) => !lhs.Equals(rhs);
	public static bool operator != (JVector4  lhs, JVector4L rhs) => !rhs.Equals(lhs);
}