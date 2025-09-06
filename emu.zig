const std = @import("std");

const SDSP = @import("s_dsp.zig").SDSP;
const SSMP = @import("s_smp.zig").SSMP;

pub const Emu = struct {
    pub const DebugMode = enum {
        none, shadow_mode, shadow_exec
    };

    pub const DebugModeOptions = struct {
        set_as_master: bool = false,
        force_exit:    bool = false
    };

    pub var rand: std.Random = undefined;
    var prng: std.Random.DefaultPrng = undefined;

    s_dsp: SDSP,
    s_smp: SSMP,

    master_debug_mode: DebugMode = DebugMode.none,
    cur_debug_mode:    DebugMode = DebugMode.none,

    pre_shadow_cycle: u64 = 0,

    debug_persist_shadow_mode:  bool = false,
    debug_persist_spc_state:    bool = false,
    debug_return_on_force_exit: bool = true,

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
            .s_smp = undefined,
            .cur_debug_mode = DebugMode.none,
        };
    }

    pub fn init(self: *Emu, s_dsp: SDSP, s_smp: SSMP) void {
        self.s_dsp          = s_dsp;
        self.s_smp          = s_smp;
        self.cur_debug_mode = DebugMode.none;
    }

    pub fn enable_shadow_mode(self: *Emu, options: DebugModeOptions) void {
        if (options.set_as_master) {
            switch (self.cur_debug_mode) {
                DebugMode.none => {
                    self.s_smp.enable_shadow_execution();
                    self.s_smp.enable_shadow_mode();
                    self.cur_debug_mode = DebugMode.shadow_mode;
                },
                DebugMode.shadow_mode => { },
                DebugMode.shadow_exec => {
                    self.s_smp.enable_shadow_mode();
                    self.cur_debug_mode = DebugMode.shadow_mode;
                },
            }

            self.master_debug_mode = DebugMode.shadow_mode;
        }
        else {
            switch (self.cur_debug_mode) {
                DebugMode.none => {
                    self.s_smp.enable_shadow_execution();
                    self.s_smp.enable_shadow_mode();
                    self.cur_debug_mode = DebugMode.shadow_mode;
                    self.master_debug_mode = DebugMode.shadow_mode;
                },
                DebugMode.shadow_mode => { },
                DebugMode.shadow_exec => {
                    self.s_smp.enable_shadow_mode();
                    self.cur_debug_mode = DebugMode.shadow_mode;
                },
            }
        }
    }

    pub fn enable_shadow_execution(self: *Emu, _: DebugModeOptions) void {
        switch (self.cur_debug_mode) {
            DebugMode.none => {
                self.s_smp.enable_shadow_execution();
                self.cur_debug_mode = DebugMode.shadow_exec;
                self.master_debug_mode = DebugMode.shadow_exec;
            },
            DebugMode.shadow_mode => { },
            DebugMode.shadow_exec => { },
        }
    }

    pub fn disable_shadow_mode(self: *Emu, options: DebugModeOptions) void {
        if (options.set_as_master) {
            switch (self.cur_debug_mode) {
                DebugMode.none => { },
                DebugMode.shadow_mode => {
                    self.cur_debug_mode = DebugMode.shadow_exec;
                    self.master_debug_mode = DebugMode.shadow_exec;
                    self.s_smp.disable_shadow_mode();
                },
                DebugMode.shadow_exec => {
                    self.master_debug_mode = DebugMode.shadow_exec;
                },
            }
        }
        else {
            switch (self.cur_debug_mode) {
                DebugMode.none => { },
                DebugMode.shadow_mode => {
                    self.cur_debug_mode = DebugMode.shadow_exec;
                    self.s_smp.disable_shadow_mode();
                },
                DebugMode.shadow_exec => { },
            }
        }
    }

    pub fn disable_shadow_execution(self: *Emu, options: DebugModeOptions) void {
        switch (self.cur_debug_mode) {
            DebugMode.none => {
                if (!options.force_exit) {
                    self.s_smp.disable_shadow_mode();
                    self.s_smp.disable_shadow_execution(options.force_exit);
                }
            },
            DebugMode.shadow_mode => {
                self.s_smp.disable_shadow_mode();
                self.s_smp.disable_shadow_execution(options.force_exit);
                self.cur_debug_mode = DebugMode.none;
                self.master_debug_mode = DebugMode.none;
            },
            DebugMode.shadow_exec => {
                self.s_smp.disable_shadow_mode();
                self.s_smp.disable_shadow_execution(options.force_exit);
                self.cur_debug_mode = DebugMode.none;
                self.master_debug_mode = DebugMode.none;
            }
        }
    }

    pub fn step_instruction(self: *Emu) void {
        self.step();
        while (!self.s_smp.instr_boundary) {
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

        var null_transition = false;

        if (!self.s_dsp.co.null_transition(.{.no_reset = true})) {
            self.s_smp.step();
        }
        else {
            null_transition = true;
        }

        if (!self.s_smp.co.null_transition(.{.no_reset = true})) {
            if (!self.s_dsp.paused) { // Don't step S-DSP while paused (Shadow Mode enabled)
                self.s_dsp.step();
            }
        }
        else {
            null_transition = true;
        }

        if (!null_transition) {
            self.s_dsp.inc_cycle(); // Increment clock counter by 1 DSP cycle.
        }
    }

    pub fn pause_sdsp(self: *Emu) void {
        self.pre_shadow_cycle = self.s_dsp.clock_counter;
        self.s_dsp.pause();
    }

    pub fn unpause_sdsp(self: *Emu) void {
        self.s_dsp.clock_counter = self.pre_shadow_cycle;
        self.s_dsp.unpause();
    }
};
