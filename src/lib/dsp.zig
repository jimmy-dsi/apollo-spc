const std = @import("std");

const Emu  = @import("core/emu.zig").Emu;
const SDSP = @import("core/s_dsp.zig").SDSP;

const main = @import("main.zig");
const emu  = @import("emu.zig");

pub const GlobalState = extern struct {
    echo_feedback:  ?[*]i8 = null,
    echo_vol_left:  ?[*]i8 = null,
    echo_vol_right: ?[*]i8 = null,

    echo_fir: ?[*]i8 = null,

    esa_page:   ?[*]u8 = null,
    echo_delay: ?[*]u8 = null,

    echo_readonly: ?[*]u8 = null,
    reset:         ?[*]u8 = null,
    mute:          ?[*]u8 = null,
    noise_rate:    ?[*]u8 = null,

    main_vol_left:  ?[*]i8 = null,
    main_vol_right: ?[*]i8 = null,

    brr_bank: ?[*]u8 = null
};

pub const VoiceState = extern struct {
    vol_left:  ?[*]i8 = null,
    vol_right: ?[*]i8 = null,

    pitch:  ?[*]u16 = null,
    source: ?[*]u8  = null,

    adsr_0: ?[*]u8 = null,
    adsr_1: ?[*]u8 = null,
    gain:   ?[*]u8 = null,

    envx: ?[*]u8 = null,

    keyon:  ?[*]u8 = null,
    keyoff: ?[*]u8 = null,

    pitch_mod_on: ?[*]u8 = null,
    noise_on:     ?[*]u8 = null,
    echo_on:      ?[*]u8 = null,
    end:          ?[*]u8 = null
};

pub inline fn get_aram_ptr(emu_ptr: ?*Emu) !?[]u8 {
    var ep = emu.get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    return ep.?.s_dsp.audio_ram[0..];
}

pub inline fn get_reg_map_ptr(emu_ptr: ?*Emu) !?[]u8 {
    var ep = emu.get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    return ep.?.s_dsp.dsp_map[0..];
}

pub inline fn get_current_cycle(emu_ptr: ?*Emu) !u64 {
    var ep = emu.get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    return ep.?.s_dsp.cur_cycle();
}

pub inline fn get_global_state(emu_ptr: ?*Emu) !GlobalState {
    var ep = emu.get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    const state = &ep.?.s_dsp.state;

    return .{
        .echo_feedback  = @ptrCast(&state.echo.feedback),
        .echo_vol_left  = @ptrCast(&state.echo.vol_left),
        .echo_vol_right = @ptrCast(&state.echo.vol_right),

        .echo_fir = @ptrCast(&state.echo.fir[0]),

        .esa_page   = @ptrCast(&state.echo.esa_page),
        .echo_delay = @ptrCast(&state.echo.delay),

        .echo_readonly = @ptrCast(&state.echo.readonly),
        .reset         = @ptrCast(&state.reset),
        .mute          = @ptrCast(&state.mute),
        .noise_rate    = @ptrCast(&state.noise_rate),

        .main_vol_left  = @ptrCast(&state.main_vol_left),
        .main_vol_right = @ptrCast(&state.main_vol_right),

        .brr_bank = @ptrCast(&state.brr_bank)
    };
}

pub inline fn get_voice_state(voice_idx: u3, emu_ptr: ?*Emu) !VoiceState {
    var ep = emu.get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    const voice = &ep.?.s_dsp.state.voice[voice_idx];

    return .{
        .vol_left  = @ptrCast(&voice.vol_left),
        .vol_right = @ptrCast(&voice.vol_right),

        .pitch  = @ptrCast(&voice.pitch),
        .source = @ptrCast(&voice.source),

        .adsr_0 = @ptrCast(&voice.adsr_0),
        .adsr_1 = @ptrCast(&voice.adsr_1),
        .gain   = @ptrCast(&voice.gain),

        .envx = @ptrCast(&voice.envx),

        .keyon  = @ptrCast(&voice.keyon),
        .keyoff = @ptrCast(&voice.keyoff),

        .pitch_mod_on = @ptrCast(&voice.pitch_mod_on),
        .noise_on     = @ptrCast(&voice.noise_on),
        .echo_on      = @ptrCast(&voice.echo_on),
        .end          = @ptrCast(&voice.end)
    };
}