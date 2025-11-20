const std = @import("std");

const Emu       = @import("core/emu.zig").Emu;
const SDSP      = @import("core/s_dsp.zig").SDSP;
const SSMP      = @import("core/s_smp.zig").SSMP;
const Script700 = @import("core/script700.zig").Script700;

const main = @import("main.zig");

pub const Error = error {
    multiple_main_emu
};

var _main_emu_instance: ?*Emu           = null;
var _emu_singleton:     ?*Emu.Singleton = null;

pub inline fn create(set_as_main_instance: bool) !*Emu {
    try main.validate();

    if (set_as_main_instance and _main_emu_instance != null) {
        return Error.multiple_main_emu;
    }

    const emu = try main.alloc.create(Emu);
    emu.* = Emu.new();

    if (set_as_main_instance) {
        if (_emu_singleton == null) {
            _emu_singleton = try main.alloc.create(Emu.Singleton);
        }

        emu.init(
            SDSP.new(emu),
            SSMP.new(emu, .{}),
            Script700.new(emu),
            _emu_singleton
        );

        _main_emu_instance = emu;
    }
    else {
        emu.init(
            SDSP.new(emu),
            SSMP.new(emu, .{}),
            Script700.new(emu),
            null
        );
    }

    return emu;
}

pub inline fn get_main_instance() !?*Emu {
    try main.validate();
    return _main_emu_instance;
}

pub inline fn reassign_main_instance(emu_ptr: ?*Emu) !void {
    try main.validate_ptr(Emu, emu_ptr);

    const old_main = _main_emu_instance;

    if (old_main == null) {
        if (_emu_singleton == null) {
            _emu_singleton = try main.alloc.create(Emu.Singleton);
        }

        emu_ptr.?.singleton = _emu_singleton;
        _main_emu_instance = emu_ptr;
    }
    else {
        old_main.?.singleton = null;
        _main_emu_instance = emu_ptr;
        emu_ptr.?.singleton = _emu_singleton;
    }
}

pub inline fn step_cycle(emu_ptr: ?*Emu) !void {
    var ep = get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    try ep.?.step_cycle_safe();
}

pub inline fn step_cycle_fast(emu_ptr: ?*Emu) !void {
    var ep = get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    ep.?.step_cycle_fast();
}

pub inline fn step_instruction(emu_ptr: ?*Emu) !void {
    var ep = get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    try ep.?.step_instruction();
}

pub inline fn step_n_cycles(cycles: u32, emu_ptr: ?*Emu) u32 {
    var ep = get_ptr(emu_ptr);
    main.validate_ptr(Emu, ep) catch { return 0; };

    var completed_cycles: u32 = 0;
    for (0..cycles) |_| {
        ep.?.step_cycle_safe() catch { return completed_cycles; };
        completed_cycles += 1;
    }

    return completed_cycles;
}

pub inline fn step_n_cycles_fast(cycles: u32, emu_ptr: ?*Emu) u32 {
    var ep = get_ptr(emu_ptr);
    main.validate_ptr(Emu, ep) catch { return 0; };

    for (0..cycles) |_| {
        ep.?.step_cycle_fast();
    }

    return cycles;
}

pub inline fn get_render_buffer(channel: u1, emu_ptr: ?*Emu) ![]i16 {
    var ep = get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    return ep.?.get_render_buffer(channel);
}

pub inline fn get_render_position(emu_ptr: ?*Emu) !u32 {
    var ep = get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    return ep.?.get_dac_buffer_offset();
}

pub inline fn destroy(emu_ptr: ?*Emu) !void {
    try main.validate_ptr(Emu, emu_ptr);

    if (emu_ptr == _main_emu_instance) {
        main.alloc.destroy(_emu_singleton.?);

        _main_emu_instance = null;
        _emu_singleton     = null;
    }

    main.alloc.destroy(emu_ptr.?);
}

// Non export
pub inline fn main_emu_instance() ?*Emu {
    return _main_emu_instance;
}

pub inline fn get_ptr(emu_ptr: ?*Emu) ?*Emu {
    if (emu_ptr == null) {
        return main_emu_instance();
    }
    else {
        return emu_ptr;
    }
}