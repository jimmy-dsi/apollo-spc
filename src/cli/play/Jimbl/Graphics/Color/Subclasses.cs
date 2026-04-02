namespace Jimbl.Graphics;

using System.Numerics;
using JMath;

public partial class Color {
	public abstract class Vec3<T> where T: INumber<T> {
		internal JVector3<T> Vec { get; set; }
		
		public T this[int itemIndex] {
			get => Vec[itemIndex];
			set => Vec[itemIndex] = value;
		}
	}
	
	public abstract class Vec4<T> where T: INumber<T> {
		internal JVector4<T> Vec { get; set; }
		
		public T this[int itemIndex] {
			get => Vec[itemIndex];
			set => Vec[itemIndex] = value;
		}
	}
	
	public class Vec3B: Vec3<byte> {
		public Vec3B(Action<int, byte>? setterHook = null): this(0, 0, 0, setterHook) { }
		public Vec3B(Action setterHook): this(0, 0, 0, (_, _) => setterHook()) { }
		public Vec3B(JVector3B vec, Action<int, byte>? setterHook = null): this(vec.X, vec.Y, vec.Z, setterHook) { }
		public Vec3B(JVector3B vec, Action setterHook): this(vec.X, vec.Y, vec.Z, (_, _) => setterHook()) { }
		public Vec3B(byte x, byte y, byte z, Action setterHook): this(x, y, z, (_, _) => setterHook()) { }
		
		public Vec3B(byte x, byte y, byte z, Action<int, byte>? setterHook = null) {
			var vec = new JVector3B(x, y, z);
			vec.SetterHook = setterHook;
			Vec = vec;
		}
		
		internal JVector3B Inner {
			set {
				JVector3B vec = value;
				vec.SetterHook = ((JVector3B) Vec).SetterHook;
				Vec = vec;
				// Trigger hook for all elements
				for (var i = 0; i < 3; i++) {
					vec.SetterHook?.Invoke(i, vec[i]);
				}
			}
		}
		
		public static implicit operator Vec3B (JVector3B v) => new() { Vec =             v };
		public static explicit operator Vec3B (JVector3I v) => new() { Vec = (JVector3B) v };
		public static explicit operator Vec3B (JVector3L v) => new() { Vec = (JVector3B) v };
		public static explicit operator Vec3B (JVector3F v) => new() { Vec = (JVector3B) v };
		public static explicit operator Vec3B (JVector3D v) => new() { Vec = (JVector3B) v };
		
		public static implicit operator Vec3B ((byte, byte, byte) tup) => new() { Vec = (JVector3B) tup };
		public static implicit operator Vec3B (byte[] arr) => new() { Vec = (JVector3B) arr };
		
		public static implicit operator JVector3B (Vec3B v) => new(v[0], v[1], v[2]);
		public static implicit operator JVector3I (Vec3B v) => new(v[0], v[1], v[2]);
		public static implicit operator JVector3L (Vec3B v) => new(v[0], v[1], v[2]);
		public static implicit operator JVector3F (Vec3B v) => new(v[0], v[1], v[2]);
		public static implicit operator JVector3D (Vec3B v) => new(v[0], v[1], v[2]);
		
		public JVector3B Unwrap() => (JVector3B) Vec;
	}
	
	public class Vec3D: Vec3<double> {
		public Vec3D(Action<int, double>? setterHook = null): this(0, 0, 0, setterHook) { }
		public Vec3D(Action setterHook): this(0, 0, 0, (_, _) => setterHook()) { }
		public Vec3D(JVector3D vec, Action<int, double>? setterHook = null): this(vec.X, vec.Y, vec.Z, setterHook) { }
		public Vec3D(JVector3D vec, Action setterHook): this(vec.X, vec.Y, vec.Z, (_, _) => setterHook()) { }
		public Vec3D(double x, double y, double z, Action setterHook): this(x, y, z, (_, _) => setterHook()) { }
		
		public Vec3D(double x, double y, double z, Action<int, double>? setterHook = null) {
			JVector3D vec = (x, y, z);
			vec.SetterHook = setterHook;
			Vec = vec;
		}
		
		internal JVector3D Inner {
			set {
				JVector3D vec = value;
				vec.SetterHook = ((JVector3D) Vec).SetterHook;
				Vec = vec;
				// Trigger hook for all elements
				for (var i = 0; i < 3; i++) {
					vec.SetterHook?.Invoke(i, vec[i]);
				}
			}
		}
		
		public static explicit operator Vec3D (JVector3B v) => new() { Vec = (JVector3D) v };
		public static explicit operator Vec3D (JVector3I v) => new() { Vec = (JVector3D) v };
		public static explicit operator Vec3D (JVector3L v) => new() { Vec = (JVector3D) v };
		public static explicit operator Vec3D (JVector3F v) => new() { Vec = (JVector3D) v };
		public static implicit operator Vec3D (JVector3D v) => new() { Vec =             v };
		
		public static implicit operator Vec3D ((double, double, double) tup) => new() { Vec = (JVector3D) tup };
		public static implicit operator Vec3D (double[] arr) => new() { Vec = (JVector3D) arr };
		
		public static explicit operator JVector3B (Vec3D v) => new((byte)  v[0], (byte)  v[1], (byte)  v[2]);
		public static explicit operator JVector3I (Vec3D v) => new((int)   v[0], (int)   v[1], (int)   v[2]);
		public static explicit operator JVector3L (Vec3D v) => new((long)  v[0], (long)  v[1], (long)  v[2]);
		public static explicit operator JVector3F (Vec3D v) => new((float) v[0], (float) v[1], (float) v[2]);
		public static implicit operator JVector3D (Vec3D v) => new(v[0], v[1], v[2]);
		
