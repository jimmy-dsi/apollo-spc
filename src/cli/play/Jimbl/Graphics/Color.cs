namespace Jimbl.Graphics;

using System.Numerics;
using System.Diagnostics;
using DataStructs;
using JMath;

public class Color {
	public abstract class Vec3<T> where T: INumber<T> {
		internal JVector3<T> Vec { get; set; }
		
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
	
	public enum Space {
		RGB, LRGB, HSV, HSL, XYZ, Lab, LCh
	}
	
	public const double Gamma = 2.2;
	//static Cache<JVector3B, JVector3D> rgbToLabCache = new(sizeLimit: 256);
	
	public static Color FromRGB(byte r, byte g, byte b)       => new(r, g, b);
	public static Color FromRGB(double r, double g, double b) => new(r, g, b);
	
	public static Color FromRGB(JVector3B rgb) => FromRGB(rgb[0], rgb[1], rgb[2]);
	public static Color FromRGB(JVector3D rgb) => FromRGB(rgb[0], rgb[1], rgb[2]);
	
	public static Color FromLRGB(double r, double g, double b) => new(r, g, b, colorSpace: Space.LRGB);
	public static Color FromLRGB(JVector3D rgb)                => FromLRGB(rgb[0], rgb[1], rgb[2]);
	
	public static Color FromHSV(double h, double s, double v) => new(h, s, v, colorSpace: Space.HSV);
	public static Color FromHSV(JVector3D hsv)                => FromHSV(hsv[0], hsv[1], hsv[2]);
	
	public static Color FromHSL(double h, double s, double L) => new(h, s, L, colorSpace: Space.HSL);
	public static Color FromHSL(JVector3D hsl)                => FromHSV(hsl[0], hsl[1], hsl[2]);
	
	public static Color FromXYZ(double x, double y, double z) => new(x, y, z, colorSpace: Space.XYZ);
	public static Color FromXYZ(JVector3D xyz)                => FromXYZ(xyz[0], xyz[1], xyz[2]);
	
	public static Color FromLab(double L, double a, double b) => new(L, a, b, colorSpace: Space.Lab);
	public static Color FromLab(JVector3D lab)                => FromLab(lab[0], lab[1], lab[2]);
	
	public static Color FromLCh(double L, double c, double h) => new(L, c, h, colorSpace: Space.LCh);
	public static Color FromLCh(JVector3D lch)                => FromLCh(lch[0], lch[1], lch[2]);
	
	JVector3I vec = new();
	
	// Other vector representations
	Vec3B rgb;
	Vec3D srgb;
	Vec3D lrgb;
	Vec3D hsv;
	Vec3D hsl;
	Vec3D xyz;
	Vec3D lab;
	Vec3D lch;

	public Space DefaultSpace {
		set {
			ArithSpace  = value;
			BlendSpace  = value;
			FilterSpace = value;
		}
	}

	public Space ArithSpace  { get; set; } = Space.RGB;
	public Space BlendSpace  { get; set; } = Space.LRGB;
	public Space FilterSpace { get; set; } = Space.LRGB;
	
	public byte Red {
		get => (byte) Math.Clamp(vec.X, 0, 255);
		set => vec.X = value;
	}
	
	public byte Green {
		get => (byte) Math.Clamp(vec.Y, 0, 255);
		set => vec.Y = value;
	}
	
	public byte Blue {
		get => (byte) Math.Clamp(vec.Z, 0, 255);
		set => vec.Z = value;
	}
	
	public byte Alpha {
		get /*=> vec.W*/;
		set /*=> vec.W = value*/;
	}
	
	// Other vector representations
	public Vec3B RGB  { get => rgb;  set => rgb .Inner = value; }
	public Vec3D SRGB { get => srgb; set => srgb.Inner = value; }
	public Vec3D LRGB { get => lrgb; set => lrgb.Inner = value; }
	public Vec3D HSV  { get => hsv;  set => hsv .Inner = value; }
	public Vec3D HSL  { get => hsl;  set => hsl .Inner = value; }
	public Vec3D XYZ  { get => xyz;  set => xyz .Inner = value; }
	public Vec3D Lab  { get => lab;  set => lab .Inner = value; }
	public Vec3D LCh  { get => lch;  set => lch .Inner = value; }
	
	public Color(byte r, byte g, byte b, byte a = 255) {
		Red   = r;
		Green = g;
		Blue  = b;
		Alpha = a;
		
		init();
	}
	
