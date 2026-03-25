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

pub const InstrInfo = extern struct {
    mnemonic: [3]u8,

    oper_1_prefix:    [3]u8,
    oper_1_has_value:    u8,
    oper_1_value:        u32,

    operator: u8,

    oper_2_prefix:    [3]u8,
    oper_2_has_value:    u8,
    oper_2_value:        u32,
};

pub const CompiledInstr = extern struct {
    word_data: [4]u32 = .{0, 0, 0, 0},
    length:       u32 = 0,
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

pub inline fn compile_instruction(instr: InstrInfo) !CompiledInstr {
    const mn_len = get_buf_len(&instr.mnemonic);

    const mnemonic = instr.mnemonic[0 .. mn_len];

    const p1_len = get_buf_len(&instr.oper_1_prefix);
    const p2_len = get_buf_len(&instr.oper_2_prefix);

    var oper = Script700.Operands { };

    oper.oper_1_prefix = if (p1_len == 0 and instr.oper_1_prefix[1] == 0) null else instr.oper_1_prefix[0 .. p1_len];
    oper.oper_1_value  = if (instr.oper_1_has_value != 0) instr.oper_1_value else null;

    oper.operator = if (instr.operator == 0) null else instr.operator;

    oper.oper_2_prefix = if (p2_len == 0 and instr.oper_2_prefix[1] == 0) null else instr.oper_2_prefix[0 .. p2_len];
    oper.oper_2_value  = if (instr.oper_2_has_value != 0) instr.oper_2_value else null;

    // Prepare return object
    var result = CompiledInstr { };
    inline for (0..4) |i| {
        result.word_data[i] = 0;
    }
    result.length = 0;

    const res = Script700.compile_instruction(&result.word_data, mnemonic, oper);
    if (res) |r| {
        result.length = @intCast(r.len);
        for (0..r.len) |i| {
            result.word_data[i] = r[i];
        }
        return result;
    }
    else |err| {
        return err;
    }
}

inline fn get_buf_len(buf: []const u8) u32 {
    var len: u32 = 0;

    for (buf) |item| {
        if (item == 0) {
            break;
        }
        len +%= 1;
    }

    return len;
}