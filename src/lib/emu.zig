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