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