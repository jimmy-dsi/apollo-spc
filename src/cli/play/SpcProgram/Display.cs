namespace SpcProgram;

using System.Text;

using Jimbl;

public static class Display {
	// For scrollable display region
	static List<List<char>>    charBuffer;
	static List<List<Color?>> colorBuffer;
	
	// For static display
	static   char[][]  charGrid;
	static Color?[][] colorGrid;
	
	// Previous static display color buffers
	static Color?[][][] prevColorGrids;
	
	static int x = 0;
	static int y = 0;
	static Color? color = null;
	
	public static int Width  { get; private set; }
	public static int Height { get; private set; }
	
	public static Color? Color {
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
		
		charGrid  = new   char[height][];
		colorGrid = new Color?[height][];
		
		List<Color?[][]> pcg = new();
		
		for (var i = 0; i < 4; i++) {
			pcg.Add(new Color?[][]{ });
		}
		
		prevColorGrids = pcg.ToArray();
			
		for (var i = 0; i < 4; i++) {
			prevColorGrids[i] = new Color?[height][];
		}
		
		for (var y = 0; y < height; y++) {
			charGrid[y]  = new   char[width];
			colorGrid[y] = new Color?[width];
			
			for (var i = 0; i < 4; i++) {
				prevColorGrids[i][y] = new Color?[width];
			}
		}
		
		Clear();
	}
	
	public static void Clear(Color? col = null) {
		for (var y = 0; y < Height; y++) {
			for (var x = 0; x < Width; x++) {
				charGrid[y][x]  = ' ';
				colorGrid[y][x] = col;
			}
		}
		
		x = 0;
		y = 0;
	}
	
