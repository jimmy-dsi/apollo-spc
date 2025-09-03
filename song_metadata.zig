const std = @import("std");

const spc_loader  = @import("spc_loader.zig");
const read_u16_le = spc_loader.read_u16_le;
const read_u24_le = spc_loader.read_u24_le;
const read_u32_le = spc_loader.read_u32_le;

pub const SongMetadata = struct {
    // ID666 Main
    title:    ?[257] u8 = null,
    artist:   ?[257] u8 = null,
    game:     ?[257] u8 = null,
    dumper:   ?[257] u8 = null,
    comments: ?[257] u8 = null,

    month: ?u32 = null,
    day:   ?u32 = null,
    year:  ?u32 = null,

    date_other: ?[12]u8 = null,

    length_in_seconds: ?u32 = null,
    fade_length_in_ms: ?u32 = null,

    channels_disabled: ?[8]u1 = null,

    emulator_id: ?u8 = null,

    // ID666 Extended
    ost_title: ?[257] u8 = null,
    ost_disc:  ?u8 = null,
    ost_track: ?u8 = null,

    publisher: ?[257] u8 = null,
    copyright_year: ?u32 = null,

    intro_length_in_dsp_cycles: ?u32 = null,
    loop_length_in_dsp_cycles:  ?u32 = null,
    end_length_in_dsp_cycles:   ?u32 = null,
    loop_times:                 ?u8  = null,

    mixing_level: ?u8 = null,

    pub fn new(use_id666: bool, id666_main: []const u8, id666_ext: []const u8) SongMetadata {
        var r = SongMetadata { };

        if (use_id666) {
            r.title    = [_]u8 {0x00} ** 257;
            r.game     = [_]u8 {0x00} ** 257;
            r.dumper   = [_]u8 {0x00} ** 257;
            r.comments = [_]u8 {0x00} ** 257;

            r.month = 0x00;
            r.day   = 0x00;
            r.year  = 0x00;

            r.date_other = [_]u8 {0} ** 12;

            r.length_in_seconds = 0;
            r.fade_length_in_ms = 0;

            r.artist = [_]u8 {0x00} ** 257;

            r.channels_disabled = [_]u1 {0} ** 8;
            r.emulator_id = 0x00;

            var of: u32 = 0;
            @memcpy(r.title.?[0..32],    id666_main[of..(of+32)]); of += 32; r.title.?[32]    = 0;
            @memcpy(r.game.?[0..32],     id666_main[of..(of+32)]); of += 32; r.game.?[32]     = 0;
            @memcpy(r.dumper.?[0..16],   id666_main[of..(of+16)]); of += 16; r.dumper.?[32]   = 0;
            @memcpy(r.comments.?[0..32], id666_main[of..(of+32)]); of += 32; r.comments.?[32] = 0;

            const fmt = determine_format(id666_main[0x70..]);
            switch (fmt) {
                ID666Fmt.text => {
                    const date = extract_date(id666_main[0x70..0x7B]) catch null;
                    if (date) |d| {
                        r.month = d.month;
                        r.day   = d.day;
                        r.year  = d.year;

                        r.date_other = null;
                    }
                    else {
                        r.month = null;
                        r.day   = null;
                        r.year  = null;

                        @memcpy(r.date_other.?[0..11], id666_main[0x70..0x7B]); r.date_other.?[11] = 0;
                    }

                    r.length_in_seconds = std.fmt.parseInt(u32, id666_main[0x7B..0x7E], 10) catch null;
                    r.fade_length_in_ms = std.fmt.parseInt(u32, id666_main[0x7E..0x83], 10) catch null;

                    @memcpy(r.artist.?[0..32], id666_main[0x83..0xA3]); r.artist.?[32] = 0;

                    r.set_chan_disables(id666_main[0xA3]);
                    r.emulator_id = std.fmt.parseInt(u8, id666_main[0xA4..0xA5], 10) catch null;
                },
                ID666Fmt.binary => {
                    r.month = id666_main[0x71];
                    r.day   = id666_main[0x70];
                    r.year  = read_u16_le(id666_main, 0x72);

                    r.date_other = null;

                    r.length_in_seconds = @intCast(read_u24_le(id666_main, 0x7B));
                    r.fade_length_in_ms = @intCast(read_u32_le(id666_main, 0x7E));

                    @memcpy(r.artist.?[0..32], id666_main[0x82..0xA2]);

                    r.set_chan_disables(id666_main[0xA2]);
                    r.emulator_id = id666_main[0xA3];
                }
            }
        }

        _ = id666_ext;

        return r;
    }

    pub fn print(self: *const SongMetadata) !void {
        const padding: u32 = 13;
        const max_len: u32 = 64;

        print_str("Title:",       if (self.title != null)    null_term(self.title.?[0..])    else "\x1B[32m<none>\x1B[0m", padding, max_len);
        print_str("Artist:",      if (self.artist != null)   null_term(self.artist.?[0..])   else "\x1B[32m<none>\x1B[0m", padding, max_len);
        print_str("Game:",        if (self.game != null)     null_term(self.game.?[0..])     else "\x1B[32m<none>\x1B[0m", padding, max_len);
        print_str("Dumper:",      if (self.dumper != null)   null_term(self.dumper.?[0..])   else "\x1B[32m<none>\x1B[0m", padding, max_len);
        print_str("Comments:",    if (self.comments != null) null_term(self.comments.?[0..]) else "\x1B[32m<none>\x1B[0m", padding, max_len);

        if (self.date_other) |d| {
            print_str("Date Dumped:", d[0..], padding, max_len);
        }
        else if (self.year != null and self.month != null and self.day != null) {
            var date_str = [_]u8 {'0', '0', '0', '0', '-', '0', '0', '-', '0', '0', 0};

            _ = try buf_print_padded(date_str[0..4],  self.year.?,  10_000);
            _ = try buf_print_padded(date_str[5..7],  self.month.?,    100);
            _ = try buf_print_padded(date_str[8..10], self.day.?,      100);

            print_str("Date Dumped:", date_str[0..10], padding, max_len);
        }
        else {
            print_str("Date Dumped:", "\x1B[32m<none>\x1B[0m", padding, max_len);
        }

        if (self.length_in_seconds) |slis| {
            const song_length = try buf_print_time(slis, false);
            print_str("Song Length:", song_length[0..], padding, max_len);
        }
        else {
            print_str("Song Length:", "\x1B[32m<none>\x1B[0m", padding, max_len);
        }

        if (self.fade_length_in_ms) |flims| {
            const fade_length = try buf_print_time(flims, true);
            print_str("Fade Time:", fade_length[0..], padding, max_len);
        }
        else {
            print_str("Fade Time:", "\x1B[32m<none>\x1B[0m", padding, max_len);
        }

        std.debug.print("Initial Channel States:\n", .{});
        for (0..8) |i| {
            std.debug.print("    #{d}: ", .{i});
            if (self.channels_disabled == null) {
                std.debug.print("Enabled\n", .{});
            }
            else if (self.channels_disabled.?[i] == 0) {
                std.debug.print("Enabled\n", .{});
            }
            else {
                std.debug.print("Disabled\n", .{});
            }
        }

        if (self.emulator_id) |emu_id| {
            var emu_buf = [_]u8 {' ', ' ', ' '};
            const emu_str = try std.fmt.bufPrint(emu_buf[0..], "{}", .{emu_id});
            print_str("Emulator ID:", emu_str, padding, max_len);
        }
        else {
            print_str("Emulator ID:", "\x1B[32m<none>\x1B[0m", padding, max_len);
        }
        
        std.debug.print("\n", .{});
    }

    fn buf_print_time(amount: u32, use_ms: bool) ![13]u8 {
        var time_str = [_]u8 {'0', '0', ':', '0', '0', ':', '0', '0', '.', '0', '0', '0', 0};

        var ms: u64 = @intCast(amount);

        if (!use_ms) {
            ms *= 1000;
        }

        const ms_: u32 = @intCast(ms % 1000);
        const s_:  u32 = @intCast(ms / 1000 % 60);
        const m_:  u32 = @intCast(ms / (1000 * 60) % 60);
        const h_:  u32 = @intCast(ms / (1000 * 60 * 60) % 100);

        _ = try buf_print_padded(time_str[0..2],  h_,   100);
        _ = try buf_print_padded(time_str[3..5],  m_,   100);
        _ = try buf_print_padded(time_str[6..8],  s_,   100);
        _ = try buf_print_padded(time_str[9..12], ms_, 1000);

        return time_str;
    }

    fn buf_print_padded(buf: []u8, value: u32, max_value: u32) ![]u8 {
        if (max_value >= 10_000) {
            if (value >= 1000) {
                return std.fmt.bufPrint(buf,      "{}", .{value % max_value});
            }
            else if (value >= 100) {
                return std.fmt.bufPrint(buf[1..], "{}", .{value % max_value});
            }
            else if (value >= 10) {
                return std.fmt.bufPrint(buf[2..], "{}", .{value % max_value});
            }
            else {
                return std.fmt.bufPrint(buf[3..], "{}", .{value % max_value});
            }
        }
        else if (max_value >= 1000) {
            if (value >= 100) {
                return std.fmt.bufPrint(buf, "{}", .{value % max_value});
            }
            else if (value >= 10) {
                return std.fmt.bufPrint(buf[1..], "{}", .{value % max_value});
            }
            else {
                return std.fmt.bufPrint(buf[2..], "{}", .{value % max_value});
            }
        }
        else {
            if (value >= 10) {
                return std.fmt.bufPrint(buf, "{}", .{value % max_value});
            }
            else {
                return std.fmt.bufPrint(buf[1..], "{}", .{value % max_value});
            }
        }
    }

    fn print_str(label: []const u8, str: []const u8, comptime padding: u32, max_len: u32) void {
        const pad_str = [_]u8 {' '} ** padding;

        var remainder: u32 = @intCast(str.len);
        var start:     u32 = 0;

        var label_str = [_]u8 {' '} ** padding;
        for (label, 0..) |char, i| {
            label_str[i] = char;
        }

        std.debug.print("{s}", .{label_str});

        while (remainder > max_len) {
            if (start > 0) {
                std.debug.print("{s}", .{pad_str});
            }
            std.debug.print("{s}\n", .{str[start..(start + max_len)]});

            remainder -= max_len;
            start     += max_len;
        }

        if (start > 0) {
            std.debug.print("{s}", .{pad_str});
        }
        std.debug.print("{s}\n", .{str[start..]});
    }

    fn null_term(in_buf: []const u8) []const u8 {
        const sent_ptr: [*:0]const u8 = @ptrCast(in_buf.ptr);
        const slice = std.mem.span(sent_ptr);
        return slice;
    }

    fn set_chan_disables(self: *SongMetadata, value: u8) void {
        for (0..8) |bit| {
            const b: u3 = @intCast(bit);
            self.channels_disabled.?[b] = @intCast(value >> b & 1);
        }
    }
};

