const std = @import("std");

const Emu          = @import("core/emu.zig").Emu;
const SDSP         = @import("core/s_dsp.zig").SDSP;
const SSMP         = @import("core/s_smp.zig").SSMP;
const Script700    = @import("core/script700.zig").Script700;
const SPCLoadError = @import("core/spc_loader.zig").SPCLoadError;

const main = @import("main.zig");

pub const Error = error {
    multiple_main_emu
};

var _main_emu_instance: ?*Emu           = null;
var _emu_singleton:     ?*Emu.Singleton = null;

var _main_inst_lock = std.Thread.Mutex{};

pub inline fn create(set_as_main_instance: bool) !*Emu {
    try main.validate();

    if (set_as_main_instance and _main_emu_instance != null) {
        return Error.multiple_main_emu;
    }

    const emu = try main.alloc.create(Emu);
    emu.* = Emu.new();

    if (set_as_main_instance) {
        _main_inst_lock.lock();
        defer _main_inst_lock.unlock();

        if (_emu_singleton == null) {
            _emu_singleton = try main.alloc.create(Emu.Singleton);
            _emu_singleton.?.* = .{};
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

    _main_inst_lock.lock();
    const inst = _main_emu_instance;
    _main_inst_lock.unlock();

    return inst;
}

pub inline fn reassign_main_instance(emu_ptr: ?*Emu) !void {
    try main.validate_ptr(Emu, emu_ptr);

    _main_inst_lock.lock();
    defer _main_inst_lock.unlock();

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

pub inline fn step_n_cycles(cycles: u32, emu_ptr: ?*Emu, comptime bp_enabled: bool) i32 {
    var ep = get_ptr(emu_ptr);
    main.validate_ptr(Emu, ep) catch { return 0; };

    var completed_cycles: i32 = 0;
    for (0..cycles) |_| {
        ep.?.step_cycle_safe() catch { return completed_cycles; };
        completed_cycles += 1;

        // Return negative if breakpoint hit
        if (bp_enabled and ep.?.break_check(true)) {
            return -completed_cycles;
        }
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

pub inline fn toggle_voice(emu_ptr: ?*Emu, index: u3) !void {
    var ep = get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    return ep.?.toggle_voice(index);
}

pub inline fn enable_voice(emu_ptr: ?*Emu, index: u3) !void {
    var ep = get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    return ep.?.enable_voice(index);
}

pub inline fn disable_voice(emu_ptr: ?*Emu, index: u3) !void {
    var ep = get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    return ep.?.disable_voice(index);
}

pub inline fn toggle_main_voice(emu_ptr: ?*Emu, index: u3) !void {
    var ep = get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    return ep.?.toggle_main_voice(index);
}

pub inline fn enable_main_voice(emu_ptr: ?*Emu, index: u3) !void {
    var ep = get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    return ep.?.enable_main_voice(index);
}

pub inline fn disable_main_voice(emu_ptr: ?*Emu, index: u3) !void {
    var ep = get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    return ep.?.disable_main_voice(index);
}

pub inline fn toggle_echo_voice(emu_ptr: ?*Emu, index: u3) !void {
    var ep = get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    return ep.?.toggle_echo_voice(index);
}

pub inline fn enable_echo_voice(emu_ptr: ?*Emu, index: u3) !void {
    var ep = get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    return ep.?.enable_echo_voice(index);
}

pub inline fn disable_echo_voice(emu_ptr: ?*Emu, index: u3) !void {
    var ep = get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    return ep.?.disable_echo_voice(index);
}

pub inline fn check_breakpoint(emu_ptr: ?*Emu) !bool {
    var ep = get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    return ep.?.break_check(false);
}

pub inline fn consume_breakpoint(emu_ptr: ?*Emu) !bool {
    var ep = get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    return ep.?.break_check(true);
}

pub inline fn copy(dest_emu_ptr: ?*Emu, src_emu_ptr: ?*const Emu) !void {
    var ep = get_ptr(dest_emu_ptr);
    try main.validate_ptr(Emu, ep);

    try main.validate_ptr(Emu, src_emu_ptr);

    try ep.?.load_from(src_emu_ptr.?, .{ .copy_everything = true });
}

pub inline fn acquire_lock(emu_ptr: ?*Emu) !void {
    var ep = get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    ep.?.lock.lock();
}

pub inline fn release_lock(emu_ptr: ?*Emu) !void {
    var ep = get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    ep.?.lock.unlock();
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

pub inline fn get_last_result(emu_ptr: ?*Emu) u32 {
    if (emu_ptr == null) { return 0; }
    const em = emu_ptr.?;

    const last_result = em.lib_result_code;
    em.lib_result_code = @intFromEnum(main.Result.success); // Prepare success result for next API call
    return last_result;
}

pub inline fn get_last_error(emu_ptr: ?*Emu) u32 {
    if (emu_ptr == null) { return 0; }
    const em = emu_ptr.?;

    const last_error = em.lib_error_code;
    em.lib_result_code = @intFromEnum(main.Result.success); // Prepare success result for next API call
    return last_error;
}

// Non export
pub inline fn main_emu_instance() ?*Emu {
    _main_inst_lock.lock();
    const inst = _main_emu_instance;
    _main_inst_lock.unlock();
    
    return inst;
}

pub inline fn get_ptr(emu_ptr: ?*Emu) ?*Emu {
    if (emu_ptr == null) {
        return main_emu_instance();
    }
    else {
        return emu_ptr;
    }
}

pub inline fn set_last_result_code(code: main.Result, em: *Emu) void {
    if (code == .success) {
        return;
    }

    em.lib_result_code = @intFromEnum(code);
    em.lib_error_code  = @intFromEnum(code);
}

pub inline fn err(e: anyerror, emu_ptr: ?*Emu) anyerror {
    if (emu_ptr == null) { return e; }
    const em = emu_ptr.?;

    const code: main.Result =
        switch (e) {
            main.Error.multiple_deinit => .multiple_deinit,
            main.Error.already_inited  => .already_inited,
            main.Error.invalid_state   => .invalid_state,
            main.Error.null_ptr        => .null_ptr,

            Error.multiple_main_emu => .multiple_main_emu,

            std.mem.Allocator.Error.OutOfMemory => .alloc_error,

            SPCLoadError.MissingFileHeader => .spc_missing_file_header,
            SPCLoadError.SizeTooShort      => .spc_size_too_short,

            Emu.Error.Timeout                 => .script700_timeout,
            Script700.Load.bytecode_too_large => .script700_load_error,
            Script700.Load.data_too_large     => .script700_load_error,
            Script700.Load.malformed_file     => .script700_load_error,
            Script700.Compile.no_space        => .script700_compile_error,
            Script700.Compile.unencodable     => .script700_compile_error,

            Emu.Error.NoSingletonAttached => .emu_is_not_main,

            else => .unknown_error
        };

    set_last_result_code(code, em);

    return e;
}

pub inline fn ferr(e: anyerror, emu_ptr: ?*Emu) bool {
    _ = @intFromError(err(e, emu_ptr)); // Need to convert error to something else to avoid compiler error about error discard
    return false;
}

pub inline fn nerr(comptime T: type, e: anyerror, emu_ptr: ?*Emu) ?T {
    _ = @intFromError(err(e, emu_ptr));
    return null;
}

pub inline fn zerr(comptime T: type, e: anyerror, emu_ptr: ?*Emu) T {
    _ = @intFromError(err(e, emu_ptr));
    return 0;
}

pub inline fn lerr(e: anyerror, emu_ptr: ?*Emu) u64 {
    _ = @intFromError(err(e, emu_ptr));
    return 0xFFFF_FFFF_FFFF_FFFF;
}

pub inline fn derr(comptime T: type, e: anyerror, emu_ptr: ?*Emu) T {
    _ = @intFromError(err(e, emu_ptr));
    return .{};
}