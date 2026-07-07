const Emu = @import("lib/core/emu.zig").Emu;

const main      = @import("lib/main.zig");
const emu       = @import("lib/emu.zig");
const dsp       = @import("lib/dsp.zig");
const smp       = @import("lib/smp.zig");
const spc       = @import("lib/spc.zig");
const spcl      = @import("lib/spc_loader.zig");
const script700 = @import("lib/script700.zig");
const buffer    = @import("lib/buffer.zig");

// Main
pub export fn init() bool {
    main.init() catch |e| { return main.ferr(e); };
    return true;
}

pub export fn deinit() bool {
    main.deinit() catch |e| { return main.ferr(e); };
    return true;
}

pub export fn get_last_result() u32 {
    return main.get_last_result();
}

pub export fn get_last_error() u32 {
    return main.get_last_error();
}

// Emu
pub export fn emu_create(set_as_main_instance: bool) ?[*]Emu {
    return @ptrCast(emu.create(set_as_main_instance) catch |e| main.nerr(*Emu, e));
}

pub export fn emu_get_main_instance() ?[*]Emu {
    return @ptrCast(emu.get_main_instance() catch |e| main.nerr(*Emu, e));
}

pub export fn emu_reassign_main_instance(emu_ptr: ?[*]Emu) bool {
    emu.reassign_main_instance(@ptrCast(emu_ptr)) catch |e| { return main.ferr(e); };
    return true;
}

pub export fn emu_destroy(emu_ptr: ?[*]Emu) bool {
    emu.destroy(@ptrCast(emu_ptr)) catch |e| { return main.ferr(e); };
    return true;
}

pub export fn emu_step_cycle(emu_ptr: ?[*]Emu) bool {
    emu.step_cycle(@ptrCast(emu_ptr)) catch |e| { return emu.ferr(e, @ptrCast(emu_ptr)); };
    return true;
}

pub export fn emu_step_cycle_fast(emu_ptr: ?[*]Emu) bool {
    emu.step_cycle_fast(@ptrCast(emu_ptr)) catch |e| { return emu.ferr(e, @ptrCast(emu_ptr)); };
    return true;
}

pub export fn emu_step_instruction(emu_ptr: ?[*]Emu) bool {
    emu.step_instruction(@ptrCast(emu_ptr)) catch |e| { return emu.ferr(e, @ptrCast(emu_ptr)); };
    return true;
}

pub export fn emu_step_n_cycles(cycles: u32, emu_ptr: ?[*]Emu, bp_enabled: bool) i32 {
    if (bp_enabled) {
        return emu.step_n_cycles(cycles, @ptrCast(emu_ptr), true);
    }
    else {
        return emu.step_n_cycles(cycles, @ptrCast(emu_ptr), false);
    }
}

pub export fn emu_step_n_cycles_fast(cycles: u32, emu_ptr: ?[*]Emu) u32 {
    return emu.step_n_cycles_fast(cycles, @ptrCast(emu_ptr));
}

pub export fn emu_get_render_buffer(channel: u8, emu_ptr: ?[*]Emu) ?[*]i16 {
    const res = emu.get_render_buffer(@intCast(channel & 1), @ptrCast(emu_ptr)) catch |e| emu.nerr([]i16, e, @ptrCast(emu_ptr));
    if (res) |r| {
        return @ptrCast(r.ptr);
    }
    return null;
}

pub export fn emu_get_render_buffer_len(emu_ptr: ?[*]Emu) u32 {
    const res = emu.get_render_buffer(0, @ptrCast(emu_ptr)) catch |e| emu.nerr([]i16, e, @ptrCast(emu_ptr));
    if (res) |r| {
        return @intCast(r.len);
    }
    return 0;
}

pub export fn emu_get_render_position(emu_ptr: ?[*]Emu) u32 {
    return emu.get_render_position(@ptrCast(emu_ptr)) catch |e| emu.zerr(u32, e, @ptrCast(emu_ptr));
}

pub export fn emu_toggle_voice(emu_ptr: ?[*]Emu, index: u8) bool {
    emu.toggle_voice(@ptrCast(emu_ptr), @intCast(index & 7)) catch |e| { return emu.ferr(e, @ptrCast(emu_ptr)); };
    return true;
}

