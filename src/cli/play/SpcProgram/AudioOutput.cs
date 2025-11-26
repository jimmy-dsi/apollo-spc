namespace SpcProgram;

using System.Runtime.InteropServices;

using Apollo;
using SDL2;

public static class AudioOutput {
	static Emulator emu;
	
	static volatile bool signal = false;
	
	static class Comm {
		static object commLock = new();
		
		static bool uiUsingBufferA = false;
		//static bool uiUsingBufferB = false;
		
		static bool emuUsingBufferA = false;
		//static bool emuUsingBufferB = false;
		
		static object?[] buffers = new object?[] {null, null};
		
		public static void UseBufferUI(Action<object?> uiCallback) {
			var bufferIndex = uiObtainBufferIndex();
			try     { uiCallback(buffers[bufferIndex]); }
			finally { uiReleaseBuffers(); }
		}
		
		public static void UseBufferEmu(Action<object?> emuCallback) {
			var bufferIndex = emuObtainBufferIndex();
			try     { emuCallback(buffers[bufferIndex]); }
			finally { emuReleaseBuffers(); }
		}
		
		static int uiObtainBufferIndex() {
			lock (commLock) {
				if (!emuUsingBufferA) {
					uiUsingBufferA = true;
					return 0;
				}
				else {
					//uiUsingBufferB = true;
					return 1;
				}
			}
		}
		
		static void uiReleaseBuffers() {
			lock (commLock) {
				uiUsingBufferA = false;
				//uiUsingBufferB = false;
			}
		}
		
		static int emuObtainBufferIndex() {
			lock (commLock) {
				if (!uiUsingBufferA) {
					emuUsingBufferA = true;
					return 0;
				}
				else {
					//emuUsingBufferB = true;
					return 1;
				}
			}
		}
		
		static void emuReleaseBuffers() {
			lock (commLock) {
				emuUsingBufferA = false;
				//emuUsingBufferB = false;
			}
		}
	}
	
	public static void Setup(Emulator emulator, Action uiCallback) {
		emu = emulator;
		
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
	
			while (true) {
				while (!signal) {
					Thread.Sleep(1); // 1 millisecond sleep to reduce CPU load
				}
				
				// TODO: Retrieve whichever data processed by emu in audio callback is available - Signal when done
				Comm.UseBufferUI(buffer => {
					// ...
				});
				
				signal = false;
				// Do UI display
				uiCallback();
			}
		}
		finally {
			SDL.SDL_CloseAudioDevice(device);
			SDL.SDL_Quit();
		}
	}
	
	// This will be called by SDL when it needs more audio data
	static void Callback(IntPtr userdata, IntPtr stream, int len) {
		var samples = len / sizeof(Int16) / 2;
	
		var approxCycles = (samples - 1) * 64;
		var cycles       = emu.StepNCyclesFast(approxCycles);
	
		if (cycles < approxCycles) {
			//Console.WriteLine($"{cycles} cycles ran out of {approxCycles}");
			throw new Exception();
		}
	
		while (emu.SamplesQueued < samples) {
			emu.StepCycleFast();
		}
	
		var buffer = emu.GetBufferedSamples();
		//Console.WriteLine($"buffered: {buffer.Length}");

		// Copy managed array into unmanaged buffer
		Marshal.Copy(buffer, 0, stream, samples * 2);
		
		// TODO: Transfer data from emu to main thread which the UI requests
		Comm.UseBufferEmu(buffer => {
			// ...
		});
		
		// After that's done, give the main thread the go ahead signal to display said data
		signal = true;
	}
}