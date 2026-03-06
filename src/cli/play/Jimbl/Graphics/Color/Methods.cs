namespace Jimbl.Graphics;

using System.Diagnostics;
using JMath;

public partial class Color {
	public Color(byte r, byte g, byte b, byte a = 255) {
		Red   = r;
		Green = g;
		Blue  = b;
		Alpha = a;
		
		inited = true;
	}
	
	public Color(JVector3D xyz, double a = 1.0, Space colorSpace = Space.RGB): this(xyz.X, xyz.Y, xyz.Z, a, colorSpace) { }
	public Color(JVector4D xyzw, Space colorSpace = Space.RGB): this(xyzw.X, xyzw.Y, xyzw.Z, xyzw.W, colorSpace) { }
	
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
		
		Alpha  = (byte) Math.Clamp(denormalize(a), 0, 255);
		inited = true;
	}
	
	Vec3B getRGB() {
		if (rgb is null) {
			var self = this;
			rgb = new(vec3.Copy(n => (byte) Math.Clamp(n, 0, 255)), (i, v) => self.vec[i] = v);
		}
		
		return rgb;
	}
	
	Vec3D getSRGB() {
		if (srgb is null) {
			var self = this;
			srgb = new(vec3.Copy(normalize), (i, v) => self.vec[i] = denormalize(v));
		}
		
		return srgb;
	}
	
	Vec3D getLRGB() {
		if (lrgb is null) {
			var self = this;
			lrgb = new(vec3.Copy(toLinear), (i, v) => self.vec[i] = denormalize(fromLinear(v)));
		}
		
		return lrgb;
	}
	
	Vec3D getHSV() {
		if (hsv is null) {
			var self = this;
			hsv = new(rgbToHSV(vec3), (i, _) => { if (self.inited || i == 2) self.vec = hsvToRGB(hsv); });
		}
		
		return hsv;
	}
	
	Vec3D getHSL() {
		if (hsl is null) {
			var self = this;
			hsl = new(rgbToHSL(vec3), (i, _) => { if (self.inited || i == 2) self.vec = hslToRGB(hsl); });
		}
		
		return hsl;
	}
	
	Vec3D getXYZ() {
		if (xyz is null) {
			var self = this;
			xyz = new(rgbToXYZ(vec3), (i, _) => { if (self.inited || i == 2) self.vec = xyzToRGB(xyz); });
		}
		
		return xyz;
	}
	
	Vec3D getLab() {
		if (lab is null) {
			var self = this;
			lab = new(rgbToLab(vec3), (i, _) => { if (self.inited || i == 2) self.vec = labToRGB(xyz); });
		}
		
		return lab;
	}
	
	Vec3D getLCh() {
		if (lch is null) {
			var self = this;
			lch = new(rgbToLCh(vec3), (i, _) => { if (self.inited || i == 2) self.vec = lchToRGB(lch); });
		}
		
		return lch;
	}
	
	
	Vec4B getRGBA() {
		if (rgba is null) {
			var self = this;
			rgba = new(vec.Copy(n => (byte) Math.Clamp(n, 0, 255)), (i, v) => self.vec[i] = v);
		}
		
		return rgba;
	}
	
	Vec4D getSRGBA() {
		if (srgba is null) {
			var self = this;
			srgba = new(vec.Copy(normalize), (i, v) => self.vec[i] = denormalize(v));
		}
		
		return srgba;
	}
	
	Vec4D getLRGBA() {
		if (lrgba is null) {
			var self = this;
			lrgba = new(appendAlpha(vec3.Copy(toLinear)), (i, v) => self.vec[i] = denormalize(fromLinear(v)));
		}
		
		return lrgba;
	}
	
	Vec4D getHSVA() {
		if (hsva is null) {
			var self = this;
			hsva = new(appendAlpha(rgbToHSV(vec3)), (i, _) => { if (self.inited || i == 2) self.vec = hsvToRGB(hsv); });
		}
		
		return hsva;
	}
	
	Vec4D getHSLA() {
		if (hsla is null) {
			var self = this;
			hsla = new(appendAlpha(rgbToHSL(vec3)), (i, _) => { if (self.inited || i == 2) self.vec = hslToRGB(hsl); });
		}
		
		return hsla;
	}
	
	Vec4D getXYZA() {
		if (xyza is null) {
			var self = this;
			xyza = new(appendAlpha(rgbToXYZ(vec3)), (i, _) => { if (self.inited || i == 2) self.vec = xyzToRGB(xyz); });
		}
		
		return xyza;
	}
	
	Vec4D getLabA() {
		if (laba is null) {
			var self = this;
			laba = new(appendAlpha(rgbToLab(vec3)), (i, _) => { if (self.inited || i == 2) self.vec = labToRGB(xyz); });
		}
		
		return laba;
	}
	
	Vec4D getLChA() {
		if (lcha is null) {
			var self = this;
			lcha = new(appendAlpha(rgbToLCh(vec3)), (i, _) => { if (self.inited || i == 2) self.vec = lchToRGB(lch); });
		}
		
		return lcha;
	}
	
	JVector4B appendAlpha(JVector3B xyz) {
		JVector4B xyzw = xyz;
		xyzw.W = Alpha;
		return xyzw;
	}
	
	JVector4D appendAlpha(JVector3D xyz) {
		JVector4D xyzw = xyz;
		xyzw.W = normalize(Alpha);
		return xyzw;
	}
	
	public byte this[int channel] {
		get => (byte) Math.Clamp(vec[channel], 0, 255);
		set => vec[channel] = value;
	}
	
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
		var f = filter.ToArray();
		
		if (f.Length == 0) {
			return this;
		}
		
		Color resultColor = Multiply(f[0], colorSpace);
		
		var pc = prevColors.ToArray();
		
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
		alpha = Alpha;
	}
}