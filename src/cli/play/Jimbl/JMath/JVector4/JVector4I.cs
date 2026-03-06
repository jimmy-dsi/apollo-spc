namespace Jimbl.JMath;

using System.Numerics;

public struct JVector4I: JVector4<int>, IEquatable<JVector4I> {
	int x;
	int y;
	int z;
	int w;
	Action<int, int>? setterHook = null;
	
	public Type InnerType => JVector<int>.Defaults.InnerType();
	
	public (int, int, int, int) AsTuple => JVector4<int>.Defaults.AsTuple(this);
	public int[]                AsArray => JVector4<int>.Defaults.AsArray(this);
	
	public int X {
		get => x;
		set {
			x = value;
			setterHook?.Invoke(0, value);
		}
	}

	public int Y {
		get => y;
		set {
			y = value;
			setterHook?.Invoke(1, value);
		}
	}

	public int Z {
		get => z;
		set {
			z = value;
			setterHook?.Invoke(2, value);
		}
	}

	public int W {
		get => w;
		set {
			w = value;
			setterHook?.Invoke(3, value);
		}
	}
	
	internal Action<int, int>? SetterHook {
		get => setterHook;
		set => setterHook = value;
	}
	
	public static explicit operator JVector2B (JVector4I vec) => new((byte) vec.X, (byte) vec.Y);
	public static explicit operator JVector2I (JVector4I vec) => new(       vec.X,        vec.Y);
	public static explicit operator JVector2L (JVector4I vec) => new(       vec.X,        vec.Y);
	public static explicit operator JVector2F (JVector4I vec) => new(       vec.X,        vec.Y);
	public static explicit operator JVector2D (JVector4I vec) => new(       vec.X,        vec.Y);
	
	public static explicit operator JVector3B (JVector4I vec) => new((byte) vec.X, (byte) vec.Y, (byte) vec.Z);
	public static explicit operator JVector3I (JVector4I vec) => new(       vec.X,        vec.Y,        vec.Z);
	public static explicit operator JVector3L (JVector4I vec) => new(       vec.X,        vec.Y,        vec.Z);
	public static explicit operator JVector3F (JVector4I vec) => new(       vec.X,        vec.Y,        vec.Z);
	public static explicit operator JVector3D (JVector4I vec) => new(       vec.X,        vec.Y,        vec.Z);
	
	public static explicit operator JVector4B (JVector4I vec) => new((byte) vec.X, (byte) vec.Y, (byte) vec.Z, (byte) vec.W);
	public static implicit operator JVector4L (JVector4I vec) => new(       vec.X,        vec.Y,        vec.Z,        vec.W);
	public static explicit operator JVector4F (JVector4I vec) => new(       vec.X,        vec.Y,        vec.Z,        vec.W);
	public static implicit operator JVector4D (JVector4I vec) => new(       vec.X,        vec.Y,        vec.Z,        vec.W);
	
	public static implicit operator JVector4I ((int, int, int, int) tup) => new(tup.Item1, tup.Item2, tup.Item3, tup.Item4);
	public static explicit operator JVector4I (int[] arr) => new(arr[0], arr[1], arr[2], arr[3]);
	
	public JVector4I(int x, int y, int z, int w) {
		X = x;
		Y = y;
		Z = z;
		W = w;
	}
	
