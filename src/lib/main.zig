const std = @import("std");

const Emu          = @import("core/emu.zig").Emu;
const SPCLoadError = @import("core/spc_loader.zig").SPCLoadError;

const emu = @import("emu.zig");

pub const Error = error {
    multiple_deinit,
    already_inited,
    invalid_state,
    null_ptr
};

pub const Result = enum(u32) {
    success                 = 0,

    unknown_error           = 1,
                            
    multiple_deinit         = 2,
    already_inited          = 3,
    invalid_state           = 4,
    null_ptr                = 5,
                            
    multiple_main_emu       = 6,
                            
    alloc_error             = 7,
                            
    spc_missing_file_header = 8,
    spc_size_too_short      = 9,
                            
    script700_timeout       = 10,
    script700_load_error    = 11,

    emu_is_not_main         = 12,

    spc_not_loaded          = 13,
};

pub const LogType = enum(u32) {
    none       = 0,
    read       = 1,
    write      = 2,
    exec       = 3,
    fetch      = 4,
    dummy_read = 5,
};

const State = enum {
    uninited, active, deinited
};

pub var alloc: std.mem.Allocator = std.heap.smp_allocator;

var _state: State = .uninited;
//var _gpa = std.heap.SmpAllocator{}; // This type of allocator is thread-safe and high-performance
var _lock = std.Thread.Mutex{};

pub inline fn init() !void {
    if (_state == .active) {
        return err(Error.already_inited);
    }

    Emu.static_init();

    //alloc = _gpa.allocator();
    _state = .active;
}

pub inline fn deinit() !void {
    if (_state == .deinited) {
        return err(Error.multiple_deinit);
    }

    //_ = _gpa.deinit();
    _state = .deinited;
}

pub inline fn get_last_result() u32 {
    if (_lock.tryLock()) { // If we can grab the lock, we can infer there is presently no error
        last_result_code = @intFromEnum(Result.success); // But set it just in case
        _lock.unlock();
        return @intFromEnum(Result.success);
    }
    else {
        const last_result = last_result_code;
        last_result_code = @intFromEnum(Result.success); // Prepare success result for next API call
        _lock.unlock();
        return last_result;
    }
}

pub inline fn get_last_error() u32 {
    // Assume lock has already been grabbed
    _ = _lock.tryLock(); // But do this just to be safe

    const last_error = last_error_code;
    last_result_code = @intFromEnum(Result.success); // Prepare success result for next API call

    _lock.unlock();

    return last_error;
}

// Non export
pub var last_error_code:  u32 = @intFromEnum(Result.unknown_error);
pub var last_result_code: u32 = @intFromEnum(Result.success);

pub inline fn validate() !void {
    if (_state != .active) {
        return err(Error.invalid_state);
    }
}

pub inline fn validate_ptr(comptime T: type, ptr: ?*T) !void {
    try validate();

    if (ptr == null) {
        return err(Error.null_ptr);
    }
}

pub inline fn set_last_result_code(code: Result) void {
    if (code == .success) {
        return;
    }

    _lock.lock();

    last_result_code = @intFromEnum(code);
    last_error_code  = @intFromEnum(code);
}

pub inline fn err(e: anyerror) anyerror {
    const code: Result =
        switch (e) {
            Error.multiple_deinit => .multiple_deinit,
            Error.already_inited  => .already_inited,
            Error.invalid_state   => .invalid_state,
            Error.null_ptr        => .null_ptr,

            emu.Error.multiple_main_emu => .multiple_main_emu,

            std.mem.Allocator.Error.OutOfMemory => .alloc_error,

            SPCLoadError.MissingFileHeader => .spc_missing_file_header,
            SPCLoadError.SizeTooShort      => .spc_size_too_short,

            Emu.Error.Timeout => .script700_timeout,

            Emu.Error.NoSingletonAttached => .emu_is_not_main,

            else => .unknown_error
        };

    set_last_result_code(code);

    return e;
}

pub inline fn ferr(e: anyerror) bool {
    _ = @intFromError(err(e)); // Need to convert error to something else to avoid compiler error about error discard
    return false;
}

pub inline fn nerr(comptime T: type, e: anyerror) ?T {
    _ = @intFromError(err(e));
    return null;
}

pub inline fn zerr(comptime T: type, e: anyerror) T {
    _ = @intFromError(err(e));
    return 0;
}

pub inline fn lerr(e: anyerror) u64 {
    _ = @intFromError(err(e));
    return 0xFFFF_FFFF_FFFF_FFFF;
}

pub inline fn derr(comptime T: type, e: anyerror) T {
    _ = @intFromError(err(e));
    return .{};
}