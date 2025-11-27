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
		static bool uiUsingBufferB = false;
		
		static bool emuUsingBufferA = false;
		static bool emuUsingBufferB = false;
		
		static EmuDataContainer[] buffers = new EmuDataContainer[] { new(null), new(null) };
		
		public static void UseBufferUI(Action<EmuDataContainer> uiCallback) {
			var buffer = uiObtainBuffer();
			try     { uiCallback(buffer); }
			finally { uiReleaseBuffers(); }
		}
		
		public static void UseBufferEmu(Action<EmuDataContainer> emuCallback) {
			var buffer = emuObtainBuffer();
			try     { emuCallback(buffer); }
			finally { emuReleaseBuffers(); }
		}
		
		static EmuDataContainer uiObtainBuffer() {
			lock (commLock) {
				if (!emuUsingBufferA && !emuUsingBufferB) {
					if (buffers[0].Buffer is null) {
						uiUsingBufferB = true;
						return buffers[1];
					}
					else if (buffers[1].Buffer is null) {
						uiUsingBufferA = true;
						return buffers[0];
					}
					else {
						// Select the most recently computed buffer
						return buffers[0].Buffer!.DSPCycle >= buffers[1].Buffer!.DSPCycle ? buffers[0] : buffers[1];
					}
				}
				else if (!emuUsingBufferA) {
					uiUsingBufferA = true;
					return buffers[0];
				}
				else {
					uiUsingBufferB = true;
					return buffers[1];
				}
			}
		}
		
		static void uiReleaseBuffers() {
			lock (commLock) {
				uiUsingBufferA = false;
				uiUsingBufferB = false;
			}
		}
		
		static EmuDataContainer emuObtainBuffer() {
			lock (commLock) {
				if (!uiUsingBufferA && !uiUsingBufferB) {
					if (buffers[0].Buffer is null) {
						emuUsingBufferB = true;
						return buffers[1];
					}
					else if (buffers[1].Buffer is null) {
						emuUsingBufferA = true;
						return buffers[0];
					}
					else {
						// Select the least recently computed buffer for overwriting
						return buffers[0].Buffer!.DSPCycle <= buffers[1].Buffer!.DSPCycle ? buffers[0] : buffers[1];
					}
				}
				else if (!uiUsingBufferA) {
					emuUsingBufferA = true;
					return buffers[0];
				}
				else {
					emuUsingBufferB = true;
					return buffers[1];
				}
			}
		}
		
		static void emuReleaseBuffers() {
			lock (commLock) {
				emuUsingBufferA = false;
				emuUsingBufferB = false;
			}
		}
	}
	
	public static void Setup(Emulator emulator, Action<EmuDataBuffer?> uiCallback) {
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
				
				EmuDataBuffer? buffer = null;
				
				// Retrieve whichever data processed by emu in audio callback is available - Signal when done
				Comm.UseBufferUI(container => {
					if (container.Buffer is null) return;
					buffer = container.Buffer.Clone();
				});
				
				signal = false;
				// Do UI display
				uiCallback(buffer);
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
		
		// Transfer data from emu to main thread which the UI requests
		Comm.UseBufferEmu(container => {
			container.Buffer = new EmuGenericBuffer(emu.DSP.CurrentCycle);
		});
		
		// After that's done, give the main thread the go ahead signal to display said data
		signal = true;
	}
}