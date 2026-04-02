const std = @import("std");

const Script700 = @import("script700.zig").Script700;
const Load      = Script700.Load;

pub const Script700Loader = struct {
    const State = struct {
        offset: u32 = 0,
        entries_remaining: u32 = 1024,

        data_7sb: ?[]const u8 = null,

        script_area: ?[]u32 = null,
        data_area:   ?[] u8 = null,

        pub inline fn read_u32le_safe(self: *State) !u32 {
            const b1: u8 = try self._read_byte_safe();
            const b2: u8 = try self._read_byte_safe();
            const b3: u8 = try self._read_byte_safe();
            const b4: u8 = try self._read_byte_safe();

            const res = @as(u32, b1) | @as(u32, b2) << 8 | @as(u32, b3) << 16 | @as(u32, b4) << 24;
            self.entries_remaining -%= 1;

            return res;
        }

        pub fn read_u32le(self: *State) u32 {
            const b1: u8 = self._read_byte();
            const b2: u8 = self._read_byte();
            const b3: u8 = self._read_byte();
            const b4: u8 = self._read_byte();

            const res = @as(u32, b1) | @as(u32, b2) << 8 | @as(u32, b3) << 16 | @as(u32, b4) << 24;
            self.entries_remaining -%= 1;

            return res;
        }

        pub inline fn read_byte_safe(self: *State) !u8 {
            const res = try self._read_byte_safe();
            self.entries_remaining -%= 1;
            return res;
        }

        pub fn read_byte(self: *State) u8 {
            const res = self._read_byte();
            self.entries_remaining -%= 1;
            return res;
        }

        inline fn _read_byte_safe(self: *State) !u8 {
            if (self.offset >= self.data_7sb.?.len) {
                return Load.malformed_file;
            }
            return self._read_byte();
        }

        fn _read_byte(self: *State) u8 {
            const res = self.data_7sb.?[self.offset];
            self.offset +%= 1;
            return res;
        }
    };

    pub const allocator = std.heap.page_allocator;
    pub var state = State { };

    pub fn load_script(s: *Script700, data_7sb: []const u8) !void {
        state.data_7sb = data_7sb;

        // Load labels
        for (0..1024) |i| {
            const address = try state.read_u32le_safe();
            s.label_addresses[i] = address;
        }

        // Load script bytecode
        state.entries_remaining = try state.read_u32le_safe();
        if (state.entries_remaining > 0x1000_0000) {
            return Load.bytecode_too_large;
        }

        state.script_area = try allocator.alloc(u32, state.entries_remaining);

        var index: u32 = 0;
        while (state.entries_remaining > 0) {
            state.script_area.?[index] = try state.read_u32le_safe();
            index +%= 1;
        }

        state.entries_remaining = try state.read_u32le_safe();
        if (state.entries_remaining > 0x4000_0000) {
            return Load.data_too_large;
        }

        state.data_area = try allocator.alloc(u8, state.entries_remaining);

        index = 0;
        while (state.entries_remaining > 0) {
            state.data_area.?[index] = try state.read_byte_safe();
            index +%= 1;
        }

        try s.load_bytecode(state.script_area.?);

        s.load_data(state.data_area.?);
        s.self_alloc_data = true;

        state.data_area = null;
    }

    pub fn deinit() void {
        if (state.script_area != null) {
            allocator.free(state.script_area);
            state.script_area = null;
        }

        //if (state.data_area != null) {
        //    allocator.free(state.data_area);
        //    state.data_area = null;
        //}

        state.data_area = null;

        state.entries_remaining = 1024;
        state.offset = 0;
        state.data_s7b = null;
    }
};