namespace SpcProgram;

using System.Text;

using Jimbl;
using Jimbl.JMath;
using Jimbl.Graphics;

public static class Display {
	public class BufferTuple {
		public required List<char[]>       CharBuffer  { get; set; }
		public required List<AnsiColor?[]> ColorBuffer { get; set; }
		public required int                VirtualSize { get; set; }
		
		public static implicit operator BufferTuple ((List<char[]>, List<AnsiColor?[]>, int) tup) =>
			new() {
				CharBuffer  = tup.Item1,
				ColorBuffer = tup.Item2,
				VirtualSize = tup.Item3
			};
	}
	
	public const int MaxBufferRows = 16384;
	
	// Swappable display region buffers
	static Dictionary<string, BufferTuple> namedBuffers = new() {
		["default"] = (new(), new(), 0)
	};
	
	// For scrollable display region
	static string currentBufferId = "default";
	
	static List<      char[]>  charBuffer = namedBuffers[currentBufferId].CharBuffer;
	static List<AnsiColor?[]> colorBuffer = namedBuffers[currentBufferId].ColorBuffer;
	
	public static List<      char[]>  CharBuffer =>  charBuffer;
	public static List<AnsiColor?[]> ColorBuffer => colorBuffer;
	
	public static bool UseBufferBlending { get; set; } = true;
	
	// Previous scrollable display color buffers
	static List<      char[]>[] prevCharBuffers  = new List<      char[]>[]{};
	static List<AnsiColor?[]>[] prevColorBuffers = new List<AnsiColor?[]>[]{};
	
	static bool windowEnabled = false;
	
	static int windowLeft   = 0;
	static int windowTop    = 0;
	static int windowWidth  = 0;
	static int windowHeight = 0;
	
	static bool cutoutEnabled = false;
	
	static int cutoutLeft   = 0;
	static int cutoutTop    = 0;
	static int cutoutWidth  = 0;
	static int cutoutHeight = 0;
	
	static int  scrollTop = 0;
	static bool scrollBufPrevSource = false;
	
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

	public static int ScrollTop {
		get => scrollTop;
		set => scrollTop = value;
	}

	public static JVector2I WindowTopLeft     => (windowLeft,               windowTop               );
	public static JVector2I WindowTopRight    => (windowLeft + windowWidth, windowTop               );
	public static JVector2I WindowBottomLeft  => (windowLeft,               windowTop + windowHeight);
	public static JVector2I WindowBottomRight => (windowLeft + windowWidth, windowTop + windowHeight);

	public static JVector2I CutoutTopLeft     => (cutoutLeft,               cutoutTop               );
	public static JVector2I CutoutTopRight    => (cutoutLeft + cutoutWidth, cutoutTop               );
	public static JVector2I CutoutBottomLeft  => (cutoutLeft,               cutoutTop + cutoutHeight);
	public static JVector2I CutoutBottomRight => (cutoutLeft + cutoutWidth, cutoutTop + cutoutHeight);
	
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
	
	public static int VirtualSize { get; private set; } = 0;
	
	public static bool NoResetBuffer { get; set; } = false;

	public static string CurrentBufferId {
		get => currentBufferId;
		set {
			var prevBufferId = currentBufferId;
			currentBufferId = value;
			
			if (prevBufferId == currentBufferId) {
				return;
			}
			
			if (!namedBuffers.ContainsKey(currentBufferId)) {
				namedBuffers[currentBufferId] = (new(), new(), 0);
			}
			
			charBuffer  = namedBuffers[currentBufferId].CharBuffer;
			colorBuffer = namedBuffers[currentBufferId].ColorBuffer;
			VirtualSize = namedBuffers[currentBufferId].VirtualSize;
			
			if (!NoResetBuffer) {
				initBuffers();
				ResetPrevBuffers();
			}
		}
	}
	
	public static ItemGetter<string, BufferTuple> Buffer = new() {
		Get = k => namedBuffers[k]
	};

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
		