pub export fn emu_enable_voice(emu_ptr: ?[*]Emu, index: u8) bool {
    emu.enable_voice(@ptrCast(emu_ptr), @intCast(index & 7)) catch |e| { return emu.ferr(e, @ptrCast(emu_ptr)); };
    return true;
}

pub export fn emu_disable_voice(emu_ptr: ?[*]Emu, index: u8) bool {
    emu.disable_voice(@ptrCast(emu_ptr), @intCast(index & 7)) catch |e| { return emu.ferr(e, @ptrCast(emu_ptr)); };
    return true;
}

pub export fn emu_toggle_main_voice(emu_ptr: ?[*]Emu, index: u8) bool {
    emu.toggle_main_voice(@ptrCast(emu_ptr), @intCast(index & 7)) catch |e| { return emu.ferr(e, @ptrCast(emu_ptr)); };
    return true;
}

pub export fn emu_enable_main_voice(emu_ptr: ?[*]Emu, index: u8) bool {
    emu.enable_main_voice(@ptrCast(emu_ptr), @intCast(index & 7)) catch |e| { return emu.ferr(e, @ptrCast(emu_ptr)); };
    return true;
}

pub export fn emu_disable_main_voice(emu_ptr: ?[*]Emu, index: u8) bool {
    emu.disable_main_voice(@ptrCast(emu_ptr), @intCast(index & 7)) catch |e| { return emu.ferr(e, @ptrCast(emu_ptr)); };
    return true;
}

pub export fn emu_toggle_echo_voice(emu_ptr: ?[*]Emu, index: u8) bool {
    emu.toggle_echo_voice(@ptrCast(emu_ptr), @intCast(index & 7)) catch |e| { return emu.ferr(e, @ptrCast(emu_ptr)); };
    return true;
}

pub export fn emu_enable_echo_voice(emu_ptr: ?[*]Emu, index: u8) bool {
    emu.enable_echo_voice(@ptrCast(emu_ptr), @intCast(index & 7)) catch |e| { return emu.ferr(e, @ptrCast(emu_ptr)); };
    return true;
}

pub export fn emu_disable_echo_voice(emu_ptr: ?[*]Emu, index: u8) bool {
    emu.disable_echo_voice(@ptrCast(emu_ptr), @intCast(index & 7)) catch |e| { return emu.ferr(e, @ptrCast(emu_ptr)); };
    return true;
}

pub export fn emu_check_breakpoint(emu_ptr: ?[*]Emu) bool {
    return emu.check_breakpoint(@ptrCast(emu_ptr)) catch |e| emu.ferr(e, @ptrCast(emu_ptr));
}

pub export fn emu_consume_breakpoint(emu_ptr: ?[*]Emu) bool {
    return emu.consume_breakpoint(@ptrCast(emu_ptr)) catch |e| return emu.ferr(e, @ptrCast(emu_ptr));
}

pub export fn emu_enable_lowpass(emu_ptr: ?[*]Emu) bool {
    emu.enable_lowpass(@ptrCast(emu_ptr)) catch |e| { return emu.ferr(e, @ptrCast(emu_ptr)); };
    return true;
}

pub export fn emu_disable_lowpass(emu_ptr: ?[*]Emu) bool {
    emu.disable_lowpass(@ptrCast(emu_ptr)) catch |e| { return emu.ferr(e, @ptrCast(emu_ptr)); };
    return true;
}

pub export fn emu_copy(dest_emu_ptr: ?[*]Emu, src_emu_ptr: ?[*]Emu) bool {
    emu.copy(@ptrCast(dest_emu_ptr), @ptrCast(src_emu_ptr)) catch |e| { return emu.ferr(e, @ptrCast(dest_emu_ptr)); };
    return true;
}

pub export fn emu_acquire_lock(emu_ptr: ?[*]Emu) bool {
    emu.acquire_lock(@ptrCast(emu_ptr)) catch |e| { return emu.ferr(e, @ptrCast(emu_ptr)); };
    return true;
}

pub export fn emu_release_lock(emu_ptr: ?[*]Emu) bool {
    emu.release_lock(@ptrCast(emu_ptr)) catch |e| { return emu.ferr(e, @ptrCast(emu_ptr)); };
    return true;
}

pub export fn emu_get_last_result(emu_ptr: ?[*]Emu) u32 {
    return emu.get_last_result(@ptrCast(emu_ptr));
}