	public Color(JVector3D xyz, double a = 1.0, Space colorSpace = Space.RGB): this(xyz.X, xyz.Y, xyz.Z, a, colorSpace) { }
	
	public Color(double x, double y, double z, double a = 1.0, Space colorSpace = Space.RGB) {
		JVector3D input = (x, y, z);
			
		switch (colorSpace) {
			case Space.RGB: {
				vec = denormalize(input);
				break;
			}
			
			case Space.LRGB: {
				vec = input.Copy(v => denormalize(fromLinear(v)));
				break;
			}
			
			case Space.HSV: {
				vec = hsvToRGB(input);
				break;
			}
			
			case Space.HSL: {
				vec = hslToRGB(input);
				break;
			}
			
			case Space.XYZ: {
				vec = xyzToRGB(input);
				break;
			}
			
			case Space.Lab: {
				vec = labToRGB(input);
				break;
			}
			
			case Space.LCh: {
				vec = lchToRGB(input);
				break;
			}
			
			default: {
				throw new UnreachableException();
			}
		}
		
		Alpha = (byte) Math.Clamp(denormalize(a), 0, 255);
		init();
	}
	
	void init() {
		var self = this;
		
		// Setup other vector representations
		rgb  = new(vec.Copy(n => (byte) Math.Clamp(n, 0, 255)), (i, v) => self.vec[i] = v);
		srgb = new(vec.Copy(normalize),                         (i, v) => self.vec[i] = denormalize(v));
		lrgb = new(vec.Copy(toLinear),                          (i, v) => self.vec[i] = denormalize(fromLinear(v)));
		hsv  = new(rgbToHSV(vec),                               () => self.vec = hsvToRGB(hsv));
		hsl  = new(rgbToHSL(vec),                               () => self.vec = hslToRGB(hsl));
		xyz  = new(rgbToXYZ(vec),                               () => self.vec = xyzToRGB(xyz));
		lab  = new(rgbToLab(vec),                               () => self.vec = labToRGB(lab));
		lch  = new(rgbToLCh(vec),                               () => self.vec = lchToRGB(lch));
	}
	
	public byte this[int channel] {
		get => (byte) Math.Clamp(vec[channel], 0, 255);
		set => vec[channel] = value;
	}
	
	// Conversions
	public static implicit operator Color (JVector3B          rgb) => new(rgb[0],    rgb[1],    rgb[2]   );
	public static implicit operator Color ((byte, byte, byte) rgb) => new(rgb.Item1, rgb.Item2, rgb.Item3);
	public static explicit operator Color (byte[]             rgb) => new(rgb[0],    rgb[1],    rgb[2]   );
	
	public static implicit operator Color (JVector3D                rgb) => new(rgb[0],    rgb[1],    rgb[2]   );
	public static implicit operator Color ((double, double, double) rgb) => new(rgb.Item1, rgb.Item2, rgb.Item3);
	public static explicit operator Color (double[]                 rgb) => new(rgb[0],    rgb[1],    rgb[2]   );
	
	public static explicit operator JVector3B (Color col) => (JVector3B) col.vec;
	public static explicit operator JVector3D (Color col) => col.SRGB.Unwrap();
	
	// Operations
	public Color Add(Color other) => Add(other, ArithSpace);
	
	public Color Add(Color other, Space colorSpace) {
		return colorSpace switch {
			Space .RGB =>      new(SRGB.Unwrap() + other.SRGB.Unwrap()),
			Space.LRGB => FromLRGB(LRGB.Unwrap() + other.LRGB.Unwrap()),
			Space .Lab =>  FromLab(Lab .Unwrap() + other .Lab.Unwrap()),
			_          => throw new NotSupportedException()
		};
	}
	
	public Color Subtract(Color other) => Subtract(other, ArithSpace);
	
	public Color Subtract(Color other, Space colorSpace) {
		return colorSpace switch {
			Space .RGB =>      new(SRGB.Unwrap() - other.SRGB.Unwrap()),
			Space.LRGB => FromLRGB(LRGB.Unwrap() - other.LRGB.Unwrap()),
			Space .Lab =>  FromLab(Lab .Unwrap() - other .Lab.Unwrap()),
			_          => throw new NotSupportedException()
		};
	}
	
	public Color Multiply(Color other) => Multiply(other, ArithSpace);
	
