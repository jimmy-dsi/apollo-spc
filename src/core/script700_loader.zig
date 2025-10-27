const std = @import("std");

const Script700 = @import("script700.zig").Script700;

pub const Script700Loader = struct {
    const State = struct {
        offset: u32 = 0,
        entries_remaining: u32 = 1024,

        data_7sb: ?[]const u8 = null,

        script_area: ?[]u32 = null,
        data_area:   ?[] u8 = null,

        pub fn read_u32le(self: *State) u32 {
            const b1: u8 = self._read_byte();
            const b2: u8 = self._read_byte();
            const b3: u8 = self._read_byte();
            const b4: u8 = self._read_byte();

            const res = @as(u32, b1) | @as(u32, b2) << 8 | @as(u32, b3) << 16 | @as(u32, b4) << 24;
            self.entries_remaining -%= 1;

            return res;
        }

        pub fn read_byte(self: *State) u8 {
            const res = self._read_byte();
            self.entries_remaining -%= 1;
            return res;
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
            const address = state.read_u32le();
            s.label_addresses[i] = address;
        }

        // Load script bytecode
        state.entries_remaining = state.read_u32le();
        state.script_area = try allocator.alloc(u32, state.entries_remaining);

        var index: u32 = 0;
        while (state.entries_remaining > 0) {
            state.script_area.?[index] = state.read_u32le();
            index +%= 1;
        }

        state.entries_remaining = state.read_u32le();
        state.data_area = try allocator.alloc(u8, state.entries_remaining);

        index = 0;
        while (state.entries_remaining > 0) {
            state.data_area.?[index] = state.read_byte();
            index +%= 1;
        }

        s.load_bytecode(state.script_area.?) catch {
            // TODO: Report error
            std.process.exit(1);
        };

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