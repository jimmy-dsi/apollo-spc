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

    paused: bool = false,

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

    pub fn pause(self: *SDSP) void {
        self.paused = true;
    }

    pub fn unpause(self: *SDSP) void {
        self.paused = false;
    }

    pub fn proc(self: *SDSP, substate: u32) !void {
        // Main 32-step S-DSP loop. Seems there are some discrepancies with the FullSNES timing diagram and how Ares implements this. Replicating Ares behavior.
        switch (substate) {
            0  => { self.proc_t0();  try self.co.wait(2); },
            1  => { self.proc_t1();  try self.co.wait(2); },
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
        // Reads only grab the value from the DSP register array, even if the underlying internal state value doesn't match this
        // In practice, such mismatches would only ever happen right after reset, before the SPC program runs any DSP initialization code
        // After that, they'll pretty much always be in sync
        return self.dsp_map[address & 0x7F];
    }

    pub fn write(self: *SDSP, address: u8, data: u8) void {
        if (address & 0x80 == 0) {
            // Always write to the DSP register array
            self.dsp_map[address] = data;

            const s = &self.state;
            const idx: u3 = @intCast(address >> 4 & 7);

            // Then copy to internal DSP state values
            switch (address) {
                0x0C => { // MVOLL
                    s.main_vol_left = @bitCast(data);
                },
                0x1C => { // MVOLL
                    s.main_vol_right = @bitCast(data);
                },
                0x2C => { // EVOLL
                    s.echo.vol_left = @bitCast(data);
                },
                0x3C => { // EVOLR
                    s.echo.vol_right = @bitCast(data);
                },
                0x4C => { // KON
                    for (0..8) |bit| {
                        const b: u3 = @intCast(bit);
                        s.voice[b].keyon = @intCast(data >> b & 1);
                    }
                },
                0x5C => { // KOFF
                    for (0..8) |bit| {
                        const b: u3 = @intCast(bit);
                        s.voice[b].keyoff = @intCast(data >> b & 1);
                    }
                },
                0x6C => { // FLG
                    s.noise.output_rate = @intCast(data & 0x1F);
                    s.echo.readonly     = @intCast(data >> 5 & 1);
                    s.mute              = @intCast(data >> 6 & 1);
                    s.reset             = @intCast(data >> 7 & 1);
                },
                0x7C => { // ENDX
                    // The lone exception to the "DSP map value always matches internal state after initialization" rule:
                    // Because this is a register that is meant to be read, writing to it simply resets to zero
                    self.dsp_map[address] = 0x00;
                },
                0x0D => { // EFB
                    s.echo.feedback = @bitCast(data);
                },
                0x2D => { // PMON
                    for (0..8) |bit| {
                        const b: u3 = @intCast(bit);
                        s.voice[b].pitch_mod_on = @intCast(data >> b & 1);
                    }
                    s.voice[0].pitch_mod_on = 0; // Voice 0 does not support modulation
                },
                0x3D => { // NON
                    for (0..8) |bit| {
                        const b: u3 = @intCast(bit);
                        s.voice[b].noise_on = @intCast(data >> b & 1);
                    }
                },
                0x4D => { // EON
                    for (0..8) |bit| {
                        const b: u3 = @intCast(bit);
                        s.voice[b].echo_on = @intCast(data >> b & 1);
                    }
                },
                0x5D => { // DIR
                    s.brr_bank = data;
                },
                0x6D => { // ESA
                    s.echo.esa_page = data;
                },
                0x7D => { // EDL
                    s.echo.delay = @intCast(data & 0xF);
                },
                0x00, 0x10, 0x20, 0x30, 0x40, 0x50, 0x60, 0x70 => { // VxVOLL
                    s.voice[idx].vol_left = @bitCast(data);
                },
                0x01, 0x11, 0x21, 0x31, 0x41, 0x51, 0x61, 0x71 => { // VxVOLR
                    s.voice[idx].vol_right = @bitCast(data);
                },
                0x02, 0x12, 0x22, 0x32, 0x42, 0x52, 0x62, 0x72 => { // VxPITCHL
                    const pitch_hi = s.voice[idx].pitch & 0x3F00;
                    s.voice[idx].pitch = pitch_hi | @as(u14, data);
                },
                0x03, 0x13, 0x23, 0x33, 0x43, 0x53, 0x63, 0x73 => { // VxPITCHH
                    const pitch_lo = s.voice[idx].pitch & 0x00FF;
                    s.voice[idx].pitch = @as(u14, data & 0x3F) << 8 | pitch_lo;
                },
                0x04, 0x14, 0x24, 0x34, 0x44, 0x54, 0x64, 0x74 => { // VxSRCN
                    s.voice[idx].source = data;
                },
                0x05, 0x15, 0x25, 0x35, 0x45, 0x55, 0x65, 0x75 => { // VxADSR0
                    s.voice[idx].adsr_0 = data;
                },
                0x06, 0x16, 0x26, 0x36, 0x46, 0x56, 0x66, 0x76 => { // VxADSR1
                    s.voice[idx].adsr_1 = data;
                },
                0x07, 0x17, 0x27, 0x37, 0x47, 0x57, 0x67, 0x77 => { // VxGAIN
                    s.voice[idx].gain = data;
                },
                0x08, 0x18, 0x28, 0x38, 0x48, 0x58, 0x68, 0x78 => { // VxENVX
                    // TODO
                },
                0x09, 0x19, 0x29, 0x39, 0x49, 0x59, 0x69, 0x79 => { // VxOUTX
                    // TODO
                },
                0x0F, 0x1F, 0x2F, 0x3F, 0x4F, 0x5F, 0x6F, 0x7F => { // FIRx
                    s.echo.fir[idx] = @bitCast(data);
                },
                else => {
                    // Nothing
                }
            }
        }
    }

    pub fn debug_write(self: *SDSP, address: u8, data: u8) void {
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