const ID666Fmt = enum {
    text,
    binary
};

fn determine_format(id666_main_pt2: []const u8) ID666Fmt {
    var binary_score: i32 = 0;

    const date = id666_main_pt2[0..11];
    if (std.mem.allEqual(u8, date[4..], 0x00) or std.mem.allEqual(u8, date[4..], 0xFF)) {
        binary_score += 3;
    }
    else if (std.mem.allEqual(u8, date[4..10], date[4])) {
        if (date[4] < 0x20 or date[4] >= 0x7F) {
            binary_score += 2;
        }
        else if (std.mem.allEqual(u8, date[0..4], date[4])) {
            binary_score -= 1;
        }
        else {
            binary_score += 1;
        }
    }

    if (is_all_ascii(date[0..4])) {
        binary_score -= 4;
    }

    if (is_all_digits(id666_main_pt2[11..14])) {
        binary_score -= 1;
    }
    else if (is_all_digits(id666_main_pt2[11..13]) and id666_main_pt2[13] == 0) {
        binary_score -= 1;
    }
    else if (is_all_digits(id666_main_pt2[11..12]) and std.mem.allEqual(u8, id666_main_pt2[12..13], 0)) {
        binary_score -= 1;
    }
    else {
        binary_score += 6;
    }

    if (is_all_digits(id666_main_pt2[14..19])) {
        binary_score -= 4;
    }
    else if (is_all_digits(id666_main_pt2[14..18]) and id666_main_pt2[18] == 0) {
        binary_score -= 3;
    }
    else if (is_all_digits(id666_main_pt2[14..17]) and std.mem.allEqual(u8, id666_main_pt2[17..18], 0)) {
        binary_score -= 2;
    }
    else if (is_all_digits(id666_main_pt2[14..16]) and std.mem.allEqual(u8, id666_main_pt2[16..18], 0)) {
        binary_score -= 1;
    }
    else if (is_all_digits(id666_main_pt2[14..15]) and std.mem.allEqual(u8, id666_main_pt2[15..18], 0)) {
        binary_score -= 1;
    }
    else {
        binary_score += 6;
    }

    if (id666_main_pt2[0xD2 - 0x9E] != 0x00) {
        binary_score -= 2;
    }
    else {
        binary_score += 10;
    }

    const emu_id = id666_main_pt2[(0xD2 - 0x9E)..(0xD3 - 0x9E)];
    if (is_all_digits(emu_id)) {
        binary_score -= 4;
    }
    else {
        binary_score += 3;
    }

    return
        if (binary_score >= 0)
            ID666Fmt.binary
        else
            ID666Fmt.text;
}

fn is_all_ascii(slc: []const u8) bool {
    for (slc) |c| {
        if (c < 0x20 or c >= 0x7F) {
            return false;
        }
    }

    return true;
}

fn is_all_digits(slc: []const u8) bool {
    for (slc) |c| {
        if (c < '0' or c > '9') {
            return false;
        }
    }

    return true;
}

fn extract_date(date: []const u8) !? struct { month: u32, day: u32, year: u32 } {
    if (!is_all_digits(date[0..2]) or !is_all_digits(date[3..5]) or !is_all_digits(date[6..10])) {
        return null;
    }

    if (date[3] != '/' and date[6] != '/') {
        return null;
    }

    const mm   = try std.fmt.parseInt(u32, date[0..2],  10);
    const dd   = try std.fmt.parseInt(u32, date[3..5],  10);
    const yyyy = try std.fmt.parseInt(u32, date[6..10], 10);

    return .{
        .month = mm,
        .day   = dd,
        .year  = yyyy
    };
}