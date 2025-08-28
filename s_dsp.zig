const std = @import("std");

const Emu       = @import("emu.zig").Emu;
const SSMP      = @import("s_smp.zig").SSMP;
const DSPState  = @import("dsp_state.zig").DSPState;
const CoManager = @import("co_mgr.zig").CoManager;
const CoState   = @import("co_state.zig").CoState;

const Co = CoState.Co;

pub const SDSP = struct {
    pub const sample_rate:       u32 = 32_000;
    pub const cycles_per_sample: u32 = 64;
    pub const clock_rate:        u32 = sample_rate * cycles_per_sample;

    pub const gauss_table = blk: {
        const embed_u8 = @embedFile("data/gauss.bin");
        var table_u16: [0x200] u16 = undefined;

        @setEvalBranchQuota(embed_u8.len);

        for (0..embed_u8.len) |idx| {
            if (idx % 2 == 1) {
                const b_lo = embed_u8[idx - 1];
                const b_hi = embed_u8[idx];
                const result = b_lo | @as(u16, b_hi) << 8;
                table_u16[idx / 2] = result;
            }
        }

        break :blk table_u16;
    };

    const State = enum {
        init,
        main
    };

    emu: *Emu,

    audio_ram: [0x1_0000] u8 = undefined,
    dsp_map:   [0x80]     u8 = undefined,

    state: DSPState  = undefined,
    co:    CoManager,

    exec_state: State = State.init,

    last_processed_cycle: u64 = 0,
    clock_counter: u64 = 0,

    pub fn new(emu: *Emu) SDSP {
        var s_dsp = SDSP {
            .emu = emu,
            .co  = CoManager.new()
        };
        s_dsp.power_on();
        return s_dsp;
    }

    pub fn power_on(self: *SDSP) void {
        self.exec_state = State.init;

        for (&self.audio_ram) |*value| {
            value.* = Emu.rand.int(u8);
        }

        for (&self.dsp_map) |*value| {
            value.* = 0x00;
        }

        self.reset();
    }

    pub fn reset(self: *SDSP) void {
        self.co.reset();
        // Reset FLG to $E0 (soft reset, channel mute, and echo write disable. Noise clock set to frequency %0000)
        self.dsp_map[0x6C] = 0b1110000;
    }

    pub fn step(self: *SDSP) void {
        if (!self.co.waiting()) {
            self.main() catch {};
        }
        self.co.step();
    }

    pub inline fn cur_cycle(self: *SDSP) u64 {
        return self.clock_counter;
    }

    pub inline fn inc_cycle(self: *SDSP) void {
        self.clock_counter += 1;
    }

    pub inline fn s_smp(self: *const SDSP) *SSMP {
        return &self.emu.*.s_smp;
    }

    pub fn main(self: *SDSP) !void {
        switch (self.exec_state) {
            State.init => {
                // Delay S-DSP execution by 1 DSP cycle
                self.co.finish(1);
                self.exec_state = State.main;
            },
            State.main => {
                const substate = self.co.substate();
                try self.proc(substate);
                //std.debug.print("Finished S-DSP sample loop\n", .{});
            }
        }
    }

    pub fn proc(self: *SDSP, substate: u32) !void {
        // Main 32-step S-DSP loop. Seems there are some discrepancies with the FullSNES timing diagram and how Ares implements this. Replicating Ares behavior.
        switch (substate) {
            0  => { self.proc_t1();  try self.co.wait(2); },
            1  => { self.proc_t0();  try self.co.wait(2); },
            2  => { self.proc_t2();  try self.co.wait(2); },
            3  => { self.proc_t3();  try self.co.wait(2); },
            4  => { self.proc_t4();  try self.co.wait(2); },
            5  => { self.proc_t5();  try self.co.wait(2); },
            6  => { self.proc_t6();  try self.co.wait(2); },
            7  => { self.proc_t7();  try self.co.wait(2); },
            8  => { self.proc_t8();  try self.co.wait(2); },
            9  => { self.proc_t9();  try self.co.wait(2); },
            10 => { self.proc_t10(); try self.co.wait(2); },
            11 => { self.proc_t11(); try self.co.wait(2); },
            12 => { self.proc_t12(); try self.co.wait(2); },
            13 => { self.proc_t13(); try self.co.wait(2); },
            14 => { self.proc_t14(); try self.co.wait(2); },
            15 => { self.proc_t15(); try self.co.wait(2); },
            16 => { self.proc_t16(); try self.co.wait(2); },
            17 => { self.proc_t17(); try self.co.wait(2); },
            18 => { self.proc_t18(); try self.co.wait(2); },
            19 => { self.proc_t19(); try self.co.wait(2); },
            20 => { self.proc_t20(); try self.co.wait(2); },
            21 => { self.proc_t21(); try self.co.wait(2); },
            22 => { self.proc_t22(); try self.co.wait(2); },
            23 => { self.proc_t23(); try self.co.wait(2); },
            24 => { self.proc_t24(); try self.co.wait(2); },
            25 => { self.proc_t25(); try self.co.wait(2); },
            26 => { self.proc_t26(); try self.co.wait(2); },
            27 => { self.proc_t27(); try self.co.wait(2); },
            28 => { self.proc_t28(); try self.co.wait(2); },
            29 => { self.proc_t29(); try self.co.wait(2); },
            30 => { self.proc_t30(); try self.co.wait(2); },
            31 => { self.proc_t31();   self.co.finish(2); },
            else => unreachable
        }
    }

    pub fn read(self: *const SDSP, address: u8) u8 {
        return self.dsp_map[address & 0x7F];
    }

    pub fn write(self: *SDSP, address: u8, data: u8) void {
        if (address & 0x80 == 0) {
            self.dsp_map[address] = data;
        }
    }

    inline fn proc_t0(self: *SDSP) void {
        _ = self;
    }

    inline fn proc_t1(self: *SDSP) void {
        _ = self;
    }

    inline fn proc_t2(self: *SDSP) void {
        _ = self;
    }

    inline fn proc_t3(self: *SDSP) void {
        _ = self;
    }

    inline fn proc_t4(self: *SDSP) void {
        _ = self;
    }

    inline fn proc_t5(self: *SDSP) void {
        _ = self;
    }

    inline fn proc_t6(self: *SDSP) void {
        _ = self;
    }

    inline fn proc_t7(self: *SDSP) void {
        _ = self;
    }

    inline fn proc_t8(self: *SDSP) void {
        _ = self;
    }

    inline fn proc_t9(self: *SDSP) void {
        _ = self;
    }

    inline fn proc_t10(self: *SDSP) void {
        _ = self;
    }

    inline fn proc_t11(self: *SDSP) void {
        _ = self;
    }

    inline fn proc_t12(self: *SDSP) void {
        _ = self;
    }

    inline fn proc_t13(self: *SDSP) void {
        _ = self;
    }

    inline fn proc_t14(self: *SDSP) void {
        _ = self;
    }

    inline fn proc_t15(self: *SDSP) void {
        _ = self;
    }

    inline fn proc_t16(self: *SDSP) void {
        _ = self;
    }

    inline fn proc_t17(self: *SDSP) void {
        _ = self;
    }

    inline fn proc_t18(self: *SDSP) void {
        _ = self;
    }

    inline fn proc_t19(self: *SDSP) void {
        _ = self;
    }

    inline fn proc_t20(self: *SDSP) void {
        _ = self;
    }

    inline fn proc_t21(self: *SDSP) void {
        _ = self;
    }

    inline fn proc_t22(self: *SDSP) void {
        _ = self;
    }

    inline fn proc_t23(self: *SDSP) void {
        _ = self;
    }

    inline fn proc_t24(self: *SDSP) void {
        _ = self;
    }

    inline fn proc_t25(self: *SDSP) void {
        _ = self;
    }

    inline fn proc_t26(self: *SDSP) void {
        _ = self;
    }

    inline fn proc_t27(self: *SDSP) void {
        _ = self;
    }

    inline fn proc_t28(self: *SDSP) void {
        _ = self;
    }

    inline fn proc_t29(self: *SDSP) void {
        _ = self;
    }

    inline fn proc_t30(self: *SDSP) void {
        _ = self;
    }

    inline fn proc_t31(self: *SDSP) void {
        _ = self;
    }
};