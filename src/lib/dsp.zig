const std = @import("std");

const Emu  = @import("core/emu.zig").Emu;
const SDSP = @import("core/s_dsp.zig").SDSP;

const main = @import("main.zig");
const emu  = @import("emu.zig");

pub inline fn get_aram_ptr(emu_ptr: ?*Emu) !?[]u8 {
    var ep = get_emu_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    return ep.?.s_dsp.audio_ram[0..];
}

pub inline fn get_current_cycle(emu_ptr: ?*Emu) !u64 {
    var ep = get_emu_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    return ep.?.s_dsp.cur_cycle();
}

inline fn get_emu_ptr(emu_ptr: ?*Emu) ?*Emu {
    if (emu_ptr == null) {
        return emu.main_emu_instance();
    }
    else {
        return emu_ptr;
    }
}