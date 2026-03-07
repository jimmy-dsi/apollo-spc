namespace SpcProgram;

using System.Text;

using Jimbl;
using Jimbl.Graphics;

public static class Display {
	// For scrollable display region
	static List<      char[]> charBuffer;
	static List<AnsiColor?[]> colorBuffer;
	
	static bool windowEnabled = false;
	
	static int windowTL = 0;
	static int windowTR = 0;
	static int windowBL = 0;
	static int windowBR = 0;
	
	static int scrollY = 0;
	
	// For static display
	static       char[][]  charGrid;
	static AnsiColor?[][] colorGrid;
	
	// Previous static display color buffers
	static       char[][][]  prevCharGrids;
	static AnsiColor?[][][] prevColorGrids;
	
	static int x = 0;
	static int y = 0;
	static AnsiColor? color = null;
	
	static long frame     = 0;
	static long prevFrame = 0;
	
	public static int Width  { get; private set; }
	public static int Height { get; private set; }
	
	public static AnsiColor? Color {
		get => color;
		set => color = value;
	}
	
	public static int X {
		get => x;
		set => x = value;
	}
	
	public static int Y {
		get => y;
		set => y = value;
	}
	
	public static void Init(int width, int height) {
		Width  = width;
		Height = height;
		
		charGrid  = new       char[height][];
		colorGrid = new AnsiColor?[height][];
		
		List<AnsiColor?[][]>  pcg = new();
		List<      char[][]> pchg = new();
		
		for (var i = 0; i < 4; i++) {
			pcg .Add(new AnsiColor?[][]{ });
			pchg.Add(new       char[][]{ });
		}
		
		prevColorGrids = pcg .ToArray();
		prevCharGrids  = pchg.ToArray();
			
		for (var i = 0; i < 4; i++) {
			prevColorGrids[i] = new AnsiColor?[height][];
			prevCharGrids [i] = new       char[height][];
		}
		
		for (var y = 0; y < height; y++) {
			charGrid[y]  = new       char[width];
			colorGrid[y] = new AnsiColor?[width];
			
			for (var i = 0; i < 4; i++) {
				prevColorGrids[i][y] = new AnsiColor?[width];
				prevCharGrids [i][y] = new       char[width];
			}
		}
		
		Clear();
	}
	
	public static void Clear(AnsiColor? col = null) {
		for (var y = 0; y < Height; y++) {
			for (var x = 0; x < Width; x++) {
				charGrid[y][x]  = ' ';
				colorGrid[y][x] = col;
			}
		}
		
		x = 0;
		y = 0;
	}
	
	public static void Write(string text, int? x_ = null, int? y_ = null, AnsiColor? col = null, bool writeThruToScrollBuf = false) {
		if (x_ != null) x = x_.Value;
		if (y_ != null) y = y_.Value;
		
		foreach (var r in text.EnumerateRunes()) {
			var c = (char) r.Value;
			var print = true;
			
			if (r.Value is >= 0xD800 and <= 0xDFFF or > 0xFFFF) {
				c = '?';
			}
			else if (c < 0x20) {
				print = false;
				
				if (c == '\r') {
					x = 0;
				}
				else if (c == '\n') {
					x = 0;
					y++;
				}
				else if (c == '\t') {
					var rem = 4 - x % 4;
					
					for (var i = 0; i < rem; i++) {
						writeChar(' ', x, y, col ?? color);
						x++;
					}
				}
			}
			
			if (print) {
				writeChar(c, x, y, col ?? color);
				x++;
			}
		}
	}
	
	public static void WriteBox(string[] lines, int? x_ = null, int? y_ = null, AnsiColor? col = null, bool writeThruToScrollBuf = false) {
		var initX = x_ ?? x;
		var initY = y_ ?? y;
		
		var maxLength = lines.Max(line => line.Length);
		ClearBox(maxLength, lines.Length, initX, initY, col, writeThruToScrollBuf);
		
		x = initX;
		y = initY;
		
		foreach (var line in lines) {
			Write(line, x, y, col, writeThruToScrollBuf);
			x = initX;
			y++;
		}
	}
	