pub export fn emu_get_last_error(emu_ptr: ?[*]Emu) u32 {
    return emu.get_last_error(@ptrCast(emu_ptr));
}

// DSP
pub export fn dsp_get_aram_ptr(emu_ptr: ?[*]Emu) ?[*]u8 {
    return @ptrCast(dsp.get_aram_ptr(@ptrCast(emu_ptr)) catch |e| emu.nerr([]u8, e, @ptrCast(emu_ptr)));
}

pub export fn dsp_get_reg_map_ptr(emu_ptr: ?[*]Emu) ?[*]u8 {
    return @ptrCast(dsp.get_reg_map_ptr(@ptrCast(emu_ptr)) catch |e| emu.nerr([]u8, e, @ptrCast(emu_ptr)));
}

pub export fn dsp_get_current_cycle(emu_ptr: ?[*]Emu) u64 {
    return dsp.get_current_cycle(@ptrCast(emu_ptr)) catch |e| emu.lerr(e, @ptrCast(emu_ptr));
}

pub export fn dsp_get_global_state(emu_ptr: ?[*]Emu) dsp.GlobalState {
    return dsp.get_global_state(@ptrCast(emu_ptr)) catch |e| emu.derr(dsp.GlobalState, e, @ptrCast(emu_ptr));
}

pub export fn dsp_get_global_debug_state(emu_ptr: ?[*]Emu) dsp.DebugGlobalState {
    return dsp.get_global_debug_state(@ptrCast(emu_ptr)) catch |e| emu.derr(dsp.DebugGlobalState, e, @ptrCast(emu_ptr));
}

pub export fn dsp_get_voice_state(voice_idx: u8, emu_ptr: ?[*]Emu) dsp.VoiceState {
    return dsp.get_voice_state(@intCast(voice_idx & 7), @ptrCast(emu_ptr)) catch |e| emu.derr(dsp.VoiceState, e, @ptrCast(emu_ptr));
}

pub export fn dsp_get_voice_debug_state(voice_idx: u8, emu_ptr: ?[*]Emu) dsp.DebugVoiceState {
    return dsp.get_voice_debug_state(@intCast(voice_idx & 7), @ptrCast(emu_ptr)) catch |e| emu.derr(dsp.DebugVoiceState, e, @ptrCast(emu_ptr));
}

pub export fn dsp_get_sample_usage_flags(emu_ptr: ?[*]Emu) dsp.SampleUsageFlags {
    return dsp.get_sample_usage_flags(@ptrCast(emu_ptr)) catch |e| emu.derr(dsp.SampleUsageFlags, e, @ptrCast(emu_ptr));
}

pub export fn dsp_reset_sample_usage(sample_id: u8, emu_ptr: ?[*]Emu) bool {
    dsp.reset_sample_usage(sample_id ,@ptrCast(emu_ptr)) catch |e| { return emu.ferr(e, @ptrCast(emu_ptr)); };
    return true;
}

// SMP
pub export fn smp_read_byte(address: u16, emu_ptr: ?[*]Emu) u8 {
    return smp.read_byte(address, @ptrCast(emu_ptr)) catch |e| emu.zerr(u8, e, @ptrCast(emu_ptr));
}

pub export fn smp_read_word(address: u16, emu_ptr: ?[*]Emu) u16 {
    return smp.read_word(address, @ptrCast(emu_ptr)) catch |e| emu.zerr(u16, e, @ptrCast(emu_ptr));
}

pub export fn smp_read_page(address: u16, emu_ptr: ?[*]Emu) smp.MemoryPage {
    var page: smp.MemoryPage = undefined;
    page = smp.read_page(address, @ptrCast(emu_ptr)) catch |e| b: {
        _ = @intFromError(emu.err(e, @ptrCast(emu_ptr)));
        page.is_error = true;
        break :b page;
    };
    return page;
}

pub export fn smp_debug_read_byte(address: u16, emu_ptr: ?[*]Emu) u8 {
    return smp.debug_read_byte(address, @ptrCast(emu_ptr)) catch |e| emu.zerr(u8, e, @ptrCast(emu_ptr));
}

