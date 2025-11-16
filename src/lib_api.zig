const Emu = @import("lib/core/emu.zig").Emu;

const main = @import("lib/main.zig");
const emu  = @import("lib/emu.zig");
const dsp  = @import("lib/dsp.zig");

// Main
pub export fn init() bool {
    main.init() catch { return false; };
    return true;
}

pub export fn deinit() bool {
    main.deinit() catch { return false; };
    return true;
}

// Emu
pub export fn emu_create(set_as_main_instance: bool) ?[*]Emu {
    return @ptrCast(emu.create(set_as_main_instance) catch null);
}

pub export fn emu_get_main_instance() ?[*]Emu {
    return @ptrCast(emu.get_main_instance() catch null);
}

pub export fn emu_reassign_main_instance(emu_ptr: ?[*]Emu) bool {
    emu.reassign_main_instance(@ptrCast(emu_ptr)) catch { return false; };
    return true;
}

pub export fn emu_destroy(emu_ptr: ?[*]Emu) bool {
    emu.destroy(@ptrCast(emu_ptr)) catch { return false; };
    return true;
}

// DSP
pub export fn dsp_get_aram_ptr(emu_ptr: ?[*]Emu) ?[*]u8 {
    return @ptrCast(dsp.get_aram_ptr(@ptrCast(emu_ptr)) catch null);
}

pub export fn dsp_get_current_cycle(emu_ptr: ?[*]Emu) u64 {
    return dsp.get_current_cycle(@ptrCast(emu_ptr)) catch 0xFFFF_FFFF_FFFF_FFFF;
}