	public static void ClearLine(int? y_ = null, AnsiColor? col = null, bool writeThruToScrollBuf = false) {
		if (y_ != null) y = y_.Value;
		var initY = y;
		
		Write(new(' ', Width), 0, y_, col ?? color, writeThruToScrollBuf);
		x = 0;
		y = initY + 1;
	}
	
	public static void ClearBox(int width, int height, int? x_ = null, int? y_ = null, AnsiColor? col = null, bool writeThruToScrollBuf = false) {
		var initX = x_ ?? x;
		var initY = y_ ?? y;
		
		for (var yy = 0; yy < height; yy++) {
			Write(new(' ', width), x_, initY + yy, col ?? color, writeThruToScrollBuf);
			x = initX;
		}
	}
	
	public static void DrawOutline(int x, int y, int width, int height, AnsiColor? col = null, bool removeSides = false) {
		var left   = x;
		var right  = x + width - 1;
		var top    = y;
		var bottom = y + height - 1;
			
		for (var xx = left; xx <= right; xx++) {
			writeChar('-', xx, top,    col ?? color);
			writeChar('-', xx, bottom, col ?? color);
		}
		
		if (!removeSides) {
			writeChar('+', left,  top,    col ?? color);
			writeChar('+', right, top,    col ?? color);
			writeChar('+', left,  bottom, col ?? color);
			writeChar('+', right, bottom, col ?? color);
			
			for (var yy = top + 1; yy < bottom; yy++) {
				writeChar('|', left,  yy, col ?? color);
				writeChar('|', right, yy, col ?? color);
			}
		}
	}
	
	public static string Flush() {
		prevFrame = frame;
		frame = Driver.Frame;
		
		var framesSinceLastDisplay = Math.Max(1, frame - prevFrame);
		
		StringBuilder sb = new("\x1B[H");
		
		sb.Append("\x1B[0m");
		if (colorGrid[0][0] != null) {
			sb.Append(colorGrid[0][0]!.AnsiString);
		}
		
		AnsiColor? prevColor = null;
				
		// Update positions
		var offset0 = CliMain.StartAddr / 0x10 - CliMain.PrevStartAddr1 / 0x10;
		var offset1 = CliMain.StartAddr / 0x10 - CliMain.PrevStartAddr2 / 0x10;
		var offset2 = CliMain.StartAddr / 0x10 - CliMain.PrevStartAddr3 / 0x10;
		var offset3 = CliMain.StartAddr / 0x10 - CliMain.PrevStartAddr4 / 0x10;
		
		// Update color and char grids
		for (var i = 0; i < framesSinceLastDisplay; i++) {
			for (var y = 0; y < Height; y++) {
				for (var x = 0; x < Width; x++) {
					var cl = colorGrid[y][x];
					var ch =  charGrid[y][x];
				
					// Update color grids
					prevColorGrids[3][y][x] = prevColorGrids[2][y][x];
					prevColorGrids[2][y][x] = prevColorGrids[1][y][x];
					prevColorGrids[1][y][x] = prevColorGrids[0][y][x];
					prevColorGrids[0][y][x] = cl;
				
					// Update char grids
					prevCharGrids[3][y][x] = prevCharGrids[2][y][x];
					prevCharGrids[2][y][x] = prevCharGrids[1][y][x];
					prevCharGrids[1][y][x] = prevCharGrids[0][y][x];
					prevCharGrids[0][y][x] = ch;
				}
			}
		}
		
		for (var y = 0; y < Height; y++) {
			for (var x = 0; x < Width; x++) {
				var ch =  charGrid[y][x];
				var cl = colorGrid[y][x];
				
				var isMulti = cl is not null
				           && cl.BackgroundRGB is not null && cl.ForegroundRGB is not null
				           && ch is ' ' or '▄' or '▀' or '█';
				
				var isMultiBlended = false;
				
				if (cl is not null && cl.IsBG || isMulti) {
					var (y0, y1, y2, y3) = (y + offset0, y + offset1, y + offset2, y + offset3);
					
					if (x < Width - 32 && y0 >= 0 && y0 < Height && y1 >= 0 && y1 < Height && y2 >= 0 && y2 < Height && y3 >= 0 && y3 < Height) {
						if (isMulti) {
							isMultiBlended = true;
							
							cl = blendDualColors(
								(cl!, ch),
								(prevColorGrids[0][y0][x], prevCharGrids[0][y0][x]),
								(prevColorGrids[1][y1][x], prevCharGrids[1][y1][x]),
								(prevColorGrids[2][y2][x], prevCharGrids[2][y2][x]),
								(prevColorGrids[3][y3][x], prevCharGrids[3][y3][x])
							);
						}
						else {
							cl = blendColors(
								cl,
								prevColorGrids[0][y0][x],
								prevColorGrids[1][y1][x],
								prevColorGrids[2][y2][x],
								prevColorGrids[3][y3][x]
							);
						}
					}
					else {
						if (isMulti) {
							isMultiBlended = true;
							
							cl = blendDualColors(
								(cl!, ch),
								(prevColorGrids[0][y][x], prevCharGrids[0][y][x]),
								(prevColorGrids[1][y][x], prevCharGrids[1][y][x]),
								(prevColorGrids[2][y][x], prevCharGrids[2][y][x]),
								(prevColorGrids[3][y][x], prevCharGrids[3][y][x])
							);
						}
						else {
							cl = blendColors(
								cl,
								prevColorGrids[0][y][x],
								prevColorGrids[1][y][x],
								prevColorGrids[2][y][x],
								prevColorGrids[3][y][x]
							);
						}
					}
				}
				
				if (cl != prevColor) {
					if (prevColor is not null) {
						sb.Append("\x1B[0m");
					}
					if (cl is not null) {
						sb.Append(cl.AnsiString);
					}
					prevColor = cl;
				}
				
				sb.Append(isMultiBlended ? '▀' : ch);
			}
			
			if (y < Height - 1) {
				sb.Append('\n');
			}
		}
		
		// Reset draw position
		X = 0;
		Y = 0;
		
		sb.Append("\x1B[0m");
		return sb.ToString();
	}
	
