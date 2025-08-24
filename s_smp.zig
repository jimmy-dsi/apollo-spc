const std = @import("std");

const Emu       = @import("emu.zig").Emu;
const SDSP      = @import("s_dsp.zig").SDSP;
const SMPState  = @import("smp_state.zig").SMPState;
const SPC       = @import("spc.zig").SPC;
const CoManager = @import("co_mgr.zig").CoManager;
const CoState   = @import("co_state.zig").CoState;

const Co = CoState.Co;

pub const SSMP = struct {
    pub const cycles_per_sample: u32 = SDSP.cycles_per_sample / 2;
    pub const clock_rate:        u32 = SDSP.sample_rate * cycles_per_sample;

    pub const Options = struct {
        a: ?u8 = null,
        x: ?u8 = null,
        y: ?u8 = null,

        sp: ?u8  = null,
        pc: ?u16 = null,

        psw: ?u8 = null
    };

    pub const boot_rom_embedded = @embedFile("data/bootrom.bin");

    pub const AccessType = enum {
        none,
        read, write, exec, fetch, dummy_read
    };

    pub const AccessLog = struct {
        type: AccessType = AccessType.none,

        dsp_cycle:  u64,
        address:    u16,
        pre_data:   ?u8 = null,
        write_data: ?u8 = null,
        post_data:  ?u8 = null
    };

    pub const TimerLogType = enum {
        none,
        enable, disable,
        step, read, reset
    };

    pub const TimerLog = struct {
        type: TimerLogType = TimerLogType.none,

        dsp_cycle: u64,
        timer_number: ?u32,
        internal_counter: u8,
        output: u4
    };

    const State = enum {
        init,
        main
    };

    const cycle_wait_states = [4]u32 { 2, 4, 10, 20 };
    const timer_wait_states = [4]u32 { 2, 4,  8, 16 };

    emu: *Emu,

    boot_rom: [] const u8 = boot_rom_embedded,
    state: SMPState,
    co:    CoManager,

    exec_state: State = State.init,

    last_opcode:        u8 = 0x00,
    last_read_bytes: [3]u8 = [3]u8 { 0x00, 0x00, 0x00 },

    enable_access_logs: bool = false,
    access_logs: [256]AccessLog = undefined,
    last_log_index: u32 = 0,

    enable_timer_logs: bool = false,
    timer_logs: [256]TimerLog = undefined,
    last_timer_log_index: u32 = 0,

    instr_counter: u64 = 0,

    timer_wait_cycles: u32 = 0,

    cur_exec_cycle:  u64 = 0,
    prev_exec_cycle: u64 = 0,

    spc: SPC,

    pub fn new(emu: *Emu, options: Options) SSMP {
        var s_smp = SSMP {
            .emu   = emu,
            .state = SMPState { },
            .co    = CoManager.new(),

            .spc = SPC.new(
                emu,
                options.a,
                options.x,
                options.y,
                options.sp,
                options.pc,
                options.psw
            )
        };
        s_smp.power_on();
        return s_smp;
    }

    pub fn power_on(self: *SSMP) void {
        self.exec_state = State.init;
        self.spc.power_on();
        self.reset();
    }

    pub fn reset(self: *SSMP) void {
        self.spc.reset();
        self.spc.state.pc = self.boot_rom[0x3E] | @as(u16, self.boot_rom[0x3F]) << 8;
    }

    pub fn step(self: *SSMP) void {
        if (!self.co.waiting()) {
            if (self.timer_wait_cycles > 0) {
                self.step_timers();
            }
            self.main() catch {};
        }
        if (self.co.null_transition()) {
            if (self.timer_wait_cycles > 0) {
                self.step_timers();
            }
            self.main() catch {};
        }
        self.co.step();
    }

    inline fn step_timers(self: *SSMP) void {
        inline for (0..3) |index| {
            const timer  = &self.state.timer_states[index];
            const output = &self.state.timer_outputs[index];

            const prev_stage_2 = timer.stage_2;
            const prev_output  = output.*;

            timer.step(
                &self.state,
                self.timer_wait_cycles,
                index,
                if (index == 2) 16 else 128
            );

            if (self.enable_timer_logs) {
                if (prev_stage_2 != timer.stage_2 or prev_output != output.*) {
                    self.append_timer_step_log(index);
                }
            }
        }
        self.timer_wait_cycles = 0;
    }

    pub fn main(self: *SSMP) !void {
        sw: switch (self.exec_state) {
            State.init => {
                // Stagger SMP process cycles so that it occurs on every 2nd DSP cycle
                self.exec_state = State.main;
                // Delay by 1 DSP cycle
                self.co.finish(1);
            },
            State.main => {
                try self.run_next_instr();
                continue :sw State.main;
            }
        }
    }

    pub fn get_access_logs(self: *SSMP, options: struct { exclude_at_end: u32 = 0 }) []AccessLog {
        if (options.exclude_at_end > self.last_log_index) {
            return self.access_logs[0..self.last_log_index];
        }
        else {
            return self.access_logs[0..(self.last_log_index - options.exclude_at_end)];
        }
    }

    pub fn clear_access_logs(self: *SSMP) void {
        self.access_logs[0] = .{ .dsp_cycle = 0, .address = 0 };
        self.last_log_index = 0;
    }

    inline fn append_read_log(self: *SSMP, address: u16, data: u8) void {
        self.append_access_log(
            AccessLog {
                .type = AccessType.read,
                .dsp_cycle = self.s_dsp().cur_cycle(),
                .address = address,
                .post_data = data
            }
        );
    }

    inline fn append_write_log(self: *SSMP, address: u16, pre_data: u8, write_data_: u8, post_data: u8) void {
        self.append_access_log(
            AccessLog {
                .type = AccessType.write,
                .dsp_cycle = self.s_dsp().cur_cycle(),
                .address = address,
                .pre_data = pre_data,
                .write_data = write_data_,
                .post_data = post_data
            }
        );
    }

    inline fn append_exec_log(self: *SSMP, address: u16) void {
        self.append_access_log(
            AccessLog {
                .type = AccessType.exec,
                .dsp_cycle = self.s_dsp().cur_cycle(),
                .address = address
            }
        );
    }

    inline fn append_fetch_log(self: *SSMP, address: u16) void {
        self.append_access_log(
            AccessLog {
                .type = AccessType.fetch,
                .dsp_cycle = self.s_dsp().cur_cycle(),
                .address = address
            }
        );
    }

    inline fn append_dummy_read_log(self: *SSMP, address: u16) void {
        self.append_access_log(
            AccessLog {
                .type = AccessType.dummy_read,
                .dsp_cycle = self.s_dsp().cur_cycle(),
                .address = address
            }
        );
    }

    inline fn append_access_log(self: *SSMP, entry: AccessLog) void {
        if (self.last_log_index < self.access_logs.len) {
            self.access_logs[self.last_log_index] = entry;
            self.last_log_index += 1;
        }
        if (self.last_log_index < self.access_logs.len) {
            self.access_logs[self.last_log_index] = .{ .dsp_cycle = 0, .address = 0 };
        }
    }

    pub fn get_timer_logs(self: *SSMP, options: struct { exclude_at_end: u32 = 0 }) []TimerLog {
        if (options.exclude_at_end > self.last_timer_log_index) {
            return self.timer_logs[0..self.last_timer_log_index];
        }
        else {
            return self.timer_logs[0..(self.last_timer_log_index - options.exclude_at_end)];
        }
    }

    pub fn clear_timer_logs(self: *SSMP) void {
        self.last_timer_log_index = 0;
    }

    inline fn append_timer_enable_log(self: *SSMP, timer_index: ?u32) void {
        self.append_timer_log(
            TimerLog {
                .type = TimerLogType.enable,
                .dsp_cycle = self.s_dsp().cur_cycle(),
                .timer_number = timer_index,
                .internal_counter = if (timer_index == null) 0 else self.state.timer_states[timer_index.?].stage_2,
                .output = if (timer_index == null) 0 else self.state.timer_outputs[timer_index.?]
            }
        );
    }

    inline fn append_timer_disable_log(self: *SSMP, timer_index: ?u32) void {
        self.append_timer_log(
            TimerLog {
                .type = TimerLogType.disable,
                .dsp_cycle = self.s_dsp().cur_cycle(),
                .timer_number = timer_index,
                .internal_counter = if (timer_index == null) 0 else self.state.timer_states[timer_index.?].stage_2,
                .output = if (timer_index == null) 0 else self.state.timer_outputs[timer_index.?]
            }
        );
    }

    inline fn append_timer_step_log(self: *SSMP, timer_index: u32) void {
        self.append_timer_log(
            TimerLog {
                .type = TimerLogType.step,
                .dsp_cycle = self.s_dsp().cur_cycle(),
                .timer_number = timer_index,
                .internal_counter = self.state.timer_states[timer_index].stage_2,
                .output = self.state.timer_outputs[timer_index]
            }
        );
    }

    inline fn append_timer_read_log(self: *SSMP, timer_index: u32) void {
        self.append_timer_log(
            TimerLog {
                .type = TimerLogType.read,
                .dsp_cycle = self.s_dsp().cur_cycle(),
                .timer_number = timer_index,
                .internal_counter = self.state.timer_states[timer_index].stage_2,
                .output = self.state.timer_outputs[timer_index]
            }
        );
    }

    inline fn append_timer_reset_log(self: *SSMP, timer_index: u32) void {
        self.append_timer_log(
            TimerLog {
                .type = TimerLogType.reset,
                .dsp_cycle = self.s_dsp().cur_cycle(),
                .timer_number = timer_index,
                .internal_counter = self.state.timer_states[timer_index].stage_2,
                .output = self.state.timer_outputs[timer_index]
            }
        );
    }

    inline fn append_timer_log(self: *SSMP, entry: TimerLog) void {
        if (self.last_timer_log_index < self.timer_logs.len) {
            self.timer_logs[self.last_timer_log_index] = entry;
            self.last_timer_log_index += 1;
        }
    }

    inline fn run_next_instr(self: *SSMP) !void {
        @setEvalBranchQuota(5000);

        const substate = self.co.substate();

        switch (substate) {
            0, 1 => {
                if (substate == 0) {
                    if (self.enable_access_logs) {
                        self.append_exec_log(self.spc.pc());
                    }
                    self.prev_exec_cycle = self.cur_exec_cycle;
                    self.cur_exec_cycle  = self.s_dsp().*.cur_cycle();
                    self.instr_counter += 1;
                    //std.debug.print("SMP | Current DSP cycle: {d} | PC: {d}\n", .{self.s_dsp().*.cur_cycle(), self.spc.pc()});
                }
                _ = try self.fetch(substate);
            },
            else => {
                // Store to last_opcode only when we first get to this step
                // Otherwise, the emulator will attempt to repeatedly overwrite the last opcode while an instruction is still running
                if (substate == 2) {
                    self.last_opcode = self.last_read_bytes[0];
                }
                try self.exec_opcode(substate - 2);
            }
        }
    }

    inline fn exec_opcode(self: *SSMP, substate_offset: u32) !void {
        const opcode = self.last_opcode;
        
        switch (opcode) {
            0x00 => { try self.spc.nop(substate_offset);                                               }, // nop
            0x01 => { try self.spc.tcall(substate_offset, 0);                                          }, // tcall 0
            0x02 => { try self.spc.set1(substate_offset, 0);                                           }, // set1 dp.0
            0x03 => { try self.spc.branch_bit(substate_offset, 0, 1);                                  }, // bbs dp.0, r
            0x04 => { try self.spc.alu_a_with_d(substate_offset, SPC.AluOp.bitor);                     }, // or a, dp
            0x05 => { try self.spc.alu_a_with_abs(substate_offset, SPC.AluOp.bitor);                   }, // or a, addr
            0x06 => { try self.spc.alu_a_with_x_ind(substate_offset, SPC.AluOp.bitor);                 }, // or a, (x)
            0x07 => { try self.spc.alu_a_with_d_x_ind(substate_offset, SPC.AluOp.bitor);               }, // or a, [dp+x]
            0x08 => { try self.spc.alu_a_with_imm(substate_offset, SPC.AluOp.bitor);                   }, // or a, #im
            0x09 => { try self.spc.alu_d_with_d(substate_offset, SPC.AluOp.bitor);                     }, // or dp, dp
            0x0A => { try self.spc.alu_c_with_mem_1bit(substate_offset, SPC.AluOp.bitor, false);       }, // or1 c, mem.b
            0x0B => { try self.spc.alu_modify_d(substate_offset, SPC.AluModifyOp.asl);                 }, // asl dp
            0x0C => { try self.spc.alu_modify_abs(substate_offset, SPC.AluModifyOp.asl);               }, // asl addr
            0x0D => { try self.spc.push_reg(substate_offset, &self.spc.state.psw);                     }, // push psw
            0x0E => { try self.spc.tset1(substate_offset);                                             }, // tset1 addr
            0x0F => { try self.spc.brk(substate_offset);                                               }, // brk
            0x10 => { try self.spc.branch(substate_offset, self.spc.n() == 0);                         }, // bpl r
            0x11 => { try self.spc.tcall(substate_offset, 1);                                          }, // tcall 1
            0x12 => { try self.spc.clr1(substate_offset, 0);                                           }, // clr1 dp.0
            0x13 => { try self.spc.branch_bit(substate_offset, 0, 0);                                  }, // bbc dp.0, r
            0x14 => { try self.spc.alu_a_with_d_x(substate_offset, SPC.AluOp.bitor);                   }, // or a, dp+x
            0x15 => { try self.spc.alu_a_with_abs_x(substate_offset, SPC.AluOp.bitor);                 }, // or a, addr+x
            0x16 => { try self.spc.alu_a_with_abs_y(substate_offset, SPC.AluOp.bitor);                 }, // or a, addr+y
            0x17 => { try self.spc.alu_a_with_d_ind_y(substate_offset, SPC.AluOp.bitor);               }, // or a, [dp]+y
            0x18 => { try self.spc.alu_d_with_imm(substate_offset, SPC.AluOp.bitor);                   }, // or dp, #im
            0x19 => { try self.spc.alu_x_ind_with_y_ind(substate_offset, SPC.AluOp.bitor);             }, // or (x), (y)
            0x1A => { try self.spc.decw_d(substate_offset);                                            }, // decw dp
            0x1B => { try self.spc.alu_modify_d_x(substate_offset, SPC.AluModifyOp.asl);               }, // asl dp+x
            0x1C => { try self.spc.alu_modify_a(substate_offset, SPC.AluModifyOp.asl);                 }, // asl a
            0x1D => { try self.spc.alu_modify_x(substate_offset, SPC.AluModifyOp.dec);                 }, // dec x
            0x1E => { try self.spc.alu_x_with_abs(substate_offset, SPC.AluOp.cmp);                     }, // cmp x, addr
            0x1F => { try self.spc.jmp_abs_x_ind(substate_offset);                                     }, // jmp [addr+x]
            0x20 => { try self.spc.clrp(substate_offset);                                              }, // clrp
            0x21 => { try self.spc.tcall(substate_offset, 2);                                          }, // tcall 2
            0x22 => { try self.spc.set1(substate_offset, 1);                                           }, // set1 dp.1
            0x23 => { try self.spc.branch_bit(substate_offset, 1, 1);                                  }, // bbs dp.1, r
            0x24 => { try self.spc.alu_a_with_d(substate_offset, SPC.AluOp.bitand);                    }, // and a, dp
            0x25 => { try self.spc.alu_a_with_abs(substate_offset, SPC.AluOp.bitand);                  }, // and a, addr
            0x26 => { try self.spc.alu_a_with_x_ind(substate_offset, SPC.AluOp.bitand);                }, // and a, (x)
            0x27 => { try self.spc.alu_a_with_d_x_ind(substate_offset, SPC.AluOp.bitand);              }, // and a, [dp+x]
            0x28 => { try self.spc.alu_a_with_imm(substate_offset, SPC.AluOp.bitand);                  }, // and a, #im
            0x29 => { try self.spc.alu_d_with_d(substate_offset, SPC.AluOp.bitand);                    }, // and dp, dp
            0x2A => { try self.spc.alu_c_with_mem_1bit(substate_offset, SPC.AluOp.bitor, true);        }, // or1 c, /mem.b
            0x2B => { try self.spc.alu_modify_d(substate_offset, SPC.AluModifyOp.rol);                 }, // rol dp
            0x2C => { try self.spc.alu_modify_abs(substate_offset, SPC.AluModifyOp.rol);               }, // rol addr
            0x2D => { try self.spc.push_reg(substate_offset, &self.spc.state.a);                       }, // push a
            0x2E => { try self.spc.cbne_d(substate_offset);                                            }, // cbne dp, r
            0x2F => { try self.spc.branch(substate_offset, true);                                      }, // bra r
            0x30 => { try self.spc.branch(substate_offset, self.spc.n() != 0);                         }, // bmi r
            0x31 => { try self.spc.tcall(substate_offset, 3);                                          }, // tcall 3
            0x32 => { try self.spc.clr1(substate_offset, 1);                                           }, // clr1 dp.1
            0x33 => { try self.spc.branch_bit(substate_offset, 1, 0);                                  }, // bbc dp.1, r
            0x34 => { try self.spc.alu_a_with_d_x(substate_offset, SPC.AluOp.bitand);                  }, // and a, dp+x
            0x35 => { try self.spc.alu_a_with_abs_x(substate_offset, SPC.AluOp.bitand);                }, // and a, addr+x
            0x36 => { try self.spc.alu_a_with_abs_y(substate_offset, SPC.AluOp.bitand);                }, // and a, addr+y
            0x37 => { try self.spc.alu_a_with_d_ind_y(substate_offset, SPC.AluOp.bitand);              }, // and a, [dp]+y
            0x38 => { try self.spc.alu_d_with_imm(substate_offset, SPC.AluOp.bitand);                  }, // and dp, #im
            0x39 => { try self.spc.alu_x_ind_with_y_ind(substate_offset, SPC.AluOp.bitand);            }, // and (x), (y)
            0x3A => { try self.spc.incw_d(substate_offset);                                            }, // incw dp
            0x3B => { try self.spc.alu_modify_d_x(substate_offset, SPC.AluModifyOp.rol);               }, // rol dp+x
            0x3C => { try self.spc.alu_modify_a(substate_offset, SPC.AluModifyOp.rol);                 }, // rol a
            0x3D => { try self.spc.alu_modify_x(substate_offset, SPC.AluModifyOp.inc);                 }, // inc x
            0x3E => { try self.spc.alu_x_with_d(substate_offset, SPC.AluOp.cmp);                       }, // cmp x, dp
            0x3F => { try self.spc.call(substate_offset);                                              }, // call addr
            0x40 => { try self.spc.setp(substate_offset);                                              }, // setp
            0x41 => { try self.spc.tcall(substate_offset, 4);                                          }, // tcall 4
            0x42 => { try self.spc.set1(substate_offset, 2);                                           }, // set1 dp.2
            0x43 => { try self.spc.branch_bit(substate_offset, 2, 1);                                  }, // bbs dp.2, r
            0x44 => { try self.spc.alu_a_with_d(substate_offset, SPC.AluOp.bitxor);                    }, // eor a, dp
            0x45 => { try self.spc.alu_a_with_abs(substate_offset, SPC.AluOp.bitxor);                  }, // eor a, addr
            0x46 => { try self.spc.alu_a_with_x_ind(substate_offset, SPC.AluOp.bitxor);                }, // eor a, (x)
            0x47 => { try self.spc.alu_a_with_d_x_ind(substate_offset, SPC.AluOp.bitxor);              }, // eor a, [dp+x]
            0x48 => { try self.spc.alu_a_with_imm(substate_offset, SPC.AluOp.bitxor);                  }, // eor a, #im
            0x49 => { try self.spc.alu_d_with_d(substate_offset, SPC.AluOp.bitxor);                    }, // eor dp, dp
            0x4A => { try self.spc.alu_c_with_mem_1bit(substate_offset, SPC.AluOp.bitand, false);      }, // and1 c, mem.b
            0x4B => { try self.spc.alu_modify_d(substate_offset, SPC.AluModifyOp.lsr);                 }, // lsr dp
            0x4C => { try self.spc.alu_modify_abs(substate_offset, SPC.AluModifyOp.lsr);               }, // lsr addr
            0x4D => { try self.spc.push_reg(substate_offset, &self.spc.state.x);                       }, // push x
            0x4E => { try self.spc.tclr1(substate_offset);                                             }, // tclr1 addr
            0x4F => { try self.spc.pcall(substate_offset);                                             }, // pcall up
            0x50 => { try self.spc.branch(substate_offset, self.spc.v() == 0);                         }, // bvc r
            0x51 => { try self.spc.tcall(substate_offset, 5);                                          }, // tcall 5
            0x52 => { try self.spc.clr1(substate_offset, 2);                                           }, // clr1 dp.2
            0x53 => { try self.spc.branch_bit(substate_offset, 2, 0);                                  }, // bbc dp.2, r
            0x54 => { try self.spc.alu_a_with_d_x(substate_offset, SPC.AluOp.bitxor);                  }, // eor a, dp+x
            0x55 => { try self.spc.alu_a_with_abs_x(substate_offset, SPC.AluOp.bitxor);                }, // eor a, addr+x
            0x56 => { try self.spc.alu_a_with_abs_y(substate_offset, SPC.AluOp.bitxor);                }, // eor a, addr+y
            0x57 => { try self.spc.alu_a_with_d_ind_y(substate_offset, SPC.AluOp.bitxor);              }, // eor a, [dp]+y
            0x58 => { try self.spc.alu_d_with_imm(substate_offset, SPC.AluOp.bitxor);                  }, // eor dp, #im
            0x59 => { try self.spc.alu_x_ind_with_y_ind(substate_offset, SPC.AluOp.bitxor);            }, // eor (x), (y)
            0x5A => { try self.spc.cmpw(substate_offset);                                              }, // cmpw ya, dp
            0x5B => { try self.spc.alu_modify_d_x(substate_offset, SPC.AluModifyOp.lsr);               }, // lsr dp+x
            0x5C => { try self.spc.alu_modify_a(substate_offset, SPC.AluModifyOp.lsr);                 }, // lsr a
            0x5D => { try self.spc.mov_reg_reg(substate_offset, &self.spc.state.x, &self.spc.state.a); }, // mov x, a
            0x5E => { try self.spc.alu_y_with_abs(substate_offset, SPC.AluOp.cmp);                     }, // cmp y, addr
            0x5F => { try self.spc.jmp_abs(substate_offset);                                           }, // jmp addr
            0x60 => { try self.spc.clrc(substate_offset);                                              }, // clrc
            else => { try self.spc.clrc(substate_offset);                                              },
        }
    }

    pub inline fn cur_cycle(self: *SSMP) u64 {
        return self.emu.*.s_dsp.clock_counter / 2;
    }

    pub inline fn s_dsp(self: *const SSMP) *SDSP {
        return &self.emu.*.s_dsp;
    }

    pub inline fn fetch(self: *SSMP, substate_offset: u32) !u8 {
        switch (substate_offset) {
            0, 1 => {
                if (substate_offset == 1) {
                    self.append_fetch_log(self.spc.pc());
                }
                const result = self.read(self.spc.pc(), substate_offset);
                if (substate_offset == 1) {
                    self.spc.step_pc();
                }
                _ = try result;
                unreachable;
            },
            2 => {
                return self.last_read_bytes[0];
            },
            else => unreachable
        }
    }

    pub inline fn push(self: *SSMP, data: u8, substate_offset: u32) !void {
        switch (substate_offset) {
            0, 1 => {
                const result = self.write(0x100 | self.spc.sp(), data, substate_offset);
                if (substate_offset == 1) { self.spc.dec_sp(); }
                try result;
            },
            else => unreachable
        }
    }

    pub inline fn fetch_word(self: *SSMP, substate_offset: u32) !u16 {
        switch (substate_offset) {
            // First, fetch low byte
            0, 1 => {
                _ = try self.fetch(substate_offset);
                unreachable;
            },
            // Next, pull fetch byte
            2, 3 => {
                _ = try self.fetch(substate_offset - 2);
                unreachable;
            },
            4 => {
                const hi = self.last_read_bytes[0]; // high byte == most recently read
                const lo = self.last_read_bytes[1]; // low byte == the one before that

                return lo | @as(u16, hi) << 8;
            },
            else => unreachable
        }
    }

    pub inline fn push_word(self: *SSMP, data: u16, substate_offset: u32) !void {
        switch (substate_offset) {
            // First, push high byte
            0, 1 => {
                try self.push(@intCast(data >> 8), substate_offset);
            },
            // Next, push low byte
            2, 3 => {
                try self.push(@intCast(data & 0xFF), substate_offset - 2);
            },
            else => unreachable
        }
    }

    pub inline fn pull(self: *SSMP, substate_offset: u32) !u8 {
        switch (substate_offset) {
            0 => {
                self.spc.inc_sp();
                _ = try self.read(self.spc.pc(), substate_offset);
                unreachable;
            },
            1 => {
                try self.read(self.spc.pc(), substate_offset);
                unreachable;
            },
            2 => {
                return self.last_read_bytes[0];
            },
            else => unreachable
        }
    }

    pub inline fn pull_word(self: *SSMP, substate_offset: u32) !u16 {
        switch (substate_offset) {
            // First, pull low byte
            0, 1 => {
                _ = try self.pull(substate_offset);
                unreachable;
            },
            // Next, pull high byte
            2, 3 => {
                _ = try self.pull(substate_offset - 2);
                unreachable;
            },
            4 => {
                const hi = self.last_read_bytes[0]; // high byte == most recently read
                const lo = self.last_read_bytes[1]; // low byte == the one before that

                return lo | @as(u16, hi) << 8;
            },
            else => unreachable
        }
    }

    pub inline fn dummy_read(self: *SSMP, address: u16, substate_offset: u32) !void {
        switch (substate_offset) {
            0, 1 => {
                if (substate_offset == 1) {
                    self.append_dummy_read_log(address);
                }
                const result = self.read(address, substate_offset);
                _ = try result;
                unreachable;
            },
            2 => {
                return;
            },
            else => unreachable
        }
    }

    pub fn read(self: *SSMP, address: u16, substate_offset: u32) !u8 {
        switch (substate_offset) {
            0 => {
                if (address & 0xFFFC == 0x00F4) {
                    // From Ares source code: "reads from $00f4-$00f7 require more time than internal reads"
                    // So I guess to compensate for that, the SMP schedules the start of the read sooner than it would otherwise?
                    try self.wait(true, address);
                    unreachable;
                }
                else {
                    try self.wait(false, address);
                    unreachable;
                }
            },
            1 => {
                // Shift and load last read bytes buffer
                self.last_read_bytes[2] = self.last_read_bytes[1];
                self.last_read_bytes[1] = self.last_read_bytes[0];
                self.last_read_bytes[0] = self.read_data(address);

                if (self.enable_access_logs) {
                    self.append_read_log(address, self.last_read_bytes[0]);
                }

                const delay = 
                    if (address & 0xFFFC == 0x00F4) b: {
                        const d, _ = self.get_wait_cycles(true, address);
                        break :b d;
                    }
                    else
                        0
                    ;

                try self.co.wait(delay);
                unreachable;
            },
            2 => {
                return self.last_read_bytes[0];
            },
            else => unreachable
        }
    }

    pub inline fn read_word(self: *SSMP, address: u16, substate_offset: u32) !u16 {
        switch (substate_offset) {
            // First, read low byte:
            0, 1 => {
                _ = try self.read(address, substate_offset);
                unreachable;
            },
            // Next, read high byte:
            2, 3 => {
                _ = try self.read(address +% 1, substate_offset - 2);
                unreachable;
            },
            4 => {
                const hi = self.last_read_bytes[0]; // high byte == most recently read
                const lo = self.last_read_bytes[1]; // low byte == the one before that

                return lo | @as(u16, hi) << 8;
            },
            else => unreachable
        }
    }

    pub inline fn read_data(self: *SSMP, address: u16) u8 {
        if (address & 0xFFF0 == 0x00F0) {
            const result = self.read_io(address);
            return result;
        }
        else {
            const result = self.read_ram(address);
            return result;
        }
    }

    pub fn read_ram(self: *const SSMP, address: u16) u8 {
        if (address >= 0xFFC0 and self.state.use_boot_rom == 1) {
            return self.boot_rom[address & 0x3F];
        }
        else if (self.state.ram_disable == 1) {
            return 0x5A;
        }
        else {
            return self.s_dsp().audio_ram[address];
        }
    }

    pub inline fn read_io(self: *SSMP, address: u16) u8 {
        switch (address) {
            0x00F0...0x00FC => { // IO registers which don't modify state
                return self.debug_read_io(address);
            },
            0x00FD => { // T0OUT (read triggers SMP state change)
                const last_out = self.state.timer_outputs[0];
                self.state.timer_outputs[0] = 0x0; // Reset output to zero

                if (self.enable_timer_logs) {
                    self.append_timer_read_log(0);
                }

                return last_out;
            },
            0x00FE => { // T1OUT (read triggers SMP state change)
                const last_out = self.state.timer_outputs[1];
                self.state.timer_outputs[1] = 0x0; // Reset output to zero

                if (self.enable_timer_logs) {
                    self.append_timer_read_log(1);
                }

                return last_out;
            },
            0x00FF => { // T2OUT (read triggers SMP state change)
                const last_out = self.state.timer_outputs[2];
                self.state.timer_outputs[2] = 0x0; // Reset output to zero

                if (self.enable_timer_logs) {
                    self.append_timer_read_log(2);
                }

                return last_out;
            },
            else => unreachable
        }
    }

    pub inline fn debug_read_data(self: *const SSMP, address: u16) u8 {
        if (address & 0xFFF0 == 0x00F0) {
            const result = self.debug_read_io(address);
            return result;
        }
        else {
            const result = self.read_ram(address);
            return result;
        }
    }

    pub fn debug_read_io(self: *const SSMP, address: u16) u8 {
        switch (address) {
            0x00F0 => { // TEST (write-only)
                return 0x00;
            },
            0x00F1 => { // CONTROL (write-only)
                return 0x00;
            },
            0x00F2 => { // DSPADDR
                return self.state.dsp_address;
            },
            0x00F3 => { // DSPDATA
                return self.s_dsp().read(self.state.dsp_address);
            },
            0x00F4 => { // CPUIO0
                return self.state.input_ports[0];
            },
            0x00F5 => { // CPUIO1
                return self.state.input_ports[1];
            },
            0x00F6 => { // CPUIO2
                return self.state.input_ports[2];
            },
            0x00F7 => { // CPUIO3
                return self.state.input_ports[3];
            },
            0x00F8 => { // AUXIO4
                return self.state.aux[0];
            },
            0x00F9 => { // AUXIO5
                return self.state.aux[1];
            },
            0x00FA => { // T0TARGET (write-only)
                return 0x00;
            },
            0x00FB => { // T1TARGET (write-only)
                return 0x00;
            },
            0x00FC => { // T2TARGET (write-only)
                return 0x00;
            },
            0x00FD => { // T0OUT
                return self.state.timer_outputs[0];
            },
            0x00FE => { // T1OUT
                return self.state.timer_outputs[1];
            },
            0x00FF => { // T2OUT
                return self.state.timer_outputs[2];
            },
            else => unreachable
        }
    }

    pub fn write(self: *SSMP, address: u16, data: u8, substate_offset: u32) !void {
        switch (substate_offset) {
            0 => {
                try self.wait(false, address);
            },
            1 => {
                const pre_data = self.debug_read_data(address);
                self.write_data(address, data);
                const post_data = self.debug_read_data(address);

                if (self.enable_access_logs) {
                    self.append_write_log(address, pre_data, data, post_data);
                }

                try self.co.wait(0); // Trigger immediate state advance
            },
            else => unreachable
        }
    }

    pub inline fn write_data(self: *SSMP, address: u16, data: u8) void {
        // All writes will write to the underlying RAM unconditionally, even if it's an IO write
        self.write_ram(address, data);
        if (address & 0xFFF0 == 0x00F0) {
            self.write_io(address, data);
        }
    }

    pub fn write_ram(self: *const SSMP, address: u16, data: u8) void {
        // Even writes to the BootROM region will always write to the underlying RAM
        if (self.state.ram_write_enable == 1 and self.state.ram_disable == 0) {
            self.s_dsp().*.audio_ram[address] = data;
        }
    }

    pub fn write_io(self: *SSMP, address: u16, data: u8) void {
        switch (address) {
            0x00F0 => { // TEST
                if (self.spc.p() == 0) { // Apparently writes only work here when the direct page flag is set to 0
                    const prev_timer_enable = self.state.global_timer_disable == 0 and self.state.global_timer_enable == 1;

                    self.state.global_timer_disable = @intCast(data & 1);
                    self.state.ram_write_enable     = @intCast(data >> 1 & 1);
                    self.state.ram_disable          = @intCast(data >> 2 & 1);
                    self.state.global_timer_enable  = @intCast(data >> 3 & 1);
                    self.state.ram_waitstates       = @intCast(data >> 4 & 0b11);
                    self.state.io_waitstates        = @intCast(data >> 6 & 0b11);

                    const new_timer_enable = self.state.global_timer_disable == 0 and self.state.global_timer_enable == 1;

                    if (self.enable_timer_logs) {
                        if (!prev_timer_enable and new_timer_enable) {
                            self.append_timer_enable_log(null);
                        }
                        else if (prev_timer_enable and !new_timer_enable) {
                            self.append_timer_disable_log(null);
                        }
                    }

                    // Synchronize timers
                    inline for (0..3) |index| {
                        const timer  = &self.state.timer_states[index];
                        const output = &self.state.timer_outputs[index];

                        const prev_stage_2 = timer.stage_2;
                        const prev_output  = output.*;

                        timer.step_stage_1(&self.state, index);

                        if (self.enable_timer_logs) {
                            if (prev_stage_2 != timer.stage_2 or prev_output != output.*) {
                                self.append_timer_step_log(index);
                            }
                        }
                    }
                }
            },
            0x00F1 => { // CONTROL
                if (data >> 4 & 1 == 1) {
                    self.state.input_ports[0] = 0x00;
                    self.state.input_ports[1] = 0x00;
                }

                if (data >> 5 & 1 == 1) {
                    self.state.input_ports[2] = 0x00;
                    self.state.input_ports[3] = 0x00;
                }

                inline for (0..3) |index| {
                    const enable: u1 = @intCast(data >> index & 1);

                    // 0->1 transition resets timers
                    if (self.state.timer_on_flags[index] == 0 and enable == 1) {
                        if (self.enable_timer_logs) {
                            self.append_timer_enable_log(index);
                            self.append_timer_reset_log(index);
                        }
                        self.state.timer_states[index].reset(&self.state, index);
                    }
                    else if (self.enable_timer_logs and self.state.timer_on_flags[index] == 1 and enable == 0) {
                        self.append_timer_disable_log(index);
                    }

                    self.state.timer_on_flags[index] = enable;
                }

                self.state.use_boot_rom = @intCast(data >> 7 & 1);
            },
            0x00F2 => { // DSPADDR
                self.state.dsp_address = data;
            },
            0x00F3 => { // DSPDATA
                self.s_dsp().write(self.state.dsp_address, data);
            },
            0x00F4 => { // CPUIO0
                self.state.output_ports[0] = data;
            },
            0x00F5 => { // CPUIO1
                self.state.output_ports[1] = data;
            },
            0x00F6 => { // CPUIO2
                self.state.output_ports[2] = data;
            },
            0x00F7 => { // CPUIO3
                self.state.output_ports[3] = data;
            },
            0x00F8 => { // AUXIO4
                self.state.aux[0] = data;
            },
            0x00F9 => { // AUXIO5
                self.state.aux[1] = data;
            },
            0x00FA => { // T0TARGET
                self.state.timer_dividers[0] = data;
            },
            0x00FB => { // T1TARGET
                self.state.timer_dividers[1] = data;
            },
            0x00FC => { // T2TARGET
                self.state.timer_dividers[2] = data;
            },
            0x00FD => { }, // T0OUT (read-only)
            0x00FE => { }, // T1OUT (read-only)
            0x00FF => { }, // T2OUT (read-only)
            else => unreachable
        }
    }

    pub fn idle(self: *SSMP) !void {
        const wait_states  = self.state.io_waitstates;
        const dsp_cycles   = cycle_wait_states[wait_states];
        const timer_cycles = timer_wait_states[wait_states];

        self.timer_wait_cycles = timer_cycles;
        try self.co.wait(dsp_cycles);
    }

    inline fn wait(self: *SSMP, halve: bool, address: u16) !void {
        const dsp_cycles, const timer_cycles = self.get_wait_cycles(halve, address);
        self.timer_wait_cycles = timer_cycles;
        try self.co.wait(dsp_cycles);
    }

    inline fn get_wait_cycles(self: *SSMP, halve: bool, address: u16) struct {u32, u32} {
        const wait_states =
            if (address & 0xFFF0 == 0x00F0 or address >= 0xFFC0 and self.state.use_boot_rom == 1)
                self.state.io_waitstates
            else
                self.state.ram_waitstates
            ;

        var dsp_cycles   = cycle_wait_states[wait_states];
        var timer_cycles = timer_wait_states[wait_states];

        if (halve) {
            dsp_cycles /= 2;
            timer_cycles /= 2;
        }

        return .{dsp_cycles, timer_cycles};
    }
};