const CoState = @import("co_state.zig").CoState;

const Co = CoState.Co;

pub const CoManager = struct {
    co_state: CoState,

    pub fn new() CoManager {
        return CoManager {
            .co_state = CoState.new()
        };
    }

    pub fn reset(self: *CoManager) void {
        self.co_state.reset();
    }

    pub inline fn substate(self: *CoManager) u32 {
        return self.co_state.cur_substate;
    }

    pub inline fn waiting(self: *CoManager) bool {
        return self.co_state.wait_cycles > 0 or self.co_state.finish_delay > 0;
    }

    pub inline fn step(self: *CoManager) void {
        self.co_state.step();
    }

    pub inline fn null_transition(self: *CoManager) bool {
        const result = self.co_state.null_transition;
        self.co_state.null_transition = false;
        return result;
    }

    pub fn wait(self: *CoManager, cycles: u32) Co! void {
        self.co_state.wait_cycles = cycles;
        if (cycles > 0) {
            return Co.Yield;
        }
        else {
            // Advance 1 state immediately if wait time is zero
            self.co_state.null_transition = true;
            self.co_state.cur_substate += 1;
            return Co.Yield;
        }
    }

    pub inline fn finish(self: *CoManager, delay_amt: u32) void {
        self.co_state.finish(delay_amt);
    }
};