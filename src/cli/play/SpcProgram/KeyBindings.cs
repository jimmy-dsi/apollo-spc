namespace SpcProgram;

using System.Diagnostics;
using Jimbl;

public static class KeyBindings {
	public enum Code {
		ArrowLeft, ArrowUp, ArrowRight, ArrowDown,
		F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
		Insert, Delete, Home, End, PageUp, PageDown,
		Enter, Backspace, Escape, Tab,
		Printable,
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
		// Function Keys
		public static Key F1         { get; } = new(Code.F1);
		public static Key F2         { get; } = new(Code.F2);
		public static Key F3         { get; } = new(Code.F3);
		public static Key F4         { get; } = new(Code.F4);
		public static Key F5         { get; } = new(Code.F5);
		public static Key F6         { get; } = new(Code.F6);
		public static Key F7         { get; } = new(Code.F7);
		public static Key F8         { get; } = new(Code.F8);
		public static Key F9         { get; } = new(Code.F9);
		public static Key F10        { get; } = new(Code.F10);
		public static Key F11        { get; } = new(Code.F11);
		public static Key F12        { get; } = new(Code.F12);
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
					// Function keys
					Code.F1         => keyInfo.IsF1(),
					Code.F2         => keyInfo.IsF2(),
					Code.F3         => keyInfo.IsF3(),
					Code.F4         => keyInfo.IsF4(),
					Code.F5         => keyInfo.IsF5(),
					Code.F6         => keyInfo.IsF6(),
					Code.F7         => keyInfo.IsF7(),
					Code.F8         => keyInfo.IsF8(),
					Code.F9         => keyInfo.IsF9(),
					Code.F10        => keyInfo.IsF10(),
					Code.F11        => keyInfo.IsF11(),
					Code.F12        => keyInfo.IsF12(),
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
			if (!(printable is '!' or '@' or '#' or '$' or '%' or '^' or '&' or '*' or '(' or ')' or '<' or '>' or ' ' or '-' or '+' or '=') && !printable.IsAsciiLetterOrDigit()) {
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
		EnableAllChannels,
		ToggleChannel_1,
		ToggleChannel_2,
		ToggleChannel_3,
		ToggleChannel_4,
		ToggleChannel_5,
		ToggleChannel_6,
		ToggleChannel_7,
		ToggleChannel_8,
		ToggleMainChannel_1,
		ToggleMainChannel_2,
		ToggleMainChannel_3,
		ToggleMainChannel_4,
		ToggleMainChannel_5,
		ToggleMainChannel_6,
		ToggleMainChannel_7,
		ToggleMainChannel_8,
		ToggleEchoChannel_1,
		ToggleEchoChannel_2,
		ToggleEchoChannel_3,
		ToggleEchoChannel_4,
		ToggleEchoChannel_5,
		ToggleEchoChannel_6,
		ToggleEchoChannel_7,
		ToggleEchoChannel_8,
		ToggleLPF,
		ScrollRowUp,
		ScrollRowDown,
		ScrollPageUp,
		ScrollPageDown,
		ScrollStart,
		ScrollEnd,
		ToggleHeatMap,
		ToggleCycleUnit,
		SeekFwd,
		SeekBack,
		SeekFwdFar,
		SeekBackFar,
		SeekPos_0,
		SeekPos_1,
		SeekPos_2,
		SeekPos_3,
		SeekPos_4,
		SeekPos_5,
		SeekPos_6,
		SeekPos_7,
		SeekPos_8,
		SeekPos_9,
		TogglePause,
		ToggleBreak,
		StepInstruction,
		ToggleBreakpoints,
		IncHeatMapDataSize,
		DecHeatMapDataSize,
		ContextKey_B,
		ContextKey_L,
		ContextKey_M,
		ContextKey_R,
		ZoomIn,
		ZoomOut,
		SettingsMenuSelect,
		
		WindowsCharSetting,
	}
		
	static Key?      lastCtrlKey    = null;
	static Key?      lastSingleKey  = null;
	
	static Stopwatch lastCtrlTime   = new();
	static Stopwatch lastSingleTime = new();
	
	// Key bindings must be checked in the order they are listed here:
	public static Dictionary<Key,        Action> CtrlBindings      = new(); // CTRL+<key>
	public static Dictionary<(Key, Key), Action> Ctrl2KeyBindings  = new(); // CTRL+<key>+<key>
	public static Dictionary<Key,        Action> SingleKeyBindings = new(); // <key>
	public static Dictionary<(Key, Key), Action> DoubleKeyBindings = new(); // <key>+<key>
	
	public static void ResetKeyBindingState() {
		lastCtrlKey   = null;
		lastSingleKey = null;
		
		lastCtrlTime  .Reset();
		lastSingleTime.Reset();
	}
	
	public static void Register(Key key, Action action, bool ctrl = false) {
		if (ctrl) {
			CtrlBindings.Add(key, action);
		}
		else {
			SingleKeyBindings.Add(key, action);
		}
	}
	
	public static void Register(Key firstKey, Key secondKey, Action action, bool ctrl = true) {
		if (ctrl) {
			Ctrl2KeyBindings.Add((firstKey, secondKey), action);
		}
		else {
			DoubleKeyBindings.Add((firstKey, secondKey), action);
		}
	}
	
	public static Action? GetAction() {
		var keyInfo = KeyListener.GetKeyInfo();
		if (keyInfo is null) return null;
		
		var ki = keyInfo.Value;
		
		if (ki.HasCtrl()) {
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
			
			foreach (var (k, v) in CtrlBindings) {
				if (k.IsPressed(ki)) {
					ResetKeyBindingState();
					return v;
				}
			}
		}
		else {
			foreach (var ((k1, k2), v) in DoubleKeyBindings) {
				if (lastSingleTime.ElapsedMilliseconds > 1000) {
					lastSingleTime.Reset();
				}
				else if (lastSingleKey == k1 && k2.IsPressed(ki)) {
					ResetKeyBindingState();
					return v;
				}
				else if (k1.IsPressed(ki)) {
					lastSingleKey = k1;
					lastSingleTime.Restart();
				}
			}
			
			foreach (var (k, v) in SingleKeyBindings) {
				if (k.IsPressed(ki)) {
					return v;
				}
			}
		}
		
		return null;
	}
}