using System.Diagnostics;
using Jimbl;

namespace SpcProgram;

using System.Runtime.InteropServices;

using Apollo;
using SDL2;

public static class Driver {
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
		
		// Run display before first cycle runs, in case it gets stuck in a Script700 infinite loop (we want there to be display instead of black screen)
		uiCallback(null);

		SDL.SDL_AudioSpec want = new();
		want.freq     = 32000;
		want.format   = SDL.AUDIO_S16; // 16-bit signed
		want.channels = 2;             // Stereo
		want.samples  = 512;           // Buffer size in samples
		want.callback = Callback;

		SDL.SDL_SetHint(SDL.SDL_HINT_AUDIO_RESAMPLING_MODE, "3"); // Use highest possible quality resampling if available

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
	
	static bool    paused = false;
	static long    instrStep = 0;
	static UInt16? breakPC = null;
	
	const int MaxConsecutiveTimeouts = 90;
	const int BusyloopReliefMS       = 20;
	
	// This will be called by SDL when it needs more audio data
	static void Callback(IntPtr userdata, IntPtr stream, int len) {
		var numShorts = len / sizeof(Int16);
		Int16[] buffer = new Int16[numShorts];
		
		lock (CliMain.EmuRestoreLock) {
			try {
				if (CliMain.UI_State is CliMain.State.Break) {
					return;
				}
				else if (CliMain.UI_State is not CliMain.State.Normal) {
					if (!Transfer.StepSignal.WaitOne(0)) {
						return;
					}
					
					StepInstruction();
					Transfer.StepSignal.Reset();
					
					instrStep++;
					
					breakPC = null;
					return;
				}
					
				breakPC = null;
				paused = false;
				
				var samples = numShorts / 2;
		
				var approxCycles = (samples - 1) * 64 - cycleSpillOver;
				if (!StepCycles(approxCycles)) {
					return;
				}
				
				if (!StepCycles(() => emu.SamplesQueued < samples)) {
					return;
				}
			
				// Run a random extra number of cycles, between 0 and 63
				// This way the UI display doesn't stay "phase-locked" with DSP pipeline step and look too unnatural
				cycleSpillOver = rng.Next(0, 64);
			
				if (!StepCycles(cycleSpillOver)) {
					return;
				}
		
				buffer = emu.GetBufferedSamples();
			}
			finally {
				// Copy managed array into unmanaged buffer
				Marshal.Copy(buffer, 0, stream, numShorts);
				
				Transfer.SendEmuData(instrStep, breakPC);
				advanceFrame();
			}
		}
	}
	
	static bool StepCycles(int cycles) {
		if (emu.Script700.IsRunning) {
			var completed = emu.StepNCycles(cycles);
			if (completed < 0) {
				CliMain.UI_State = CliMain.State.Break;
				breakPC = emu.SPC.State.InstructionStartPC;
				return false;
			}
			
			var attempts = 1;
			
			while (completed < cycles) {
				Thread.Sleep(BusyloopReliefMS);
				
				if (attempts >= MaxConsecutiveTimeouts) {
					CliMain.UI_State = CliMain.State.NonFatalError;
					return false;
				}
				
				var remainingCycles = cycles - completed;
				var c = emu.StepNCycles(remainingCycles);
				if (c < 0) {
					CliMain.UI_State = CliMain.State.Break;
					breakPC = emu.SPC.State.InstructionStartPC;
					return false;
				}
				
				completed += c;
				
				attempts++;
			}
			
			return true;
		}
		else {
			var completed = emu.StepNCyclesFast(cycles);
			if (completed < cycles) {
				throw new UnreachableException($"Attempted run of {cycles} fast cycles, only {completed} succeeded. This should never happen.");
			}
			return true;
		}
	}
	
	static bool StepCycles(Func<bool> condition) {
		if (emu.Script700.IsRunning) {
			var success = emu.StepCyclesUntil(condition, out var steps, out var breakpoint);
			if (breakpoint) {
				CliMain.UI_State = CliMain.State.Break;
				breakPC = emu.SPC.State.InstructionStartPC;
				return false;
			}
			
			var attempts = 1;
			
			while (!success) {
				Thread.Sleep(BusyloopReliefMS);
				
				if (attempts >= MaxConsecutiveTimeouts) {
					CliMain.UI_State = CliMain.State.NonFatalError;
					return false;
				}
				
				success = emu.StepCyclesUntil(condition, out steps, out breakpoint);
				if (breakpoint) {
					CliMain.UI_State = CliMain.State.Break;
					breakPC = emu.SPC.State.InstructionStartPC;
					return false;
				}
				
				if (steps > 0 && !success) { // Reset attempt counter if there's movement, but it hits a different Script700 snag afterward
					attempts = 1;
				}
				else {
					attempts++;
				}
			}
			
			return true;
		}
		else {
			var success = emu.StepCyclesUntilFast(condition, out _);
			if (!success) {
				throw new UnreachableException($"Attempted run of fast cycles until condition, ended prematurely. This should never happen.");
			}
			return true;
		}
	}
	
	static bool StepInstruction() {
		if (emu.Script700.IsRunning) {
			var success = true;
			try { emu.StepInstruction(consumeBreakpoint: true); } catch (Script700Timeout) { success = false; }
			
			var attempts = 1;
			var lastCycle = emu.DSP.CurrentCycle;
			
			while (!success) {
				Thread.Sleep(BusyloopReliefMS);
				
				if (attempts >= MaxConsecutiveTimeouts) {
					CliMain.FlagStepInTransit();
					return false;
				}
				
				success = true;
				try { emu.StepInstruction(consumeBreakpoint: true); } catch (Script700Timeout) { success = false; }
				
				if (emu.DSP.CurrentCycle > lastCycle && !success) { // Reset attempt counter if there's movement, but it hits a different Script700 snag
					attempts = 1;
				}
				else {
					attempts++;
				}
			}
			
			CliMain.InstrStepInTransit = false;
			return true;
		}
		else {
			emu.StepInstruction();
			
			CliMain.InstrStepInTransit = false;
			return true;
		}
	}
}