pub export fn smp_debug_read_word(address: u16, emu_ptr: ?[*]Emu) u16 {
    return smp.debug_read_word(address, @ptrCast(emu_ptr)) catch |e| emu.zerr(u16, e, @ptrCast(emu_ptr));
}

pub export fn smp_debug_read_page(address: u16, emu_ptr: ?[*]Emu) smp.MemoryPage {
    var page: smp.MemoryPage = undefined;
    page = smp.debug_read_page(address, @ptrCast(emu_ptr)) catch |e| b: {
        _ = @intFromError(emu.err(e, @ptrCast(emu_ptr)));
        page.is_error = true;
        break :b page;
    };
    return page;
}

pub export fn smp_write_byte(address: u16, value: u8, emu_ptr: ?[*]Emu) bool {
    smp.write_byte(address, value, @ptrCast(emu_ptr)) catch |e| { return emu.ferr(e, @ptrCast(emu_ptr)); };
    return true;
}

pub export fn smp_write_word(address: u16, value: u16, emu_ptr: ?[*]Emu) bool {
    smp.write_word(address, value, @ptrCast(emu_ptr)) catch |e| { return emu.ferr(e, @ptrCast(emu_ptr)); };
    return true;
}

pub export fn smp_get_boot_rom_ptr(emu_ptr: ?[*]Emu) ?[*]const u8 {
    return @ptrCast(smp.get_boot_rom_ptr(@ptrCast(emu_ptr)) catch |e| emu.nerr([]const u8, e, @ptrCast(emu_ptr)));
}

pub export fn smp_get_state(emu_ptr: ?[*]Emu) smp.State {
    return smp.get_state(@ptrCast(emu_ptr)) catch |e| emu.derr(smp.State, e, @ptrCast(emu_ptr));
}

pub export fn smp_enable_logging(emu_ptr: ?[*]Emu) bool {
    smp.enable_logging(@ptrCast(emu_ptr)) catch |e| { return main.ferr(e); };
    return true;
}

pub export fn smp_disable_logging(emu_ptr: ?[*]Emu) bool {
    smp.disable_logging(@ptrCast(emu_ptr)) catch |e| { return main.ferr(e); };
    return true;
}

pub export fn smp_get_access_logs(start_cycle: u64, emu_ptr: ?[*]Emu) smp.LogSlice {
    return smp.get_access_logs(start_cycle, @ptrCast(emu_ptr)) catch |e| emu.derr(smp.LogSlice, e, @ptrCast(emu_ptr));
}

pub export fn smp_free_logs(log_start_ptr: ?[*]smp.Log) bool {
    smp.free_logs(@ptrCast(log_start_ptr)) catch |e| { return main.ferr(e); };
    return true;
}

// SPC
pub export fn spc_get_cpu_state(emu_ptr: ?[*]Emu) spc.State {
    return spc.get_cpu_state(@ptrCast(emu_ptr)) catch |e| emu.derr(spc.State, e, @ptrCast(emu_ptr));
}

// SPC Loader
pub export fn spc_load(file_data: ?[*]const u8, len: u64, emu_ptr: ?[*]Emu) bool {
    // TODO: Null-check file_data
    spcl.load_spc(file_data.?[0..len], @ptrCast(emu_ptr)) catch |e| { return emu.ferr(e, @ptrCast(emu_ptr)); };
    return true;
}

pub export fn spc_get_metadata(emu_ptr: ?[*]Emu) spcl.Metadata {
    return spcl.get_metadata(@ptrCast(emu_ptr)) catch |e| emu.derr(spcl.Metadata, e, @ptrCast(emu_ptr));
}

// Script700
pub export fn script700_get_state(emu_ptr: ?[*]Emu) script700.State {
    return script700.get_state(@ptrCast(emu_ptr)) catch |e| emu.derr(script700.State, e, @ptrCast(emu_ptr));
}

pub export fn script700_disable(emu_ptr: ?[*]Emu) bool {
    script700.disable(@ptrCast(emu_ptr)) catch |e| { return emu.ferr(e, @ptrCast(emu_ptr)); };
    return true;
}

pub export fn script700_load_binary_file(emu_ptr: ?[*]Emu, bin_data: ?[*]const u8, len: u64) bool {
    // TODO: Null-check bin_data
    script700.load_binary_file(@ptrCast(emu_ptr), bin_data.?[0..len]) catch |e| { return emu.ferr(e, @ptrCast(emu_ptr)); };
    return true;
}

