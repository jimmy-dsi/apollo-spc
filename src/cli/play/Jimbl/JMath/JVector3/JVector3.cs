namespace Jimbl.JMath;

using System.Numerics;

public interface JVector3: JVector {
	int JVector.Count => 3;
	
	JVector JVector.Negate() => Negate();
	public new JVector3 Negate();
	
	JVector JVector  .Multiply(double other) =>   Multiply(other);
	JVector JVector    .Divide(double other) =>     Divide(other);
	JVector JVector.DivideFrom(double other) => DivideFrom(other);
	
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
			throw new ArgumentException("Could not auto-promote JVector2. Please cast to an explicit type.");
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
	int JVector.Count => 3;
	
	public T X { get; set; }
	public T Y { get; set; }
	public T Z { get; set; }
	
	T JVector<T>.this[int itemIndex] {
		get => this[itemIndex];
		set => this[itemIndex] = value;
	}
	
	public new T this[int itemIndex] {
		get {
			if (itemIndex == 0) return X;
			if (itemIndex == 1) return Y;
			if (itemIndex == 2) return Z;
			throw new ArgumentOutOfRangeException();
		}
		set {
			if (itemIndex == 0) X = value;
			if (itemIndex == 1) Y = value;
			if (itemIndex == 2) Z = value;
			throw new ArgumentOutOfRangeException();
		}
	}
	
	double JVector.Magnitude => Magnitude;
	public new double Magnitude => Math.Sqrt((double) (object) (X * X + Y * Y + Z * Z));
	
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
}
