namespace Jimbl.JMath;

using System.Numerics;

public interface JVector4: JVector {
	int JVector.Count => 4;
	
	public object X { get; }
	public object Y { get; }
	public object Z { get; }
	public object W { get; }
	
	object[] JVector.AsArray => AsArray;
	
	public     (object, object, object, object) AsTuple => (X, Y, Z, W);
	public new object[]                         AsArray => [X, Y, Z, W];
	
	JVector JVector.    Copy(Func<object, object> transformation) => Copy(transformation);
	public new JVector4 Copy(Func<object, object> transformation);
	
	JVector JVector.Negate() => Negate();
	public new JVector4 Negate();
	
	JVector JVector  .Multiply(double other) =>   Multiply(other);
	JVector JVector    .Divide(double other) =>     Divide(other);
	JVector JVector.DivideFrom(double other) => DivideFrom(other);
	
	JVector JVector.Add(JVector other) {
		if (other is JVector4 v4) {
			return Add(v4);
		}
		else {
			throw new NotImplementedException();
		}
	}
	
	JVector JVector.Subtract(JVector other) {
		if (other is JVector4 v4) {
			return Subtract(v4);
		}
		else {
			throw new NotImplementedException();
		}
	}
	
	JVector JVector.SubtractFrom(JVector other) {
		if (other is JVector4 v4) {
			return Subtract(v4);
		}
		else {
			throw new NotImplementedException();
		}
	}
	
	public new JVector4          Add(JVector4 other);
	public new JVector4     Subtract(JVector4 other) => Add(other.Negate());
	public new JVector4 SubtractFrom(JVector4 other) => other.Subtract(this);
	
	public new JVector4   Multiply(double other);
	public new JVector4     Divide(double other) => Multiply(1 / other);
	public new JVector4 DivideFrom(double other);
	
	internal static (JVector4, JVector4) Promote(JVector4 v1, JVector4 v2) {
		var promotedType = PromoteType(v1, v2);
		
		if (promotedType == typeof(Int32)) {
			return ((JVector4I) v1, (JVector4I) v2);
		}
		else if (promotedType == typeof(float)) {
			return ((JVector4F) v1, (JVector4F) v2);
		}
		else if (promotedType == typeof(UInt32) || promotedType == typeof(Int64)) {
			return ((JVector4L) v1, (JVector4L) v2);
		}
		else if (promotedType == typeof(double)) {
			return ((JVector4D) v1, (JVector4D) v2);
		}
		else {
			throw new ArgumentException("Could not auto-promote JVector4. Please cast to an explicit type.");
		}
	}
	
	internal static (JVector4, JVector4) Promote(JVector4 v1, JVector3 v2) {
		var promotedType = PromoteType(v1, v2);
		
		if (promotedType == typeof(Int32)) {
			return ((JVector4I) v1, (JVector4I) (JVector3I) v2);
		}
		else if (promotedType == typeof(float)) {
			return ((JVector4F) v1, (JVector4F) (JVector3F) v2);
		}
		else if (promotedType == typeof(UInt32) || promotedType == typeof(Int64)) {
			return ((JVector4L) v1, (JVector4L) (JVector3L) v2);
		}
		else if (promotedType == typeof(double)) {
			return ((JVector4D) v1, (JVector4D) (JVector3D) v2);
		}
		else {
			throw new ArgumentException("Could not auto-promote JVector4, JVector3. Please cast to an explicit type.");
		}
	}
	
	internal static (JVector4, JVector4) Promote(JVector4 v1, JVector2 v2) {
		var promotedType = PromoteType(v1, v2);
		
		if (promotedType == typeof(Int32)) {
			return ((JVector4I) v1, (JVector4I) (JVector2I) v2);
		}
		else if (promotedType == typeof(float)) {
			return ((JVector4F) v1, (JVector4F) (JVector2F) v2);
		}
		else if (promotedType == typeof(UInt32) || promotedType == typeof(Int64)) {
			return ((JVector4L) v1, (JVector4L) (JVector2L) v2);
		}
		else if (promotedType == typeof(double)) {
			return ((JVector4D) v1, (JVector4D) (JVector2D) v2);
		}
		else {
			throw new ArgumentException("Could not auto-promote JVector4, JVector2. Please cast to an explicit type.");
		}
	}
	
	public static virtual JVector4 operator + (JVector4 self) => self;
	public static virtual JVector4 operator - (JVector4 self) => self.Negate();
	
	public static virtual JVector operator + (JVector4 lhs, JVector  rhs) => lhs.Add(rhs);
	public static virtual JVector operator + (JVector  lhs, JVector4 rhs) => lhs.Add(rhs);
	public static virtual JVector operator - (JVector4 lhs, JVector  rhs) => lhs.Subtract(rhs);
	public static virtual JVector operator - (JVector  lhs, JVector4 rhs) => lhs.Subtract(rhs);
	public static virtual double  operator * (JVector4 lhs, JVector4 rhs) => lhs.Multiply(rhs);
	public static virtual double  operator * (JVector4 lhs, JVector  rhs) => lhs.Multiply(rhs);
	public static virtual double  operator * (JVector  lhs, JVector4 rhs) => lhs.Multiply(rhs);
	
