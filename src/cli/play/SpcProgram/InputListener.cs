namespace SpcProgram;

using System.Text;
using Jimbl;

public static class InputListener {
	static object keyLock = new();
	static ConsoleKeyInfo? keyInfo   = null;
	static MouseInfo?      mouseInfo = null;
	
	static Dictionary<MouseEventType, ButtonStatus> mouseState = new() {
		[MouseEventType.LeftClick]       = ButtonStatus.Off,
		[MouseEventType.MiddleClick]     = ButtonStatus.Off,
		[MouseEventType.RightClick]      = ButtonStatus.Off,
		[MouseEventType.ScrollWheelUp]   = ButtonStatus.Off,
		[MouseEventType.ScrollWheelDown] = ButtonStatus.Off,
	};
	
	static Dictionary<MouseEventType, ButtonStatus> mouseEffectiveState = new() {
		[MouseEventType.LeftClick]       = ButtonStatus.Off,
		[MouseEventType.MiddleClick]     = ButtonStatus.Off,
		[MouseEventType.RightClick]      = ButtonStatus.Off,
		[MouseEventType.ScrollWheelUp]   = ButtonStatus.Off,
		[MouseEventType.ScrollWheelDown] = ButtonStatus.Off,
	};
	
	public static int MouseX          { get; private set; } = -1;
	public static int MouseY          { get; private set; } = -1;
	
	public static int LeftClickMouseX { get; private set; } = -1;
	public static int LeftClickMouseY { get; private set; } = -1;
	
	public static void Run() {
		// Enable Mouse Tracking
		Console.Write("\x1B[?1003h");
		Console.Write("\x1B[?1006h");
		
		using var stream = Console.OpenStandardInput();
		StringBuilder sb = new();
		
		// Enable raw mode - Needed to read from our stream without requiring the user to press enter
		ConsoleOS.EnableRawMode();
		
		while (true) {
			ConsoleKeyInfo? info = null;
			MouseInfo?     minfo = null;
			
			#if LINUX || OSX
				info = Console.ReadKey(true);
				if (info!.Value.Key == ConsoleKey.Escape) {
					sb.Append('\x1B');
					while (Console.KeyAvailable) {
						var c = Console.ReadKey(true).KeyChar;
						sb.Append(c);
						if (c.IsAsciiLetter() && c != 'O' || c is '~') break;
					}
					minfo = toMouseInfo(sb.ToString());
					if (minfo is not null) info = null;
					sb.Clear();
				}
			#else
				// Read one byte at a time and append into our own sequence manually, in case of OS context switching interrupting escape sequences
				var b = stream.ReadByte();
				if (b == -1) break;
				
				var c = (char) b;
				
				if (sb.Length > 0 || c == '\x1B') {
					sb.Append(c);
					// Escape sequences terminate on alphabetical characters (other than O) or a tilde
					// There may be other terminating cases, but they are not relevant here with the supported key and mouse commands
					if (c.IsAsciiLetter() && c != 'O' || c is '~') {
						info = toKeyInfo(sb.ToString());
						if (info is null) minfo = toMouseInfo(sb.ToString());
						sb.Clear();
					}
				}
				else {
					info = toKeyInfo($"{c}");
				}
			#endif
			
			if (info is ConsoleKeyInfo inf) {
				updateKeyInfo(inf);
			}
			else if (minfo is MouseInfo minf) {
				updateMouseInfo(minf);
			}
		}
	}
	
	public static ConsoleKeyInfo? GetKeyInfo() {
		lock (keyLock) {
			if (keyInfo is null) return null;
			
			var info = keyInfo.Value;
			keyInfo = null;
			
			return info;
		}
	}
	
