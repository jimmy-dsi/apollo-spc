pub const Script700State = struct {
    pub const WaitDevice = enum {
        none, input, output
    };

    port_in: [4]u8 = [_]u8 {0x00} ** 4,

    work: [8]u32 = [_]u32 {0} ** 8,
    cmp:  [2]u32 = [_]u32 {0} ** 2,

    callstack: [64]u32 = [_]u32 {0} ** 64,
    sp: u8 = 0x00,

    callstack_on:  bool = true,
    port_queue_on: bool = false,

    pc:   u32 = 0,
    step: u32 = 0,

    aram_breakpoints: [8192]u8 = [_]u8 {0x00} ** 8192,

    wait_until:  ?u64        = null,
    wait_device:  WaitDevice = .none,
    wait_port:    u2         = 0,
    wait_value: ?*u8         = null,

    pub fn reset(self: *Script700State) void {
        for (0..4) |i| {
            self.port_in[i] = 0x00;
        }

        for (0..8) |i| {
            self.work[i] = 0;
        }

        self.cmp[0] = 0;
        self.cmp[1] = 0;

        for (0..64) |i| {
            self.callstack[i] = 0;
        }

        self.callstack_on  = true;
        self.port_queue_on = false;

        self.sp   = 0x00;
        self.pc   = 0;
        self.step = 0;

        for (0..8192) |i| {
            self.aram_breakpoints[i] = 0x00;
        }

        self.wait_until = null;
    }

    pub inline fn enable_breakpoint(self: *Script700State, address: u16) void {
        const bit_addr: u13 = @intCast(address >> 3);
        const bit:      u3  = @intCast(address & 7);

        self.aram_breakpoints[bit_addr] |= @as(u8, 1) << bit;
    }

    pub inline fn has_breakpoint(self: *const Script700State, address: u16) void {
        const bit_addr: u13 = @intCast(address >> 3);
        const bit:      u3  = @intCast(address & 7);

        return self.aram_breakpoints[bit_addr] & @as(u8, 1) << bit != 0;
    }

    pub inline fn set_wait_condition(self: *Script700State, device: WaitDevice, port: u2, value: ?*u8) void {
        if (device == .none) {
            return;
        }

        self.wait_until  = 0xFFFF_FFFF_FFFF_FFFF;
        self.wait_device = device;
        self.wait_port   = port;
        self.wait_value  = value;
    }
};