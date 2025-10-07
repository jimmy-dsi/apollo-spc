const DSPStateInternal = @import("dsp_state_int.zig").DSPStateInternal;

const counter = @import("counter.zig");

pub inline fn step_a(s: *DSPStateInternal, pmon: u8) void {
    for (0..8) |idx| {
        const v_idx: u3 = @intCast(idx);
        const v = &s._voice[idx];
        v.__pitch_mod_on = @intCast(pmon >> v_idx & 1);
    }
}

pub inline fn step_b(s: *DSPStateInternal, non: u8, eon: u8, brr_bank: u8) void {
    for (0..8) |idx| {
        const v_idx: u3 = @intCast(idx);
        const v = &s._voice[idx];
        v.__noise_on = @intCast(non >> v_idx & 1);
        v.__echo_on  = @intCast(eon >> v_idx & 1);
    }

    s._brr._bank = brr_bank;
}

pub inline fn step_c(s: *DSPStateInternal) void {
    s._sample_clk ^= 1;
    if (s._sample_clk != 0) { // Clears KON 63 clocks after it was last read
        for (0..8) |idx| {
            const v = &s._voice[idx];
            v.__key_latch &= ~v.__key_on;
        }
    }
}

pub inline fn step_d(s: *DSPStateInternal, koff: u8, noise_freq: u5) void {
    if (s._sample_clk != 0) {
        for (0..8) |idx| {
            const v_idx: u3 = @intCast(idx);
            const v = &s._voice[idx];
            v.__key_on  = v.__key_latch;
            v.__key_off = @intCast(koff >> v_idx & 1);
        }
    }

    counter.tick(s);

    // Update noise
    if (counter.poll(s, noise_freq)) {
        const feedback: u32 = @as(u32, s._noise_lfsr) << 13 ^ @as(u32, s._noise_lfsr) << 14;
        var new_lfsr: u15 = @intCast(feedback & 0x4000);
        new_lfsr |= s._noise_lfsr >> 1;
        s._noise_lfsr = new_lfsr;
    }
}