pub export fn script700_load_bytecode(emu_ptr: ?[*]Emu, script_bytecode: ?[*]const u32, len: u64) bool {
    // TODO: Null-check script_bytecode
    script700.load_bytecode(@ptrCast(emu_ptr), script_bytecode.?[0..len]) catch |e| { return emu.ferr(e, @ptrCast(emu_ptr)); };
    return true;
}

pub export fn script700_load_data(emu_ptr: ?[*]Emu, data: ?[*]u8, len: u64) bool {
    // TODO: Null-check data
    script700.load_data(@ptrCast(emu_ptr), data.?[0..len]) catch |e| { return emu.ferr(e, @ptrCast(emu_ptr)); };
    return true;
}

pub export fn script700_load_label_addresses(emu_ptr: ?[*]Emu, label_addresses: ?[*]u32, len: u64) bool {
    // TODO: Null-check label_addresses
    script700.load_label_addresses(@ptrCast(emu_ptr), label_addresses.?[0..len]) catch |e| { return emu.ferr(e, @ptrCast(emu_ptr)); };
    return true;
}

pub export fn script700_load_label_remappings(emu_ptr: ?[*]Emu, label_remappings: ?[*]u32, len: u64) bool {
    // TODO: Null-check remappings
    script700.load_label_remappings(@ptrCast(emu_ptr), label_remappings.?[0..len]) catch |e| { return emu.ferr(e, @ptrCast(emu_ptr)); };
    return true;
}

pub export fn script700_is_running(emu_ptr: ?[*]Emu) bool {
    return script700.is_running(@ptrCast(emu_ptr)) catch |e| emu.ferr(e, @ptrCast(emu_ptr));
}

pub export fn script700_get_wait_until_cycle(emu_ptr: ?[*]Emu) u64 {
    return script700.get_wait_until_cycle(@ptrCast(emu_ptr)) catch |e| emu.zerr(?u64, e, @ptrCast(emu_ptr)) orelse 0;
}

pub export fn script700_get_script_bytecode_length(emu_ptr: ?[*]Emu) u32 {
    const result = script700.get_script_bytecode(@ptrCast(emu_ptr)) catch |e| emu.nerr([]const u32, e, @ptrCast(emu_ptr));
    if (result) |r| {
        return @intCast(r.len);
    }
    else {
        return 0;
    }
}

pub export fn script700_get_script_bytecode(emu_ptr: ?[*]Emu) ?[*]const u32 {
    return @ptrCast(script700.get_script_bytecode(@ptrCast(emu_ptr)) catch |e| emu.nerr([]const u32, e, @ptrCast(emu_ptr)));
}

pub export fn script700_get_data_length(emu_ptr: ?[*]Emu) u32 {
    const result = script700.get_data(@ptrCast(emu_ptr)) catch |e| emu.nerr([]u8, e, @ptrCast(emu_ptr));
    if (result) |r| {
        return @intCast(r.len);
    }
    else {
        return 0;
    }
}

pub export fn script700_get_data(emu_ptr: ?[*]Emu) ?[*]u8 {
    return @ptrCast(script700.get_data(@ptrCast(emu_ptr)) catch |e| emu.nerr([]u8, e, @ptrCast(emu_ptr)));
}

pub export fn script700_get_label_addresses(emu_ptr: ?[*]Emu) ?[*]u32 {
    return @ptrCast(script700.get_label_addresses(@ptrCast(emu_ptr)) catch |e| emu.nerr([]u32, e, @ptrCast(emu_ptr)));
}

pub export fn script700_compile_instruction(instr: script700.InstrInfo) script700.CompiledInstr {
    return script700.compile_instruction(instr) catch |e| main.derr(script700.CompiledInstr, e);
}

// Buffer
pub export fn buffer_create(num_bytes: u32) ?[*]u8 {
    return @ptrCast(buffer.create(num_bytes) catch |e| main.nerr([]u8, e));
}

pub export fn buffer_destroy(buf_ptr: ?[*]u8, num_bytes: u32) bool {
    if (buf_ptr == null) {
        return false;
    }
    buffer.destroy(buf_ptr.?[0..num_bytes]) catch |e| { return main.ferr(e); };
    return true;
}