	public static MouseInfo? GetMouseInfo() {
		lock (keyLock) {
			if (mouseInfo is null) return null;
			
			var info = mouseInfo.Value;
			mouseInfo = null;
			
			MouseX = info.X;
			MouseY = info.Y;
			
			foreach (var item in mouseState) {
				var effValue = mouseEffectiveState[item.Key];
				
				if (item.Key is MouseEventType.LeftClick or MouseEventType.MiddleClick or MouseEventType.RightClick) {
					if (item.Value == ButtonStatus.On && effValue is ButtonStatus.Off or ButtonStatus.Released) { // Off -> On
						effValue = ButtonStatus.Pressed;
						
						if (item.Key == MouseEventType.LeftClick) {
							LeftClickMouseX = info.X;
							LeftClickMouseY = info.Y;
						}
					}
					else if (item.Value == ButtonStatus.On && effValue is ButtonStatus.Pressed or ButtonStatus.Held) { // On -> On
						effValue = ButtonStatus.Held;
					}
					else if (item.Value == ButtonStatus.Off && effValue is ButtonStatus.Off or ButtonStatus.Released) { // Off -> Off
						effValue = ButtonStatus.Off;
						
						if (item.Key == MouseEventType.LeftClick) {
							LeftClickMouseX = -1;
							LeftClickMouseY = -1;
						}
					}
					else if (item.Value == ButtonStatus.Off && effValue is ButtonStatus.Pressed or ButtonStatus.Held) { // On -> Off
						effValue = ButtonStatus.Released;
					}
				}
				else {
					if (item.Value == ButtonStatus.On && effValue is ButtonStatus.Off or ButtonStatus.Released) { // Off -> On
						effValue = ButtonStatus.Pressed;
					}
					else if (item.Value == ButtonStatus.On && effValue == ButtonStatus.Pressed) { // On -> On
						effValue = ButtonStatus.Released;
					}
					else if (item.Value == ButtonStatus.Off && effValue is ButtonStatus.Off or ButtonStatus.Released) { // Off -> Off
						effValue = ButtonStatus.Off;
					}
					else if (item.Value == ButtonStatus.Off && effValue is ButtonStatus.Pressed) { // On -> Off
						effValue = ButtonStatus.Released;
					}
				}
				
				mouseEffectiveState[item.Key] = effValue;
			}
			
			return info;
		}
	}
	
	public static bool MouseButtonPressed(MouseEventType button) {
		return mouseEffectiveState[button] == ButtonStatus.Pressed;
	}
	
	public static bool MouseButtonDown(MouseEventType button) {
		return mouseEffectiveState[button] is ButtonStatus.Pressed or ButtonStatus.Held;
	}
	
	public static bool MouseButtonReleased(MouseEventType button) {
		return mouseEffectiveState[button] == ButtonStatus.Released;
	}
	
	// Modifiers
	public static bool HasCtrl(this ConsoleKeyInfo info) {
		return info.Modifiers.HasFlag(ConsoleModifiers.Control);
	}
	
	public static bool HasShift(this ConsoleKeyInfo info) {
		return info.Modifiers.HasFlag(ConsoleModifiers.Shift);
	}
	
	public static bool HasAlt(this ConsoleKeyInfo info) {
		return info.Modifiers.HasFlag(ConsoleModifiers.Alt);
	}
	
	// Printables
	public static bool IsChar(this ConsoleKeyInfo info, char c) {
		if (HasCtrl(info) && c.ToUpper() is >= 'A' and <= 'Z') {
			return info.KeyChar + 'A' - 1 == c.ToUpper();
		}
		else {
			return info.KeyChar.ToUpper() == c.ToUpper();
		}
	}
	
	// Arrow keys
	public static bool  IsLeftArrow(this ConsoleKeyInfo info) => info.Key == ConsoleKey.LeftArrow;
	public static bool IsRightArrow(this ConsoleKeyInfo info) => info.Key == ConsoleKey.RightArrow;
	public static bool    IsUpArrow(this ConsoleKeyInfo info) => info.Key == ConsoleKey.UpArrow;
	public static bool  IsDownArrow(this ConsoleKeyInfo info) => info.Key == ConsoleKey.DownArrow;
	
