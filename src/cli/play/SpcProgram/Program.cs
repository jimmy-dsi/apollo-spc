using SpcProgram;

using System.Diagnostics;
using Jimbl;

const int WIDTH  = 130;
const int HEIGHT = 34;

var fwdArgs       = args.Where(x => x != "--force-no-resize").ToArray();
var forceNoResize = args.  Any(x => x == "--force-no-resize");
var fileError     = false;

if (fwdArgs.Length == 0 || fwdArgs[0].StartsWith("--")) {
	forceNoResize = true;
	fileError     = true;
}
else if (!File.Exists(fwdArgs[0])) {
	// Attempt to open first argument as file - If fails, mark as error
	forceNoResize = true;
	fileError     = true;
}

bool autoResizeable = false;

if (OS.Get() == OS.Windows) {
	autoResizeable = false;
}
else if (OS.Get() == OS.Linux && !forceNoResize && Shell.CommandExists("resize") && Env.ParentTerminal != "konsole") {
	var success = Try.Catch(
		() => (string?) Shell.ExecGetStdout("resize", "-s", $"{HEIGHT}", $"{WIDTH}"), // Capture stdout so that the console doesn't display it
		(Shell.CommandNotFoundError _) => null
	);
	
	if (success != null) {
		var (newWidth, newHeight) = Env.WindowSize;
		autoResizeable            = newWidth >= WIDTH || newHeight >= HEIGHT;
	}
	else {
		autoResizeable = false;
	}
}

var (widthPx, heightPx) = (0, 0);

if (forceNoResize && fwdArgs.Length != args.Length) {
	var idx = Array.IndexOf(args, "--force-no-resize");
	
	if (args.Length > idx + 1) {
		fwdArgs = args.Enum() 
		              .Where(t => t.Item1 != idx && t.Item1 != idx + 1) 
		              .Select(t => t.Item2) 
		              .ToArray();
		
		var sizeStrs = args[idx + 1].Split(',');
		
		if (sizeStrs.Length != 2 || !int.TryParse(sizeStrs[0], out widthPx) || !int.TryParse(sizeStrs[1], out heightPx)) {
			Console.Error.WriteLine("invalid value provided for force-no-resize");
			Console.WriteLine("\nPress any key to continue...");
			Console.ReadKey();
			Environment.Exit(1);
		}
	}
	
	var (width, height) = Env.WindowSize;
	if (height >= HEIGHT && width >= WIDTH && height <= HEIGHT + 3 && width <= WIDTH + 5) {
		forceNoResize = true;
	}
	else if (widthPx != 0 && heightPx != 0 && OS.Get() == OS.Linux && Env.ParentTerminal == "konsole") {
		var (curWidth,    curHeight   ) = Env.WindowSize;
		var (widthRatio,  heightRatio ) = ((double) widthPx / curWidth, (double) heightPx / curHeight);
		var (targetWidth, targetHeight) = ((int) Math.Ceiling(WIDTH * widthRatio), (int) Math.Ceiling(HEIGHT * heightRatio));
			
		var fullCmd = Shell.GetFullCommand($"{Path.Join(Env.ProgramDirectory, "apollo-spc-program")}", fwdArgs.Concat(["--force-no-resize"]).ToArray());
		Console.WriteLine(fullCmd);
		
		Shell.ExecInBG("nohup", "konsole",
		               $"--qwindowgeometry", $"{targetWidth}x{targetHeight}",
		               "-e", $"{fullCmd}");
		Thread.Sleep(50);
		return;
	}
}

