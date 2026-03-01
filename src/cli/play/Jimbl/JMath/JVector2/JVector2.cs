namespace Jimbl.JMath;

using System.Numerics;

public interface JVector2: JVector {
	int JVector.Count => 2;
	
	JVector JVector.Negate() => Negate();
	public new JVector2 Negate();
	
	JVector JVector  .Multiply(double other) =>   Multiply(other);
	JVector JVector    .Divide(double other) =>     Divide(other);
	JVector JVector.DivideFrom(double other) => DivideFrom(other);
	
	public new JVector2          Add(JVector2 other);
	public new JVector2     Subtract(JVector2 other) => Add(other.Negate());
	public new JVector2 SubtractFrom(JVector2 other) => other.Subtract(this);
	
	public new JVector2   Multiply(double other);
	public new JVector2     Divide(double other) => Multiply(1 / other);
	public new JVector2 DivideFrom(double other);
	
	internal static (JVector2, JVector2) Promote(JVector2 v1, JVector2 v2) {
		var promotedType = PromoteType(v1, v2);
		
		if (promotedType == typeof(Int32)) {
			return ((JVector2I) v1, (JVector2I) v2);
		}
		else if (promotedType == typeof(float)) {
			return ((JVector2F) v1, (JVector2F) v2);
		}
		else if (promotedType == typeof(UInt32) || promotedType == typeof(Int64)) {
			return ((JVector2L) v1, (JVector2L) v2);
		}
		else if (promotedType == typeof(double)) {
			return ((JVector2D) v1, (JVector2D) v2);
		}
		else {
			throw new ArgumentException("Could not auto-promote JVector2. Please cast to an explicit type.");
		}
	}
	
	public static virtual JVector2 operator + (JVector2 self) => self;
	public static virtual JVector2 operator - (JVector2 self) => self.Negate();
	
	public static virtual JVector operator + (JVector2 lhs, JVector  rhs) => lhs.Add(rhs);
	public static virtual JVector operator + (JVector  lhs, JVector2 rhs) => lhs.Add(rhs);
	public static virtual JVector operator - (JVector2 lhs, JVector  rhs) => lhs.Subtract(rhs);
	public static virtual JVector operator - (JVector  lhs, JVector2 rhs) => lhs.Subtract(rhs);
	public static virtual double  operator * (JVector2 lhs, JVector2 rhs) => lhs.Multiply(rhs);
	public static virtual double  operator * (JVector2 lhs, JVector  rhs) => lhs.Multiply(rhs);
	public static virtual double  operator * (JVector  lhs, JVector2 rhs) => lhs.Multiply(rhs);
	
	public static virtual JVector2 operator + (JVector2 lhs, JVector2 rhs) => lhs.Add(rhs);
	public static virtual JVector2 operator - (JVector2 lhs, JVector2 rhs) => lhs.Subtract(rhs);
	
	public static virtual JVector2 operator * (JVector2 lhs, double   rhs) => lhs.Multiply(rhs);
	public static virtual JVector2 operator * (double   lhs, JVector2 rhs) => rhs.Multiply(lhs);
	public static virtual JVector2 operator / (JVector2 lhs, double   rhs) => lhs.Divide(rhs);
	public static virtual JVector2 operator / (double   lhs, JVector2 rhs) => rhs.DivideFrom(lhs);
}

public interface JVector2<T>: JVector2, JVector<T> where T: INumber<T> {
	int JVector.Count => 2;
	
	public T X { get; set; }
	public T Y { get; set; }
	
	T JVector<T>.this[int itemIndex] {
		get => this[itemIndex];
		set => this[itemIndex] = value;
	}
	
	public new T this[int itemIndex] {
		get {
			if (itemIndex == 0) return X;
			if (itemIndex == 1) return Y;
			throw new ArgumentOutOfRangeException();
		}
		set {
			if (itemIndex == 0) X = value;
			if (itemIndex == 1) Y = value;
			throw new ArgumentOutOfRangeException();
		}
	}
	
	double JVector.Magnitude => Magnitude;
	public new double Magnitude => Math.Sqrt((double) (object) (X * X + Y * Y));
	
	public static virtual JVector2<T> operator + (JVector2<T> self) => self;
	public static virtual JVector2    operator - (JVector2<T> self) => self.Negate();
	
	public static virtual JVector2 operator + (JVector2<T> lhs, JVector2<T> rhs) => lhs.Add(rhs);
	public static virtual JVector  operator + (JVector2<T> lhs, JVector     rhs) => lhs.Add(rhs);
	public static virtual JVector  operator + (JVector     lhs, JVector2<T> rhs) => lhs.Add(rhs);
	public static virtual JVector2 operator - (JVector2<T> lhs, JVector2<T> rhs) => lhs.Subtract(rhs);
	public static virtual JVector  operator - (JVector2<T> lhs, JVector     rhs) => lhs.Subtract(rhs);
	public static virtual JVector  operator - (JVector     lhs, JVector2<T> rhs) => lhs.Subtract(rhs);
	public static virtual double   operator * (JVector2<T> lhs, JVector2<T> rhs) => lhs.Multiply(rhs);
	public static virtual double   operator * (JVector2<T> lhs, JVector     rhs) => lhs.Multiply(rhs);
	public static virtual double   operator * (JVector     lhs, JVector2<T> rhs) => lhs.Multiply(rhs);
}
