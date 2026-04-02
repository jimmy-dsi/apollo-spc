namespace Jimbl.JMath;

using System.Numerics;

public struct JVector4D: JVector4<double>, IEquatable<JVector4D> {
	double x;
	double y;
	double z;
	double w;
	Action<int, double>? setterHook = null;
	
	public Type InnerType => JVector<double>.Defaults.InnerType();
	
	public (double, double, double, double) AsTuple => JVector4<double>.Defaults.AsTuple(this);
	public double[]                         AsArray => JVector4<double>.Defaults.AsArray(this);
	
	public double X {
		get => x;
		set {
			x = value;
			setterHook?.Invoke(0, value);
		}
	}

	public double Y {
		get => y;
		set {
			y = value;
			setterHook?.Invoke(1, value);
		}
	}

	public double Z {
		get => z;
		set {
			z = value;
			setterHook?.Invoke(2, value);
		}
	}

	public double W {
		get => w;
		set {
			w = value;
			setterHook?.Invoke(3, value);
		}
	}
	
	internal Action<int, double>? SetterHook {
		get => setterHook;
		set => setterHook = value;
	}
	
	public static explicit operator JVector2B (JVector4D vec) => new((byte)  vec.X, (byte)  vec.Y);
	public static explicit operator JVector2I (JVector4D vec) => new((int)   vec.X, (int)   vec.Y);
	public static explicit operator JVector2L (JVector4D vec) => new((long)  vec.X, (long)  vec.Y);
	public static explicit operator JVector2F (JVector4D vec) => new((float) vec.X, (float) vec.Y);
	public static explicit operator JVector2D (JVector4D vec) => new(        vec.X,         vec.Y);
	
	public static explicit operator JVector3B (JVector4D vec) => new((byte)  vec.X, (byte)  vec.Y, (byte)  vec.Z);
	public static explicit operator JVector3I (JVector4D vec) => new((int)   vec.X, (int)   vec.Y, (int)   vec.Z);
	public static explicit operator JVector3L (JVector4D vec) => new((long)  vec.X, (long)  vec.Y, (long)  vec.Z);
	public static explicit operator JVector3F (JVector4D vec) => new((float) vec.X, (float) vec.Y, (float) vec.Z);
	public static explicit operator JVector3D (JVector4D vec) => new(        vec.X,         vec.Y,         vec.Z);
	
	public static explicit operator JVector4B (JVector4D vec) => new((byte)  vec.X, (byte)  vec.Y, (byte)  vec.Z, (byte)  vec.W);
	public static explicit operator JVector4I (JVector4D vec) => new((int)   vec.X, (int)   vec.Y, (int)   vec.Z, (int)   vec.W);
	public static explicit operator JVector4L (JVector4D vec) => new((long)  vec.X, (long)  vec.Y, (long)  vec.Z, (long)  vec.W);
	public static explicit operator JVector4F (JVector4D vec) => new((float) vec.X, (float) vec.Y, (float) vec.Z, (float) vec.W);
	
	public static implicit operator JVector4D ((double, double, double, double) tup) => new(tup.Item1, tup.Item2, tup.Item3, tup.Item4);
	public static explicit operator JVector4D (double[] arr)                         => new(arr[0], arr[1], arr[2], arr[3]);
	
	public JVector4D(double x, double y, double z, double w) {
		X = x;
		Y = y;
		Z = z;
		W = w;
	}
	
