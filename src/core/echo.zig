const DSPStateInternal = @import("dsp_state_int.zig").DSPStateInternal;
const std = @import("std");

fn calc_fir(s: *DSPStateInternal, comptime channel: u1, fir_idx: u3, fir_coef: i8) i17 {
    const e = &s._echo;

    const hist_idx: u3 = e._history_offset +% fir_idx +% 1;
    const samp: i17 =
        if (channel == 0)
            e._history_left[hist_idx]
        else
            e._history_right[hist_idx];

    return @truncate(@as(i32, samp) * @as(i32, fir_coef) >> 6);
}

fn output(s: *DSPStateInternal, comptime channel: u1, mvol: i8, evol: i8) i17 {
    const e = &s._echo;

    const main_out: *i17 = 
        if (channel == 0) &s._main_out_left
        else              &s._main_out_right;

    const echo_in: *i17 =
        if (channel == 0) &e._input_left
        else              &e._input_right;

    const mainvol_output: i16 = @truncate(@as(i32, main_out.*) * @as(i32, mvol) >> 7);
    const echo_output:    i16 = @truncate(@as(i32,  echo_in.*) * @as(i32, evol) >> 7);

    return clamp_i16(@as(i17, mainvol_output) +% @as(i17, echo_output));
}

fn read(s: *DSPStateInternal, comptime channel: u1, aram_echo_0: [*]u8, aram_echo_1: [*]u8) void {
    var e = &s._echo;
    const addr = e._address +% @as(u16, channel) * 2;

    const lo = aram_echo_0[addr];
    const hi = aram_echo_1[addr +% 1];

    const samp_u16: u16 = @as(u16, lo) | @as(u16, hi) << 8;
    const samp_i16: i16 = @bitCast(samp_u16);

    if (channel == 0) {
        e._history_left[e._history_offset] = samp_i16 >> 1;
    }
    else {
        e._history_right[e._history_offset] = samp_i16 >> 1;
    }
}

fn write(s: *DSPStateInternal, comptime channel: u1, aram_echo_0: [*]u8, aram_echo_1: [*]u8) void {
    const e = &s._echo;

    const echo_out: *i17 =
        if (channel == 0) &s._echo_out_left
        else              &s._echo_out_right;

    if (e._readonly == 0) {
        const addr = e._address +% @as(u16, channel) * 2;
        const sample: u17 = @bitCast(echo_out.*);
    
        aram_echo_0[addr]      = @truncate(sample & 0xFF);
        aram_echo_1[addr +% 1] = @truncate(sample >> 8);
    }

    // Reset cumulative echo output - DSP will accumulate from each voice output after this
    echo_out.* = 0;
}

fn clamp_i16(input: i17) i17 {
    if (input > 0x7FFF) {
        return 0x7FFF;
    }
    else if (input < -0x8000) {
        return -0x8000;
    }
    else {
        return input;
    }
}

pub fn step_a(s: *DSPStateInternal, aram_echo_0: [*]u8, aram_echo_1: [*]u8, fir_0: i8) void {
    var e = &s._echo;

    for (0..8) |i| {
        e.__calc_history_left[i]  = 0;
        e.__calc_history_right[i] = 0;
    }

    e.__calc_final_left  = 0;
    e.__calc_final_right = 0;

    // History
    e._history_offset +%= 1;

    e._address = (@as(u16, e._esa_page) << 8) +% e._offset;
    read(s, 0, aram_echo_0, aram_echo_1);

    // FIR - Coefficient 0
    const left:  i17 = calc_fir(s, 0, 0, fir_0);
    const right: i17 = calc_fir(s, 1, 0, fir_0);

    e.__calc_history_left [0] = left;
    e.__calc_history_right[0] = right;

    e._input_left  = left;
    e._input_right = right;
}

pub fn step_b(s: *DSPStateInternal, aram_echo_0: [*]u8, aram_echo_1: [*]u8, fir_1: i8, fir_2: i8) void {
    var e = &s._echo;

    // FIR - Coefficients 1, 2
    var left:  i17 = calc_fir(s, 0, 1, fir_1); 
    var right: i17 = calc_fir(s, 1, 1, fir_1); 

    e.__calc_history_left [1] = e._input_left  +% left;
    e.__calc_history_right[1] = e._input_right +% right;

    left  +%= calc_fir(s, 0, 2, fir_2);
    right +%= calc_fir(s, 1, 2, fir_2);

    e._input_left  +%= left;
    e._input_right +%= right;

    e.__calc_history_left [2] = e._input_left;
    e.__calc_history_right[2] = e._input_right;

    read(s, 1, aram_echo_0, aram_echo_1);
}