	public Color Multiply(Color other, Space colorSpace) {
		return colorSpace switch {
			Space .RGB =>      new(SRGB[0] * other.SRGB[0], SRGB[1] * other.SRGB[1], SRGB[2] * other.SRGB[2]),
			Space.LRGB => FromLRGB(LRGB[0] * other.LRGB[0], LRGB[1] * other.LRGB[1], LRGB[2] * other.LRGB[2]),
			Space .Lab =>  FromLab(
				Lab[0] * other.Lab[0] / 100,
				Lab[1] * other.Lab[1] / 127,
				Lab[2] * other.Lab[2] / 127
			),
			_          => throw new NotSupportedException()
		};
	}
	
	public Color Multiply(double other) => Multiply(other, ArithSpace);
	
	public Color Multiply(double other, Space colorSpace) {
		return colorSpace switch {
			Space .RGB =>      new(SRGB.Unwrap() * other),
			Space.LRGB => FromLRGB(LRGB.Unwrap() * other),
			Space .Lab =>  FromLab(Lab .Unwrap() * other),
			_          => throw new NotSupportedException()
		};
	}
	
	public Color Divide(double other) => Multiply(other, ArithSpace);
	
	public Color Divide(double other, Space colorSpace) {
		return colorSpace switch {
			Space .RGB =>      new(SRGB.Unwrap() * other),
			Space.LRGB => FromLRGB(LRGB.Unwrap() / other),
			Space .Lab =>  FromLab(Lab .Unwrap() / other),
			_          => throw new NotSupportedException()
		};
	}
	
	public Color DivideFrom(double other) => Multiply(other, ArithSpace);
	
	public Color DivideFrom(double other, Space colorSpace) {
		return colorSpace switch {
			Space .RGB =>      new(SRGB.Unwrap() * other),
			Space.LRGB => FromLRGB(other / LRGB.Unwrap()),
			Space .Lab =>  FromLab(other / Lab .Unwrap()),
			_          => throw new NotSupportedException()
		};
	}
	
	public Color Blend(Color other, double amount) => Blend(other, amount, BlendSpace);
	
	public Color Blend(Color other, double amount, Space colorSpace) {
		amount = Math.Clamp(amount, 0, 1);
		
		return colorSpace switch {
			Space .RGB =>      new(SRGB.Unwrap() * (1 - amount) + other.SRGB.Unwrap() * amount),
			Space.LRGB => FromLRGB(LRGB.Unwrap() * (1 - amount) + other.LRGB.Unwrap() * amount),
			Space. HSV =>  FromHSV(
				lerpAngle(HSV[0], other.HSV[0], amount),
				HSV[1] * (1 - amount) + other.HSV[1] * amount,
				HSV[2] * (1 - amount) + other.HSV[2] * amount
			),
			Space. HSL =>  FromHSL(
				lerpAngle(HSL[0], other.HSL[0], amount),
				HSL[1] * (1 - amount) + other.HSL[1] * amount,
				HSL[2] * (1 - amount) + other.HSL[2] * amount
			),
			Space .XYZ =>  FromLab(XYZ .Unwrap() * (1 - amount) + other.XYZ .Unwrap() * amount),
			Space .Lab =>  FromLab(Lab .Unwrap() * (1 - amount) + other.Lab .Unwrap() * amount),
			Space. LCh =>  FromLCh(
				LCh[0] * (1 - amount) + other.LCh[0] * amount,
				LCh[1] * (1 - amount) + other.LCh[1] * amount,
				lerpAngle(LCh[2], other.LCh[2], amount)
			),
			_ => throw new NotSupportedException()
		};
	}
	
	public Color Filter(IEnumerable<Color> prevColors, IEnumerable<double> filter) => Filter(prevColors, filter, FilterSpace);
	
	public Color Filter(IEnumerable<Color> prevColors, IEnumerable<double> filter, Space colorSpace) {
		if (!filter.Any()) {
			return this;
		}
		
		Color resultColor = Multiply(filter.First(), colorSpace);
		
		var pc = prevColors.ToArray();
		var f  = filter    .ToArray();
		
		for (var i = 1; i < f.Length; i++) {
			var col  = pc.Length == 0 ? this : i - 1 >= pc.Length ? pc[^1] : pc[i - 1];
			var samp = f[i];
			
			var c = col.Multiply(samp, colorSpace);
			
			resultColor = resultColor.Add(c, colorSpace);
		}
		
		return resultColor;
	}
	