		initBuffers();
		Clear();
	}
	
	static void initBuffers() {
		if (prevCharBuffers.Length > 0) {
			return;
		}
		
		List<List<AnsiColor?[]>>  pcb = new();
		List<List<      char[]>> pchb = new();
		
		for (var i = 0; i < 4; i++) {
			pcb .Add(new List<AnsiColor?[]>());
			pchb.Add(new List<      char[]>());
		}
		
		prevColorBuffers = pcb .ToArray();
		prevCharBuffers  = pchb.ToArray();
	}
	
	public static void EnableCutout(int portX, int portY, int portWidth, int portHeight) {
		cutoutEnabled = true;
		
		cutoutLeft = portX;
		cutoutTop  = portY;
		
		cutoutWidth  = portWidth;
		cutoutHeight = portHeight;
	}
	
	public static void DisableCutout() {
		cutoutEnabled = false;
	}
	
	public static void SetWindowProps(int portX, int portY, int portWidth, int portHeight) {
		windowLeft = portX;
		windowTop  = portY;
		
		windowWidth  = portWidth;
		windowHeight = portHeight;
	}
	
	public static void ResetPrevBuffers() {
		var bufHeight = charBuffer.Count;
		var bufWidth  = charBuffer.Count > 0 ? charBuffer[^1].Length : 0;
		
		for (var i = 0; i < prevCharBuffers.Length; i++) {
			prevCharBuffers [i].Clear();
			prevColorBuffers[i].Clear();
		
			for (var _ = 0; _ < bufHeight; _++) {
				prevCharBuffers [i].Add(new       char[bufWidth]);
				prevColorBuffers[i].Add(new AnsiColor?[bufWidth]);
			
				for (var x = 0; x < bufWidth; x++) {
					prevCharBuffers [i][^1][x] = ' ';
					prevColorBuffers[i][^1][x] = null;
				}
			}
		}
		
		windowEnabled = true;
	}
	
	public static void AddBufferRow() {
		VirtualSize++;
		namedBuffers[currentBufferId].VirtualSize = VirtualSize;
		
		if (CharBuffer.Count < MaxBufferRows) {
			CharBuffer .Add(new       char[windowWidth]);
			ColorBuffer.Add(new AnsiColor?[windowWidth]);
		
			for (var i = 0; i < windowWidth; i++) {
				CharBuffer [(CharBuffer .Count - 1) % MaxBufferRows][i] = ' ';
				ColorBuffer[(ColorBuffer.Count - 1) % MaxBufferRows][i] = null;
			}
		}
	}
	
	public static void SyncVirtualSize() {
		VirtualSize = namedBuffers[currentBufferId].CharBuffer.Count;
		namedBuffers[currentBufferId].VirtualSize = VirtualSize;
	}
	
	public static void ResetWindowBuffer(int bufWidth, int bufHeight, int portX, int portY, int portWidth, int portHeight) {
		bufHeight %= MaxBufferRows;
		VirtualSize = bufHeight;
		namedBuffers[currentBufferId].VirtualSize = VirtualSize;
		
		charBuffer .Clear();
		colorBuffer.Clear();
		
		for (var i = 0; i < prevCharBuffers.Length; i++) {
			prevCharBuffers [i].Clear();
			prevColorBuffers[i].Clear();
		
			for (var _ = 0; _ < bufHeight; _++) {
				prevCharBuffers [i].Add(new       char[bufWidth]);
				prevColorBuffers[i].Add(new AnsiColor?[bufWidth]);
			
				for (var x = 0; x < bufWidth; x++) {
					prevCharBuffers [i][^1][x] = ' ';
					prevColorBuffers[i][^1][x] = null;
				}
			}
		}
		
		for (var _ = 0; _ < bufHeight; _++) {
			charBuffer .Add(new       char[bufWidth]);
			colorBuffer.Add(new AnsiColor?[bufWidth]);
			
			for (var x = 0; x < bufWidth; x++) {
				charBuffer [^1][x] = ' ';
				colorBuffer[^1][x] = null;
			}
		}
		
		windowLeft = portX;
		windowTop  = portY;
		
		windowWidth  = portWidth;
		windowHeight = portHeight;
		
		windowEnabled = true;
		scrollTop = 0;
	}
	
	public static void UpdateState(bool writeToScrollBuf) {
		updateState(writeToScrollBuf);
	}
	
	public static void HideWindow() {
		windowEnabled = false;
	}
	
	public static void EnableWindow() {
		windowEnabled = true;
	}
	
	public static bool IsInWindow(int x, int y) {
		return x >= windowLeft && x < windowLeft + windowWidth
		    && y >= windowTop  && y < windowTop  + windowHeight;
	}
	
	public static bool IsInCutout(int x, int y) {
		return x >= cutoutLeft && x < cutoutLeft + cutoutWidth
		    && y >= cutoutTop  && y < cutoutTop  + cutoutHeight;
	}
	
	public static JVector2I WindowCoords(int x, int y) {
		JVector2I coords = (x - windowLeft, y - windowTop + scrollTop);
		coords.Y %= MaxBufferRows;
		return coords;
	}
	
	public static char CharAt(int x, int y) {
		if ((!cutoutEnabled || !IsInCutout(x, y)) && windowEnabled && IsInWindow(x, y)) {
			var wcoords = WindowCoords(x, y);
			
			if (wcoords.Y >= charBuffer.Count) {
				return ' ';
			}
			else if (wcoords.X >= charBuffer[wcoords.Y].Length) {
				return ' ';
			}
			else {
				return charBuffer[wcoords.Y][wcoords.X];
			}
		}
		else {
			return charGrid[y][x];
		}
	}
	
	public static AnsiColor? ColorAt(int x, int y) {
		if ((!cutoutEnabled || !IsInCutout(x, y)) && windowEnabled && IsInWindow(x, y)) {
			var wcoords = WindowCoords(x, y);
			
			if (wcoords.Y >= charBuffer.Count) {
				return null;
			}
			else if (wcoords.X >= charBuffer[wcoords.Y].Length) {
				return null;
			}
			else {
				return colorBuffer[wcoords.Y][wcoords.X];
			}
		}
		else {
			return colorGrid[y][x];
		}
	}
	
	public static void SetBufferCharAt(int x, int y, char ch, AnsiColor? col) {
		y %= MaxBufferRows;
		
		if (y < charBuffer.Count && x < charBuffer[y].Length) {
			charBuffer [y][x] = ch;
			colorBuffer[y][x] = col;
		}
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
	
	public static void Write(string text, int? x_ = null, int? y_ = null, AnsiColor? col = null, bool writeToScrollBuf = false) {
		updateState(writeToScrollBuf);
		
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
						writeChar(' ', x, y, col ?? color, writeToScrollBuf);
						x++;
					}
				}
			}
			
			if (print) {
				writeChar(c, x, y, col ?? color, writeToScrollBuf);
				x++;
			}
		}
	}
	
	public static void WriteBox(string[] lines, int? x_ = null, int? y_ = null, AnsiColor? col = null, bool writeToScrollBuf = false) {
		updateState(writeToScrollBuf);
		
		var initX = x_ ?? x;
		var initY = y_ ?? y;
		
		var maxLength = lines.Max(line => line.Length);
		ClearBox(maxLength, lines.Length, initX, initY, col, writeToScrollBuf);
		
		x = initX;
		y = initY;
		
		foreach (var line in lines) {
			Write(line, x, y, col, writeToScrollBuf);
			x = initX;
			y++;
		}
	}
	
	public static void ClearLine(int? y_ = null, AnsiColor? col = null, bool writeToScrollBuf = false) {
		updateState(writeToScrollBuf);
		
		if (y_ != null) y = y_.Value;
		var initY = y;
		
		Write(new(' ', Width), 0, y_, col ?? color, writeToScrollBuf);
		x = 0;
		y = initY + 1;
	}
	
	public static void ClearBox(int width, int height, int? x_ = null, int? y_ = null, AnsiColor? col = null, bool writeToScrollBuf = false) {
		updateState(writeToScrollBuf);
		
		var initX = x_ ?? x;
		var initY = y_ ?? y;
		
		for (var yy = 0; yy < height; yy++) {
			Write(new(' ', width), x_, initY + yy, col ?? color, writeToScrollBuf);
			x = initX;
		}
	}
	
	public static void DrawOutline(int x, int y,
	                               int width, int height,
	                               AnsiColor? col = null,
	                               bool removeSides = false,
	                               bool writeToScrollBuf = false) {
		updateState(writeToScrollBuf);
		
		var left   = x;
		var right  = x + width - 1;
		var top    = y;
		var bottom = y + height - 1;
			
		for (var xx = left; xx <= right; xx++) {
			writeChar('-', xx, top,    col ?? color, writeToScrollBuf);
			writeChar('-', xx, bottom, col ?? color, writeToScrollBuf);
		}
		
		if (!removeSides) {
			writeChar('+', left,  top,    col ?? color, writeToScrollBuf);
			writeChar('+', right, top,    col ?? color, writeToScrollBuf);
			writeChar('+', left,  bottom, col ?? color, writeToScrollBuf);
			writeChar('+', right, bottom, col ?? color, writeToScrollBuf);
			
			for (var yy = top + 1; yy < bottom; yy++) {
				writeChar('|', left,  yy, col ?? color, writeToScrollBuf);
				writeChar('|', right, yy, col ?? color, writeToScrollBuf);
			}
		}
	}
	
	public static string Flush() {
		prevFrame = frame;
		frame = Driver.Frame;
		
		var framesSinceLastDisplay = Math.Max(1, frame - prevFrame);
		
		StringBuilder sb = new("\x1B[H");
		
		sb.Append("\x1B[0m");
		if (ColorAt(0, 0) != null) {
			sb.Append(ColorAt(0, 0)!.AnsiString);
		}
		
		AnsiColor? prevColor = null;
		
		// Update color and char grids
		for (var i = 0; i < framesSinceLastDisplay; i++) {
			for (var y = 0; y < Height; y++) {
				for (var x = 0; x < Width; x++) {
					var ch =  CharAt(x, y);
					var cl = ColorAt(x, y);
					
					if (windowEnabled && IsInWindow(x, y)) {
						if (UseBufferBlending) {
							var (wx, wy) = WindowCoords(x, y).AsTuple;
							
							// Update color buffers
							prevColorBuffers[3][wy][wx] = prevColorBuffers[2][wy][wx];
							prevColorBuffers[2][wy][wx] = prevColorBuffers[1][wy][wx];
							prevColorBuffers[1][wy][wx] = prevColorBuffers[0][wy][wx];
							prevColorBuffers[0][wy][wx] = cl;
					
							// Update char buffers
							prevCharBuffers[3][wy][wx] = prevCharBuffers[2][wy][wx];
							prevCharBuffers[2][wy][wx] = prevCharBuffers[1][wy][wx];
							prevCharBuffers[1][wy][wx] = prevCharBuffers[0][wy][wx];
							prevCharBuffers[0][wy][wx] = ch;
						}
					}
					else {
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
		}
		
		var screenToAreaRatio = 1.0;
		var scrollbarSize     = 0;
		var scrollbarTop      = 0;
		
		if (windowEnabled) {
			screenToAreaRatio = (double) windowHeight / charBuffer.Count;
			scrollbarSize     = (int) Math.Ceiling(screenToAreaRatio * windowHeight);
			scrollbarTop      = windowTop + windowHeight * (ScrollTop - Math.Max(0, VirtualSize - charBuffer.Count)) / charBuffer.Count;
		}
		
		for (var y = 0; y < Height; y++) {
			for (var x = 0; x < Width; x++) {
				var ch =  CharAt(x, y);
				var cl = ColorAt(x, y);
				
				var isMulti = cl is not null
				           && cl.BackgroundRGB is not null && cl.ForegroundRGB is not null
				           && ch is ' ' or '▄' or '▀' or '█';
				
				var isMultiBlended = false;
				
				if ((!cutoutEnabled || !IsInCutout(x, y))
				    && windowEnabled && charBuffer.Count > windowHeight && IsInWindow(x, y) && x == WindowBottomRight.X - 1)
				{
					if (y >= scrollbarTop && y < scrollbarTop + scrollbarSize) {
						ch = '█';
						cl = AnsiColor.Grey;
					}
					else {
						ch = '▒';
						cl = AnsiColor.DarkGrey;
					}
				}
				else if (cl is not null && cl.IsBG || isMulti) {
					if ((!cutoutEnabled || !IsInCutout(x, y)) && windowEnabled && IsInWindow(x, y)) {
						if (UseBufferBlending) {
							var (wx, wy) = WindowCoords(x, y).AsTuple;
						
							cl = blendColors(
								cl,
								prevColorBuffers[0][wy][wx],
								prevColorBuffers[1][wy][wx],
								prevColorBuffers[2][wy][wx],
								prevColorBuffers[3][wy][wx]
							);
						}
					}
					else if (x < Width - 32 && y >= 0 && y < Height) {
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
	
	static void updateState(bool useScrollBuffer) {
		if (useScrollBuffer != scrollBufPrevSource) {
			x = 0;
			y = 0;
			scrollBufPrevSource = useScrollBuffer;
		}
	}
	
	static void writeChar(char c, int x, int y, AnsiColor? color, bool writeToScrollBuf = false) {
		if (writeToScrollBuf) {
			SetBufferCharAt(x, y, c, color);
		}
		else {
			if (x >= Width || y >= Height) return;
		
			charGrid[y][x]  = c;
			colorGrid[y][x] = color;
		}
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