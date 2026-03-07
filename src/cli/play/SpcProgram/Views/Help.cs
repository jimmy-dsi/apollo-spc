namespace SpcProgram;

using Apollo;
using Jimbl.Graphics;

public static partial class CliMain {
	static void showHelpMenu() {
		var topY = 0;
		
		var y = 1;
		var x = 38;
		
		Display.Write("Exit current menu",              1, y); Display.Write("Escape",              x, y); y++;
		Display.Write("Toggle help menu",               1, y); Display.Write("Ctrl+L",              x, y); y++;
		Display.Write("Switch to next view",            1, y); Display.Write("RightArrow",          x, y); y++;
		Display.Write("Switch to previous view",        1, y); Display.Write("LeftArrow",           x, y); y++;
		y++;
		
		Display.Write("Seek ahead 5 seconds",           1, y); Display.Write("Ctrl+RightArrow",     x, y); y++;
		Display.Write("Seek back 5 seconds",            1, y); Display.Write("Ctrl+LeftArrow",      x, y); y++;
		Display.Write("Seek ahead 30 seconds",          1, y); Display.Write("Shift+.",             x, y); y++;
		Display.Write("Seek back 30 seconds",           1, y); Display.Write("Shift+,",             x, y); y++;
		Display.Write("Seek to position",               1, y); Display.Write("Shift+A+1-8",         x, y); y++;
		y++;
		
		Display.Write("Enable all channels",            1, y); Display.Write("0",                   x, y); y++;
		Display.Write("Toggle channel 1-8 output",      1, y); Display.Write("1-8",                 x, y); y++;
		Display.Write("Toggle channel 1-8 main output", 1, y); Display.Write("Shift+1-8",           x, y); y++;
		Display.Write("Toggle channel 1-4 echo output", 1, y); Display.Write("Ctrl+F1-F4 or F1-F4", x, y); y++;
		Display.Write("Toggle channel 5-8 echo output", 1, y); Display.Write("Ctrl+F5-F8",          x, y); y++;
		y++;
		
		Display.Write("Scroll up / down one row",       1, y); Display.Write("UpArrow / DownArrow", x, y); y++;
		Display.Write("Scroll up / down 16 rows",       1, y); Display.Write("PageUp / PageDown",   x, y); y++;
		Display.Write("Scroll to top / bottom",         1, y); Display.Write("Home / End",          x, y); y++;
		y++;
		
		Display.Write("Toggle heat map", 1, y); Display.Write("Ctrl+E+T", x, y);
		Display.Write("[Warning: may produce rapid flashing]", x * 2, y, AnsiColor.Yellow);
		y++;
		Display.Write($"Display cycle count in {(cyclesInSpcClocks ? "DSP" : "SPC")} clocks", 1, y); Display.Write("Ctrl+D", x, y); y++;
		y++;
		
		Display.Write("Pause/Resume",                   1, y); Display.Write("Space",               x, y); y++;
		
		Display.DrawOutline(0, 0, Display.Width, y + 1, removeSides: true); y++;
	}
}