	JVector4 JVector4<Int32>.Copy<R>(Func<Int32, R> transformation) {
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
	
	public JVector4B Copy(Func<Int32, byte> transformation) {
		JVector4B result = new();
		for (var i = 0; i < 4; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public JVector4I Copy(Func<Int32, Int32> transformation) {
		JVector4I result = new();
		for (var i = 0; i < 4; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public JVector4L Copy(Func<Int32, Int64> transformation) {
		JVector4L result = new();
		for (var i = 0; i < 4; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public JVector4F Copy(Func<Int32, float> transformation) {
		JVector4F result = new();
		for (var i = 0; i < 4; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public JVector4D Copy(Func<Int32, double> transformation) {
		JVector4D result = new();
		for (var i = 0; i < 4; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public void Transform(Func<Int32, Int32> transformation) => JVector<Int32>.Defaults.Transform(ref this, transformation);
	
	public Int32 this[int itemIndex] {
		get => JVector4<Int32>.Defaults.GetThis(this, itemIndex);
		set => JVector4<Int32>.Defaults.SetThis(ref this, itemIndex, value);
	}
	
	public double Magnitude => JVector4<Int32>.Defaults.Magnitude(this);

	JVector4 JVector4.Negate() => Negate();
	
	public JVector4I Negate() {
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
		if (other is JVector2I v2b) {
			return Add(v2b);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.Add(b);
		}
	}
	
	public JVector4 Add(JVector3 other) {
		if (other is JVector3I v3i) {
			return Add(v3i);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.Add(b);
		}
	}
	
	public JVector4 Add(JVector4 other) {
		if (other is JVector4I v4i) {
			return Add(v4i);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.Add(b);
		}
	}
	
	public JVector4I Add(JVector2I other) {
		return Add((JVector4I) other);
	}
	
	public JVector4I Add(JVector3I other) {
		return Add((JVector4I) other);
	}
	
	public JVector4I Add(JVector4I other) {
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
		if (other is JVector2I v2i) {
			return Subtract(v2i);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.Subtract(b);
		}
	}
	
	public JVector4 Subtract(JVector3 other) {
		if (other is JVector3I v3i) {
			return Subtract(v3i);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.Subtract(b);
		}
	}
	
	public JVector4 Subtract(JVector4 other) {
		if (other is JVector4I v4i) {
			return Subtract(v4i);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.Subtract(b);
		}
	}
	
	public JVector4I Subtract(JVector2I other) {
		return Subtract((JVector4I) other);
	}
	
	public JVector4I Subtract(JVector3I other) {
		return Subtract((JVector4I) other);
	}
	
	public JVector4I Subtract(JVector4I other) {
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
		if (other is JVector2I v2i) {
			return SubtractFrom(v2i);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.SubtractFrom(b);
		}
	}
	
	public JVector4 SubtractFrom(JVector3 other) {
		if (other is JVector3I v3i) {
			return SubtractFrom(v3i);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.SubtractFrom(b);
		}
	}
	
	public JVector4 SubtractFrom(JVector4 other) {
		if (other is JVector4I v4i) {
			return SubtractFrom(v4i);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.SubtractFrom(b);
		}
	}
	
	public JVector4I SubtractFrom(JVector2I other) {
		return SubtractFrom((JVector4I) other);
	}
	
	public JVector4I SubtractFrom(JVector3I other) {
		return SubtractFrom((JVector4I) other);
	}
	
	public JVector4I SubtractFrom(JVector4I other) {
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
		if (other is JVector2I v2i) {
			return Multiply(v2i);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.Multiply(b);
		}
	}
	
	public double Multiply(JVector3 other) {
		if (other is JVector3I v3i) {
			return Multiply(v3i);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.Multiply(b);
		}
	}
	
	public double Multiply(JVector4 other) {
		if (other is JVector4I v4i) {
			return Multiply(v4i);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.Multiply(b);
		}
	}
	
	public double Multiply(JVector2I other) {
		return Multiply((JVector4I) other);
	}
	
	public double Multiply(JVector3I other) {
		return Multiply((JVector4I) other);
	}
	
	public double Multiply(JVector4I other) {
		return X * other.X + Y * other.Y + Z * other.Z + W * other.W;
	}
	
	JVector4 JVector4.Multiply(double other) => Multiply(other);
	
	public JVector4D Multiply(double other) {
		return new(X * other, Y * other, Z * other, W * other);
	}
	
	public JVector4I Multiply(int other) {
		return new(X * other, Y * other, Z * other, W * other);
	}
	
	public JVector4L Multiply(long other) {
		return new(X * other, Y * other, Z * other, W * other);
	}
	
	JVector4 JVector4.Divide(double other) => Divide(other);
	
	public JVector4D Divide(double other) {
		return new(X / other, Y / other, Z / other, W / other);
	}
	
	public JVector4I Divide(int other) {
		return new(X / other, Y / other, Z / other, W / other);
	}
	
	public JVector4L Divide(long other) {
		return new(X / other, Y / other, Z / other, W / other);
	}
	
	JVector4 JVector4.DivideFrom(double other) => DivideFrom(other);
	
	public JVector4D DivideFrom(double other) {
		return new(other / X, other / Y, other / Z, other / W);
	}
	
	public JVector4I DivideFrom(int other) {
		return new(other / X, other / Y, other / Z, other / W);
	}
	
	public JVector4L DivideFrom(long other) {
		return new(other / X, other / Y, other / Z, other / W);
	}

	public override int GetHashCode() {
		return (X, Y, Z, W).GetHashCode();
	}

	bool IEquatable<JVector4I>.Equals(JVector4I other) => Equals(other);
	
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
		
		if (v2.InnerType.FitsInt32()) {
			return Equals((JVector4<Int32>) v2);
		}
		else if (v2.InnerType.FitsInt64()) {
			return Equals((JVector4<Int64>) v2);
		}
		else if (v2.InnerType.FitsFloat64()) {
			return Equals((JVector4<double>) v2);
		}
		else {
			throw new ArgumentException("Cannot compare JVector4I to JVector4");
		}
	}
	
	public bool Equals<T>(JVector4<T> other) where T: INumber<T> {
		var type = JVector.PromoteType(this, other);
		
		if (type == typeof(Int32)) {
			return X == other.X.UnboxCast<Int32>()
			    && Y == other.Y.UnboxCast<Int32>()
			    && Z == other.Z.UnboxCast<Int32>()
			    && W == other.W.UnboxCast<Int32>();
		}
		else if (type == typeof(Int64)) {
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
		else if (type == typeof(double)) {
			return X == other.X.UnboxCast<double>()
			    && Y == other.Y.UnboxCast<double>()
			    && Z == other.Z.UnboxCast<double>()
			    && W == other.W.UnboxCast<double>();
		}
		else if (type == typeof(decimal)) {
			return X == other.X.UnboxCast<decimal>()
			    && Y == other.Y.UnboxCast<decimal>()
			    && Z == other.Z.UnboxCast<decimal>()
			    && W == other.W.UnboxCast<decimal>();
		}
		else if (type == typeof(Complex)) {
			return X == other.X.UnboxCast<Complex>()
			    && Y == other.Y.UnboxCast<Complex>()
			    && Z == other.Z.UnboxCast<Complex>()
			    && W == other.W.UnboxCast<Complex>();
		}
		else {
			return (object) X == (object) other.X
			    && (object) Y == (object) other.Y
			    && (object) Z == (object) other.Z
			    && (object) W == (object) other.W;
		}
	}
	
	// Unary
	public static JVector4I operator + (JVector4I self) => self;
	public static JVector4I operator - (JVector4I self) => self.Negate();
	
	// With vec2
	public static JVector4I operator + (JVector4I lhs, JVector2I rhs) => lhs.Add(rhs);
	public static JVector4I operator + (JVector4I lhs, JVector2B rhs) => lhs.Add(rhs);
	public static JVector4  operator + (JVector4I lhs, JVector2  rhs) => lhs.Add(rhs);
	public static JVector4I operator + (JVector2I lhs, JVector4I rhs) => rhs.Add(lhs);
	public static JVector4I operator + (JVector2B lhs, JVector4I rhs) => rhs.Add(lhs);
	public static JVector4  operator + (JVector2  lhs, JVector4I rhs) => rhs.Add(lhs);
	public static JVector4I operator - (JVector4I lhs, JVector2I rhs) => lhs.Subtract(rhs);
	public static JVector4I operator - (JVector4I lhs, JVector2B rhs) => lhs.Subtract(rhs);
	public static JVector4  operator - (JVector4I lhs, JVector2  rhs) => lhs.Subtract(rhs);
	public static JVector4I operator - (JVector2I lhs, JVector4I rhs) => rhs.SubtractFrom(lhs);
	public static JVector4I operator - (JVector2B lhs, JVector4I rhs) => rhs.SubtractFrom(lhs);
	public static JVector4  operator - (JVector2  lhs, JVector4I rhs) => rhs.SubtractFrom(lhs);
	public static double    operator * (JVector4I lhs, JVector2I rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector4I lhs, JVector2B rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector4I lhs, JVector2  rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector2I lhs, JVector4I rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector2B lhs, JVector4I rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector2  lhs, JVector4I rhs) => rhs.Multiply(lhs);
	
	// With vec3
	public static JVector4I operator + (JVector4I lhs, JVector3I rhs) => lhs.Add(rhs);
	public static JVector4I operator + (JVector4I lhs, JVector3B rhs) => lhs.Add(rhs);
	public static JVector4  operator + (JVector4I lhs, JVector3  rhs) => lhs.Add(rhs);
	public static JVector4I operator + (JVector3I lhs, JVector4I rhs) => rhs.Add(lhs);
	public static JVector4I operator + (JVector3B lhs, JVector4I rhs) => rhs.Add(lhs);
	public static JVector4  operator + (JVector3  lhs, JVector4I rhs) => rhs.Add(lhs);
	public static JVector4I operator - (JVector4I lhs, JVector3I rhs) => lhs.Subtract(rhs);
	public static JVector4I operator - (JVector4I lhs, JVector3B rhs) => lhs.Subtract(rhs);
	public static JVector4  operator - (JVector4I lhs, JVector3  rhs) => lhs.Subtract(rhs);
	public static JVector4I operator - (JVector3I lhs, JVector4I rhs) => rhs.SubtractFrom(lhs);
	public static JVector4I operator - (JVector3B lhs, JVector4I rhs) => rhs.SubtractFrom(lhs);
	public static JVector4  operator - (JVector3  lhs, JVector4I rhs) => rhs.SubtractFrom(lhs);
	public static double    operator * (JVector4I lhs, JVector3I rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector4I lhs, JVector3B rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector4I lhs, JVector3  rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector3I lhs, JVector4I rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector3B lhs, JVector4I rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector3  lhs, JVector4I rhs) => rhs.Multiply(lhs);
	
	// With vec4
	public static JVector4I operator + (JVector4I lhs, JVector4I rhs) => lhs.Add(rhs);
	public static JVector4I operator + (JVector4I lhs, JVector4B rhs) => lhs.Add(rhs);
	public static JVector4  operator + (JVector4I lhs, JVector4  rhs) => lhs.Add(rhs);
	public static JVector4I operator + (JVector4B lhs, JVector4I rhs) => rhs.Add(lhs);
	public static JVector4  operator + (JVector4  lhs, JVector4I rhs) => rhs.Add(lhs);
	public static JVector4I operator - (JVector4I lhs, JVector4I rhs) => lhs.Subtract(rhs);
	public static JVector4I operator - (JVector4I lhs, JVector4B rhs) => lhs.Subtract(rhs);
	public static JVector4  operator - (JVector4I lhs, JVector4  rhs) => lhs.Subtract(rhs);
	public static JVector4I operator - (JVector4B lhs, JVector4I rhs) => rhs.SubtractFrom(lhs);
	public static JVector4  operator - (JVector4  lhs, JVector4I rhs) => rhs.SubtractFrom(lhs);
	public static double    operator * (JVector4I lhs, JVector4I rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector4I lhs, JVector4B rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector4I lhs, JVector4  rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector4B lhs, JVector4I rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector4  lhs, JVector4I rhs) => rhs.Multiply(lhs);
	
	public static JVector4D operator * (JVector4I lhs, double    rhs) => lhs.Multiply(rhs);
	public static JVector4D operator * (double    lhs, JVector4I rhs) => rhs.Multiply(lhs);
	public static JVector4D operator / (JVector4I lhs, double    rhs) => lhs.Divide(rhs);
	public static JVector4D operator / (double    lhs, JVector4I rhs) => rhs.DivideFrom(lhs);
	
	public static JVector4I operator * (JVector4I lhs, int       rhs) => lhs.Multiply(rhs);
	public static JVector4I operator * (int       lhs, JVector4I rhs) => rhs.Multiply(lhs);
	public static JVector4I operator / (JVector4I lhs, int       rhs) => lhs.Divide(rhs);
	public static JVector4I operator / (int       lhs, JVector4I rhs) => rhs.DivideFrom(lhs);
	
	public static JVector4L operator * (JVector4I lhs, long      rhs) => lhs.Multiply(rhs);
	public static JVector4L operator * (long      lhs, JVector4I rhs) => rhs.Multiply(lhs);
	public static JVector4L operator / (JVector4I lhs, long      rhs) => lhs.Divide(rhs);
	public static JVector4L operator / (long      lhs, JVector4I rhs) => rhs.DivideFrom(lhs);
	
	// Equality operator
	public static bool operator == (JVector4I lhs, JVector4I rhs) =>  lhs.Equals(rhs);
	public static bool operator == (JVector4I lhs, JVector4  rhs) =>  lhs.Equals(rhs);
	public static bool operator == (JVector4  lhs, JVector4I rhs) =>  rhs.Equals(lhs);
	public static bool operator != (JVector4I lhs, JVector4I rhs) => !lhs.Equals(rhs);
	public static bool operator != (JVector4I lhs, JVector4  rhs) => !lhs.Equals(rhs);
	public static bool operator != (JVector4  lhs, JVector4I rhs) => !rhs.Equals(lhs);
}