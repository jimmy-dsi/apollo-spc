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
	
	const int MaxConsecutiveTimeouts = 90;
	const int BusyloopReliefMS       = 20;
	
	// This will be called by SDL when it needs more audio data
	static void Callback(IntPtr userdata, IntPtr stream, int len) {
		var numShorts = len / sizeof(Int16);
		Int16[] buffer = new Int16[numShorts];
		
		lock (CliMain.EmuRestoreLock) {
			try {
				if (CliMain.UI_State is not CliMain.State.Normal) {
					return;
				}
				
				var samples = numShorts / 2;
		
				var approxCycles = (samples - 1) * 64 - cycleSpillOver;
				var cycles       = emu.Script700.IsRunning ? emu.StepNCycles(approxCycles) : emu.StepNCyclesFast(approxCycles);
		
				var attempts = 1;
			
				while (cycles < approxCycles) {
					Thread.Sleep(BusyloopReliefMS);
				
					if (attempts >= MaxConsecutiveTimeouts) {
						CliMain.UI_State = CliMain.State.NonFatalError;
						return;
					}
				
					var remainingCycles = approxCycles - cycles;
					cycles = emu.Script700.IsRunning ? emu.StepNCycles(remainingCycles) : emu.StepNCyclesFast(remainingCycles);
				
					attempts++;
				}
		
				// TODO: Check for errors
				if (emu.Script700.IsRunning) {
					while (emu.SamplesQueued < samples) {
						emu.StepCycle();
					}
				}
				else {
					while (emu.SamplesQueued < samples) {
						emu.StepCycleFast();
					}
				}
			
				// Run a random extra number of cycles, between 0 and 63
				// This way the UI display doesn't stay "phase-locked" with DSP pipeline step and look too unnatural
				cycleSpillOver = rng.Next(0, 64);
			
				// TODO: Check for errors
				if (emu.Script700.IsRunning) {
					emu.StepNCycles(cycleSpillOver);
				}
				else {
					emu.StepNCyclesFast(cycleSpillOver);
				}
		
				buffer = emu.GetBufferedSamples();
			}
			finally {
				// Copy managed array into unmanaged buffer
				Marshal.Copy(buffer, 0, stream, numShorts);
				
				Transfer.SendEmuData();
				advanceFrame();
			}
		}
	}
}