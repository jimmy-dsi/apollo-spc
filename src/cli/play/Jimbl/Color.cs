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
	
	public static Color CGreen = new(ansiCode: 32);
	
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