const std = @import("std");

const State = @import("script700_state.zig").Script700State;
const Emu   = @import("emu.zig").Emu;

pub const Script700 = struct {
    pub const RunOptions = struct {
        max_steps: u32 = 1000
    };

    pub const MemType = enum {
        port_in, port_out,
        work, label,
        aram, xram,
        data, imm
    };

    pub const default_bytecode: [1]u32 = [1]u32 {0x80FFFFFF}; // Preload with single Quit instruction
    pub var   default_data:     [0]u8  = [0]u8 { };

    const allocator = std.heap.page_allocator;

    emu: *Emu,

    enabled: bool = false,

    label_addresses:   [1024]u32 = [_]u32 {0xFFFFFFFF} ** 1024,
    label_remappings: ?[]u32 = null,

    script_bytecode: []const u32 = &default_bytecode,
    data_area:       []      u8  = &default_data,

    self_alloc_data: bool = false,

    state: State = .{},

    pub fn new(emu: *Emu) Script700 {
        return Script700 {
            .emu = emu,
        };
    }

    pub fn init(self: *Script700, script_bytecode: []u32) void {
        self.load_bytecode(script_bytecode);

        for (0..1024) |i| {
            self.label_addresses[i] = 0xFFFFFFFF;
        }

        self.label_remappings = null;
        self.data_area = &default_data;
    }

    pub fn deinit(self: *Script700) void {
        if (self.self_alloc_data) {
            self.data_area = &default_data;
            allocator.free(self.data_area);
        }
    }

    pub fn load_bytecode(self: *Script700, script_bytecode: []u32) void {
        self.enabled = true;
        self.script_bytecode = script_bytecode;
        self.state.reset();
    }

    pub fn load_data(self: *Script700, data: []u8) void {
        self.data_area = data;
    }

    pub fn load_label_addresses(self: *Script700, label_addresses: []u32) void {
        for (label_addresses, 0..) |address, i| {
            const ii = i % 1024;
            self.label_addresses[ii] = address;
        }
    }

    pub fn load_label_remappings(self: *Script700, label_remappings: []u32) void {
        self.label_remappings = label_remappings;
    }

    pub fn run(self: *Script700, options: RunOptions) void {
        self.state.step = 0;
        for (0..options.max_steps) |_| {
            if (!self.enabled or self.state.wait_until != null) {
                return;
            }
            self.step_instruction();
        }
    }

    pub inline fn step_instruction(self: *Script700) void {
        const instr = self.fetch();

        // Apollo Script700 extended instructions
        if ((instr & 0xFF00_0000) >> 24 == 0b11110111) { // Extended commands group 1
            const cmd: u8 = @intCast((instr & 0x00FF_0000) >> 16);
            switch (cmd) {
                0x00 => { // Send interrupt instruction format
                    self.proc_instr_send_int();
                },
                0x01 => { // Send interrupt and wait instruction format
                    self.proc_instr_send_int_wait(instr);
                },
                0x02 => { // Reset interrupt vector instruction format
                    self.proc_instr_reset_vector();
                },
                0x03 => { // Swap CMP1 and CMP2 instruction format
                    self.proc_instr_swap_cmp();
                },
                else => { // Reserved
                    // Do nothing (effective NOP)
                }
            }
        }
        else if ((instr & 0xF800_0000) >> 27 == 0b11011) { // Set interrupt vector instruction format
            self.proc_instr_set_vector(instr);
        }
        // Original Script700 instructions
        else if (instr & 0x8000_0000 == 0) { // General instruction format
            self.proc_instr_general(instr);
        }
        else if ((instr & 0xF000_0000) >> 28 == 0b1001) { // Branch instruction format
            self.proc_instr_branch(instr);
        }
        else if ((instr & 0xE000_0000) >> 29 == 0b110) { // Wait instruction format
            self.proc_instr_wait(instr);
        }
        else if ((instr & 0xF000_0000) >> 28 == 0b1110) { // Breakpoint instruction format
            self.proc_instr_bp(instr);
        }
        else if ((instr & 0xFF00_0000) >> 24 == 0b11110011) { // Set CMPx instruction format (Supplemental command)
            self.proc_instr_set_cmp(instr);
        }
        else if ((instr & 0xFC00_0000) >> 26 == 0b111100) { // Return instruction format
            self.proc_instr_return(instr);
        }
        else if ((instr & 0xFC00_0000) >> 26 == 0b111101) { // Flush instruction format
            self.proc_instr_flush(instr);
        }
        else if (instr & 0xFF80_0000 == 0x8000_0000) { // Designated NOP instruction format
            // Do nothing
        }
        else if (instr & 0xFF80_0000 == 0x8080_0000) { // Quit instruction format
            self.proc_instr_quit();
        }
        else if ((instr & 0xF800_0000) >> 27 == 0b11111) { // Reverse CMP instruction format (Supplemental command)
            self.proc_instr_rev_cmp(instr);
        }
        else {
            // Do nothing (un-designated NOP)
        }

        self.state.step +%= 1;
    }

    inline fn proc_instr_general(self: *Script700, instr: u32) void {
        _ = self;
        _ = instr;
    }

    inline fn proc_instr_branch(self: *Script700, instr: u32) void {
        const cmd: u4 = @intCast(instr & 0x0F00_0000 >> 24);

        const label: u10 = @intCast(
            if (instr & 0x0000_0400 == 0)
                self.read_u32(.work, instr & 7) & 0x03FF
            else
                instr & 0x03FF
        );

        const address = self.label_addresses[label];

        const c1 = self.state.cmp[0];
        const c2 = self.state.cmp[1];

        const c1_s: i32 = @bitCast(c1);
        const c2_s: i32 = @bitCast(c2);

        switch (cmd) {
            0x0 => { // bra
                self.jump(address);
            },
            0x1 => { // beq ([CMP1] == [CMP2])
                if (c1 == c2) {
                    self.jump(address);
                }
            },
            0x2 => { // bne ([CMP1] != [CMP2])
                if (c1 != c2) {
                    self.jump(address);
                }
            },
            0x3 => { // bge ([CMP1] <= [CMP2]) [signed]
                if (c1_s <= c2_s) {
                    self.jump(address);
                }
            },
            0x4 => { // ble ([CMP1] >= [CMP2]) [signed]
                if (c1_s >= c2_s) {
                    self.jump(address);
                }
            },
            0x5 => { // bgt ([CMP1] <  [CMP2]) [signed]
                if (c1_s < c2_s) {
                    self.jump(address);
                }
            },
            0x6 => { // blt ([CMP1] >  [CMP2]) [signed]
                if (c1_s > c2_s) {
                    self.jump(address);
                }
            },
            0x7 => { // bcc ([CMP1] <= [CMP2]) [unsigned]
                if (c1 <= c2) {
                    self.jump(address);
                }
            },
            0x8 => { // blo ([CMP1] >= [CMP2]) [unsigned]
                if (c1 >= c2) {
                    self.jump(address);
                }
            },
            0x9 => { // bhi ([CMP1] <  [CMP2]) [unsigned]
                if (c1 < c2) {
                    self.jump(address);
                }
            },
            0xA => { // bcs ([CMP1] >  [CMP2]) [unsigned]
                if (c1 > c2) {
                    self.jump(address);
                }
            },
            else => {
                // Reserved (No operation)
            }
        }
    }

    inline fn proc_instr_wait(self: *Script700, instr: u32) void {
        _ = self;
        _ = instr;
    }

    inline fn proc_instr_bp(self: *Script700, instr: u32) void {
        _ = self;
        _ = instr;
    }

    inline fn proc_instr_set_cmp(self: *Script700, instr: u32) void {
        const cmp_index: u1 = @intCast(instr >> 23 & 1);
        const imm = self.fetch();

        self.state.cmp[cmp_index] = imm;
    }

    inline fn proc_instr_return(self: *Script700, instr: u32) void {
        const cmd = (instr & 0x0300_0000) >> 24;
        switch (cmd) {
            0 => { // ret (r)
                const cs_index = self.state.sp >> 2;
                self.jump(self.state.callstack[cs_index]);
                if (self.state.sp > 0) {
                    self.state.sp +%= 4;
                }
            },
            1 => { // ret0 (r0)
                self.state.callstack_on = false;
            },
            2 => { // ret1 (r1)
                self.state.callstack_on = true;
            },
            else => unreachable
        }
    }

    inline fn proc_instr_flush(self: *Script700, instr: u32) void {
        const cmd = (instr & 0x0300_0000) >> 24;
        switch (cmd) {
            0 => { // flush (f)
                inline for (0..4) |p| {
                    self.emu.s_smp.state.input_ports[p] = self.state.port_in[p];
                }
                self.state.set_wait_condition(.output, 0, self.state.port_in[0]);
            },
            1 => { // flush0 (f0)
                self.state.port_queue_on = true;
            },
            2 => { // flush1 (f1)
                self.state.port_queue_on = false;
            },
            else => unreachable
        }
    }

    inline fn proc_instr_quit(self: *Script700) void {
        self.enabled = false;
    }

    inline fn proc_instr_rev_cmp(self: *Script700, instr: u32) void {
        _ = self;
        _ = instr;
    }

    inline fn proc_instr_send_int(self: *Script700) void {
        const successful = self.emu.s_smp.spc.trigger_interrupt(null);
        self.state.cmp[1] = if (successful) 1 else 0;
    }

    inline fn proc_instr_send_int_wait(self: *Script700, instr: u32) void {
        const successful = self.emu.s_smp.spc.trigger_interrupt(null);
        self.state.cmp[1] = if (successful) 1 else 0;

        const out_port: u2 = @intCast(instr & 3);

        if (successful) {
            self.state.set_wait_condition(.output, out_port, null);
        }
    }

    inline fn proc_instr_reset_vector(self: *Script700) void {
        self.emu.s_smp.update_interrupt_vector(self.emu.default_interrupt_vector);
    }

    inline fn proc_instr_swap_cmp(self: *Script700) void {
        const prev_cmp_0  = self.state.cmp[0];
        self.state.cmp[0] = self.state.cmp[1];
        self.state.cmp[1] = prev_cmp_0;
    }

    inline fn proc_instr_set_vector(self: *Script700, instr: u32) void {
        const info = decode_type_2_instr(instr);

        var new_vector: u16 = undefined;

        // TODO: Dynamic pointers
        switch (info.memtype) {
            .port_in, .port_out => {
                const value = self.read_u8(info.memtype, @as(u32, info.port_num.?));
                new_vector = @as(u16, value);
            },
            .work => {
                const value = self.read_u32(.work, @as(u32, info.work_addr.?)) & 0xFFFF;
                new_vector = @intCast(value);
            },
            .data => {
                switch (info.data_size.?) {
                    0, 3 => {
                        const value = self.read_u8(.data, @as(u32, info.data_loc.?));
                        new_vector = @as(u16, value);
                    },
                    1 => {
                        const value = self.read_u16(.data, @as(u32, info.data_loc.?));
                        new_vector = value;
                    },
                    2 => {
                        const value = self.read_u32(.data, @as(u32, info.data_loc.?)) & 0xFFFF;
                        new_vector = @intCast(value);
                    }
                }
            },
            .imm => {
                const value = self.fetch() & 0xFFFF;
                new_vector = @intCast(value);
            },
            .aram => {
                switch (info.data_size.?) {
                    0 => {
                        const value = self.read_u8(.aram, @as(u32, info.ram_addr.?));
                        new_vector = @as(u16, value);
                    },
                    1 => {
                        const value = self.read_u16(.aram, @as(u32, info.ram_addr.?));
                        new_vector = value;
                    },
                    2 => {
                        const value = self.read_u32(.aram, @as(u32, info.ram_addr.?)) & 0xFFFF;
                        new_vector = @intCast(value);
                    },
                    3 => unreachable
                }
            },
            .xram => {
                const value = self.read_u8(.xram, @as(u32, info.ram_addr.?));
                new_vector = @as(u16, value);
            },
            .label => {
                const value = self.read_u32(.label, @as(u32, info.label_num.?));
                new_vector = @intCast(value);
            }
        }
    }

    inline fn decode_type_2_instr(instr: u32)
        struct {
            memtype:  MemType,        dyn_ptr:   bool = false,
            port_num:     ?u2 = null, work_addr:  ?u3 =  null,
            label_num:   ?u10 = null, data_size:  ?u2 =  null,
            ram_addr:    ?u16 = null, data_loc:  ?u20 =  null
        }
    {
        const suffix = instr & 0x07FF_FFFF;

        const ident_1: u5  = @intCast(suffix >> 22);
        const ident_2: u22 = @intCast(suffix & 0x3F_FFFF);

        switch (ident_1) {
            0b10000 => { // Data location
                return .{
                    .memtype   = .data,
                    .data_size = @intCast(ident_2 >> 20),
                    .data_loc  = @intCast(ident_2 & 0x0F_FFFF)
                };
            },
            0b10001 => { // Immediate
                return .{
                    .memtype = .imm
                };
            },
            0b10010 => { // SPC700 RAM / XRAM location
                const data_size: u2 = @intCast(ident_2 >> 16 & 0b11);
                if (data_size == 3) {
                    return .{
                        .memtype  = .xram,
                        .ram_addr = @intCast(ident_2 & 0x003F)
                    };
                }
                else {
                    return .{
                        .memtype   = .aram,
                        .data_size = data_size,
                        .ram_addr  = @intCast(ident_2 & 0xFFFF)
                    };
                }
            },
            0b10011 => { // Label number
                return .{
                    .memtype = .label,
                    .label_num = @intCast(ident_2 & 0x3FF)
                };
            },
            0b10100 => { // Dynamic pointer immediate
                return .{
                    .memtype = .imm,
                    .dyn_ptr = true
                };
            },
            0b10101 => { // Dynamic pointer input port
                return .{
                    .memtype = .port_in,
                    .dyn_ptr = true
                };
            },
            0b10110 => { // Dynamic pointer output port
                return .{
                    .memtype = .port_out,
                    .dyn_ptr = true
                };
            },
            0b10111 => { // Dynamic pointer work RAM
                return .{
                    .memtype = .work,
                    .dyn_ptr = true
                };
            },
            0b11000 => { // Dynamic pointer 8-bit SPC700 RAM
                return .{
                    .memtype   = .aram,
                    .dyn_ptr   = true,
                    .data_size = 0
                };
            },
            0b11001 => { // Dynamic pointer 16-bit SPC700 RAM
                return .{
                    .memtype   = .aram,
                    .dyn_ptr   = true,
                    .data_size = 1
                };
            },
            0b11010 => { // Dynamic pointer 32-bit SPC700 RAM
                return .{
                    .memtype   = .aram,
                    .dyn_ptr   = true,
                    .data_size = 2
                };
            },
            0b11011 => { // Dynamic pointer 8-bit SPC700 XRAM
                return .{
                    .memtype = .xram,
                    .dyn_ptr = true
                };
            },
            0b11100 => { // Dynamic pointer 8-bit Data location
                return .{
                    .memtype   = .data,
                    .dyn_ptr   = true,
                    .data_size = 0
                };
            },
            0b11101 => { // Dynamic pointer 16-bit Data location
                return .{
                    .memtype   = .data,
                    .dyn_ptr   = true,
                    .data_size = 1
                };
            },
            0b11110 => { // Dynamic pointer 32-bit Data location
                return .{
                    .memtype   = .data,
                    .dyn_ptr   = true,
                    .data_size = 2
                };
            },
            0b11111 => { // Dynamic pointer label number
                return .{
                    .memtype = .label,
                    .dyn_ptr = true
                };
            },
            else => {
                if ((ident_1 & 0b11100) >> 2 == 0b000) { // Input port
                    return .{
                        .memtype  = .port_in,
                        .port_num = @intCast(ident_1 & 0b11)
                    };
                }
                else if ((ident_1 & 0b11100) >> 2 == 0b001) { // Output port
                    return .{
                        .memtype  = .port_out,
                        .port_num = @intCast(ident_1 & 0b11)
                    };
                }
                else if ((ident_1 & 0b11000) >> 3 == 0b01) { // Work RAM
                    return .{
                        .memtype  = .work,
                        .work_addr = @intCast(ident_1 & 0b111)
                    };
                }
                else {
                    unreachable;
                }
            }
        }
    }

    inline fn fetch(self: *Script700) u32 {
        if (self.state.pc >= self.script_bytecode.len) { // Terminate script if PC goes out of range
            return 0x80FFFFFF; // Return quit instruction
        }

        const instr = self.script_bytecode[self.state.pc];
        self.state.pc +%= 1;
        return instr;
    }

    inline fn read_u8(self: *Script700, memtype: MemType, address: u32) u8 {
        switch (memtype) {
            .port_in => {
                const addr = address & 3;
                return self.state.port_in[addr];
            },
            .port_out => {
                const addr = address & 3;
                return self.emu.s_smp.state.output_ports[addr];
            },
            .aram => {
                const addr: u16 = @intCast(address & 0xFFFF);
                return self.emu.s_smp.debug_read_data(addr);
            },
            .xram => {
                const addr: u16 = @intCast(address & 0x3F);
                return self.emu.s_dsp.audio_ram[0xFFC0 + addr];
            },
            .data => {
                const addr = address & 0x0FFFFF;
                return
                    if (addr < self.data_area.len)
                        self.data_area[addr]
                    else
                        0x00;
            },
            else => unreachable
        }
    }

    inline fn read_u16(self: *Script700, memtype: MemType, address: u32) u16 {
        const lo = @as(u16, self.read_u8(memtype, address));
        const hi = @as(u16, self.read_u8(memtype, address +% 1));
        return lo | hi << 8;
    }

    inline fn read_u32(self: *Script700, memtype: MemType, address: u32) u32 {
        switch (memtype) {
            .label => {
                const addr = address & 0x3FF;
                const loc  = self.label_addresses[addr];
                return
                    if (loc & 0x8000_0000 != 0) // Data locations never use remappings
                        loc
                    else if (self.label_remappings == null)
                        loc
                    else if (addr >= self.label_remappings.?.len)
                        loc
                    else
                        self.label_remappings.?[addr];
            },
            .work => {
                const addr = address & 7;
                return self.state.work[addr];
            },
            else => {
                const low = @as(u32, self.read_u16(memtype, address));
                const hiw = @as(u32, self.read_u16(memtype, address +% 2));
                return low | hiw << 16;
            }
        }
    }

    inline fn write_u8(self: *Script700, memtype: MemType, address: u32, data: u8) void {
        switch (memtype) {
            .port_in => {
                const addr = address & 3;
                self.state.port_in[addr] = data;
                if (!self.state.port_queue_on) {
                    self.emu.s_smp.state.input_ports[addr] = data;
                }
            },
            .port_out => {
                const addr = address & 3;
                self.emu.s_smp.state.output_ports[addr] = data;
            },
            .aram => {
                const addr = address & 0xFFFF;
                self.emu.s_dsp.audio_ram[addr] = data;
            },
            .xram => {
                const addr = address & 0x3F;
                self.emu.s_dsp.audio_ram[0xFFC0 + addr] = data;
            },
            .data => {
                const addr = address & 0x0FFFFF;
                if (addr >= self.data_area.len) {
                    self.expand_data_area(addr + 1);
                }
                self.data_area[addr] = data;
            },
            else => unreachable
        }
    }

    inline fn write_u16(self: *Script700, memtype: MemType, address: u32, data: u16) void {
        const lo: u8 = @intCast(data & 0xFF);
        const hi: u8 = @intCast(data >>   8);
        self.write_u8(memtype, address,      lo);
        self.write_u8(memtype, address +% 1, hi);
    }

    inline fn write_u32(self: *Script700, memtype: MemType, address: u32, data: u32) void {
        switch (memtype) {
            .label => {
                // No operation (Label addresses are not writeable)
            },
            .work => {
                const addr = address & 7;
                self.state.work[addr] = data;
            },
            else => {
                const low: u16 = @intCast(data & 0xFFFF);
                const hiw: u16 = @intCast(data >>    16);
                self.write_u16(memtype, address,      low);
                self.write_u16(memtype, address +% 2, hiw);
            }
        }
    }

    inline fn jump(self: *Script700, address: u32) void {
        if (address < self.script_bytecode.len) {
            self.state.pc = address;
        }
        // No operation if jump destination is out of range
    }

    inline fn expand_data_area(self: *Script700, min_size: u32) void {
        const old_data = self.data_area;

        var new_size: u32 =
            if (self.data_area.len == 0)
                1
            else
                self.data_area.len * 2;

        if (new_size < min_size) {
            new_size = min_size;
        }

        if (new_size > 0x10_0000) {
            new_size = 0x10_0000;
        }

        var new_data = try allocator.alloc(u8, new_size);
        @memcpy(new_data, old_data);
        // Fill the remainder with zeroes
        for (old_data.len..new_data.len) |i| {
            new_data[i] = 0x00;
        }

        self.data_area = new_data;

        if (self.self_alloc_data) {
            allocator.free(old_data);
        }
        else {
            self.self_alloc_data = true;
        }
    }
};