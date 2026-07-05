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

pub const DebugGlobalState = extern struct {

};

pub const DebugVoiceState = extern struct {
    buffer:          ?[*]const i16 = null,
    buffer_offset:   ?[*]const u8  = null,
    gaussian_offset: ?[*]const u16 = null,
    brr_address:     ?[*]const u16 = null,
    brr_offset:      ?[*]const u8  = null,
    key_on_delay:    ?[*]const u8  = null,
    env_mode:        ?[*]const u8  = null,
    env_level:       ?[*]const u16 = null,

    gain_env_level:  ?[*]const u8  = null,
    key_latch:       ?[*]const u8  = null,
    key_on:          ?[*]const u8  = null,
    key_off:         ?[*]const u8  = null,
    pitch_mod_on:    ?[*]const u8  = null,
    noise_on:        ?[*]const u8  = null,
    echo_on:         ?[*]const u8  = null,
    end:             ?[*]const u8  = null,
    looped:          ?[*]const u8  = null,
};

pub const SampleUsageFlags = extern struct {
    flags: ?[*]const u8 = null,
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

pub inline fn get_voice_debug_state(voice_idx: u3, emu_ptr: ?*Emu) !DebugVoiceState {
    var ep = emu.get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    const voice = &ep.?.s_dsp.state._internal._voice[voice_idx];

    return .{
        .buffer          = @ptrCast(&voice._buffer),
        .buffer_offset   = @ptrCast(&voice._buffer_offset),
        .gaussian_offset = @ptrCast(&voice._gaussian_offset),
        .brr_address     = @ptrCast(&voice._brr_address),
        .brr_offset      = @ptrCast(&voice._brr_offset),
        .key_on_delay    = @ptrCast(&voice._key_on_delay),
        .env_mode        = @ptrCast(&voice._env_mode),
        .env_level       = @ptrCast(&voice._env_level),

        .gain_env_level  = @ptrCast(&voice.__env_level),
        .key_latch       = @ptrCast(&voice.__key_latch),
        .key_on          = @ptrCast(&voice.__key_on),
        .key_off         = @ptrCast(&voice.__key_off),
        .pitch_mod_on    = @ptrCast(&voice.__pitch_mod_on),
        .noise_on        = @ptrCast(&voice.__noise_on),
        .echo_on         = @ptrCast(&voice.__echo_on),
        .end             = @ptrCast(&voice.__end),
        .looped          = @ptrCast(&voice.__looped),
    };
}

pub inline fn get_sample_usage_flags(emu_ptr: ?*Emu) !SampleUsageFlags {
    var ep = emu.get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    const dsp = &ep.?.s_dsp;

    return .{
        .flags = @ptrCast(&dsp.sample_usage_flags)
    };
}

pub inline fn reset_sample_usage(sample_id: u8, emu_ptr: ?*Emu) !void {
    var ep = emu.get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    const dsp = &ep.?.s_dsp;

    dsp.reset_sample_usage(sample_id);
}