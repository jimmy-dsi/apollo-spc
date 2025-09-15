const gauss    = @import("gauss.zig");
const envelope = @import("envelope.zig");
const brr      = @import("brr.zig");

pub const DSPStateInternal = struct {
    pub const EnvMode = enum {
        key_off, attack, decay, release
    };

    pub const BRR = struct {
        _bank: u8 = 0x00,

        _cur_source:   u8  = 0x00,
        _cur_address:  u16 = 0x0000,
        _next_address: u16 = 0x0000,

        _cur_block_header: u8 = 0x00,
        _cur_block_byte:   u8 = 0x00,
    };

    pub const Voice = struct {
        _buffer:          [12] i16 = [_]i16 {0} ** 12, // BRR sample decode buffer (holds 12 samples: 12 sample delay between decoding and gaussian interpolation + output)
        _buffer_offset:   u4       = 0,                // Location in buffer where next BRR samples will be decoded
        _gaussian_offset: u16      = 0x0000,           // Relative fractional position in sample (0x1000 = 1.0)
        _brr_address:     u16      = 0x0000,           // Address of current BRR block
        _brr_offset:      u4       = 1,                // Current decoding offset in BRR block (1-8)
        _key_on_delay:    u3       = 0,                // KON delay/current setup phase
        _env_mode:        EnvMode  = .key_off,
        _env_level:       u11      = 0,                // Current envelope level (0-2047)

        // Internal latches
        __env_level:    i16 = 0, // Used by GAIN mode 7, very obscure quirk
        __key_latch:    u1  = 0,
        __key_on:       u1  = 0,
        __key_off:      u1  = 0,
        __pitch_mod_on: u1  = 0,
        __noise_on:     u1  = 0,
        __echo_on:      u1  = 0,
        __end:          u1  = 0,
        __looped:       u1  = 0,
    };

    pub const Echo = struct {
        // Used for FIR calculations
        _history_left:  [8]i16 = [_]i16 {0} ** 8,
        _history_right: [8]i16 = [_]i16 {0} ** 8,

        _input_left:  i17 = 0,
        _input_right: i17 = 0,

        _esa_page:       u8  = 0x00,
        _readonly:       u1  = 0,
        _address:        u16 = 0,
        _offset:         u16 = 0, // Current offset from ESA into echo buffer
        _length:         u16 = 0, // Length in bytes of echo buffer
        _history_offset: u3  = 0
    };

    // Output
    _main_out_left:  i17 = 0,
    _main_out_right: i17 = 0,
    _echo_out_left:  i17 = 0,
    _echo_out_right: i17 = 0,

    // Misc. Internal state
    _brr:        BRR  = .{},
    _echo:       Echo = .{},
    _noise_lfsr: u15  = 0x4000,
    _sample_clk: u1   = 1,
    _counter:    u15  = 0x0000,

    _voice: [8]Voice = [_]Voice {.{}} ** 8,

    // Latch state
    _adsr_0: u8  = 0x00,
    _envx:   u8  = 0x00,
    _outx:   i8  = 0x00,
    _pitch:  u15 = 0x0000,
    _output: i16 = 0x0000,

    inline fn voice_output(self: *DSPStateInternal, v_idx: u3, comptime channel: u1, vol: i8) void {
        const v = &self._voice[v_idx];
        
        const main_out: *i17 = 
            if (channel == 0) &self._main_out_left
            else              &self._main_out_right;

        const echo_out: *i17 =
            if (channel == 0) &self._echo_out_left
            else              &self._echo_out_right;

        // Apply left/right volume
        const amp: i17 = @intCast(@as(i24, self._output) * @as(i24, vol) >> 7);

        // Add to output total
        main_out.* += amp;
        // Clamp to i16
        main_out.* =
            if      (main_out.* >  0x7FFF)    0x7FFF
            else if (main_out.* < -0x8000)   -0x8000
            else                            main_out.*;
        
        // Add to echo total if echo is enabled for current voice
        if (v.__echo_on == 1) {
            echo_out.* +%= amp;
            // Clamp to i16
            echo_out.* =
                if      (echo_out.* >  0x7FFF)    0x7FFF
                else if (echo_out.* < -0x8000)   -0x8000
                else                            echo_out.*;
        }
    }

    pub fn voice_step_a(self: *DSPStateInternal, source: u8) void {
        self._brr._cur_address = (@as(u16, self._brr._bank) << 8) +% (@as(u16, self._brr._cur_source) << 2);
        self._brr._cur_source = source;
    }

    pub fn voice_step_b(self: *DSPStateInternal, v_idx: u3, aram_0: [*]u8, aram_1: [*]u8, pitch_lo: u8, adsr_0: u8) void {
        // Read sample pointer (ignored if not needed)
        var address: u16 = self._brr._cur_address;
        if (self._voice[v_idx]._key_on_delay == 0) {
            address +%= 2;
        }

        // Do this to prevent buffer overflow
        const hi_address: usize = @intCast(address +% 1);
        
        self._brr._next_address =
              @as(u16, (aram_0 + address)[0])
            | @as(u16, (if (hi_address > 0) aram_1 + hi_address - 1 else aram_1 - 1)[0]) << 8;

        self._adsr_0 = adsr_0;

        // Read pitch, spread over two clocks
        self._pitch = @intCast(pitch_lo);
    }

    pub fn voice_step_c(self: *DSPStateInternal,
                        v_idx: u3,
                        aram_data_0: u8, aram_data_1: u8,
                        envx: *u8, pitch_hi: u8, adsr_1: u8, gain: u8,
                        flg_reset: u1, gauss_tbl: [*]const u16) void
    {
        self.voice_step_c_pt1(pitch_hi);
        self.voice_step_c_pt2(aram_data_0, aram_data_1);
        self.voice_step_c_pt3(v_idx, adsr_1, gain, envx, flg_reset, gauss_tbl);
    }

    pub fn voice_step_c_pt1(self: *DSPStateInternal, pitch_hi: u8) void {
        self._pitch |= @as(u15, pitch_hi) << 8;
    }

    pub fn voice_step_c_pt2(self: *DSPStateInternal, aram_data_0: u8, aram_data_1: u8) void {
        self._brr._cur_block_byte   = aram_data_0;
        self._brr._cur_block_header = aram_data_1;
    }

    pub fn voice_step_c_pt3(self: *DSPStateInternal, v_idx: u3, adsr_1: u8, gain: u8, envx: *u8, flg_reset: u1, gauss_tbl: [*]const u16) void {
        const v = &self._voice[v_idx];

        // Pitch modulation using previous voice's output (Looks like there's a single output state variable with a value that's simply carried over from previous channel processed)
        // End result is that previous channel output => input for next channel pmod
        if (v.__pitch_mod_on == 1) {
            // Pitch is adjusted by modulation amount
            // Seems that instead of being a simple addition with the output, it also factors in the current pitch as a multiplier as well
            // The math appears to work out so that the perceived change in frequency from the previous output is the same regardless of the current pitch
            // (So for example, the maximum positive output seems to always offset the pitch 1 octave higher, regardless of register)
            const p: i16 = @intCast(self._pitch);
            const o: i16 = @intCast(self._output >> 5);
            self._pitch += @intCast(o * p >> 10);
        }

        if (v._key_on_delay > 0) {
            // Get ready to start BRR decoding on next sample
            if (v._key_on_delay == 5) {
                v._brr_address   = self._brr._next_address;
                v._brr_offset    = 1;
                v._buffer_offset = 0;

                self._brr._cur_block_header = 0; // I guess the first BRR block of a sample when keyed on is forced to header value 00 (Is that why most encoders zero out the first block?)
            }

            // Envelope is never run during KON
            v._env_level  = 0;
            v.__env_level = 0;

            // Disable BRR decoding until the last 3 samples
            if (v._key_on_delay == 4 or v._key_on_delay == 2) {
                // Begin gaussian offset 4 samples after BRR decoding position in ring buffer
                v._gaussian_offset = 0x4000;
            }
            else {
                v._gaussian_offset = 0;
            }

            // Internal pitch latch is reset to zero during KON and does not advance gaussian offset
            self._pitch = 0;

            v._key_on_delay -= 1;
        }

        const output: i16 =
            if (v.__noise_on == 0)
                gauss.interpolate(self, v_idx, gauss_tbl) // Do gaussian interpolation
            else
                @bitCast(@as(u16, self._noise_lfsr) << 1); // Output is set to noise LFSR output instead, if noise is enabled for this voice

        // Apply envelope
        self._output = @intCast(@as(i32, output) * @as(i32, v._env_level) >> 11);
        envx.* = @intCast(v._env_level >> 4); // Set ENVX to top 7 bits of envelope level

        // Immediately silence the voice if reset FLG bit has been set, or if we've reached the non-looped end block of a BRR sample
        // It appears that even though __end is set unconditionally whether it's looping or non-looping, this is still determined
        // by examining the current header bits directly.
        if (flg_reset == 1 or self._brr._cur_block_header & 0b11 == 1) {
            v._env_mode  = .key_off;
            v._env_level = 0;
        }

        // Process KON and KOFF once every 2nd sample processed
        if (self._sample_clk == 1) {
            // KOFF
            if (v.__key_off == 1) {
                v._env_mode = .key_off;
            }

            // KON
            if (v.__key_on == 1) {
                v._key_on_delay = 5; // Once KON is processed, delay 5 samples before processing BRR decoding, envelope, pitch, etc.
                v._env_mode = .attack;
            }
        }

        if (v._key_on_delay == 0) {
            // Run envelope for next sample
            envelope.run(self, v_idx, adsr_1, gain);
        }
    }

    pub fn voice_step_d(self: *DSPStateInternal, v_idx: u3, aram_data_0: u8, vol_left: i8) void {
        const v = &self._voice[v_idx];

        // Decode BRR
        v.__looped = 0;
        if (v._gaussian_offset >= 0x4000) {
            // Decode 4 BRR samples
            brr.decode(self, v_idx, aram_data_0);
            v._brr_offset +%= 2;

            if (v._brr_offset >= 9) {
                // Start decoding next BRR block
                v._brr_address +%= 9;
                if (self._brr._cur_block_header & 0x01 == 1) { // Check if end or loop is set
                    // Seems that BRR always loops, even if loop header bit is not set
                    // It just sets envelope level to 0 instantly
                    v._brr_address = self._brr._next_address;
                    v.__looped = 1;
                }
                v._brr_offset = 1;
            }
        }

        // Advance sample offset by last written pitch (should match pitch of current voice when written)
        v._gaussian_offset = (v._gaussian_offset & 0x3FFF) + @as(u16, self._pitch);

        // Keep from getting too far ahead when using pitch modulation... I don't think it's actually possible for that to happen, but just to be safe
        if (v._gaussian_offset > 0x7FFF) {
            v._gaussian_offset = 0x7FFF;
        }

        // Output left volume
        self.voice_output(v_idx, 0, vol_left);
    }

    pub fn voice_step_e(self: *DSPStateInternal, v_idx: u3, vol_right: i8) void {
        const v = &self._voice[v_idx];

        // Output right volume
        self.voice_output(v_idx, 1, vol_right);

        // ENDX, OUTX, ENVX won't update if you wrote to them 1-2 clocks earlier
        v.__end |= v.__looped;

        // Clear ENDX bit once KON begins
        if (v._key_on_delay == 5) {
            v.__end = 0;
        }
    }

    pub fn voice_step_f(self: *DSPStateInternal) void {
        // Queue OUTX for current voice
        self._outx = @intCast(self._output >> 8);
    }

    pub fn voice_step_g(self: *DSPStateInternal, endx_reg: *u8, envx: u8) void {
        // Flush voice ENDX values to program-facing ENDX register
        endx_reg.* = 0x00;
        for (0..8) |n| {
            const b: u3 = @intCast(n);
            endx_reg.* |= @as(u8, self._voice[n].__end) << b;
        }

        // Queue ENVX for specified voice
        self._envx = envx;
    }

    pub fn voice_step_h(self: *DSPStateInternal, outx_reg: *u8) void {
        // Flush current voice OUTX to program-facing OUTX register
        outx_reg.* = @bitCast(self._outx);
    }

    pub fn voice_step_i(self: *DSPStateInternal, envx_reg: *u8) void {
        // Flush current voice ENVX to program-facing ENVX register
        envx_reg.* = self._envx;
    }
};