if (!autoResizeable && !forceNoResize) {
	var (width, height) = Env.WindowSize;
	
	switch (OS.Get()) {
		case OS.Windows: {
			if (height >= HEIGHT && width >= WIDTH || Env.Var["MSYSTEM"] is not null) {
				autoResizeable = true;
				break;
			}
			
			string[] terminalEmulators = ["wt", "mintty", @"C:\msys64\usr\bin\mintty"];
			string?  selectedTerminal  = null;
			
			foreach (var terminal in terminalEmulators) {
				if (Shell.CommandExists(terminal)) {
					selectedTerminal = terminal;
					break;
				}
			}
			
			if (selectedTerminal is null) {
				forceNoResize = true;
				break;
			}
			
			if (selectedTerminal == "wt") {
				Shell.ExecInBG("wt",
				               new[] {"--size", $"{WIDTH},{HEIGHT}"}
				               .Concat([$"{Path.Join(Env.ProgramDirectory, "apollo-spc-program.exe")}"])
				               .Concat(fwdArgs).Concat(["--force-no-resize"]).ToArray());
				return;
			}
			else if (selectedTerminal.Contains("mintty")) {
				Shell.ExecInBG(selectedTerminal,
				               new[] {"--geometry", $"{WIDTH}x{HEIGHT}"}
				               .Concat(["-e"])
				               .Concat([$"{Path.Join(Env.ProgramDirectory, "apollo-spc-program.exe")}"])
				               .Concat(fwdArgs).Concat(["--force-no-resize"]).ToArray());
				return;
			}
			else {
				throw new UnreachableException();
			}
		}
		
		case OS.Linux: {
			if (height >= HEIGHT && width >= WIDTH) {
				forceNoResize = true;
				break;
			}
			
			string[] terminalEmulators = ["gnome-terminal", "konsole", "xterm", "lxterminal"];
			string?  selectedTerminal  = terminalEmulators.Contains(Env.ParentTerminal) ? Env.ParentTerminal : null;
			
			foreach (var terminal in terminalEmulators) {
				if (selectedTerminal is not null) {
					break;
				}
				if (Shell.CommandExists(terminal)) {
					selectedTerminal = terminal;
				}
			}
			
			if (selectedTerminal is null) {
				forceNoResize = true;
				break;
			}
			
			var fullCmd = Shell.GetFullCommand($"{Path.Join(Env.ProgramDirectory, "apollo-spc-program")}",
			                                   fwdArgs.Concat(["--force-no-resize"]).ToArray());
			
			if (selectedTerminal == "gnome-terminal") {
				Shell.ExecInBG(selectedTerminal,
				               $"--geometry={WIDTH}x{HEIGHT}",
				               "--", "bash", "-c",
				               $"{fullCmd}; exec bash");
				return;
			}
			else if (selectedTerminal == "konsole") {
				var w = (int) Math.Ceiling(WIDTH * 9.5);
				Shell.ExecInBG(selectedTerminal,
				               $"--qwindowgeometry", $"{w}x{HEIGHT*20}", // For konsole, width and height are in pixels - Try an approximation first
				               "-e", $"{fullCmd} {w},{HEIGHT*20}");
				return;
			}
			else if (selectedTerminal == "xterm") {
				Shell.ExecInBG(selectedTerminal,
				               $"-geometry", $"{WIDTH}x{HEIGHT}",
				               "-e", $"{fullCmd}");
				return;
			}
			else if (selectedTerminal == "lxterminal") {
				Shell.ExecInBG(selectedTerminal,
				               $"--geometry={WIDTH}x{HEIGHT}",
				               "-e", $"{fullCmd}");
				return;
			}
			else {
				throw new UnreachableException();
			}
		}
		
		default: {
			throw new UnreachableException();
		}
	}
}

Console.CancelKeyPress += (_, args) => {
	Console.CursorVisible = true;
	Console.Clear();
	Environment.Exit(0);
};

if (OS.Get() == OS.Windows && autoResizeable && !forceNoResize) {
	Console.SetWindowSize(WIDTH, HEIGHT);
}

//if (fwdArgs.Length >= 1) {
//	var scriptFile = Script700.ScriptFile(fwdArgs[0]);
//	if (scriptFile is not null) {
//		var scriptData = Script700.Simplify(File.ReadAllText(scriptFile));
//		Try.Catch(() => Shell.ExecSetStdin(producerCommand, scriptData.Replace("\r", ""), scriptFile, "--script700"), false);
//	}
//}

try {
	Display.Init(WIDTH, HEIGHT);
	var exitCode = CliMain.Start(fwdArgs);

	if (!fileError && exitCode == 0) {
		Console.Clear();
	}

	Environment.Exit(exitCode);
}
catch (Exception e) {
	Console.Clear();
	Console.Error.WriteLine(e);
	Console.CursorVisible = true;
	Environment.Exit(1);
}