	public static string[] WordWrap(string text, int maxWidth, int maxLines) {
		List<string> lines = [];
		text = text.Prepare();
		
		var inputLines = text.Split('\n');
		
		foreach (var (L, inLine) in inputLines.Enum()) {
			var words = L > 0 ? inLine.Trim().Split(' ') : inLine.TrimEnd().Split(' ');
		
			StringBuilder sb = new();
			var curLen = 0;
		
			for (var i = 0; i < words.Length; i++) {
				var word = words[i];
				
				if (curLen + word.Length <= maxWidth) {
					sb.Append(word);
					curLen += word.Length;
				
					if (curLen == maxWidth) {
						lines.Add(sb.ToString());
						sb.Clear();
						curLen = 0;
					}
					else {
						curLen += 1;
						sb.Append(' ');
					}
				}
				else if (curLen == 0) { // Split the word across multiple lines if the word itself is longer than the max width
					var rem   = word.Length;
					var start = 0;
					
					while (rem > maxWidth) {
						sb.Append(word[start .. (start + maxWidth - 1)]).Append('-');
						lines.Add(sb.ToString());
						sb.Clear();
						
						rem   -= maxWidth - 1;
						start += maxWidth - 1;
					}
					
					sb.Append(word[start..]);
					curLen = word.Length - start;
				}
				else {
					lines.Add(sb.ToString());
					sb.Clear();
					curLen = 0;
					i--; // Try same word again on next line
				}
			}
			
			if (sb.Length > 0) {
				lines.Add(sb.ToString());
			}
		}
		
		var origLineCount = lines.Count;
		if (lines.Count > maxLines) {
			lines = lines[..maxLines];
		}
		
		if (origLineCount > maxLines && lines.Count == maxLines) {
			var last  = lines[^1];
			var words = last.Split(' ').ToList();
			
			var realFirstWord = last.Trim().Split(' ').FirstOrDefault("");
			var leadingSpaces = 0;
			
			foreach (var (i, c) in last.Enum()) {
				if (c != ' ') {
					leadingSpaces = i;
					break;
				}
			}
			
			while (words.Count > 0 && words[^1].Length == 0) {
				words.RemoveAt(words.Count - 1);
			}
			
			if (words.Count > 0) {
				last = string.Join(' ', words);
				
				while (last.Length > maxWidth - 3) {
					words.RemoveAt(words.Count - 1);
			
					while (words.Count > 0 && words[^1].Length == 0) {
						words.RemoveAt(words.Count - 1);
					}
					
					if (words.Count == 0) {
						break;
					}
				}
				
				if (words.Count > 0) {
					lines[^1] = last + "...";
				}
				else if (realFirstWord.Length > 0) {
					var w = new string(' ', leadingSpaces) + realFirstWord;
					lines[^1] = w[..(maxWidth - 4)] + "-...";
				}
				else {
					lines[^1] = "";
				}
			}
		}
		
		return lines.ToArray();
	}
	
