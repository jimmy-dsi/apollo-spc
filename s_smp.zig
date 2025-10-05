const std = @import("std");

const RingBuffer = @import("common/ring_buf.zig").RingBuffer;

const Emu       = @import("emu.zig").Emu;
const SDSP      = @import("s_dsp.zig").SDSP;
const SMPState  = @import("smp_state.zig").SMPState;
const SPC       = @import("spc.zig").SPC;
const SPCState  = @import("spc_state.zig").SPCState;
const CoManager = @import("co_mgr.zig").CoManager;
const CoState   = @import("co_state.zig").CoState;

const Co = CoState.Co;

pub const SSMP = struct {
    pub const cycles_per_sample: u32 = SDSP.cycles_per_sample / 2;
    pub const clock_rate:        u32 = SDSP.sample_rate * cycles_per_sample;

    pub const LogBuffer = RingBuffer(AccessLog, 256);

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
        post_data:  ?u8 = null,

        s700_consumed: bool = false,
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
    cur_debug_mode:  Emu.DebugMode = Emu.DebugMode.none,
    next_debug_mode: Emu.DebugMode = Emu.DebugMode.none,

    last_opcode:        u8 = 0x00,
    last_read_bytes: [3]u8 = [3]u8 { 0x00, 0x00, 0x00 },

    enable_access_logs: bool = false,
    access_logs: LogBuffer = .{},

    enable_timer_logs: bool = false,
    timer_logs: [256]TimerLog = undefined,
    last_timer_log_index: u32 = 0,

    instr_boundary:     bool = false,
    next_is_force_exit: bool = false,
    vector_changed:     bool = false,
    next_vector:        u16  = 0xFFDE,

    timer_wait_cycles: u32 = 0,

    cur_exec_cycle:  u64 = 0,
    prev_exec_cycle: u64 = 0,

    spc: SPC,
    prev_spc_state: SPCState = undefined,

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
        self.exec_state = State.main;
        self.spc.power_on();
        self.reset();
    }

    pub fn reset(self: *SSMP) void {
        self.spc.reset();
        self.spc.state.pc = self.boot_rom[0x3E] | @as(u16, self.boot_rom[0x3F]) << 8;
        self.prev_spc_state = self.spc.state;
    }

    pub fn update_interrupt_vector(self: *SSMP, address: u16) void {
        // Queue vector change (Takes effect on the start of next instruction)
        self.next_vector    = address;
        self.vector_changed = true;
    }

    pub fn enable_shadow_mode(self: *SSMP) void {
        self.next_debug_mode = Emu.DebugMode.shadow_mode;
        self.attempt_shadow_transition();
    }

    pub fn enable_shadow_execution(self: *SSMP) void {
        self.next_debug_mode = Emu.DebugMode.shadow_exec;
        self.attempt_shadow_transition();
    }

    pub fn disable_shadow_mode(self: *SSMP) void {
        self.next_debug_mode = Emu.DebugMode.shadow_exec;
        self.attempt_shadow_transition();
    }

    pub fn disable_shadow_execution(self: *SSMP, force_exit: bool) void {
        self.next_debug_mode    = Emu.DebugMode.none;
        self.next_is_force_exit = force_exit;
        self.attempt_shadow_transition();
    }

    pub inline fn attempt_shadow_transition(self: *SSMP) void {
        if (self.instr_boundary) { // Only allow shadow mode transitions in between instruction executions
            self.apply_spc_debug_mode_transition();
        }
    }

    pub fn step(self: *SSMP) void {
        if (self.co.null_transition(.{})) {
            if (self.instr_boundary) {
                self.prev_spc_state = self.spc.state;
            }
            self.instr_boundary = false;
        }
        
        if (!self.co.waiting()) {
            if (self.cur_debug_mode == Emu.DebugMode.shadow_mode) {
                self.timer_wait_cycles = 0; // Don't allow SMP timers to step while shadow mode is enabled
            }

            if (self.timer_wait_cycles > 0) {
                self.step_timers();
            }

            self.main() catch {};
        }

        if (self.co.null_transition(.{.no_reset = true})) {
            const substate = self.co.substate();
            if (substate == 0) {
                self.instr_boundary = true;
                self.prev_exec_cycle = self.cur_exec_cycle;
                self.cur_exec_cycle  = self.s_dsp().cur_cycle();

                self.change_interrupt_mode();

                // Update SPC interrupt vector if necessary
                if (self.vector_changed) {
                    self.spc.current_interrupt_vector = self.next_vector;
                    self.vector_changed = false;
                }

                // Apply debug mode transitions if applicable
                self.maybe_transition_debug_mode();
            }
        }
        else {
            self.co.step();
        }
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
        switch (self.exec_state) {
            State.init => { },
            State.main => {
                try self.run_next_instr();
                //continue :sw State.main;
            }
        }
    }

    pub fn receive_port_value(self: *SSMP, port_index: u2, value: u8) void {
        self.state.input_ports[port_index] = value;
        self.emu.script700.state.port_in[port_index] = value; // Reflect on Script700 side
    }

    pub fn trigger_interrupt(self: *SSMP, vector: ?u16) void {
        _ = self.spc.trigger_interrupt(vector);
        if (self.instr_boundary) {
            self.change_interrupt_mode();
        }
    }

    pub fn get_access_logs(self: *SSMP, _: struct { exclude_at_end: u32 = 0 }) LogBuffer.Iter {
        return self.access_logs.iter(false);
    }

    pub fn get_access_logs_range(self: *SSMP, start_cycle: u64) LogBuffer.Iter {
        var iter = self.access_logs.iter(true);
        while (iter.step()) {
            if (iter.value().dsp_cycle < start_cycle) {
                break;
            }
        }

        var fw_iter = iter.get_reversed();
        _ = fw_iter.step();

        return fw_iter;
    }

    pub fn clear_access_logs(self: *SSMP) void {
        self.access_logs = .{};
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
        self.access_logs.push(entry);
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
        @setEvalBranchQuota(10000);

        const substate = self.co.substate();

        sw: switch (substate) {
            0, 1 => {
                if (substate == 0) {
                    if (self.enable_access_logs or self.emu.script700.enabled) {
                        self.append_exec_log(self.spc.pc());
                    }
                    //std.debug.print("SMP | Current DSP cycle: {d} | PC: {d}\n", .{self.s_dsp().*.cur_cycle(), self.spc.pc()});
                }

                // Perform fetch only if SPC is in the normal mode of execution (ie. not asleep or stopped)
                if (self.spc.mode() == SPCState.Mode.normal) {
                    _ = try self.fetch(substate);
                }
                else {
                    continue :sw 2;
                }
            },
            else => {
                // Store to last_opcode only when we first get to this step
                // Or else, if we didn't do this, the emulator would attempt to repeatedly overwrite the last opcode while an instruction is still running
                if (self.spc.mode() == SPCState.Mode.normal and substate == 2) {
                    self.last_opcode = self.last_read_bytes[0];
                }
                try self.exec_opcode(
                    if (self.spc.mode() == SPCState.Mode.normal)
                        substate - 2
                    else
                        substate
                );
            }
        }
    }

    inline fn maybe_transition_debug_mode(self: *SSMP) void {
        const pc = self.spc.pc();

        // If PC is on the outer cusp of the end of the shadow region, automatically disable Shadow Execution/Shadow Mode
        if (self.cur_debug_mode != Emu.DebugMode.none and self.in_shadow_region(pc, 3) and !self.in_shadow_region(pc, 0)) {
            self.emu.disable_shadow_execution(.{.set_as_master = true});
        }
        // Otherwise, if the shadow region is exited via other means (i.e. call instruction), end Shadow Mode if applicable
        else if (self.cur_debug_mode == Emu.DebugMode.shadow_mode and self.next_debug_mode != Emu.DebugMode.none and !self.in_shadow_region(pc, 0) and !self.emu.debug_persist_shadow_mode) {
            self.emu.disable_shadow_mode(.{});
        }
        // If the shadow region has been *re-entered* (i.e. via ret instruction), reapply Shadow Mode if applicable
        else if (self.cur_debug_mode == Emu.DebugMode.shadow_exec and self.in_shadow_region(pc, 0) and self.emu.master_debug_mode == Emu.DebugMode.shadow_mode) {
            self.emu.enable_shadow_mode(.{});
        }
        // If shadow execution is enabled and a STOP instruction has been hit, end shadow execution
        else if (self.cur_debug_mode != Emu.DebugMode.none and self.last_opcode == 0xFF) {
            self.emu.disable_shadow_execution(.{.set_as_master = true});
        }
        // Or if we are pending a change to the current debug mode for any other reason
        else if (self.cur_debug_mode != Emu.DebugMode.none and self.next_debug_mode == Emu.DebugMode.none) {
            self.emu.disable_shadow_execution(.{.set_as_master = true});
        }
    }

    inline fn apply_spc_debug_mode_transition(self: *SSMP) void {
        if (self.next_debug_mode != self.cur_debug_mode) {
            if (self.cur_debug_mode == Emu.DebugMode.shadow_mode) {
                self.emu.unpause_sdsp();
            }
            else if (self.next_debug_mode == Emu.DebugMode.shadow_mode) {
                self.emu.pause_sdsp();
            }
        
            if (self.cur_debug_mode == Emu.DebugMode.none) {
                self.spc.enable_shadow_execution();
            }
            else if (self.next_debug_mode == Emu.DebugMode.none) {
                self.spc.disable_shadow_execution(self.next_is_force_exit);
                self.next_is_force_exit = false;
            }

            self.cur_debug_mode = self.next_debug_mode;
        }
    }

    inline fn change_interrupt_mode(self: *SSMP) void {
        if (self.spc.pending_interrupt()) {
            // If previously pending interrupt, kick off the execution of interrupt mode for this "instruction"
            self.spc.state.pending_interrupt = false;
            self.spc.state.mode = SPCState.Mode.interrupt;
        }
        else if (self.spc.mode() == SPCState.Mode.interrupt) {
            // End interrupt mode if SPC was in interrupt mode for the previous "instruction"
            self.spc.state.mode = SPCState.Mode.normal;
        }
    }

    pub inline fn in_shadow_region(self: *const SSMP, address: u16, padding: u16) bool {
        const length_until_end: u32 = @intCast(0x1_0000 - @as(u32, self.spc.shadow_start));

        if (self.spc.shadow_length + padding <= length_until_end) { // Case when Shadow Execution region does not overflow memory space
            return
                address >= self.spc.shadow_start
                and address < self.spc.shadow_start + self.spc.shadow_length + padding;
        }
        else { // Case when Shadow Execution *does* overflow memory space
            return
                address >= self.spc.shadow_start
                or address < (self.spc.shadow_start + self.spc.shadow_length + padding) & 0xFFFF;
        }
    }

    inline fn exec_opcode(self: *SSMP, substate_offset: u32) !void {
        const opcode =
            switch (self.spc.mode()) {
                SPCState.Mode.normal    => self.last_opcode,
                SPCState.Mode.asleep    => 0xEF, // Hardcoded as SLEEP instruction
                SPCState.Mode.stopped   => 0xFF, // Hardcoded as STOP instruction
                SPCState.Mode.interrupt => 0x0F, // Hardcoded as BRK instruction
            };

        const A   = &self.spc.state.a;
        const X   = &self.spc.state.x;
        const Y   = &self.spc.state.y;
        const SP  = &self.spc.state.sp;
        const PSW = &self.spc.state.psw;
        
        switch (opcode) {
            0x00 => { try self.spc.nop(substate_offset);                                                }, // nop
            0x01 => { try self.spc.tcall(substate_offset, 0);                                           }, // tcall 0
            0x02 => { try self.spc.set1(substate_offset, 0);                                            }, // set1 dp.0
            0x03 => { try self.spc.branch_bit(substate_offset, 0, 1);                                   }, // bbs dp.0, r
            0x04 => { try self.spc.alu_reg_with_d(substate_offset, A, SPC.AluOp.bitor);                 }, // or a, dp
            0x05 => { try self.spc.alu_reg_with_abs(substate_offset, A, SPC.AluOp.bitor);               }, // or a, addr
            0x06 => { try self.spc.alu_reg_with_x_ind(substate_offset, A, SPC.AluOp.bitor);             }, // or a, (x)
            0x07 => { try self.spc.alu_reg_with_d_x_ind(substate_offset, A, SPC.AluOp.bitor);           }, // or a, [dp+x]
            0x08 => { try self.spc.alu_reg_with_imm(substate_offset, A, SPC.AluOp.bitor);               }, // or a, #im
            0x09 => { try self.spc.alu_d_with_d(substate_offset, SPC.AluOp.bitor);                      }, // or dp, dp
            0x0A => { try self.spc.alu_c_with_mem_1bit(substate_offset, SPC.AluOp.bitor, false);        }, // or1 c, mem.b
            0x0B => { try self.spc.alu_modify_d(substate_offset, SPC.AluModifyOp.asl);                  }, // asl dp
            0x0C => { try self.spc.alu_modify_abs(substate_offset, SPC.AluModifyOp.asl);                }, // asl addr
            0x0D => { try self.spc.push_reg(substate_offset, PSW);                                      }, // push psw
            0x0E => { try self.spc.tset1(substate_offset);                                              }, // tset1 addr
            0x0F => { try self.spc.brk(substate_offset);                                                }, // brk
            0x10 => { try self.spc.branch(substate_offset, self.spc.n() == 0);                          }, // bpl r
            0x11 => { try self.spc.tcall(substate_offset, 1);                                           }, // tcall 1
            0x12 => { try self.spc.clr1(substate_offset, 0);                                            }, // clr1 dp.0
            0x13 => { try self.spc.branch_bit(substate_offset, 0, 0);                                   }, // bbc dp.0, r
            0x14 => { try self.spc.alu_reg_with_d_reg(substate_offset, A, X, SPC.AluOp.bitor);          }, // or a, dp+x
            0x15 => { try self.spc.alu_reg_with_abs_reg(substate_offset, A, X, SPC.AluOp.bitor);        }, // or a, addr+x
            0x16 => { try self.spc.alu_reg_with_abs_reg(substate_offset, A, Y, SPC.AluOp.bitor);        }, // or a, addr+y
            0x17 => { try self.spc.alu_reg_with_d_ind_y(substate_offset, A, SPC.AluOp.bitor);           }, // or a, [dp]+y
            0x18 => { try self.spc.alu_d_with_imm(substate_offset, SPC.AluOp.bitor);                    }, // or dp, #im
            0x19 => { try self.spc.alu_x_ind_with_y_ind(substate_offset, SPC.AluOp.bitor);              }, // or (x), (y)
            0x1A => { try self.spc.decw_d(substate_offset);                                             }, // decw dp
            0x1B => { try self.spc.alu_modify_d_x(substate_offset, SPC.AluModifyOp.asl);                }, // asl dp+x
            0x1C => { try self.spc.alu_modify_reg(substate_offset, A, SPC.AluModifyOp.asl);             }, // asl a
            0x1D => { try self.spc.alu_modify_reg(substate_offset, X, SPC.AluModifyOp.dec);             }, // dec x
            0x1E => { try self.spc.alu_reg_with_abs(substate_offset, X, SPC.AluOp.cmp);                 }, // cmp x, addr
            0x1F => { try self.spc.jmp_abs_x_ind(substate_offset);                                      }, // jmp [addr+x]
            0x20 => { try self.spc.clrp(substate_offset);                                               }, // clrp
            0x21 => { try self.spc.tcall(substate_offset, 2);                                           }, // tcall 2
            0x22 => { try self.spc.set1(substate_offset, 1);                                            }, // set1 dp.1
            0x23 => { try self.spc.branch_bit(substate_offset, 1, 1);                                   }, // bbs dp.1, r
            0x24 => { try self.spc.alu_reg_with_d(substate_offset, A, SPC.AluOp.bitand);                }, // and a, dp
            0x25 => { try self.spc.alu_reg_with_abs(substate_offset, A, SPC.AluOp.bitand);              }, // and a, addr
            0x26 => { try self.spc.alu_reg_with_x_ind(substate_offset, A, SPC.AluOp.bitand);            }, // and a, (x)
            0x27 => { try self.spc.alu_reg_with_d_x_ind(substate_offset, A, SPC.AluOp.bitand);          }, // and a, [dp+x]
            0x28 => { try self.spc.alu_reg_with_imm(substate_offset, A, SPC.AluOp.bitand);              }, // and a, #im
            0x29 => { try self.spc.alu_d_with_d(substate_offset, SPC.AluOp.bitand);                     }, // and dp, dp
            0x2A => { try self.spc.alu_c_with_mem_1bit(substate_offset, SPC.AluOp.bitor, true);         }, // or1 c, /mem.b
            0x2B => { try self.spc.alu_modify_d(substate_offset, SPC.AluModifyOp.rol);                  }, // rol dp
            0x2C => { try self.spc.alu_modify_abs(substate_offset, SPC.AluModifyOp.rol);                }, // rol addr
            0x2D => { try self.spc.push_reg(substate_offset, A);                                        }, // push a
            0x2E => { try self.spc.cbne_d(substate_offset);                                             }, // cbne dp, r
            0x2F => { try self.spc.branch(substate_offset, true);                                       }, // bra r
            0x30 => { try self.spc.branch(substate_offset, self.spc.n() != 0);                          }, // bmi r
            0x31 => { try self.spc.tcall(substate_offset, 3);                                           }, // tcall 3
            0x32 => { try self.spc.clr1(substate_offset, 1);                                            }, // clr1 dp.1
            0x33 => { try self.spc.branch_bit(substate_offset, 1, 0);                                   }, // bbc dp.1, r
            0x34 => { try self.spc.alu_reg_with_d_reg(substate_offset, A, X, SPC.AluOp.bitand);         }, // and a, dp+x
            0x35 => { try self.spc.alu_reg_with_abs_reg(substate_offset, A, X, SPC.AluOp.bitand);       }, // and a, addr+x
            0x36 => { try self.spc.alu_reg_with_abs_reg(substate_offset, A, Y, SPC.AluOp.bitand);       }, // and a, addr+y
            0x37 => { try self.spc.alu_reg_with_d_ind_y(substate_offset, A, SPC.AluOp.bitand);          }, // and a, [dp]+y
            0x38 => { try self.spc.alu_d_with_imm(substate_offset, SPC.AluOp.bitand);                   }, // and dp, #im
            0x39 => { try self.spc.alu_x_ind_with_y_ind(substate_offset, SPC.AluOp.bitand);             }, // and (x), (y)
            0x3A => { try self.spc.incw_d(substate_offset);                                             }, // incw dp
            0x3B => { try self.spc.alu_modify_d_x(substate_offset, SPC.AluModifyOp.rol);                }, // rol dp+x
            0x3C => { try self.spc.alu_modify_reg(substate_offset, A, SPC.AluModifyOp.rol);             }, // rol a
            0x3D => { try self.spc.alu_modify_reg(substate_offset, X, SPC.AluModifyOp.inc);             }, // inc x
            0x3E => { try self.spc.alu_reg_with_d(substate_offset, X, SPC.AluOp.cmp);                   }, // cmp x, dp
            0x3F => { try self.spc.call(substate_offset);                                               }, // call addr
            0x40 => { try self.spc.setp(substate_offset);                                               }, // setp
            0x41 => { try self.spc.tcall(substate_offset, 4);                                           }, // tcall 4
            0x42 => { try self.spc.set1(substate_offset, 2);                                            }, // set1 dp.2
            0x43 => { try self.spc.branch_bit(substate_offset, 2, 1);                                   }, // bbs dp.2, r
            0x44 => { try self.spc.alu_reg_with_d(substate_offset, A, SPC.AluOp.bitxor);                }, // eor a, dp
            0x45 => { try self.spc.alu_reg_with_abs(substate_offset, A, SPC.AluOp.bitxor);              }, // eor a, addr
            0x46 => { try self.spc.alu_reg_with_x_ind(substate_offset, A, SPC.AluOp.bitxor);            }, // eor a, (x)
            0x47 => { try self.spc.alu_reg_with_d_x_ind(substate_offset, A, SPC.AluOp.bitxor);          }, // eor a, [dp+x]
            0x48 => { try self.spc.alu_reg_with_imm(substate_offset, A, SPC.AluOp.bitxor);              }, // eor a, #im
            0x49 => { try self.spc.alu_d_with_d(substate_offset, SPC.AluOp.bitxor);                     }, // eor dp, dp
            0x4A => { try self.spc.alu_c_with_mem_1bit(substate_offset, SPC.AluOp.bitand, false);       }, // and1 c, mem.b
            0x4B => { try self.spc.alu_modify_d(substate_offset, SPC.AluModifyOp.lsr);                  }, // lsr dp
            0x4C => { try self.spc.alu_modify_abs(substate_offset, SPC.AluModifyOp.lsr);                }, // lsr addr
            0x4D => { try self.spc.push_reg(substate_offset, X);                                        }, // push x
            0x4E => { try self.spc.tclr1(substate_offset);                                              }, // tclr1 addr
            0x4F => { try self.spc.pcall(substate_offset);                                              }, // pcall up
            0x50 => { try self.spc.branch(substate_offset, self.spc.v() == 0);                          }, // bvc r
            0x51 => { try self.spc.tcall(substate_offset, 5);                                           }, // tcall 5
            0x52 => { try self.spc.clr1(substate_offset, 2);                                            }, // clr1 dp.2
            0x53 => { try self.spc.branch_bit(substate_offset, 2, 0);                                   }, // bbc dp.2, r
            0x54 => { try self.spc.alu_reg_with_d_reg(substate_offset, A, X, SPC.AluOp.bitxor);         }, // eor a, dp+x
            0x55 => { try self.spc.alu_reg_with_abs_reg(substate_offset, A, X, SPC.AluOp.bitxor);       }, // eor a, addr+x
            0x56 => { try self.spc.alu_reg_with_abs_reg(substate_offset, A, Y, SPC.AluOp.bitxor);       }, // eor a, addr+y
            0x57 => { try self.spc.alu_reg_with_d_ind_y(substate_offset, A, SPC.AluOp.bitxor);          }, // eor a, [dp]+y
            0x58 => { try self.spc.alu_d_with_imm(substate_offset, SPC.AluOp.bitxor);                   }, // eor dp, #im
            0x59 => { try self.spc.alu_x_ind_with_y_ind(substate_offset, SPC.AluOp.bitxor);             }, // eor (x), (y)
            0x5A => { try self.spc.cmpw(substate_offset);                                               }, // cmpw ya, dp
            0x5B => { try self.spc.alu_modify_d_x(substate_offset, SPC.AluModifyOp.lsr);                }, // lsr dp+x
            0x5C => { try self.spc.alu_modify_reg(substate_offset, A, SPC.AluModifyOp.lsr);             }, // lsr a
            0x5D => { try self.spc.mov_reg_reg(substate_offset, X, A);                                  }, // mov x, a
            0x5E => { try self.spc.alu_reg_with_abs(substate_offset, Y, SPC.AluOp.cmp);                 }, // cmp y, addr
            0x5F => { try self.spc.jmp_abs(substate_offset);                                            }, // jmp addr
            0x60 => { try self.spc.clrc(substate_offset);                                               }, // clrc
            0x61 => { try self.spc.tcall(substate_offset, 6);                                           }, // tcall 6
            0x62 => { try self.spc.set1(substate_offset, 3);                                            }, // set1 dp.3
            0x63 => { try self.spc.branch_bit(substate_offset, 3, 1);                                   }, // bbs dp.3, r
            0x64 => { try self.spc.alu_reg_with_d(substate_offset, A, SPC.AluOp.cmp);                   }, // cmp a, dp
            0x65 => { try self.spc.alu_reg_with_abs(substate_offset, A, SPC.AluOp.cmp);                 }, // cmp a, addr
            0x66 => { try self.spc.alu_reg_with_x_ind(substate_offset, A, SPC.AluOp.cmp);               }, // cmp a, (x)
            0x67 => { try self.spc.alu_reg_with_d_x_ind(substate_offset, A, SPC.AluOp.cmp);             }, // cmp a, [dp+x]
            0x68 => { try self.spc.alu_reg_with_imm(substate_offset, A, SPC.AluOp.cmp);                 }, // cmp a, #im
            0x69 => { try self.spc.cmp_d_with_d(substate_offset);                                       }, // cmp dp, dp
            0x6A => { try self.spc.alu_c_with_mem_1bit(substate_offset, SPC.AluOp.bitand, true);        }, // and1 c, /mem.b
            0x6B => { try self.spc.alu_modify_d(substate_offset, SPC.AluModifyOp.ror);                  }, // ror dp
            0x6C => { try self.spc.alu_modify_abs(substate_offset, SPC.AluModifyOp.ror);                }, // ror addr
            0x6D => { try self.spc.push_reg(substate_offset, Y);                                        }, // push y
            0x6E => { try self.spc.dbnz_d(substate_offset);                                             }, // dbnz dp, r
            0x6F => { try self.spc.ret(substate_offset);                                                }, // ret
            0x70 => { try self.spc.branch(substate_offset, self.spc.v() != 0);                          }, // bvs r
            0x71 => { try self.spc.tcall(substate_offset, 7);                                           }, // tcall 7
            0x72 => { try self.spc.clr1(substate_offset, 3);                                            }, // clr1 dp.3
            0x73 => { try self.spc.branch_bit(substate_offset, 3, 0);                                   }, // bbc dp.3, r
            0x74 => { try self.spc.alu_reg_with_d_reg(substate_offset, A, X, SPC.AluOp.cmp);            }, // cmp a, dp+x
            0x75 => { try self.spc.alu_reg_with_abs_reg(substate_offset, A, X, SPC.AluOp.cmp);          }, // cmp a, addr+x
            0x76 => { try self.spc.alu_reg_with_abs_reg(substate_offset, A, Y, SPC.AluOp.cmp);          }, // cmp a, addr+y
            0x77 => { try self.spc.alu_reg_with_d_ind_y(substate_offset, A, SPC.AluOp.cmp);             }, // cmp a, [dp]+y
            0x78 => { try self.spc.cmp_d_with_imm(substate_offset);                                     }, // cmp dp, #im
            0x79 => { try self.spc.cmp_x_ind_with_y_ind(substate_offset);                               }, // cmp (x), (y)
            0x7A => { try self.spc.addw(substate_offset);                                               }, // addw ya, dp
            0x7B => { try self.spc.alu_modify_d_x(substate_offset, SPC.AluModifyOp.ror);                }, // ror dp+x
            0x7C => { try self.spc.alu_modify_reg(substate_offset, A, SPC.AluModifyOp.ror);             }, // ror a
            0x7D => { try self.spc.mov_reg_reg(substate_offset, A, X);                                  }, // mov a, x
            0x7E => { try self.spc.alu_reg_with_d(substate_offset, Y, SPC.AluOp.cmp);                   }, // cmp y, dp
            0x7F => { try self.spc.reti(substate_offset);                                               }, // reti
            0x80 => { try self.spc.setc(substate_offset);                                               }, // setc
            0x81 => { try self.spc.tcall(substate_offset, 8);                                           }, // tcall 8
            0x82 => { try self.spc.set1(substate_offset, 4);                                            }, // set1 dp.4
            0x83 => { try self.spc.branch_bit(substate_offset, 4, 1);                                   }, // bbs dp.4, r
            0x84 => { try self.spc.alu_reg_with_d(substate_offset, A, SPC.AluOp.add);                   }, // adc a, dp
            0x85 => { try self.spc.alu_reg_with_abs(substate_offset, A, SPC.AluOp.add);                 }, // adc a, addr
            0x86 => { try self.spc.alu_reg_with_x_ind(substate_offset, A, SPC.AluOp.add);               }, // adc a, (x)
            0x87 => { try self.spc.alu_reg_with_d_x_ind(substate_offset, A, SPC.AluOp.add);             }, // adc a, [dp+x]
            0x88 => { try self.spc.alu_reg_with_imm(substate_offset, A, SPC.AluOp.add);                 }, // adc a, #im
            0x89 => { try self.spc.alu_d_with_d(substate_offset, SPC.AluOp.add);                        }, // adc dp, dp
            0x8A => { try self.spc.alu_c_with_mem_1bit(substate_offset, SPC.AluOp.bitxor, false);       }, // eor1 c, mem.b
            0x8B => { try self.spc.alu_modify_d(substate_offset, SPC.AluModifyOp.dec);                  }, // dec dp
            0x8C => { try self.spc.alu_modify_abs(substate_offset, SPC.AluModifyOp.dec);                }, // dec addr
            0x8D => { try self.spc.alu_reg_with_imm(substate_offset, Y, SPC.AluOp.mov);                 }, // mov y, #im
            0x8E => { try self.spc.pop_reg(substate_offset, PSW);                                       }, // pop psw
            0x8F => { try self.spc.alu_d_with_imm(substate_offset, SPC.AluOp.none);                     }, // mov dp, #im (Apparently mov *does* read dp first before writing, even though it doesn't need to)
            0x90 => { try self.spc.branch(substate_offset, self.spc.c() == 0);                          }, // bcc r
            0x91 => { try self.spc.tcall(substate_offset, 9);                                           }, // tcall 9
            0x92 => { try self.spc.clr1(substate_offset, 4);                                            }, // clr1 dp.4
            0x93 => { try self.spc.branch_bit(substate_offset, 4, 0);                                   }, // bbc dp.4, r
            0x94 => { try self.spc.alu_reg_with_d_reg(substate_offset, A, X, SPC.AluOp.add);            }, // adc a, dp+x
            0x95 => { try self.spc.alu_reg_with_abs_reg(substate_offset, A, X, SPC.AluOp.add);          }, // adc a, addr+x
            0x96 => { try self.spc.alu_reg_with_abs_reg(substate_offset, A, Y, SPC.AluOp.add);          }, // adc a, addr+y
            0x97 => { try self.spc.alu_reg_with_d_ind_y(substate_offset, A, SPC.AluOp.add);             }, // adc a, [dp]+y
            0x98 => { try self.spc.alu_d_with_imm(substate_offset, SPC.AluOp.add);                      }, // adc dp, #im
            0x99 => { try self.spc.alu_x_ind_with_y_ind(substate_offset, SPC.AluOp.add);                }, // adc (x), (y)
            0x9A => { try self.spc.subw(substate_offset);                                               }, // subw ya, dp
            0x9B => { try self.spc.alu_modify_d_x(substate_offset, SPC.AluModifyOp.dec);                }, // dec dp+x
            0x9C => { try self.spc.alu_modify_reg(substate_offset, A, SPC.AluModifyOp.dec);             }, // dec a
            0x9D => { try self.spc.mov_reg_reg(substate_offset, X, SP);                                 }, // mov x, sp
            0x9E => { try self.spc.div(substate_offset);                                                }, // div ya, x
            0x9F => { try self.spc.xcn(substate_offset);                                                }, // xcn a
            0xA0 => { try self.spc.ei(substate_offset);                                                 }, // ei
            0xA1 => { try self.spc.tcall(substate_offset, 10);                                          }, // tcall 10
            0xA2 => { try self.spc.set1(substate_offset, 5);                                            }, // set1 dp.5
            0xA3 => { try self.spc.branch_bit(substate_offset, 5, 1);                                   }, // bbs dp.5, r
            0xA4 => { try self.spc.alu_reg_with_d(substate_offset, A, SPC.AluOp.sub);                   }, // sbc a, dp
            0xA5 => { try self.spc.alu_reg_with_abs(substate_offset, A, SPC.AluOp.sub);                 }, // sbc a, addr
            0xA6 => { try self.spc.alu_reg_with_x_ind(substate_offset, A, SPC.AluOp.sub);               }, // sbc a, (x)
            0xA7 => { try self.spc.alu_reg_with_d_x_ind(substate_offset, A, SPC.AluOp.sub);             }, // sbc a, [dp+x]
            0xA8 => { try self.spc.alu_reg_with_imm(substate_offset, A, SPC.AluOp.sub);                 }, // sbc a, #im
            0xA9 => { try self.spc.alu_d_with_d(substate_offset, SPC.AluOp.sub);                        }, // sbc dp, dp
            0xAA => { try self.spc.alu_c_with_mem_1bit(substate_offset, SPC.AluOp.none, false);         }, // mov1 c, mem.b
            0xAB => { try self.spc.alu_modify_d(substate_offset, SPC.AluModifyOp.inc);                  }, // inc dp
            0xAC => { try self.spc.alu_modify_abs(substate_offset, SPC.AluModifyOp.inc);                }, // inc addr
            0xAD => { try self.spc.alu_reg_with_imm(substate_offset, Y, SPC.AluOp.cmp);                 }, // cmp y, #im
            0xAE => { try self.spc.pop_reg(substate_offset, A);                                         }, // pop a
            0xAF => { try self.spc.mov_x_ind_inc_from_a(substate_offset);                               }, // mov (x)+, a
            0xB0 => { try self.spc.branch(substate_offset, self.spc.c() != 0);                          }, // bcs r
            0xB1 => { try self.spc.tcall(substate_offset, 11);                                          }, // tcall 11
            0xB2 => { try self.spc.clr1(substate_offset, 5);                                            }, // clr1 dp.5
            0xB3 => { try self.spc.branch_bit(substate_offset, 5, 0);                                   }, // bbc dp.5, r
            0xB4 => { try self.spc.alu_reg_with_d_reg(substate_offset, A, X, SPC.AluOp.sub);            }, // sbc a, dp+x
            0xB5 => { try self.spc.alu_reg_with_abs_reg(substate_offset, A, X, SPC.AluOp.sub);          }, // sbc a, addr+x
            0xB6 => { try self.spc.alu_reg_with_abs_reg(substate_offset, A, Y, SPC.AluOp.sub);          }, // sbc a, addr+y
            0xB7 => { try self.spc.alu_reg_with_d_ind_y(substate_offset, A, SPC.AluOp.sub);             }, // sbc a, [dp]+y
            0xB8 => { try self.spc.alu_d_with_imm(substate_offset, SPC.AluOp.sub);                      }, // sbc dp, #im
            0xB9 => { try self.spc.alu_x_ind_with_y_ind(substate_offset, SPC.AluOp.sub);                }, // sbc (x), (y)
            0xBA => { try self.spc.movw_ya_d(substate_offset);                                          }, // movw ya, dp
            0xBB => { try self.spc.alu_modify_d_x(substate_offset, SPC.AluModifyOp.inc);                }, // inc dp+x
            0xBC => { try self.spc.alu_modify_reg(substate_offset, A, SPC.AluModifyOp.inc);             }, // inc a
            0xBD => { try self.spc.mov_reg_reg(substate_offset, SP, X);                                 }, // mov sp, x
            0xBE => { try self.spc.das(substate_offset);                                                }, // das a
            0xBF => { try self.spc.mov_a_from_x_ind_inc(substate_offset);                               }, // mov a, (x)+
            0xC0 => { try self.spc.di(substate_offset);                                                 }, // di
            0xC1 => { try self.spc.tcall(substate_offset, 12);                                          }, // tcall 12
            0xC2 => { try self.spc.set1(substate_offset, 6);                                            }, // set1 dp.6
            0xC3 => { try self.spc.branch_bit(substate_offset, 6, 1);                                   }, // bbs dp.6, r
            0xC4 => { try self.spc.mov_d_from_reg(substate_offset, A);                                  }, // mov dp, a
            0xC5 => { try self.spc.mov_abs_from_reg(substate_offset, A);                                }, // mov addr, a
            0xC6 => { try self.spc.mov_x_ind_from_a(substate_offset);                                   }, // mov (x), a
            0xC7 => { try self.spc.mov_d_x_ind_from_a(substate_offset);                                 }, // mov [dp+x], a
            0xC8 => { try self.spc.alu_reg_with_imm(substate_offset, X, SPC.AluOp.cmp);                 }, // cmp x, #im
            0xC9 => { try self.spc.mov_abs_from_reg(substate_offset, X);                                }, // mov addr, x
            0xCA => { try self.spc.mov1_mem_bit_with_c(substate_offset);                                }, // mov1 mem.b, c
            0xCB => { try self.spc.mov_d_from_reg(substate_offset, Y);                                  }, // mov dp, y
            0xCC => { try self.spc.mov_abs_from_reg(substate_offset, Y);                                }, // mov addr, y
            0xCD => { try self.spc.alu_reg_with_imm(substate_offset, X, SPC.AluOp.mov);                 }, // mov x, #im
            0xCE => { try self.spc.pop_reg(substate_offset, X);                                         }, // pop x
            0xCF => { try self.spc.mul(substate_offset);                                                }, // mul ya
            0xD0 => { try self.spc.branch(substate_offset, self.spc.z() == 0);                          }, // bne r
            0xD1 => { try self.spc.tcall(substate_offset, 13);                                          }, // tcall 13
            0xD2 => { try self.spc.clr1(substate_offset, 6);                                            }, // clr1 dp.6
            0xD3 => { try self.spc.branch_bit(substate_offset, 6, 0);                                   }, // bbc dp.6, r
            0xD4 => { try self.spc.mov_d_reg_from_reg(substate_offset, X, A);                           }, // mov dp+x, a
            0xD5 => { try self.spc.mov_abs_reg_from_a(substate_offset, X);                              }, // mov addr+x, a
            0xD6 => { try self.spc.mov_abs_reg_from_a(substate_offset, Y);                              }, // mov addr+y, a
            0xD7 => { try self.spc.mov_d_ind_y_from_a(substate_offset);                                 }, // mov [dp]+y, a
            0xD8 => { try self.spc.mov_d_from_reg(substate_offset, X);                                  }, // mov dp, x
            0xD9 => { try self.spc.mov_d_reg_from_reg(substate_offset, Y, X);                           }, // mov dp+y, x
            0xDA => { try self.spc.movw_d_ya(substate_offset);                                          }, // movw dp, ya
            0xDB => { try self.spc.mov_d_reg_from_reg(substate_offset, X, Y);                           }, // mov dp+x, y
            0xDC => { try self.spc.alu_modify_reg(substate_offset, Y, SPC.AluModifyOp.dec);             }, // dec y
            0xDD => { try self.spc.mov_reg_reg(substate_offset, A, Y);                                  }, // mov a, y
            0xDE => { try self.spc.cbne_d_x(substate_offset);                                           }, // cbne dp+x, r
            0xDF => { try self.spc.daa(substate_offset);                                                }, // daa a
            0xE0 => { try self.spc.clrv(substate_offset);                                               }, // clrv
            0xE1 => { try self.spc.tcall(substate_offset, 14);                                          }, // tcall 14
            0xE2 => { try self.spc.set1(substate_offset, 7);                                            }, // set1 dp.7
            0xE3 => { try self.spc.branch_bit(substate_offset, 7, 1);                                   }, // bbs dp.7, r
            0xE4 => { try self.spc.alu_reg_with_d(substate_offset, A, SPC.AluOp.mov);                   }, // mov a, dp
            0xE5 => { try self.spc.alu_reg_with_abs(substate_offset, A, SPC.AluOp.mov);                 }, // mov a, addr
            0xE6 => { try self.spc.alu_reg_with_x_ind(substate_offset, A, SPC.AluOp.mov);               }, // mov a, (x)
            0xE7 => { try self.spc.alu_reg_with_d_x_ind(substate_offset, A, SPC.AluOp.mov);             }, // mov a, [dp+x]
            0xE8 => { try self.spc.alu_reg_with_imm(substate_offset, A, SPC.AluOp.mov);                 }, // mov a, #im
            0xE9 => { try self.spc.alu_reg_with_abs(substate_offset, X, SPC.AluOp.mov);                 }, // mov x, addr
            0xEA => { try self.spc.not1_mem_bit(substate_offset);                                       }, // not1 mem.b
            0xEB => { try self.spc.alu_reg_with_d(substate_offset, Y, SPC.AluOp.mov);                   }, // mov y, dp
            0xEC => { try self.spc.alu_reg_with_abs(substate_offset, Y, SPC.AluOp.mov);                 }, // mov y, addr
            0xED => { try self.spc.notc(substate_offset);                                               }, // notc
            0xEE => { try self.spc.pop_reg(substate_offset, Y);                                         }, // pop y
            0xEF => { try self.spc.sleep(substate_offset);                                              }, // sleep
            0xF0 => { try self.spc.branch(substate_offset, self.spc.z() != 0);                          }, // beq r
            0xF1 => { try self.spc.tcall(substate_offset, 15);                                          }, // tcall 15
            0xF2 => { try self.spc.clr1(substate_offset, 7);                                            }, // clr1 dp.7
            0xF3 => { try self.spc.branch_bit(substate_offset, 7, 0);                                   }, // bbc dp.7, r
            0xF4 => { try self.spc.alu_reg_with_d_reg(substate_offset, A, X, SPC.AluOp.mov);            }, // mov a, dp+x
            0xF5 => { try self.spc.alu_reg_with_abs_reg(substate_offset, A, X, SPC.AluOp.mov);          }, // mov a, addr+x
            0xF6 => { try self.spc.alu_reg_with_abs_reg(substate_offset, A, Y, SPC.AluOp.mov);          }, // mov a, addr+y
            0xF7 => { try self.spc.alu_reg_with_d_ind_y(substate_offset, A, SPC.AluOp.mov);             }, // mov a, [dp]+y
            0xF8 => { try self.spc.alu_reg_with_d(substate_offset, X, SPC.AluOp.mov);                   }, // mov x, dp
            0xF9 => { try self.spc.alu_reg_with_d_reg(substate_offset, X, Y, SPC.AluOp.mov);            }, // mov x, dp+y
            0xFA => { try self.spc.mov_d_from_d(substate_offset);                                       }, // mov dp, dp
            0xFB => { try self.spc.alu_reg_with_d_reg(substate_offset, Y, X, SPC.AluOp.mov);            }, // mov y, dp+x
            0xFC => { try self.spc.alu_modify_reg(substate_offset, Y, SPC.AluModifyOp.inc);             }, // inc y
            0xFD => { try self.spc.mov_reg_reg(substate_offset, Y, A);                                  }, // mov y, a
            0xFE => { try self.spc.dbnz_y(substate_offset);                                             }, // dbnz y, r
            0xFF => { try self.spc.stop(substate_offset);                                               }, // stop
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
                _ = try self.read(0x100 | self.spc.sp(), substate_offset);
                unreachable;
            },
            1 => {
                _ = try self.read(0x100 | self.spc.sp(), substate_offset);
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

                if (self.enable_access_logs or self.emu.script700.enabled) {
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
        else if (self.cur_debug_mode != Emu.DebugMode.none and self.in_shadow_region(address, 0)) {
            const result = self.spc.shadow_mem[address];
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
        else if (self.cur_debug_mode != Emu.DebugMode.none and self.in_shadow_region(address, 0)) {
            const result = self.spc.shadow_mem[address];
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

                if (self.enable_access_logs or self.emu.script700.enabled) {
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
                    // Zero out on Script700 side as well
                    self.emu.script700.state.port_in[0] = 0x00;
                    self.emu.script700.state.port_in[1] = 0x00;
                }

                if (data >> 5 & 1 == 1) {
                    self.state.input_ports[2] = 0x00;
                    self.state.input_ports[3] = 0x00;
                    // Zero out on Script700 side as well
                    self.emu.script700.state.port_in[2] = 0x00;
                    self.emu.script700.state.port_in[3] = 0x00;
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

    pub fn debug_write_io(self: *SSMP, address: u16, data: u8) void {
        switch (address) {
            0x00F0 => { // TEST (Normal behavior, but needs to work regardless of P state)
                const p_ = self.spc.p();
                self.spc.state.set_p(0);
                self.write_io(address, data);
                self.spc.state.set_p(p_);
            },
            0x00F1...0x00F3 => { // Normal behavior
                self.write_io(address, data);
            },
            0x00F4 => { // CPUIO0 (Overwrite the input data (from S-CPU) as opposed to output)
                self.state.input_ports[0] = data;
                self.emu.script700.state.port_in[0] = data; // Reflect on Script700 side
            },
            0x00F5 => { // CPUIO1 (Overwrite the input data (from S-CPU) as opposed to output)
                self.state.input_ports[1] = data;
                self.emu.script700.state.port_in[1] = data; // Reflect on Script700 side
            },
            0x00F6 => { // CPUIO2 (Overwrite the input data (from S-CPU) as opposed to output)
                self.state.input_ports[2] = data;
                self.emu.script700.state.port_in[2] = data; // Reflect on Script700 side
            },
            0x00F7 => { // CPUIO3 (Overwrite the input data (from S-CPU) as opposed to output)
                self.state.input_ports[3] = data;
                self.emu.script700.state.port_in[3] = data; // Reflect on Script700 side
            },
            0x00F8...0x00FC => { // Normal behavior
                self.write_io(address, data);
            },
            0x00FD => { // T0OUT
                self.state.timer_outputs[0] = @intCast(data & 0x0F);
            },
            0x00FE => { // T1OUT
                self.state.timer_outputs[1] = @intCast(data & 0x0F);
            },
            0x00FF => { // T2OUT
                self.state.timer_outputs[2] = @intCast(data & 0x0F);
            },
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