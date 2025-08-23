pub const SPCState = struct {
    a: u8 = undefined,
    x: u8 = undefined,
    y: u8 = undefined,

    sp: u8  = undefined,
    pc: u16 = undefined,

    psw: u8 = undefined,

    pub fn new(a: ?u8, x: ?u8, y: ?u8, sp: ?u8, pc: ?u16, psw: ?u8) SPCState {
        var cpu_state = SPCState { };
        cpu_state.reset();

        if (a)   |val| { cpu_state.a   = val; }
        if (x)   |val| { cpu_state.x   = val; }
        if (y)   |val| { cpu_state.y   = val; }
        if (sp)  |val| { cpu_state.sp  = val; }
        if (pc)  |val| { cpu_state.pc  = val; }
        if (psw) |val| { cpu_state.psw = val; }

        return cpu_state;
    }

    pub fn reset(self: *SPCState) void {
        self.a = 0x00;
        self.x = 0x00;
        self.y = 0x00;

        self.sp = 0xEF;
        self.pc = 0x0000;

        self.psw = 0b00000010;
    }

    // PSW register flag getters
    pub inline fn n(self: *const SPCState) u1 {
        return self.get_psw_flag(7);
    }

    pub inline fn v(self: *const SPCState) u1 {
        return self.get_psw_flag(6);
    }

    pub inline fn p(self: *const SPCState) u1 {
        return self.get_psw_flag(5);
    }

    pub inline fn b(self: *const SPCState) u1 {
        return self.get_psw_flag(4);
    }

    pub inline fn h(self: *const SPCState) u1 {
        return self.get_psw_flag(3);
    }

    pub inline fn i(self: *const SPCState) u1 {
        return self.get_psw_flag(2);
    }

    pub inline fn z(self: *const SPCState) u1 {
        return self.get_psw_flag(1);
    }

    pub inline fn c(self: *const SPCState) u1 {
        return self.get_psw_flag(0);
    }

    // PSW register flag setters
    pub inline fn set_n(self: *SPCState, value: u1) void {
        self.set_psw_flag(7, value);
    }

    pub inline fn set_v(self: *SPCState, value: u1) void {
        self.set_psw_flag(6, value);
    }

    pub inline fn set_p(self: *SPCState, value: u1) void {
        self.set_psw_flag(5, value);
    }

    pub inline fn set_b(self: *SPCState, value: u1) void {
        self.set_psw_flag(4, value);
    }

    pub inline fn set_h(self: *SPCState, value: u1) void {
        self.set_psw_flag(3, value);
    }

    pub inline fn set_i(self: *SPCState, value: u1) void {
        self.set_psw_flag(2, value);
    }

    pub inline fn set_z(self: *SPCState, value: u1) void {
        self.set_psw_flag(1, value);
    }

    pub inline fn set_c(self: *SPCState, value: u1) void {
        self.set_psw_flag(0, value);
    }

    // Helpers
    inline fn get_psw_flag(self: *const SPCState, comptime bit: u8) u1 {
        return @intCast(self.psw >> bit & 1);
    }

    inline fn set_psw_flag(self: *SPCState, comptime bit: u8, value: u1) void {
        const bitval = @as(u8, 1) << bit;
        if (value == 1) {
            self.psw |= bitval;
        }
        else {
            self.psw &= ~bitval;
        }
    }
};