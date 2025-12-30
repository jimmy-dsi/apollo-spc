namespace Jimbl;

public class Color {
	uint value;
	
	public byte Red   => (byte) (value >> 16);
	public byte Green => (byte) (value >>  8 & 0xFF);
	public byte Blue  => (byte) (value       & 0xFF);
	
	public double L { get; private set; }
	public double A { get; private set; }
	public double B { get; private set; }
	public double C { get; private set; }
	public double H { get; private set; }
	
	public static Color FromLCH(double L, double C, double H, bool bg = false) {
		// Convert to Lab
		double a = C * Math.Cos(H);
		double b = C * Math.Sin(H);
		
		var color = FromLab(L, a, b, bg: bg);
		color.C = C;
		color.H = H;
		
		return color;
	}
	
	public static Color FromLab(double L, double a, double b, bool bg = false) {
		double y = (L + 16) / 116;
		double x = a / 500 + y;
		double z = y - b / 200;
		
		if (Math.Pow(y, 3) > 0.008856) {
			y = Math.Pow(y, 3);
		}
		else {
			y = (y  - 16.0 / 116) / 7.787;
		}
		
		if (Math.Pow(x, 3) > 0.008856) {
			x = Math.Pow(x, 3);
		}
		else {
			x = (x  - 16.0 / 116) / 7.787;
		}
		
		if (Math.Pow(z, 3) > 0.008856) {
			z = Math.Pow(z, 3);
		}
		else {
			z = (z  - 16.0 / 116) / 7.787;
		}
		
		x *= 95.047;
		y *= 100;
		z *= 108.883;
		
		x /= 100;
		y /= 100;
		z /= 100;
		
		double red    = x *  3.2406 + y * -1.5372 + z * -0.4986;
		double green  = x * -0.9689 + y *  1.8758 + z *  0.0415;
		double blue   = x *  0.0557 + y * -0.2040 + z *  1.0570;
		
		if (red > 0.0031308) {
			red = 1.055 * Math.Pow(red, 1 / 2.4) - 0.055;
		}
		else {
			red *= 12.92;
		}
		
		if (green > 0.0031308) {
			green = 1.055 * Math.Pow(green, 1 / 2.4) - 0.055;
		}
		else {
			green *= 12.92;
		}
		
		if (blue > 0.0031308) {
			blue = 1.055 * Math.Pow(blue, 1 / 2.4) - 0.055;
		}
		else {
			blue *= 12.92;
		}
		
		Color col = new(
			(byte) Math.Clamp(red   * 255, 0, 255),
			(byte) Math.Clamp(green * 255, 0, 255),
			(byte) Math.Clamp(blue  * 255, 0, 255),
			bg: bg
		) {
			L = L,
			A = a,
			B = b
		};
		
		col.calcLCH(compLab: false);
		return col;
	}
	
	public string AnsiString {
		get {
			if (value >> 24 > 1) {
				var code = value >> 24;
				return $"\x1B[{code}m";
			}
			else if (value >> 24 == 1) {
				return $"\x1B[48;2;{Red};{Green};{Blue}m";
			}
			else {
				return $"\x1B[38;2;{Red};{Green};{Blue}m";
			}
		}
	}
	
	public bool IsRGB => value >> 24 <= 1;
	
	public bool IsBG => value >> 24 == 1;
	
	public static Color Black      = new(ansiCode: 30);
	public static Color CRed       = new(ansiCode: 31);
	public static Color CGreen     = new(ansiCode: 32);
	public static Color Yellow     = new(ansiCode: 33);
	public static Color CBlue      = new(ansiCode: 34);
	public static Color Magenta    = new(ansiCode: 35);
	public static Color Cyan       = new(ansiCode: 36);
	public static Color Grey       = new(ansiCode: 37);
	
	public static Color DarkGrey   = new(ansiCode: 90);
	
	public static Color BGBlack    = new(ansiCode: 40);
	public static Color BGRed      = new(ansiCode: 41);
	public static Color BGGreen    = new(ansiCode: 42);
	public static Color BGYellow   = new(ansiCode: 43);
	public static Color BGBlue     = new(ansiCode: 44);
	public static Color BGMagenta  = new(ansiCode: 45);
	public static Color BGCyan     = new(ansiCode: 46);
	public static Color BGGrey     = new(ansiCode: 47);
	
	public static Color BGDarkGrey = new(ansiCode: 100);
	public static Color BGWhite    = new(ansiCode: 107);
	
	public Color(byte ansiCode) {
		value = (uint) ansiCode << 24;
	}
	
	public Color(double red, double green, double blue, bool bg = false):
		this(
			(byte) Math.Clamp(red   * 255, 0, 255),
			(byte) Math.Clamp(green * 255, 0, 255),
			(byte) Math.Clamp(blue  * 255, 0, 255),
			bg: bg
		) { }
	
	public Color(byte red, byte green, byte blue): this(red, green, blue, bg: false) { }
	
	public Color(byte red, byte green, byte blue, bool bg): this(red, green, blue, bg, compLab: true) { }
	
	Color(byte red, byte green, byte blue, bool bg, bool compLab) {
		value = (uint) (red << 16 | green << 8 | blue);
		if (bg) {
			value |= 0x01_000000;
		}
		
		if (compLab) {
			calcLCH();
		}
	}
	
	void calcLab() {
		double red   = (double) Red   / 255;
		double green = (double) Green / 255;
		double blue  = (double) Blue  / 255;
		
		if (red > 0.04045) {
			red = Math.Pow((red + 0.055) / 1.055, 2.4);
		}
		else {
			red /= 12.92;
		}
		
		if (green > 0.04045) {
			green = Math.Pow((green + 0.055) / 1.055, 2.4);
		}
		else {
			green /= 12.92;
		}
		
		if (blue > 0.04045) {
			blue = Math.Pow((blue + 0.055) / 1.055, 2.4);
		}
		else {
			blue /= 12.92;
		}
		
		red   *= 100;
		green *= 100;
		blue  *= 100;
		
		double x = red * 0.4124 + green * 0.3576 + blue * 0.1805;
		double y = red * 0.2126 + green * 0.7152 + blue * 0.0722;
		double z = red * 0.0193 + green * 0.1192 + blue * 0.9505;
		
		x /= 95.047;
		y /= 100.0;
		z /= 108.883;
		
		if (x > 0.008856) {
			x = Math.Pow(x, 1.0 / 3);
		}
		else {
			x = 7.787 * x + 16.0 / 116;
		}
		
		if (y > 0.008856) {
			y = Math.Pow(y, 1.0 / 3);
		}
		else {
			y = 7.787 * y + 16.0 / 116;
		}
		
		if (z > 0.008856) {
			z = Math.Pow(z, 1.0 / 3);
		}
		else {
			z = 7.787 * z + 16.0 / 116;
		}
		
		L = 116 * y - 16;
		A = 500 * (x - y);
		B = 200 * (y - z);
	}
	
	void calcLCH(bool compLab = true) {
		if (compLab) {
			calcLab();
		}
		
		C = Math.Sqrt(A * A + B * B);
		H = Math.Atan2(B, A);
	}
}