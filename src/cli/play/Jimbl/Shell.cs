namespace Jimbl;

using System.ComponentModel;
using System.Diagnostics;

public static class Shell {
	public class CommandNotFoundError: Exception { }
	
	public static int LastExitCode { get; private set; }
	
	/// <summary>
	/// Executes a raw, un-escaped shell command.
	/// Warning: Do NOT allow arbitrary user input to be passed into this method, as unsafe code could be executed.
	/// </summary>
	/// <param name="fullCommand">The full un-escaped shell command</param>
	/// <returns></returns>
	public static bool ExecUnsafe(string fullCommand) {
		tryRun(fullCommand);
		return true;
	}
	
	public static bool Exec(string command, params string[] args) {
		var fullCommand = GetFullCommand(command, args);
		tryRun(fullCommand);
		return true;
	}
	
	/// <summary>
	/// Executes a raw, un-escaped shell command and returns the text printed to stdout.
	/// Warning: Do NOT allow arbitrary user input to be passed into this method, as unsafe code could be executed.
	/// </summary>
	/// <param name="fullCommand">The full un-escaped shell command</param>
	/// <returns></returns>
	public static string ExecUnsafeGetStdout(string fullCommand) {
		return tryRunGetStdout(fullCommand);
	}
	
	public static string ExecGetStdout(string command, params string[] args) {
		var fullCommand = GetFullCommand(command, args);
		return tryRunGetStdout(fullCommand);
	}
	
	public static bool ExecInBG(string command, params string[] args) {
		var fullCommand = $"{GetFullCommand(command, args)}";
		tryRun(fullCommand, runInBG: true);
		return true;
	}
	
	/// <summary>
	/// Spawns two commands: One producer and one consumer. The producer's stdout is piped into the consumer's stdin.
	/// </summary>
	public static bool ExecPipe(string producerCommand, string[] producerArgs,
	                            string consumerCommand, string[] consumerArgs) {
		
		var producerFullCommand = GetFullCommand(producerCommand, producerArgs);
		var consumerFullCommand = GetFullCommand(consumerCommand, consumerArgs);
		
		string fullCommand;
		if (OS.Get() == OS.Windows) {
			fullCommand = $"{producerFullCommand} 2>CON | {consumerFullCommand}";
		}
		else if (OS.Get() == OS.Linux) {
			fullCommand = $"{producerFullCommand} | {consumerFullCommand}";
		}
		else {
			throw new UnreachableException();
		}
		
		tryRun(fullCommand);
		return true;
	}
	
	public static bool CommandExists(string command) {
		return Env.Which(command) is not null;
	}
	
	public static string GetFullCommand(string command, string[] args) {
		return $"{EscapeCommand(command)} {string.Join(' ', args.Select(Escape))}";
	}
	
	public static string EscapeCommand(string command) {
		if (OS.Get() == OS.Windows) {
			if (command.All(c => char.ToLower(c) is >= '0' and <= '9' or >= 'a' and <= 'z')) {
				return command;
			}
			else {
				return Escape(command);
			}
		}
		else {
			return Escape(command);
		}
	}
	
	public static string Escape(string value) {
		switch (OS.Get()) {
			case OS.Windows: {
				// Assuming cmd (batch) shell
				// '!' is not handled. I don't think delayed expansion enabling is possible here, so this most likely does not need special handling
				return $"\"{value.Replace("^", "")
				                 .Replace("%",  "")
				                 .Replace("\"", "%")
				                 .Replace("%",  "\"\"")}\"";
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
	
	static void tryRun(string fullCommand, bool runInBG = false) {
		var process = createProcess(fullCommand);
		
		try {
			process.Start();
			
			if (runInBG) {
				return;
			}
			else {
				process.WaitForExit();
			}
			
			if (OS.Get() == OS.Windows) {
				if (process.ExitCode == 9009) { // Not recognized as an internal or external command
					throw new CommandNotFoundError();
				}
			}
			else if (OS.Get() == OS.Linux) {
				if (process.ExitCode is 127 or 126) { // Not found / not executable
					throw new CommandNotFoundError();
				}
			}
			else {
				throw new UnreachableException();
			}
		}
		catch (Win32Exception ex) when (ex.NativeErrorCode is 2 or 13) {
			throw new CommandNotFoundError();
		}
		
		if (!runInBG) {
			LastExitCode = process.ExitCode;
		}
	}
	
	static string tryRunGetStdout(string fullCommand) {
		var process = createProcess(fullCommand, true);
		
		string output;
		
		try {
			process.Start();
			output = process.StandardOutput.ReadToEnd();
			process.WaitForExit();
			
			if (OS.Get() == OS.Windows) {
				if (process.ExitCode == 9009) { // Not recognized as an internal or external command
					throw new CommandNotFoundError();
				}
			}
			else if (OS.Get() == OS.Linux) {
				if (process.ExitCode is 127 or 126) { // Not found / not executable
					throw new CommandNotFoundError();
				}
			}
			else {
				throw new UnreachableException();
			}
		}
		catch (Win32Exception ex) when (ex.NativeErrorCode is 2 or 13) {
			throw new CommandNotFoundError();
		}
		
		return output;
	}
	
	static Process createProcess(string fullCommand, bool redirectStandardOutput = false) {
		ProcessStartInfo psi;
		
		switch (OS.Get()) {
			case OS.Windows: {
				psi = new() {
					FileName               = "cmd.exe",
					Arguments              = $"/d /s /c \"{fullCommand}\"",
					RedirectStandardOutput = redirectStandardOutput,
					RedirectStandardError  = false,
					UseShellExecute        = false,
					CreateNoWindow         = false,
				};
				break;
			}
			case OS.Linux: {
				psi = new() {
					FileName               = "/bin/bash",
					RedirectStandardOutput = redirectStandardOutput,
					RedirectStandardError  = false,
					UseShellExecute        = false,
					CreateNoWindow         = true,
				};
				psi.ArgumentList.Add("-c");
				psi.ArgumentList.Add(fullCommand);
				break;
			}
			default: {
				throw new UnreachableException();
			}
		}
		
		Process proc = new() {
			StartInfo = psi
		};
		
		return proc;
	}
}