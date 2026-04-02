namespace Jimbl.JMath;

using System.Numerics;

public struct JVector3D: JVector3<double>, IEquatable<JVector3D> {
	double x;
	double y;
	double z;
	Action<int, double>? setterHook = null;
	
	public Type InnerType => JVector<double>.Defaults.InnerType();
	
	public (double, double, double) AsTuple => JVector3<double>.Defaults.AsTuple(this);
	public double[]                 AsArray => JVector3<double>.Defaults.AsArray(this);
	
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
	
	internal Action<int, double>? SetterHook {
		get => setterHook;
		set => setterHook = value;
	}
	
	public static explicit operator JVector2B (JVector3D vec) => new((byte)  vec.X, (byte)  vec.Y);
	public static explicit operator JVector2I (JVector3D vec) => new((int)   vec.X, (int)   vec.Y);
	public static explicit operator JVector2L (JVector3D vec) => new((long)  vec.X, (long)  vec.Y);
	public static explicit operator JVector2F (JVector3D vec) => new((float) vec.X, (float) vec.Y);
	public static explicit operator JVector2D (JVector3D vec) => new(        vec.X,         vec.Y);
	
	public static explicit operator JVector3B (JVector3D vec) => new((byte)  vec.X, (byte)  vec.Y, (byte)  vec.Z);
	public static explicit operator JVector3I (JVector3D vec) => new((int)   vec.X, (int)   vec.Y, (int)   vec.Z);
	public static explicit operator JVector3L (JVector3D vec) => new((long)  vec.X, (long)  vec.Y, (long)  vec.Z);
	public static explicit operator JVector3F (JVector3D vec) => new((float) vec.X, (float) vec.Y, (float) vec.Z);
	
	public static explicit operator JVector4B (JVector3D vec) => new((byte)  vec.X, (byte)  vec.Y, (byte)  vec.Z, 0);
	public static explicit operator JVector4I (JVector3D vec) => new((int)   vec.X, (int)   vec.Y, (int)   vec.Z, 0);
	public static explicit operator JVector4L (JVector3D vec) => new((long)  vec.X, (long)  vec.Y, (long)  vec.Z, 0);
	public static explicit operator JVector4F (JVector3D vec) => new((float) vec.X, (float) vec.Y, (float) vec.Z, 0);
	public static implicit operator JVector4D (JVector3D vec) => new(        vec.X,         vec.Y,         vec.Z, 0);
	
	public static implicit operator JVector3D ((double, double, double) tup) => new(tup.Item1, tup.Item2, tup.Item3);
	public static explicit operator JVector3D (double[] arr) => new(arr[0], arr[1], arr[2]);
	
	public JVector3D(double x, double y, double z) {
		X = x;
		Y = y;
		Z = z;
	}
	
