const DSPStateInternal = @import("dsp_state_int.zig").DSPStateInternal;

pub fn step_a(s: *DSPStateInternal, aram_echo_0: [*]u8, aram_echo_1: [*]u8, fir_0: i8) void {
    // History
    s._echo._history_offset +%= 1;

    // TODO
    _ = aram_echo_0;
    _ = aram_echo_1;
    _ = fir_0;
}

pub fn step_b(s: *DSPStateInternal, aram_echo_0: [*]u8, aram_echo_1: [*]u8, fir_1: i8, fir_2: i8) void {
    _ = s;
    _ = aram_echo_0;
    _ = aram_echo_1;
    _ = fir_1;
    _ = fir_2;
}


pub fn step_c(s: *DSPStateInternal, fir_3: i8, fir_4: i8, fir_5: i8) void {
    _ = s;
    _ = fir_3;
    _ = fir_4;
    _ = fir_5;
}

pub fn step_d(s: *DSPStateInternal, fir_6: i8, fir_7: i8) void {
    _ = s;
    _ = fir_6;
    _ = fir_7;
}

pub fn step_e(s: *DSPStateInternal, mvoll: i8, evoll: i8, efb: i8) void {
    _ = s;
    _ = mvoll;
    _ = evoll;
    _ = efb;
}

pub fn step_f(s: *DSPStateInternal, mvolr: i8, evolr: i8, mute_flg: u1) void {
    _ = s;
    _ = mvolr;
    _ = evolr;
    _ = mute_flg;
}

pub fn step_g(s: *DSPStateInternal, echo_readonly_flg: u1) void {
    _ = s;
    _ = echo_readonly_flg;
}

pub fn step_h(s: *DSPStateInternal, aram_echo_0: [*]u8, aram_echo_1: [*]u8, edl: u8, esa: u8, echo_readonly_flg: u1) void {
    _ = s;
    _ = aram_echo_0;
    _ = aram_echo_1;
    _ = edl;
    _ = esa;
    _ = echo_readonly_flg;
}

pub fn step_i(s: *DSPStateInternal, aram_echo_0: [*]u8, aram_echo_1: [*]u8) void {
    _ = s;
    _ = aram_echo_0;
    _ = aram_echo_1;
}