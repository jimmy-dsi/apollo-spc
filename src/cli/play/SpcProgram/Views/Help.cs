namespace SpcProgram;

using Apollo;
using Jimbl;

public static partial class CliMain {
	static void showHelpMenu() {
		var y = 1;
		var x = 30;
		
		Display.Write("Exit current menu",       1, y); Display.Write("Escape", x, y); y++;
		Display.Write("Toggle help menu",        1, y); Display.Write("CTRL+H", x, y); y++;
		Display.Write("Switch to next view",     1, y); Display.Write("Right arrow key", x, y); y++;
		Display.Write("Switch to previous view", 1, y); Display.Write("Left arrow key",  x, y); y++;
		
		Display.DrawOutline(0, 0, Display.Width, y + 1, removeSides: true);
	}
}