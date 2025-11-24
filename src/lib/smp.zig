const std = @import("std");

const Emu  = @import("core/emu.zig").Emu;
const SSMP = @import("core/s_smp.zig").SSMP;

const main = @import("main.zig");
const emu  = @import("emu.zig");

pub const MemoryPage = extern struct {
    is_error: bool,
    array: [256]u8
};

pub const State = extern struct {
    timer_0_stage_0: ?[*]u8 = null,
    timer_0_stage_1: ?[*]u8 = null,
    timer_0_stage_2: ?[*]u8 = null,

    timer_1_stage_0: ?[*]u8 = null,
    timer_1_stage_1: ?[*]u8 = null,
    timer_1_stage_2: ?[*]u8 = null,

    timer_2_stage_0: ?[*]u8 = null,
    timer_2_stage_1: ?[*]u8 = null,
    timer_2_stage_2: ?[*]u8 = null,

    global_timer_disable: ?[*]u8 = null,
    ram_write_enable:     ?[*]u8 = null,
    ram_disable:          ?[*]u8 = null,
    global_timer_enable:  ?[*]u8 = null,
    ram_waitstates:       ?[*]u8 = null,
    io_waitstates:        ?[*]u8 = null,

    timer_on_flags: ?[*]u8 = null,
    use_boot_rom:   ?[*]u8 = null,

    dsp_address: ?[*]u8 = null,

    input_ports:  ?[*]u8 = null,
    output_ports: ?[*]u8 = null,

    aux: ?[*]u8 = null,

    timer_dividers: ?[*]u8 = null,
    timer_outputs:  ?[*]u8 = null
};

pub const LogSlice = extern struct {
    ptr: ?[*]Log = null,
    size: u32 = 0
};

pub const Log = extern struct {
    type: u32 = 0,

    dsp_cycle:  u64,
    address:    u16,
    
    pre_data:   u8 = 0,
    write_data: u8 = 0,
    post_data:  u8 = 0,
};

pub inline fn read_byte(address: u16, emu_ptr: ?*Emu) !u8 {
    var ep = emu.get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    return ep.?.s_smp.read_data(address);
}

pub inline fn read_word(address: u16, emu_ptr: ?*Emu) !u16 {
    var ep = emu.get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    const lo = ep.?.s_smp.read_data(address);
    const hi = ep.?.s_smp.read_data(address +% 1);

    return @as(u16, lo) | @as(u16, hi) << 8;
}

pub inline fn read_page(address: u16, emu_ptr: ?*Emu) !MemoryPage {
    var ep = emu.get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    var page = MemoryPage {
        .is_error = false,
        .array = undefined
    };

    for (0..256) |i| {
        const ii: u16 = @intCast(i);
        page.array[ii] = ep.?.s_smp.read_data(address +% ii);
    }

    return page;
}

pub inline fn debug_read_byte(address: u16, emu_ptr: ?*Emu) !u8 {
    var ep = emu.get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    return ep.?.s_smp.debug_read_data(address);
}

pub inline fn debug_read_word(address: u16, emu_ptr: ?*Emu) !u16 {
    var ep = emu.get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    const lo = ep.?.s_smp.debug_read_data(address);
    const hi = ep.?.s_smp.debug_read_data(address +% 1);

    return @as(u16, lo) | @as(u16, hi) << 8;
}

pub inline fn debug_read_page(address: u16, emu_ptr: ?*Emu) !MemoryPage {
    var ep = emu.get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    var page = MemoryPage {
        .is_error = false,
        .array = undefined
    };

    for (0..256) |i| {
        const ii: u16 = @intCast(i);
        page.array[ii] = ep.?.s_smp.debug_read_data(address +% ii);
    }

    return page;
}

pub inline fn write_byte(address: u16, value: u8, emu_ptr: ?*Emu) !void {
    var ep = emu.get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    ep.?.s_smp.write_data(address, value);
}

pub inline fn write_word(address: u16, value: u16, emu_ptr: ?*Emu) !void {
    var ep = emu.get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    ep.?.s_smp.write_data(address,      @intCast(value & 0xFF));
    ep.?.s_smp.write_data(address +% 1, @intCast(value >>   8));
}

