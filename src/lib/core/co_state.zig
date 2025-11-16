const std = @import("std");

pub const CoState = struct {
    pub const Co = error { Yield };

    wait_cycles:     u32  = 0,
    cur_substate:    u32  = 0,
    finish_delay:    u32  = 0,
    null_transition: bool = false,

    pub fn new() CoState {
        return CoState { };
    }

    pub fn reset(self: *CoState) void {
        self.wait_cycles     = 0;
        self.cur_substate    = 0;
        self.finish_delay    = 0;
        self.null_transition = false;
    }

    pub inline fn step(self: *CoState) void {
        if (self.wait_cycles > 0) {
            self.wait_cycles -= 1;
        }
        if (self.wait_cycles == 0 and self.finish_delay == 0) {
            self.cur_substate += 1;
        }
        if (self.finish_delay > 0) {
            self.finish_delay -= 1;
        }
    }

    pub inline fn finish(self: *CoState, delay_amt: u32) void {
        self.wait_cycles     = 0;
        self.cur_substate    = 0;
        self.finish_delay    = delay_amt;
        self.null_transition = delay_amt == 0;
        //std.debug.print("{d} {d}\n", .{@intFromBool(self.null_transition), self.cur_substate});
    }
};