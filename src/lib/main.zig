const std = @import("std");

const Emu = @import("core/emu.zig").Emu;

pub const Error = error {
    multiple_deinit,
    already_inited,
    invalid_state,
    null_ptr
};

const State = enum {
    uninited, active, deinited
};

pub var alloc: std.mem.Allocator = undefined;

var _state: State = .uninited;
var _gpa = std.heap.GeneralPurposeAllocator(.{}){};

pub inline fn init() !void {
    if (_state == .active) {
        return Error.already_inited;
    }

    Emu.static_init();

    alloc = _gpa.allocator();
    _state = .active;
}

pub inline fn deinit() !void {
    if (_state == .deinited) {
        return Error.multiple_deinit;
    }

    _ = _gpa.deinit();
    _state = .deinited;
}

// Non export
pub inline fn validate() !void {
    if (_state != .active) {
        return Error.invalid_state;
    }
}

pub inline fn validate_ptr(comptime T: type, ptr: ?*T) !void {
    try validate();

    if (ptr == null) {
        return Error.null_ptr;
    }
}