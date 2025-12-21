namespace SpcProgram;

using System.Runtime.InteropServices;

using Apollo;
using SDL2;

public static class AudioOutput {
	static Emulator emu => CliMain.PrimaryEmu;
	
	static long   frame = 0;
	static object frameLock = new();
	
	public static long Frame {
		get {
			lock (frameLock) {
				return frame;
			}
		}
		private set {
			lock (frameLock) {
				frame = value;
			}
		}
	}
	
	static void advanceFrame() {
		lock (frameLock) {
			frame++;
		}
	}
	
	public static void Setup(Action<EmuDataBuffer?> uiCallback) {
		if (SDL.SDL_Init(SDL.SDL_INIT_AUDIO) < 0) {
			Console.WriteLine("Failed to init SDL: " + SDL.SDL_GetError());
			return;
		}

		SDL.SDL_AudioSpec want = new();
		want.freq     = 32000;
		want.format   = SDL.AUDIO_S16; // 16-bit signed
		want.channels = 2;             // Stereo
		want.samples  = 512;           // Buffer size in samples
		want.callback = Callback;

		SDL.SDL_SetHint(SDL.SDL_HINT_AUDIO_RESAMPLING_MODE, "0"); // Trivial resampling

		var device = SDL.SDL_OpenAudioDevice(null, 0, ref want, out SDL.SDL_AudioSpec have, 0);
		if (device == IntPtr.Zero) {
			Console.WriteLine("Failed to open audio: " + SDL.SDL_GetError());
			SDL.SDL_Quit();
			return;
		}

		try {
			SDL.SDL_PauseAudioDevice(device, 0); // Start playback
			CliMain.MainLoop(uiCallback);
		}
		finally {
			SDL.SDL_CloseAudioDevice(device);
			SDL.SDL_Quit();
		}
	}
	
	static Random rng = new();
	static int cycleSpillOver = 0;
	
	// This will be called by SDL when it needs more audio data
	static void Callback(IntPtr userdata, IntPtr stream, int len) {
		lock (CliMain.EmuRestoreLock) {
			var samples = len / sizeof(Int16) / 2;
		
			var approxCycles = (samples - 1) * 64 - cycleSpillOver;
			var cycles       = emu.StepNCyclesFast(approxCycles);
		
			if (cycles < approxCycles) {
				//Console.WriteLine($"{cycles} cycles ran out of {approxCycles}");
				throw new Exception();
			}
		
			while (emu.SamplesQueued < samples) {
				emu.StepCycleFast(); // TODO: Check for errors
			}
			
			// Run a random extra number of cycles, between 0 and 63
			// This way the UI display doesn't stay "phase-locked" with DSP pipeline step and look too unnatural
			cycleSpillOver = rng.Next(0, 64);
			emu.StepNCyclesFast(cycleSpillOver); // TODO: Check for errors
		
			var buffer = emu.GetBufferedSamples();

			// Copy managed array into unmanaged buffer
			Marshal.Copy(buffer, 0, stream, samples * 2);
			
			Transfer.SendEmuData();
			advanceFrame();
		}
	}
}