	public Color AsClamped() {
		var (r, g, b) = RGB.Unwrap().AsTuple;
		return new(r, g, b);
	}
	
	public Color AsCompressed(double ratio = 0.5) {
		var v = SRGB.Unwrap();
		var (n, max) = v.AsArray.Enum().OrderByDescending(x => Math.Abs(x.Item2)).First();
		
		if (max > 1) {
			var overshoot    = max - 1;
			var adjOvershoot = overshoot * (1 - ratio);
		
			v /= (1 + adjOvershoot);
		}
		
		return new(v[0], v[1], v[2]);
	}
	
	public override int GetHashCode() {
		return vec.GetHashCode();
	}

	public override bool Equals(object? obj) {
		if (obj is Color other) {
			return vec == other.vec;
		}
		else {
			return false;
		}
	}
	
	public void Deconstruct(out byte red, out byte green, out byte blue) {
		red   = Red;
		green = Green;
		blue  = Blue;
	}
	
	public void Deconstruct(out byte red, out byte green, out byte blue, out byte alpha) {
		red   = Red;
		green = Green;
		blue  = Blue;
		alpha = 255;
	}

	// Operator overloads
	public static Color operator + (Color  lhs, Color  rhs) => lhs.Add(rhs);
	public static Color operator - (Color  lhs, Color  rhs) => lhs.Subtract(rhs);
	public static Color operator * (Color  lhs, Color  rhs) => lhs.Multiply(rhs);
	public static Color operator * (Color  lhs, double rhs) => lhs.Multiply(rhs);
	public static Color operator * (double lhs, Color  rhs) => rhs.Multiply(lhs);
	public static Color operator / (Color  lhs, double rhs) => lhs.Divide(rhs);
	public static Color operator / (double lhs, Color  rhs) => rhs.DivideFrom(lhs);
	
	public static bool operator == (Color lhs, Color rhs) => lhs.vec == rhs.vec;
	public static bool operator != (Color lhs, Color rhs) => lhs.vec != rhs.vec;
	
	// Misc
	static double lerpAngle(double a, double b, double t) {
		a = JMath.Mod(a, 360);
		b = JMath.Mod(b, 360);
		
		if (a + 180 < b) {
			a += 360;
		}
		else if (b + 180 < a) {
			b += 360;
		}
		
		var newAngle = a * (1 - t) + b * t;
		return JMath.Mod(newAngle, 360);
	}
	
	// Transformations
	static double toLinear(int rgbChannelValue) {
		return toLinear(normalize(rgbChannelValue));
	}
	
	static double toLinear(double rgbChannelValue) {
		return Math.Pow(rgbChannelValue, Gamma);
	}
	
	static double fromLinear(double lrgbChannelValue) {
		return Math.Pow(lrgbChannelValue, 1 / Gamma);
	}
	
	static double normalize(int rgbaChannelValue) {
		return rgbaChannelValue / 255.0;
	}
	
	static int denormalize(double rgbaNormalized) {
		return (int) (rgbaNormalized * 255);
	}
	
	static JVector3D normalize(JVector3I rgb) {
		return rgb / 255.0;
	}
	
	static JVector3I denormalize(JVector3D srgb) {
		return (JVector3I) (srgb * 255);
	}
	
	static JVector3D rgbToHSV(JVector3I rgb) {
		return rgbToHSV(normalize(rgb));
	}
	
	static JVector3D rgbToHSV(JVector3D rgb) {
		JVector3D hsv = new();
		var (r, g, b) = rgb.AsTuple;
		
		var cmax = rgb.AsArray.Max();
		var cmin = rgb.AsArray.Min();
		
		var delta = cmax - cmin;
		
		// Hue
		if (delta == 0) {
			hsv[0] = 0;
		}
		else if (cmax == r) {
			hsv[0] = 60 * (((g - b) / delta * 6 +  6) % 6);
		}
		else if (cmax == g) {
			hsv[0] = 60 * (((b - r) / delta * 6 + 10) % 6);
		}
		else if (cmax == b) {
			hsv[0] = 60 * (((r - g) / delta * 6 + 12) % 6);
		}
		// Adjust hue
		if (hsv[0] < 0) {
			hsv[0] += 360;
		}
		
		hsv[1] = cmax == 0 ? 0 : delta / cmax * 100; // Sat
		hsv[2] = cmax * 100;                         // Val
		
		return hsv;
	}
	
