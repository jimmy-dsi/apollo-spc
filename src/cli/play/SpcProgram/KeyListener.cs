namespace SpcProgram;

using Jimbl;

public static class KeyListener {
	static object keyLock = new();
	static ConsoleKeyInfo? keyInfo = null;
	
	public static void Run() {
		while (true) {
			var info = Console.ReadKey(true);
			updateKeyInfo(info);
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
}