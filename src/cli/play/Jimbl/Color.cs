namespace Jimbl;

public class Color {
	uint value;
	
	public byte Red   => (byte) (value >> 16);
	public byte Green => (byte) (value >>  8 & 0xFF);
	public byte Blue  => (byte) (value       & 0xFF);
	
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
	
	public static Color Black      = new(ansiCode: 30);
	public static Color CRed       = new(ansiCode: 31);
	public static Color CGreen     = new(ansiCode: 32);
	public static Color Yellow     = new(ansiCode: 33);
	public static Color CBlue      = new(ansiCode: 34);
	public static Color Magenta    = new(ansiCode: 35);
	public static Color Cyan       = new(ansiCode: 36);
	
	public static Color DarkGrey   = new(ansiCode: 90);
	
	public static Color BGBlack    = new(ansiCode: 40);
	public static Color BGRed      = new(ansiCode: 41);
	public static Color BGGreen    = new(ansiCode: 42);
	public static Color BGYellow   = new(ansiCode: 43);
	public static Color BGBlue     = new(ansiCode: 44);
	public static Color BGMagenta  = new(ansiCode: 45);
	public static Color BGCyan     = new(ansiCode: 46);
	
	public static Color BGDarkGrey = new(ansiCode: 100);
	
	public Color(byte ansiCode) {
		value = (uint) ansiCode << 24;
	}
	
	public Color(byte red, byte green, byte blue) {
		value = (uint) (red << 16 | green << 8 | blue);
	}
	
	public Color(byte red, byte green, byte blue, bool bg) {
		value = (uint) (red << 16 | green << 8 | blue);
		if (bg) {
			value |= 0x01_000000;
		}
	}
}