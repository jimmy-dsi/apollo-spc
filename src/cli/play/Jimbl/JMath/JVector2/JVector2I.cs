namespace Jimbl.JMath;

using System.Numerics;

public struct JVector2I: JVector2<int>, IEquatable<JVector2I> {
	int x;
	int y;
	Action<int, int>? setterHook = null;
	
	public Type InnerType => JVector<int>.Defaults.InnerType();
	
	public (int, int) AsTuple => JVector2<int>.Defaults.AsTuple(this);
	public int[]      AsArray => JVector2<int>.Defaults.AsArray(this);
	
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
	
	internal Action<int, int>? SetterHook {
		get => setterHook;
		set => setterHook = value;
	}
	
	public static explicit operator JVector2B (JVector2I vec) => new((byte) vec.X, (byte) vec.Y);
	public static implicit operator JVector2L (JVector2I vec) => new(       vec.X,        vec.Y);
	public static explicit operator JVector2F (JVector2I vec) => new(       vec.X,        vec.Y);
	public static implicit operator JVector2D (JVector2I vec) => new(       vec.X,        vec.Y);
	
	public static explicit operator JVector3B (JVector2I vec) => new((byte) vec.X, (byte) vec.Y, 0);
	public static implicit operator JVector3I (JVector2I vec) => new(       vec.X,        vec.Y, 0);
	public static implicit operator JVector3L (JVector2I vec) => new(       vec.X,        vec.Y, 0);
	public static explicit operator JVector3F (JVector2I vec) => new(       vec.X,        vec.Y, 0);
	public static implicit operator JVector3D (JVector2I vec) => new(       vec.X,        vec.Y, 0);
	
	public static explicit operator JVector4B (JVector2I vec) => new((byte) vec.X, (byte) vec.Y, 0, 0);
	public static implicit operator JVector4I (JVector2I vec) => new(       vec.X,        vec.Y, 0, 0);
	public static implicit operator JVector4L (JVector2I vec) => new(       vec.X,        vec.Y, 0, 0);
	public static explicit operator JVector4F (JVector2I vec) => new(       vec.X,        vec.Y, 0, 0);
	public static implicit operator JVector4D (JVector2I vec) => new(       vec.X,        vec.Y, 0, 0);
	
	public static implicit operator JVector2I ((int, int) tup) => new(tup.Item1, tup.Item2);
	public static explicit operator JVector2I (int[] arr) => new(arr[0], arr[1]);
	
	public JVector2I(int x, int y) {
		X = x;
		Y = y;
	}
	
