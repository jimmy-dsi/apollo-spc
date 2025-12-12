namespace SpcProgram;

using System.Diagnostics;
using Jimbl;

public static class KeyBindings {
	public enum Code {
		ArrowLeft, ArrowUp, ArrowRight, ArrowDown,
		Insert, Delete, Home, End, PageUp, PageDown,
		Enter, Backspace, Escape, Tab,
		Printable
	}
	
	public class Key {
		public Code  Code      { get; init; }
		public char? Printable { get; init; }
		
		static Dictionary<char, Key> charKeyCache = new();
		
		// Arrow Keys
		public static Key ArrowLeft  { get; } = new(Code.ArrowLeft);
		public static Key ArrowUp    { get; } = new(Code.ArrowUp);
		public static Key ArrowRight { get; } = new(Code.ArrowRight);
		public static Key ArrowDown  { get; } = new(Code.ArrowDown);
		// Other
		public static Key Insert     { get; } = new(Code.Insert);
		public static Key Delete     { get; } = new(Code.Delete);
		public static Key Home       { get; } = new(Code.Home);
		public static Key End        { get; } = new(Code.End);
		public static Key PageUp     { get; } = new(Code.PageUp);
		public static Key PageDown   { get; } = new(Code.PageDown);
		public static Key Enter      { get; } = new(Code.Enter);
		public static Key Backspace  { get; } = new(Code.Backspace);
		public static Key Escape     { get; } = new(Code.Escape);
		public static Key Tab        { get; } = new(Code.Tab);
		
		// Printable
		public static Key Char(char c) {
			c = c.ToUpper();
			
			if (!charKeyCache.ContainsKey(c)) {
				charKeyCache[c] = new Key(c);
			}
			
			return charKeyCache[c];
		}
	
		public bool IsPressed(ConsoleKeyInfo keyInfo) {
			if (Code == Code.Printable) {
				return keyInfo.IsChar(Printable!.Value);
			}
			else {
				return Code switch {
					// Arrow Keys
					Code.ArrowLeft  => keyInfo.IsLeftArrow(),
					Code.ArrowUp    => keyInfo.IsUpArrow(),
					Code.ArrowRight => keyInfo.IsRightArrow(),
					Code.ArrowDown  => keyInfo.IsDownArrow(),
					// Other
					Code.Insert     => keyInfo.IsInsert(),
					Code.Delete     => keyInfo.IsDelete(),
					Code.Home       => keyInfo.IsHome(),
					Code.End        => keyInfo.IsEnd(),
					Code.PageUp     => keyInfo.IsPageUp(),
					Code.PageDown   => keyInfo.IsPageDown(),
					Code.Enter      => keyInfo.IsEnter(),
					Code.Backspace  => keyInfo.IsBackspace(),
					Code.Escape     => keyInfo.IsEscape(),
					Code.Tab        => keyInfo.IsTab(),
					_               => throw new UnreachableException()
				};
			}
		}
		
		public Key(char printable) {
			if (!printable.IsAsciiLetterOrDigit()) {
				throw new ArgumentException();
			}
			
			Code      = Code.Printable;
			Printable = printable.ToUpper();
		}
		
		public Key(Code code) {
			if (code == Code.Printable) {
				throw new ArgumentException();
			}
			
			Code      = code;
			Printable = null;
		}
	}
	
	public enum Action {
		ExitCurrentMenu,
		ToggleHelpMenu,
		NavNextView,
		NavPrevView,
		ScrollRowUp,
		ScrollRowDown,
		ScrollPageUp,
		ScrollPageDown,
		ScrollStart,
		ScrollEnd,
		ToggleHeatMap,
		ToggleCycleUnit,
	}
		
	static Key?      lastCtrlKey  = null;
	static Stopwatch lastCtrlTime = new();
	
	// Key bindings must be checked in the order they are listed here:
	public static Dictionary<Key,        Action> CtrlBindings      = new();
	public static Dictionary<(Key, Key), Action> Ctrl2KeyBindings  = new();
	public static Dictionary<Key,        Action> SingleKeyBindings = new();
	
	public static void ResetKeyBindingState() {
		lastCtrlKey = null;
		lastCtrlTime.Reset();
	}
	
	public static void Register(Key key, Action action, bool ctrl = false) {
		if (ctrl) {
			CtrlBindings.Add(key, action);
		}
		else {
			SingleKeyBindings.Add(key, action);
		}
	}
	
	public static void Register(Key firstKey, Key secondKey, Action action) {
		Ctrl2KeyBindings.Add((firstKey, secondKey), action);
	}
	
	public static Action? GetAction() {
		var keyInfo = KeyListener.GetKeyInfo();
		if (keyInfo is null) return null;
		
		var ki = keyInfo.Value;
		
		if (ki.HasCtrl()) {
			foreach (var (k, v) in CtrlBindings) {
				if (k.IsPressed(ki)) {
					ResetKeyBindingState();
					return v;
				}
			}
			
			foreach (var ((k1, k2), v) in Ctrl2KeyBindings) {
				if (lastCtrlTime.ElapsedMilliseconds > 1000) {
					lastCtrlTime.Reset();
				}
				else if (lastCtrlKey == k1 && k2.IsPressed(ki)) {
					ResetKeyBindingState();
					return v;
				}
				else if (k1.IsPressed(ki)) {
					lastCtrlKey = k1;
					lastCtrlTime.Restart();
				}
			}
		}
		else {
			foreach (var (k, v) in SingleKeyBindings) {
				if (k.IsPressed(ki)) {
					return v;
				}
			}
		}
		
		return null;
	}
}