	JVector4 JVector4<double>.Copy<R>(Func<double, R> transformation) {
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
	
	public JVector4B Copy(Func<double, byte> transformation) {
		JVector4B result = new();
		for (var i = 0; i < 4; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public JVector4I Copy(Func<double, Int32> transformation) {
		JVector4I result = new();
		for (var i = 0; i < 4; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public JVector4L Copy(Func<double, Int64> transformation) {
		JVector4L result = new();
		for (var i = 0; i < 4; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public JVector4F Copy(Func<double, float> transformation) {
		JVector4F result = new();
		for (var i = 0; i < 4; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public JVector4D Copy(Func<double, double> transformation) {
		JVector4D result = new();
		for (var i = 0; i < 4; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public void Transform(Func<double, double> transformation) => JVector<double>.Defaults.Transform(ref this, transformation);
	
	public double this[int itemIndex] {
		get => JVector4<double>.Defaults.GetThis(this, itemIndex);
		set => JVector4<double>.Defaults.SetThis(ref this, itemIndex, value);
	}
	
	public double Magnitude => JVector4<double>.Defaults.Magnitude(this);

	JVector4 JVector4.Negate() => Negate();
	
	public JVector4D Negate() {
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
		if (other is JVector2D v2d) {
			return Add(v2d);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.Add(b);
		}
	}
	
	public JVector4 Add(JVector3 other) {
		if (other is JVector3D v3d) {
			return Add(v3d);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.Add(b);
		}
	}
	
	public JVector4 Add(JVector4 other) {
		if (other is JVector4D v4d) {
			return Add(v4d);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.Add(b);
		}
	}
	
	public JVector4D Add(JVector2D other) {
		return Add((JVector4D) other);
	}
	
	public JVector4D Add(JVector3D other) {
		return Add((JVector4D) other);
	}
	
	public JVector4D Add(JVector4D other) {
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
		if (other is JVector2D v2d) {
			return Subtract(v2d);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.Subtract(b);
		}
	}
	
	public JVector4 Subtract(JVector3 other) {
		if (other is JVector3D v3d) {
			return Subtract(v3d);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.Subtract(b);
		}
	}
	
	public JVector4 Subtract(JVector4 other) {
		if (other is JVector4D v4d) {
			return Subtract(v4d);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.Subtract(b);
		}
	}
	
	public JVector4D Subtract(JVector2D other) {
		return Subtract((JVector4D) other);
	}
	
	public JVector4D Subtract(JVector3D other) {
		return Subtract((JVector4D) other);
	}
	
	public JVector4D Subtract(JVector4D other) {
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
		if (other is JVector2D v2d) {
			return SubtractFrom(v2d);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.SubtractFrom(b);
		}
	}
	
	public JVector4 SubtractFrom(JVector3 other) {
		if (other is JVector3D v3d) {
			return SubtractFrom(v3d);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.SubtractFrom(b);
		}
	}
	
	public JVector4 SubtractFrom(JVector4 other) {
		if (other is JVector4D v4d) {
			return SubtractFrom(v4d);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.SubtractFrom(b);
		}
	}
	
	public JVector4D SubtractFrom(JVector2D other) {
		return SubtractFrom((JVector4D) other);
	}
	
	public JVector4D SubtractFrom(JVector3D other) {
		return SubtractFrom((JVector4D) other);
	}
	
	public JVector4D SubtractFrom(JVector4D other) {
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
		if (other is JVector2D v2d) {
			return Multiply(v2d);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.Multiply(b);
		}
	}
	
	public double Multiply(JVector3 other) {
		if (other is JVector3D v3d) {
			return Multiply(v3d);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.Multiply(b);
		}
	}
	
	public double Multiply(JVector4 other) {
		if (other is JVector4D v4d) {
			return Multiply(v4d);
		}
		else {
			var (a, b) = JVector4.Promote(this, other);
			return a.Multiply(b);
		}
	}
	
	public double Multiply(JVector2D other) {
		return Multiply((JVector4D) other);
	}
	
	public double Multiply(JVector3D other) {
		return Multiply((JVector4D) other);
	}
	
	public double Multiply(JVector4D other) {
		return X * other.X + Y * other.Y + Z * other.Z + W * other.W;
	}
	
	JVector4 JVector4.Multiply(double other) => Multiply(other);
	
	public JVector4D Multiply(double other) {
		return new(X * other, Y * other, Z * other, W * other);
	}
	
	JVector4 JVector4.Divide(double other) => Divide(other);
	
	public JVector4D Divide(double other) {
		return new(X / other, Y / other, Z / other, W / other);
	}
	
	JVector4 JVector4.DivideFrom(double other) => DivideFrom(other);
	
	public JVector4D DivideFrom(double other) {
		return new(other / X, other / Y, other / Z, other / W);
	}

	public override int GetHashCode() {
		return (X, Y, Z, W).GetHashCode();
	}

	bool IEquatable<JVector4D>.Equals(JVector4D other) => Equals(other);
	
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
		
		if (v2.InnerType.FitsFloat64()) {
			return Equals((JVector4<double>) v2);
		}
		else {
			throw new ArgumentException("Cannot compare JVector4D to JVector4");
		}
	}
	
	public bool Equals<T>(JVector4<T> other) where T: INumber<T> {
		var type = JVector.PromoteType(this, other);
		
		if (type == typeof(double)) {
			return X == other.X.UnboxCast<double>()
			    && Y == other.Y.UnboxCast<double>()
			    && Z == other.Z.UnboxCast<double>()
			    && W == other.W.UnboxCast<double>();
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
	public static JVector4D operator + (JVector4D self) => self;
	public static JVector4D operator - (JVector4D self) => self.Negate();
	
	// With vec2
	public static JVector4D operator + (JVector4D lhs, JVector2D rhs) => lhs.Add(rhs);
	public static JVector4D operator + (JVector4D lhs, JVector2F rhs) => lhs.Add(rhs);
	public static JVector4D operator + (JVector4D lhs, JVector2I rhs) => lhs.Add(rhs);
	public static JVector4D operator + (JVector4D lhs, JVector2B rhs) => lhs.Add(rhs);
	public static JVector4  operator + (JVector4D lhs, JVector2  rhs) => lhs.Add(rhs);
	public static JVector4D operator + (JVector2D lhs, JVector4D rhs) => rhs.Add(lhs);
	public static JVector4D operator + (JVector2F lhs, JVector4D rhs) => rhs.Add(lhs);
	public static JVector4D operator + (JVector2I lhs, JVector4D rhs) => rhs.Add(lhs);
	public static JVector4D operator + (JVector2B lhs, JVector4D rhs) => rhs.Add(lhs);
	public static JVector4  operator + (JVector2  lhs, JVector4D rhs) => rhs.Add(lhs);
	public static JVector4D operator - (JVector4D lhs, JVector2D rhs) => lhs.Subtract(rhs);
	public static JVector4D operator - (JVector4D lhs, JVector2F rhs) => lhs.Subtract(rhs);
	public static JVector4D operator - (JVector4D lhs, JVector2I rhs) => lhs.Subtract(rhs);
	public static JVector4D operator - (JVector4D lhs, JVector2B rhs) => lhs.Subtract(rhs);
	public static JVector4  operator - (JVector4D lhs, JVector2  rhs) => lhs.Subtract(rhs);
	public static JVector4D operator - (JVector2D lhs, JVector4D rhs) => rhs.SubtractFrom(lhs);
	public static JVector4D operator - (JVector2F lhs, JVector4D rhs) => rhs.SubtractFrom(lhs);
	public static JVector4D operator - (JVector2I lhs, JVector4D rhs) => rhs.SubtractFrom(lhs);
	public static JVector4D operator - (JVector2B lhs, JVector4D rhs) => rhs.SubtractFrom(lhs);
	public static JVector4  operator - (JVector2  lhs, JVector4D rhs) => rhs.SubtractFrom(lhs);
	public static double    operator * (JVector4D lhs, JVector2D rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector4D lhs, JVector2F rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector4D lhs, JVector2I rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector4D lhs, JVector2B rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector4D lhs, JVector2  rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector2D lhs, JVector4D rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector2F lhs, JVector4D rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector2I lhs, JVector4D rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector2B lhs, JVector4D rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector2  lhs, JVector4D rhs) => rhs.Multiply(lhs);
	
	// With vec3
	public static JVector4D operator + (JVector4D lhs, JVector3D rhs) => lhs.Add(rhs);
	public static JVector4D operator + (JVector4D lhs, JVector3F rhs) => lhs.Add(rhs);
	public static JVector4D operator + (JVector4D lhs, JVector3I rhs) => lhs.Add(rhs);
	public static JVector4D operator + (JVector4D lhs, JVector3B rhs) => lhs.Add(rhs);
	public static JVector4  operator + (JVector4D lhs, JVector3  rhs) => lhs.Add(rhs);
	public static JVector4D operator + (JVector3D lhs, JVector4D rhs) => rhs.Add(lhs);
	public static JVector4D operator + (JVector3F lhs, JVector4D rhs) => rhs.Add(lhs);
	public static JVector4D operator + (JVector3I lhs, JVector4D rhs) => rhs.Add(lhs);
	public static JVector4D operator + (JVector3B lhs, JVector4D rhs) => rhs.Add(lhs);
	public static JVector4  operator + (JVector3  lhs, JVector4D rhs) => rhs.Add(lhs);
	public static JVector4D operator - (JVector4D lhs, JVector3D rhs) => lhs.Subtract(rhs);
	public static JVector4D operator - (JVector4D lhs, JVector3F rhs) => lhs.Subtract(rhs);
	public static JVector4D operator - (JVector4D lhs, JVector3I rhs) => lhs.Subtract(rhs);
	public static JVector4D operator - (JVector4D lhs, JVector3B rhs) => lhs.Subtract(rhs);
	public static JVector4  operator - (JVector4D lhs, JVector3  rhs) => lhs.Subtract(rhs);
	public static JVector4D operator - (JVector3D lhs, JVector4D rhs) => rhs.SubtractFrom(lhs);
	public static JVector4D operator - (JVector3F lhs, JVector4D rhs) => rhs.SubtractFrom(lhs);
	public static JVector4D operator - (JVector3I lhs, JVector4D rhs) => rhs.SubtractFrom(lhs);
	public static JVector4D operator - (JVector3B lhs, JVector4D rhs) => rhs.SubtractFrom(lhs);
	public static JVector4  operator - (JVector3  lhs, JVector4D rhs) => rhs.SubtractFrom(lhs);
	public static double    operator * (JVector4D lhs, JVector3D rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector4D lhs, JVector3F rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector4D lhs, JVector3I rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector4D lhs, JVector3B rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector4D lhs, JVector3  rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector3D lhs, JVector4D rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector3F lhs, JVector4D rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector3I lhs, JVector4D rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector3B lhs, JVector4D rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector3  lhs, JVector4D rhs) => rhs.Multiply(lhs);
	
	// With vec4
	public static JVector4D operator + (JVector4D lhs, JVector4D rhs) => lhs.Add(rhs);
	public static JVector4D operator + (JVector4D lhs, JVector4F rhs) => lhs.Add(rhs);
	public static JVector4D operator + (JVector4D lhs, JVector4I rhs) => lhs.Add(rhs);
	public static JVector4D operator + (JVector4D lhs, JVector4B rhs) => lhs.Add(rhs);
	public static JVector4  operator + (JVector4D lhs, JVector4  rhs) => lhs.Add(rhs);
	public static JVector4D operator + (JVector4F lhs, JVector4D rhs) => rhs.Add(lhs);
	public static JVector4D operator + (JVector4I lhs, JVector4D rhs) => rhs.Add(lhs);
	public static JVector4D operator + (JVector4B lhs, JVector4D rhs) => rhs.Add(lhs);
	public static JVector4  operator + (JVector4  lhs, JVector4D rhs) => rhs.Add(lhs);
	public static JVector4D operator - (JVector4D lhs, JVector4D rhs) => lhs.Subtract(rhs);
	public static JVector4D operator - (JVector4D lhs, JVector4F rhs) => lhs.Subtract(rhs);
	public static JVector4D operator - (JVector4D lhs, JVector4I rhs) => lhs.Subtract(rhs);
	public static JVector4D operator - (JVector4D lhs, JVector4B rhs) => lhs.Subtract(rhs);
	public static JVector4  operator - (JVector4D lhs, JVector4  rhs) => lhs.Subtract(rhs);
	public static JVector4D operator - (JVector4F lhs, JVector4D rhs) => rhs.SubtractFrom(lhs);
	public static JVector4D operator - (JVector4I lhs, JVector4D rhs) => rhs.SubtractFrom(lhs);
	public static JVector4D operator - (JVector4B lhs, JVector4D rhs) => rhs.SubtractFrom(lhs);
	public static JVector4  operator - (JVector4  lhs, JVector4D rhs) => rhs.SubtractFrom(lhs);
	public static double    operator * (JVector4D lhs, JVector4D rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector4D lhs, JVector4F rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector4D lhs, JVector4I rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector4D lhs, JVector4B rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector4D lhs, JVector4  rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector4F lhs, JVector4D rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector4I lhs, JVector4D rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector4B lhs, JVector4D rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector4  lhs, JVector4D rhs) => rhs.Multiply(lhs);
	
	public static JVector4D operator * (JVector4D lhs, double    rhs) => lhs.Multiply(rhs);
	public static JVector4D operator * (double    lhs, JVector4D rhs) => rhs.Multiply(lhs);
	public static JVector4D operator / (JVector4D lhs, double    rhs) => lhs.Divide(rhs);
	public static JVector4D operator / (double    lhs, JVector4D rhs) => rhs.DivideFrom(lhs);
	
	// Equality operator
	public static bool operator == (JVector4D lhs, JVector4D rhs) =>  lhs.Equals(rhs);
	public static bool operator == (JVector4D lhs, JVector4  rhs) =>  lhs.Equals(rhs);
	public static bool operator == (JVector4  lhs, JVector4D rhs) =>  rhs.Equals(lhs);
	public static bool operator != (JVector4D lhs, JVector4D rhs) => !lhs.Equals(rhs);
	public static bool operator != (JVector4D lhs, JVector4  rhs) => !lhs.Equals(rhs);
	public static bool operator != (JVector4  lhs, JVector4D rhs) => !rhs.Equals(lhs);
}