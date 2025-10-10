using Play;

using System.Diagnostics;
using Jimbl;

const bool VERIFY_HASH = true;

const int WIDTH  = 158;
const int HEIGHT = 33;

bool autoResizeable = false;
if (OS.Get() == OS.Windows) {
	autoResizeable = false;
}
else if (OS.Get() == OS.Linux && Shell.CommandExists("resize")) {
	autoResizeable = true;
}

var fwdArgs       = args.Where(x => x != "--force-no-resize").ToArray();
var forceNoResize = args.  Any(x => x == "--force-no-resize");

if (!autoResizeable && !forceNoResize) {
	switch (OS.Get()) {
		case OS.Windows: {
			var width  = Console.WindowWidth;
			var height = Console.WindowHeight;
			
			if (height >= HEIGHT && width >= WIDTH) {
				forceNoResize = true;
				break;
			}
			
			var success = Try.Catch(() => 
				Shell.ExecInBG("wt",
				               new[] {"--size", $"{WIDTH},{HEIGHT}"}
				               .Concat([$"{Path.Join(Env.ProgramDirectory, "play.exe")}"])
				               .Concat(fwdArgs).Concat(["--force-no-resize"]).ToArray()),
				(Shell.CommandNotFoundError _) => false
			);
			
			if (!success) {
				forceNoResize = true;
				break;
			}
			
			return;
		}
		
		case OS.Linux: {
			var size   = Shell.ExecGetStdout("stty", "size").Split();
			var height = int.Parse(size[0]);
			var width  = int.Parse(size[1]);
			
			if (height >= HEIGHT && width >= WIDTH) {
				forceNoResize = true;
				break;
			}
			
			string[] terminalEmulators = ["gnome-terminal", "konsole", "xterm", "lxterminal"];
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
			
			var fullCmd = Shell.GetFullCommand($"{Path.Join(Env.ProgramDirectory, "play")}",
			                                   fwdArgs.Concat(["--force-no-resize"]).ToArray());
			
			if (selectedTerminal == "gnome-terminal") {
				Shell.ExecInBG(selectedTerminal,
				               $"--geometry={WIDTH}x{HEIGHT}",
				               "--", "bash", "-c",
				               $"{fullCmd}; exec bash");
				return;
			}
			else if (selectedTerminal == "konsole") {
				return;
			}
			else if (selectedTerminal == "xterm") {
				return;
			}
			else if (selectedTerminal == "lxterminal") {
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

string[] audioConsumers   = ["paplay", "aplay", "ffplay"];
string   selectedConsumer = audioConsumers[0]; // Default to paplay if it exists

foreach (var consumer in audioConsumers) {
	if (Shell.CommandExists(consumer)) { // Find the first command which exists
		selectedConsumer = consumer;
		break;
	}
}

string[] consumerArgs;
if (selectedConsumer == "paplay") {
	consumerArgs = [
		"--raw", "--format=s16le", "--rate=32000", "--channels=2"
	];
}
else if (selectedConsumer == "aplay") {
	consumerArgs = [
		"-t", "raw", "-f", "s16_le", "-r", "32000", "-c", "2", "-q"
	];
}
else if (selectedConsumer == "ffplay") {
	consumerArgs = [
		"-f", "s16le", "-ar", "32000", "-ac", "2",
		"-i", "pipe:0",
		"-loglevel", "quiet",
		"-fflags", "nobuffer",
		"-flags", "low_delay",
		"-analyzeduration", "0",
		"-probesize", "32",
		"-nodisp", "-framedrop",
		"-autoexit"
	];
}
else {
	throw new UnreachableException();
}

var producerCommand = Path.Join(Env.ProgramDirectory, "apollo-spc-program");
if (OS.Get() == OS.Windows) {
	producerCommand += ".exe";
}

if (VERIFY_HASH) {
	if (!verifyHash(producerCommand)) {
		Console.Error.WriteLine($"Could not verify hash string of program {Shell.Escape(producerCommand)}");
		Environment.Exit(1);
	}
}

if (OS.Get() == OS.Linux && autoResizeable && !forceNoResize) {
	Shell.Exec("resize", "-s", $"{HEIGHT}", $"{WIDTH}");
}

Shell.ExecPipe(
	producerCommand: producerCommand,
	producerArgs:    fwdArgs, // Forward CLI arguments of this app into apollo-spc-program
	consumerCommand: selectedConsumer,
	consumerArgs:    consumerArgs
);

return;

bool verifyHash(string programPath) {
	var actualHash = Crypto.HashFileSHA256(programPath).ToLower();
	
	// First, check to see if the hash is in the additional hashes set
	foreach (var hash in AdditionalHashes.Set) {
		if (hash.ToLower() == actualHash) {
			return true;
		}
	}
	
	// If not, check to see if the hash is in the known hashes set
	foreach (var hash in KnownHashes.Set) {
		if (hash.ToLower() == actualHash) {
			return true;
		}
	}
	
	// If no match, return false
	return false;
}