pub fn step_c(s: *DSPStateInternal, fir_3: i8, fir_4: i8, fir_5: i8) void {
    var e = &s._echo;

    // FIR - Coefficients 3, 4, 5
    var left:  i17 = calc_fir(s, 0, 3, fir_3);
    var right: i17 = calc_fir(s, 1, 3, fir_3);

    e.__calc_history_left [3] = e._input_left  +% left;
    e.__calc_history_right[3] = e._input_right +% right;

    left  +%= calc_fir(s, 0, 4, fir_4);
    right +%= calc_fir(s, 1, 4, fir_4);

    e.__calc_history_left [4] = e._input_left  +% left;
    e.__calc_history_right[4] = e._input_right +% right;

    left  +%= calc_fir(s, 0, 5, fir_5);
    right +%= calc_fir(s, 1, 5, fir_5);

    e._input_left  +%= left;
    e._input_right +%= right;

    e.__calc_history_left [5] = e._input_left;
    e.__calc_history_right[5] = e._input_right;
}

pub fn step_d(s: *DSPStateInternal, fir_6: i8, fir_7: i8) void {
    var e = &s._echo;

    // FIR - Coefficients 6, 7
    var left:  i17 = e._input_left  +% calc_fir(s, 0, 6, fir_6);
    var right: i17 = e._input_right +% calc_fir(s, 1, 6, fir_6);

    e.__calc_history_left [6] = left;
    e.__calc_history_right[6] = right;

    const left_i16:  i16 = @truncate(left);
    const right_i16: i16 = @truncate(right);

    const fir7_l_i16: i16 = @truncate(calc_fir(s, 0, 7, fir_7));
    const fir7_r_i16: i16 = @truncate(calc_fir(s, 1, 7, fir_7));

    left  = @as(i17,  left_i16) +% @as(i17, fir7_l_i16);
    right = @as(i17, right_i16) +% @as(i17, fir7_r_i16);

    e.__calc_history_left [7] = left;
    e.__calc_history_right[7] = right;

    // Clamp final result from FIR calculations
    e._input_left  = clamp_i16(left)  & ~@as(i17, 1);
    e._input_right = clamp_i16(right) & ~@as(i17, 1);

    e.__calc_final_left  = e._input_left;
    e.__calc_final_right = e._input_right;
}

pub fn step_e(s: *DSPStateInternal, mvoll: i8, evoll: i8, efb: i8) void {
    const e = &s._echo;
    // Store echo left output for next clock tick
    s.__echo_out_left = output(s, 0, mvoll, evoll);

    // Echo feedback: Add cumulative echo-enabled channels outputs to the previous echo input multiplied by the feedback value
    const fb_left:  i16 = @truncate(@as(i32, e._input_left)  * @as(i32, efb) >> 7);
    const fb_right: i16 = @truncate(@as(i32, e._input_right) * @as(i32, efb) >> 7);

    const left:  i17 = s._echo_out_left  +% @as(i17,  fb_left);
    const right: i17 = s._echo_out_right +% @as(i17, fb_right);

    // Store into output buffer, to be written back to the echo buffer
    s._echo_out_left  = clamp_i16(left)  & ~@as(i17, 1);
    s._echo_out_right = clamp_i16(right) & ~@as(i17, 1);
}

pub fn step_f(s: *DSPStateInternal, mvolr: i8, evolr: i8, mute_flg: u1) void {
    var left:  i17 = s.__echo_out_left;
    var right: i17 = output(s, 1, mvolr, evolr);

    // From Ares source: "todo: global muting isn't this simple"
    //                   "(turns DAC on and off or something, causing small ~37-sample pulse when first muted)"
    // Might be worth looking into at some point
    if (mute_flg == 1) {
        left  = 0;
        right = 0;
    }

    s._dac_left  = left;
    s._dac_right = right;
}

pub fn step_g(s: *DSPStateInternal, echo_readonly_flg: u1) void {
    s._echo._readonly = echo_readonly_flg;
}

pub fn step_h(s: *DSPStateInternal, aram_echo_0: [*]u8, aram_echo_1: [*]u8, edl: u4, esa: u8, echo_readonly_flg: u1) void {
    var e = &s._echo;

    e._esa_page = esa;

    // Update buffer size based on edl once the current echo read offset wraps back to zero
    if (e._offset == 0) {
        e._length = @as(u16, edl) << 11; // 1 << 11 is 2048. 2KB buffer unit size for EDL - as expected
    }

    e._offset +%= 4;
    if (e._offset >= e._length) {
        e._offset = 0; // Wrap back around to zero once it reaches the buffer length
    }

    write(s, 0, aram_echo_0, aram_echo_1);
    e._readonly = echo_readonly_flg;
}

pub fn step_i(s: *DSPStateInternal, aram_echo_0: [*]u8, aram_echo_1: [*]u8) void {
    write(s, 1, aram_echo_0, aram_echo_1);
}