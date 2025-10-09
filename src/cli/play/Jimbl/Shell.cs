namespace Jimbl;

using System.ComponentModel;
using System.Diagnostics;

public static class Shell {
	public static void Exec(string command, params string[] args) {
		var fullCommand = getFullCommand(command, args);
		var process = createProcess(fullCommand);
		process.Start();
		process.WaitForExit();
	}
	
	/// <summary>
	/// Spawns two commands: One producer and one consumer. The producer's stdout is piped into the consumer's stdin.
	/// </summary>
	public static void ExecPipe(string producerCommand, string[] producerArgs,
	                            string consumerCommand, string[] consumerArgs) {
		
		var producerFullCommand = getFullCommand(producerCommand, producerArgs);
		var consumerFullCommand = getFullCommand(consumerCommand, consumerArgs);
		
		var process = createProcess($"{producerFullCommand} | {consumerFullCommand}");
		process.Start();
		process.WaitForExit();
	}
	
	public static bool CommandExists(string command) {
		ProcessStartInfo psi = new() {
			FileName               = command,
			Arguments              = "",
			UseShellExecute        = false,
			RedirectStandardOutput = true,
			RedirectStandardError  = true
		};
		
		switch (OS.Get()) {
			case OS.Windows: {
				try {
					using var p = Process.Start(psi);
					p.WaitForExit();
					return p.ExitCode != 9009; // Not recognized as an internal or external command
				}
				catch (Win32Exception ex) when (ex.NativeErrorCode is 2 or 13) { // Not found / found but not executable
					return false;
				}
			}
			case OS.Linux: {
				using var p = Process.Start(psi);
				p.WaitForExit();
				return p.ExitCode is not 127 and not 126; // Not found / not executable
			}
			default: {
				throw new UnreachableException();
			}
		}
	}
	
	public static string Escape(string value) {
		switch (OS.Get()) {
			case OS.Windows: {
				// Assuming cmd (batch) shell
				// '!' is not handled. I don't think delayed expansion enabling is possible here, so this most likely does not need special handling
				return $"\"{value.Replace("^", "")
				                 .Replace("%",  "")
				                 .Replace("\"", "")}\"";
			}
			case OS.Linux: {
				// Assuming bash shell
				return $"\"{value.Replace(@"\", @"\\")
				                 .Replace("\"", "\\\"")
				                 .Replace(@"$", @"\$")
				                 .Replace(@"`", @"\`")}\"";
			}
			default: {
				throw new UnreachableException();
			}
		}
	}
	
	static string getFullCommand(string command, string[] args) {
		return $"{Escape(command)} {string.Join(' ', args.Select(Escape))}";
	}
	
	static Process createProcess(string fullCommand) {
		switch (OS.Get()) {
			case OS.Windows: {
				return new() {
					StartInfo = new() {
						FileName               = "cmd.exe",
						Arguments              = $"/c {fullCommand}",
						RedirectStandardOutput = false,
						RedirectStandardError  = false,
						UseShellExecute        = false,
						CreateNoWindow         = true,
					}
				};
			}
			case OS.Linux: {
				return new() {
					StartInfo = new() {
						FileName               = "/bin/bash",
						Arguments              = $"-c {Escape(fullCommand)}",
						RedirectStandardOutput = false,
						RedirectStandardError  = false,
						UseShellExecute        = false,
						CreateNoWindow         = true,
					}
				};
			}
			default: {
				throw new UnreachableException();
			}
		}
	}
}