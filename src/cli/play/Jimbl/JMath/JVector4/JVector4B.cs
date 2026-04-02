namespace Jimbl.JMath;

using System.Numerics;

public struct JVector4B: JVector4<byte>, IEquatable<JVector4B> {
	byte x;
	byte y;
	byte z;
	byte w;
	Action<int, byte>? setterHook = null;
	
	public Type InnerType => JVector<byte>.Defaults.InnerType();
	
	public (byte, byte, byte, byte) AsTuple => JVector4<byte>.Defaults.AsTuple(this);
	public byte[]                   AsArray => JVector4<byte>.Defaults.AsArray(this);
	
	public byte X {
		get => x;
		set {
			x = value;
			setterHook?.Invoke(0, value);
		}
	}

	public byte Y {
		get => y;
		set {
			y = value;
			setterHook?.Invoke(1, value);
		}
	}

	public byte Z {
		get => z;
		set {
			z = value;
			setterHook?.Invoke(2, value);
		}
	}

	public byte W {
		get => w;
		set {
			w = value;
			setterHook?.Invoke(3, value);
		}
	}
	
	internal Action<int, byte>? SetterHook {
		get => setterHook;
		set => setterHook = value;
	}
	
	public static explicit operator JVector2B (JVector4B vec) => new(vec.X, vec.Y);
	public static explicit operator JVector2I (JVector4B vec) => new(vec.X, vec.Y);
	public static explicit operator JVector2L (JVector4B vec) => new(vec.X, vec.Y);
	public static explicit operator JVector2F (JVector4B vec) => new(vec.X, vec.Y);
	public static explicit operator JVector2D (JVector4B vec) => new(vec.X, vec.Y);
	
	public static explicit operator JVector3B (JVector4B vec) => new(vec.X, vec.Y, vec.Z);
	public static explicit operator JVector3I (JVector4B vec) => new(vec.X, vec.Y, vec.Z);
	public static explicit operator JVector3L (JVector4B vec) => new(vec.X, vec.Y, vec.Z);
	public static explicit operator JVector3F (JVector4B vec) => new(vec.X, vec.Y, vec.Z);
	public static explicit operator JVector3D (JVector4B vec) => new(vec.X, vec.Y, vec.Z);
	
	public static implicit operator JVector4I (JVector4B vec) => new(vec.X, vec.Y, vec.Z, vec.W);
	public static implicit operator JVector4L (JVector4B vec) => new(vec.X, vec.Y, vec.Z, vec.W);
	public static implicit operator JVector4F (JVector4B vec) => new(vec.X, vec.Y, vec.Z, vec.W);
	public static implicit operator JVector4D (JVector4B vec) => new(vec.X, vec.Y, vec.Z, vec.W);
	
	public static implicit operator JVector4B ((byte, byte, byte, byte) tup) => new(tup.Item1, tup.Item2, tup.Item3, tup.Item4);
	public static explicit operator JVector4B (byte[] arr) => new(arr[0], arr[1], arr[2], arr[3]);
	
	public JVector4B(byte x, byte y, byte z, byte w) {
		X = x;
		Y = y;
		Z = z;
		W = w;
	}
	
