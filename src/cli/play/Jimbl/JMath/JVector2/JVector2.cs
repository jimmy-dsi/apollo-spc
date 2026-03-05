namespace Jimbl.JMath;

using System.Numerics;

public interface JVector2: JVector {
	int JVector.Count => 2;
	
	public object X { get; }
	public object Y { get; }
	
	object[] JVector.AsArray => AsArray;
	
	public (object, object) AsTuple => (X, Y);
	public new object[]     AsArray => [X, Y];
	
	JVector JVector.Copy(Func<object, object> transformation) => Copy(transformation);
	public new JVector2 Copy(Func<object, object> transformation);
	
	JVector JVector.Negate() => Negate();
	public new JVector2 Negate();
	
	JVector JVector  .Multiply(double other) =>   Multiply(other);
	JVector JVector    .Divide(double other) =>     Divide(other);
	JVector JVector.DivideFrom(double other) => DivideFrom(other);
	
	JVector JVector.Add(JVector other) {
		if (other is JVector2 v2) {
			return Add(v2);
		}
		else {
			throw new NotImplementedException();
		}
	}
	
	JVector JVector.Subtract(JVector other) {
		if (other is JVector2 v2) {
			return Subtract(v2);
		}
		else {
			throw new NotImplementedException();
		}
	}
	
	JVector JVector.SubtractFrom(JVector other) {
		if (other is JVector2 v2) {
			return Subtract(v2);
		}
		else {
			throw new NotImplementedException();
		}
	}
	
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
	object JVector2.X => X;
	object JVector2.Y => Y;
	
	public new T X { get; set; }
	public new T Y { get; set; }
	
	object[] JVector.AsArray => AsArray.Cast<object>().ToArray();
	T[] JVector<T>.  AsArray => AsArray;
	
	(object, object) JVector2.AsTuple => AsTuple;
	object[]         JVector2.AsArray => AsArray.Cast<object>().ToArray();
	
	public new (T, T) AsTuple => Defaults.AsTuple(this);
	public new T[]    AsArray => Defaults.AsArray(this);
	
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
				default: throw new ArgumentOutOfRangeException();
			}
		}
	}
	
	JVector     JVector.Copy(Func<object, object> transformation) => Copy(x => transformation(x));
	JVector  JVector<T>.Copy<R>(Func<T, R> transformation)        => Copy(transformation);
	JVector2   JVector2.Copy(Func<object, object> transformation) => Copy(x => transformation(x));
	
	public new JVector2 Copy<R>(Func<T, R> transformation);
	
	double JVector.Magnitude => Magnitude;
	public new double Magnitude => Defaults.Magnitude(this);
	
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
	
	// Default implementations
	public new static class Defaults {
		public static double Magnitude(JVector2<T> self) {
			return Math.Sqrt((double) (object) (self.X * self.X + self.Y * self.Y));
		}
		
		public static (T, T) AsTuple(JVector2<T> self) => (self.X, self.Y);
		public static T[]    AsArray(JVector2<T> self) => [self.X, self.Y];
		
		public static T GetThis(JVector2<T> self, int itemIndex) {
			if (itemIndex == 0) return self.X;
			if (itemIndex == 1) return self.Y;
			throw new ArgumentOutOfRangeException();
		}
		
		public static void SetThis<TSelf>(ref TSelf self, int itemIndex, T value) where TSelf: struct, JVector2<T> {
			switch (itemIndex) {
				case 0:  self.X = value; break;
				case 1:  self.Y = value; break;
				default: throw new ArgumentOutOfRangeException();
			}
		}
	}
}