	// Function keys
	public static bool IsF1 (this ConsoleKeyInfo info) => info.Key == ConsoleKey.F1;
	public static bool IsF2 (this ConsoleKeyInfo info) => info.Key == ConsoleKey.F2;
	public static bool IsF3 (this ConsoleKeyInfo info) => info.Key == ConsoleKey.F3;
	public static bool IsF4 (this ConsoleKeyInfo info) => info.Key == ConsoleKey.F4;
	public static bool IsF5 (this ConsoleKeyInfo info) => info.Key == ConsoleKey.F5;
	public static bool IsF6 (this ConsoleKeyInfo info) => info.Key == ConsoleKey.F6;
	public static bool IsF7 (this ConsoleKeyInfo info) => info.Key == ConsoleKey.F7;
	public static bool IsF8 (this ConsoleKeyInfo info) => info.Key == ConsoleKey.F8;
	public static bool IsF9 (this ConsoleKeyInfo info) => info.Key == ConsoleKey.F9;
	public static bool IsF10(this ConsoleKeyInfo info) => info.Key == ConsoleKey.F10;
	public static bool IsF11(this ConsoleKeyInfo info) => info.Key == ConsoleKey.F11;
	public static bool IsF12(this ConsoleKeyInfo info) => info.Key == ConsoleKey.F12;
	
	// Others
	public static bool    IsInsert(this ConsoleKeyInfo info) => info.Key == ConsoleKey.Insert;
	public static bool    IsDelete(this ConsoleKeyInfo info) => info.Key == ConsoleKey.Delete;
	public static bool      IsHome(this ConsoleKeyInfo info) => info.Key == ConsoleKey.Home;
	public static bool       IsEnd(this ConsoleKeyInfo info) => info.Key == ConsoleKey.End;
	public static bool    IsPageUp(this ConsoleKeyInfo info) => info.Key == ConsoleKey.PageUp;
	public static bool  IsPageDown(this ConsoleKeyInfo info) => info.Key == ConsoleKey.PageDown;
	public static bool    IsEscape(this ConsoleKeyInfo info) => info.Key == ConsoleKey.Escape;
	public static bool     IsEnter(this ConsoleKeyInfo info) => info.Key == ConsoleKey.Enter;
	public static bool       IsTab(this ConsoleKeyInfo info) => info.Key == ConsoleKey.Tab;
	public static bool IsBackspace(this ConsoleKeyInfo info) => info.Key == ConsoleKey.Backspace;
	
	static void updateKeyInfo(ConsoleKeyInfo info) {
		lock (keyLock) {
			keyInfo = info;
		}
	}
	
	static void updateMouseInfo(MouseInfo info) {
		lock (keyLock) {
			mouseInfo = info;
		}
	}
	