	static JVector3D rgbToHSL(JVector3I rgb) {
		return rgbToHSL(normalize(rgb));
	}
	
	static JVector3D rgbToHSL(JVector3D rgb) {
		JVector3D hsl = new();
		var (r, g, b) = rgb.AsTuple;
		
		var cmax = rgb.AsArray.Max();
		var cmin = rgb.AsArray.Min();
		
		var delta = cmax - cmin;
		var lum   = (cmax + cmin) / 2;
		
		// Hue
		if (delta == 0) {
			hsl[0] = 0;
		}
		else if (cmax == r) {
			hsl[0] = 60 * (((g - b) / delta * 6 +  6) % 6);
		}
		else if (cmax == g) {
			hsl[0] = 60 * (((b - r) / delta * 6 + 10) % 6);
		}
		else if (cmax == b) {
			hsl[0] = 60 * (((r - g) / delta * 6 + 12) % 6);
		}
		// Adjust hue
		if (hsl[0] < 0) {
			hsl[0] += 360;
		}
		
		hsl[1] = cmax == 0 ? 0 : delta / (1 - Math.Abs(2 * lum - 1)) * 100; // Sat
		hsl[2] = lum * 100;                                                 // Lum
		
		return hsl;
	}
	
	static JVector3D rgbToLab(JVector3I rgb) {
		return rgbToLab(normalize(rgb));
	}
	
	static JVector3D rgbToLab(JVector3D rgb) {
		var xyz = rgbToXYZ(rgb);
		var lab = xyzToLab(xyz);
		return lab;
	}
	
	static double[][] rgbToXyzMatrix = [[0.4124, 0.3576, 0.1805],
	                                    [0.2126, 0.7152, 0.0722],
	                                    [0.0193, 0.1192, 0.9505]];
	
	static JVector3D rgbToXYZ(JVector3I rgb) {
		return rgbToXYZ(normalize(rgb));
	}
	
	static JVector3D rgbToXYZ(JVector3D rgb) {
		JVector3D xyz = new();
		
		rgb.Transform(n => {
			if (n > 0.04045) {
				return Math.Pow((n + 0.055) / 1.055, 2.4);
			}
			else {
				return n / 12.92;
			}
		});
		rgb *= 100;
		
		for (var i = 0; i < 3; i++) {
			xyz[i] = rgb * (JVector3D) rgbToXyzMatrix[i];
		}
		
		return xyz;
	}
	
	static JVector3D xyzToLab(JVector3D xyz) {
		JVector3D lab = new();
		
		xyz[0] /= 95.047;
		xyz[1] /= 100.0;
		xyz[2] /= 108.883;
		
		xyz.Transform(n => {
			if (n > 0.008856) {
				return Math.Pow(n, 1.0 / 3);
			}
			else {
				return 7.787 * n + 16.0 / 116;
			}
		});
		
		var (x, y, z) = xyz.AsTuple;
		
		lab[0] = 116 *  y - 16;
		lab[1] = 500 * (x - y);
		lab[2] = 200 * (y - z);
		
		return lab;
	}
	
	static JVector3D rgbToLCh(JVector3I rgb) {
		return rgbToLCh(normalize(rgb));
	}
	
	static JVector3D rgbToLCh(JVector3D rgb) {
		var lab = rgbToLab(rgb);
		var lch = labToLCh(lab);
		return lch;
	}
	
	static JVector3D labToLCh(JVector3D lab) {
		JVector3D lch = lab;
		var (L, a, b) = lab.AsTuple;
		
		lch[1] = Math.Sqrt(a * a + b * b);
		lch[2] = (360 + double.RadiansToDegrees(Math.Atan2(b, a))) % 360;
		
		return lch;
	}
	
	static JVector3I hsvToRGB(JVector3D hsv) {
		var rgb = hsvToSRGB(hsv);
		return rgb.Copy(denormalize);
	}
	
