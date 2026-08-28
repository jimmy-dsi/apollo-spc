namespace Jimbl.Graphics;

using Jimbl.JMath;

public class AnsiColor {
	public enum Code {
		Black    =  0,       Red,       Green,       Yellow,       Blue,       Magenta,       Cyan,  Grey,
		DarkGrey = 60, BrightRed, BrightGreen, BrightYellow, BrightBlue, BrightMagenta, BrightCyan, White,
		Invalid  = 255
	}
	
	object? foregroundColor;
	object? backgroundColor;
	
	public static bool RGB24Enabled { get; set; } = false;
	
	static int chan6val(byte chan256val) {
		if (chan256val <= 47) return 0;
		return Math.Clamp(JMath.Round((chan256val - 40) / 40.0), 1, 5);
	}
	
	static Color col256ToRGB24(byte c) {
		if (c is < 16) throw new NotSupportedException();
		
		if (c < 232) {
			var r6 = (c - 16) / 36;
			var g6 = (c - 16) % 36 / 6;
			var b6 = (c - 16) % 6;
		
			var r = (byte) (r6 == 0 ? 0 : 95 + 40 * (r6 - 1));
			var g = (byte) (g6 == 0 ? 0 : 95 + 40 * (g6 - 1));
			var b = (byte) (b6 == 0 ? 0 : 95 + 40 * (b6 - 1));
		
			return new(r, g, b);
		}
		else {
			var v = 8 + (c - 232) * 10;
			
			var r = (byte) v;
			var g = (byte) v;
			var b = (byte) v;
			
			return new(r, g, b);
		}
	}
	
	static ((double, byte), (double, byte)) col256vals(Color c) {
		var (r, g, b) = c;
		
		List<(double, byte)> distances = [];
		
		if (r == g && g == b && r != 0) {
			var bv = (byte) Math.Clamp(r > 8 ? (r - 4) / 10 + 232 : 232, 232, 255);
			
			distances.Add((0, bv));
			for (var d = -1; d <= 1; d++) {
				var byteVal = (byte) (bv + d);
				if (byteVal is < 232 or > 255) continue;
				
				var col = col256ToRGB24(byteVal);
				if (byteVal == bv) {
					distances.Add((0, byteVal));
				}
				else {
					distances.Add((c.Distance(col, Color.Space.RGB), byteVal));
				}
			}
		}
		else {
			var (r6, g6, b6) = (chan6val(r), chan6val(g), chan6val(b));
		
			for (var dr = -1; dr <= 1; dr++) {
				for (var dg = -1; dg <= 1; dg++) {
					for (var db = -1; db <= 1; db++) {
						var (cr6, cg6, cb6) = (r6 + dr, g6 + dg, b6 + db);
						if (cr6 >= 6 || cg6 >= 6 || cb6 >= 6) continue;
						if (cr6 <  0 || cg6 <  0 || cb6 <  0) continue;
					
						var byteVal = (byte) (16 + 36 * cr6 + 6 * cg6 + cb6);
						var col     = col256ToRGB24(byteVal);
					
						if (dr == 0 && dg == 0 && db == 0) {
							distances.Add((0, byteVal));
						}
						else {
							distances.Add((c.Distance(col, Color.Space.RGB), byteVal));
						}
					}
				}
			}
		}
		
		// Grab the 2 nearest colors by distance
		var colors     = distances.OrderBy(d => d.Item1).Take(2).ToArray();
		var trueColors = colors.Select(c => col256ToRGB24(c.Item2)).ToArray();
		
		return ((c.Distance(trueColors[0], Color.Space.RGB), colors[0].Item2),
		        (c.Distance(trueColors[1], Color.Space.RGB), colors[1].Item2));
	}
	
	public string? SecondaryAnsiString { get; private set; } = null;
	public double? SecondaryBlendRatio { get; private set; } = null;
	
	public string AnsiString {
		get {
			var str = "";
			
			if (IsBold) {
				str += $"\x1B[1m";
			}
			else {
				str += $"\x1B[22m";
			}
			
			return str + FGAnsiString(omitBold: true) + BGAnsiString(omitBold: true);
		}
	}
	
	public string FGAnsiString(bool omitBold = false) {
		var fgCode = (byte) (ForegroundANSI ?? Code.Invalid);
		
		var str = "";
		
		if (!omitBold) {
			if (IsBold) {
				str += $"\x1B[1m";
			}
			else {
				str += $"\x1B[22m";
			}
		}
			
		if (fgCode < 255) {
			str += $"\x1B[{fgCode + 30}m";
		}
		else if (foregroundColor is Color c) {
			var (r, g, b) = c;
				
			if (RGB24Enabled) str += $"\x1B[38;2;{r};{g};{b}m";
			else {
				var (col1, col2) = col256vals(c);
				str += $"\x1B[38;5;{col1.Item2}m";
				SecondaryAnsiString  = $"\x1B[48;5;{col2.Item2}m";
				SecondaryBlendRatio  = col1.Item1 / (col1.Item1 + col2.Item1);
			}
		}
		
		return str;
	}
	