		public JVector3D Unwrap() => (JVector3D) Vec;
	}
	
	public class Vec4B: Vec4<byte> {
		public Vec4B(Action<int, byte>? setterHook = null): this(0, 0, 0, 0, setterHook) { }
		public Vec4B(Action setterHook): this(0, 0, 0, 0, (_, _) => setterHook()) { }
		public Vec4B(JVector4B vec, Action<int, byte>? setterHook = null): this(vec.X, vec.Y, vec.Z, vec.W, setterHook) { }
		public Vec4B(JVector4B vec, Action setterHook): this(vec.X, vec.Y, vec.Z, vec.W, (_, _) => setterHook()) { }
		public Vec4B(byte x, byte y, byte z, byte w, Action setterHook): this(x, y, z, w, (_, _) => setterHook()) { }
		
		public Vec4B(byte x, byte y, byte z, byte w, Action<int, byte>? setterHook = null) {
			var vec = new JVector4B(x, y, z, w);
			vec.SetterHook = setterHook;
			Vec = vec;
		}
		
		internal JVector4B Inner {
			set {
				JVector4B vec = value;
				vec.SetterHook = ((JVector4B) Vec).SetterHook;
				Vec = vec;
				// Trigger hook for all elements
				for (var i = 0; i < 4; i++) {
					vec.SetterHook?.Invoke(i, vec[i]);
				}
			}
		}
		
		public static implicit operator Vec4B (JVector4B v) => new() { Vec =             v };
		public static explicit operator Vec4B (JVector4I v) => new() { Vec = (JVector4B) v };
		public static explicit operator Vec4B (JVector4L v) => new() { Vec = (JVector4B) v };
		public static explicit operator Vec4B (JVector4F v) => new() { Vec = (JVector4B) v };
		public static explicit operator Vec4B (JVector4D v) => new() { Vec = (JVector4B) v };
		
		public static implicit operator Vec4B ((byte, byte, byte, byte) tup) => new() { Vec = (JVector4B) tup };
		public static implicit operator Vec4B (byte[] arr)                   => new() { Vec = (JVector4B) arr };
		
		public static implicit operator JVector4B (Vec4B v) => new(v[0], v[1], v[2], v[3]);
		public static implicit operator JVector4I (Vec4B v) => new(v[0], v[1], v[2], v[3]);
		public static implicit operator JVector4L (Vec4B v) => new(v[0], v[1], v[2], v[3]);
		public static implicit operator JVector4F (Vec4B v) => new(v[0], v[1], v[2], v[3]);
		public static implicit operator JVector4D (Vec4B v) => new(v[0], v[1], v[2], v[3]);
		
		public JVector4B Unwrap() => (JVector4B) Vec;
	}
	
	public class Vec4D: Vec4<double> {
		public Vec4D(Action<int, double>? setterHook = null): this(0, 0, 0, 0, setterHook) { }
		public Vec4D(Action setterHook): this(0, 0, 0, 0, (_, _) => setterHook()) { }
		public Vec4D(JVector4D vec, Action<int, double>? setterHook = null): this(vec.X, vec.Y, vec.Z, vec.W, setterHook) { }
		public Vec4D(JVector4D vec, Action setterHook): this(vec.X, vec.Y, vec.Z, vec.W, (_, _) => setterHook()) { }
		public Vec4D(double x, double y, double z, double w, Action setterHook): this(x, y, z, w, (_, _) => setterHook()) { }
		
		public Vec4D(double x, double y, double z, double w, Action<int, double>? setterHook = null) {
			JVector4D vec = (x, y, z, w);
			vec.SetterHook = setterHook;
			Vec = vec;
		}
		
		internal JVector4D Inner {
			set {
				JVector4D vec = value;
				vec.SetterHook = ((JVector4D) Vec).SetterHook;
				Vec = vec;
				// Trigger hook for all elements
				for (var i = 0; i < 3; i++) {
					vec.SetterHook?.Invoke(i, vec[i]);
				}
			}
		}
		
		public static explicit operator Vec4D (JVector4B v) => new() { Vec = (JVector4D) v };
		public static explicit operator Vec4D (JVector4I v) => new() { Vec = (JVector4D) v };
		public static explicit operator Vec4D (JVector4L v) => new() { Vec = (JVector4D) v };
		public static explicit operator Vec4D (JVector4F v) => new() { Vec = (JVector4D) v };
		public static implicit operator Vec4D (JVector4D v) => new() { Vec =             v };
		
		public static implicit operator Vec4D ((double, double, double, double) tup) => new() { Vec = (JVector4D) tup };
		public static implicit operator Vec4D (double[] arr)                         => new() { Vec = (JVector4D) arr };
		
		public static explicit operator JVector4B (Vec4D v) => new((byte)  v[0], (byte)  v[1], (byte)  v[2], (byte)  v[3]);
		public static explicit operator JVector4I (Vec4D v) => new((int)   v[0], (int)   v[1], (int)   v[2], (int)   v[3]);
		public static explicit operator JVector4L (Vec4D v) => new((long)  v[0], (long)  v[1], (long)  v[2], (long)  v[3]);
		public static explicit operator JVector4F (Vec4D v) => new((float) v[0], (float) v[1], (float) v[2], (float) v[3]);
		public static implicit operator JVector4D (Vec4D v) => new(v[0], v[1], v[2], v[3]);
		
		public JVector4D Unwrap() => (JVector4D) Vec;
	}
}