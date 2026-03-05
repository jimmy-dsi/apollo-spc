namespace Jimbl.Graphics;

public class AnsiColor {
	public enum Code {
		Black    =  0,       Red,       Green,       Yellow,       Blue,       Magenta,       Cyan,  Grey,
		DarkGrey = 60, BrightRed, BrightGreen, BrightYellow, BrightBlue, BrightMagenta, BrightCyan, White,
		Invalid  = 255
	}
	
	object? foregroundColor;
	object? backgroundColor;
	
	public string AnsiString {
		get {
			var fgCode = (byte) (ForegroundANSI ?? Code.Invalid);
			var bgCode = (byte) (BackgroundANSI ?? Code.Invalid);
			
			var str = "";
			
			if (fgCode < 255) {
				str += $"\x1B[{fgCode + 30}m";
			}
			else if (foregroundColor is Color c) {
				var (r, g, b) = c;
				str += $"\x1B[38;2;{r};{g};{b}m";
			}
			
			if (bgCode < 255) {
				str += $"\x1B[{bgCode + 40}m";
			}
			else if (backgroundColor is Color c) {
				var (r, g, b) = c;
				str += $"\x1B[48;2;{r};{g};{b}m";
			}
			
			return str;
		}
	}
	
	public bool IsRGB => foregroundColor is null or Color 
	                  && backgroundColor is null or Color;
	
	public bool IsBG => foregroundColor is     null 
	                 && backgroundColor is not null;
	
	public bool IsFG => foregroundColor is not null 
	                 && backgroundColor is     null;
	
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
	
	public AnsiColor(Color color, bool isBG = false) {
		if (isBG) {
			BackgroundRGB = color;
		}
		else {
			ForegroundRGB = color;
		}
	}
	
	public AnsiColor(byte red, byte green, byte blue, bool isBG = false) {
		if (isBG) {
			BackgroundRGB = (red, green, blue);
		}
		else {
			ForegroundRGB = (red, green, blue);
		}
	}
	
	public AnsiColor(double red, double green, double blue, bool isBG = false) {
		if (isBG) {
			BackgroundRGB = (red, green, blue);
		}
		else {
			ForegroundRGB = (red, green, blue);
		}
	}
	
	public AnsiColor(Code code, bool isBG = false) {
		if (isBG) {
			BackgroundANSI = code;
		}
		else {
			ForegroundANSI = code;
		}
	}
	
	public AnsiColor(Color foreground, Color background) {
		ForegroundRGB = foreground;
		BackgroundRGB = background;
	}
	
	public AnsiColor(Color foreground, Code background) {
		ForegroundRGB  = foreground;
		BackgroundANSI = background;
	}
	
	public AnsiColor(Code foreground, Color background) {
		ForegroundANSI = foreground;
		BackgroundRGB  = background;
	}
	
	public AnsiColor(Code foreground, Code background) {
		ForegroundANSI = foreground;
		BackgroundANSI = background;
	}
	
	public AnsiColor(byte fgRed, byte fgGreen, byte fgBlue, byte bgRed, byte bgGreen, byte bgBlue) {
		ForegroundRGB = (fgRed, fgGreen, fgBlue);
		BackgroundRGB = (bgRed, bgGreen, bgBlue);
	}
	
	public AnsiColor(double fgRed, double fgGreen, double fgBlue, double bgRed, double bgGreen, double bgBlue) {
		ForegroundRGB = (fgRed, fgGreen, fgBlue);
		BackgroundRGB = (bgRed, bgGreen, bgBlue);
	}
	
	public AnsiColor(byte fgRed, byte fgGreen, byte fgBlue, Code background) {
		ForegroundRGB  = (fgRed, fgGreen, fgBlue);
		BackgroundANSI = background;
	}
	
	public AnsiColor(double fgRed, double fgGreen, double fgBlue, Code background) {
		ForegroundRGB  = (fgRed, fgGreen, fgBlue);
		BackgroundANSI = background;
	}
	
	public AnsiColor(Code foreground, byte bgRed, byte bgGreen, byte bgBlue) {
		ForegroundANSI = foreground;
		BackgroundRGB  = (bgRed, bgGreen, bgBlue);
	}
	
	public AnsiColor(Code foreground, double bgRed, double bgGreen, double bgBlue) {
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
		
		return fgMatch && bgMatch;
	}
	
	public static bool operator != (AnsiColor? lhs, AnsiColor? rhs) => !(lhs == rhs);
}