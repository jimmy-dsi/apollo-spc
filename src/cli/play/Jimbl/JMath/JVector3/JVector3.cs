namespace Jimbl.JMath;

using System.Numerics;

public interface JVector3: JVector {
	int JVector.Count => 3;
	
	public object X { get; }
	public object Y { get; }
	public object Z { get; }
	
	object[] JVector.AsArray => AsArray;
	
	public     (object, object, object) AsTuple => (X, Y, Z);
	public new object[]                 AsArray => [X, Y, Z];
	
	JVector JVector.    Copy(Func<object, object> transformation) => Copy(transformation);
	public new JVector3 Copy(Func<object, object> transformation);
	
	JVector JVector.Negate() => Negate();
	public new JVector3 Negate();
	
	JVector JVector  .Multiply(double other) =>   Multiply(other);
	JVector JVector    .Divide(double other) =>     Divide(other);
	JVector JVector.DivideFrom(double other) => DivideFrom(other);
	
	JVector JVector.Add(JVector other) {
		if (other is JVector3 v3) {
			return Add(v3);
		}
		else {
			throw new NotImplementedException();
		}
	}
	
	JVector JVector.Subtract(JVector other) {
		if (other is JVector3 v3) {
			return Subtract(v3);
		}
		else {
			throw new NotImplementedException();
		}
	}
	
	JVector JVector.SubtractFrom(JVector other) {
		if (other is JVector3 v3) {
			return Subtract(v3);
		}
		else {
			throw new NotImplementedException();
		}
	}
	
	public new JVector3          Add(JVector3 other);
	public new JVector3     Subtract(JVector3 other) => Add(other.Negate());
	public new JVector3 SubtractFrom(JVector3 other) => other.Subtract(this);
	
	public new JVector3   Multiply(double other);
	public new JVector3     Divide(double other) => Multiply(1 / other);
	public new JVector3 DivideFrom(double other);
	
	internal static (JVector3, JVector3) Promote(JVector3 v1, JVector3 v2) {
		var promotedType = PromoteType(v1, v2);
		
		if (promotedType == typeof(Int32)) {
			return ((JVector3I) v1, (JVector3I) v2);
		}
		else if (promotedType == typeof(float)) {
			return ((JVector3F) v1, (JVector3F) v2);
		}
		else if (promotedType == typeof(UInt32) || promotedType == typeof(Int64)) {
			return ((JVector3L) v1, (JVector3L) v2);
		}
		else if (promotedType == typeof(double)) {
			return ((JVector3D) v1, (JVector3D) v2);
		}
		else {
			throw new ArgumentException("Could not auto-promote JVector3. Please cast to an explicit type.");
		}
	}
	
	internal static (JVector3, JVector3) Promote(JVector3 v1, JVector2 v2) {
		var promotedType = PromoteType(v1, v2);
		
		if (promotedType == typeof(Int32)) {
			return ((JVector3I) v1, (JVector3I) (JVector2I) v2);
		}
		else if (promotedType == typeof(float)) {
			return ((JVector3F) v1, (JVector3F) (JVector2F) v2);
		}
		else if (promotedType == typeof(UInt32) || promotedType == typeof(Int64)) {
			return ((JVector3L) v1, (JVector3L) (JVector2L) v2);
		}
		else if (promotedType == typeof(double)) {
			return ((JVector3D) v1, (JVector3D) (JVector2D) v2);
		}
		else {
			throw new ArgumentException("Could not auto-promote JVector3, JVector2. Please cast to an explicit type.");
		}
	}
	
	public static virtual JVector3 operator + (JVector3 self) => self;
	public static virtual JVector3 operator - (JVector3 self) => self.Negate();
	
	public static virtual JVector operator + (JVector3 lhs, JVector  rhs) => lhs.Add(rhs);
	public static virtual JVector operator + (JVector  lhs, JVector3 rhs) => lhs.Add(rhs);
	public static virtual JVector operator - (JVector3 lhs, JVector  rhs) => lhs.Subtract(rhs);
	public static virtual JVector operator - (JVector  lhs, JVector3 rhs) => lhs.Subtract(rhs);
	public static virtual double  operator * (JVector3 lhs, JVector3 rhs) => lhs.Multiply(rhs);
	public static virtual double  operator * (JVector3 lhs, JVector  rhs) => lhs.Multiply(rhs);
	public static virtual double  operator * (JVector  lhs, JVector3 rhs) => lhs.Multiply(rhs);
	
	public static virtual JVector3 operator + (JVector3 lhs, JVector3 rhs) => lhs.Add(rhs);
	public static virtual JVector3 operator - (JVector3 lhs, JVector3 rhs) => lhs.Subtract(rhs);
	
