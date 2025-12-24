const std = @import("std");

const Emu             = @import("core/emu.zig").Emu;
const Script700       = @import("core/script700.zig").Script700;
const Script700Loader = @import("core/script700_loader.zig").Script700Loader;

const main = @import("main.zig");
const emu  = @import("emu.zig");

pub const State = extern struct {
    port_in: ?[*]u8 = null,

    work: ?[*]u32 = null,
    cmp:  ?[*]u32 = null,

    callstack: ?[*]u32 = null,
    sp:        ?[*]u8  = null,
    sp_top:    ?[*]u8  = null,

    callstack_on:  ?[*]u8 = null,
    port_queue_on: ?[*]u8 = null,

    pc:   ?[*]u32 = null,
    step: ?[*]u32 = null,

    cur_cycle:   ?[*]u64 = null,
    begin_cycle: ?[*]u64 = null,
    sync_point:  ?[*]u64 = null,
    last_cycle:  ?[*]u64 = null,

    wait_device: ?[*]u8  = null,
    wait_port:   ?[*]u8  = null,
};

pub inline fn get_state(emu_ptr: ?*Emu) !State {
    var ep = emu.get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    const state = &ep.?.script700.state;

    return .{
        .port_in = @ptrCast(&state.port_in[0]),

        .work = @ptrCast(&state.work[0]),
        .cmp  = @ptrCast(&state .cmp[0]),

        .callstack = @ptrCast(&state.callstack[0]),
        .sp        = @ptrCast(&state.sp),
        .sp_top    = @ptrCast(&state.sp_top),

        .callstack_on  = @ptrCast(&state .callstack_on),
        .port_queue_on = @ptrCast(&state.port_queue_on),

        .pc   = @ptrCast(&state.pc),
        .step = @ptrCast(&state.step),

        .cur_cycle   = @ptrCast(&state.cur_cycle),
        .begin_cycle = @ptrCast(&state.begin_cycle),
        .sync_point  = @ptrCast(&state.sync_point),
        .last_cycle  = @ptrCast(&state.last_cycle),

        .wait_device = @ptrCast(&state.wait_device),
        .wait_port   = @ptrCast(&state.wait_port),
    };
}

pub inline fn disable(emu_ptr: ?*Emu) !void {
    const ep = emu.get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    var s700 = &ep.?.script700;
    s700.enabled = false;
}

pub inline fn load_binary_file(emu_ptr: ?*Emu, bin_data: []const u8) !void {
    const ep = emu.get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    const s700 = &ep.?.script700;
    try Script700Loader.load_script(s700, bin_data);
    s700.enabled = true;
}

pub inline fn load_bytecode(emu_ptr: ?*Emu, script_bytecode: []const u32) !void {
    const ep = emu.get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    var s700 = ep.?.script700;
    try s700.load_bytecode(script_bytecode);
}

pub inline fn load_data(emu_ptr: ?*Emu, data: []u8) !void {
    const ep = emu.get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    var s700 = ep.?.script700;
    s700.load_data(data);
}

pub inline fn load_label_addresses(emu_ptr: ?*Emu, label_addresses: []u32) !void {
    const ep = emu.get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    var s700 = ep.?.script700;
    s700.load_label_addresses(label_addresses);
}

pub inline fn load_label_remappings(emu_ptr: ?*Emu, label_remappings: []u32) !void {
    const ep = emu.get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    var s700 = ep.?.script700;
    s700.load_label_remappings(label_remappings);
}

pub inline fn is_running(emu_ptr: ?*Emu) !bool {
    const ep = emu.get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    return ep.?.script700.enabled;
}

pub inline fn get_wait_until_cycle(emu_ptr: ?*Emu) !?u64 {
    const ep = emu.get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    return ep.?.script700.state.wait_until;
}

pub inline fn get_script_bytecode(emu_ptr: ?*Emu) ![]const u32 {
    const ep = emu.get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    return ep.?.script700.script_bytecode;
}

pub inline fn get_data(emu_ptr: ?*Emu) ![]u8 {
    const ep = emu.get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    return ep.?.script700.data_area;
}

pub inline fn get_label_addresses(emu_ptr: ?*Emu) ![]u32 {
    const ep = emu.get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    return &ep.?.script700.label_addresses;
}