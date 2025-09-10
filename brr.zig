const DSPStateInternal = @import("dsp_state_int.zig").DSPStateInternal;

pub inline fn decode(s: *DSPStateInternal, v_idx: u3, aram_data_0: u8) void {
    const v = &s._voice[v_idx];

    const b0_b1 = s._brr._cur_block_byte; // Cached from previous clock cycle
    const b2_b3 = aram_data_0;

    const nybbles: [4]u4 = [_]u4 {
        @intCast(b0_b1 >>  4),
        @intCast(b0_b1 & 0xF),
        @intCast(b2_b3 >>  4),
        @intCast(b2_b3 & 0xF)
    };

    const filter: u2 = @intCast(s._brr._cur_block_header >> 2 & 0b11);
    const shift:  u4 = @intCast(s._brr._cur_block_header >> 4 & 0b1111);

    // Decode 4 samples
    for (nybbles) |nybble| {
        const n:   i4  = @bitCast(nybble);
        var   smp: i32 = @intCast(n);

        if (shift <= 12) {
            smp <<= shift;
            smp >>= 1;
        }
        else { // From Fullsnes: "When shift=13..15, decoding works as if shift=12 and nibble=(nibble SAR 3)."
            smp >>= 3;
            smp <<= 12;
            smp >>= 1;
        }

        // Apply filter: Grab the 2 most recent decoded samples from buffer
        const offset = v._buffer_offset;
        const p1 = v._buffer[@mod(offset - 1, 12)] >> 1;
        const p2 = v._buffer[@mod(offset - 2, 12)] >> 1;

        switch (filter) {
            0 => {
                // Do nothing. Filter 0 is just shifted 4-bit PCM
            },
            1 => {
                // Filter 1: new = sample + old*0.9375
                smp += p1 + (-p1 >> 4);
            },
            2 => {
                // Filter 2: new = sample + old*1.90625  - older*0.9375
                smp += p1 * 2 + (-p1 * 3 >> 5) - p2 + (p2 >> 4);
            },
            3 => {
                // Filter 3: new = sample + old*1.796875 - older*0.8125
                smp += p1 * 2 + (-p1 * 13 >> 6) - p2 + (p2 * 3 >> 4);
            }
        }

        // Resulting sample is first clamped to signed 16-bit, then clipped to signed 15-bit
        const clamped: i16 =
            @intCast(
                if (smp > 0x7FFF)        0x7FFF
                else if (smp < -0x8000) -0x8000
                else                       smp
            );
        
        const clipped_1: u16 = @bitCast(clamped);
        const clipped_2: u15 = @intCast(clipped_1 & 0x7FFF);
        const clipped_3: i15 = @bitCast(clipped_2);

        v._buffer[v._buffer_offset] = @as(i16, clipped_3) << 1;

        v._buffer_offset += 1;
        v._buffer_offset %= 12;
    }
}