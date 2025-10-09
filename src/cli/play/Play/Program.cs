using Play;

using System.Diagnostics;
using Jimbl;

const bool VERIFY_HASH = true;

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
		"-nodisp", "-framedrop"
	];
}
else {
	throw new UnreachableException();
}

var producerCommand = Path.Join(Env.ProgramDirectory, "apollo-spc-program");

if (VERIFY_HASH) {
	if (!verifyHash(producerCommand)) {
		Console.Error.WriteLine($"Could not verify hash string of program {Shell.Escape(producerCommand)}");
		Environment.Exit(1);
	}
}

Shell.ExecPipe(
	producerCommand: producerCommand,
	producerArgs:    args, // Forward CLI arguments of this app into apollo-spc-program
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