	JVector3 JVector3<double>.Copy<R>(Func<double, R> transformation) {
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
	
	public JVector3B Copy(Func<double, byte> transformation) {
		JVector3B result = new();
		for (var i = 0; i < 3; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public JVector3I Copy(Func<double, Int32> transformation) {
		JVector3I result = new();
		for (var i = 0; i < 3; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public JVector3L Copy(Func<double, Int64> transformation) {
		JVector3L result = new();
		for (var i = 0; i < 3; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public JVector3F Copy(Func<double, float> transformation) {
		JVector3F result = new();
		for (var i = 0; i < 3; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public JVector3D Copy(Func<double, double> transformation) {
		JVector3D result = new();
		for (var i = 0; i < 3; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public void Transform(Func<double, double> transformation) => JVector<double>.Defaults.Transform(ref this, transformation);
	
	public double this[int itemIndex] {
		get => JVector3<double>.Defaults.GetThis(this, itemIndex);
		set => JVector3<double>.Defaults.SetThis(ref this, itemIndex, value);
	}
	
	public double Magnitude => JVector3<double>.Defaults.Magnitude(this);
	
	JVector3 JVector3.Negate() => Negate();
	
	public JVector3D Negate() {
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
		if (other is JVector2D v2d) {
			return Add(v2d);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.Add(b);
		}
	}
	
	public JVector3 Add(JVector3 other) {
		if (other is JVector3D v3d) {
			return Add(v3d);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.Add(b);
		}
	}
	
	public JVector3D Add(JVector2D other) {
		return Add((JVector3D) other);
	}
	
	public JVector3D Add(JVector3D other) {
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
		if (other is JVector2D v2d) {
			return Subtract(v2d);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.Subtract(b);
		}
	}
	
	public JVector3 Subtract(JVector3 other) {
		if (other is JVector3D v3d) {
			return Subtract(v3d);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.Subtract(b);
		}
	}
	
	public JVector3D Subtract(JVector2D other) {
		return Subtract((JVector3D) other);
	}
	
	public JVector3D Subtract(JVector3D other) {
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
		if (other is JVector2D v2d) {
			return SubtractFrom(v2d);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.SubtractFrom(b);
		}
	}
	
	public JVector3 SubtractFrom(JVector3 other) {
		if (other is JVector3D v3d) {
			return SubtractFrom(v3d);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.SubtractFrom(b);
		}
	}
	
	public JVector3D SubtractFrom(JVector2D other) {
		return SubtractFrom((JVector3D) other);
	}
	
	public JVector3D SubtractFrom(JVector3D other) {
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
		if (other is JVector2D v2d) {
			return Multiply(v2d);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.Multiply(b);
		}
	}
	
	public double Multiply(JVector3 other) {
		if (other is JVector3D v3d) {
			return Multiply(v3d);
		}
		else {
			var (a, b) = JVector3.Promote(this, other);
			return a.Multiply(b);
		}
	}
	
	public double Multiply(JVector2D other) {
		return Multiply((JVector3D) other);
	}
	
	public double Multiply(JVector3D other) {
		return X * other.X + Y * other.Y + Z * other.Z;
	}
	
	JVector3 JVector3.Multiply(double other) => Multiply(other);
	
	public JVector3D Multiply(double other) {
		return new(X * other, Y * other, Z * other);
	}
	
	JVector3 JVector3.Divide(double other) => Divide(other);
	
	public JVector3D Divide(double other) {
		return new(X / other, Y / other, Z / other);
	}
	
	JVector3 JVector3.DivideFrom(double other) => DivideFrom(other);
	
	public JVector3D DivideFrom(double other) {
		return new(other / X, other / Y, other / Z);
	}

	public override int GetHashCode() {
		return (X, Y, Z).GetHashCode();
	}

	bool IEquatable<JVector3D>.Equals(JVector3D other) => Equals(other);
	
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
		
		if (v2.InnerType.FitsFloat64()) {
			return Equals((JVector3<double>) v2);
		}
		else {
			throw new ArgumentException("Cannot compare JVector3D to JVector3");
		}
	}
	
	public bool Equals<T>(JVector3<T> other) where T: INumber<T> {
		var type = JVector.PromoteType(this, other);
		
		if (type == typeof(double)) {
			return X == other.X.UnboxCast<double>()
			    && Y == other.Y.UnboxCast<double>()
			    && Z == other.Z.UnboxCast<double>();
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
	public static JVector3D operator + (JVector3D self) => self;
	public static JVector3D operator - (JVector3D self) => self.Negate();
	
	// With vec2
	public static JVector3D operator + (JVector3D lhs, JVector2D rhs) => lhs.Add(rhs);
	public static JVector3D operator + (JVector3D lhs, JVector2F rhs) => lhs.Add(rhs);
	public static JVector3D operator + (JVector3D lhs, JVector2I rhs) => lhs.Add(rhs);
	public static JVector3D operator + (JVector3D lhs, JVector2B rhs) => lhs.Add(rhs);
	public static JVector3  operator + (JVector3D lhs, JVector2  rhs) => lhs.Add(rhs);
	public static JVector3D operator + (JVector2D lhs, JVector3D rhs) => rhs.Add(lhs);
	public static JVector3D operator + (JVector2F lhs, JVector3D rhs) => rhs.Add(lhs);
	public static JVector3D operator + (JVector2I lhs, JVector3D rhs) => rhs.Add(lhs);
	public static JVector3D operator + (JVector2B lhs, JVector3D rhs) => rhs.Add(lhs);
	public static JVector3  operator + (JVector2  lhs, JVector3D rhs) => rhs.Add(lhs);
	public static JVector3D operator - (JVector3D lhs, JVector2D rhs) => lhs.Subtract(rhs);
	public static JVector3D operator - (JVector3D lhs, JVector2F rhs) => lhs.Subtract(rhs);
	public static JVector3D operator - (JVector3D lhs, JVector2I rhs) => lhs.Subtract(rhs);
	public static JVector3D operator - (JVector3D lhs, JVector2B rhs) => lhs.Subtract(rhs);
	public static JVector3  operator - (JVector3D lhs, JVector2  rhs) => lhs.Subtract(rhs);
	public static JVector3D operator - (JVector2D lhs, JVector3D rhs) => rhs.SubtractFrom(lhs);
	public static JVector3D operator - (JVector2F lhs, JVector3D rhs) => rhs.SubtractFrom(lhs);
	public static JVector3D operator - (JVector2I lhs, JVector3D rhs) => rhs.SubtractFrom(lhs);
	public static JVector3D operator - (JVector2B lhs, JVector3D rhs) => rhs.SubtractFrom(lhs);
	public static JVector3  operator - (JVector2  lhs, JVector3D rhs) => rhs.SubtractFrom(lhs);
	public static double    operator * (JVector3D lhs, JVector2D rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector3D lhs, JVector2F rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector3D lhs, JVector2I rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector3D lhs, JVector2B rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector3D lhs, JVector2  rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector2D lhs, JVector3D rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector2F lhs, JVector3D rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector2I lhs, JVector3D rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector2B lhs, JVector3D rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector2  lhs, JVector3D rhs) => rhs.Multiply(lhs);
	
	// With vec3
	public static JVector3D operator + (JVector3D lhs, JVector3D rhs) => lhs.Add(rhs);
	public static JVector3D operator + (JVector3D lhs, JVector3F rhs) => lhs.Add(rhs);
	public static JVector3D operator + (JVector3D lhs, JVector3I rhs) => lhs.Add(rhs);
	public static JVector3D operator + (JVector3D lhs, JVector3B rhs) => lhs.Add(rhs);
	public static JVector3  operator + (JVector3D lhs, JVector3  rhs) => lhs.Add(rhs);
	public static JVector   operator + (JVector3D lhs, JVector   rhs) => lhs.Add(rhs);
	public static JVector3D operator + (JVector3F lhs, JVector3D rhs) => rhs.Add(lhs);
	public static JVector3D operator + (JVector3I lhs, JVector3D rhs) => rhs.Add(lhs);
	public static JVector3D operator + (JVector3B lhs, JVector3D rhs) => rhs.Add(lhs);
	public static JVector3  operator + (JVector3  lhs, JVector3D rhs) => rhs.Add(lhs);
	public static JVector   operator + (JVector   lhs, JVector3D rhs) => rhs.Add(lhs);
	public static JVector3D operator - (JVector3D lhs, JVector3D rhs) => lhs.Subtract(rhs);
	public static JVector3D operator - (JVector3D lhs, JVector3F rhs) => lhs.Subtract(rhs);
	public static JVector3D operator - (JVector3D lhs, JVector3I rhs) => lhs.Subtract(rhs);
	public static JVector3D operator - (JVector3D lhs, JVector3B rhs) => lhs.Subtract(rhs);
	public static JVector3  operator - (JVector3D lhs, JVector3  rhs) => lhs.Subtract(rhs);
	public static JVector   operator - (JVector3D lhs, JVector   rhs) => lhs.Subtract(rhs);
	public static JVector3D operator - (JVector3F lhs, JVector3D rhs) => rhs.SubtractFrom(lhs);
	public static JVector3D operator - (JVector3I lhs, JVector3D rhs) => rhs.SubtractFrom(lhs);
	public static JVector3D operator - (JVector3B lhs, JVector3D rhs) => rhs.SubtractFrom(lhs);
	public static JVector3  operator - (JVector3  lhs, JVector3D rhs) => rhs.SubtractFrom(lhs);
	public static JVector   operator - (JVector   lhs, JVector3D rhs) => rhs.SubtractFrom(lhs);
	public static double    operator * (JVector3D lhs, JVector3D rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector3D lhs, JVector3F rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector3D lhs, JVector3I rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector3D lhs, JVector3B rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector3D lhs, JVector3  rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector3D lhs, JVector   rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector3F lhs, JVector3D rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector3I lhs, JVector3D rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector3B lhs, JVector3D rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector3  lhs, JVector3D rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector   lhs, JVector3D rhs) => rhs.Multiply(lhs);
	
	public static JVector3D operator * (JVector3D lhs, double    rhs) => lhs.Multiply(rhs);
	public static JVector3D operator * (double    lhs, JVector3D rhs) => rhs.Multiply(lhs);
	public static JVector3D operator / (JVector3D lhs, double    rhs) => lhs.Divide(rhs);
	public static JVector3D operator / (double    lhs, JVector3D rhs) => rhs.DivideFrom(lhs);
	
	// Equality operator
	public static bool operator == (JVector3D lhs, JVector3D rhs) =>  ((IEquatable<JVector3D>) lhs).Equals(rhs);
	public static bool operator == (JVector3D lhs, JVector3  rhs) =>  lhs.Equals(rhs);
	public static bool operator == (JVector3  lhs, JVector3D rhs) =>  rhs.Equals(lhs);
	public static bool operator != (JVector3D lhs, JVector3D rhs) => !((IEquatable<JVector3D>) lhs).Equals(rhs);
	public static bool operator != (JVector3D lhs, JVector3  rhs) => !lhs.Equals(rhs);
	public static bool operator != (JVector3  lhs, JVector3D rhs) => !rhs.Equals(lhs);
}