	static JVector3D hsvToSRGB(JVector3D hsv) {
		JVector3D rgb;
		
		// Adjust input
		var (h, s, v) = hsv.AsTuple;
		s /= 100;
		v /= 100;
		
		if (s == 0) {
			rgb = (v, v, v);
		}
		else {
			var hsect = h / 60;
		
			var i = (int) Math.Floor(hsect);
			var f = hsect - i;
		
			var p = v * (1 - s);
			var q = v * (1 - s * f);
			var t = v * (1 - s * (1 - f));
		
			switch (i % 6) {
				case 0:  rgb = (v, t, p); break;
				case 1:  rgb = (q, v, p); break;
				case 2:  rgb = (p, v, t); break;
				case 3:  rgb = (p, q, v); break;
				case 4:  rgb = (t, p, v); break;
				case 5:  rgb = (v, p, q); break;
				default: throw new UnreachableException();
			}
		}
		
		return rgb;
	}
	
	static JVector3I hslToRGB(JVector3D hsl) {
		var rgb = hslToSRGB(hsl);
		return rgb.Copy(denormalize);
	}
	
	static JVector3D hslToSRGB(JVector3D hsl) {
		JVector3D rgb;
		
		// Adjust input
		var (h, s, L) = hsl.AsTuple;
		s /= 100;
		L /= 100;
		
		if (s == 0) {
			rgb = (L, L, L);
		}
		else {
			var c = (1 - Math.Abs(2 * L - 1)) * s;
			var hsect  = h / 60;
			
			var i = (int) Math.Floor(hsect);
			var x = c * (1 - Math.Abs(hsect % 2 - 1));
		
			switch (i % 6) {
				case 0:  rgb = (c, x, 0); break;
				case 1:  rgb = (x, c, 0); break;
				case 2:  rgb = (0, c, x); break;
				case 3:  rgb = (0, x, c); break;
				case 4:  rgb = (x, 0, c); break;
				case 5:  rgb = (c, 0, x); break;
				default: throw new UnreachableException();
			}
			
			var m = L - c / 2;
			rgb += (m, m, m);
		}
			
		return rgb;
	}
	
	static JVector3I labToRGB(JVector3D lab) {
		var rgb = labToSRGB(lab);
		return rgb.Copy(denormalize);
	}
	
	static JVector3D labToSRGB(JVector3D lab) {
		var xyz = labToXYZ(lab);
		var rgb = xyzToSRGB(xyz);
		return rgb;
	}
	
	static JVector3D labToXYZ(JVector3D lab) {
		JVector3D xyz = new();
		var (L, a, b) = lab.AsTuple;
		
		xyz.Y = (L + 16) / 116;
		xyz.X = a / 500 + xyz.Y;
		xyz.Z = xyz.Y - b / 200;
		
		xyz.Transform(n => {
			if (Math.Pow(n, 3) > 0.008856) {
				return Math.Pow(n, 3);
			}
			else {
				return (n - 16.0 / 116) / 7.787;
			}
		});
		
		xyz.X *= 95.047;
		xyz.Y *= 100;
		xyz.Z *= 108.883;
		
		xyz /= 100;
		return xyz;
	}
	
	static double[][] xyzToRgbMatrix = [[ 3.2406, -1.5372, -0.4986],
	                                    [-0.9689,  1.8758,  0.0415],
	                                    [ 0.0557, -0.2040,  1.0570]];
	
	static JVector3I xyzToRGB(JVector3D xyz) {
		var rgb = labToSRGB(xyz);
		return rgb.Copy(denormalize);
	}
	
	static JVector3D xyzToSRGB(JVector3D xyz) {
		JVector3D rgb = new();
		
		for (var i = 0; i < 3; i++) {
			rgb[i] = xyz * (JVector3D) xyzToRgbMatrix[i];
		}
		
		rgb.Transform(n => {
			if (n > 0.0031308) {
				return 1.055 * Math.Pow(n, 1 / 2.4) - 0.055;
			}
			else {
				return n * 12.92;
			}
		});
		
		return rgb;
	}
	
	static JVector3I lchToRGB(JVector3D lch) {
		var rgb = lchToSRGB(lch);
		return rgb.Copy(denormalize);
	}
	
	static JVector3D lchToSRGB(JVector3D lch) {
		var lab = lchToLab(lch);
		var rgb = labToSRGB(lab);
		return rgb;
	}
	
	static JVector3D lchToLab(JVector3D lch) {
		JVector3D lab = lch;
		var (L, c, h) = lch.AsTuple;
		
		var rad = double.DegreesToRadians(h);
		
		lab[1] = c * Math.Cos(rad);
		lab[2] = c * Math.Sin(rad);
		
		return lab;
	}
}