	JVector2 JVector2<Int32>.Copy<R>(Func<Int32, R> transformation) {
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
	
	public JVector2B Copy(Func<Int32, byte> transformation) {
		JVector2B result = new();
		for (var i = 0; i < 2; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public JVector2I Copy(Func<Int32, Int32> transformation) {
		JVector2I result = new();
		for (var i = 0; i < 2; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public JVector2L Copy(Func<Int32, Int64> transformation) {
		JVector2L result = new();
		for (var i = 0; i < 2; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public JVector2F Copy(Func<Int32, float> transformation) {
		JVector2F result = new();
		for (var i = 0; i < 2; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public JVector2D Copy(Func<Int32, double> transformation) {
		JVector2D result = new();
		for (var i = 0; i < 2; i++) {
			result[i] = transformation(this[i]);
		}
		return result;
	}
	
	public void Transform(Func<Int32, Int32> transformation) => JVector<Int32>.Defaults.Transform(ref this, transformation);

	public Int32 this[int itemIndex] {
		get => JVector2<Int32>.Defaults.GetThis(this, itemIndex);
		set => JVector2<Int32>.Defaults.SetThis(ref this, itemIndex, value);
	}
	
	public double Magnitude => JVector2<Int32>.Defaults.Magnitude(this);
	
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
		if (other is JVector2I v2i) {
			return Add(v2i);
		}
		else {
			var (a, b) = JVector2.Promote(this, other);
			return a.Add(b);
		}
	}
	
	public JVector2I Add(JVector2I other) {
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
		if (other is JVector2I v2i) {
			return Subtract(v2i);
		}
		else {
			var (a, b) = JVector2.Promote(this, other);
			return a.Subtract(b);
		}
	}
	
	public JVector2I Subtract(JVector2I other) {
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
		if (other is JVector2I v2i) {
			return SubtractFrom(v2i);
		}
		else {
			var (a, b) = JVector2.Promote(this, other);
			return a.SubtractFrom(b);
		}
	}
	
	public JVector2I SubtractFrom(JVector2I other) {
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
		if (other is JVector2I v2i) {
			return Multiply(v2i);
		}
		else {
			var (a, b) = JVector2.Promote(this, other);
			return a.Multiply(b);
		}
	}
	
	public double Multiply(JVector2I other) {
		return X * other.X + Y * other.Y;
	}
	
	JVector2 JVector2.Multiply(double other) => Multiply(other);
	
	public JVector2D Multiply(double other) {
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
	
	public JVector2I DivideFrom(int other) {
		return new(other / X, other / Y);
	}
	
	public JVector2L DivideFrom(long other) {
		return new(other / X, other / Y);
	}

	public override int GetHashCode() {
		return (X, Y).GetHashCode();
	}

	bool IEquatable<JVector2I>.Equals(JVector2I other) => Equals(other);
	
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
		
		if (v2.InnerType.FitsInt32()) {
			return Equals((JVector2<Int32>) v2);
		}
		else if (v2.InnerType.FitsInt64()) {
			return Equals((JVector2<Int64>) v2);
		}
		else if (v2.InnerType.FitsFloat32()) {
			return Equals((JVector2<float>) v2);
		}
		else if (v2.InnerType.FitsFloat64()) {
			return Equals((JVector2<double>) v2);
		}
		else {
			throw new ArgumentException("Cannot compare JVector2I to JVector2");
		}
	}
	
	public bool Equals<T>(JVector2<T> other) where T: INumber<T> {
		var type = JVector.PromoteType(this, other);
		
		if (type == typeof(Int32)) {
			return X == (Int32) (object) other.X
			    && Y == (Int32) (object) other.Y;
		}
		else if (type == typeof(Int64)) {
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
		else if (type == typeof(double)) {
			return X == (double) (object) other.X
			    && Y == (double) (object) other.Y;
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
	public static JVector2I operator + (JVector2I self) => self;
	public static JVector2I operator - (JVector2I self) => self.Negate();
	
	public static JVector2I operator + (JVector2I lhs, JVector2I rhs) => lhs.Add(rhs);
	public static JVector2I operator + (JVector2I lhs, JVector2B rhs) => lhs.Add(rhs);
	public static JVector2  operator + (JVector2I lhs, JVector2  rhs) => lhs.Add(rhs);
	public static JVector   operator + (JVector2I lhs, JVector   rhs) => lhs.Add(rhs);
	public static JVector2I operator + (JVector2B lhs, JVector2I rhs) => rhs.Add(lhs);
	public static JVector2  operator + (JVector2  lhs, JVector2I rhs) => rhs.Add(lhs);
	public static JVector   operator + (JVector   lhs, JVector2I rhs) => rhs.Add(lhs);
	public static JVector2I operator - (JVector2I lhs, JVector2I rhs) => lhs.Subtract(rhs);
	public static JVector2I operator - (JVector2I lhs, JVector2B rhs) => lhs.Subtract(rhs);
	public static JVector2  operator - (JVector2I lhs, JVector2  rhs) => lhs.Subtract(rhs);
	public static JVector   operator - (JVector2I lhs, JVector   rhs) => lhs.Subtract(rhs);
	public static JVector2I operator - (JVector2B lhs, JVector2I rhs) => rhs.SubtractFrom(lhs);
	public static JVector2  operator - (JVector2  lhs, JVector2I rhs) => rhs.SubtractFrom(lhs);
	public static JVector   operator - (JVector   lhs, JVector2I rhs) => rhs.SubtractFrom(lhs);
	public static double    operator * (JVector2I lhs, JVector2I rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector2I lhs, JVector2B rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector2I lhs, JVector2  rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector2I lhs, JVector   rhs) => lhs.Multiply(rhs);
	public static double    operator * (JVector2B lhs, JVector2I rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector2  lhs, JVector2I rhs) => rhs.Multiply(lhs);
	public static double    operator * (JVector   lhs, JVector2I rhs) => rhs.Multiply(lhs);
	
	public static JVector2D operator * (JVector2I lhs, double    rhs) => lhs.Multiply(rhs);
	public static JVector2D operator * (double    lhs, JVector2I rhs) => rhs.Multiply(lhs);
	public static JVector2D operator / (JVector2I lhs, double    rhs) => lhs.Divide(rhs);
	public static JVector2D operator / (double    lhs, JVector2I rhs) => rhs.DivideFrom(lhs);
	
	public static JVector2I operator * (JVector2I lhs, int       rhs) => lhs.Multiply(rhs);
	public static JVector2I operator * (int       lhs, JVector2I rhs) => rhs.Multiply(lhs);
	public static JVector2I operator / (JVector2I lhs, int       rhs) => lhs.Divide(rhs);
	public static JVector2I operator / (int       lhs, JVector2I rhs) => rhs.DivideFrom(lhs);
	
	public static JVector2L operator * (JVector2I lhs, long      rhs) => lhs.Multiply(rhs);
	public static JVector2L operator * (long      lhs, JVector2I rhs) => rhs.Multiply(lhs);
	public static JVector2L operator / (JVector2I lhs, long      rhs) => lhs.Divide(rhs);
	public static JVector2L operator / (long      lhs, JVector2I rhs) => rhs.DivideFrom(lhs);
	
	// Equality operator
	public static bool operator == (JVector2I lhs, JVector2I rhs) =>  ((IEquatable<JVector2I>) lhs).Equals(rhs);
	public static bool operator == (JVector2I lhs, JVector2  rhs) =>  lhs.Equals(rhs);
	public static bool operator == (JVector2  lhs, JVector2I rhs) =>  rhs.Equals(lhs);
	public static bool operator != (JVector2I lhs, JVector2I rhs) => !((IEquatable<JVector2I>) lhs).Equals(rhs);
	public static bool operator != (JVector2I lhs, JVector2  rhs) => !lhs.Equals(rhs);
	public static bool operator != (JVector2  lhs, JVector2I rhs) => !rhs.Equals(lhs);
}