pub inline fn get_boot_rom_ptr(emu_ptr: ?*Emu) !?[]const u8 {
    const ep = emu.get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    return ep.?.s_smp.boot_rom;
}

pub inline fn get_state(emu_ptr: ?*Emu) !State {
    var ep = emu.get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    const state = &ep.?.s_smp.state;

    return .{
        .timer_0_stage_0 = @ptrCast(&state.timer_states[0].stage_0),
        .timer_0_stage_1 = @ptrCast(&state.timer_states[0].stage_1),
        .timer_0_stage_2 = @ptrCast(&state.timer_states[0].stage_2),

        .timer_1_stage_0 = @ptrCast(&state.timer_states[1].stage_0),
        .timer_1_stage_1 = @ptrCast(&state.timer_states[1].stage_1),
        .timer_1_stage_2 = @ptrCast(&state.timer_states[1].stage_2),

        .timer_2_stage_0 = @ptrCast(&state.timer_states[2].stage_0),
        .timer_2_stage_1 = @ptrCast(&state.timer_states[2].stage_1),
        .timer_2_stage_2 = @ptrCast(&state.timer_states[2].stage_2),

        .global_timer_disable = @ptrCast(&state.global_timer_disable),
        .ram_write_enable     = @ptrCast(&state.ram_write_enable),
        .ram_disable          = @ptrCast(&state.ram_disable),
        .global_timer_enable  = @ptrCast(&state.global_timer_enable),
        .ram_waitstates       = @ptrCast(&state.ram_waitstates),
        .io_waitstates        = @ptrCast(&state.io_waitstates),

        .timer_on_flags = @ptrCast(&state.timer_on_flags[0]),
        .use_boot_rom   = @ptrCast(&state.use_boot_rom),

        .dsp_address = @ptrCast(&state.dsp_address),

        .input_ports  = @ptrCast(&state .input_ports[0]),
        .output_ports = @ptrCast(&state.output_ports[0]),

        .aux = @ptrCast(&state.aux[0]),

        .timer_dividers = @ptrCast(&state.timer_dividers[0]),
        .timer_outputs  = @ptrCast(&state .timer_outputs[0])
    };
}

pub inline fn enable_logging(emu_ptr: ?*Emu) !void {
    var ep = emu.get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    ep.?.s_smp.enable_access_logs = true;
}

pub inline fn disable_logging(emu_ptr: ?*Emu) !void {
    var ep = emu.get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    ep.?.s_smp.enable_access_logs = false;
}

pub inline fn get_access_logs(start_cycle: u64, emu_ptr: ?*Emu) !LogSlice {
    var ep = emu.get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    const logs_ = ep.?.s_smp.get_access_logs_range(start_cycle);

    var logs = logs_;
    var size: u32 = 0;

    while (logs.step()) {
        size += 1;
    }

    const array = if (size > 0)
        try main.alloc.alloc(Log, size)
    else
        try main.alloc.alloc(Log, 1);

    logs = logs_;
    var i: u32 = 0;

    while (logs.step()) {
        const log = logs.value();

        array[i] = .{
            .type = switch (log.type) {
                SSMP.AccessType.none       => @intFromEnum(main.LogType.none),
                SSMP.AccessType.read       => @intFromEnum(main.LogType.read),
                SSMP.AccessType.write      => @intFromEnum(main.LogType.write),
                SSMP.AccessType.exec       => @intFromEnum(main.LogType.exec),
                SSMP.AccessType.fetch      => @intFromEnum(main.LogType.fetch),
                SSMP.AccessType.dummy_read => @intFromEnum(main.LogType.dummy_read),
            },
            .dsp_cycle  = log.dsp_cycle,
            .address    = log.address,
            .pre_data   = log.pre_data   orelse 0,
            .write_data = log.write_data orelse 0,
            .post_data  = log.post_data  orelse 0,
        };

        i += 1;
    }

    return .{
        .ptr  = @ptrCast(array.ptr),
        .size = size,
    };
}

pub inline fn free_logs(log_ptr: ?*Log) !void {
    try main.validate_ptr(Log, log_ptr);
    main.alloc.destroy(log_ptr.?);
}