	static ConsoleKeyInfo? toKeyInfo(string s) {
		if (s.Length == 1) {
			var c = s[0];
			
			if (c == 9) {
				return new('\0', ConsoleKey.Tab, shift: false, alt: false, control: false);
			}
			else if ((int) c is 10 or 13) {
				return new('\0', ConsoleKey.Enter, shift: false, alt: false, control: false);
			}
			else if ((int) c is >= 1 and <= 26) {
				return new(c, (ConsoleKey) ('A' + c - 1), shift: false, alt: false, control: true);
			}
			else if (c.IsAsciiLetter()) {
				Enum.TryParse<ConsoleKey>(c.ToUpper().ToString(), out var keyEnum);
				return new(c, keyEnum, shift: false, alt: false, control: false);
			}
			else if (c.IsAsciiDigit()) {
				Enum.TryParse<ConsoleKey>($"D{c}", out var keyEnum);
				return new(c, keyEnum, shift: false, alt: false, control: false);
			}
			else {
				var keyEnum = ConsoleKey.NoName;
				var isShift = false;
				
				switch (c) {
					case '!': {
						keyEnum = ConsoleKey.D1;
						isShift = true;
						break;
					}
					
					case '@': {
						keyEnum = ConsoleKey.D2;
						isShift = true;
						break;
					}
					
					case '#': {
						keyEnum = ConsoleKey.D3;
						isShift = true;
						break;
					}
					
					case '$': {
						keyEnum = ConsoleKey.D4;
						isShift = true;
						break;
					}
					
					case '%': {
						keyEnum = ConsoleKey.D5;
						isShift = true;
						break;
					}
					
					case '^': {
						keyEnum = ConsoleKey.D6;
						isShift = true;
						break;
					}
					
					case '&': {
						keyEnum = ConsoleKey.D7;
						isShift = true;
						break;
					}
					
					case '*': {
						keyEnum = ConsoleKey.D8;
						isShift = true;
						break;
					}
					
					case '(': {
						keyEnum = ConsoleKey.D9;
						isShift = true;
						break;
					}
					
					case ')': {
						keyEnum = ConsoleKey.D0;
						isShift = true;
						break;
					}
					
					case '<': {
						keyEnum = ConsoleKey.OemComma;
						isShift = true;
						break;
					}
					
					case '>': {
						keyEnum = ConsoleKey.OemPeriod;
						isShift = true;
						break;
					}
					
					case ' ': {
						keyEnum = ConsoleKey.Spacebar;
						isShift = true;
						break;
					}
					
					case '-': {
						keyEnum = ConsoleKey.OemMinus;
						break;
					}
					
					case '+': {
						keyEnum = ConsoleKey.OemPlus;
						isShift = true;
						break;
					}
					
					case '=': {
						keyEnum = ConsoleKey.OemPlus;
						break;
					}
					
					case ',': {
						keyEnum = ConsoleKey.OemComma;
						break;
					}
					
					case '.': {
						keyEnum = ConsoleKey.OemPeriod;
						break;
					}
				};
				
				return new(c, keyEnum, shift: isShift, alt: false, control: false);
			}
		}
		// Arrow keys
		else if (s == "\x1B[A") return new('\0', ConsoleKey.UpArrow,    shift: false, alt: false, control: false);
		else if (s == "\x1B[B") return new('\0', ConsoleKey.DownArrow,  shift: false, alt: false, control: false);
		else if (s == "\x1B[C") return new('\0', ConsoleKey.RightArrow, shift: false, alt: false, control: false);
		else if (s == "\x1B[D") return new('\0', ConsoleKey.LeftArrow,  shift: false, alt: false, control: false);
		// Ctrl + Arrow keys
		else if (s == "\x1B[1;5A") return new('\0', ConsoleKey.UpArrow,    shift: false, alt: false, control: true);
		else if (s == "\x1B[1;5B") return new('\0', ConsoleKey.DownArrow,  shift: false, alt: false, control: true);
		else if (s == "\x1B[1;5C") return new('\0', ConsoleKey.RightArrow, shift: false, alt: false, control: true);
		else if (s == "\x1B[1;5D") return new('\0', ConsoleKey.LeftArrow,  shift: false, alt: false, control: true);
		// PgUp, PgDown, Home, End
		else if (s == "\x1B[5~") return new('\0', ConsoleKey.PageUp,   shift: false, alt: false, control: false);
		else if (s == "\x1B[6~") return new('\0', ConsoleKey.PageDown, shift: false, alt: false, control: false);
		else if (s == "\x1B[H")  return new('\0', ConsoleKey.Home,     shift: false, alt: false, control: false);
		else if (s == "\x1B[F")  return new('\0', ConsoleKey.End,      shift: false, alt: false, control: false);
		// F# keys
		else if (s == "\x1BOP")   return new('\0', ConsoleKey.F1,  shift: false, alt: false, control: false);
		else if (s == "\x1BOQ")   return new('\0', ConsoleKey.F2,  shift: false, alt: false, control: false);
		else if (s == "\x1BOR")   return new('\0', ConsoleKey.F3,  shift: false, alt: false, control: false);
		else if (s == "\x1BOS")   return new('\0', ConsoleKey.F4,  shift: false, alt: false, control: false);
		else if (s == "\x1B[15~") return new('\0', ConsoleKey.F5,  shift: false, alt: false, control: false);
		else if (s == "\x1B[17~") return new('\0', ConsoleKey.F6,  shift: false, alt: false, control: false);
		else if (s == "\x1B[18~") return new('\0', ConsoleKey.F7,  shift: false, alt: false, control: false);
		else if (s == "\x1B[19~") return new('\0', ConsoleKey.F8,  shift: false, alt: false, control: false);
		else if (s == "\x1B[20~") return new('\0', ConsoleKey.F9,  shift: false, alt: false, control: false);
		else if (s == "\x1B[21~") return new('\0', ConsoleKey.F10, shift: false, alt: false, control: false);
		else if (s == "\x1B[23~") return new('\0', ConsoleKey.F11, shift: false, alt: false, control: false);
		else if (s == "\x1B[24~") return new('\0', ConsoleKey.F12, shift: false, alt: false, control: false);
		// Ctrl + F# keys
		else if (s == "\x1B[1;5P")  return new('\0', ConsoleKey.F1,  shift: false, alt: false, control: true);
		else if (s == "\x1B[1;5Q")  return new('\0', ConsoleKey.F2,  shift: false, alt: false, control: true);
		else if (s == "\x1B[1;5R")  return new('\0', ConsoleKey.F3,  shift: false, alt: false, control: true);
		else if (s == "\x1B[1;5S")  return new('\0', ConsoleKey.F4,  shift: false, alt: false, control: true);
		else if (s == "\x1B[15;5~") return new('\0', ConsoleKey.F5,  shift: false, alt: false, control: true);
		else if (s == "\x1B[17;5~") return new('\0', ConsoleKey.F6,  shift: false, alt: false, control: true);
		else if (s == "\x1B[18;5~") return new('\0', ConsoleKey.F7,  shift: false, alt: false, control: true);
		else if (s == "\x1B[19;5~") return new('\0', ConsoleKey.F8,  shift: false, alt: false, control: true);
		else if (s == "\x1B[20;5~") return new('\0', ConsoleKey.F9,  shift: false, alt: false, control: true);
		else if (s == "\x1B[21;5~") return new('\0', ConsoleKey.F10, shift: false, alt: false, control: true);
		else if (s == "\x1B[23;5~") return new('\0', ConsoleKey.F11, shift: false, alt: false, control: true);
		else if (s == "\x1B[24;5~") return new('\0', ConsoleKey.F12, shift: false, alt: false, control: true);
		
		return null;
	}
	