	public static virtual JVector4 operator + (JVector4 lhs, JVector4 rhs) => lhs.Add(rhs);
	public static virtual JVector4 operator - (JVector4 lhs, JVector4 rhs) => lhs.Subtract(rhs);
	
	public static virtual JVector4 operator * (JVector4 lhs, double   rhs) => lhs.Multiply(rhs);
	public static virtual JVector4 operator * (double   lhs, JVector4 rhs) => rhs.Multiply(lhs);
	public static virtual JVector4 operator / (JVector4 lhs, double   rhs) => lhs.Divide(rhs);
	public static virtual JVector4 operator / (double   lhs, JVector4 rhs) => rhs.DivideFrom(lhs);
}

public interface JVector4<T>: JVector4, JVector<T> where T: INumber<T> {
	object JVector4.X => X;
	object JVector4.Y => Y;
	object JVector4.Z => Z;
	object JVector4.W => W;
	
	public new T X { get; set; }
	public new T Y { get; set; }
	public new T Z { get; set; }
	public new T W { get; set; }
	
	object[] JVector.AsArray => AsArray.Cast<object>().ToArray();
	T[] JVector<T>.  AsArray => AsArray;
	
	(object, object, object, object) JVector4.AsTuple => AsTuple;
	object[]                         JVector4.AsArray => AsArray.Cast<object>().ToArray();
	
	public new (T, T, T, T) AsTuple => Defaults.AsTuple(this);
	public new T[]          AsArray => Defaults.AsArray(this);
	
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
				case 3:  W = value; break;
				default: throw new ArgumentOutOfRangeException();
			}
		}
	}
	
	JVector     JVector.Copy(Func<object, object> transformation) => Copy(x => transformation(x));
	JVector  JVector<T>.Copy<R>(Func<T, R> transformation)        => Copy(transformation);
	JVector4   JVector4.Copy(Func<object, object> transformation) => Copy(x => transformation(x));
	
	public new JVector4 Copy<R>(Func<T, R> transformation);
	
	double JVector.Magnitude => Magnitude;
	public new double Magnitude => Defaults.Magnitude(this);
	
	public static virtual JVector4<T> operator + (JVector4<T> self) => self;
	public static virtual JVector4    operator - (JVector4<T> self) => self.Negate();
	
	public static virtual JVector4 operator + (JVector4<T> lhs, JVector4<T> rhs) => lhs.Add(rhs);
	public static virtual JVector  operator + (JVector4<T> lhs, JVector     rhs) => lhs.Add(rhs);
	public static virtual JVector  operator + (JVector     lhs, JVector4<T> rhs) => lhs.Add(rhs);
	public static virtual JVector4 operator - (JVector4<T> lhs, JVector4<T> rhs) => lhs.Subtract(rhs);
	public static virtual JVector  operator - (JVector4<T> lhs, JVector     rhs) => lhs.Subtract(rhs);
	public static virtual JVector  operator - (JVector     lhs, JVector4<T> rhs) => lhs.Subtract(rhs);
	public static virtual double   operator * (JVector4<T> lhs, JVector4<T> rhs) => lhs.Multiply(rhs);
	public static virtual double   operator * (JVector4<T> lhs, JVector     rhs) => lhs.Multiply(rhs);
	public static virtual double   operator * (JVector     lhs, JVector4<T> rhs) => lhs.Multiply(rhs);
	
	// Default implementations
	public new static class Defaults {
		public static double Magnitude(JVector4<T> self) {
			return Math.Sqrt((self.X * self.X + self.Y * self.Y + self.Z * self.Z).UnboxCast<double>());
		}
		
		public static (T, T, T, T) AsTuple(JVector4<T> self) => (self.X, self.Y, self.Z, self.W);
		public static T[]          AsArray(JVector4<T> self) => [self.X, self.Y, self.Z, self.W];
		
		public static T GetThis(JVector4<T> self, int itemIndex) {
			switch (itemIndex) {
				case 0:  return self.X;
				case 1:  return self.Y;
				case 2:  return self.Z;
				case 3:  return self.W;
				default: throw new ArgumentOutOfRangeException();
			}
		}
		
		public static void SetThis<TSelf>(ref TSelf self, int itemIndex, T value) where TSelf: struct, JVector4<T> {
			switch (itemIndex) {
				case 0:  self.X = value; break;
				case 1:  self.Y = value; break;
				case 2:  self.Z = value; break;
				case 3:  self.W = value; break;
				default: throw new ArgumentOutOfRangeException();
			}
		}
	}
}
