const std = @import("std");

const Emu      = @import("core/emu.zig").Emu;
const SPC      = @import("core/spc.zig").SPC;
const SPCState = @import("core/spc_state.zig").SPCState;

const main = @import("main.zig");
const emu  = @import("emu.zig");

pub const State = extern struct {
    a: ?[*]u8 = null,
    x: ?[*]u8 = null,
    y: ?[*]u8 = null,

    sp: ?[*]u8  = null,
    pc: ?[*]u16 = null,

    psw:  ?[*]u8 = null,
    mode: ?[*]u8 = null,

    instruction_start_pc: ?[*]u16 = null,
};

pub inline fn get_cpu_state(emu_ptr: ?*Emu) !State {
    var ep = emu.get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    return .{
        .a = @ptrCast(&ep.?.s_smp.spc.state.a),
        .x = @ptrCast(&ep.?.s_smp.spc.state.x),
        .y = @ptrCast(&ep.?.s_smp.spc.state.y),

        .sp = @ptrCast(&ep.?.s_smp.spc.state.sp),
        .pc = @ptrCast(&ep.?.s_smp.spc.state.pc),

        .psw  = @ptrCast(&ep.?.s_smp.spc.state.psw),
        .mode = @ptrCast(&ep.?.s_smp.spc.state.mode),

        .instruction_start_pc = @ptrCast(&ep.?.s_smp.spc.state.instruction_start_pc),
    };
}