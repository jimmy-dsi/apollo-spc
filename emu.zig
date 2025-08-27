const std = @import("std");

const SDSP = @import("s_dsp.zig").SDSP;
const SSMP = @import("s_smp.zig").SSMP;

pub const Emu = struct {
    pub var rand: std.Random = undefined;
    var prng: std.Random.DefaultPrng = undefined;

    s_dsp: SDSP,
    s_smp: SSMP,

    pub fn static_init() void {
        prng = std.Random.DefaultPrng.init(blk: {
            var seed: u64 = undefined;
            std.crypto.random.bytes(std.mem.asBytes(&seed));
            break :blk seed;
        });
        rand = prng.random();
    }

    pub fn new() Emu {
        return Emu {
            .s_dsp = undefined,
            .s_smp = undefined
        };
    }

    pub fn init(self: *Emu, s_dsp: SDSP, s_smp: SSMP) void {
        self.s_dsp = s_dsp;
        self.s_smp = s_smp;
    }

    pub fn step_instruction(self: *Emu) void {
        const last_instr = self.s_smp.instr_counter;
        while (self.s_smp.instr_counter == last_instr) {
            self.step();
        }
    }

    pub fn event_loop(self: *Emu) void {
        for (0..200) |_| {
            self.step();
        }
    }

    pub fn step(self: *Emu) void {
        // Note: Normally the S-DSP and S-SMP steps are "staggered", meaning that on a period of every 2 DSP cycles (or 1 SMP cycle),
        // the first DSP cycle is used to process the S-SMP main loop (the execution of SPC700 instructions and handling of MMIO)
        // and the second DSP cycle is used to process the S-DSP main loop (the raw audio processing).
        // However, according to the ares source code, there is one scenario where the S-SMP run is delayed by a single DSP cycle
        // and overlaps the S-DSP main execution: When the S-SMP reads incoming data from one of the APU IO ports that are sent from the S-CPU.
        // To account for this, we will execute the step function for both the S-DSP and S-SMP every single DSP cycle.
        // All staggering execution delay will be handled in each one's own main loop function.

        self.s_dsp.last_processed_cycle = self.s_dsp.clock_counter;

        self.s_smp.step();
        self.s_dsp.step();

        self.s_dsp.inc_cycle(); // Increment clock counter by 1 DSP cycle.
    }
};
