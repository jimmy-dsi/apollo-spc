const std = @import("std");

const Emu       = @import("emu.zig").Emu;
const SSMP      = @import("s_smp.zig").SSMP;
const SPCState  = @import("spc_state.zig").SPCState;
const CoManager = @import("co_mgr.zig").CoManager;
const CoState   = @import("co_state.zig").CoState;

const Co = CoState.Co;

pub const SPC = struct {
    pub const AluOp = enum {
        none, mov,
        add, sub, cmp,
        bitand, bitor, bitxor
    };

    pub const AluModifyOp = enum {
        none,
        inc, dec,
        asl, lsr, rol, ror,
        not
    };

    pub const AluWordOp = enum {
        none,
        addw, subw, cmpw
    };

    emu: *Emu,

    state: SPCState,

    // Temporary persistent working variables across coroutine states
    data_u8:  [4]u8  = [4]u8  { 0, 0, 0, 0 },
    data_i8:  [4]i8  = [4]i8  { 0, 0, 0, 0 },
    data_u16: [2]u16 = [2]u16 { 0, 0 },
    data_u32: [2]u32 = [2]u32 { 0, 0 },

    pub fn new(emu: *Emu, a_: ?u8, x_: ?u8, y_: ?u8, sp_: ?u8, pc_: ?u16, psw_: ?u8) SPC {
        return SPC {
            .emu = emu,
            .state = SPCState.new(a_, x_, y_, sp_, pc_, psw_)
        };
    }

    pub fn power_on(self: *SPC) void {
        _ = self;
    }

    pub fn reset(self: *SPC) void {
        _  = self;
    }

    pub inline fn s_smp(self: *const SPC) *SSMP {
        return &self.emu.*.s_smp;
    }

    pub inline fn a(self: *const SPC) u8 {
        return self.state.a;
    }

    pub inline fn x(self: *const SPC) u8 {
        return self.state.x;
    }

    pub inline fn y(self: *const SPC) u8 {
        return self.state.y;
    }

    pub inline fn ya(self: *const SPC) u16 {
        return @as(u16, self.state.y) << 8 | @as(u16, self.state.a);
    }

    pub inline fn set_ya(self: *SPC, value: u16) void {
        self.state.a = @intCast(value & 0xFF);
        self.state.y = @intCast(value >> 8);
    }

    pub inline fn pc(self: *const SPC) u16 {
        return self.state.pc;
    }

    pub inline fn sp(self: *const SPC) u16 {
        return self.state.sp;
    }

    pub inline fn psw(self: *const SPC) u8 {
        return self.state.psw;
    }

    pub inline fn n(self: *const SPC) u1 {
        return self.state.n();
    }

    pub inline fn v(self: *const SPC) u1 {
        return self.state.v();
    }

    pub inline fn p(self: *const SPC) u1 {
        return self.state.p();
    }

    pub inline fn z(self: *const SPC) u1 {
        return self.state.z();
    }

    pub inline fn c(self: *const SPC) u1 {
        return self.state.c();
    }

    pub inline fn step_pc(self: *SPC) void {
        self.state.pc +%= 1;
    }

    pub inline fn inc_sp(self: *SPC) void {
        self.state.sp +%= 1;
    }

    pub inline fn dec_sp(self: *SPC) void {
        self.state.sp -%= 1;
    }

    inline fn fetch(self: *const SPC, substate_offset: u32) !void {
        _ = try self.s_smp().fetch(substate_offset);
    }

    inline fn fetch_word(self: *const SPC, substate_offset: u32) !void {
        _ = try self.s_smp().fetch_word(substate_offset);
    }

    inline fn push(self: *const SPC, data: u8, substate_offset: u32) !void {
        return self.s_smp().push(data, substate_offset);
    }

    inline fn push_word(self: *const SPC, data: u16, substate_offset: u32) !void {
        return self.s_smp().push_word(data, substate_offset);
    }

    inline fn pull(self: *const SPC, substate_offset: u32) !void {
        _ = try self.s_smp().pull(substate_offset);
    }

    inline fn pull_word(self: *const SPC, substate_offset: u32) !void {
        _ = try self.s_smp().pull_word(substate_offset);
    }

    inline fn dummy_read(self: *const SPC, address: u16, substate_offset: u32) !void {
        _ = try self.s_smp().dummy_read(address, substate_offset);
    }

    inline fn read(self: *const SPC, address: u16, substate_offset: u32) !void {
        _ = try self.s_smp().read(address, substate_offset);
    }

    inline fn read_word(self: *const SPC, address: u16, substate_offset: u32) !void {
        _ = try self.s_smp().read_word(address, substate_offset);
    }

    inline fn read_dp(self: *const SPC, address: u8, substate_offset: u32) !void {
        const real_address = @as(u9, self.p()) << 8 | address;
        _ = try self.read(real_address, substate_offset);
    }

    inline fn read_dp_word(self: *const SPC, address: u8, substate_offset: u32) !void {
        const page_offset: u16 = @as(u9, self.p()) << 8;

        const real_address_lo = page_offset | address;
        const real_address_hi = page_offset | address +% 1;

        switch (substate_offset) {
            // First, read low byte:
            0, 1 => {
                try self.read(real_address_lo, substate_offset);
            },
            // Next, read high byte:
            2, 3 => {
                try self.read(real_address_hi, substate_offset - 2);
            },
            4 => {
                return;
            },
            else => unreachable
        }
    }

    inline fn write(self: *SPC, address: u16, data: u8, substate_offset: u32) !void {
        return self.s_smp().write(address, data, substate_offset);
    }

    inline fn write_word(self: *SPC, address: u16, data: u16, substate_offset: u32) !void {
        return self.s_smp().write_word(address, data, substate_offset);
    }

    inline fn write_dp(self: *SPC, address: u8, data: u8, substate_offset: u32) !void {
        const real_address = @as(u9, self.p()) << 8 | address;
        return self.s_smp().write(real_address, data, substate_offset);
    }

    inline fn idle(self: *const SPC) !void {
        return self.s_smp().idle();
    }

    inline fn wait(self: *const SPC, cycles: u32) !void {
        try self.s_smp().*.co.wait(cycles);
    }

    inline fn finish(self: *const SPC, delay_amt: u32) void {
        self.s_smp().co.finish(delay_amt);
    }

    inline fn last_read_byte(self: *const SPC) u8 {
        return self.s_smp().last_read_bytes[0];
    }

    inline fn last_read_word(self: *const SPC) u16 {
        const hi = self.s_smp().last_read_bytes[0]; // high byte == most recently read
        const lo = self.s_smp().last_read_bytes[1]; // low byte == the one before that

       return lo | @as(u16, hi) << 8;
    }

    inline fn do_alu_op(self: *SPC, lhs: *u8, rhs: u8, comptime op: AluOp) void {
        switch (op) {
            AluOp.none => {
                lhs.* = rhs;
            },
            AluOp.mov => {
                lhs.* = rhs;
                self.state.set_z(@intFromBool(lhs.* == 0));
                self.state.set_n(@intFromBool(lhs.* & 0x80 != 0));
            },
            AluOp.add, AluOp.sub => {
                const r: u8 = if (op == AluOp.add) rhs else ~rhs;

                const carry = self.c();
                const res: i16 = @as(i16, lhs.*) +% @as(i16, r) +% @as(i16, carry);

                self.state.set_c(@intFromBool(res >= 0x100));

                const res_u16: u16 = @bitCast(res);
                const res_u8:  u8  = @intCast(res_u16 & 0xFF);

                const sign_l = lhs.* >> 7;
                const sign_r = r     >> 7;
                const sign_v = @intFromBool(res < 0);

                self.state.set_z(@intFromBool(res_u8 == 0));
                self.state.set_n(@intFromBool(res_u8 & 0x80 != 0));
                self.state.set_h(@intFromBool((lhs.* ^ r ^ res_u8) & 0x10 != 0));

                // Overflow bit is triggered if the two inputs have the same sign but get the result is the opposite sign
                self.state.set_v(@intFromBool(sign_l == sign_r and sign_v != sign_l));

                lhs.* = res_u8;
            },
            AluOp.cmp => {
                const res:     i16 = @as(i16, lhs.*) -% @as(i16, rhs);
                const res_u16: u16 = @bitCast(res);
                const res_u8:  u8  = @intCast(res_u16 & 0xFF);

                self.state.set_c(@intFromBool(res >= 0));
                self.state.set_z(@intFromBool(res_u8 == 0));
                self.state.set_n(@intFromBool(res_u8 & 0x80 != 0));
            },
            AluOp.bitand => {
                lhs.* &= rhs;
                self.state.set_z(@intFromBool(lhs.* == 0));
                self.state.set_n(@intFromBool(lhs.* & 0x80 != 0));
            },
            AluOp.bitor => {
                lhs.* |= rhs;
                self.state.set_z(@intFromBool(lhs.* == 0));
                self.state.set_n(@intFromBool(lhs.* & 0x80 != 0));
            },
            AluOp.bitxor => {
                lhs.* ^= rhs;
                self.state.set_z(@intFromBool(lhs.* == 0));
                self.state.set_n(@intFromBool(lhs.* & 0x80 != 0));
            }
        }
    }

    inline fn do_alu_op_1bit(_: *SPC, lhs: u1, rhs: u8, bit: u3, comptime op: AluOp) u1 {
        const rhs_bit: u1 = @intCast(rhs >> bit & 1);

        switch (op) {
            AluOp.none => {
                return rhs_bit;
            },
            AluOp.mov => unreachable,
            AluOp.add => unreachable,
            AluOp.sub => unreachable,
            AluOp.cmp => unreachable,
            AluOp.bitand => {
                return lhs & rhs_bit;
            },
            AluOp.bitor => {
                return lhs | rhs_bit;
            },
            AluOp.bitxor => {
                return lhs ^ rhs_bit;
            }
        }
    }

    inline fn do_alu_modify_op(self: *SPC, value: *u8, comptime op: AluModifyOp) void {
        switch (op) {
            AluModifyOp.none => {

            },
            AluModifyOp.inc => {
                value.* +%= 1;
                self.state.set_z(@intFromBool(value.* == 0));
                self.state.set_n(@intFromBool(value.* & 0x80 != 0));
            },
            AluModifyOp.dec => {
                value.* -%= 1;
                self.state.set_z(@intFromBool(value.* == 0));
                self.state.set_n(@intFromBool(value.* & 0x80 != 0));
            },
            AluModifyOp.asl => {
                value.*, const carry = @shlWithOverflow(value.*, 1);
                self.state.set_c(carry);
                self.state.set_z(@intFromBool(value.* == 0));
                self.state.set_n(@intFromBool(value.* & 0x80 != 0));
            },
            AluModifyOp.lsr => {
                self.state.set_c(@intCast(value.* & 0x01));
                value.* >>= 1;
                self.state.set_z(@intFromBool(value.* == 0));
                self.state.set_n(@intFromBool(value.* & 0x80 != 0));
            },
            AluModifyOp.rol => {
                value.*, const carry = @shlWithOverflow(value.*, 1);
                value.* |= @as(u8, self.c());
                self.state.set_c(carry);
                self.state.set_z(@intFromBool(value.* == 0));
                self.state.set_n(@intFromBool(value.* & 0x80 != 0));
            },
            AluModifyOp.ror => {
                const prev_carry = self.c();
                self.state.set_c(@intCast(value.* & 0x01));
                value.* = value.* >> 1 | @as(u8, prev_carry) << 7;
                self.state.set_z(@intFromBool(value.* == 0));
                self.state.set_n(@intFromBool(value.* & 0x80 != 0));
            },
            else => unreachable
        }
    }

    inline fn do_alu_word_op(self: *SPC, lhs: u16, rhs: u16, comptime op: AluWordOp) u16 {
        switch (op) {
            AluWordOp.none => {

            },
            AluWordOp.addw => {
                var   lhs_lo: u8 = @intCast(lhs & 0xFF);
                var   lhs_hi: u8 = @intCast(lhs >>   8);
                const rhs_lo: u8 = @intCast(rhs & 0xFF);
                const rhs_hi: u8 = @intCast(rhs >>   8);

                self.state.set_c(0); // Clear carry before addition
                self.do_alu_op(&lhs_lo, rhs_lo, AluOp.add);
                self.do_alu_op(&lhs_hi, rhs_hi, AluOp.add);

                const res: u16 = @as(u16, lhs_lo) | @as(u16, lhs_hi) << 8;
                self.state.set_z(@intFromBool(res == 0));
                return res;
            },
            AluWordOp.subw => {
                var   lhs_lo: u8 = @intCast(lhs & 0xFF);
                var   lhs_hi: u8 = @intCast(lhs >>   8);
                const rhs_lo: u8 = @intCast(rhs & 0xFF);
                const rhs_hi: u8 = @intCast(rhs >>   8);

                self.state.set_c(1); // Set carry before subtraction
                self.do_alu_op(&lhs_lo, rhs_lo, AluOp.sub);
                self.do_alu_op(&lhs_hi, rhs_hi, AluOp.sub);

                const res: u16 = @as(u16, lhs_lo) | @as(u16, lhs_hi) << 8;
                self.state.set_z(@intFromBool(res == 0));
                return res;
            },
            AluWordOp.cmpw => {
                const res:     i32 = @as(i32, lhs) -% @as(i32, rhs);
                const res_u32: u32 = @bitCast(res);
                const res_u16: u16 = @intCast(res_u32 & 0xFFFF);

                self.state.set_c(@intFromBool(res >= 0));
                self.state.set_z(@intFromBool(res_u16 == 0));
                self.state.set_n(@intFromBool(res_u16 & 0x8000 != 0));

                return lhs;
            }
        }
    }

    // Opcode functions
    pub inline fn nop(self: *SPC, substate: u32) !void {
        switch (substate) {
            0, 1 => {  try self.dummy_read(self.pc(), substate);  },
            2    => {  self.finish(0);                            },
            else => unreachable
        }
    }

    pub inline fn tcall(self: *SPC, substate: u32, comptime index: u4) !void {
        switch (substate) {
            0, 1   => {  try self.dummy_read(self.pc(), substate);         },
            2      => {  try self.idle();                                  },
            3...6  => {  try self.push_word(self.pc(), substate - 3);      },
            7      => {  try self.idle();                                  },
            8...11 => {  const address = 0xFFDE - (@as(u16, index) << 1); 
                         try self.read_word(address, substate - 8);        },
            12     => {  self.state.pc = self.last_read_word(); 
                         self.finish(0);                                   },

            else => unreachable
        }
    }

    pub inline fn set1(self: *SPC, substate: u32, comptime bit: u3) !void {
        const address = &self.data_u8[0];
        const data    = &self.data_u8[1];

        sw: switch (substate) {
            0, 1 => {  try self.fetch(substate);                                   }, // Fetch
            2    => {  address.* = self.last_read_byte(); continue :sw 3;          }, // Start of DP read (2)
            3    => {  try self.read_dp(address.*, substate - 2);                  },
            4    => {  data.* = self.last_read_byte() | 1 << bit; continue :sw 5;  }, // Start of DP write (4)
            5    => {  try self.write_dp(address.*, data.*, substate - 4);         },
            6    => {  self.finish(0);                                             }, // End
            
            else => unreachable
        }
    }

    pub inline fn branch_bit(self: *SPC, substate: u32, comptime bit: u3, comptime expect: u1) !void {
        const address = &self.data_u8[0];
        const data    = &self.data_u8[1];
        const offset  = &self.data_i8[0];

        sw: switch (substate) {
            0, 1 => {  try self.fetch(substate);                             }, // Fetch
            2    => {  address.* = self.last_read_byte(); continue :sw 3;    }, // Start of DP read (2)
            3    => {  try self.read_dp(address.*, substate - 2);            },
            4    => {  try self.idle();                                      }, // Idle
            5    => {  data.* = self.last_read_byte(); continue :sw 6;       }, // Start of next fetch (5)
            6    => {  try self.fetch(substate - 5);                         },
            7    => {  if (data.* >> bit & 1 != expect) { self.finish(0); }     // End if branch condition is false
                       else { try self.idle(); }                             }, // Idle
            8    => {  try self.idle();                                      }, // Idle
            9    => {  offset.* = @bitCast(self.last_read_byte());              // End
                       self.state.pc +%= @bitCast(@as(i16, offset.*));  
                       self.finish(0);                                       },
            
            else => unreachable
        }
    }

    pub inline fn alu_a_with_d(self: *SPC, substate: u32, comptime op: AluOp) !void {
        const address = &self.data_u8[0];
        const data    = &self.data_u8[1];

        sw: switch (substate) {
            0, 1 => {  try self.fetch(substate);                           }, // Fetch
            2    => {  address.* = self.last_read_byte(); continue :sw 3;  }, // Start of DP read (2)
            3    => {  try self.read_dp(address.*, substate - 2);          },
            4    => {  data.* = self.last_read_byte();                        // Perform ALU operation - End
                       self.do_alu_op(&self.state.a, data.*, op);           
                       self.finish(0);                                     },

            else => unreachable
        }
    }

    pub inline fn alu_a_with_abs(self: *SPC, substate: u32, comptime op: AluOp) !void {
        const address = &self.data_u16[0];
        const data    = &self.data_u8[0];

        sw: switch (substate) {
            0...3 => {  try self.fetch_word(substate);                      }, // Fetch
            4     => {  address.* = self.last_read_word(); continue :sw 5;  }, // Start of ABS read (4)
            5     => {  try self.read(address.*, substate - 4);             },
            6     => {  data.* = self.last_read_byte();                        // Perform ALU operation - End
                        self.do_alu_op(&self.state.a, data.*, op);           
                        self.finish(0);                                     },
            
            else => unreachable
        }
    }

    pub inline fn alu_a_with_x_ind(self: *SPC, substate: u32, comptime op: AluOp) !void {
        const data = &self.data_u8[0];

        switch (substate) {
            0, 1 => {  try self.dummy_read(self.pc(), substate);   }, // Dummy read
            2, 3 => {  try self.read_dp(self.x(), substate - 2);   }, // DP read from X
            4    => {  data.* = self.last_read_byte();                // Perform ALU operation - End
                       self.do_alu_op(&self.state.a, data.*, op);    
                       self.finish(0);                             },
            
            else => unreachable
        }
    }

    pub inline fn alu_a_with_d_x_ind(self: *SPC, substate: u32, comptime op: AluOp) !void {
        const indirect = &self.data_u8[0];
        const data     = &self.data_u8[1];
        const address  = &self.data_u16[0];

        sw: switch (substate) {
            0, 1  => {  try self.fetch(substate);                                     }, // Fetch
            2     => {  try self.idle();                                              }, // Idle
            3     => {  indirect.* = self.last_read_byte(); continue :sw 4;           }, // Start of DP word read (3)
            4...6 => {  try self.read_dp_word(indirect.* +% self.x(), substate - 3);  },
            7     => {  address.* = self.last_read_word(); continue :sw 8;            }, // Start of ABS read (7)
            8     => {  try self.read(address.*, substate - 7);                       },
            9     => {  data.* = self.last_read_byte();                                  // Perform ALU operation - End
                        self.do_alu_op(&self.state.a, data.*, op);           
                        self.finish(0);                                               },

            else => unreachable
        }
    }

    pub inline fn alu_a_with_imm(self: *SPC, substate: u32, comptime op: AluOp) !void {
        const data = &self.data_u8[0];
        
        switch (substate) {
            0, 1  => {  try self.fetch(substate);                   }, // Fetch
            2     => {  data.* = self.last_read_byte();                // Perform ALU operation - End
                        self.do_alu_op(&self.state.a, data.*, op);
                        self.finish(0);                             },

            else => unreachable
        }
    }

    pub inline fn alu_d_with_d(self: *SPC, substate: u32, comptime op: AluOp) !void {
        const address_d = &self.data_u8[0];
        const address_s = &self.data_u8[1];
        const data_l    = &self.data_u8[2];
        const data_r    = &self.data_u8[3];

        sw: switch (substate) {
            0, 1 => {  try self.fetch(substate);                                }, // Fetch
            2    => {  address_s.* = self.last_read_byte(); continue :sw 3;     }, // Start of DP read (2)
            3    => {  try self.read_dp(address_s.*, substate - 2);             },
            4    => {  data_r.* = self.last_read_byte(); continue :sw 5;        }, // Start of fetch (4)
            5    => {  try self.fetch(substate - 4);                            },
            6    => {  address_d.* = self.last_read_byte(); continue :sw 7;     }, // Start of DP read (6)
            7    => {  try self.read_dp(address_d.*, substate - 6);             },
            8    => {  data_l.* = self.last_read_byte();                           // Start of DP write (8)
                       self.do_alu_op(data_l, data_r.*, op); continue :sw 9;    },
            9    => {  try self.write_dp(address_d.*, data_l.*, substate - 8);  },
            10   => {  self.finish(0);                                          }, // End

            else => unreachable
        }
    }

    pub inline fn alu_c_with_mem_1bit(self: *SPC, substate: u32, comptime op: AluOp, comptime negate: bool) !void {
        const requires_idle = op == AluOp.bitor or op == AluOp.bitxor;

        const address  = &self.data_u16[0];
        const data     = &self.data_u8[0];
        const bit      = &self.data_u8[1];

        sw: switch (substate) {          
            0...3 => {  try self.fetch_word(substate);                                             }, // Fetch word
            4     => {  address.* = self.last_read_word();                                            // Start of ABS read (4)
                        bit.* = @intCast(address.* >> 13);                                  
                        address.* &= 0x1FFF; continue :sw 5;                                       }, // Looks like only the first 0x2000 ARAM addresses can have these bit ops applied... TIL
            5     => {  try self.read(address.*, substate - 4);                                    },
            6     => {  data.* = self.last_read_byte();                                               // Idle (if needed)
                        if (requires_idle) { try self.idle(); }
                        else { continue :sw 7; }                                                   },
            7     => {  const dt = if (negate) ~data.* else data.*;                                   // Perform ALU operation - End
                        self.state.set_c(self.do_alu_op_1bit(self.c(), dt, @intCast(bit.*), op));  
                        self.finish(0);                                                            },

            else => unreachable
        }
    }

    pub inline fn alu_modify_d(self: *SPC, substate: u32, comptime op: AluModifyOp) !void {
        const address = &self.data_u8[0];
        const data    = &self.data_u8[1];

        sw: switch (substate) {   
            0, 1 => {  try self.fetch(substate);                            }, // Fetch
            2    => {  address.* = self.last_read_byte(); continue :sw 3;   }, // Start of DP read (2)
            3    => {  try self.read_dp(address.*, substate - 2);           },
            4    => {  data.* = self.last_read_byte();                         // Start of DP write (4)
                       self.do_alu_modify_op(data, op); continue :sw 5;     },
            5    => {  try self.write_dp(address.*, data.*, substate - 4);  },
            6    => {  self.finish(0);                                      }, // End

            else => unreachable
        }
    }

    pub inline fn alu_modify_abs(self: *SPC, substate: u32, comptime op: AluModifyOp) !void {
        const address = &self.data_u16[0];
        const data    = &self.data_u8[0];

        sw: switch (substate) {   
            0...3 => {  try self.fetch_word(substate);                      }, // Fetch word
            4     => {  address.* = self.last_read_word(); continue :sw 5;  }, // Start of ABS read (4)
            5     => {  try self.read(address.*, substate - 4);             },
            6     => {  data.* = self.last_read_byte();                        // Start of ABS write (6)
                        self.do_alu_modify_op(data, op); continue :sw 7;    },
            7     => {  try self.write(address.*, data.*, substate - 6);    },
            8     => {  self.finish(0);                                     }, // End

            else => unreachable
        }
    }

    pub inline fn push_reg(self: *SPC, substate: u32, reg: *u8) !void {
        switch (substate) {
            0, 1 => {  try self.dummy_read(self.pc(), substate);  }, // Dummy read
            2, 3 => {  try self.push(reg.*, substate - 2);        }, // Push register
            4    => {  try self.idle();                           }, // Idle
            5    => {  self.finish(0);                            }, // End

            else => unreachable
        }
    }

    pub inline fn tset1(self: *SPC, substate: u32) !void {
        const address = &self.data_u16[0];
        const data    = &self.data_u8[0];

        sw: switch (substate) {
            0...3 => {  try self.fetch_word(substate);                               }, // Fetch word
            4     => {  address.* = self.last_read_word(); continue :sw 5;           }, // Start of ABS read (4)
            5     => {  try self.read(address.*, substate - 4);                      },
            6     => {  data.* = self.last_read_byte();                                 // Start of ABS dummy read (6)
                        const diff = self.a() -% data.*;         
                        self.state.set_z(@intFromBool(diff == 0));         
                        self.state.set_n(@intCast(diff >> 7 & 1)); continue :sw 7;   },
            7     => {  try self.dummy_read(address.*, substate - 6);                },
            8, 9  => {  try self.write(address.*, data.* | self.a(), substate - 8);  }, // ABS write
            10    => {  self.finish(0);                                              }, // End

            else => unreachable
        }
    }

    pub inline fn brk(self: *SPC, substate: u32) !void {
        switch (substate) {
            0, 1   => {  try self.dummy_read(self.pc(), substate);     }, // Dummy read
            2...5  => {  try self.push_word(self.pc(), substate - 2);  }, // Push PC
            6, 7   => {  try self.push(self.psw(), substate - 6);      }, // Push PSW
            8      => {  try self.idle();                              }, // Idle
            9...12 => {  try self.read_word(0xFFDE, substate - 9);     }, // Read address
            13     => {  self.state.pc = self.last_read_word();           // Set registers - End
                         self.state.set_i(0); 
                         self.state.set_b(1); 
                         self.finish(0);                               },

            else => unreachable
        }
    }

    pub inline fn branch(self: *SPC, substate: u32, condition: bool) !void {
        switch (substate) {
            0, 1 => {  try self.fetch(substate);                            }, // Fetch
            2    => {  if (!condition) { self.finish(0); }                     // End if branch condition is false
                       else { try self.idle(); }                            }, // Idle
            3    => {  try self.idle();                                     }, // Idle
            4    => {  const offset: i8 = @bitCast(self.last_read_byte());     // End
                       self.state.pc +%= @bitCast(@as(i16, offset));
                       self.finish(0);                                      },

            else => unreachable
        }
    }

    pub inline fn clr1(self: *SPC, substate: u32, comptime bit: u3) !void {
        const address = &self.data_u8[0];
        const data    = &self.data_u8[1];

        sw: switch (substate) {
            0, 1 => {  try self.fetch(substate);                                             }, // Fetch
            2    => {  address.* = self.last_read_byte(); continue :sw 3;                    }, // Start of DP read (2)
            3    => {  try self.read_dp(address.*, substate - 2);                            },
            4    => {  data.* = self.last_read_byte() & ~@as(u8, 1 << bit); continue :sw 5;  }, // Start of DP write (4)
            5    => {  try self.write_dp(address.*, data.*, substate - 4);                   },
            6    => {  self.finish(0);                                                       }, // End
            
            else => unreachable
        }
    }

    pub inline fn alu_a_with_d_x(self: *SPC, substate: u32, comptime op: AluOp) !void {
        const address = &self.data_u8[0];
        const data    = &self.data_u8[1];

        sw: switch (substate) {
            0, 1 => {  try self.fetch(substate);                               }, // Fetch
            2    => {  try self.idle();                                        }, // Idle
            3    => {  address.* = self.last_read_byte(); continue :sw 4;      }, // Start of DP read (3)
            4    => {  try self.read_dp(address.* +% self.x(), substate - 3);  },
            5    => {  data.* = self.last_read_byte();                        // Perform ALU operation - End
                       self.do_alu_op(&self.state.a, data.*, op);           
                       self.finish(0);                                         },

            else => unreachable
        }
    }

    pub inline fn alu_a_with_abs_x(self: *SPC, substate: u32, comptime op: AluOp) !void {
        const address = &self.data_u16[0];
        const data    = &self.data_u8[0];

        sw: switch (substate) {
            0...3 => {  try self.fetch_word(substate);                       }, // Fetch
            4     => {  try self.idle();                                     }, // Idle
            5     => {  address.* = self.last_read_word(); continue :sw 6;   }, // Start of ABS read (5)
            6     => {  try self.read(address.* +% self.x(), substate - 5);  },
            7     => {  data.* = self.last_read_byte();                        // Perform ALU operation - End
                        self.do_alu_op(&self.state.a, data.*, op);           
                        self.finish(0);                                      },
            
            else => unreachable
        }
    }

    pub inline fn alu_a_with_abs_y(self: *SPC, substate: u32, comptime op: AluOp) !void {
        const address = &self.data_u16[0];
        const data    = &self.data_u8[0];

        sw: switch (substate) {
            0...3 => {  try self.fetch_word(substate);                       }, // Fetch
            4     => {  try self.idle();                                     }, // Idle
            5     => {  address.* = self.last_read_word(); continue :sw 6;   }, // Start of ABS read (5)
            6     => {  try self.read(address.* +% self.y(), substate - 5);  },
            7     => {  data.* = self.last_read_byte();                        // Perform ALU operation - End
                        self.do_alu_op(&self.state.a, data.*, op);           
                        self.finish(0);                                      },
            
            else => unreachable
        }
    }

    pub inline fn alu_a_with_d_ind_y(self: *SPC, substate: u32, comptime op: AluOp) !void {
        const indirect = &self.data_u8[0];
        const data     = &self.data_u8[1];
        const address  = &self.data_u16[0];

        sw: switch (substate) {
            0, 1  => {  try self.fetch(substate);                                     }, // Fetch
            2     => {  try self.idle();                                              }, // Idle
            3     => {  indirect.* = self.last_read_byte(); continue :sw 4;           }, // Start of DP word read (3)
            4...6 => {  try self.read_dp_word(indirect.*, substate - 3);              },
            7     => {  address.* = self.last_read_word(); continue :sw 8;            }, // Start of ABS read (7)
            8     => {  try self.read(address.* +% self.y(), substate - 7);           },
            9     => {  data.* = self.last_read_byte();                                  // Perform ALU operation - End
                        self.do_alu_op(&self.state.a, data.*, op);           
                        self.finish(0);                                               },

            else => unreachable
        }
    }

    pub inline fn alu_d_with_imm(self: *SPC, substate: u32, comptime op: AluOp) !void {
        const immediate = &self.data_u8[0];
        const address   = &self.data_u8[1];
        const data      = &self.data_u8[2];
        
        sw: switch (substate) {
            0, 1  => {  try self.fetch(substate);                               }, // Fetch immediate
            2     => {  immediate.* = self.last_read_byte(); continue :sw 3;    }, // Start of DP fetch (2)
            3     => {  try self.fetch(substate - 2);                           }, 
            4     => {  address.* = self.last_read_byte(); continue :sw 5;      }, // Start of DP read (4)
            5     => {  try self.read_dp(address.*, substate - 4);              }, 
            6     => {  data.* = self.last_read_byte();                            // Start of DP write(6) - After perform ALU operation
                        self.do_alu_op(data, immediate.*, op); continue :sw 7;  },
            7     => {  try self.write_dp(address.*, data.*, substate - 6);     }, 
            8     => {  self.finish(0);                                         }, // End

            else => unreachable
        }
    }

    pub inline fn alu_x_ind_with_y_ind(self: *SPC, substate: u32, comptime op: AluOp) !void {
        const lhs = &self.data_u8[0];
        const rhs = &self.data_u8[1];
        
        sw: switch (substate) {
            0, 1 => {  try self.dummy_read(self.pc(), substate);          }, // Dummy read
            2, 3 => {  try self.read_dp(self.y(), substate - 2);          }, // DP read
            4    => {  rhs.* = self.last_read_byte(); continue :sw 5;     }, // Start of DP write (4)
            5    => {  try self.read_dp(self.x(), substate - 4);          }, 
            6    => {  lhs.* = self.last_read_byte();                        // Start of DP write (6) - After perform ALU operation
                       self.do_alu_op(lhs, rhs.*, op); continue :sw 7;    },
            7    => {  try self.write_dp(self.x(), lhs.*, substate - 6);  }, 
            8    => {  self.finish(0);                                    }, // End

            else => unreachable
        }
    }

    pub inline fn decw_d(self: *SPC, substate: u32) !void {
        const address = &self.data_u8[0];
        const data    = &self.data_u16[0];

        sw: switch (substate) {
            0, 1 => {  try self.fetch(substate);                                                }, // Fetch DP address
            2    => {  address.* = self.last_read_byte(); continue :sw 3;                       }, // Start of DP read (2) - Low byte
            3    => {  try self.read_dp(address.*, substate - 2);                               },
            4    => {  data.* = @as(u16, self.last_read_byte()) -% 1; continue :sw 5;           }, // Start of DP write (4) - Low byte
            5    => {  try self.write_dp(address.*, @intCast(data.* & 0xFF), substate - 4);     },
            6, 7 => {  try self.read_dp(address.* +% 1, substate - 6);                          }, // Start of DP read (6) - High byte
            8    => {  data.* +%= @as(u16, self.last_read_byte()) << 8; continue :sw 9;         }, // Start of DP write (8) - High byte
            9    => {  try self.write_dp(address.* +% 1, @intCast(data.* >> 8), substate - 8);  },
            10   => {  self.state.set_z(@intFromBool(data.* == 0));                                // Perform ALU operation - End
                       self.state.set_n(@intFromBool(data.* & 0x8000 != 0));       
                       self.finish(0);                                                          },

            else => unreachable
        }
    }

    pub inline fn alu_modify_d_x(self: *SPC, substate: u32, comptime op: AluModifyOp) !void {
        const address = &self.data_u8[0];
        const data    = &self.data_u8[1];

        sw: switch (substate) {   
            0, 1 => {  try self.fetch(substate);                                        }, // Fetch
            2    => {  try self.idle();                                                 }, // Idle
            3    => {  address.* = self.last_read_byte(); continue :sw 4;               }, // Start of DP read (3)
            4    => {  try self.read_dp(address.* +% self.x(), substate - 3);           },
            5    => {  data.* = self.last_read_byte();                                     // Start of DP write (5)
                       self.do_alu_modify_op(data, op); continue :sw 6;                 },
            6    => {  try self.write_dp(address.* +% self.x(), data.*, substate - 5);  },
            7    => {  self.finish(0);                                                  }, // End

            else => unreachable
        }
    }

    pub inline fn alu_modify_a(self: *SPC, substate: u32, comptime op: AluModifyOp) !void {
        switch (substate) {   
            0, 1 => {  try self.dummy_read(self.pc(), substate);  }, // Dummy read
            2    => {  self.do_alu_modify_op(&self.state.a, op);     // Perform ALU operation - End
                       self.finish(0);                            },

            else => unreachable
        }
    }

    pub inline fn alu_modify_x(self: *SPC, substate: u32, comptime op: AluModifyOp) !void {
        switch (substate) {   
            0, 1 => {  try self.dummy_read(self.pc(), substate);  }, // Dummy read
            2    => {  self.do_alu_modify_op(&self.state.x, op);     // Perform ALU operation - End
                       self.finish(0);                            },

            else => unreachable
        }
    }

    pub inline fn alu_x_with_abs(self: *SPC, substate: u32, comptime op: AluOp) !void {
        const address = &self.data_u16[0];
        const data    = &self.data_u8[0];

        sw: switch (substate) {
            0...3 => {  try self.fetch_word(substate);                      }, // Fetch
            4     => {  address.* = self.last_read_word(); continue :sw 5;  }, // Start of ABS read (4)
            5     => {  try self.read(address.*, substate - 4);             },
            6     => {  data.* = self.last_read_byte();                        // Perform ALU operation - End
                        self.do_alu_op(&self.state.x, data.*, op);           
                        self.finish(0);                                     },
            
            else => unreachable
        }
    }

    pub inline fn jmp_abs_x_ind(self: *SPC, substate: u32) !void {
        const address = &self.data_u16[0];

        sw: switch (substate) {
            0...3 => {  try self.fetch_word(substate);                            }, // Fetch
            4     => {  try self.idle();                                          }, // Idle
            5     => {  address.* = self.last_read_word(); continue :sw 6;        }, // Start of ABS read (5)
            6...8 => {  try self.read_word(address.* +% self.x(), substate - 5);  },
            9     => {  self.state.pc = self.last_read_word();                       // Set PC - End
                        self.finish(0);                                           },

            else => unreachable
        }
    }

    pub inline fn clrp(self: *SPC, substate: u32) !void {
        switch (substate) {
            0, 1 => {  try self.dummy_read(self.pc(), substate);  }, // Dummy read
            2    => {  self.state.set_p(0);                          // Set flag - End
                       self.finish(0);                            },

            else => unreachable
        }
    }

    pub inline fn cbne_d(self: *SPC, substate: u32) !void {
        const address = &self.data_u8[0];
        const data    = &self.data_u8[1];
        const offset  = &self.data_i8[0];

        sw: switch (substate) {
            0, 1 => {  try self.fetch(substate);                             }, // Fetch
            2    => {  address.* = self.last_read_byte(); continue :sw 3;    }, // Start of DP read (2)
            3    => {  try self.read_dp(address.*, substate - 2);            },
            4    => {  try self.idle();                                      }, // Idle
            5    => {  data.* = self.last_read_byte(); continue :sw 6;       }, // Start of next fetch (5)
            6    => {  try self.fetch(substate - 5);                         },
            7    => {  if (self.a() == data.*) { self.finish(0); }              // End if branch condition is false
                       else { try self.idle(); }                             }, // Idle
            8    => {  try self.idle();                                      }, // Idle
            9    => {  offset.* = @bitCast(self.last_read_byte());              // End
                       self.state.pc +%= @bitCast(@as(i16, offset.*));  
                       self.finish(0);                                       },
            
            else => unreachable
        }
    }

    pub inline fn incw_d(self: *SPC, substate: u32) !void {
        const address = &self.data_u8[0];
        const data    = &self.data_u16[0];

        sw: switch (substate) {
            0, 1 => {  try self.fetch(substate);                                                }, // Fetch DP address
            2    => {  address.* = self.last_read_byte(); continue :sw 3;                       }, // Start of DP read (2) - Low byte
            3    => {  try self.read_dp(address.*, substate - 2);                               },
            4    => {  data.* = @as(u16, self.last_read_byte()) +% 1; continue :sw 5;           }, // Start of DP write (4) - Low byte
            5    => {  try self.write_dp(address.*, @intCast(data.* & 0xFF), substate - 4);     },
            6, 7 => {  try self.read_dp(address.* +% 1, substate - 6);                          }, // Start of DP read (6) - High byte
            8    => {  data.* +%= @as(u16, self.last_read_byte()) << 8; continue :sw 9;         }, // Start of DP write (8) - High byte
            9    => {  try self.write_dp(address.* +% 1, @intCast(data.* >> 8), substate - 8);  },
            10   => {  self.state.set_z(@intFromBool(data.* == 0));                                // Perform ALU operation - End
                       self.state.set_n(@intFromBool(data.* & 0x8000 != 0));       
                       self.finish(0);                                                          },

            else => unreachable
        }
    }

    pub inline fn alu_x_with_d(self: *SPC, substate: u32, comptime op: AluOp) !void {
        const address = &self.data_u8[0];
        const data    = &self.data_u8[1];

        sw: switch (substate) {
            0, 1 => {  try self.fetch(substate);                           }, // Fetch
            2    => {  address.* = self.last_read_byte(); continue :sw 3;  }, // Start of DP read (2)
            3    => {  try self.read_dp(address.*, substate - 2);          },
            4    => {  data.* = self.last_read_byte();                        // Perform ALU operation - End
                       self.do_alu_op(&self.state.x, data.*, op);           
                       self.finish(0);                                     },

            else => unreachable
        }
    }

    pub inline fn call(self: *SPC, substate: u32) !void {
        const address = &self.data_u16[0];

        sw: switch (substate) {
            0...3 => {  try self.fetch_word(substate);                      }, // Fetch
            4     => {  try self.idle();                                    },
            5     => {  address.* = self.last_read_word(); continue :sw 6;  }, // Start of push word (5)
            6...8 => {  try self.push_word(self.pc(), substate - 5);        },
            9     => {  try self.idle();                                    },
            10    => {  try self.idle();                                    },
            11    => {  self.state.pc = address.*;                             // Set PC - End
                        self.finish(0);                                     },

            else => unreachable
        }
    }

    pub inline fn setp(self: *SPC, substate: u32) !void {
        switch (substate) {
            0, 1 => {  try self.dummy_read(self.pc(), substate);  }, // Dummy read
            2    => {  self.state.set_p(1);                          // Set flag - End
                       self.finish(0);                            },

            else => unreachable
        }
    }

    pub inline fn tclr1(self: *SPC, substate: u32) !void {
        const address = &self.data_u16[0];
        const data    = &self.data_u8[0];

        sw: switch (substate) {
            0...3 => {  try self.fetch_word(substate);                                }, // Fetch word
            4     => {  address.* = self.last_read_word(); continue :sw 5;            }, // Start of ABS read (4)
            5     => {  try self.read(address.*, substate - 4);                       },
            6     => {  data.* = self.last_read_byte();                                  // Start of ABS dummy read (6)
                        const diff = self.a() -% data.*;          
                        self.state.set_z(@intFromBool(diff == 0));          
                        self.state.set_n(@intCast(diff >> 7 & 1)); continue :sw 7;    },
            7     => {  try self.dummy_read(address.*, substate - 6);                 },
            8, 9  => {  try self.write(address.*, data.* & ~self.a(), substate - 8);  }, // ABS write
            10    => {  self.finish(0);                                               }, // End

            else => unreachable
        }
    }

    pub inline fn pcall(self: *SPC, substate: u32) !void {
        const address = &self.data_u8[0];

        sw: switch (substate) {
            0, 1  => {  try self.fetch(substate);                           }, // Fetch
            2     => {  try self.idle();                                    },
            3     => {  address.* = self.last_read_byte(); continue :sw 4;  }, // Start of push word (3)
            4...6 => {  try self.push_word(self.pc(), substate - 3);        },
            7     => {  try self.idle();                                    },
            8     => {  self.state.pc = 0xFF00 | @as(u16, address.*);          // Set PC - End
                        self.finish(0);                                     },

            else => unreachable
        }
    }

    pub inline fn cmpw(self: *SPC, substate: u32) !void {
        const address = &self.data_u8[0];
        const data    = &self.data_u16[0];

        const op = AluWordOp.cmpw;

        sw: switch (substate) {
            0, 1  => {  try self.fetch(substate);                                }, // Fetch
            2     => {  address.* = self.last_read_byte(); continue :sw 3;       }, // Start of DP word read (2)
            3...5 => {  try self.read_dp_word(address.*, substate - 2);          },
            6     => {  data.* = self.last_read_word();                             // Perform ALU operation - End
                        _ = self.do_alu_word_op(self.ya(), data.*, op);
                        self.finish(0);                                          },

            else => unreachable
        }
    }

    pub inline fn mov_reg_reg(self: *SPC, substate: u32, lhs: *u8, rhs: *u8) !void {
        switch (substate) {
            0, 1 => {  try self.dummy_read(self.pc(), substate);           }, // Dummy read
            2    => {  lhs.* = rhs.*;                                         // Transfer and set flags - End
                       self.state.set_z(@intFromBool(lhs.* == 0));            // Perform ALU operation - End
                       self.state.set_n(@intFromBool(lhs.* & 0x80 != 0)); 
                       self.finish(0);                                     },

            else => unreachable
        }
    }

    pub inline fn alu_y_with_abs(self: *SPC, substate: u32, comptime op: AluOp) !void {
        const address = &self.data_u16[0];
        const data    = &self.data_u8[0];

        sw: switch (substate) {
            0...3 => {  try self.fetch_word(substate);                      }, // Fetch
            4     => {  address.* = self.last_read_word(); continue :sw 5;  }, // Start of ABS read (4)
            5     => {  try self.read(address.*, substate - 4);             },
            6     => {  data.* = self.last_read_byte();                        // Perform ALU operation - End
                        self.do_alu_op(&self.state.y, data.*, op);           
                        self.finish(0);                                     },
            
            else => unreachable
        }
    }

    pub inline fn jmp_abs(self: *SPC, substate: u32) !void {
        switch (substate) {
            0...3 => {  try self.fetch_word(substate);          }, // Fetch
            4     => {  self.state.pc = self.last_read_word();     // Set PC - End
                        self.finish(0);                         },

            else => unreachable
        }
    }

    pub inline fn clrc(self: *SPC, substate: u32) !void {
        switch (substate) {
            0, 1 => {  try self.dummy_read(self.pc(), substate);  }, // Dummy read
            2    => {  self.state.set_c(0);                          // Set flag - End
                       self.finish(0);                            },

            else => unreachable
        }
    }



    pub inline fn cmp_d_with_d(self: *SPC, substate: u32) !void {
        const op = AluOp.cmp;

        const address_d = &self.data_u8[0];
        const address_s = &self.data_u8[1];
        const data_l    = &self.data_u8[2];
        const data_r    = &self.data_u8[3];

        sw: switch (substate) {
            0, 1 => {  try self.fetch(substate);                                }, // Fetch
            2    => {  address_s.* = self.last_read_byte(); continue :sw 3;     }, // Start of DP read (2)
            3    => {  try self.read_dp(address_s.*, substate - 2);             },
            4    => {  data_r.* = self.last_read_byte(); continue :sw 5;        }, // Start of fetch (4)
            5    => {  try self.fetch(substate - 4);                            },
            6    => {  address_d.* = self.last_read_byte(); continue :sw 7;     }, // Start of DP read (6)
            7    => {  try self.read_dp(address_d.*, substate - 6);             },
            8    => {  data_l.* = self.last_read_byte();                           // Idle
                       self.do_alu_op(data_l, data_r.*, op);
                       try self.idle();                                         },
            9    => {  self.finish(0);                                          }, // End

            else => unreachable
        }
    }

    pub inline fn dbnz_d(self: *SPC, substate: u32) !void {
        const address = &self.data_u8[0];
        const data    = &self.data_u8[1];
        const offset  = &self.data_i8[0];

        sw: switch (substate) {
            0, 1 => {  try self.fetch(substate);                                 }, // Fetch
            2    => {  address.* = self.last_read_byte(); continue :sw 3;        }, // Start of DP read (2)
            3    => {  try self.read_dp(address.*, substate - 2);                },
            4    => {  data.* = self.last_read_byte(); continue :sw 5;           }, // Start of DP write (4)
            5    => {  try self.write_dp(address.*, data.* -% 1, substate - 4);  },
            6    => {  data.* -%= 1; continue :sw 7;                             }, // Start of fetch (6)
            7    => {  try self.fetch(substate - 6);                             },
            8    => {  if (data.* == 0) { self.finish(0); }                         // End if branch condition is false
                       else { try self.idle(); }                                 }, // Idle
            9    => {  try self.idle();                                          },
            10   => {  offset.* = @bitCast(self.last_read_byte());                  // End
                       self.state.pc +%= @bitCast(@as(i16, offset.*));  
                       self.finish(0);                                           },

            else => unreachable
        }
    }

    pub inline fn ret(self: *SPC, substate: u32) !void {
        switch (substate) {
            0, 1  => {  try self.dummy_read(self.pc(), substate);  }, // Dummy read
            2     => {  try self.idle();                           }, // Idle
            3...6 => {  try self.pull_word(substate - 3);          }, // Pull word
            7     => {  self.state.pc = self.last_read_word();        // End
                        self.finish(0);                            },

            else => unreachable
        }
    }



    pub inline fn cmp_d_with_imm(self: *SPC, substate: u32) !void {
        const op = AluOp.cmp;

        const immediate = &self.data_u8[0];
        const address   = &self.data_u8[1];
        const data      = &self.data_u8[2];
        
        sw: switch (substate) {
            0, 1  => {  try self.fetch(substate);                               }, // Fetch immediate
            2     => {  immediate.* = self.last_read_byte(); continue :sw 3;    }, // Start of DP fetch (2)
            3     => {  try self.fetch(substate - 2);                           }, 
            4     => {  address.* = self.last_read_byte(); continue :sw 5;      }, // Start of DP read (4)
            5     => {  try self.read_dp(address.*, substate - 4);              }, 
            6     => {  data.* = self.last_read_byte();                            // Idle
                        self.do_alu_op(data, immediate.*, op);
                        try self.idle();                                        },
            7     => {  self.finish(0);                                         }, // End

            else => unreachable
        }
    }

    pub inline fn cmp_x_ind_with_y_ind(self: *SPC, substate: u32) !void {
        const op = AluOp.cmp;

        const lhs = &self.data_u8[0];
        const rhs = &self.data_u8[1];
        
        sw: switch (substate) {
            0, 1 => {  try self.dummy_read(self.pc(), substate);          }, // Dummy read
            2, 3 => {  try self.read_dp(self.y(), substate - 2);          }, // DP read
            4    => {  rhs.* = self.last_read_byte(); continue :sw 5;     }, // Start of DP write (4)
            5    => {  try self.read_dp(self.x(), substate - 4);          }, 
            6    => {  lhs.* = self.last_read_byte();                        // Idle
                       self.do_alu_op(lhs, rhs.*, op);
                       try self.idle();                                   }, 
            7    => {  self.finish(0);                                    }, // End

            else => unreachable
        }
    }

    pub inline fn addw(self: *SPC, substate: u32) !void {
        const address = &self.data_u8[0];
        const data    = &self.data_u16[0];

        const op = AluWordOp.addw;

        sw: switch (substate) {
            0, 1  => {  try self.fetch(substate);                                }, // Fetch
            2     => {  address.* = self.last_read_byte(); continue :sw 3;       }, // Start of DP word read (2)
            3...5 => {  try self.read_dp_word(address.*, substate - 2);          },
            6     => {  try self.idle();                                         }, // Idle
            7     => {  data.* = self.last_read_word();                             // Perform ALU operation - End
                        const res = self.do_alu_word_op(self.ya(), data.*, op);
                        self.set_ya(res);
                        self.finish(0);                                          },

            else => unreachable
        }
    }

    pub inline fn alu_y_with_d(self: *SPC, substate: u32, comptime op: AluOp) !void {
        const address = &self.data_u8[0];
        const data    = &self.data_u8[1];

        sw: switch (substate) {
            0, 1 => {  try self.fetch(substate);                           }, // Fetch
            2    => {  address.* = self.last_read_byte(); continue :sw 3;  }, // Start of DP read (2)
            3    => {  try self.read_dp(address.*, substate - 2);          },
            4    => {  data.* = self.last_read_byte();                        // Perform ALU operation - End
                       self.do_alu_op(&self.state.y, data.*, op);           
                       self.finish(0);                                     },

            else => unreachable
        }
    }

    pub inline fn reti(self: *SPC, substate: u32) !void {
        sw: switch (substate) {
            0, 1  => {  try self.dummy_read(self.pc(), substate);                }, // Dummy read
            2     => {  try self.idle();                                         }, // Idle
            3, 4  => {  try self.pull(substate - 3);                             }, // Pull 
            5     => {  self.state.psw = self.last_read_byte(); continue :sw 6;  }, // Start of pull word (5)
            6...8 => {  try self.pull_word(substate - 5);                        }, 
            9     => {  self.state.pc = self.last_read_word();                      // End
                        self.finish(0);                                          },

            else => unreachable
        }
    }

    pub inline fn setc(self: *SPC, substate: u32) !void {
        switch (substate) {
            0, 1 => {  try self.dummy_read(self.pc(), substate);  }, // Dummy read
            2    => {  self.state.set_c(1);                          // Set flag - End
                       self.finish(0);                            },

            else => unreachable
        }
    }

    pub inline fn alu_y_with_imm(self: *SPC, substate: u32, comptime op: AluOp) !void {
        const data = &self.data_u8[0];
        
        switch (substate) {
            0, 1  => {  try self.fetch(substate);                   }, // Fetch
            2     => {  data.* = self.last_read_byte();                // Perform ALU operation - End
                        self.do_alu_op(&self.state.y, data.*, op);
                        self.finish(0);                             },

            else => unreachable
        }
    }

    pub inline fn pop_reg(self: *SPC, substate: u32, reg: *u8) !void {
        switch (substate) {
            0, 1 => {  try self.dummy_read(self.pc(), substate);  }, // Dummy read
            2    => {  try self.idle();                           }, // Idle
            3, 4 => {  try self.pull(substate - 3);               }, // Pull
            5    => {  reg.* = self.last_read_byte();                // Set register - End
                       self.finish(0);                            },

            else => unreachable
        }
    }

    pub inline fn subw(self: *SPC, substate: u32) !void {
        const address = &self.data_u8[0];
        const data    = &self.data_u16[0];

        const op = AluWordOp.subw;

        sw: switch (substate) {
            0, 1  => {  try self.fetch(substate);                                }, // Fetch
            2     => {  address.* = self.last_read_byte(); continue :sw 3;       }, // Start of DP word read (2)
            3...5 => {  try self.read_dp_word(address.*, substate - 2);          },
            6     => {  try self.idle();                                         }, // Idle
            7     => {  data.* = self.last_read_word();                             // Perform ALU operation - End
                        const res = self.do_alu_word_op(self.ya(), data.*, op);
                        self.set_ya(res);
                        self.finish(0);                                          },

            else => unreachable
        }
    }

    pub inline fn div(self: *SPC, substate: u32) !void {
        switch (substate) {
            0, 1   => {  try self.dummy_read(self.pc(), substate);                          }, // Dummy read
            2...11 => {  try self.idle();                                                   }, // Idle for 10 cycles - simulates the wait time it takes for division to process
            12     => {  const top = self.ya();                                                // Perform division - End
                         self.state.set_h(@intFromBool(self.y() & 0xF >= self.x() & 0xF));
                         // The high bit of what would be the 9-bit quotient goes into the overflow flag
                         self.state.set_v(@intFromBool(self.y() >= self.x()));
                         if (@as(u16, self.y()) < @as(u16, self.x()) << 1) { // Seems like this is the SPC's creative way of checking if quotient would be less than 512
                             // In which case, perform normal integer division
                             self.state.a = @intCast(top / self.x() & 0xFF);
                             self.state.y = @intCast(top % self.x() & 0xFF);
                         }
                         else {
                             // Otherwise... I have absolutely no idea what this is even trying to do here
                             // But it replicates the "glitchy" results you'd get if the quotient can't fit within 9 bits
                             self.state.a = @intCast(255      - (top - (@as(u16, self.x()) << 9)) / (@as(u16, 256) - self.x()) & 0xFF);
                             self.state.y = @intCast(self.x() + (top - (@as(u16, self.x()) << 9)) % (@as(u16, 256) - self.x()) & 0xFF);
                         }
                         // Set N and Z flags based on quotient value
                         self.state.set_z(@intFromBool(self.a() == 0));
                         self.state.set_n(@intFromBool(self.a() & 0x80 != 0));
                         self.finish(0);                                                    },

            else => unreachable
        }
    }

    pub inline fn xcn(self: *SPC, substate: u32) !void {
        switch (substate) {
            0, 1  => {  try self.dummy_read(self.pc(), substate);               }, // Dummy read
            2...4 => {  try self.idle();                                        }, // Idle
            5     => {  self.state.a = self.a() >> 4 | (self.a() & 0x0F) << 4;
                        self.state.set_z(@intFromBool(self.a() == 0));
                        self.state.set_n(@intFromBool(self.a() & 0x80 != 0));
                        self.finish(0);                                         }, // Perform exchange - End

            else => unreachable
        }
    }

    pub inline fn ei(self: *SPC, substate: u32) !void {
        switch (substate) {
            0, 1 => {  try self.dummy_read(self.pc(), substate);  }, // Dummy read
            2    => {  try self.idle();                           }, // Idle
            3    => {  self.state.set_i(1);                          // Set flag - End
                       self.finish(0);                            },

            else => unreachable
        }
    }

    pub inline fn mov_x_ind_inc_a(self: *SPC, substate: u32) !void {
        switch (substate) {
            0, 1 => {  try self.dummy_read(self.pc(), substate);             }, // Dummy read
            2    => {  try self.idle();                                      }, // Idle
            3, 4 => {  try self.write_dp(self.x(), self.a(), substate - 3);  }, // DP write
            5    => {  self.state.x = self.x() +% 1;                            // Increment X - End
                       self.finish(0);                                       },

            else => unreachable
        }
    }
};