	public string BGAnsiString(bool omitBold = false) {
		var bgCode = (byte) (BackgroundANSI ?? Code.Invalid);
		
		var str = "";
		
		if (!omitBold) {
			if (IsBold) {
				str += $"\x1B[1m";
			}
			else {
				str += $"\x1B[22m";
			}
		}
			
		if (bgCode < 255) {
			str += $"\x1B[{bgCode + 40}m";
		}
		else if (backgroundColor is Color c) {
			var (r, g, b) = c;
				
			if (RGB24Enabled) str += $"\x1B[48;2;{r};{g};{b}m";
			else {
				var (col1, col2) = col256vals(c);
				str += $"\x1B[48;5;{col1.Item2}m";
				SecondaryAnsiString  = $"\x1B[38;5;{col2.Item2}m";
				SecondaryBlendRatio  = col1.Item1 / (col1.Item1 + col2.Item1);
			}
		}
		
		return str;
	}
	
	public bool IsRGB => foregroundColor is null or Color 
	                  && backgroundColor is null or Color;
	
	public bool IsBG => foregroundColor is     null 
	                 && backgroundColor is not null;
	
	public bool IsFG => foregroundColor is not null 
	                 && backgroundColor is     null;
	
	public bool IsBold { get; private init; } = false;
	
	public static AnsiColor Black   = new(Code.Black);
	public static AnsiColor Red     = new(Code.Red);
	public static AnsiColor Green   = new(Code.Green);
	public static AnsiColor Yellow  = new(Code.Yellow);
	public static AnsiColor Blue    = new(Code.Blue);
	public static AnsiColor Magenta = new(Code.Magenta);
	public static AnsiColor Cyan    = new(Code.Cyan);
	public static AnsiColor Grey    = new(Code.Grey);
	
	public static AnsiColor DarkGrey      = new(Code.DarkGrey);
	public static AnsiColor BrightRed     = new(Code.BrightRed);
	public static AnsiColor BrightGreen   = new(Code.BrightGreen);
	public static AnsiColor BrightYellow  = new(Code.BrightYellow);
	public static AnsiColor BrightBlue    = new(Code.BrightBlue);
	public static AnsiColor BrightMagenta = new(Code.BrightMagenta);
	public static AnsiColor BrightCyan    = new(Code.BrightCyan);
	public static AnsiColor White         = new(Code.White);
	
	public static AnsiColor BGBlack   = new(Code.Black,   isBG: true);
	public static AnsiColor BGRed     = new(Code.Red,     isBG: true);
	public static AnsiColor BGGreen   = new(Code.Green,   isBG: true);
	public static AnsiColor BGYellow  = new(Code.Yellow,  isBG: true);
	public static AnsiColor BGBlue    = new(Code.Blue,    isBG: true);
	public static AnsiColor BGMagenta = new(Code.Magenta, isBG: true);
	public static AnsiColor BGCyan    = new(Code.Cyan,    isBG: true);
	public static AnsiColor BGGrey    = new(Code.Grey,    isBG: true);
	
	public static AnsiColor BGDarkGrey      = new(Code.DarkGrey,      isBG: true);
	public static AnsiColor BGBrightRed     = new(Code.BrightRed,     isBG: true);
	public static AnsiColor BGBrightGreen   = new(Code.BrightGreen,   isBG: true);
	public static AnsiColor BGBrightYellow  = new(Code.BrightYellow,  isBG: true);
	public static AnsiColor BGBrightBlue    = new(Code.BrightBlue,    isBG: true);
	public static AnsiColor BGBrightMagenta = new(Code.BrightMagenta, isBG: true);
	public static AnsiColor BGBrightCyan    = new(Code.BrightCyan,    isBG: true);
	public static AnsiColor BGWhite         = new(Code.White,         isBG: true);
	
	public Color? ForegroundRGB {
		get => foregroundColor as Color;
		private init {
			foregroundColor = value;
		}
	}
	
	public Code? ForegroundANSI {
		get => (Code?) (foregroundColor as byte?);
		private init {
			foregroundColor = value is null ? null : (byte) value;
		}
	}
	
	public Color? BackgroundRGB {
		get => backgroundColor as Color;
		private init {
			backgroundColor = value;
		}
	}
	
	public Code? BackgroundANSI {
		get => (Code?) (backgroundColor as byte?);
		private init {
			backgroundColor = value is null ? null : (byte) value;
		}
	}
	
	public AnsiColor(Color color, bool isBG = false, bool isBold = false) {
		IsBold = isBold;
		
		if (isBG) {
			BackgroundRGB = color;
		}
		else {
			ForegroundRGB = color;
		}
	}
	
