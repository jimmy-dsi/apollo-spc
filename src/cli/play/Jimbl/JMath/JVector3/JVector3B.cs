namespace Jimbl.JMath;

using System.Numerics;

public struct JVector3B: JVector3<byte>, IEquatable<JVector3B> {
	byte x;
	byte y;
	byte z;
	Action<int, byte>? setterHook = null;
	
	public Type InnerType => JVector<byte>.Defaults.InnerType();
	
	public (byte, byte, byte) AsTuple => JVector3<byte>.Defaults.AsTuple(this);
	public byte[]             AsArray => JVector3<byte>.Defaults.AsArray(this);
	
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
	
	internal Action<int, byte>? SetterHook {
		get => setterHook;
		set => setterHook = value;
	}
	
	public static explicit operator JVector2B (JVector3B vec) => new(vec.X, vec.Y);
	public static explicit operator JVector2I (JVector3B vec) => new(vec.X, vec.Y);
	public static explicit operator JVector2L (JVector3B vec) => new(vec.X, vec.Y);
	public static explicit operator JVector2F (JVector3B vec) => new(vec.X, vec.Y);
	public static explicit operator JVector2D (JVector3B vec) => new(vec.X, vec.Y);
	
	public static implicit operator JVector3I (JVector3B vec) => new(vec.X, vec.Y, vec.Z);
	public static implicit operator JVector3L (JVector3B vec) => new(vec.X, vec.Y, vec.Z);
	public static implicit operator JVector3F (JVector3B vec) => new(vec.X, vec.Y, vec.Z);
	public static implicit operator JVector3D (JVector3B vec) => new(vec.X, vec.Y, vec.Z);
	
	public static implicit operator JVector4B (JVector3B vec) => new(vec.X, vec.Y, vec.Z, 0);
	public static implicit operator JVector4I (JVector3B vec) => new(vec.X, vec.Y, vec.Z, 0);
	public static implicit operator JVector4L (JVector3B vec) => new(vec.X, vec.Y, vec.Z, 0);
	public static implicit operator JVector4F (JVector3B vec) => new(vec.X, vec.Y, vec.Z, 0);
	public static implicit operator JVector4D (JVector3B vec) => new(vec.X, vec.Y, vec.Z, 0);
	
	public static implicit operator JVector3B ((byte, byte, byte) tup) => new(tup.Item1, tup.Item2, tup.Item3);
	public static explicit operator JVector3B (byte[] arr) => new(arr[0], arr[1], arr[2]);
	
	public JVector3B(byte x, byte y, byte z) {
		X = x;
		Y = y;
		Z = z;
	}
	
