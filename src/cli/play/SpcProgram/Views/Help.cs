namespace SpcProgram;

using Apollo;
using Jimbl.Graphics;
using Jimbl.JMath;

public static partial class CliMain {
	static void showHelpMenu() {
		showSettingsMenu();
	}
	
	static int settingsRow = 0;
	
	static (string, string[], string[])[] settingsMenu = [
		(" Save and exit <┐ ",            [], [""]),
		(" SNES lowpass filter",          ["On", "Off"],                           ["Ctrl+P to toggle"]),
		(" ID666 fadeout",                ["On", "Off"],                           [""]),
		(" Cycle display format",         ["DSP", "SPC"],                          ["Ctrl+D to toggle"]),
		(" Heat map",                     ["Typed", "Unsigned 8-bit", "Off"],      ["Ctrl+E+T to change"]),
		(" Script700 heat map data size", ["8-bit", "16-bit", "32-bit", "64-bit"], ["Ctrl+Up / Ctrl+Down to change"]),
		(" Channels enabled",             ["On", "On", "On", "On",
		                                   "On", "On", "On", "On"], ["1-4 to toggle", "5-8 to toggle"]),
		(" Main channels enabled",        ["On", "On", "On", "On",
		                                   "On", "On", "On", "On"], ["Shift+1-4 to toggle", "Shift+5-8 to toggle"]),
		(" Echo channels enabled",        ["On", "On", "On", "On",
			                                "On", "On", "On", "On"], ["Ctrl+F1-F4 / F1-F4 to toggle", "Ctrl+F5-F8 to toggle"]),
	];
	
	static (string, string)[] helpMenu = [
		("Navigate to previous/next view", "LeftArrow / RightArrow"),
		("", ""),
		("Pause / resume",                 "Space"),
		("", ""),
		("Seek ahead 5 seconds",           "Ctrl+RightArrow"),
		("Seek back 5 seconds",            "Ctrl+LeftArrow"),
		("Seek ahead 30 seconds",          "Shift+."),
		("Seek back 30 seconds",           "Shift+,"),
		("Seek to position",               "Shift+A+0-9"),
		
		("Enable all channels",            "0"),
		("", ""),
		("Break execution / resume",       "F5"),
		("Step SPC700 instruction",        "F6"),
		("Toggle all breakpoints",         "Ctrl+B"),
		("", ""),
		("Scroll one row / sample point",  "UpArrow / DownArrow"),
		("Scroll 16 rows / 256 bytes",     "PageUp / PageDown"),
		("Scroll to start / end",          "Home / End"),
		
		("Close app",                      "Ctrl+C"),
	];
	
	static void showSettingsMenu() {
		Color highlight = new(0.10, 0.10, 0.50);
		Color darkGrey  = new(0.36, 0.36, 0.36);
		Color black     = new(0.00, 0.00, 0.00);
		
		AnsiColor buttonOn  = new(black, darkGrey);
		AnsiColor buttonSel = new(black, AnsiColor.Code.White, isBold: false);
		
		var columnX_1 = 2;
		var columnX_2 = 42;
		var columnX_3 = 84;
		var columnX_4 = 84;
		
		var topRowY   = 1;
		
		var y = topRowY;
		
		for (var r = 0; r < settingsMenu.Length; r++) {
			var (description, options, keybindings) = settingsMenu[r];
			var x = columnX_1;
			
			if (options.Length > 4) y++;
			
			AnsiColor? lineColor = r == settingsRow && options.Length > 0 ? new(highlight, isBG: true) : null;
			
			Display.ClearLine(y, col: lineColor);
			if (options.Length > 4) Display.ClearLine(y + 1, col: lineColor);
			Display.Write(description, x, y, col: options.Length > 0 ? lineColor : r == settingsRow ? buttonSel : buttonOn);
			
			var selectedItem = 0;
			x = columnX_2;
			
			switch (r) {
				case 0: { // Save and exit
					selectedItem = 0;
					break;
				}
				
				case 1: { // SNES lowpass filter
					selectedItem = lowpassStatus ? 0 : 1;
					break;
				}
				
				case 2: { // ID666 fadeout
					selectedItem = FadeoutsEnabled ? 0 : 1;
					break;
				}
				
				case 3: { // Cycle display format
					selectedItem = cyclesInSpcClocks ? 1 : 0;
					break;
				}
				
				case 4: { // Heat map
					selectedItem = !heatMapEnabled ? 2 : heatMapMemMode == HeatMapMode.TypeAware ? 0 : 1;
					break;
				}
				
				case 5: { // Script700 heat map data size
					selectedItem = heatMapDataSize switch {
						BusSize.Bit8  => 0, BusSize.Bit16 => 1,
						BusSize.Bit32 => 2, BusSize.Bit64 => 3,
						_ => 2
					};
					break;
				}
				
				case 6: { // Channels
					selectedItem = lastChanChanged;
					break;
				}
				
				case 7: { // Main channels
					selectedItem = lastMainChanged;
					break;
				}
				
				case 8: { // Main channels
					selectedItem = lastEchoChanged;
					break;
				}
			}
			
			var topY = y;
			
			for (var i = 0; i < options.Length; i++) {
				var option = options[i];
				if (r == 6) {
					if      (!mainChannelsEnabled[i] && !echoChannelsEnabled[i]) option = "Off";
					else if ( mainChannelsEnabled[i] && !echoChannelsEnabled[i]) option = "On*";
					else if (!mainChannelsEnabled[i] &&  echoChannelsEnabled[i]) option = "Off*";
				}
				else if (r == 7) {
					if (!mainChannelsEnabled[i]) option = "Off";
				}
				else if (r == 8) {
					if (!echoChannelsEnabled[i]) option = "Off";
				}
				
				Display.Write(
					$" {option} ", x, y,
					col: selectedItem != i ? (settingsRow == r ? lineColor : null) : settingsRow == r ? buttonSel : buttonOn);
				x += Math.Max(8, JMath.Sat(option.Length + 2));
				
				if (i == 3) {
					x = columnX_2; y++;
				}
			}
			
			x = columnX_4; y = topY;
			foreach (var kb in keybindings) {
				Display.Write(kb, x, y, col: settingsRow == r ? lineColor : AnsiColor.DarkGrey); y++;
			}
			y = topY;
			
			y += 1;
			if (r == 0 || options.Length > 4) y++;
		}
		
		Display.DrawOutline(0, 0, Display.Width, Display.Height - 16, removeSides: false);
		Display.Write(" Settings ", Display.Width / 2 - 5, 0, col: AnsiColor.Yellow);
		
		y = Display.Height - 16;
		
		Display.DrawOutline(0, y, Display.Width, Display.Height - 22, removeSides: false);
		Display.Write(" Other UI controls ", Display.Width / 2 - 10, y, col: AnsiColor.Green);
		
		y++;
		
		var xx = 2;
		
		for (var i = 0; i < helpMenu.Length; i++) {
			var (desc, keybinding) = helpMenu[i];
			
			Display.Write(desc,       xx, y, col: i == 18 ? AnsiColor.BrightYellow : null);
			Display.Write(keybinding, xx + 33, y, col: AnsiColor.DarkGrey);
			
			if (i == 8) {
				y = Display.Height - 16;
				xx += Display.Width / 2;
			}
			
			y++;
		}
	}
}