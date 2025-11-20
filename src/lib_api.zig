const Emu = @import("lib/core/emu.zig").Emu;

const main   = @import("lib/main.zig");
const emu    = @import("lib/emu.zig");
const dsp    = @import("lib/dsp.zig");
const smp    = @import("lib/smp.zig");
const buffer = @import("lib/buffer.zig");
const spcl   = @import("lib/spc_loader.zig");

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

pub export fn dsp_get_global_state(emu_ptr: ?[*]Emu) dsp.GlobalState {
    return dsp.get_global_state(@ptrCast(emu_ptr)) catch .{};
}

pub export fn dsp_get_voice_state(voice_idx: u8, emu_ptr: ?[*]Emu) dsp.VoiceState {
    return dsp.get_voice_state(@intCast(voice_idx & 7), @ptrCast(emu_ptr)) catch .{};
}

// SMP
pub export fn smp_read_byte(address: u16, emu_ptr: ?[*]Emu) u8 {
    return smp.read_byte(address, @ptrCast(emu_ptr)) catch 0;
}

pub export fn smp_read_word(address: u16, emu_ptr: ?[*]Emu) u16 {
    return smp.read_word(address, @ptrCast(emu_ptr)) catch 0;
}

pub export fn smp_read_page(address: u16, emu_ptr: ?[*]Emu) smp.MemoryPage {
    var page: smp.MemoryPage = undefined;
    page = smp.read_page(address, @ptrCast(emu_ptr)) catch b: {
        page.is_error = true;
        break :b page;
    };
    return page;
}

pub export fn smp_debug_read_byte(address: u16, emu_ptr: ?[*]Emu) u8 {
    return smp.debug_read_byte(address, @ptrCast(emu_ptr)) catch 0;
}

pub export fn smp_debug_read_word(address: u16, emu_ptr: ?[*]Emu) u16 {
    return smp.debug_read_word(address, @ptrCast(emu_ptr)) catch 0;
}

pub export fn smp_debug_read_page(address: u16, emu_ptr: ?[*]Emu) smp.MemoryPage {
    var page: smp.MemoryPage = undefined;
    page = smp.debug_read_page(address, @ptrCast(emu_ptr)) catch b: {
        page.is_error = true;
        break :b page;
    };
    return page;
}

pub export fn smp_write_byte(address: u16, value: u8, emu_ptr: ?[*]Emu) bool {
    smp.write_byte(address, value, @ptrCast(emu_ptr)) catch { return false; };
    return true;
}

pub export fn smp_write_word(address: u16, value: u16, emu_ptr: ?[*]Emu) bool {
    smp.write_word(address, value, @ptrCast(emu_ptr)) catch { return false; };
    return true;
}

pub export fn smp_get_boot_rom_ptr(emu_ptr: ?[*]Emu) ?[*]const u8 {
    return @ptrCast(smp.get_boot_rom_ptr(@ptrCast(emu_ptr)) catch null);
}

pub export fn smp_get_state(emu_ptr: ?[*]Emu) smp.State {
    return smp.get_state(@ptrCast(emu_ptr)) catch smp.State { };
}

// Buffer
pub export fn buffer_create(num_bytes: u32) ?[*]u8 {
    return @ptrCast(buffer.create(num_bytes) catch null);
}

pub export fn buffer_destroy(buf_ptr: ?[*]u8, num_bytes: u32) bool {
    if (buf_ptr == null) {
        return false;
    }
    buffer.destroy(buf_ptr.?[0..num_bytes]) catch { return false; };
    return true;
}

// SPC Loader
pub export fn spc_load(file_data: ?[*]const u8, len: u64, emu_ptr: ?[*]Emu) bool {
    // TODO: Null-check file_data
    spcl.load_spc(file_data.?[0..len], @ptrCast(emu_ptr)) catch { return false; };
    return true;
}