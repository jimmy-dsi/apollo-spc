const DSPStateInternal = @import("dsp_state_int.zig").DSPStateInternal;

// Table of counter rates - Used for ADSR, GAIN, and Noise frequencies
const counter_rate: [32]u16 = [_]u16 {
       0, 2048, 1536, 1280,
    1024,  768,  640,  512,
     384,  320,  256,  192,
     160,  128,   96,   80,
      64,   48,   40,   32,
      24,   20,   16,   12,
      10,    8,    6,    5,
       4,    3,    2,    1,
};

// For some reason, not every rate completes when the counter reaches zero. Some are offset.
// Seems to be only at powers of 2 where it's centered at zero. Must be a weird hardware thing
const counter_offset: [32]u16 = [_]u16 {
       0,    0, 1040,  536,
       0, 1040,  536,    0,
    1040,  536,    0, 1040,
     536,    0, 1040,  536,
       0, 1040,  536,    0,
    1040,  536,    0, 1040,
     536,    0, 1040,  536,
       0, 1040,    0,    0,
};

pub inline fn tick(s: *DSPStateInternal) void {
    if (s._counter == 0) {
        s._counter = 2048 * 5 * 3; // 30720 (0x7800) ...I see. This is the LCM of all the counter rates
    }
    s._counter -= 1;
}

pub inline fn poll(s: *const DSPStateInternal, rate: u5) bool {
    if (rate == 0) {
        return false;
    }
    else {
        return (@as(u16, s._counter) + counter_offset[rate]) % counter_rate[rate] == 0;
    }
}