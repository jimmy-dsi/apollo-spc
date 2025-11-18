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

pub export fn emu_step_cycle(emu_ptr: ?[*]Emu) bool {
    emu.step_cycle(@ptrCast(emu_ptr)) catch { return false; };
    return true;
}

pub export fn emu_step_cycle_fast(emu_ptr: ?[*]Emu) bool {
    emu.step_cycle_fast(@ptrCast(emu_ptr)) catch { return false; };
    return true;
}

pub export fn emu_step_instruction(emu_ptr: ?[*]Emu) bool {
    emu.step_instruction(@ptrCast(emu_ptr)) catch { return false; };
    return true;
}

pub export fn emu_step_n_cycles(cycles: u32, emu_ptr: ?[*]Emu) u32 {
    return emu.step_n_cycles(cycles, @ptrCast(emu_ptr));
}

pub export fn emu_step_n_cycles_fast(cycles: u32, emu_ptr: ?[*]Emu) u32 {
    return emu.step_n_cycles_fast(cycles, @ptrCast(emu_ptr));
}

// DSP
pub export fn dsp_get_aram_ptr(emu_ptr: ?[*]Emu) ?[*]u8 {
    return @ptrCast(dsp.get_aram_ptr(@ptrCast(emu_ptr)) catch null);
}

pub export fn dsp_get_reg_map_ptr(emu_ptr: ?[*]Emu) ?[*]u8 {
    return @ptrCast(dsp.get_reg_map_ptr(@ptrCast(emu_ptr)) catch null);
}

pub export fn dsp_get_current_cycle(emu_ptr: ?[*]Emu) u64 {
    return dsp.get_current_cycle(@ptrCast(emu_ptr)) catch 0xFFFF_FFFF_FFFF_FFFF;
}

pub export fn dsp_get_global_state(emu_ptr: ?*Emu) dsp.GlobalState {
    return dsp.get_global_state(@ptrCast(emu_ptr)) catch .{};
}

pub export fn dsp_get_voice_state(voice_idx: u8, emu_ptr: ?*Emu) dsp.VoiceState {
    return dsp.get_voice_state(@intCast(voice_idx & 7), @ptrCast(emu_ptr)) catch .{};
}