	public static string Prepare(this string self) {
		StringBuilder sb = new();
		var col = 0;
		
		foreach (var r in self.EnumerateRunes()) {
			var c = (char) r.Value;
			
			if (r.Value is >= 0xD800 and <= 0xDFFF or > 0xFFFF) {
				c = '?';
				sb.Append(c);
				col++;
			}
			else if (c < 0x20) {
				if (c == '\t') {
					var rem = 4 - col % 4;
					sb.Append(new string(' ', rem));
					col += rem;
				}
				else if (c == '\n') {
					sb.Append(c);
					col = 0;
				}
			}
			else {
				sb.Append(c);
				col++;
			}
		}
		
		return sb.ToString();
	}
	
	static void writeChar(char c, int x, int y, AnsiColor? color) {
		if (x >= Width || y >= Height) return;
		
		charGrid[y][x]  = c;
		colorGrid[y][x] = color;
	}
	
	static double[] blendFilter = [
		0.10,
		0.25,
		0.30,
		0.25,
		0.10,
	];
	
	static AnsiColor? blendColors(params AnsiColor?[] colors) {
		if (colors.Length == 0) {
			return null;
		}
		
		var cc1 = colors[0];
		
		if (cc1?.BackgroundRGB is null && cc1?.ForegroundRGB is null) {
			return cc1;
		}
		
		List<Color> prevColors = [];
		
		foreach (var col in colors) {
			if (col?.BackgroundRGB is Color c) {
				prevColors.Add(c);
			}
			else {
				break;
			}
		}
		
		var blended = prevColors[0].Filter(prevColors[1..], blendFilter, Jimbl.Graphics.Color.Space.RGB);
		return new(blended, isBG: true);
	}
	
	static AnsiColor? blendDualColors(params (AnsiColor? Color, char Char)[] colors) {
		if (colors.Length == 0) {
			return null;
		}
		
		var cc1 = colors[0];
		
		if (cc1.Color?.BackgroundRGB is null && cc1.Color?.ForegroundRGB is null) {
			return cc1.Color;
		}
		
		List<Color> topPrevColors    = [];
		List<Color> bottomPrevColors = [];
		
		// Top blending
		foreach (var col in colors) {
			if (col.Char is ' ' or '▄' && col.Color?.BackgroundRGB is Color bc) {
				topPrevColors.Add(bc);
			}
			else if (col.Char is '█' or '▀' && col.Color?.ForegroundRGB is Color fc) {
				topPrevColors.Add(fc);
			}
			else {
				break;
			}
		}
		
		// Bottom blending
		foreach (var col in colors) {
			if (col.Char is '█' or '▄' && col.Color?.ForegroundRGB is Color fc) {
				bottomPrevColors.Add(fc);
			}
			else if (col.Char is ' ' or '▀' && col.Color?.BackgroundRGB is Color bc) {
				bottomPrevColors.Add(bc);
			}
			else {
				break;
			}
		}
		
		var topBlended    =    topPrevColors[0].Filter(   topPrevColors[1..], blendFilter, Jimbl.Graphics.Color.Space.RGB);
		var bottomBlended = bottomPrevColors[0].Filter(bottomPrevColors[1..], blendFilter, Jimbl.Graphics.Color.Space.RGB);
		
		return new(topBlended, bottomBlended); // Convention: Top is FG, bottom is BG. Caller will force char to '▀'
	}
}