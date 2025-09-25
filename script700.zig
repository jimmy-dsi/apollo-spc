const std = @import("std");

const State = @import("script700_state.zig").Script700State;
const Emu   = @import("emu.zig").Emu;

const db = @import("debug.zig");

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

    _finished: bool = false,

    enabled: bool = false,
    compat_mode: bool = true, // Indicates whether timing operations should be consistent with spcplay Script700 behavior. `false` instead indicates cycle-level accuracy.
    initialized: bool = false,

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

    pub fn init(self: *Script700, script_bytecode: []u32) Load! void {
        try self.load_bytecode(script_bytecode);

        for (0..1024) |i| {
            self.label_addresses[i] = 0xFFFFFFFF;
        }

        self.label_remappings = null;
        self.initialized = false;
        self.data_area = &default_data;
    }

    pub fn reset(self: *Script700) void {
        self.deinit();
        self.script_bytecode = &default_bytecode;
        self.label_remappings = null;
        self.initialized = false;
        self.state.reset();
    }

    pub fn deinit(self: *Script700) void {
        if (self.self_alloc_data) {
            allocator.free(self.data_area);
            self.data_area = &default_data;
            self.self_alloc_data = false;
        }
    }

    pub const Load = error { bytecode_too_large };

    pub fn load_bytecode(self: *Script700, script_bytecode: []u32) Load! void {
        if (script_bytecode.len > 0x1000_0000) {
            // Script bytecode cannot be more than 0x10000000 32-bit words (or 1 GB of data)
            return Load.bytecode_too_large;
        }

        self.enabled = true;
        self.script_bytecode = script_bytecode;
        self.initialized = false;
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

    pub inline fn finished(self: *Script700) bool {
        const result = self._finished;
        //self._finished = false;
        return result;
    }

    pub fn run(self: *Script700, options: RunOptions) void {
        if (!self.initialized) {
            if (self.compat_mode) {
                // SPCPlay's Script700 engine does not start running until 32 DSP cycles have elapsed after SPC reset
                self.state.wait_until = self.state.cur_cycle +| 32;
            }
            self.initialized = true;
            self._finished = true;
            return;
        }

        self.state.step = 0;
        self._finished = false;

        for (0..options.max_steps) |_| {
            if (!self.enabled or self.state.wait_until != null) {
                self._finished = true;
                return;
            }
            self.step_instruction() catch {
                self.enabled = false; // Disable script if we run into an unrecoverable error (such as an issue when allocating memory)
                // TODO: Add error reporting mechanism for when this happens
            };

            // Track last executed cycle on running -> not running transition
            if (!self.enabled or self.state.wait_until != null) {
                self.state.last_cycle = self.emu.s_dsp.cur_cycle();
            }
        }
    }

    pub inline fn step_instruction(self: *Script700) !void {
        const instr = try self.fetch();

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
            try self.proc_instr_send_int_wait(instr);
        }
        else if ((instr & 0xF800_0000) >> 27 == 0b11011) { // Set interrupt vector instruction format
            try self.proc_instr_set_vector(instr);
        }
        // Original Script700 instructions
        else if (instr & 0x8000_0000 == 0) { // General instruction format
            try self.proc_instr_general(instr);
        }
        else if ((instr & 0xF000_0000) >> 28 == 0b1001) { // Branch instruction format
            self.proc_instr_branch(instr);
        }
        else if ((instr & 0xE000_0000) >> 29 == 0b110) { // Wait instruction format
            try self.proc_instr_wait(instr);
        }
        else if ((instr & 0xF800_0000) >> 27 == 0b11100) { // Breakpoint instruction format
            try self.proc_instr_bp(instr);
        }
        else if ((instr & 0xFF00_0000) >> 24 == 0b11110011) { // Set CMPx instruction format (Supplemental command)
            try self.proc_instr_set_cmp(instr);
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

    pub inline fn resume_script(self: *Script700, cur_cycle: u64, cur_cycle_real: u64, cur_begin_cycle: u64, dynamic_wait: bool) void {
        const prev_cycle       = self.state.cur_cycle;
        //const prev_begin_cycle = self.state.begin_cycle;
        const prev_sync_point  = self.state.sync_point;

        self.state.cur_cycle   = cur_cycle_real;
        self.state.begin_cycle = cur_begin_cycle;
        self.state.sync_point  = @divFloor(cur_begin_cycle, 32) * 32;

        const wr: i64 = @intCast(cur_cycle - self.state.sync_point);

        if (dynamic_wait) {
            if (self.compat_mode) {
                self.state.clock_offset = 0;
                self.state.wait_accum   = -wr;

                self.state.cmp[0] = @intCast((cur_cycle - prev_sync_point) & 0xFFFF_FFFF);
            }
            else {
                self.state.wait_accum = 0;
                self.state.cmp[0] = @intCast((cur_cycle - prev_cycle) & 0xFFFF_FFFF);
            }
        }
        else {
            if (self.compat_mode) {
                self.state.clock_offset = 0;
            }
            else {
                self.state.wait_accum = 0;
            }
        }

        self.state.wait_until  = null;
        self.state.wait_device = .none;
    }

    pub const Operands = struct {
        oper_1_prefix: ?[]const u8 = null,
        oper_1_value:  ?u32        = null,

        operator:      ?u8         = null,

        oper_2_prefix: ?[]const u8 = null,
        oper_2_value:  ?u32        = null,
    };

    pub const Compile = error {no_space, unencodable};

    pub fn compile_instruction(buffer: []u32, mnemonic: []const u8, operands: Operands) Compile! void {
        if (buffer.len == 0) {
            return Compile.no_space;
        }

        const op = operands;

        var instr_type: u8 = 0;
        var oper:       u5 = 0;

        if (std.mem.eql(u8, mnemonic, "m")) {
            instr_type = 1;
            oper = 0b0000;
        }
        else if (std.mem.eql(u8, mnemonic, "a")) {
            instr_type = 1;
            oper = 0b0001;
        }
        else if (std.mem.eql(u8, mnemonic, "s")) {
            instr_type = 1;
            oper = 0b0010;
        }
        else if (std.mem.eql(u8, mnemonic, "u")) {
            instr_type = 1;
            oper = 0b0011;
        }
        else if (std.mem.eql(u8, mnemonic, "d")) {
            instr_type = 1;
            oper = 0b0100;
        }
        else if (std.mem.eql(u8, mnemonic, "n")) {
            instr_type = 1;
            if (op.operator) |opr| {
                oper = switch (opr) {
                    '\\' => 0b0101,
                    '%'  => 0b0110,
                    '$'  => 0b0111,
                    '&'  => 0b1000,
                    '|'  => 0b1001,
                    '^'  => 0b1010,
                    '<'  => 0b1011,
                    '_'  => 0b1100,
                    '>'  => 0b1101,
                    '!'  => 0b1110,
                    else => {
                        return Compile.unencodable;
                    }
                };
            }
            else {
                return Compile.unencodable;
            }
        }
        else if (std.mem.eql(u8, mnemonic, "c")) {
            instr_type = 1;
            oper = 0b1111;
        }
        else if (std.mem.eql(u8, mnemonic, "nop")) {
            buffer[0] = 0x8000_0000;
            return;
        }
        else if (std.mem.eql(u8, mnemonic, "q")) {
            buffer[0] = 0x80FF_FFFF;
            return;
        }
        else if (std.mem.eql(u8, mnemonic, "i")) {
            buffer[0] = 0xF700_0000;
            return;
        }
        else if (std.mem.eql(u8, mnemonic, "ib")) {
            buffer[0] = 0xF701_0000;
            return;
        }
        else if (std.mem.eql(u8, mnemonic, "sw")) {
            buffer[0] = 0xF702_0000;
            return;
        }
        else if (std.mem.eql(u8, mnemonic, "r")) {
            buffer[0] = 0xF000_0000;
            return;
        }
        else if (std.mem.eql(u8, mnemonic, "r0")) {
            buffer[0] = 0xF100_0000;
            return;
        }
        else if (std.mem.eql(u8, mnemonic, "r1")) {
            buffer[0] = 0xF200_0000;
            return;
        }
        else if (std.mem.eql(u8, mnemonic, "f")) {
            buffer[0] = 0xF400_0000;
            return;
        }
        else if (std.mem.eql(u8, mnemonic, "f0")) {
            buffer[0] = 0xF500_0000;
            return;
        }
        else if (std.mem.eql(u8, mnemonic, "f1")) {
            buffer[0] = 0xF600_0000;
            return;
        }
        else if (std.mem.eql(u8, mnemonic, "bra")) {
            instr_type = 3;
            oper = 0b0000;
        }
        else if (std.mem.eql(u8, mnemonic, "beq")) {
            instr_type = 3;
            oper = 0b0001;
        }
        else if (std.mem.eql(u8, mnemonic, "bne")) {
            instr_type = 3;
            oper = 0b0010;
        }
        else if (std.mem.eql(u8, mnemonic, "bge")) {
            instr_type = 3;
            oper = 0b0011;
        }
        else if (std.mem.eql(u8, mnemonic, "ble")) {
            instr_type = 3;
            oper = 0b0100;
        }
        else if (std.mem.eql(u8, mnemonic, "bgt")) {
            instr_type = 3;
            oper = 0b0101;
        }
        else if (std.mem.eql(u8, mnemonic, "blt")) {
            instr_type = 3;
            oper = 0b0110;
        }
        else if (std.mem.eql(u8, mnemonic, "bcc")) {
            instr_type = 3;
            oper = 0b0111;
        }
        else if (std.mem.eql(u8, mnemonic, "blo")) {
            instr_type = 3;
            oper = 0b1000;
        }
        else if (std.mem.eql(u8, mnemonic, "bhi")) {
            instr_type = 3;
            oper = 0b1001;
        }
        else if (std.mem.eql(u8, mnemonic, "bcs")) {
            instr_type = 3;
            oper = 0b1010;
        }
        else if (std.mem.eql(u8, mnemonic, "w")) {
            instr_type = 2;
            oper = 0b11000;
        }
        else if (std.mem.eql(u8, mnemonic, "wi")) {
            instr_type = 2;
            oper = 0b11001;
        }
        else if (std.mem.eql(u8, mnemonic, "wo")) {
            instr_type = 2;
            oper = 0b11010;
        }
        else if (std.mem.eql(u8, mnemonic, "bp")) {
            instr_type = 2;
            oper = 0b11100;
        }
        else if (std.mem.eql(u8, mnemonic, "iw")) {
            instr_type = 2;
            oper = 0b11101;
        }
        else if (std.mem.eql(u8, mnemonic, "iv")) {
            instr_type = 2;
            oper = 0b11011;
        }
        else {
            return Compile.unencodable;
        }

        switch (instr_type) {
            1 => {
                if (op.oper_1_prefix == null or op.oper_2_prefix == null) {
                    return Compile.unencodable;
                }

                const p1 = op.oper_1_prefix.?;
                const p2 = op.oper_2_prefix.?;

                var v1: u32 = 0;
                var v2: u32 = 0;

                const s_mt_, const s_sz_, const s_dp = try parse_memtype(p1);
                const d_mt_, const d_sz_, const d_dp = try parse_memtype(p2);

                const s_mt = s_mt_ orelse .port_out;
                const s_sz = s_sz_ orelse 0;
                const d_mt = d_mt_ orelse if (oper == 0b1111) @as(MemType, .port_out) else @as(MemType, .port_in);
                const d_sz = d_sz_ orelse 0;

                if (!s_dp) {
                    if (op.oper_1_value == null) {
                        return Compile.unencodable;
                    }
                    v1 = op.oper_1_value.?;
                }

                if (!d_dp) {
                    if (op.oper_2_value == null) {
                        return Compile.unencodable;
                    }
                    v2 = op.oper_2_value.?;
                }

                if (d_mt == .imm and !d_dp and s_mt == .imm and !s_dp) {
                    // Encode as two set cmp instructions
                    if (buffer.len < 4) {
                        return Compile.no_space;
                    }

                    buffer[0] = 0xF300_0000; // Affects CMP1
                    buffer[1] = v1;          // CMP1 immediate value
                    buffer[2] = 0xF380_0000; // Affects CMP2
                    buffer[3] = v2;          // CMP2 immediate value
                }
                else if (d_mt == .imm and !d_dp) {
                    // Swap operands and encode as revcmp
                    try encode_type_1_instr(
                        buffer,
                        .rcmp,
                        d_mt, d_dp, d_sz, v2,
                        s_mt, s_dp, s_sz, v1
                    );
                }
                else {
                    // General behavior
                    try encode_type_1_instr(
                        buffer,
                        @enumFromInt(oper),
                        s_mt, s_dp, s_sz, v1,
                        d_mt, d_dp, d_sz, v2
                    );
                }
            },
            2 => {
                if (op.oper_1_prefix == null) {
                    return Compile.unencodable;
                }

                const p1 = op.oper_1_prefix.?;

                var v1: u32 = 0;

                const s_mt_, const s_sz_, const s_dp = try parse_memtype(p1);

                const s_mt = s_mt_ orelse .imm;
                const s_sz = s_sz_ orelse 0;

                if (!s_dp) {
                    if (op.oper_1_value == null) {
                        return Compile.unencodable;
                    }
                    v1 = op.oper_1_value.?;
                }

                try encode_type_2_instr(
                    buffer,
                    oper,
                    s_mt, s_dp, s_sz, v1
                );
            },
            3 => {
                if (op.oper_1_prefix == null or op.oper_1_value == null) {
                    return Compile.unencodable;
                }

                const p1 = op.oper_1_prefix.?;
                const v1 = op.oper_1_value.?;

                const s_mt_, _, const s_dp = try parse_memtype(p1);
                const s_mt = s_mt_ orelse .imm;

                if (s_dp) {
                    return Compile.unencodable;
                }

                var icode: u32 = 0x9000_0000;
                var param: u11 = 0b000_00000000;

                icode |= @as(u32, oper) << 24;

                switch (s_mt) {
                    .imm => {
                        param = 0b100_00000000;
                        param |= @intCast(v1 & 0x3FF);
                    },
                    .work => {
                        param = @intCast(v1 & 0b111);
                    },
                    else => {
                        return Compile.unencodable;
                    }
                }

                buffer[0] = icode | @as(u32, param);
            },
            else => unreachable
        }
    }

    inline fn parse_memtype(prefix: []const u8) Compile! struct{?MemType, ?u2, bool} {
        const p = prefix;
        const d = p.len >= 1 and p[p.len - 1] == '?';

        if (p.len == 0 or p.len == 1 and p[0] == '?') {
            return .{null, null, d};
        }
        else if (p.len == 1 or p.len == 2 and p[1] == '?') {
            return switch (p[0]) {
                'o'  => .{.port_out, null, d},
                'i'  => .{.port_in,  null, d},
                'w'  => .{.work,     null, d},
                'r'  => .{.aram,        0, d},
                'x'  => .{.xram,        0, d},
                'd'  => .{.data,        0, d},
                '#'  => .{.imm,      null, d},
                'l'  => .{.label,    null, d},
                else => Compile.unencodable
            };
        }
        else if (p.len == 2 or p.len == 3 and p[2] == '?') {
            if (std.mem.eql(u8, p[0..2], "rb")) {
                return .{.aram, 0, d};
            }
            else if (std.mem.eql(u8, p[0..2], "rw")) {
                return .{.aram, 1, d};
            }
            else if (std.mem.eql(u8, p[0..2], "rd")) {
                return .{.aram, 2, d};
            }
            else if (std.mem.eql(u8, p[0..2], "db")) {
                return .{.data, 0, d};
            }
            else if (std.mem.eql(u8, p[0..2], "dw")) {
                return .{.data, 1, d};
            }
            else if (std.mem.eql(u8, p[0..2], "dd")) {
                return .{.data, 2, d};
            }
            else {
                return Compile.unencodable;
            }
        }
        else {
            return Compile.unencodable;
        }
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

    inline fn proc_instr_wait(self: *Script700, instr: u32) Runtime! void {
        const result = try self.process_type_2_instr(instr);

        const cmd: u2 = @intCast((instr & 0x1800_0000) >> 27);
        switch (cmd) {
            0 => { // wait (w)
                if (result == 0) {
                    return;
                }

                if (self.compat_mode) {
                    self.state.wait_accum +|= result;

                    if (self.state.wait_accum >= 32) {
                        const w: f64 = @floatFromInt(self.state.wait_accum);
                        const step_amt: i64 = @intFromFloat(@ceil(w / 32) * 32);

                        if (step_amt > 0) {
                            const amt: u64 = @intCast(step_amt);

                            self.state.wait_until = self.state.sync_point +| amt;
                            self.state.wait_accum -= step_amt;

                            self.state.clock_offset = 0;
                        }
                    }
                }
                else {
                    self.state.wait_until  = self.state.cur_cycle +| @as(u64, result);
                    self.state.wait_device = .none;
                }
            },
            1 => { // waiti (wi)
                const port_num: u2 = @intCast(result & 3);
                self.state.wait_accum = 0;
                self.state.set_wait_condition(.input, port_num, null);
            },
            2 => { // waito (wo)
                const port_num: u2 = @intCast(result & 3);
                self.state.wait_accum = 0;
                self.state.set_wait_condition(.output, port_num, null);
            },
            3 => unreachable
        }
    }

    inline fn proc_instr_bp(self: *Script700, instr: u32) Runtime! void {
        const aram_addr: u16 = @intCast(try self.process_type_2_instr(instr) & 0xFFFF);
        self.state.enable_breakpoint(aram_addr);
    }

    inline fn proc_instr_set_cmp(self: *Script700, instr: u32) Runtime! void {
        const cmp_index: u1 = @intCast(instr >> 23 & 1);
        const imm = try self.fetch();

        self.state.cmp[cmp_index] = imm;
    }

    inline fn proc_instr_return(self: *Script700, instr: u32) void {
        const cmd = (instr & 0x0300_0000) >> 24;
        switch (cmd) {
            0 => { // ret (r)
                const cs_index = self.state.sp >> 2;
                // Treat return as NOP if we've reached the stack top already
                if (self.state.sp != self.state.sp_top) {
                    self.jump(self.state.callstack[cs_index], false);
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

    inline fn proc_instr_send_int_wait(self: *Script700, instr: u32) Runtime! void {
        const successful = self.emu.s_smp.spc.trigger_interrupt(null);
        self.state.cmp[1] = if (successful) 1 else 0;

        const out_port: u2 = @intCast(try self.process_type_2_instr(instr) & 3);

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

    inline fn proc_instr_set_vector(self: *Script700, instr: u32) Runtime! void {
        const new_vector: u16 = @intCast(try self.process_type_2_instr(instr) & 0xFFFF);
        self.emu.s_smp.update_interrupt_vector(new_vector);
    }

    inline fn process_type_1_instr(self: *Script700, instr: u32, optype: OpType) !void {
        var info = decode_type_1_instr(instr);

        if (!info.src_dyn_ptr) {
            switch (info.src_memtype) {
                .data => {
                    const next = try self.fetch();
                    info.src_data_size = @intCast(next >> 20 & 0b11);
                    info.src_address   = @intCast(next & 0x000F_FFFF);
                },
                .imm => { },
                .aram, .xram => {
                    const next = try self.fetch();
                    info.src_data_size = @intCast(next >> 16 & 0b11);
                    info.src_address   = next & 0xFFFF;
                    if (info.src_data_size == 3) {
                        info.src_memtype = .xram;
                    }
                },
                .label => {
                    const next = try self.fetch();
                    info.src_address = next & 0x03FF;
                },
                else => { }
            }
        }
        
        var result: u32 = undefined;

        const address: u32 = 
            if (info.src_dyn_ptr)
                if (optype == .rcmp)
                    self.state.cmp[1]
                else
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
                        try self.fetch();
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
                        const value = self.read_u32(info.src_memtype, address);
                        result = value;
                    }
                }
            }
        }
        
        var dest_result: u32 = undefined;

        const dest_address: u32 = 
            if (info.dest_dyn_ptr)
                if (optype == .rcmp)
                    self.state.cmp[0]
                else
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
                if (rhs_s == 0) {
                    return; // No operation if attempted division by zero
                }
                const res: i32 = @divFloor(lhs_s, rhs_s);
                final_result = @bitCast(res);
            },
            .udiv => {
                if (result == 0) {
                    return; // No operation if attempted division by zero
                }
                final_result = dest_result / result;
            },
            .mod => {
                if (rhs_s == 0) {
                    return; // No operation if attempted division by zero
                }
                const res: i32 = @rem(lhs_s, rhs_s);
                final_result = @bitCast(res);
            },
            .umod => {
                if (result == 0) {
                    return; // No operation if attempted division by zero
                }
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

    inline fn process_type_2_instr(self: *Script700, instr: u32) Runtime! u32 {
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
                        try self.fetch();
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

    inline fn encode_type_1_instr(buffer: []u32,
                                  optype: OpType,
                                  src_memtype:  MemType, src_dyn_ptr:  bool, src_size:  u2, src_addr:  u32,
                                  dest_memtype: MemType, dest_dyn_ptr: bool, dest_size: u2, dest_addr: u32) Compile! void
    {
        var icode: u32 = 0x0000_0000;

        if (optype == .rcmp) {
            icode = 0xF800_0000;
        }
        else {
            const opval: u4 = @intCast(@intFromEnum(optype));
            icode = @as(u32, opval) << 27;
        }

        var enc_src: u5 = undefined;
        var use_2nd_word: bool = false;

        if (src_dyn_ptr) {
            switch (src_memtype) {
                .imm => {
                    enc_src = 0b10100;
                },
                .port_in => {
                    enc_src = 0b10101;
                },
                .port_out => {
                    enc_src = 0b10110;
                },
                .work => {
                    enc_src = 0b10111;
                },
                .aram => {
                    if (src_size == 0) {
                        enc_src = 0b11000;
                    }
                    else if (src_size == 1) {
                        enc_src = 0b11001;
                    }
                    else if (src_size == 2) {
                        enc_src = 0b11010;
                    }
                    else {
                        return Compile.unencodable;
                    }
                },
                .xram => {
                    enc_src = 0b11011;
                },
                .data => {
                    if (src_size == 0) {
                        enc_src = 0b11100;
                    }
                    else if (src_size == 1) {
                        enc_src = 0b11101;
                    }
                    else if (src_size == 2) {
                        enc_src = 0b11110;
                    }
                    else {
                        return Compile.unencodable;
                    }
                },
                .label => {
                    enc_src = 0b11111;
                }
            }
        }
        else {
            switch (src_memtype) {
                .port_in => {
                    enc_src = @intCast(src_addr & 0b11);
                },
                .port_out => {
                    enc_src = @intCast(src_addr & 0b11);
                    enc_src |= 0b00100;
                },
                .work => {
                    enc_src = @intCast(src_addr & 0b111);
                    enc_src |= 0b01000;
                },
                .data => {
                    enc_src = 0b10000;
                    use_2nd_word = true;
                },
                .imm => {
                    enc_src = 0b10001;
                    use_2nd_word = true;
                },
                .aram, .xram => {
                    enc_src = 0b10010;
                    use_2nd_word = true;
                },
                .label => {
                    enc_src = 0b10011;
                    use_2nd_word = true;
                }
            }
        }

        if (use_2nd_word and buffer.len == 1) {
            return Compile.no_space;
        }

        icode |= @as(u32, enc_src) << 22;

        var enc_dest: u22 = 0;

        if (dest_dyn_ptr) {
            switch (dest_memtype) {
                .imm => {
                    enc_dest = @as(u22, 0b10100) << 13;
                },
                .port_in => {
                    enc_dest = @as(u22, 0b10101) << 13;
                },
                .port_out => {
                    enc_dest = @as(u22, 0b10110) << 13;
                },
                .work => {
                    enc_dest = @as(u22, 0b10111) << 13;
                },
                .aram => {
                    if (src_size == 0) {
                        enc_dest = @as(u22, 0b11000) << 13;
                    }
                    else if (src_size == 1) {
                        enc_dest = @as(u22, 0b11001) << 13;
                    }
                    else if (src_size == 2) {
                        enc_dest = @as(u22, 0b11010) << 13;
                    }
                    else {
                        return Compile.unencodable;
                    }
                },
                .xram => {
                    enc_dest = @as(u22, 0b11011) << 13;
                },
                .data => {
                    if (src_size == 0) {
                        enc_dest = @as(u22, 0b11100) << 13;
                    }
                    else if (src_size == 1) {
                        enc_dest = @as(u22, 0b11101) << 13;
                    }
                    else if (src_size == 2) {
                        enc_dest = @as(u22, 0b11110) << 13;
                    }
                    else {
                        return Compile.unencodable;
                    }
                },
                .label => {
                    enc_dest = @as(u22, 0b11111) << 13;
                }
            }
        }
        else {
            switch (dest_memtype) {
                .port_in => {
                    const addr: u22 = @intCast(dest_addr & 0b11);
                    enc_dest = @as(u22, addr) << 13;
                },
                .port_out => {
                    const addr: u22 = @intCast(dest_addr & 0b11);
                    enc_dest = @as(u22, 0b00100 | addr) << 13;
                },
                .work => {
                    const addr: u22 = @intCast(dest_addr & 0b111);
                    enc_dest = @as(u22, 0b01000 | addr) << 13;
                },
                .label => {
                    enc_dest = @intCast(dest_addr & 0x3FF);
                    enc_dest |= @as(u22, 0b10011) << 13;
                },
                .aram => {
                    enc_dest = @intCast(dest_addr & 0xFFFF);
                    enc_dest |= @as(u22, 0b001) << 19 | @as(u22, dest_size) << 16;
                },
                .xram => {
                    enc_dest = @intCast(dest_addr & 0x003F);
                    enc_dest |= @as(u22, 0b001) << 19 | @as(u22, 0b11) << 16;
                },
                .data => {
                    enc_dest = @intCast(dest_addr & 0xF_FFFF);
                    enc_dest |= @as(u22, dest_size +| 1) << 20;
                },
                else => {
                    return Compile.unencodable;
                }
            }
        }

        icode |= @as(u32, enc_src) << 22 | @as(u32, enc_dest);
        buffer[0] = icode;

        if (use_2nd_word) {
            var icode_2: u32 = 0x0000_0000;

            switch (src_memtype) {
                .label => {
                    icode_2 = src_addr & 0x3FF;
                },
                .aram => {
                    icode_2 = src_addr & 0xFFFF;
                    icode_2 |= @as(u32, src_size) << 16;
                },
                .xram => {
                    icode_2 = src_addr & 0x3F;
                    icode_2 |= @as(u32, 0b11) << 16;
                },
                .data => {
                    if (src_size == 3) {
                        return Compile.unencodable;
                    }

                    icode_2 = src_addr & 0xF_FFFF;
                    icode_2 |= @as(u32, src_size) << 20;
                },
                .imm => {
                    icode_2 = src_addr;
                },
                else => unreachable
            }

            buffer[1] = icode_2;
        }
    }

    inline fn encode_type_2_instr(buffer: []u32,
                                  optype: u5,
                                  src_memtype: MemType, src_dyn_ptr: bool, src_size: u2, src_addr: u32) Compile! void
    {
        var icode: u32 = 0x0000_0000;
        icode = @as(u32, optype) << 27;

        var enc_src: u5 = undefined;
        var use_2nd_word: bool = false;

        if (src_dyn_ptr) {
            switch (src_memtype) {
                .imm => {
                    enc_src = 0b10100;
                },
                .port_in => {
                    enc_src = 0b10101;
                },
                .port_out => {
                    enc_src = 0b10110;
                },
                .work => {
                    enc_src = 0b10111;
                },
                .aram => {
                    if (src_size == 0) {
                        enc_src = 0b11000;
                    }
                    else if (src_size == 1) {
                        enc_src = 0b11001;
                    }
                    else if (src_size == 2) {
                        enc_src = 0b11010;
                    }
                    else {
                        return Compile.unencodable;
                    }
                },
                .xram => {
                    enc_src = 0b11011;
                },
                .data => {
                    if (src_size == 0) {
                        enc_src = 0b11100;
                    }
                    else if (src_size == 1) {
                        enc_src = 0b11101;
                    }
                    else if (src_size == 2) {
                        enc_src = 0b11110;
                    }
                    else {
                        return Compile.unencodable;
                    }
                },
                .label => {
                    enc_src = 0b11111;
                }
            }
        }
        else {
            switch (src_memtype) {
                .port_in => {
                    enc_src = @intCast(src_addr & 0b11);
                },
                .port_out => {
                    enc_src = @intCast(src_addr & 0b11);
                    enc_src |= 0b00100;
                },
                .work => {
                    enc_src = @intCast(src_addr & 0b111);
                    enc_src |= 0b01000;
                },
                .data => {
                    enc_src = 0b10000;
                },
                .imm => {
                    enc_src = 0b10001;
                    use_2nd_word = true;
                },
                .aram, .xram => {
                    enc_src = 0b10010;
                },
                .label => {
                    enc_src = 0b10011;
                }
            }
        }

        if (use_2nd_word and buffer.len == 1) {
            return Compile.no_space;
        }

        icode |= @as(u32, enc_src) << 22;

        var enc_src_2: u22 = 0;
    
        switch (src_memtype) {
            .label => {
                enc_src_2 = @intCast(src_addr & 0x3FF);
            },
            .aram => {
                enc_src_2 = @intCast(src_addr & 0xFFFF);
                enc_src_2 |= @as(u22, src_size) << 16;
            },
            .xram => {
                enc_src_2 = @intCast(src_addr & 0x003F);
                enc_src_2 |= @as(u22, 0b11) << 16;
            },
            .data => {
                enc_src_2 = @intCast(src_addr & 0xF_FFFF);
                enc_src_2 |= @as(u22, src_size) << 20;
            },
            else => {
                enc_src_2 = 0;
            }
        }

        icode |= @as(u32, enc_src) << 22 | @as(u32, enc_src_2);
        buffer[0] = icode;

        if (use_2nd_word) {
            var icode_2: u32 = 0x0000_0000;

            switch (src_memtype) {
                .imm => {
                    icode_2 = src_addr;
                },
                else => unreachable
            }

            buffer[1] = icode_2;
        }
    }

    pub const Runtime = error { fetch_range };

    inline fn fetch(self: *Script700) Runtime! u32 {
        if (self.state.pc >= self.script_bytecode.len) { // Terminate script if PC goes out of range
            return Runtime.fetch_range; // Return fetch out of range error
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

                if (self.state.sp == self.state.sp_top) {
                    self.state.sp_top -%= 4; // Adjust stack top so that it's always 64 or fewer returns away from current SP
                }
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