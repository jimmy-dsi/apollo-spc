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
		(" Channels enabled",             [" On  ", " On  ", " On  ", " On  ",
		                                   " On  ", " On  ", " On  ", " On  "], ["1-4 to toggle", "5-8 to toggle"]),
		(" Main channels enabled",        [" On  ", " On  ", " On  ", " On  ",
		                                   " On  ", " On  ", " On  ", " On  "], ["Shift+1-4 to toggle", "Shift+5-8 to toggle"]),
		(" Echo channels enabled",        [" On  ", " On  ", " On  ", " On  ",
			                                " On  ", " On  ", " On  ", " On  "], ["Ctrl+F1-F4 / F1-F4 to toggle", "Ctrl+F5-F8 to toggle"]),
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
	
	public static UIElement[] SettingsMenuUIElements = [
		new(UIElement.Type.ClickableButton_3, KeyBindings.Action.ExitCurrentMenu,     null,  2,  1, 18, 1),
		
		new(UIElement.Type.ClickableButton_3, KeyBindings.Action.EnableLPF,           null, 42,  3,  4, 1),
		new(UIElement.Type.ClickableButton_3, KeyBindings.Action.DisableLPF,          null, 50,  3,  5, 1),
		
		new(UIElement.Type.ClickableButton_3, KeyBindings.Action.EnableFadeouts,      null, 42,  4,  4, 1),
		new(UIElement.Type.ClickableButton_3, KeyBindings.Action.DisableFadeouts,     null, 50,  4,  5, 1),
		
		new(UIElement.Type.ClickableButton_3, KeyBindings.Action.SetDSPCycles,        null, 42,  5,  5, 1),
		new(UIElement.Type.ClickableButton_3, KeyBindings.Action.SetSPCCycles,        null, 50,  5,  5, 1),
		
		new(UIElement.Type.ClickableButton_3, KeyBindings.Action.SetHeatMapTyped,     null, 42,  6,  7, 1),
		new(UIElement.Type.ClickableButton_3, KeyBindings.Action.SetHeatMapUnsigned,  null, 50,  6, 16, 1),
		new(UIElement.Type.ClickableButton_3, KeyBindings.Action.DisableHeatMap,      null, 66,  6,  5, 1),
		
		new(UIElement.Type.ClickableButton_3, KeyBindings.Action.HeapMapSize8bit,     null, 42,  7,  7, 1),
		new(UIElement.Type.ClickableButton_3, KeyBindings.Action.HeapMapSize16bit,    null, 50,  7,  8, 1),
		new(UIElement.Type.ClickableButton_3, KeyBindings.Action.HeapMapSize32bit,    null, 58,  7,  8, 1),
		new(UIElement.Type.ClickableButton_3, KeyBindings.Action.HeapMapSize64bit,    null, 66,  7,  8, 1),
		
		new(UIElement.Type.ClickableButton_3, KeyBindings.Action.ToggleChannel_1,     null, 42,  9,  7, 1, useCustomTrigger: true),
		new(UIElement.Type.ClickableButton_3, KeyBindings.Action.ToggleChannel_2,     null, 50,  9,  7, 1, useCustomTrigger: true),
		new(UIElement.Type.ClickableButton_3, KeyBindings.Action.ToggleChannel_3,     null, 58,  9,  7, 1, useCustomTrigger: true),
		new(UIElement.Type.ClickableButton_3, KeyBindings.Action.ToggleChannel_4,     null, 66,  9,  7, 1, useCustomTrigger: true),
		new(UIElement.Type.ClickableButton_3, KeyBindings.Action.ToggleChannel_5,     null, 42, 10,  7, 1, useCustomTrigger: true),
		new(UIElement.Type.ClickableButton_3, KeyBindings.Action.ToggleChannel_6,     null, 50, 10,  7, 1, useCustomTrigger: true),
		new(UIElement.Type.ClickableButton_3, KeyBindings.Action.ToggleChannel_7,     null, 58, 10,  7, 1, useCustomTrigger: true),
		new(UIElement.Type.ClickableButton_3, KeyBindings.Action.ToggleChannel_8,     null, 66, 10,  7, 1, useCustomTrigger: true),
		
		new(UIElement.Type.ClickableButton_3, KeyBindings.Action.ToggleMainChannel_1, null, 42, 12,  7, 1, useCustomTrigger: true),
		new(UIElement.Type.ClickableButton_3, KeyBindings.Action.ToggleMainChannel_2, null, 50, 12,  7, 1, useCustomTrigger: true),
		new(UIElement.Type.ClickableButton_3, KeyBindings.Action.ToggleMainChannel_3, null, 58, 12,  7, 1, useCustomTrigger: true),
		new(UIElement.Type.ClickableButton_3, KeyBindings.Action.ToggleMainChannel_4, null, 66, 12,  7, 1, useCustomTrigger: true),
		new(UIElement.Type.ClickableButton_3, KeyBindings.Action.ToggleMainChannel_5, null, 42, 13,  7, 1, useCustomTrigger: true),
		new(UIElement.Type.ClickableButton_3, KeyBindings.Action.ToggleMainChannel_6, null, 50, 13,  7, 1, useCustomTrigger: true),
		new(UIElement.Type.ClickableButton_3, KeyBindings.Action.ToggleMainChannel_7, null, 58, 13,  7, 1, useCustomTrigger: true),
		new(UIElement.Type.ClickableButton_3, KeyBindings.Action.ToggleMainChannel_8, null, 66, 13,  7, 1, useCustomTrigger: true),
		
		new(UIElement.Type.ClickableButton_3, KeyBindings.Action.ToggleEchoChannel_1, null, 42, 15,  7, 1, useCustomTrigger: true),
		new(UIElement.Type.ClickableButton_3, KeyBindings.Action.ToggleEchoChannel_2, null, 50, 15,  7, 1, useCustomTrigger: true),
		new(UIElement.Type.ClickableButton_3, KeyBindings.Action.ToggleEchoChannel_3, null, 58, 15,  7, 1, useCustomTrigger: true),
		new(UIElement.Type.ClickableButton_3, KeyBindings.Action.ToggleEchoChannel_4, null, 66, 15,  7, 1, useCustomTrigger: true),
		new(UIElement.Type.ClickableButton_3, KeyBindings.Action.ToggleEchoChannel_5, null, 42, 16,  7, 1, useCustomTrigger: true),
		new(UIElement.Type.ClickableButton_3, KeyBindings.Action.ToggleEchoChannel_6, null, 50, 16,  7, 1, useCustomTrigger: true),
		new(UIElement.Type.ClickableButton_3, KeyBindings.Action.ToggleEchoChannel_7, null, 58, 16,  7, 1, useCustomTrigger: true),
		new(UIElement.Type.ClickableButton_3, KeyBindings.Action.ToggleEchoChannel_8, null, 66, 16,  7, 1, useCustomTrigger: true),
		
		// Select lines
		new(UIElement.Type.ClickableButton_2, KeyBindings.Action.SettingsMenuSelect_1, null, 1,  3, Display.Width - 2, 1),
		new(UIElement.Type.ClickableButton_2, KeyBindings.Action.SettingsMenuSelect_2, null, 1,  4, Display.Width - 2, 1),
		new(UIElement.Type.ClickableButton_2, KeyBindings.Action.SettingsMenuSelect_3, null, 1,  5, Display.Width - 2, 1),
		new(UIElement.Type.ClickableButton_2, KeyBindings.Action.SettingsMenuSelect_4, null, 1,  6, Display.Width - 2, 1),
		new(UIElement.Type.ClickableButton_2, KeyBindings.Action.SettingsMenuSelect_5, null, 1,  7, Display.Width - 2, 1),
		new(UIElement.Type.ClickableButton_2, KeyBindings.Action.SettingsMenuSelect_6, null, 1,  9, Display.Width - 2, 2),
		new(UIElement.Type.ClickableButton_2, KeyBindings.Action.SettingsMenuSelect_7, null, 1, 12, Display.Width - 2, 2),
		new(UIElement.Type.ClickableButton_2, KeyBindings.Action.SettingsMenuSelect_8, null, 1, 15, Display.Width - 2, 2),
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
					if      (!mainChannelsEnabled[i] && !echoChannelsEnabled[i]) option = " Off ";
					else if ( mainChannelsEnabled[i] && !echoChannelsEnabled[i]) option = " On* ";
					else if (!mainChannelsEnabled[i] &&  echoChannelsEnabled[i]) option = " Off*";
				}
				else if (r == 7) {
					if (!mainChannelsEnabled[i]) option = " Off ";
				}
				else if (r == 8) {
					if (!echoChannelsEnabled[i]) option = " Off ";
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