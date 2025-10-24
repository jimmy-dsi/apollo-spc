// Alternate render pipeline - This is used when any modifications are made in regard to player output
// i.e. Channel disables, sample rate, interpolation/echo settings, etc.
// This all goes through a separate pipeline so as not to interfere with emulation
pub const Pipeline2 = struct {
    pub const Format = enum {
        u8, s16le, s24le, s32le, f32
    };

    pub const Interpolation = enum {
        none, linear, cubic, gauss, sinc
    };

    const Settings = struct {
        rate: u32 = 32000,
        format: Format = .s16le,

        interpolation: Interpolation = .gauss,

        snes_lowpass: bool = true,

        pitch_adjust: f32 = 1.0,
        speed_adjust: f32 = 1.0,

        speed_sync_env: bool = false,

        channels_enabled: [8]bool = [_]bool {true} ** 8,
        channel_mixer:    [8]f32  = [_]f32  {1.0}  ** 8,

        master_vol: f32 = 1.0,
        main_vol:   f32 = 1.0,
        echo_vol:   f32 = 1.0,

        stereo_sep: f32 = 1.0,

        reverse_master: bool = false,
        reverse_main:   bool = false,
        reverse_echo:   bool = false,

        double_echo: bool = false,

        disable_surround:    bool = false,
        disable_fir:         bool = false,
        disable_echo:        bool = false,
        disable_pitch_mod:   bool = false,
        disable_noise:       bool = false,
        disable_pitch_limit: bool = false,
        disable_envelope:    bool = false
    };

    pub const Voice = struct {
        buffer_left:  [3]f64 = [_]f64{0} ** 3,
        buffer_right: [3]f64 = [_]f64{0} ** 3,

        vol_left:  i8 = 0x7F,
        vol_right: i8 = 0x7F,

        env: u11 = 0x7FF,

        noise: bool = false,
        pmod:  bool = false
    };

    pub const Echo = struct {
        delay: u4 = 0,
        feedback: i8 = 0,

        fir: [8]i8 = [_]i8{0} ** 8,
        eon: u8 = 0,

        shadow_buffer: [0x2000]f64 = [_]f64{0} ** 0x2000
    };

    enabled: bool = false,
    settings: Settings = .{},

    mvol_left:  i8 = 0x7F,
    mvol_right: i8 = 0x7F,
    evol_left:  i8 = 0x00,
    evol_right: i8 = 0x00,

    echo: Echo = .{},
    noise_out: i16 = 0,

    voice: [8]Voice = [_]Voice{.{}} ** 8,

    samples_queued: u2 = 1,

    dac_left:  [3]f64 = [_]f64{0} ** 3,
    dac_right: [3]f64 = [_]f64{0} ** 3,

    pub fn disable_voice(self: *Pipeline2, index: u3) void {
        self.enabled = true;
        self.settings.channels_enabled[index] = false;
    }

    pub fn enable_voice(self: *Pipeline2, index: u3) void {
        self.enabled = true;
        self.settings.channels_enabled[index] = true;
    }

    pub inline fn voice_output(self: *Pipeline2, index: u3, out: []const i16, vol: i8, comptime channel: u1) void {
        const v = &self.voice[index];

        for (out, 0..) |o, i| {
            if (channel == 0) {
                v.buffer_left[i] = i16_to_f64(o);
            }
            else {
                v.buffer_right[i] = i16_to_f64(o);
            }
        }

        if (channel == 0) {
            v.vol_left = vol;
        }
        else {
            v.vol_right = vol;
        }
    }

    pub fn output(self: *Pipeline2) void {
        for (0..self.samples_queued) |i| {
            // TODO: Consider alternate output formats for mixing
            var sum_left:  i32 = 0;
            var sum_right: i32 = 0;

            for (0..8) |c| {
                if (self.settings.channels_enabled[c]) {
                    const v = &self.voice[c];

                    const left  = f64_to_i16(v.buffer_left[i]);
                    const right = f64_to_i16(v.buffer_right[i]);

                    sum_left  +%= @as(i32, left)  * @as(i32, v.vol_left)  >> 7;
                    sum_right +%= @as(i32, right) * @as(i32, v.vol_right) >> 7;

                    sum_left  = clamp_i16(i32, sum_left);
                    sum_right = clamp_i16(i32, sum_right);
                }
            }

            self.dac_left[i]  = i16_to_f64(@intCast(sum_left));
            self.dac_right[i] = i16_to_f64(@intCast(sum_right));
        }
    }

    pub fn get_output_i16(self: *const Pipeline2, index: u2) struct {i16, i16} {
        return .{
            f64_to_i16(self.dac_left[index]),
            f64_to_i16(self.dac_right[index])
        };
    }

    fn f64_to_i16(n: f64) i16 {
        return @intFromFloat(n * 0x8000);
    }

    fn i16_to_f64(n: i16) f64 {
        const fl: f64 = @floatFromInt(n);
        return fl / 0x8000;
    }

    fn clamp_i16(comptime T: type, n: T) T {
        if (n > 0x7FFF) {
            return 0x7FFF;
        }
        else if (n < -0x8000) {
            return -0x8000;
        }
        else {
            return n;
        }
    }
};