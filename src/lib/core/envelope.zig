const DSPStateInternal = @import("dsp_state_int.zig").DSPStateInternal;
const counter = @import("counter.zig");

pub fn run(s: *DSPStateInternal, v_idx: u3, adsr_1: u8, gain: u8) void {
    const v = &s._voice[v_idx];

    var next_env_level: i16 = undefined;
    var rate:            u5 = undefined;
    var env_data:        u8 = undefined;

    if (v._env_mode == .key_off) {
        // Used during key-off. Perform linear volume down ramp until silent. Max time: 256 samples (8ms)
        v._env_level -|= 0x8;
        return;
    }

    if (s._adsr_0 & 0x80 != 0) { // ADSR Mode: Bit 7 of ADSR 0 register set
        env_data = adsr_1;

        switch (v._env_mode) {
            .key_off => unreachable,

            .attack => {
                const attack: u4 = @intCast(s._adsr_0 & 0xF);

                rate = @as(u5, attack) * 2 + 1; // $0 == rate of 1. $F == rate of 31
                next_env_level = @as(i16, v._env_level) + (
                    if (rate < 31)
                        @as(i16, 0x20) // Wtf Zig... stop overfitting your inferred types as comptime_int and then throwing errors about it >_>
                    else
                        @as(i16, 0x400) // Fastest attack setting ($F) seems to increase env level faster than normal
                );
            },
            .decay => {
                const decay:   u3 = @intCast(s._adsr_0 >> 4 & 0x7);
                const sustain: u3 = @intCast(env_data >> 5); // $0 == softest, $7 == loudest

                rate = @as(u5, decay) * 2 + 16; // $0 == slowest (rate of 16), $7 = fastest (rate of 30)
                next_env_level  = @as(i16, v._env_level) - 1;
                next_env_level -= next_env_level >> 8;

                // When top 3 bits of envelope level fall below (sustain + 1), change mode to release
                // Note: This means that the final envelope level when reaching sustain should be roughly sustain + 1, since it only checks if it goes below that threshold,
                //       not if the bottom 8 bits reach zero as well. This means that a sustain value of $0 could hit the threshold as high as %000.11111111
                //       In practice, it would probably stop at %001.00000000 (or maybe even a bit higher?) due to the fact that next_env_level is only committed once the timer hits target
                if (next_env_level >> 8 == sustain) {
                    v._env_mode = .release;
                }
            },
            .release => {
                const release: u5 = @intCast(env_data & 0x1F);

                rate = release; // $00 == infinite, $1F == quick release
                next_env_level  = @as(i16, v._env_level) - 1;
                next_env_level -= next_env_level >> 8;
            }
        }
    }
    else { // GAIN mode
        env_data = gain;
        const mode: u3 = @intCast(env_data >> 5); // Top 3 bits correspond to GAIN mode

        sw: switch (mode) {
            0...3 => { // Direct GAIN
                next_env_level = @as(i16, env_data & 0x7F) << 4;
                rate = 31; // Set rate to the fastest so that the update to the target Direct GAIN value can occur as soon as possible
            },
            4 => { // Linear decrease
                next_env_level = @as(i16, v._env_level) - 0x20;
                rate = @intCast(env_data & 0x1F);
            },
            5 => { // Exponential decrease
                next_env_level  = @as(i16, v._env_level) - 1;
                next_env_level -= next_env_level >> 8;
                rate = @intCast(env_data & 0x1F);
            },
            6 => { // Linear increase
                next_env_level = @as(i16, v._env_level) + 0x20;
                rate = @intCast(env_data & 0x1F);
            },
            7 => { // Bent line increase
                if (v.__env_level >= 0x600) {
                    next_env_level = @as(i16, v._env_level) + 0x8;
                    rate = @intCast(env_data & 0x1F);
                }
                else {
                    continue :sw 6;
                }
            }
        }
    }

    v.__env_level = next_env_level;

    // Trigger attack->decay transition on envelope overflow/underflow
    if (next_env_level < 0 or next_env_level > 0x7FF) {
        // Clamp between 0..=0x7FF
        next_env_level =
            if (next_env_level < 0)
                0
            else
                0x7FF;
    
        if (v._env_mode == .attack) {
            v._env_mode = .decay;
        }
    }

    if (counter.poll(s, rate)) {
        // Flush the queued up value for next envelope level if the tick of the current counter rate has completed
        v._env_level = @intCast(next_env_level);
    }
}