	JVector3 JVector3<byte>.Copy<R>(Func<byte, R> transformation) {
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
	
	public JVector3B Copy(Func<byte, byte> transformation) {
		JVector3B result = new();
		for (var i = 0; i < 3; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public JVector3I Copy(Func<byte, Int32> transformation) {
		JVector3I result = new();
		for (var i = 0; i < 3; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public JVector3L Copy(Func<byte, Int64> transformation) {
		JVector3L result = new();
		for (var i = 0; i < 3; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public JVector3F Copy(Func<byte, float> transformation) {
		JVector3F result = new();
		for (var i = 0; i < 3; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public JVector3D Copy(Func<byte, double> transformation) {
		JVector3D result = new();
		for (var i = 0; i < 3; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public void Transform(Func<byte, byte> transformation) => JVector<byte>.Defaults.Transform(ref this, transformation);
	
	public byte this[int itemIndex] {
		get => JVector3<byte>.Defaults.GetThis(this, itemIndex);
		set => JVector3<byte>.Defaults.SetThis(ref this, itemIndex, value);
	}
	
	public double Magnitude => JVector3<byte>.Defaults.Magnitude(this);

	JVector3 JVector3.Negate() => Negate();
	
	public JVector3I Negate() {
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
		if (other is JVector2B v2b) {
			return Add(v2b);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.Add(b);
		}
	}
	
	public JVector3 Add(JVector3 other) {
		if (other is JVector3B v3b) {
			return Add(v3b);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.Add(b);
		}
	}
	
	public JVector3I Add(JVector2B other) {
		return Add((JVector3B) other);
	}
	
	public JVector3I Add(JVector3B other) {
		return new(X + other.X, Y + other.Y, Z + other.Z);
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
		if (other is JVector2B v2b) {
			return Subtract(v2b);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.Subtract(b);
		}
	}
	
	public JVector3 Subtract(JVector3 other) {
		if (other is JVector3B v3b) {
			return Subtract(v3b);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.Subtract(b);
		}
	}
	
	public JVector3I Subtract(JVector2B other) {
		return Subtract((JVector3B) other);
	}
	
	public JVector3I Subtract(JVector3B other) {
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
		if (other is JVector2B v2b) {
			return SubtractFrom(v2b);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.SubtractFrom(b);
		}
	}
	
	public JVector3 SubtractFrom(JVector3 other) {
		if (other is JVector3B v3b) {
			return SubtractFrom(v3b);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.SubtractFrom(b);
		}
	}
	
	public JVector3I SubtractFrom(JVector2B other) {
		return SubtractFrom((JVector3B) other);
	}
	
	public JVector3I SubtractFrom(JVector3B other) {
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
		if (other is JVector2B v2b) {
			return Multiply(v2b);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.Multiply(b);
		}
	}
	
	public double Multiply(JVector3 other) {
		if (other is JVector3B v3b) {
			return Multiply(v3b);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.Multiply(b);
		}
	}
	
	public double Multiply(JVector2B other) {
		return Multiply((JVector3B) other);
	}
	
	public double Multiply(JVector3B other) {
		return X * other.X + Y * other.Y + Z * other.Z;
	}
	
	JVector3 JVector3.Multiply(double other) => Multiply(other);
	
	public JVector3D Multiply(double other) {
		return new(X * other, Y * other, Z * other);
	}
	
	public JVector3F Multiply(float other) {
		return new(X * other, Y * other, Z * other);
	}
	
	public JVector3I Multiply(int other) {
		return new(X * other, Y * other, Z * other);
	}
	
	public JVector3L Multiply(long other) {
		return new(X * other, Y * other, Z * other);
	}
	
	JVector3 JVector3.Divide(double other) => Divide(other);
	
	public JVector3D Divide(double other) {
		return new(X / other, Y / other, Z / other);
	}
	
	public JVector3F Divide(float other) {
		return new(X / other, Y / other, Z / other);
	}
	
	public JVector3I Divide(int other) {
		return new(X / other, Y / other, Z / other);
	}
	
	public JVector3L Divide(long other) {
		return new(X / other, Y / other, Z / other);
	}
	
	JVector3 JVector3.DivideFrom(double other) => DivideFrom(other);
	
	public JVector3D DivideFrom(double other) {
		return new(other / X, other / Y, other / Z);
	}
	
	public JVector3F DivideFrom(float other) {
		return new(other / X, other / Y, other / Z);
	}
	
	public JVector3I DivideFrom(int other) {
		return new(other / X, other / Y, other / Z);
	}
	
	public JVector3L DivideFrom(long other) {
		return new(other / X, other / Y, other / Z);
	}

	public override int GetHashCode() {
		return (X, Y, Z).GetHashCode();
	}

	bool IEquatable<JVector3B>.Equals(JVector3B other) => Equals(other);
	
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
		
		if (v2.InnerType.FitsInt32()) {
			return Equals((JVector3<Int32>) v2);
		}
		else if (v2.InnerType.FitsInt64()) {
			return Equals((JVector3<Int64>) v2);
		}
		else if (v2.InnerType.FitsFloat32()) {
			return Equals((JVector3<float>) v2);
		}
		else if (v2.InnerType.FitsFloat64()) {
			return Equals((JVector3<double>) v2);
		}
		else {
			throw new ArgumentException("Cannot compare JVector3B to JVector3");
		}
	}
	
	public bool Equals<T>(JVector3<T> other) where T: INumber<T> {
		var type = JVector.PromoteType(this, other);
		
		if (type == typeof(Int32)) {
			return X == other.X.UnboxCast<Int32>()
			    && Y == other.Y.UnboxCast<Int32>()
			    && Z == other.Z.UnboxCast<Int32>();
		}
		else if (type == typeof(UInt32)) {
			return X == other.X.UnboxCast<UInt32>()
			    && Y == other.Y.UnboxCast<UInt32>()
			    && Z == other.Z.UnboxCast<UInt32>();
		}
		else if (type == typeof(Int64)) {
			return X == other.X.UnboxCast<Int64>()
			    && Y == other.Y.UnboxCast<Int64>()
			    && Z == other.Z.UnboxCast<Int64>();
		}
		else if (type == typeof(UInt64)) {
			return X == other.X.UnboxCast<UInt64>()
			    && Y == other.Y.UnboxCast<UInt64>()
			    && Z == other.Z.UnboxCast<UInt64>();
		}
		else if (type == typeof(Int128)) {
			return X == other.X.UnboxCast<Int128>()
			    && Y == other.Y.UnboxCast<Int128>()
			    && Z == other.Z.UnboxCast<Int128>();
		}
		else if (type == typeof(UInt128)) {
			return X == other.X.UnboxCast<UInt128>()
			    && Y == other.Y.UnboxCast<UInt128>()
			    && Z == other.Z.UnboxCast<UInt128>();
		}
		else if (type == typeof(BigInteger)) {
			return (BigInteger) X == other.X.UnboxCast<BigInteger>()
			    && (BigInteger) Y == other.Y.UnboxCast<BigInteger>()
			    && (BigInteger) Z == other.Z.UnboxCast<BigInteger>();
		}
		else if (type == typeof(float)) {
			return X == other.X.UnboxCast<float>()
			    && Y == other.Y.UnboxCast<float>()
			    && Z == other.Z.UnboxCast<float>();
		}
		else if (type == typeof(double)) {
			return X == other.X.UnboxCast<double>()
			    && Y == other.Y.UnboxCast<double>()
			    && Z == other.Z.UnboxCast<double>();
		}
		else if (type == typeof(decimal)) {
			return X == other.X.UnboxCast<decimal>()
			    && Y == other.Y.UnboxCast<decimal>()
			    && Z == other.Z.UnboxCast<decimal>();
		}
		else if (type == typeof(Complex)) {
			return X == other.X.UnboxCast<Complex>()
			    && Y == other.Y.UnboxCast<Complex>()
			    && Z == other.Z.UnboxCast<Complex>();
		}
		else {
			return (object) X == (object) other.X
			    && (object) Y == (object) other.Y
			    && (object) Z == (object) other.Z;
		}
	}
	
	// Unary
	public static JVector3B operator + (JVector3B self) => self;
	public static JVector3I operator - (JVector3B self) => self.Negate();
	
	// With vec2
	public static JVector3I operator + (JVector3B lhs, JVector2B rhs) => lhs.Add(rhs);
	public static JVector3  operator + (JVector3B lhs, JVector2  rhs) => lhs.Add(rhs);
	public static JVector3I operator + (JVector2B lhs, JVector3B rhs) => rhs.Add(lhs);
	public static JVector3  operator + (JVector2  lhs, JVector3B rhs) => rhs.Add(lhs);
	public static JVector3I operator - (JVector3B lhs, JVector2B rhs) => lhs.Subtract(rhs);
	public static JVector3  operator - (JVector3B lhs, JVector2  rhs) => lhs.Subtract(rhs);
	public static JVector3I operator - (JVector2B lhs, JVector3B rhs) => rhs.SubtractFrom(lhs);
	public static JVector3  operator - (JVector2  lhs, JVector3B rhs) => rhs.SubtractFrom(lhs);
	public static double    operator * (JVector3B lhs, JVector2B rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector3B lhs, JVector2  rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector2B lhs, JVector3B rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector2  lhs, JVector3B rhs) => rhs.Multiply(lhs);
	
	// With vec3
	public static JVector3I operator + (JVector3B lhs, JVector3B rhs) => lhs.Add(rhs);
	public static JVector3  operator + (JVector3B lhs, JVector3  rhs) => lhs.Add(rhs);
	public static JVector   operator + (JVector3B lhs, JVector   rhs) => lhs.Add(rhs);
	public static JVector3  operator + (JVector3  lhs, JVector3B rhs) => rhs.Add(lhs);
	public static JVector   operator + (JVector   lhs, JVector3B rhs) => rhs.Add(lhs);
	public static JVector3I operator - (JVector3B lhs, JVector3B rhs) => lhs.Subtract(rhs);
	public static JVector3  operator - (JVector3B lhs, JVector3  rhs) => lhs.Subtract(rhs);
	public static JVector   operator - (JVector3B lhs, JVector   rhs) => lhs.Subtract(rhs);
	public static JVector3  operator - (JVector3  lhs, JVector3B rhs) => rhs.SubtractFrom(lhs);
	public static JVector   operator - (JVector   lhs, JVector3B rhs) => rhs.SubtractFrom(lhs);
	public static double    operator * (JVector3B lhs, JVector3B rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector3B lhs, JVector3  rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector3B lhs, JVector   rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector3  lhs, JVector3B rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector   lhs, JVector3B rhs) => rhs.Multiply(lhs);
	
	public static JVector3D operator * (JVector3B lhs, double    rhs) => lhs.Multiply(rhs);
	public static JVector3D operator * (double    lhs, JVector3B rhs) => rhs.Multiply(lhs);
	public static JVector3D operator / (JVector3B lhs, double    rhs) => lhs.Divide(rhs);
	public static JVector3D operator / (double    lhs, JVector3B rhs) => rhs.DivideFrom(lhs);
	
	public static JVector3F operator * (JVector3B lhs, float     rhs) => lhs.Multiply(rhs);
	public static JVector3F operator * (float     lhs, JVector3B rhs) => rhs.Multiply(lhs);
	public static JVector3F operator / (JVector3B lhs, float     rhs) => lhs.Divide(rhs);
	public static JVector3F operator / (float     lhs, JVector3B rhs) => rhs.DivideFrom(lhs);
	
	public static JVector3I operator * (JVector3B lhs, int       rhs) => lhs.Multiply(rhs);
	public static JVector3I operator * (int       lhs, JVector3B rhs) => rhs.Multiply(lhs);
	public static JVector3I operator / (JVector3B lhs, int       rhs) => lhs.Divide(rhs);
	public static JVector3I operator / (int       lhs, JVector3B rhs) => rhs.DivideFrom(lhs);
	
	public static JVector3L operator * (JVector3B lhs, long      rhs) => lhs.Multiply(rhs);
	public static JVector3L operator * (long      lhs, JVector3B rhs) => rhs.Multiply(lhs);
	public static JVector3L operator / (JVector3B lhs, long      rhs) => lhs.Divide(rhs);
	public static JVector3L operator / (long      lhs, JVector3B rhs) => rhs.DivideFrom(lhs);
	
	// Equality operator
	public static bool operator == (JVector3B lhs, JVector3B rhs) =>  lhs.Equals(rhs);
	public static bool operator == (JVector3B lhs, JVector3  rhs) =>  lhs.Equals(rhs);
	public static bool operator == (JVector3  lhs, JVector3B rhs) =>  rhs.Equals(lhs);
	public static bool operator != (JVector3B lhs, JVector3B rhs) => !lhs.Equals(rhs);
	public static bool operator != (JVector3B lhs, JVector3  rhs) => !lhs.Equals(rhs);
	public static bool operator != (JVector3  lhs, JVector3B rhs) => !rhs.Equals(lhs);
}