	public static void Write(string text, int? x_ = null, int? y_ = null, Color? col = null, bool writeThruToScrollBuf = false) {
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
	
	public static void WriteBox(string[] lines, int? x_ = null, int? y_ = null, Color? col = null, bool writeThruToScrollBuf = false) {
		var initX = x_ ?? x;
		var lastY = y_ ?? y;
		
		var maxLength = lines.Max(line => line.Length);
		ClearBox(maxLength, lines.Length, x, y, col, writeThruToScrollBuf);
		
		x = initX;
		y = lastY;
		
		foreach (var line in lines) {
			Write(line, x, y, col, writeThruToScrollBuf);
			x = initX;
			lastY++;
			y = lastY;
		}
	}
	
	public static void ClearLine(int? y_ = null, Color? col = null, bool writeThruToScrollBuf = false) {
		if (y_ != null) y = y_.Value;
		var initY = y;
		
		Write(new(' ', Width), 0, y_, col ?? color, writeThruToScrollBuf);
		x = 0;
		y = initY + 1;
	}
	
	public static void ClearBox(int width, int height, int? x_ = null, int? y_ = null, Color? col = null, bool writeThruToScrollBuf = false) {
		var initX = x_ ?? x;
		
		for (var yy = 0; yy < height; yy++) {
			var lastY = x_ ?? y;
			Write(new(' ', width), x_, y_, col ?? color, writeThruToScrollBuf);
			x = initX;
			y = lastY + 1;
		}
	}
	
	public static void DrawOutline(int x, int y, int width, int height, Color? col = null, bool removeSides = false) {
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
		StringBuilder sb = new("\x1B[H");
		
		sb.Append("\x1B[0m");
		if (colorGrid[0][0] != null) {
			sb.Append(colorGrid[0][0]!.AnsiString);
		}
		
		Color? prevColor = null;
		
		for (var y = 0; y < Height; y++) {
			for (var x = 0; x < Width; x++) {
				var ch =  charGrid[y][x];
				var cl = colorGrid[y][x];
				
				prevColorGrids[3][y][x] = prevColorGrids[2][y][x];
				prevColorGrids[2][y][x] = prevColorGrids[1][y][x];
				prevColorGrids[1][y][x] = prevColorGrids[0][y][x];
				prevColorGrids[0][y][x] = cl;
				
				if (cl is not null) {
					cl = blendColors(cl, prevColorGrids[0][y][x], prevColorGrids[1][y][x], prevColorGrids[2][y][x], prevColorGrids[3][y][x]);
				}
				
				if (cl != prevColor) {
					if (prevColor != null) {
						sb.Append("\x1B[0m");
					}
					if (cl is not null) {
						sb.Append(cl.AnsiString);
					}
					prevColor = cl;
				}
				
				sb.Append(ch);
			}
			
			if (y < Height - 1) {
				sb.Append('\n');
			}
		}
		
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
	
	static void writeChar(char c, int x, int y, Color? color) {
		if (x >= Width || y >= Height) return;
		
		charGrid[y][x]  = c;
		colorGrid[y][x] = color;
	}
	
	static double[] blendFilter = [
		-0.075,
		 0.250,
		 0.650,
		 0.250,
		-0.075,
	];
	
	const double Gamma = 2.2;
	
	static Color? blendColors(Color c1, Color? c2, Color? c3, Color? c4, Color? c5) {
		if (!c1.IsRGB || !c1.IsBG) {
			return c1;
		}
		
		var (red, green, blue) = (0.0, 0.0, 0.0);
		
		red   += Math.Pow(c1.Red   / 255.0, 1 / Gamma) * blendFilter[0];
		green += Math.Pow(c1.Green / 255.0, 1 / Gamma) * blendFilter[0];
		blue  += Math.Pow(c1.Blue  / 255.0, 1 / Gamma) * blendFilter[0];
		
		if (c2 is null || !c2.IsRGB || !c2.IsBG) {
			return new(Math.Pow(red, 2.2), Math.Pow(green, 2.2), Math.Pow(blue, 2.2), bg: true);
		}
		
		red   += Math.Pow(c2.Red   / 255.0, 1 / Gamma) * blendFilter[1];
		green += Math.Pow(c2.Green / 255.0, 1 / Gamma) * blendFilter[1];
		blue  += Math.Pow(c2.Blue  / 255.0, 1 / Gamma) * blendFilter[1];
		
		if (c3 is null || !c3.IsRGB || !c3.IsBG) {
			return new(Math.Pow(red, 2.2), Math.Pow(green, 2.2), Math.Pow(blue, 2.2), bg: true);
		}
		
		red   += Math.Pow(c3.Red   / 255.0, 1 / Gamma) * blendFilter[2];
		green += Math.Pow(c3.Green / 255.0, 1 / Gamma) * blendFilter[2];
		blue  += Math.Pow(c3.Blue  / 255.0, 1 / Gamma) * blendFilter[2];
		
		if (c4 is null || !c4.IsRGB || !c4.IsBG) {
			return new(Math.Pow(red, 2.2), Math.Pow(green, 2.2), Math.Pow(blue, 2.2), bg: true);
		}
		
		red   += Math.Pow(c4.Red   / 255.0, 1 / Gamma) * blendFilter[3];
		green += Math.Pow(c4.Green / 255.0, 1 / Gamma) * blendFilter[3];
		blue  += Math.Pow(c4.Blue  / 255.0, 1 / Gamma) * blendFilter[3];
		
		if (c5 is null || !c5.IsRGB || !c5.IsBG) {
			return new(Math.Pow(red, 2.2), Math.Pow(green, 2.2), Math.Pow(blue, 2.2), bg: true);
		}
		
		red   += Math.Pow(c5.Red   / 255.0, 1 / Gamma) * blendFilter[4];
		green += Math.Pow(c5.Green / 255.0, 1 / Gamma) * blendFilter[4];
		blue  += Math.Pow(c5.Blue  / 255.0, 1 / Gamma) * blendFilter[4];
		
		return new(Math.Pow(red, 2.2), Math.Pow(green, 2.2), Math.Pow(blue, 2.2), bg: true);
	}
}