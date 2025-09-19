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

    pub const OpType = enum {
        mov,
        add, sub, umul, div,
        udiv, mod, umod,
        band, bor, bxor,
        shl, asr, lsr,
        not,
        cmp, rcmp
    };

    pub const default_bytecode: [1]u32 = [1]u32 {0x80FFFFFF}; // Preload with single Quit instruction
    pub var   default_data:     [0]u8  = [0]u8  { };

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

    pub fn reset(self: *Script700) void {
        self.deinit();
        self.script_bytecode = &default_bytecode;
        self.label_remappings = null;
        self.state.reset();
    }

    pub fn deinit(self: *Script700) void {
        if (self.self_alloc_data) {
            allocator.free(self.data_area);
            self.data_area = &default_data;
            self.self_alloc_data = false;
        }
    }

    pub fn load_bytecode(self: *Script700, script_bytecode: []u32) void {
        self.enabled = true;
        self.script_bytecode = script_bytecode;
        self.state.reset();
    }

    pub fn load_data(self: *Script700, data: []u8) void {
        self.deinit();
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
            self.step_instruction() catch {
                self.enabled = false; // Disable script if we run into an unrecoverable error (such as an issue when allocating memory)
                // TODO: Add error reporting mechanism for when this happens
            };
        }
    }

    pub inline fn step_instruction(self: *Script700) !void {
        const instr = self.fetch();

        // Apollo Script700 extended instructions
        if ((instr & 0xFF00_0000) >> 24 == 0b11110111) { // Extended commands group 1
            const cmd: u8 = @intCast((instr & 0x00FF_0000) >> 16);
            switch (cmd) {
                0x00 => { // Send interrupt instruction format
                    self.proc_instr_send_int();
                },
                0x01 => { // Reset interrupt vector instruction format
                    self.proc_instr_reset_vector();
                },
                0x02 => { // Swap CMP1 and CMP2 instruction format
                    self.proc_instr_swap_cmp();
                },
                else => { // Reserved
                    // Do nothing (effective NOP)
                }
            }
        }
        else if ((instr & 0xF800_0000) >> 27 == 0b11101) { // Send interrupt instruction and wait format
            self.proc_instr_send_int_wait(instr);
        }
        else if ((instr & 0xF800_0000) >> 27 == 0b11011) { // Set interrupt vector instruction format
            self.proc_instr_set_vector(instr);
        }
        // Original Script700 instructions
        else if (instr & 0x8000_0000 == 0) { // General instruction format
            try self.proc_instr_general(instr);
        }
        else if ((instr & 0xF000_0000) >> 28 == 0b1001) { // Branch instruction format
            self.proc_instr_branch(instr);
        }
        else if ((instr & 0xE000_0000) >> 29 == 0b110) { // Wait instruction format
            self.proc_instr_wait(instr);
        }
        else if ((instr & 0xF800_0000) >> 27 == 0b11100) { // Breakpoint instruction format
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
            try self.proc_instr_rev_cmp(instr);
        }
        else {
            // Do nothing (un-designated NOP)
        }

        self.state.step +%= 1;
    }

    inline fn proc_instr_general(self: *Script700, instr: u32) !void {
        const op: OpType = @enumFromInt((instr & 0x7800_0000) >> 27);
        try self.process_type_1_instr(instr, op);
    }

    inline fn proc_instr_branch(self: *Script700, instr: u32) void {
        const cmd: u4 = @intCast((instr & 0x0F00_0000) >> 24);

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
                self.jump(address, true);
            },
            0x1 => { // beq ([CMP1] == [CMP2])
                if (c1 == c2) {
                    self.jump(address, true);
                }
            },
            0x2 => { // bne ([CMP1] != [CMP2])
                if (c1 != c2) {
                    self.jump(address, true);
                }
            },
            0x3 => { // bge ([CMP1] <= [CMP2]) [signed]
                if (c1_s <= c2_s) {
                    self.jump(address, true);
                }
            },
            0x4 => { // ble ([CMP1] >= [CMP2]) [signed]
                if (c1_s >= c2_s) {
                    self.jump(address, true);
                }
            },
            0x5 => { // bgt ([CMP1] <  [CMP2]) [signed]
                if (c1_s < c2_s) {
                    self.jump(address, true);
                }
            },
            0x6 => { // blt ([CMP1] >  [CMP2]) [signed]
                if (c1_s > c2_s) {
                    self.jump(address, true);
                }
            },
            0x7 => { // bcc ([CMP1] <= [CMP2]) [unsigned]
                if (c1 <= c2) {
                    self.jump(address, true);
                }
            },
            0x8 => { // blo ([CMP1] >= [CMP2]) [unsigned]
                if (c1 >= c2) {
                    self.jump(address, true);
                }
            },
            0x9 => { // bhi ([CMP1] <  [CMP2]) [unsigned]
                if (c1 < c2) {
                    self.jump(address, true);
                }
            },
            0xA => { // bcs ([CMP1] >  [CMP2]) [unsigned]
                if (c1 > c2) {
                    self.jump(address, true);
                }
            },
            else => {
                // Reserved (No operation)
            }
        }
    }

    inline fn proc_instr_wait(self: *Script700, instr: u32) void {
        const result = self.process_type_2_instr(instr);

        const cmd: u2 = @intCast((instr & 0x1800_0000) >> 27);
        switch (cmd) {
            0 => { // wait (w)
                self.state.wait_until  = self.emu.s_dsp.cur_cycle() +| @as(u64, result);
                self.state.wait_device = .none;
            },
            1 => { // waiti (wi)
                const port_num: u2 = @intCast(result & 3);
                self.state.set_wait_condition(.input, port_num, null);
            },
            2 => { // waito (wo)
                const port_num: u2 = @intCast(result & 3);
                self.state.set_wait_condition(.output, port_num, null);
            },
            3 => unreachable
        }
    }

    inline fn proc_instr_bp(self: *Script700, instr: u32) void {
        const aram_addr: u16 = @intCast(self.process_type_2_instr(instr) & 0xFFFF);
        self.state.enable_breakpoint(aram_addr);
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
                self.jump(self.state.callstack[cs_index], false);
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
                self.state.port_queue_on = false;
                self.state.set_wait_condition(.output, 0, &self.emu.s_smp.state.input_ports[0]);
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

    inline fn proc_instr_rev_cmp(self: *Script700, instr: u32) !void {
        try self.process_type_1_instr(instr, .rcmp);
    }

    inline fn proc_instr_send_int(self: *Script700) void {
        const successful = self.emu.s_smp.spc.trigger_interrupt(null);
        self.state.cmp[1] = if (successful) 1 else 0;
    }

    inline fn proc_instr_send_int_wait(self: *Script700, instr: u32) void {
        const successful = self.emu.s_smp.spc.trigger_interrupt(null);
        self.state.cmp[1] = if (successful) 1 else 0;

        const out_port: u2 = @intCast(self.process_type_2_instr(instr) & 3);

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
        const new_vector: u16 = @intCast(self.process_type_2_instr(instr) & 0xFFFF);
        self.emu.s_smp.update_interrupt_vector(new_vector);
    }

    inline fn process_type_1_instr(self: *Script700, instr: u32, optype: OpType) !void {
        var info = decode_type_1_instr(instr);

        if (!info.src_dyn_ptr) {
            switch (info.src_memtype) {
                .data => {
                    const next = self.fetch();
                    info.src_data_size = @intCast(next >> 20 & 0b11);
                    info.src_address   = @intCast(next & 0x000F_FFFF);
                },
                .imm => { },
                .aram, .xram => {
                    const next = self.fetch();
                    info.src_data_size = @intCast(next >> 16 & 0b11);
                    info.src_address   = next & 0xFFFF;
                    if (info.src_data_size == 3) {
                        info.src_memtype = .xram;
                    }
                },
                .label => {
                    const next = self.fetch();
                    info.src_address = next & 0x03FF;
                },
                else => { }
            }
        }
        
        var result: u32 = undefined;

        const address: u32 = 
            if (info.src_dyn_ptr)
                self.state.cmp[0]
            else
                info.src_address orelse 0;

        switch (info.src_memtype) {
            .port_in, .port_out, .xram => {
                const value = self.read_u8(info.src_memtype, address);
                result = @as(u32, value);
            },
            .work, .label => {
                const value = self.read_u32(info.src_memtype, address);
                result = value;
            },
            .imm => {
                const value =
                    if (info.src_dyn_ptr)
                        address
                    else
                        self.fetch();
                result = value;
            },
            else => {
                switch (info.src_data_size.?) {
                    0, 3 => {
                        const value = self.read_u8(info.src_memtype, address);
                        result = @as(u32, value);
                    },
                    1 => {
                        const value = self.read_u16(info.src_memtype, address);
                        result = @as(u32, value);
                    },
                    2 => {
                        const value = self.read_u32(info.src_memtype, address) & 0xFFFF;
                        result = value;
                    }
                }
            }
        }
        
        var dest_result: u32 = undefined;

        const dest_address: u32 = 
            if (info.dest_dyn_ptr)
                self.state.cmp[1]
            else
                info.dest_address orelse 0;

        switch (info.dest_memtype) {
            .port_in, .port_out, .xram => {
                const value = self.read_u8(info.dest_memtype, dest_address);
                dest_result = @as(u32, value);
            },
            .work, .label => {
                const value = self.read_u32(info.dest_memtype, dest_address);
                dest_result = value;
            },
            .imm => {
                if (info.dest_dyn_ptr) {
                    dest_result = dest_address;
                }
                else {
                    return; // Destination immediate value cannot be obtained using this encoding - Treat as NOP
                }
            },
            else => {
                switch (info.src_data_size.?) {
                    0, 3 => {
                        const value = self.read_u8(info.dest_memtype, dest_address);
                        dest_result = @as(u32, value);
                    },
                    1 => {
                        const value = self.read_u16(info.dest_memtype, dest_address);
                        dest_result = @as(u32, value);
                    },
                    2 => {
                        const value = self.read_u32(info.dest_memtype, dest_address) & 0xFFFF;
                        dest_result = value;
                    }
                }
            }
        }

        var final_result: u32 = undefined;

        const lhs_s: i32 = @bitCast(dest_result);
        const rhs_s: i32 = @bitCast(result);

        switch (optype) {
            .mov => {
                final_result = result;
            },
            .add => {
                final_result = dest_result +% result;
            },
            .sub => {
                final_result = dest_result -% result;
            },
            .umul => {
                final_result = dest_result *% result;
            },
            .div => {
                const res: i32 = @divFloor(lhs_s, rhs_s);
                final_result = @bitCast(res);
            },
            .udiv => {
                final_result = dest_result / result;
            },
            .mod => {
                const res: i32 = @mod(lhs_s, rhs_s);
                final_result = @bitCast(res);
            },
            .umod => {
                final_result = dest_result % result;
            },
            .band => {
                final_result = dest_result & result;
            },
            .bor => {
                final_result = dest_result | result;
            },
            .bxor => {
                final_result = dest_result ^ result;
            },
            .shl => {
                final_result = dest_result <<| result;
            },
            .asr => {
                if (result >= 31) {
                    final_result =
                        if (lhs_s >= 0) 0
                        else            0xFFFF_FFFF;
                }
                else {
                    const amt: u5  = @intCast(result & 0x1F);
                    const res: i32 = lhs_s >> amt;
                    final_result = @bitCast(res);
                }
            },
            .lsr => {
                if (result >= 32) {
                    final_result = 0;
                }
                else {
                    const amt: u5 = @intCast(result & 0x1F);
                    final_result = dest_result >> amt;
                }
            },
            .not => {
                final_result = result ^ 0xFFFF_FFFF;
            },
            .cmp => {
                self.state.cmp[0] = result;
                self.state.cmp[1] = dest_result;
                return;
            },
            .rcmp => {
                self.state.cmp[0] = dest_result;
                self.state.cmp[1] = result;
                return;
            }
        }

        switch (info.dest_memtype) {
            .port_in, .port_out, .xram => {
                try self.write_u8(info.dest_memtype, dest_address, @intCast(final_result & 0xFF));
            },
            .work => {
                try self.write_u32(info.dest_memtype, dest_address, final_result);
            },
            .imm, .label => {
                return; // Cannot write to immediate or label - Treat as NOP
            },
            else => {
                switch (info.src_data_size.?) {
                    0, 3 => {
                        try self.write_u8(info.dest_memtype, dest_address, @intCast(final_result & 0xFF));
                    },
                    1 => {
                        try self.write_u16(info.dest_memtype, dest_address, @intCast(final_result & 0xFFFF));
                    },
                    2 => {
                        try self.write_u32(info.dest_memtype, dest_address, final_result);
                    }
                }
            }
        }
    } 

    inline fn process_type_2_instr(self: *Script700, instr: u32) u32 {
        const info = decode_type_2_instr(instr);
        
        var result: u32 = undefined;

        const address: u32 = 
            if (info.dyn_ptr)
                self.state.cmp[0]
            else
                info.address orelse 0;

        switch (info.memtype) {
            .port_in, .port_out, .xram => {
                const value = self.read_u8(info.memtype, address);
                result = @as(u32, value);
            },
            .work, .label => {
                const value = self.read_u32(info.memtype, address);
                result = value;
            },
            .imm => {
                const value =
                    if (info.dyn_ptr)
                        address
                    else
                        self.fetch();
                result = value;
            },
            else => {
                switch (info.data_size.?) {
                    0, 3 => {
                        const value = self.read_u8(info.memtype, address);
                        result = @as(u32, value);
                    },
                    1 => {
                        const value = self.read_u16(info.memtype, address);
                        result = @as(u32, value);
                    },
                    2 => {
                        const value = self.read_u32(info.memtype, address) & 0xFFFF;
                        result = value;
                    }
                }
            }
        }

        return result;
    }

    const Instr1DecodeResult = struct {
        src_memtype: MemType,
        src_dyn_ptr: bool = false,
        src_data_size: ?u2  = null,
        src_address:   ?u32 = null,

        dest_memtype: MemType,
        dest_dyn_ptr: bool = false,
        dest_data_size: ?u2  = null,
        dest_address:   ?u32 = null,
    };

    inline fn decode_type_1_instr(instr: u32) Instr1DecodeResult {
        const suffix = instr & 0x07FF_FFFF;

        const src_ident:  u5  = @intCast(suffix >> 22);
        const dest_ident: u22 = @intCast(suffix & 0x3F_FFFF);

        var result: Instr1DecodeResult = .{
            .src_memtype  = undefined,
            .dest_memtype = undefined
        };

        switch (src_ident) {
            0b10000 => { // Data location
                result.src_memtype = .data;
            },
            0b10001 => { // Immediate
                result.src_memtype = .imm;
            },
            0b10010 => { // SPC700 RAM / XRAM location
                result.src_memtype = .aram;
            },
            0b10011 => { // Label number
                result.src_memtype = .label;
            },
            else => {
                const src_res = decode_type_2_instr(instr);
                result.src_memtype   = src_res.memtype;
                result.src_dyn_ptr   = src_res.dyn_ptr;
                result.src_data_size = src_res.data_size;
                result.src_address   = src_res.address;
            }
        }

        if ((dest_ident & 0x38_0000) >> 19 == 0b000) { // General destination identifier case
            const sub_ident: u5 = @intCast((dest_ident & 0x03_E000) >> 13);
            switch (sub_ident) {
                0b10000, 0b10001, 0b10010 => {
                    // Reserved (No operation) - Mark dest type as immediate (since writing to an immediate is impossible)
                    result.dest_memtype = .imm;
                },
                0b10011 => { // Label location
                    result.dest_memtype = .label;
                    result.dest_address = @as(u32, dest_ident & 0x3FF);
                },
                0b10100 => { // Dynamic pointer immediate
                    result.dest_memtype = .imm;
                    result.dest_dyn_ptr = true;
                },
                0b10101 => { // Dynamic pointer input port
                    result.dest_memtype = .port_in;
                    result.dest_dyn_ptr = true;
                },
                0b10110 => { // Dynamic pointer output port
                    result.dest_memtype = .port_out;
                    result.dest_dyn_ptr = true;
                },
                0b10111 => { // Dynamic pointer work RAM
                    result.dest_memtype = .work;
                    result.dest_dyn_ptr = true;
                },
                0b11000 => { // Dynamic pointer 8-bit SPC700 RAM
                    result.dest_memtype   = .aram;
                    result.dest_dyn_ptr   = true;
                    result.dest_data_size = 0;
                },
                0b11001 => { // Dynamic pointer 16-bit SPC700 RAM
                    result.dest_memtype   = .aram;
                    result.dest_dyn_ptr   = true;
                    result.dest_data_size = 1;
                },
                0b11010 => { // Dynamic pointer 32-bit SPC700 RAM
                    result.dest_memtype   = .aram;
                    result.dest_dyn_ptr   = true;
                    result.dest_data_size = 2;
                },
                0b11011 => { // Dynamic pointer 8-bit SPC700 XRAM
                    result.dest_memtype = .xram;
                    result.dest_dyn_ptr = true;
                },
                0b11100 => { // Dynamic pointer 8-bit Data location
                    result.dest_memtype   = .data;
                    result.dest_dyn_ptr   = true;
                    result.dest_data_size = 0;
                },
                0b11101 => { // Dynamic pointer 16-bit Data location
                    result.dest_memtype   = .data;
                    result.dest_dyn_ptr   = true;
                    result.dest_data_size = 1;
                },
                0b11110 => { // Dynamic pointer 32-bit Data location
                    result.dest_memtype   = .data;
                    result.dest_dyn_ptr   = true;
                    result.dest_data_size = 2;
                },
                0b11111 => { // Dynamic pointer label number
                    result.dest_memtype = .label;
                    result.dest_dyn_ptr = true;
                },
                else => {
                    if ((sub_ident & 0b11100) >> 2 == 0b000) { // Input port
                        result.dest_memtype = .port_in;
                        result.dest_address = @as(u32, sub_ident & 0b11);
                    }
                    else if ((sub_ident & 0b11100) >> 2 == 0b001) { // Output port
                        result.dest_memtype = .port_out;
                        result.dest_address = @as(u32, sub_ident & 0b11);
                    }
                    else if ((sub_ident & 0b11000) >> 3 == 0b01) { // Work RAM
                        result.dest_memtype = .work;
                        result.dest_address = @as(u32, sub_ident & 0b111);
                    }
                    else {
                        unreachable;
                    }
                }
            }
        }
        else if ((dest_ident & 0x38_0000) >> 19 == 0b001) { // SPC RAM case
            const sub_ident: u2 = @intCast((dest_ident & 0x03_0000) >> 16);
            const address = @as(u32, dest_ident & 0xFFFF);

            switch (sub_ident) {
                0 => { // 8-bit SPC700 ARAM
                    result.dest_memtype   = .aram;
                    result.dest_address   = address;
                    result.dest_data_size = 0;
                },
                1 => { // 16-bit SPC700 ARAM
                    result.dest_memtype   = .aram;
                    result.dest_address   = address;
                    result.dest_data_size = 1;
                },
                2 => { // 32-bit SPC700 ARAM
                    result.dest_memtype   = .aram;
                    result.dest_address   = address;
                    result.dest_data_size = 2;
                },
                3 => { // 8-bit SPC700 XRAM
                    result.dest_memtype   = .xram;
                    result.dest_address   = address & 0x003F;
                    result.dest_data_size = 0;
                }
            }
        }
        else { // Data location case
            const size: u2  = @intCast((dest_ident & 0x30_0000) >> 20);
            const loc:  u20 = @intCast( dest_ident & 0x0F_FFFF);
            result.dest_memtype   = .data;
            result.dest_address   = loc;
            result.dest_data_size = size;
        }

        return result;
    }

    inline fn decode_type_2_instr(instr: u32)
        struct {
            memtype: MemType,
            dyn_ptr: bool = false,
            data_size: ?u2  = null,
            address:   ?u32 = null
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
                    .address   = @as(u32, ident_2 & 0x0F_FFFF)
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
                        .memtype = .xram,
                        .address = @as(u32, ident_2 & 0x003F)
                    };
                }
                else {
                    return .{
                        .memtype   = .aram,
                        .data_size = data_size,
                        .address   = @as(u32, ident_2 & 0xFFFF)
                    };
                }
            },
            0b10011 => { // Label number
                return .{
                    .memtype = .label,
                    .address = @as(u32, ident_2 & 0x03FF)
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
                        .memtype = .port_in,
                        .address = @as(u32, ident_1 & 0b11)
                    };
                }
                else if ((ident_1 & 0b11100) >> 2 == 0b001) { // Output port
                    return .{
                        .memtype  = .port_out,
                        .address = @as(u32, ident_1 & 0b11)
                    };
                }
                else if ((ident_1 & 0b11000) >> 3 == 0b01) { // Work RAM
                    return .{
                        .memtype  = .work,
                        .address = @as(u32, ident_1 & 0b111)
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
                return self.emu.s_smp.state.input_ports[addr];
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

    inline fn write_u8(self: *Script700, memtype: MemType, address: u32, data: u8) !void {
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
                    try self.expand_data_area(addr + 1);
                }
                self.data_area[addr] = data;
            },
            else => unreachable
        }
    }

    inline fn write_u16(self: *Script700, memtype: MemType, address: u32, data: u16) !void {
        const lo: u8 = @intCast(data & 0xFF);
        const hi: u8 = @intCast(data >>   8);
        try self.write_u8(memtype, address,      lo);
        try self.write_u8(memtype, address +% 1, hi);
    }

    inline fn write_u32(self: *Script700, memtype: MemType, address: u32, data: u32) !void {
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
                try self.write_u16(memtype, address,      low);
                try self.write_u16(memtype, address +% 2, hiw);
            }
        }
    }

    inline fn jump(self: *Script700, address: u32, use_stack: bool) void {
        if (address < self.script_bytecode.len) {
            if (use_stack and self.state.callstack_on) {
                self.state.sp -%= 4;
                self.state.callstack[self.state.sp >> 2] = self.state.pc;
            }
            self.state.pc = address;
        }
        // No operation if jump destination is out of range
    }

    inline fn expand_data_area(self: *Script700, min_size: u32) !void {
        const old_data = self.data_area;

        var new_size: u32 = @intCast(
            if (self.data_area.len == 0)
                1
            else
                self.data_area.len * 2
        );

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