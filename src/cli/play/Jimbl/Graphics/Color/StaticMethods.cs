namespace Jimbl.Graphics;

using System.Diagnostics;
using JMath;

public partial class Color {
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
	
	// Conversions
	public static implicit operator Color (JVector3B          rgb) => new(rgb[0],    rgb[1],    rgb[2]   );
	public static implicit operator Color ((byte, byte, byte) rgb) => new(rgb.Item1, rgb.Item2, rgb.Item3);
	public static explicit operator Color (byte[]             rgb) => new(rgb[0],    rgb[1],    rgb[2]   );
	
	public static implicit operator Color (JVector4B                rgba) => new(rgba[0],    rgba[1],    rgba[2]   , rgba[3]   );
	public static implicit operator Color ((byte, byte, byte, byte) rgba) => new(rgba.Item1, rgba.Item2, rgba.Item3, rgba.Item4);
	
	public static implicit operator Color (JVector3D                rgb) => new(rgb[0],    rgb[1],    rgb[2]   );
	public static implicit operator Color ((double, double, double) rgb) => new(rgb.Item1, rgb.Item2, rgb.Item3);
	public static explicit operator Color (double[]                 rgb) => new(rgb[0],    rgb[1],    rgb[2]   );
	
	public static implicit operator Color (JVector4D                        rgba) => new(rgba[0],    rgba[1],    rgba[2]   , rgba[3]   );
	public static implicit operator Color ((double, double, double, double) rgba) => new(rgba.Item1, rgba.Item2, rgba.Item3, rgba.Item4);
	
	public static explicit operator JVector3B (Color col) => col.vec3.Copy(n => (byte) Math.Clamp(n, 0, 255));
	public static explicit operator JVector3D (Color col) => col.SRGB.Unwrap();
	public static explicit operator JVector4B (Color col) => col.vec.Copy(n => (byte) Math.Clamp(n, 0, 255));
	
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