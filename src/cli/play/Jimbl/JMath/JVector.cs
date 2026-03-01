namespace Jimbl.JMath;

using System.Numerics;

public interface JVector {
	public Type InnerType => typeof(object);
	
	public int    Count     { get; }
	public double Magnitude { get; }
	
	public JVector Negate();
	
	public JVector          Add(JVector other);
	public JVector     Subtract(JVector other) => Add(other.Negate());
	public JVector SubtractFrom(JVector other) => other.Subtract(this);
	public double      Multiply(JVector other);
	public JVector     Multiply(double  other);
	public JVector       Divide(double  other) => Multiply(1 / other);
	public JVector   DivideFrom(double  other);
	
	public static virtual JVector operator + (JVector self) => self;
	public static virtual JVector operator - (JVector self) => self.Negate();
	
	public static virtual JVector operator + (JVector lhs, JVector rhs) => lhs.Add(rhs);
	public static virtual JVector operator - (JVector lhs, JVector rhs) => lhs.Subtract(rhs);
	public static virtual double  operator * (JVector lhs, JVector rhs) => lhs.Multiply(rhs);
	
	public static virtual JVector operator * (JVector lhs, double  rhs) => lhs.Multiply(rhs);
	public static virtual JVector operator * (double  lhs, JVector rhs) => rhs.Multiply(lhs);
	public static virtual JVector operator / (JVector lhs, double  rhs) => lhs.Divide(rhs);
	public static virtual JVector operator / (double  lhs, JVector rhs) => rhs.DivideFrom(lhs);
	
	internal static Type PromoteType(JVector v1, JVector v2) {
		var t1 = v1.InnerType;
		var t2 = v2.InnerType;
		
		if (t1.FitsInt32() && t2.FitsInt32()) {
			return typeof(Int32);
		}
		else if (t1.FitsUInt32() && t2.FitsUInt32()) {
			return typeof(UInt32);
		}
		else if (t1.FitsInt64() && t2.FitsInt64()) {
			return typeof(Int64);
		}
		else if (t1.FitsUInt64() && t2.FitsUInt64()) {
			return typeof(UInt64);
		}
		else if (t1.FitsFloat32() && t2.FitsFloat32()) {
			return typeof(float);
		}
		else if (t1.FitsFloat64() && t2.FitsFloat64()) {
			return typeof(double);
		}
		else if (t1.FitsDecimal() && t2.FitsDecimal()) {
			return typeof(decimal);
		}
		else if (t1.FitsInt128() && t2.FitsInt128()) {
			return typeof(Int128);
		}
		else if (t1.FitsUInt128() && t2.FitsUInt128()) {
			return typeof(UInt128);
		}
		else if (t1.FitsBigInteger() && t2.FitsBigInteger()) {
			return typeof(BigInteger);
		}
		else if (t1.FitsComplex() && t2.FitsComplex()) {
			return typeof(Complex);
		}
		else {
			throw new ArgumentException("Could not auto-promote JVector. Please cast to an explicit type.");
		}
	}
}

public interface JVector<T>: JVector where T: INumber<T> {
	Type JVector.InnerType => typeof(T);
	
	public T this[int itemIndex] { get; set; }
	
	public static virtual JVector<T> operator + (JVector<T> self) => self;
	public static virtual JVector    operator - (JVector<T> self) => self.Negate();
	
	public static virtual JVector operator + (JVector<T> lhs, JVector<T> rhs) => lhs.Add(rhs);
	public static virtual JVector operator + (JVector<T> lhs, JVector    rhs) => lhs.Add(rhs);
	public static virtual JVector operator + (JVector    lhs, JVector<T> rhs) => lhs.Add(rhs);
	public static virtual JVector operator - (JVector<T> lhs, JVector<T> rhs) => lhs.Subtract(rhs);
	public static virtual JVector operator - (JVector<T> lhs, JVector    rhs) => lhs.Subtract(rhs);
	public static virtual JVector operator - (JVector    lhs, JVector<T> rhs) => lhs.Subtract(rhs);
	public static virtual double  operator * (JVector<T> lhs, JVector<T> rhs) => lhs.Multiply(rhs);
	public static virtual double  operator * (JVector<T> lhs, JVector    rhs) => lhs.Multiply(rhs);
	public static virtual double  operator * (JVector    lhs, JVector<T> rhs) => lhs.Multiply(rhs);
	
	public static virtual JVector operator * (JVector<T> lhs, double     rhs) => lhs.Multiply(rhs);
	public static virtual JVector operator * (double     lhs, JVector<T> rhs) => rhs.Multiply(lhs);
	public static virtual JVector operator / (JVector<T> lhs, double     rhs) => lhs.Divide(rhs);
	public static virtual JVector operator / (double     lhs, JVector<T> rhs) => rhs.DivideFrom(lhs);
}