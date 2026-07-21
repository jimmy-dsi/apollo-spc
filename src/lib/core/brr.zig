const DSPStateInternal = @import("dsp_state_int.zig").DSPStateInternal;
const SDSP = @import("s_dsp.zig").SDSP;

pub const Error = error { RangeError };

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
        const n: i4 = @bitCast(nybble);

        // Grab the 2 most recent decoded samples from buffer to be used by filter
        const offset: i5 = @intCast(v._buffer_offset);
        const p1 = v._buffer[@intCast(@mod(offset - 1, 12))];
        const p2 = v._buffer[@intCast(@mod(offset - 2, 12))];

        const dec = decode_nybble(filter, shift, n, p1, p2);

        v._buffer[v._buffer_offset] = dec;

        v._buffer_offset += 1;
        v._buffer_offset %= 12;
    }
}

pub inline fn decode_from_address(dsp: *const SDSP, addr: u16, result: []i16, old_: i16, older_: i16) !i32 {
    return decode_from_buffer(&dsp.audio_ram, addr, result, old_, older_);
}

pub inline fn decode_from_buffer(input: []const u8, offset: u16, result: []i16, old_: i16, older_: i16) !i32 {
    const addr = offset;

    var old:   i16 = old_;
    var older: i16 = older_;

    var idx:     u16 = 0;
    var counter: u32 = 0;

    var header: u8 = input[addr];
    var end_block = false;
    var looped    = false;

    while (!end_block) {
        // Failsafe: prevent from writing beyond the end of the provided result buffer
        if (counter >= result.len - 1) {
            break;
        }

        if (header & 1 != 0) {
            end_block = true;

            if (header & 2 != 0) {
                looped = true;
            }
        }

        const filter: u2 = @intCast(header >> 2 & 0b11);
        const shift:  u4 = @intCast(header >> 4 & 0b1111);

        var nybbles: [16]u4 = .{0} ** 16;

        inline for (0..8) |i| {
            const ii: u16 = @intCast(i);

            nybbles[i * 2]     = @intCast(input[addr +% idx +% 1 +% ii] >>  4);
            nybbles[i * 2 + 1] = @intCast(input[addr +% idx +% 1 +% ii] & 0xF);
        }

        // Decode block
        inline for (nybbles, 0..) |nybble, i| {
            const ii: u32 = @intCast(i);

            const n: i4 = @bitCast(nybble);
            const smp = decode_nybble(filter, shift, n, old, older);

            if (counter + ii >= result.len) {
                return Error.RangeError;
            }

            result[counter + ii] = smp;

            older = old;
            old   = smp;
        }

        idx += 9;
        counter += 16;

        header = input[addr +% idx];
    }

    const r: i32 = @intCast(counter);

    return if (looped) -r else r;
}

inline fn decode_nybble(filter: u2, shift: u4, n: i4, old: i16, older: i16) i16 {
    var smp: i32 = @intCast(n);

    if (shift <= 12) {
        smp <<= shift;
        smp >>= 1;
    }
    else { // From Fullsnes: "When shift=13..15, decoding works as if shift=12 and nibble=(nibble SAR 3)."
        smp >>= 3;
        smp <<= 12;
        smp >>= 1;
    }

    // Apply filter: Grab the 2 most recent decoded samples
    const p1 = @as(i32, old   >> 1);
    const p2 = @as(i32, older >> 1);

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

    return @as(i16, clipped_3) << 1;
}