	JVector4 JVector4<byte>.Copy<R>(Func<byte, R> transformation) {
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
	
	public JVector4B Copy(Func<byte, byte> transformation) {
		JVector4B result = new();
		for (var i = 0; i < 4; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public JVector4I Copy(Func<byte, Int32> transformation) {
		JVector4I result = new();
		for (var i = 0; i < 4; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public JVector4L Copy(Func<byte, Int64> transformation) {
		JVector4L result = new();
		for (var i = 0; i < 4; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public JVector4F Copy(Func<byte, float> transformation) {
		JVector4F result = new();
		for (var i = 0; i < 4; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public JVector4D Copy(Func<byte, double> transformation) {
		JVector4D result = new();
		for (var i = 0; i < 4; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public void Transform(Func<byte, byte> transformation) => JVector<byte>.Defaults.Transform(ref this, transformation);
	
	public byte this[int itemIndex] {
		get => JVector4<byte>.Defaults.GetThis(this, itemIndex);
		set => JVector4<byte>.Defaults.SetThis(ref this, itemIndex, value);
	}
	
	public double Magnitude => JVector4<byte>.Defaults.Magnitude(this);

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
		if (other is JVector2B v2b) {
			return Add(v2b);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.Add(b);
		}
	}
	
	public JVector4 Add(JVector3 other) {
		if (other is JVector3B v3b) {
			return Add(v3b);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.Add(b);
		}
	}
	
	public JVector4 Add(JVector4 other) {
		if (other is JVector4B v4b) {
			return Add(v4b);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.Add(b);
		}
	}
	
	public JVector4I Add(JVector2B other) {
		return Add((JVector4B) other);
	}
	
	public JVector4I Add(JVector3B other) {
		return Add((JVector4B) other);
	}
	
	public JVector4I Add(JVector4B other) {
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
		if (other is JVector2B v2b) {
			return Subtract(v2b);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.Subtract(b);
		}
	}
	
	public JVector4 Subtract(JVector3 other) {
		if (other is JVector3B v3b) {
			return Subtract(v3b);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.Subtract(b);
		}
	}
	
	public JVector4 Subtract(JVector4 other) {
		if (other is JVector4B v4b) {
			return Subtract(v4b);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.Subtract(b);
		}
	}
	
	public JVector4I Subtract(JVector2B other) {
		return Subtract((JVector4B) other);
	}
	
	public JVector4I Subtract(JVector3B other) {
		return Subtract((JVector4B) other);
	}
	
	public JVector4I Subtract(JVector4B other) {
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
		if (other is JVector2B v2b) {
			return SubtractFrom(v2b);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.SubtractFrom(b);
		}
	}
	
	public JVector4 SubtractFrom(JVector3 other) {
		if (other is JVector3B v3b) {
			return SubtractFrom(v3b);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.SubtractFrom(b);
		}
	}
	
	public JVector4 SubtractFrom(JVector4 other) {
		if (other is JVector4B v4b) {
			return SubtractFrom(v4b);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.SubtractFrom(b);
		}
	}
	
	public JVector4I SubtractFrom(JVector2B other) {
		return SubtractFrom((JVector4B) other);
	}
	
	public JVector4I SubtractFrom(JVector3B other) {
		return SubtractFrom((JVector4B) other);
	}
	
	public JVector4I SubtractFrom(JVector4B other) {
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
		if (other is JVector2B v2b) {
			return Multiply(v2b);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.Multiply(b);
		}
	}
	
	public double Multiply(JVector3 other) {
		if (other is JVector3B v3b) {
			return Multiply(v3b);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.Multiply(b);
		}
	}
	
	public double Multiply(JVector4 other) {
		if (other is JVector4B v4b) {
			return Multiply(v4b);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.Multiply(b);
		}
	}
	
	public double Multiply(JVector2B other) {
		return Multiply((JVector4B) other);
	}
	
	public double Multiply(JVector3B other) {
		return Multiply((JVector4B) other);
	}
	
	public double Multiply(JVector4B other) {
		return X * other.X + Y * other.Y + Z * other.Z + W * other.W;
	}
	
	JVector4 JVector4.Multiply(double other) => Multiply(other);
	
	public JVector4D Multiply(double other) {
		return new(X * other, Y * other, Z * other, W * other);
	}
	
	public JVector4F Multiply(float other) {
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
	
	public JVector4F Divide(float other) {
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
	
	public JVector4F DivideFrom(float other) {
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

	bool IEquatable<JVector4B>.Equals(JVector4B other) => Equals(other);
	
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
		else if (v2.InnerType.FitsFloat32()) {
			return Equals((JVector4<float>) v2);
		}
		else if (v2.InnerType.FitsFloat64()) {
			return Equals((JVector4<double>) v2);
		}
		else {
			throw new ArgumentException("Cannot compare JVector4B to JVector4");
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
		else if (type == typeof(UInt32)) {
			return X == other.X.UnboxCast<UInt32>()
			    && Y == other.Y.UnboxCast<UInt32>()
			    && Z == other.Z.UnboxCast<UInt32>()
			    && W == other.W.UnboxCast<UInt32>();
		}
		else if (type == typeof(Int64)) {
			return X == other.X.UnboxCast<Int64>()
			    && Y == other.Y.UnboxCast<Int64>()
			    && Z == other.Z.UnboxCast<Int64>()
			    && W == other.W.UnboxCast<Int64>();
		}
		else if (type == typeof(UInt64)) {
			return X == other.X.UnboxCast<UInt64>()
			    && Y == other.Y.UnboxCast<UInt64>()
			    && Z == other.Z.UnboxCast<UInt64>()
			    && W == other.W.UnboxCast<UInt64>();
		}
		else if (type == typeof(Int128)) {
			return X == other.X.UnboxCast<Int128>()
			    && Y == other.Y.UnboxCast<Int128>()
			    && Z == other.Z.UnboxCast<Int128>()
			    && W == other.W.UnboxCast<Int128>();
		}
		else if (type == typeof(UInt128)) {
			return X == other.X.UnboxCast<UInt128>()
			    && Y == other.Y.UnboxCast<UInt128>()
			    && Z == other.Z.UnboxCast<UInt128>()
			    && W == other.W.UnboxCast<UInt128>();
		}
		else if (type == typeof(BigInteger)) {
			return (BigInteger) X == other.X.UnboxCast<BigInteger>()
			    && (BigInteger) Y == other.Y.UnboxCast<BigInteger>()
			    && (BigInteger) Z == other.Z.UnboxCast<BigInteger>()
			    && (BigInteger) W == other.W.UnboxCast<BigInteger>();
		}
		else if (type == typeof(float)) {
			return X == other.X.UnboxCast<float>()
			    && Y == other.Y.UnboxCast<float>()
			    && Z == other.Z.UnboxCast<float>()
			    && W == other.W.UnboxCast<float>();
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
	public static JVector4B operator + (JVector4B self) => self;
	public static JVector4I operator - (JVector4B self) => self.Negate();
	
	// With vec2
	public static JVector4I operator + (JVector4B lhs, JVector2B rhs) => lhs.Add(rhs);
	public static JVector4  operator + (JVector4B lhs, JVector2  rhs) => lhs.Add(rhs);
	public static JVector4I operator + (JVector2B lhs, JVector4B rhs) => rhs.Add(lhs);
	public static JVector4  operator + (JVector2  lhs, JVector4B rhs) => rhs.Add(lhs);
	public static JVector4I operator - (JVector4B lhs, JVector2B rhs) => lhs.Subtract(rhs);
	public static JVector4  operator - (JVector4B lhs, JVector2  rhs) => lhs.Subtract(rhs);
	public static JVector4I operator - (JVector2B lhs, JVector4B rhs) => rhs.SubtractFrom(lhs);
	public static JVector4  operator - (JVector2  lhs, JVector4B rhs) => rhs.SubtractFrom(lhs);
	public static double    operator * (JVector4B lhs, JVector2B rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector4B lhs, JVector2  rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector2B lhs, JVector4B rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector2  lhs, JVector4B rhs) => rhs.Multiply(lhs);
	
	// With vec3
	public static JVector4I operator + (JVector4B lhs, JVector3B rhs) => lhs.Add(rhs);
	public static JVector4  operator + (JVector4B lhs, JVector3  rhs) => lhs.Add(rhs);
	public static JVector4I operator + (JVector3B lhs, JVector4B rhs) => rhs.Add(lhs);
	public static JVector4  operator + (JVector3  lhs, JVector4B rhs) => rhs.Add(lhs);
	public static JVector4I operator - (JVector4B lhs, JVector3B rhs) => lhs.Subtract(rhs);
	public static JVector4  operator - (JVector4B lhs, JVector3  rhs) => lhs.Subtract(rhs);
	public static JVector4I operator - (JVector3B lhs, JVector4B rhs) => rhs.SubtractFrom(lhs);
	public static JVector4  operator - (JVector3  lhs, JVector4B rhs) => rhs.SubtractFrom(lhs);
	public static double    operator * (JVector4B lhs, JVector3B rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector4B lhs, JVector3  rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector3B lhs, JVector4B rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector3  lhs, JVector4B rhs) => rhs.Multiply(lhs);
	
	// With vec4
	public static JVector4I operator + (JVector4B lhs, JVector4B rhs) => lhs.Add(rhs);
	public static JVector4  operator + (JVector4B lhs, JVector4  rhs) => lhs.Add(rhs);
	public static JVector   operator + (JVector4B lhs, JVector   rhs) => lhs.Add(rhs);
	public static JVector4  operator + (JVector4  lhs, JVector4B rhs) => rhs.Add(lhs);
	public static JVector   operator + (JVector   lhs, JVector4B rhs) => rhs.Add(lhs);
	public static JVector4I operator - (JVector4B lhs, JVector4B rhs) => lhs.Subtract(rhs);
	public static JVector4  operator - (JVector4B lhs, JVector4  rhs) => lhs.Subtract(rhs);
	public static JVector   operator - (JVector4B lhs, JVector   rhs) => lhs.Subtract(rhs);
	public static JVector4  operator - (JVector4  lhs, JVector4B rhs) => rhs.SubtractFrom(lhs);
	public static JVector   operator - (JVector   lhs, JVector4B rhs) => rhs.SubtractFrom(lhs);
	public static double    operator * (JVector4B lhs, JVector4B rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector4B lhs, JVector4  rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector4B lhs, JVector   rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector4  lhs, JVector4B rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector   lhs, JVector4B rhs) => rhs.Multiply(lhs);
	
	public static JVector4D operator * (JVector4B lhs, double    rhs) => lhs.Multiply(rhs);
	public static JVector4D operator * (double    lhs, JVector4B rhs) => rhs.Multiply(lhs);
	public static JVector4D operator / (JVector4B lhs, double    rhs) => lhs.Divide(rhs);
	public static JVector4D operator / (double    lhs, JVector4B rhs) => rhs.DivideFrom(lhs);
	
	public static JVector4F operator * (JVector4B lhs, float     rhs) => lhs.Multiply(rhs);
	public static JVector4F operator * (float     lhs, JVector4B rhs) => rhs.Multiply(lhs);
	public static JVector4F operator / (JVector4B lhs, float     rhs) => lhs.Divide(rhs);
	public static JVector4F operator / (float     lhs, JVector4B rhs) => rhs.DivideFrom(lhs);
	
	public static JVector4I operator * (JVector4B lhs, int       rhs) => lhs.Multiply(rhs);
	public static JVector4I operator * (int       lhs, JVector4B rhs) => rhs.Multiply(lhs);
	public static JVector4I operator / (JVector4B lhs, int       rhs) => lhs.Divide(rhs);
	public static JVector4I operator / (int       lhs, JVector4B rhs) => rhs.DivideFrom(lhs);
	
	public static JVector4L operator * (JVector4B lhs, long      rhs) => lhs.Multiply(rhs);
	public static JVector4L operator * (long      lhs, JVector4B rhs) => rhs.Multiply(lhs);
	public static JVector4L operator / (JVector4B lhs, long      rhs) => lhs.Divide(rhs);
	public static JVector4L operator / (long      lhs, JVector4B rhs) => rhs.DivideFrom(lhs);
	
	// Equality operator
	public static bool operator == (JVector4B lhs, JVector4B rhs) =>  lhs.Equals(rhs);
	public static bool operator == (JVector4B lhs, JVector4  rhs) =>  lhs.Equals(rhs);
	public static bool operator == (JVector4  lhs, JVector4B rhs) =>  rhs.Equals(lhs);
	public static bool operator != (JVector4B lhs, JVector4B rhs) => !lhs.Equals(rhs);
	public static bool operator != (JVector4B lhs, JVector4  rhs) => !lhs.Equals(rhs);
	public static bool operator != (JVector4  lhs, JVector4B rhs) => !rhs.Equals(lhs);
}