	static MouseInfo? toMouseInfo(string s) {
		if (s.StartsWith("\x1B[<") && s[^1] is 'm' or 'M') {
			var isRelease = s[^1] == 'm';
			var parts = s[3 .. ^1].Split(';');
			
			if (parts.Length == 3 && parts.All(n => n.All(c => c.IsAsciiDigit()))) {
				var button = int.Parse(parts[0]);
				var x      = int.Parse(parts[1]);
				var y      = int.Parse(parts[2]);
			
				MouseInfo info = new() {
					X = x,
					Y = y,
					Released = isRelease,
					EventType = button switch {
						0  => MouseEventType.LeftClick,
						1  => MouseEventType.MiddleClick,
						2  => MouseEventType.RightClick,
						64 => MouseEventType.ScrollWheelUp,
						65 => MouseEventType.ScrollWheelDown,
						_  => MouseEventType.None
					}
				};
				
				// Monitor changes in mouse button states
				if (info.EventType != MouseEventType.None) {
					if (info.Released) {
						mouseState[info.EventType] = ButtonStatus.Off;
					}
					else {
						mouseState[info.EventType] = ButtonStatus.On;
					}
				}
				
				return info;
			}
		}
		
		return null;
	}
}

public struct MouseInfo {
	public int X { get; init; }
	public int Y { get; init; }
	
	public MouseEventType EventType { get; init; }
	public bool Released { get; init; }
}

public enum ButtonStatus {
	Off, On,
	Pressed, Held, Released
}

public enum MouseEventType {
	None, LeftClick, MiddleClick, RightClick, ScrollWheelUp, ScrollWheelDown
}