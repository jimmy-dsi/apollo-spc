pub const DSPStateInternal = struct {
    pub const EnvMode = enum {
        release, attack, decay, sustain
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
        _env_mode:        EnvMode  = .release,
        _env_level:       u11      = 0,                // Current envelope level (0-2047)
    };

    _brr:   BRR      = .{},
    _voice: [8]Voice = [_]Voice {.{}} ** 8,

    // Latch state
    _adsr_0: u8  = 0x00,
    _envx:   u8  = 0x00,
    _outx:   u8  = 0x00,
    _pitch:  u15 = 0x0000,
    _output: i16 = 0x0000,

    pub fn voice_step_a(self: *DSPStateInternal, source: u8) void {
        self._brr._cur_address = (@as(u16, self._brr._bank) << 8) +% (@as(u16, self._brr._cur_source) << 2);
        self._brr._cur_source = source;
    }

    pub fn voice_step_b(self: *DSPStateInternal, v_idx: u3, aram_0: [*]u8, aram_1: [*]u8, adsr_0: u8, pitch_lo: u8) void {
        // Read sample pointer (ignored if not needed)
        var address: u16 = self._brr._cur_address;
        if (self._voice[v_idx]._key_on_delay == 0) {
            address +%= 2;
        }

        // Do this to prevent buffer overflow
        var hi_address: i32 = @intCast(address +% 1);
        hi_address -= 1;
        
        self._brr._next_address =
              @as(u16, (aram_0 + address)[0])
            | @as(u16, (aram_1 + hi_address)[0]) << 8;

        self._adsr_0 = adsr_0;

        // Read pitch, spread over two clocks
        self._pitch = @intCast(pitch_lo);
    }

    pub fn voice_step_c(self: *DSPStateInternal, v_idx: u3, aram_data_0: u8, aram_data_1: u8, pitch_hi: u8, adsr_1: u8, gain: u8, envx: *u8) void {
        self.voice_step_c_pt1(pitch_hi);
        self.voice_step_c_pt2(aram_data_0, aram_data_1);
        self.voice_step_c_pt3(v_idx, adsr_1, gain, envx);
    }

    pub fn voice_step_c_pt1(self: *DSPStateInternal, pitch_hi: u8) void {
        self._pitch |= @as(u16, pitch_hi) << 8;
    }

    pub fn voice_step_c_pt2(self: *DSPStateInternal, aram_data_0: u8, aram_data_1: u8) void {
        self._brr._cur_block_byte   = aram_data_0;
        self._brr._cur_block_header = aram_data_1;
    }

    pub fn voice_step_c_pt3(self: *DSPStateInternal, v_idx: u3, adsr_1: u8, gain: u8, envx: *u8) void {
        const v = &self._voice[v_idx];

        _ = v;
        _ = adsr_1;
        _ = gain;
        envx.* += 0;
    }
};