	public static virtual JVector3 operator * (JVector3 lhs, double   rhs) => lhs.Multiply(rhs);
	public static virtual JVector3 operator * (double   lhs, JVector3 rhs) => rhs.Multiply(lhs);
	public static virtual JVector3 operator / (JVector3 lhs, double   rhs) => lhs.Divide(rhs);
	public static virtual JVector3 operator / (double   lhs, JVector3 rhs) => rhs.DivideFrom(lhs);
}

public interface JVector3<T>: JVector3, JVector<T> where T: INumber<T> {
	object JVector3.X => X;
	object JVector3.Y => Y;
	object JVector3.Z => Z;
	
	public new T X { get; set; }
	public new T Y { get; set; }
	public new T Z { get; set; }
	
	object[] JVector.AsArray => AsArray.Cast<object>().ToArray();
	T[] JVector<T>.  AsArray => AsArray;
	
	(object, object, object) JVector3.AsTuple => AsTuple;
	object[]                 JVector3.AsArray => AsArray.Cast<object>().ToArray();
	
	public new (T, T, T) AsTuple => Defaults.AsTuple(this);
	public new T[]       AsArray => Defaults.AsArray(this);
	
	object JVector.this[int itemIndex] {
		get => this[itemIndex];
	}
	
	T JVector<T>.this[int itemIndex] {
		get => this[itemIndex];
		set => this[itemIndex] = value;
	}
	
	public new T this[int itemIndex] {
		get => Defaults.GetThis(this, itemIndex);
		set {
			switch (itemIndex) {
				case 0:  X = value; break;
				case 1:  Y = value; break;
				case 2:  Z = value; break;
				default: throw new ArgumentOutOfRangeException();
			}
		}
	}
	
	JVector     JVector.Copy(Func<object, object> transformation) => Copy(x => transformation(x));
	JVector  JVector<T>.Copy<R>(Func<T, R> transformation)        => Copy(transformation);
	JVector3   JVector3.Copy(Func<object, object> transformation) => Copy(x => transformation(x));
	
	public new JVector3 Copy<R>(Func<T, R> transformation);
	
	double JVector.Magnitude => Magnitude;
	public new double Magnitude => Defaults.Magnitude(this);
	
	public static virtual JVector3<T> operator + (JVector3<T> self) => self;
	public static virtual JVector3    operator - (JVector3<T> self) => self.Negate();
	
	public static virtual JVector3 operator + (JVector3<T> lhs, JVector3<T> rhs) => lhs.Add(rhs);
	public static virtual JVector  operator + (JVector3<T> lhs, JVector     rhs) => lhs.Add(rhs);
	public static virtual JVector  operator + (JVector     lhs, JVector3<T> rhs) => lhs.Add(rhs);
	public static virtual JVector3 operator - (JVector3<T> lhs, JVector3<T> rhs) => lhs.Subtract(rhs);
	public static virtual JVector  operator - (JVector3<T> lhs, JVector     rhs) => lhs.Subtract(rhs);
	public static virtual JVector  operator - (JVector     lhs, JVector3<T> rhs) => lhs.Subtract(rhs);
	public static virtual double   operator * (JVector3<T> lhs, JVector3<T> rhs) => lhs.Multiply(rhs);
	public static virtual double   operator * (JVector3<T> lhs, JVector     rhs) => lhs.Multiply(rhs);
	public static virtual double   operator * (JVector     lhs, JVector3<T> rhs) => lhs.Multiply(rhs);
	
	// Default implementations
	public new static class Defaults {
		public static double Magnitude(JVector3<T> self) {
			return Math.Sqrt((self.X * self.X + self.Y * self.Y + self.Z * self.Z).UnboxCast<double>());
		}
		
		public static (T, T, T) AsTuple(JVector3<T> self) => (self.X, self.Y, self.Z);
		public static T[]       AsArray(JVector3<T> self) => [self.X, self.Y, self.Z];
		
		public static T GetThis(JVector3<T> self, int itemIndex) {
			switch (itemIndex) {
				case 0:  return self.X;
				case 1:  return self.Y;
				case 2:  return self.Z;
				default: throw new ArgumentOutOfRangeException();
			}
		}
		
		public static void SetThis<TSelf>(ref TSelf self, int itemIndex, T value) where TSelf: struct, JVector3<T> {
			switch (itemIndex) {
				case 0:  self.X = value; break;
				case 1:  self.Y = value; break;
				case 2:  self.Z = value; break;
				default: throw new ArgumentOutOfRangeException();
			}
		}
	}
}
