pub const DSPState = struct {
    pub const Echo = struct {
        feedback:  i8 = 0x00,
        vol_left:  i8 = 0x00,
        vol_right: i8 = 0x00,

        fir: [8]i8 = [8]i8 {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00},

        esa_page: u8 = 0x00,
        delay:    u4 = 0x0,

        readonly: u1 = 1
    };

    pub const Noise = struct {
        output_rate: u5  = 0x00,
        lfsr:        u15 = 0x4000
    };

    reset: u1 = 1,
    mute:  u1 = 1,
    main_vol_left:  i8 = 0x00,
    main_vol_right: i8 = 0x00,

    echo:  Echo  = .{},
    noise: Noise = .{},
};