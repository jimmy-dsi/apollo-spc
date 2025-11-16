const std = @import("std");

const Emu = @import("emu.zig").Emu;

pub const SMPState = struct {
    pub const Timer = struct {
        stage_0: u8,
        stage_1: u1,
        stage_1_prev: u1,
        stage_2: u8,

        pub fn new() Timer {
            return Timer {
                .stage_0 = 0x00,
                .stage_1 = 0,
                .stage_1_prev = 0,
                .stage_2 = 0x00
            };
        }

        pub inline fn reset(self: *Timer, state: *SMPState, comptime timer_index: u32) void {
            // Don't reset stage 0 or 1. Keep internal clock synced with current SMP wait time.
            self.stage_2 = 0;
            state.timer_outputs[timer_index] = 0x0;
        }

        pub inline fn step(self: *Timer, state: *SMPState, cycles: u32, comptime timer_index: u32, comptime period: u32) void {
            self.stage_0 += @intCast(cycles);

            if (self.stage_0 >= period) {
                self.stage_0 -= period;
                self.stage_1 ^= 1;

                self.step_stage_1(state, timer_index);
            }
        }

        pub fn step_stage_1(self: *Timer, state: *SMPState, comptime timer_index: u32) void {
            const stage_1_next =
                if (state.global_timer_enable == 1 and state.global_timer_disable == 0)
                    self.stage_1
                else
                    0;

            // Only pulse on 1->0 transition, and if this particular timer is enabled
            if (self.stage_1_prev == 1 and stage_1_next == 0 and state.timer_on_flags[timer_index] == 1) {
                self.step_stage_2(state, timer_index);
            }

            self.stage_1_prev = stage_1_next;
        }

        inline fn step_stage_2(self: *Timer, state: *SMPState, comptime timer_index: u32) void {
            const target = state.timer_dividers[timer_index];
            self.stage_2 +%= 1;

            if (self.stage_2 == target) {
                self.stage_2 = 0;
                state.timer_outputs[timer_index] +%= 1;
            }
        }
    };

    timer_states: [3]Timer = [3]Timer {
        Timer.new(), Timer.new(), Timer.new()
    },

    // $00F0 MMIO
    global_timer_disable: u1 = 0,
    ram_write_enable:     u1 = 1,
    ram_disable:          u1 = 0,
    global_timer_enable:  u1 = 1,
    ram_waitstates:       u2 = 0b00,
    io_waitstates:        u2 = 0b00,

    // $00F1 MMIO
    timer_on_flags: [3]u1 = [3]u1 { 0, 0, 0 },
    use_boot_rom:   u1 = 1,

    // $00F2 MMIO
    dsp_address: u8 = 0x00,

    // $00F4-$00F7 MMIO
    input_ports:  [4]u8 = [4]u8 { 0x00, 0x00, 0x00, 0x00 }, // From S-CPU to S-SMP
    output_ports: [4]u8 = [4]u8 { 0x00, 0x00, 0x00, 0x00 }, // From S-SMP to S-CPU

    // $00F8-$00F9 MMIO
    aux: [2]u8 = [2]u8 { 0x00, 0x00 },

    // $00FA-$00FC MMIO
    timer_dividers: [3]u8 = [3]u8 { 0x00, 0x00, 0x00 },

    // $00FD-$00FF MMIO
    timer_outputs: [3]u4 = [3]u4 { 0xF, 0xF, 0xF },
};