const std = @import("std");

const Emu              = @import("emu.zig").Emu;
const SSMP             = @import("s_smp.zig").SSMP;
const DSPState         = @import("dsp_state.zig").DSPState;
const DSPStateInternal = @import("dsp_state_int.zig").DSPStateInternal;
const CoManager        = @import("co_mgr.zig").CoManager;
const CoState          = @import("co_state.zig").CoState;

const echo = @import("echo.zig");
const misc = @import("misc.zig");

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
            value.* = Emu.rand.int(u8);
        }

        self.reset();
    }

    pub fn reset(self: *SDSP) void {
        self.co.reset();
        // Reset FLG internal state (soft reset, channel mute, and echo write disable. Noise clock set to frequency %00000)
        self.state.reset = 1;
        self.state.mute  = 1;
        self.state.echo.readonly = 1;
        self.state.noise_rate = 0x00;
        self.state._internal = .{};
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
        const aram = &self.audio_ram;
        const s    = &self.state;
        const v    = &s.voice;
        const n    = self.int();
        const vi   = &n._voice;
        const r    = &self.dsp_map;

        const aram_ref_0: [*]u8 = aram;
        const aram_ref_1: [*]u8 = aram_ref_0 + 1;

        // Main 32-step S-DSP loop. Seems there are some discrepancies with the FullSNES timing diagram and how Ares implements this. Replicating Ares behavior.
        switch (substate) {
            0 => {
                self.proc_t0(
                    // RAM access
                    aram_ref_0, aram_ref_1, // Used for BRR - V1
                    // Register array
                    v[0].vol_right, @intCast(v[1].pitch & 0xFF), v[1].adsr_0
                    // Extras
                );
                try self.co.wait(2);
            },
            1 => {
                self.proc_t1(
                    // RAM access
                    aram[vi[1]._brr_address + vi[1]._brr_offset],
                    aram[vi[1]._brr_address],
                    // Register array
                    &v[1].envx, @intCast(v[1].pitch >> 8), v[1].adsr_1, // Note: Accesses ONE OF: gain/adsr_1. Never both within the same cycle
                                                           v[1].gain,   //       So, still satisfies the max 3 DSP registers per cycle rule
                    // Extras
                    s.reset
                );
                try self.co.wait(2);
            },
            2 => {
                self.proc_t2(
                    // RAM access
                    aram[vi[1]._brr_address + vi[1]._brr_offset + 1],
                    // Register array
                    v[0].envx, v[1].vol_left, v[3].source,
                    // Extras
                    &r[0x7C], // ENDX
                );
                try self.co.wait(2);
            },
            3 => {
                self.proc_t3(
                    // RAM access
                    aram_ref_0, aram_ref_1, // Used for BRR - V2
                    // Register array
                    v[1].vol_right, @intCast(v[2].pitch & 0xFF), v[2].adsr_0,
                    // Extras
                    &r[0x09] // V0OUTX - This is a little weird though. Fullsnes does not consider OUTX or ENVX as part of the "extra" array
                             //          However, it can't be part of the DSP register array either because then there would be 4 DSP accesses in 1 cycle here
                             //          Maybe it doesn't count when it's a write to the program-facing DSP map directly? (as opposed to the internal mirrored values)
                );
                try self.co.wait(2);
            },
            4 => {
                self.proc_t4(
                    // RAM access
                    aram[vi[2]._brr_address + vi[2]._brr_offset],
                    aram[vi[2]._brr_address],
                    // Register array
                    &v[2].envx, @intCast(v[2].pitch >> 8), v[2].adsr_1,
                                                           v[2].gain, 
                    // Extras
                    &r[0x08], s.reset // V0ENVX, FLG
                );
                try self.co.wait(2);
            },
            5 => {
                self.proc_t5(
                    // RAM access
                    aram[vi[2]._brr_address + vi[2]._brr_offset + 1],
                    // Register array
                    v[1].envx, v[2].vol_left, v[4].source,
                    // Extras
                    &r[0x7C], // ENDX
                );
                try self.co.wait(2);
            },
            6 => {
                self.proc_t6(
                    // RAM access
                    aram_ref_0, aram_ref_1, // Used for BRR - V3
                    // Register array
                    v[2].vol_right, @intCast(v[3].pitch & 0xFF), v[3].adsr_0,
                    // Extras
                    &r[0x19], // V1OUTX
                );
                try self.co.wait(2);
            },
            7 => {
                self.proc_t7(
                    // RAM access
                    aram[vi[3]._brr_address + vi[3]._brr_offset],
                    aram[vi[3]._brr_address],
                    // Register array
                    &v[3].envx, @intCast(v[3].pitch >> 8), v[3].adsr_1,
                                                           v[3].gain, 
                    // Extras
                    &r[0x18], s.reset // V1ENVX, FLG
                );
                try self.co.wait(2);
            },
            8 => {
                self.proc_t8(
                    // RAM access
                    aram[vi[3]._brr_address + vi[3]._brr_offset + 1],
                    // Register array
                    v[2].envx, v[3].vol_left, v[5].source,
                    // Extras
                    &r[0x7C], // ENDX
                );
                try self.co.wait(2);
            },
            9 => {
                self.proc_t9(
                    // RAM access
                    aram_ref_0, aram_ref_1, // Used for BRR - V4
                    // Register array
                    v[3].vol_right, @intCast(v[4].pitch & 0xFF), v[4].adsr_0,
                    // Extras
                    &r[0x29], // V2OUTX
                );
                try self.co.wait(2);
            },
            10 => {
                self.proc_t10(
                    // RAM access
                    aram[vi[4]._brr_address + vi[4]._brr_offset],
                    aram[vi[4]._brr_address],
                    // Register array
                    &v[4].envx, @intCast(v[4].pitch >> 8), v[4].adsr_1,
                                                           v[4].gain, 
                    // Extras
                    &r[0x28], s.reset // V2ENVX, FLG
                );
                try self.co.wait(2);
            },
            11 => {
                self.proc_t11(
                    // RAM access
                    aram[vi[4]._brr_address + vi[4]._brr_offset + 1],
                    // Register array
                    v[3].envx, v[4].vol_left, v[6].source,
                    // Extras
                    &r[0x7C], // ENDX
                );
                try self.co.wait(2);
            },
            12 => {
                self.proc_t12(
                    // RAM access
                    aram_ref_0, aram_ref_1, // Used for BRR - V5
                    // Register array
                    v[4].vol_right, @intCast(v[5].pitch & 0xFF), v[5].adsr_0,
                    // Extras
                    &r[0x39], // V3OUTX
                );
                try self.co.wait(2);
            },
            13 => {
                self.proc_t13(
                    // RAM access
                    aram[vi[5]._brr_address + vi[5]._brr_offset],
                    aram[vi[5]._brr_address],
                    // Register array
                    &v[5].envx, @intCast(v[5].pitch >> 8), v[5].adsr_1,
                                                           v[5].gain, 
                    // Extras
                    &r[0x38], s.reset // V3ENVX, FLG
                );
                try self.co.wait(2);
            },
            14 => {
                self.proc_t14(
                    // RAM access
                    aram[vi[5]._brr_address + vi[5]._brr_offset + 1],
                    // Register array
                    v[4].envx, v[5].vol_left, v[7].source,
                    // Extras
                    &r[0x7C], // ENDX
                );
                try self.co.wait(2);
            },
            15 => {
                self.proc_t15(
                    // RAM access
                    aram_ref_0, aram_ref_1, // Used for BRR - V6
                    // Register array
                    v[5].vol_right, @intCast(v[6].pitch & 0xFF), v[6].adsr_0,
                    // Extras
                    &r[0x49], // V4OUTX
                );
                try self.co.wait(2);
            },
            16 => {
                self.proc_t16(
                    // RAM access
                    aram[vi[6]._brr_address + vi[6]._brr_offset],
                    aram[vi[6]._brr_address],
                    // Register array
                    &v[6].envx, @intCast(v[6].pitch >> 8), v[6].adsr_1,
                                                           v[6].gain, 
                    // Extras
                    &r[0x48], s.reset // V4ENVX, FLG
                );
                try self.co.wait(2);
            },
            17 => {
                self.proc_t17(
                    // RAM access
                    aram[vi[6]._brr_address + vi[6]._brr_offset + 1],
                    // Register array
                    v[5].envx, v[6].vol_left, v[0].source,
                    // Extras
                    &r[0x7C], // ENDX
                );
                try self.co.wait(2);
            },
            18 => {
                self.proc_t18(
                    // RAM access
                    aram_ref_0, aram_ref_1, // Used for BRR - V7
                    // Register array
                    v[6].vol_right, @intCast(v[7].pitch & 0xFF), v[7].adsr_0,
                    // Extras
                    &r[0x59], // V5OUTX
                );
                try self.co.wait(2);
            },
            19 => {
                self.proc_t19(
                    // RAM access
                    aram[vi[7]._brr_address + vi[7]._brr_offset],
                    aram[vi[7]._brr_address],
                    // Register array
                    &v[7].envx, @intCast(v[7].pitch >> 8), v[7].adsr_1,
                                                           v[7].gain, 
                    // Extras
                    &r[0x58], s.reset // V5ENVX, FLG
                );
                try self.co.wait(2);
            },
            20 => {
                self.proc_t20(
                    // RAM access
                    aram[vi[7]._brr_address + vi[7]._brr_offset + 1],
                    // Register array
                    v[6].envx, v[7].vol_left, v[1].source,
                    // Extras
                    &r[0x7C], // ENDX
                );
                try self.co.wait(2);
            },
            21 => {
                self.proc_t21(
                    // RAM access
                    aram_ref_0, aram_ref_1, // Used for BRR - V0
                    // Register array
                    v[7].vol_right, @intCast(v[0].pitch & 0xFF), v[0].adsr_0,
                    // Extras
                    &r[0x69], // V6OUTX
                );
                try self.co.wait(2);
            },
            22 => {
                self.proc_t22(
                    // RAM access
                    aram_ref_0, aram_ref_1, // Used for echo
                    // Register array
                    @intCast(v[0].pitch >> 8), s.echo.fir[0],
                    // Extras
                    &r[0x68] // V6ENVX
                );
                try self.co.wait(2);
            },
            23 => {
                self.proc_t23(
                    // RAM access
                    aram_ref_0, aram_ref_1, // Used for echo
                    // Register array
                    v[7].envx, s.echo.fir[1], s.echo.fir[2],
                    // Extras
                    &r[0x7C], // ENDX
                );
                try self.co.wait(2);
            },
            24 => {
                self.proc_t24(
                    // RAM access
                    // Register array
                    s.echo.fir[3], s.echo.fir[4], s.echo.fir[5],
                    // Extras
                    &r[0x79], // V7OUTX
                );
                try self.co.wait(2);
            },
            25 => {
                self.proc_t25(
                    // RAM access
                    aram[vi[0]._brr_address + vi[0]._brr_offset],
                    aram[vi[0]._brr_address],
                    // Register array
                    s.echo.fir[6], s.echo.fir[7],
                    // Extras
                    &r[0x78] // V7ENVX
                );
                try self.co.wait(2);
            },
            26 => {
                self.proc_t26(
                    // RAM access
                    // Register array
                    s.main_vol_left, s.echo.vol_left, s.echo.feedback,
                    // Extras
                );
                try self.co.wait(2);
            },
            27 => {
                var pmon: u8 = 0x00;
                inline for (0..8) |bit| {
                    const b: u3 = @intCast(bit);
                    pmon |= @as(u8, v[b].pitch_mod_on) << b;
                }

                self.proc_t27(
                    // RAM access
                    // Register array
                    s.main_vol_right, s.echo.vol_right, pmon,
                    // Extras
                    s.mute
                );
                try self.co.wait(2);
            },
            28 => {
                var non: u8 = 0x00;
                var eon: u8 = 0x00;

                inline for (0..8) |bit| {
                    const b: u3 = @intCast(bit);
                    non |= @as(u8, v[b].noise_on) << b;
                    eon |= @as(u8, v[b].echo_on) << b;
                }

                self.proc_t28(
                    // RAM access
                    // Register array
                    non, eon, s.brr_bank,
                    // Extras
                    s.echo.readonly
                );
                try self.co.wait(2);
            },
            29 => {
                self.proc_t29(
                    // RAM access
                    aram_ref_0, aram_ref_1, // Used for echo
                    // Register array
                    s.echo.delay, s.echo.esa_page,
                    // Extras
                    s.echo.readonly
                );
                try self.co.wait(2);
            },
            30 => {
                var koff: u8 = 0x00;
                inline for (0..8) |bit| {
                    const b: u3 = @intCast(bit);
                    koff |= @as(u8, v[b].keyoff) << b;
                }

                self.proc_t30(
                    // RAM access
                    aram_ref_0, aram_ref_1, // Used for echo
                    // Register array
                    &v[0].envx, v[0].adsr_1, koff, s.noise_rate,
                                v[0].gain,
                    // Extras
                    s.reset
                );
                try self.co.wait(2);
            },
            31 => {
                self.proc_t31(
                    // RAM access
                    aram[vi[0]._brr_address + vi[0]._brr_offset + 1],
                    // Register array
                    v[0].vol_left, v[2].source,
                    // Extras
                );
                self.co.finish(2);
            },
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
                0x1C => { // MVOLR
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
                        self.state._internal._voice[b].__key_latch = @intCast(data >> b & 1);
                    }
                },
                0x5C => { // KOFF
                    for (0..8) |bit| {
                        const b: u3 = @intCast(bit);
                        s.voice[b].keyoff = @intCast(data >> b & 1);
                    }
                },
                0x6C => { // FLG
                    s.noise_rate    = @intCast(data & 0x1F);
                    s.echo.readonly = @intCast(data >> 5 & 1);
                    s.mute          = @intCast(data >> 6 & 1);
                    s.reset         = @intCast(data >> 7 & 1);
                },
                0x7C => { // ENDX
                    // The lone exception to the "DSP map value always matches internal state after initialization" rule:
                    // Because this is a register that is meant to be read-only, writing to it simply resets to zero
                    for (0..8) |v_idx| {
                        self.state._internal._voice[v_idx].__end = 0;
                    }
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
                    self.state._internal._envx = data;
                },
                0x09, 0x19, 0x29, 0x39, 0x49, 0x59, 0x69, 0x79 => { // VxOUTX
                    self.state._internal._outx = @bitCast(data);
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
        //if (address & 0x80 == 0) {
        //    self.dsp_map[address] = data;
        //}
        self.write(address, data);
    }

    pub inline fn int(self: *SDSP) *DSPStateInternal {
        return &self.state._internal;
    }

    inline fn proc_t0(s: *SDSP,
                      aram_0: [*]u8, aram_1: [*]u8,
                      v0_volr: i8, v1_pitchl: u8, v1_adsr_0: u8) void
    {
        s.int().voice_step_e(0, v0_volr);
        s.int().voice_step_b(1, aram_0, aram_1, v1_pitchl, v1_adsr_0);
    }

    inline fn proc_t1(s: *SDSP,
                      aram_data_0: u8, aram_data_1: u8,
                      v1_envx: *u8, v1_pitchh: u6, v1_adsr_1: u8, v1_gain: u8,
                      flg_r: u1) void
    {
        s.int().voice_step_f();
        s.int().voice_step_c(
            1,
            aram_data_0, aram_data_1,
            v1_envx, v1_pitchh, v1_adsr_1, v1_gain,
            flg_r, &gauss_table
        );
    }

    inline fn proc_t2(s: *SDSP,
                      aram_data_0: u8,
                      v0_envx: u8, v1_voll: i8, v3_srcn: u8,
                      endx: *u8) void
    {
        s.int().voice_step_g(endx, v0_envx);
        s.int().voice_step_d(1, aram_data_0, v1_voll);
        s.int().voice_step_a(v3_srcn);
    }

    inline fn proc_t3(s: *SDSP,
                      aram_0: [*]u8, aram_1: [*]u8,
                      v1_volr: i8, v2_pitchl: u8, v2_adsr_0: u8,
                      outx: *u8) void
    {
        s.int().voice_step_h(outx);
        s.int().voice_step_e(1, v1_volr);
        s.int().voice_step_b(2, aram_0, aram_1, v2_pitchl, v2_adsr_0);
    }

    inline fn proc_t4(s: *SDSP,
                      aram_data_0: u8, aram_data_1: u8,
                      v2_envx: *u8, v2_pitchh: u6, v2_adsr_1: u8, v2_gain: u8,
                      envx: *u8, flg_r: u1) void
    {
        s.int().voice_step_i(envx);
        s.int().voice_step_f();
        s.int().voice_step_c(
            2,
            aram_data_0, aram_data_1,
            v2_envx, v2_pitchh, v2_adsr_1, v2_gain,
            flg_r, &gauss_table
        );
    }

    inline fn proc_t5(s: *SDSP,
                      aram_data_0: u8,
                      v1_envx: u8, v2_voll: i8, v4_srcn: u8,
                      endx: *u8) void
    {
        s.int().voice_step_g(endx, v1_envx);
        s.int().voice_step_d(2, aram_data_0, v2_voll);
        s.int().voice_step_a(v4_srcn);
    }

    inline fn proc_t6(s: *SDSP,
                      aram_0: [*]u8, aram_1: [*]u8,
                      v2_volr: i8, v3_pitchl: u8, v3_adsr_0: u8,
                      outx: *u8) void
    {
        s.int().voice_step_h(outx);
        s.int().voice_step_e(2, v2_volr);
        s.int().voice_step_b(3, aram_0, aram_1, v3_pitchl, v3_adsr_0);
    }

    inline fn proc_t7(s: *SDSP,
                      aram_data_0: u8, aram_data_1: u8,
                      v3_envx: *u8, v3_pitchh: u6, v3_adsr_1: u8, v3_gain: u8,
                      envx: *u8, flg_r: u1) void
    {
        s.int().voice_step_i(envx);
        s.int().voice_step_f();
        s.int().voice_step_c(
            3,
            aram_data_0, aram_data_1,
            v3_envx, v3_pitchh, v3_adsr_1, v3_gain,
            flg_r, &gauss_table
        );
    }

    inline fn proc_t8(s: *SDSP,
                      aram_data_0: u8,
                      v2_envx: u8, v3_voll: i8, v5_srcn: u8,
                      endx: *u8) void
    {
        s.int().voice_step_g(endx, v2_envx);
        s.int().voice_step_d(3, aram_data_0, v3_voll);
        s.int().voice_step_a(v5_srcn);
    }

    inline fn proc_t9(s: *SDSP,
                      aram_0: [*]u8, aram_1: [*]u8,
                      v3_volr: i8, v4_pitchl: u8, v4_adsr_0: u8,
                      outx: *u8) void
    {
        s.int().voice_step_h(outx);
        s.int().voice_step_e(3, v3_volr);
        s.int().voice_step_b(4, aram_0, aram_1, v4_pitchl, v4_adsr_0);
    }

    inline fn proc_t10(s: *SDSP,
                       aram_data_0: u8, aram_data_1: u8,
                       v4_envx: *u8, v4_pitchh: u6, v4_adsr_1: u8, v4_gain: u8,
                       envx: *u8, flg_r: u1) void
    {
        s.int().voice_step_i(envx);
        s.int().voice_step_f();
        s.int().voice_step_c(
            4,
            aram_data_0, aram_data_1,
            v4_envx, v4_pitchh, v4_adsr_1, v4_gain,
            flg_r, &gauss_table
        );
    }

    inline fn proc_t11(s: *SDSP,
                       aram_data_0: u8,
                       v3_envx: u8, v4_voll: i8, v6_srcn: u8,
                       endx: *u8) void
    {
        s.int().voice_step_g(endx, v3_envx);
        s.int().voice_step_d(4, aram_data_0, v4_voll);
        s.int().voice_step_a(v6_srcn);
    }

    inline fn proc_t12(s: *SDSP,
                       aram_0: [*]u8, aram_1: [*]u8,
                       v4_volr: i8, v5_pitchl: u8, v5_adsr_0: u8,
                       outx: *u8) void
    {
        s.int().voice_step_h(outx);
        s.int().voice_step_e(4, v4_volr);
        s.int().voice_step_b(5, aram_0, aram_1, v5_pitchl, v5_adsr_0);
    }

    inline fn proc_t13(s: *SDSP,
                       aram_data_0: u8, aram_data_1: u8,
                       v5_envx: *u8, v5_pitchh: u6, v5_adsr_1: u8, v5_gain: u8,
                       envx: *u8, flg_r: u1) void
    {
        s.int().voice_step_i(envx);
        s.int().voice_step_f();
        s.int().voice_step_c(
            5,
            aram_data_0, aram_data_1,
            v5_envx, v5_pitchh, v5_adsr_1, v5_gain,
            flg_r, &gauss_table
        );
    }

    inline fn proc_t14(s: *SDSP,
                       aram_data_0: u8,
                       v4_envx: u8, v5_voll: i8, v7_srcn: u8,
                       endx: *u8) void
    {
        s.int().voice_step_g(endx, v4_envx);
        s.int().voice_step_d(5, aram_data_0, v5_voll);
        s.int().voice_step_a(v7_srcn);
    }

    inline fn proc_t15(s: *SDSP,
                       aram_0: [*]u8, aram_1: [*]u8,
                       v5_volr: i8, v6_pitchl: u8, v6_adsr_0: u8,
                       outx: *u8) void
    {
        s.int().voice_step_h(outx);
        s.int().voice_step_e(5, v5_volr);
        s.int().voice_step_b(6, aram_0, aram_1, v6_pitchl, v6_adsr_0);
    }

    inline fn proc_t16(s: *SDSP,
                       aram_data_0: u8, aram_data_1: u8,
                       v6_envx: *u8, v6_pitchh: u6, v6_adsr_1: u8, v6_gain: u8,
                       envx: *u8, flg_r: u1) void
    {
        s.int().voice_step_i(envx);
        s.int().voice_step_f();
        s.int().voice_step_c(
            6,
            aram_data_0, aram_data_1,
            v6_envx, v6_pitchh, v6_adsr_1, v6_gain,
            flg_r, &gauss_table
        );
    }

    inline fn proc_t17(s: *SDSP,
                       aram_data_0: u8,
                       v5_envx: u8, v6_voll: i8, v0_srcn: u8,
                       endx: *u8) void
    {
        s.int().voice_step_a(v0_srcn);
        s.int().voice_step_g(endx, v5_envx);
        s.int().voice_step_d(6, aram_data_0, v6_voll);
    }

    inline fn proc_t18(s: *SDSP,
                       aram_0: [*]u8, aram_1: [*]u8,
                       v6_volr: i8, v7_pitchl: u8, v7_adsr_0: u8,
                       outx: *u8) void
    {
        s.int().voice_step_h(outx);
        s.int().voice_step_e(6, v6_volr);
        s.int().voice_step_b(7, aram_0, aram_1, v7_pitchl, v7_adsr_0);
    }

    inline fn proc_t19(s: *SDSP,
                       aram_data_0: u8, aram_data_1: u8,
                       v7_envx: *u8, v7_pitchh: u6, v7_adsr_1: u8, v7_gain: u8,
                       envx: *u8, flg_r: u1) void
    {
        s.int().voice_step_i(envx);
        s.int().voice_step_f();
        s.int().voice_step_c(
            7,
            aram_data_0, aram_data_1,
            v7_envx, v7_pitchh, v7_adsr_1, v7_gain,
            flg_r, &gauss_table
        );
    }

    inline fn proc_t20(s: *SDSP,
                       aram_data_0: u8,
                       v6_envx: u8, v7_voll: i8, v1_srcn: u8,
                       endx: *u8) void
    {
        s.int().voice_step_a(v1_srcn);
        s.int().voice_step_g(endx, v6_envx);
        s.int().voice_step_d(7, aram_data_0, v7_voll);
    }

    inline fn proc_t21(s: *SDSP,
                       aram_0: [*]u8, aram_1: [*]u8,
                       v7_volr: i8, v0_pitchl: u8, v0_adsr_0: u8,
                       outx: *u8) void
    {
        s.int().voice_step_h(outx);
        s.int().voice_step_e(7, v7_volr);
        s.int().voice_step_b(0, aram_0, aram_1, v0_pitchl, v0_adsr_0);
    }

    inline fn proc_t22(s: *SDSP,
                       aram_echo_0: [*]u8, aram_echo_1: [*]u8,
                       v0_pitchh: u6, fir_0: i8,
                       envx: *u8) void
    {
        s.int().voice_step_c_pt1(v0_pitchh);
        s.int().voice_step_i(envx);
        s.int().voice_step_f();
        echo.step_a(s.int(), aram_echo_0, aram_echo_1, fir_0);
    }

    inline fn proc_t23(s: *SDSP,
                       aram_echo_0: [*]u8, aram_echo_1: [*]u8,
                       v7_envx: u8, fir_1: i8, fir_2: i8,
                       endx: *u8) void
    {
        s.int().voice_step_g(endx, v7_envx);
        echo.step_b(s.int(), aram_echo_0, aram_echo_1, fir_1, fir_2);
    }

    inline fn proc_t24(s: *SDSP,
                       fir_3: i8, fir_4: i8, fir_5: i8,
                       outx: *u8) void
    {
        s.int().voice_step_h(outx);
        echo.step_c(s.int(), fir_3, fir_4, fir_5);
    }

    inline fn proc_t25(s: *SDSP,
                       aram_data_0: u8, aram_data_1: u8,
                       fir_6: i8, fir_7: i8,
                       envx: *u8) void
    {
        s.int().voice_step_c_pt2(aram_data_0, aram_data_1);
        s.int().voice_step_i(envx);
        echo.step_d(s.int(), fir_6, fir_7);
    }

    inline fn proc_t26(s: *SDSP,
                       mvoll: i8, evoll: i8, efb: i8) void
    {
        echo.step_e(s.int(), mvoll, evoll, efb);
    }

    inline fn proc_t27(s: *SDSP,
                       mvolr: i8, evolr: i8, pmon: u8,
                       mute_flg: u1) void
    {
        misc.step_a(s.int(), pmon);
        echo.step_f(s.int(), mvolr, evolr, mute_flg);
        // Output to DAC
        s.emu.queue_dac_sample(
            @intCast(s.int()._main_out_left),
            @intCast(s.int()._main_out_right),
        );
        // Clear output for next sample
        s.int()._main_out_left  = 0;
        s.int()._main_out_right = 0;
    }

    inline fn proc_t28(s: *SDSP,
                       non: u8, eon: u8, dir: u8,
                       echo_readonly_flg: u1) void
    {
        misc.step_b(s.int(), non, eon, dir);
        echo.step_g(s.int(), echo_readonly_flg);
    }

    inline fn proc_t29(s: *SDSP,
                       aram_echo_0: [*]u8, aram_echo_1: [*]u8,
                       edl: u8, esa: u8,
                       echo_readonly_flg: u1) void
    {
        misc.step_c(s.int());
        echo.step_h(s.int(), aram_echo_0, aram_echo_1, edl, esa, echo_readonly_flg);
    }

    inline fn proc_t30(s: *SDSP,
                       aram_echo_0: [*]u8, aram_echo_1: [*]u8,
                       v0_envx: *u8, v0_adsr_1: u8, koff: u8, flg_lsb: u5, v0_gain: u8,
                       flg_r: u1) void
    {
        misc.step_d(s.int(), koff, flg_lsb);
        s.int().voice_step_c_pt3(0, v0_adsr_1, v0_gain, v0_envx, flg_r, &gauss_table);
        echo.step_i(s.int(), aram_echo_0, aram_echo_1);
    }

    inline fn proc_t31(s: *SDSP,
                       aram_data_0: u8,
                       v0_voll: i8, v2_srcn: u8) void
    {
        s.int().voice_step_d(0, aram_data_0, v0_voll);
        s.int().voice_step_a(v2_srcn);
    }
};