const std = @import("std");

// Alternate render pipeline - This is used when any modifications are made in regard to player output
// i.e. Channel disables, sample rate, interpolation/echo settings, etc.
// This all goes through a separate pipeline so as not to interfere with emulation
pub const Pipeline2 = struct {
    pub const ProcessStage = enum {
        Voices, MainEcho, Master
    };

    // Full emulator rendering diagram:
    // ┌───────────────────────────────────────────────────────┐
    // │ S-DSP                                    ┌────────┐   │  ┌┐ ┌───────────┐
    // │                                      ┌───┤ Master ├───┼──┤├─┤ Post-proc ├─ Speaker
    // │   ┌────────────┐            ┌──────┐ │ ┌─┤        │   │ ┌┤│ │           │
    // │   │ Voices 0-8 ├───█───/────┤ Main ├─█ │ │        │   │ │└┘ └───────────┘
    // │   └────────────┘   │        │      │ │ │ │        │   │ │      ▲
    // │                    │        └──────┘ │ │ └────────┘   │ │     Final mix
    // │                    │        ┌──────┐ │ │              │ │     multiplier
    // │                    █───/────┤ Echo ├─┼─█              │ │
    // │                    │        │      │ │ │              │ │
    // │                    │        └──────┘ │ │              │ │
    // └────────────────────┼─────────────────┼─┼──────────────┘ │
    // ┌────────────────────┼─────────────────┼─┼──────────────┐ │
    // │ "Pipeline 2"       │   Chan. muting  │ │ ┌────────┐   │ │
    // │                    │           ▼     │ └─┤ Master ├───┼─┘
    // │                    │     ┌┐ ┌──────┐ └───┤        │   │
    // │   ┌────────────┐   █───/─┤├─┤ Main ├─────┤        │   │
    // │   │ Voices 0-8 ├─█─┼───/─┤│ │      │ ┌───┤        │   │
    // │   └────────────┘ │ │     └┘ └──────┘ │   └────────┘   │
    // │     ▲            │ │     ┌┐ ┌──────┐ │     ▲          │
    // │    Speed/pitch   │ └───/─┤├─┤ Echo ├─┘   Main/vol mix │
    // │    changes       └─────/─┤│ │      │     multipliers  │
    // │                          └┘ └──────┘                  │
    // └───────────────────────────────────────────────────────┘
    //
    // Notes:
    //  - S-DSP is always processed regardless of any Pipeline 2 overrides. Therefore, these do not affect the actual emulation in any way
    //  - In terms of what gets output to the speaker, Pipeline 2 can override S-DSP rendering at any stage and utilize the output of
    //    any of the S-DSP sub-modules diagrammed above.
    //  - This is done to ensure that the output is as true to hardware as possible, and only some components will be overridden as is necessary
    // Examples:
    //  - Adjusting the mixing levels of the main or echo streams will override the "Master" submodule in the Pipeline 2 engine
    //    In this case, Pipeline 2 will not handle "Voices 0-8" or Main and Echo - It will take the S-DSP's output from those instead
    //  - Disabling 1 or more channels will override the "Main" and "Echo" submodules in the Pipeline 2 engine
    //  - Adjusting the pitch offset of the song requires Pipeline 2 to override the "Voices 0-8" portion
    //    This effectively overrides the entire S-DSP output and makes it so the final output is produced entirely from Pipeline 2

    // Note: Currently, pipeline 2 always overrides the "Main/echo" portion
    // TODO: Finish implementation according to the above diagram

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

        pitch_adjust: f32 = 1.0,
        speed_adjust: f32 = 1.0,

        speed_sync_env: bool = false,

        channels_enabled:   [8]bool = [_]bool {true} ** 8, // For main
        e_channels_enabled: [8]bool = [_]bool {true} ** 8, // For echo
        channel_mixer:      [8]f32  = [_]f32  {1.0}  ** 8,

        main_vol:   f32 = 1.0,
        echo_vol:   f32 = 1.0,

        stereo_sep: f32 = 1.0,

        reverse_main:   bool = false,
        reverse_echo:   bool = false,

        double_echo: bool = false,

        disable_surround:    bool = false,
        disable_fir:         bool = false,
        disable_main:        bool = false,
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
        readonly: bool = false,

        esa: u8 = 0,
        delay: u4 = 0,
        feedback: i8 = 0,

        fir: [8]i8 = [_]i8{0} ** 8,
        eon: u8 = 0,

        aram_record: [0x1_0000]?u8 = [_]?u8{null} ** 0x1_0000, // Records ARAM values written by the SMP, or uninited DSP buffer values
        shadow_buffer_a: [0x8000]f64 = [_]f64{0} ** 0x8000,    // Main buffer used by this second pipeline - can be written through from ARAM writes
        shadow_buffer_b: [0x8000]f64 = [_]f64{0} ** 0x8000,    // Similar to above, used when double echo-delay is enabled

        history_left:  [8]f64 = [_]f64{0} ** 8,
        history_right: [8]f64 = [_]f64{0} ** 8,

        history_idx: u8 = 0,

        read_head: u32 = 0,

        out_left:  [3]f64 = [_]f64{0} ** 3,
        out_right: [3]f64 = [_]f64{0} ** 3,

        write_left:  [3]f64 = [_]f64{0} ** 3,
        write_right: [3]f64 = [_]f64{0} ** 3,

        _cur_edl: u4 = 0,
        _cur_esa: u8 = 0,
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

    pub fn toggle_voice(self: *Pipeline2, index: u3) void {
        if (self.settings.channels_enabled[index]) {
            self.disable_voice(index);
        }
        else {
            self.enable_voice(index);
        }
    }

    pub fn disable_voice(self: *Pipeline2, index: u3) void {
        self.enabled = true;
        self.settings.channels_enabled[index]   = false;
        self.settings.e_channels_enabled[index] = false;
    }

    pub fn enable_voice(self: *Pipeline2, index: u3) void {
        self.enabled = true;
        self.settings.channels_enabled[index]   = true;
        self.settings.e_channels_enabled[index] = true;
        self.check_settings();
    }

    pub fn toggle_main_voice(self: *Pipeline2, index: u3) void {
        if (self.settings.channels_enabled[index]) {
            self.disable_main_voice(index);
        }
        else {
            self.enable_main_voice(index);
        }
    }

    pub fn disable_main_voice(self: *Pipeline2, index: u3) void {
        self.enabled = true;
        self.settings.channels_enabled[index] = false;
    }

    pub fn enable_main_voice(self: *Pipeline2, index: u3) void {
        self.enabled = true;
        self.settings.channels_enabled[index] = true;
        self.check_settings();
    }

    pub fn toggle_echo_voice(self: *Pipeline2, index: u3) void {
        if (self.settings.e_channels_enabled[index]) {
            self.disable_echo_voice(index);
        }
        else {
            self.enable_echo_voice(index);
        }
    }

    pub fn disable_echo_voice(self: *Pipeline2, index: u3) void {
        self.enabled = true;
        self.settings.e_channels_enabled[index] = false;
    }

    pub fn enable_echo_voice(self: *Pipeline2, index: u3) void {
        self.enabled = true;
        self.settings.e_channels_enabled[index] = true;
        self.check_settings();
    }

    pub fn disable_main(self: *Pipeline2) void {
        self.enabled = true;
        self.settings.disable_main = true;
    }

    pub fn enable_main(self: *Pipeline2) void {
        self.enabled = true;
        self.settings.disable_main = false;
        self.check_settings();
    }

    pub inline fn write_through(self: *Pipeline2, aram_addr: u16, byte_val: u8) void {
        var e = &self.echo;
        e.aram_record[aram_addr] = byte_val;

        var samp_u16:     u16 = undefined;
        var samp:         i16 = undefined;
        var shadow_index: u32 = undefined;

        if (aram_addr % 2 == 0) {
            samp_u16 = @as(u16, e.aram_record[aram_addr].?) | @as(u16, e.aram_record[aram_addr +% 1] orelse 0) << 8;
            samp     = @bitCast(samp_u16);

            shadow_index = @as(u32, @divFloor(aram_addr, 2));
        }
        else {
            samp_u16 = @as(u16, e.aram_record[aram_addr -% 1] orelse 0) | @as(u16, e.aram_record[aram_addr].?) << 8;
            samp     = @bitCast(samp_u16);

            shadow_index = @as(u32, @divFloor(aram_addr -% 1, 2));
        }

        const f_sample: f64 = i16_to_f64(samp);
        e.shadow_buffer_a[shadow_index] = f_sample;
        e.shadow_buffer_b[shadow_index] = f_sample;
    }

    pub inline fn trigger_echo_read(self: *Pipeline2, comptime channel: u1, sample: i16) void {
        var e = &self.echo;

        const head: u32 = e.read_head +% @as(u32, channel);

        var aram_addr: u16 = self.esa_base_aram();
        aram_addr +%= @intCast(head * 2);

        const buffer_index: u32 = (self.esa_base_index() +% head) % 0x8000;

        // Carry over data from the DSP echo buffer if we're attempting to read from an uninitialized area of the shadow buffer
        if (e.aram_record[aram_addr] == null or e.aram_record[aram_addr +% 1] == null) {

            e.aram_record[aram_addr]      = @intCast(sample & 0xFF);
            e.aram_record[aram_addr +% 1] = @intCast(sample >>   8);

            const f_sample: f64 = i16_to_f64(sample);
            e.shadow_buffer_a[buffer_index] = f_sample;
            e.shadow_buffer_b[buffer_index] = f_sample;
        }
    }

    pub inline fn set_fir_coef(self: *Pipeline2, comptime fir_idx: u3, value: i8) void {
        var e = &self.echo;
        e.fir[fir_idx] = value;
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
        var e = &self.echo;

        for (0..self.samples_queued) |i| {
            // TODO: Consider alternate output formats for mixing
            var sum_left:    i32 = 0;
            var sum_right:   i32 = 0;
            var e_sum_left:  i32 = 0;
            var e_sum_right: i32 = 0;

            for (0..8) |c| {
                const cc: u3 = @intCast(c);

                const v = &self.voice[c];

                const left  = f64_to_i16(v.buffer_left[i]);
                const right = f64_to_i16(v.buffer_right[i]);
                const voice_left:  i32 = @as(i32, left)  * @as(i32, v.vol_left)  >> 7;
                const voice_right: i32 = @as(i32, right) * @as(i32, v.vol_right) >> 7;

                if (self.settings.channels_enabled[c]) {
                    sum_left  +%= voice_left;
                    sum_right +%= voice_right;
                    sum_left  = clamp_i16(i32, sum_left);
                    sum_right = clamp_i16(i32, sum_right);
                }

                if (self.settings.e_channels_enabled[c] and e.eon & (@as(u8, 1) << cc) != 0) { // If echo enabled for channel
                    e_sum_left  +%= voice_left;
                    e_sum_right +%= voice_right;
                    e_sum_left  = clamp_i16(i32, e_sum_left);
                    e_sum_right = clamp_i16(i32, e_sum_right);
                }
            }

            sum_left  = trunc_i16(i32, sum_left  * self.mvol_left  >> 7);
            sum_right = trunc_i16(i32, sum_right * self.mvol_right >> 7);

            // TODO: Consider case when multiple samples will be output at once for echo - Also consider double-echo mode
            const buffer_index: u32 = self.esa_base_index() +% e.read_head;

            e.history_left[e.history_idx]  = e.shadow_buffer_a[buffer_index];
            e.history_right[e.history_idx] = e.shadow_buffer_a[buffer_index +% 1];

            e.history_idx = (e.history_idx + 1) % 8;

            // Perform FIR calculation (TODO: Consider 24-bit/32-bit precision)
            var out_left:  i17 = 0;
            var out_right: i17 = 0;

            inline for (0..8) |f| {
                const idx = (e.history_idx + f) % 8;
                
                const samp_left:  i16 = f64_to_i16(e.history_left[idx])  >> 1;
                const samp_right: i16 = f64_to_i16(e.history_right[idx]) >> 1;

                var fir_res_left:  i17 = @truncate(@as(i32,  samp_left) * @as(i32, e.fir[f]) >> 6);
                var fir_res_right: i17 = @truncate(@as(i32, samp_right) * @as(i32, e.fir[f]) >> 6);

                if (f == 7) {
                    fir_res_left  = trunc_i16(i17, fir_res_left);
                    fir_res_right = trunc_i16(i17, fir_res_right);
                }
                
                out_left  +%= fir_res_left;
                out_right +%= fir_res_right;

                if (f == 6) {
                    out_left  = trunc_i16(i17, out_left);
                    out_right = trunc_i16(i17, out_right);
                }
            }

            out_left  = clamp_i16(i17, out_left)  & ~@as(i17, 1);
            out_right = clamp_i16(i17, out_right) & ~@as(i17, 1);

            const fb_left:  i16 = @truncate(@as(i32, out_left)  * @as(i32, e.feedback) >> 7);
            const fb_right: i16 = @truncate(@as(i32, out_right) * @as(i32, e.feedback) >> 7);

            out_left  = trunc_i16(i17, @intCast(@as(i32, out_left)  * self.evol_left  >> 7));
            out_right = trunc_i16(i17, @intCast(@as(i32, out_right) * self.evol_right >> 7));

            e.out_left[i]  = i16_to_f64(@intCast(out_left));
            e.out_right[i] = i16_to_f64(@intCast(out_right));

            var left:  i17 = @intCast(e_sum_left);  
            var right: i17 = @intCast(e_sum_right);
            left  +%= @as(i17,  fb_left);
            right +%= @as(i17, fb_right);

            // Store into output buffer, to be written back to the echo buffer
            const write_left:  i16 = @truncate(clamp_i16(i17, left)  & ~@as(i17, 1));
            const write_right: i16 = @truncate(clamp_i16(i17, right) & ~@as(i17, 1));
            e.write_left[i]  = i16_to_f64(write_left);
            e.write_right[i] = i16_to_f64(write_right);

            var dac_left_i17:  i17 = undefined;
            var dac_right_i17: i17 = undefined;

            if (self.settings.disable_main) {
                dac_left_i17  = @intCast(out_left);
                dac_right_i17 = @intCast(out_right);
            }
            else {
                dac_left_i17  = @intCast(sum_left);
                dac_right_i17 = @intCast(sum_right);
                dac_left_i17  +%= out_left;
                dac_right_i17 +%= out_right;
            }

            self.dac_left[i]  = i16_to_f64(@intCast(clamp_i16(i17,  dac_left_i17)));
            self.dac_right[i] = i16_to_f64(@intCast(clamp_i16(i17,  dac_left_i17)));
        }
    }

    pub inline fn prepare_next_output(self: *Pipeline2) void {
        var e = &self.echo;

        e._cur_esa = e.esa;

        // Post-output: Prepare echo parameters for next pipeline cycle
        const read_head = e.read_head;

        if (e.read_head == 0) {
            e._cur_edl = e.delay;
        }

        e.read_head +%= 2;
        if (e.read_head >= @as(u32, e._cur_edl) << 10) {
            e.read_head = 0;
        }

        // Write back to buffer
        if (!e.readonly) {
            const buffer_index = (self.esa_base_index() +% read_head) % 0x8000;
            e.shadow_buffer_a[buffer_index]      = e.write_left[0];
            e.shadow_buffer_a[buffer_index +% 1] = e.write_right[0];
        }
    }

    pub fn get_output_i16(self: *const Pipeline2, index: u2) struct {i16, i16} {
        return .{
            f64_to_i16(self.dac_left[index]),
            f64_to_i16(self.dac_right[index])
        };
    }

    inline fn edl_aram_len(self: *const Pipeline2) u16 {
        const e = &self.echo;
        return @as(u16, e._cur_edl) * 0x800;
    }

    inline fn edl_buffer_len(self: *const Pipeline2) u32 {
        const e = &self.echo;
        return @as(u32, e._cur_edl) * 0x400;
    }

    inline fn esa_base_aram(self: *const Pipeline2) u16 {
        const e = &self.echo;
        return @as(u16, e._cur_esa) * 0x100;
    }

    inline fn esa_base_index(self: *const Pipeline2) u32 {
        const e = &self.echo;
        return @as(u32, e._cur_esa) * 0x80;
    }

    fn check_settings(self: *Pipeline2) void {
        if (self.settings.disable_main) {
            return;
        }

        for (0..8) |c| {
            if (!self.settings.channels_enabled[c]) {
                return;
            }
            if (!self.settings.e_channels_enabled[c]) {
                return;
            }
        }

        self.enabled = false;
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

    fn trunc_i16(comptime T: type, n: T) T {
        const n_i16: i16 = @truncate(n);
        return @as(T, n_i16);
    }

    fn clamp_float(comptime T: type, n: T) T {
        if (n > 1) {
            return 1;
        }
        else if (n < -1) {
            return -1;
        }
        else {
            return n;
        }
    }
};