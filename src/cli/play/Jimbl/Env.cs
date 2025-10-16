namespace Jimbl;

using System.Diagnostics;

using SPath = System.IO.Path;

public static class Env {
	public class VarFront {
		public string? this[string varName] {
			get => Environment.GetEnvironmentVariable(varName);
			set {
				Environment.SetEnvironmentVariable(varName, value);
			}
		}
	}
	
	public static string ProgramDirectory => AppContext.BaseDirectory;
	public static string WorkingDirectory => Directory.GetCurrentDirectory();
	
	public static VarFront Var = new();
			
	static Dictionary<string, string> recognizedTerminals = new() {
		["gnome-terminal-server"] = "gnome-terminal",
		["konsole"]               = "konsole",
		["xterm"]                 = "xterm",
		["lxterminal"]            = "lxterminal"
	};
	
	static string? parentTerminal = null;
	
	public static string? ParentTerminal {
		get {
			if (parentTerminal is not null) {
				return parentTerminal;
			}
			
			if (OS.Get() == OS.Linux) {
				// First, try the immediate parent of the current C# app's process
				parentTerminal = Shell.ExecUnsafeGetStdout(
					"cur_ppid=$PPID; " +
					"ps -aux | grep $(ps -o ppid= -p \"$cur_ppid\") | awk 'NR==1{print $11}' | xargs basename"
				).Trim();
				
				if (recognizedTerminals.TryGetValue(parentTerminal, out parentTerminal)) {
					return parentTerminal;
				}
				
				// If that's not a recognized terminal, try checking the parent of the immediate parent
				parentTerminal = Shell.ExecUnsafeGetStdout(
					"cur_ppid=$PPID; " +
					"cur_ppid=$(ps -o ppid= -p \"$cur_ppid\"); " +
					"ps -aux | grep $(ps -o ppid= -p \"$cur_ppid\") | awk 'NR==1{print $11}' | xargs basename"
				).Trim();
			
				if (recognizedTerminals.TryGetValue(parentTerminal, out parentTerminal)) {
					return parentTerminal;
				}
				
				// Once more
				parentTerminal = Shell.ExecUnsafeGetStdout(
					"cur_ppid=$PPID; " +
					"cur_ppid=$(ps -o ppid= -p \"$cur_ppid\"); " +
					"cur_ppid=$(ps -o ppid= -p \"$cur_ppid\"); " +
					"ps -aux | grep $(ps -o ppid= -p \"$cur_ppid\") | awk 'NR==1{print $11}' | xargs basename"
				).Trim();
			
				if (recognizedTerminals.TryGetValue(parentTerminal, out parentTerminal)) {
					return parentTerminal;
				}
				
				return null;
			}
			else if (OS.Get() == OS.Windows) {
				throw new NotImplementedException();
			}
			else {
				throw new UnreachableException();
			}
		}
	}
	
	/// <summary>
	/// Returns a 2-tuple (width, height) of the current terminal window's size in columns and rows.
	/// </summary>
	public static (int, int) WindowSize {
		get {
			if (OS.Get() == OS.Linux) {
				var size   = Shell.ExecGetStdout("stty", "size").Split();
				var height = int.Parse(size[0]);
				var width  = int.Parse(size[1]);
				
				return (width, height);
			}
			else {
				return (Console.WindowWidth, Console.WindowHeight);
			}
		}
	}
	
	public static string[] Path {
		get {
			if (OS.Get() == OS.Windows) {
				return Var["PATH"]?.Split(';') ?? [];
			}
			else if (OS.Get() == OS.Linux) {
				return Var["PATH"]?.Split(':') ?? [];
			}
			else {
				throw new UnreachableException();
			}
		}
	}
	
	public static string? Which(string command) {
		if (command.EndsWith('/') || OS.Get() == OS.Windows && command.EndsWith('\\')) {
			return null;
		}
		
		if (OS.Get() == OS.Windows) {
			if (File.Exists(command) && !Directory.Exists(command)) {
				return SPath.GetFullPath(command);
			}
			else if (ExePath(command) is string p && !Directory.Exists(p)) {
				return SPath.GetFullPath(p);
			}
		}
		else if (OS.Get() == OS.Linux && command.Contains('/') && IsExecutable(command)) {
			return SPath.GetFullPath(command);
		}
		
		// Command cannot be an absolute or relative directory to do lookup in PATH - Must be the name only
		if (command.Contains('/') || OS.Get() == OS.Windows && command.Contains('\\')) {
			return null;
		}
		
		foreach (var dir in Path) {
			var fullPath = SPath.Join(dir, command);
			var fullExePath = ExePath(fullPath);
			
			if (fullExePath is string p && IsExecutable(p)) {
				return fullExePath;
			}
		}
		
		return null;
	}
	
	public static string? ExePath(string exePath) {
		if (OS.Get() == OS.Linux) {
			return exePath;
		}
		else if (OS.Get() == OS.Windows) {
			foreach (var ext in new[] {".exe", ".cmd", ".bat", ".com"}) {
				if (File.Exists(exePath + ext)) {
					return exePath + ext;
				}
			}
		}
		
		return null;
	}
	
	public static bool IsExecutable(string path) {
		if (!File.Exists(path) || Directory.Exists(path)) {
			return false;
		}
		
		if (OS.Get() == OS.Windows) {
			path = path.ToLower();
			return path.EndsWith(".exe") || path.EndsWith(".cmd") || path.EndsWith(".bat") || path.EndsWith(".com");
		}
		else if (OS.Get() == OS.Linux) {
			var perms = Shell.ExecGetStdout("stat", "-c", "%A", path);
			return perms.Length > 9 && perms[9] == 'x'
			    || perms.Length > 6 && perms[6] == 'x'
			    || perms.Length > 3 && perms[3] == 'x';
		}
		
		return false;
	}
}