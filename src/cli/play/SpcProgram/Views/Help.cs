namespace SpcProgram;

using Apollo;
using Jimbl;

public static partial class CliMain {
	static void showHelpMenu() {
		var topY = 0;
		
		var y = 1;
		var x = 38;
		
		Display.Write("Exit current menu",       1, y); Display.Write("Escape", x, y); y++;
		Display.Write("Toggle help menu",        1, y); Display.Write("CTRL+L", x, y); y++;
		Display.Write("Switch to next view",     1, y); Display.Write("Right arrow key", x, y); y++;
		Display.Write("Switch to previous view", 1, y); Display.Write("Left arrow key",  x, y); y++;
		y++;
		
		Display.Write("Scroll up one row",   1, y); Display.Write("Up arrow key",   x, y); y++;
		Display.Write("Scroll down one row", 1, y); Display.Write("Down arrow key", x, y); y++;
		Display.Write("Scroll up 16 rows",   1, y); Display.Write("PageUp",         x, y); y++;
		Display.Write("Scroll down 16 rows", 1, y); Display.Write("PageDown",       x, y); y++;
		Display.Write("Scroll to top",       1, y); Display.Write("Home",           x, y); y++;
		Display.Write("Scroll to bottom",    1, y); Display.Write("End",            x, y); y++;
		y++;
		
		Display.Write("Toggle heat map", 1, y); Display.Write("CTRL+E+T", x, y);
		Display.Write("[Warning: may produce rapid flashing]", x * 2, y, Color.Yellow);
		y++;
		Display.Write($"Display cycle count in {(cyclesInSpcClocks ? "DSP" : "SPC")} clocks", 1, y); Display.Write("CTRL+D", x, y); y++;
		
		Display.DrawOutline(0, 0, Display.Width, y + 1, removeSides: true); y++;
	}
}