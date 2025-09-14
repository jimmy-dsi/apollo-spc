const DSPStateInternal = @import("dsp_state_int.zig").DSPStateInternal;

pub const DSPState = struct {
    pub const Echo = struct {
        feedback:  i8 = 0x00,
        vol_left:  i8 = 0x00,
        vol_right: i8 = 0x00,

        fir: [8]i8 = [_]i8 {0x00} ** 8,

        esa_page: u8 = 0x00,
        delay:    u4 = 0x0,

        readonly: u1 = 1
    };

    pub const Voice = struct {
        vol_left:  i8  = 0x00,
        vol_right: i8  = 0x00,

        pitch:  u14 = 0x0000,
        source: u8  = 0x00,

        adsr_0: u8 = 0x00,
        adsr_1: u8 = 0x00,
        gain:   u8 = 0x00,

        envx: u8 = 0x00,

        keyon:  u1 = 0,
        keyoff: u1 = 0,

        pitch_mod_on: u1 = 0,
        noise_on:     u1 = 0,
        echo_on:      u1 = 0,
        end:          u1 = 0,
    };

    reset: u1 = 1,
    mute:  u1 = 1,
    main_vol_left:  i8 = 0x00,
    main_vol_right: i8 = 0x00,

    echo: Echo  = .{},

    noise_rate: u5 = 0x00,

    brr_bank: u8 = 0x00,

    voice: [8]Voice = [_]Voice {.{}} ** 8,

    _internal: DSPStateInternal
};