	public AnsiColor(byte red, byte green, byte blue, bool isBG = false, bool isBold = false) {
		IsBold = isBold;
		
		if (isBG) {
			BackgroundRGB = (red, green, blue);
		}
		else {
			ForegroundRGB = (red, green, blue);
		}
	}
	
	public AnsiColor(double red, double green, double blue, bool isBG = false, bool isBold = false) {
		IsBold = isBold;

		if (isBG) {
			BackgroundRGB = (red, green, blue);
		}
		else {
			ForegroundRGB = (red, green, blue);
		}
	}
	
	public AnsiColor(Code code, bool isBG = false, bool isBold = false) {
		IsBold = isBold;

		if (isBG) {
			BackgroundANSI = code;
		}
		else {
			ForegroundANSI = code;
		}
	}
	
	public AnsiColor(Color foreground, Color background, bool isBold = false) {
		IsBold = isBold;

		ForegroundRGB = foreground;
		BackgroundRGB = background;
	}
	
	public AnsiColor(Color foreground, Code background, bool isBold = false) {
		IsBold = isBold;

		ForegroundRGB  = foreground;
		BackgroundANSI = background;
	}
	
	public AnsiColor(Code foreground, Color background, bool isBold = false) {
		IsBold = isBold;

		ForegroundANSI = foreground;
		BackgroundRGB  = background;
	}
	
	public AnsiColor(Code foreground, Code background, bool isBold = false) {
		IsBold = isBold;

		ForegroundANSI = foreground;
		BackgroundANSI = background;
	}
	
	public AnsiColor(byte fgRed, byte fgGreen, byte fgBlue, byte bgRed, byte bgGreen, byte bgBlue, bool isBold = false) {
		IsBold = isBold;

		ForegroundRGB = (fgRed, fgGreen, fgBlue);
		BackgroundRGB = (bgRed, bgGreen, bgBlue);
	}
	
	public AnsiColor(double fgRed, double fgGreen, double fgBlue, double bgRed, double bgGreen, double bgBlue, bool isBold = false) {
		IsBold = isBold;

		ForegroundRGB = (fgRed, fgGreen, fgBlue);
		BackgroundRGB = (bgRed, bgGreen, bgBlue);
	}
	
	public AnsiColor(byte fgRed, byte fgGreen, byte fgBlue, Code background, bool isBold = false) {
		IsBold = isBold;

		ForegroundRGB  = (fgRed, fgGreen, fgBlue);
		BackgroundANSI = background;
	}
	
	public AnsiColor(double fgRed, double fgGreen, double fgBlue, Code background, bool isBold = false) {
		IsBold = isBold;

		ForegroundRGB  = (fgRed, fgGreen, fgBlue);
		BackgroundANSI = background;
	}
	
	public AnsiColor(Code foreground, byte bgRed, byte bgGreen, byte bgBlue, bool isBold = false) {
		IsBold = isBold;

		ForegroundANSI = foreground;
		BackgroundRGB  = (bgRed, bgGreen, bgBlue);
	}
	
	public AnsiColor(Code foreground, double bgRed, double bgGreen, double bgBlue, bool isBold = false) {
		IsBold = isBold;

		ForegroundANSI = foreground;
		BackgroundRGB  = (bgRed, bgGreen, bgBlue);
	}
	
	public static bool operator == (AnsiColor? lhs, AnsiColor? rhs) {
		if (lhs is null && rhs is null) {
			return true;
		}
		
		if (lhs is null || rhs is null) {
			return false;
		}
		
		if ((lhs.foregroundColor is null) != (rhs.foregroundColor is null)) {
			return false;
		}
		
		if ((lhs.backgroundColor is null) != (rhs.backgroundColor is null)) {
			return false;
		}
		
		var fgMatch = false;
		var bgMatch = false;
		
		if (lhs.foregroundColor is null && rhs.foregroundColor is null) {
			fgMatch = true;
		}
		else if (lhs.foregroundColor is byte codef1 && rhs.foregroundColor is byte codef2) {
			fgMatch = codef1 == codef2;
		}
		else if (lhs.foregroundColor is Color colorf1 && rhs.foregroundColor is Color colorf2) {
			fgMatch = colorf1 == colorf2;
		}
		
		if (lhs.backgroundColor is null && rhs.backgroundColor is null) {
			bgMatch = true;
		}
		else if (lhs.backgroundColor is byte codeb1 && rhs.backgroundColor is byte codeb2) {
			bgMatch = codeb1 == codeb2;
		}
		else if (lhs.backgroundColor is Color colorb1 && rhs.backgroundColor is Color colorb2) {
			bgMatch = colorb1 == colorb2;
		}
		
		return fgMatch && bgMatch && lhs.IsBold == rhs.IsBold;
	}
	
	public static bool operator != (AnsiColor? lhs, AnsiColor? rhs) => !(lhs == rhs);
}