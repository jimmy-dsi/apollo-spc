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
    ost_track: ?[2] u8 = null,

    publisher: ?[257] u8 = null,
    copyright_year: ?u32 = null,

    intro_length_in_timer2_steps: ?u32 = null,
    loop_length_in_timer2_steps:  ?u32 = null,
    end_length_in_timer2_steps:   ?u32 = null,
    loop_times:                   ?u8  = null,

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

                    r.length_in_seconds = parse_int_null_term(id666_main[0x7B..0x7E]);
                    r.fade_length_in_ms = parse_int_null_term(id666_main[0x7E..0x83]);

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

        if (id666_ext.len < 8) {
            return r;
        }

        if (!std.mem.eql(u8, id666_ext[0..4], "xid6")) {
            return r;
        }

        const chunk_len = read_u32_le(id666_ext[4..8], 0);
        if (id666_ext.len < 8 + chunk_len) {
            return r;
        }

        const chunk = id666_ext[8..(8 + chunk_len)];
        //const chunk_end = (chunk_len + 3) / 4 * 4; // Round up to next multiple of 4

        var remainder = chunk[0..];
        while (remainder.len > 0) {
            const header = read_4(&remainder);

            const id   = header[0];
            const typ  = header[1];
            const data = read_u16_le(header[2..], 0);

            if (typ != 0) {
                const length:     u8 = @intCast(data & 0xFF);
                const num_dwords: u8 = @intCast((@as(u16, length) + 3) / 4); // Round up to next multiple of 4

                var valid = false;
                var str_buffer: ?[]u8 = null;

                for (0..num_dwords) |i| {
                    const dword = read_4(&remainder);
                    switch (id) {
                        0x01 => { // Song Name
                            if (typ == 1) { // Must be a string
                                valid = true;
                                if (r.title == null) {
                                    r.title = [_]u8 {0x00} ** 257;
                                }
                                str_buffer = r.title.?[0..];
                                @memcpy(r.title.?[(i * 4)..(i * 4 + 4)], dword[0..]);
                            }
                        },
                        0x02 => { // Game Name
                            if (typ == 1) { // Must be a string
                                valid = true;
                                if (r.game == null) {
                                    r.game = [_]u8 {0x00} ** 257;
                                }
                                str_buffer = r.game.?[0..]; 
                                @memcpy(r.game.?[(i * 4)..(i * 4 + 4)], dword[0..]);
                            }
                        },
                        0x03 => { // Artist's Name
                            if (typ == 1) { // Must be a string
                                valid = true;
                                if (r.artist == null) {
                                    r.artist = [_]u8 {0x00} ** 257;
                                }
                                str_buffer = r.artist.?[0..];
                                @memcpy(r.artist.?[(i * 4)..(i * 4 + 4)], dword[0..]);
                            }
                        },
                        0x04 => { // Dumper's Name
                            if (typ == 1) { // Must be a string
                                valid = true;
                                if (r.dumper == null) {
                                    r.dumper = [_]u8 {0x00} ** 257;
                                }
                                str_buffer = r.dumper.?[0..];
                                @memcpy(r.dumper.?[(i * 4)..(i * 4 + 4)], dword[0..]);
                            }
                        },
                        0x05 => { // Date Song was Dumped
                            if (typ == 4 and length == 4) { // Must be an integer with only one dword length
                                const value = dword[0..];

                                r.month = value[1];
                                r.day   = value[0];
                                r.year  = read_u16_le(value, 2);

                                r.date_other = null;
                            }
                        },
                        0x07 => { // Comments
                            if (typ == 1) { // Must be a string
                                valid = true;
                                if (r.comments == null) {
                                    r.comments = [_]u8 {0x00} ** 257;
                                }
                                str_buffer = r.comments.?[0..];
                                @memcpy(r.comments.?[(i * 4)..(i * 4 + 4)], dword[0..]);
                            }
                        },
                        0x10 => { // Official Soundtrack Title
                            if (typ == 1) { // Must be a string
                                valid = true;
                                if (r.ost_title == null) {
                                    r.ost_title = [_]u8 {0x00} ** 257;
                                }
                                str_buffer = r.ost_title.?[0..];
                                @memcpy(r.ost_title.?[(i * 4)..(i * 4 + 4)], dword[0..]);
                            }
                        },
                        0x13 => { // Publisher's Name
                            if (typ == 1) { // Must be a string
                                valid = true;
                                if (r.publisher == null) {
                                    r.publisher = [_]u8 {0x00} ** 257;
                                }
                                str_buffer = r.publisher.?[0..];
                                @memcpy(r.publisher.?[(i * 4)..(i * 4 + 4)], dword[0..]);
                            }
                        },
                        0x30 => { // Introduction Length
                            if (typ == 4 and length == 4) { // Must be an integer with only one dword length
                                const value = dword[0..];
                                r.intro_length_in_timer2_steps = read_u32_le(value, 0);
                            }
                        },
                        0x31 => { // Loop Length
                            if (typ == 4 and length == 4) { // Must be an integer with only one dword length
                                const value = dword[0..];
                                r.loop_length_in_timer2_steps = read_u32_le(value, 0);
                            }
                        },
                        0x32 => { // End Length
                            if (typ == 4 and length == 4) { // Must be an integer with only one dword length
                                const value = dword[0..];
                                r.end_length_in_timer2_steps = read_u32_le(value, 0);
                            }
                        },
                        0x33 => { // Fade Length
                            if (typ == 4 and length == 4) { // Must be an integer with only one dword length
                                const value = dword[0..];
                                const fade_length_in_timer2_steps = read_u32_le(value, 0);
                                r.fade_length_in_ms = fade_length_in_timer2_steps / 64; // Convert from timer2 steps to milliseconds
                            }
                        },
                        else => {
                            // Do nothing if ID, type, and size don't match what they should be, or if ID is not in the list of known Extended ID666 IDs
                        }
                    }
                }

                if (valid and typ == 1) {
                    // Fill in the remainder portion of the string with zeroes if we have a valid string
                    const filled_amt: u16 = @as(u16, num_dwords) * 4;
                    for (filled_amt..257) |i| {
                        str_buffer.?[i] = 0;
                    }
                }
            }
            else {
                switch (id) {
                    0x06 => { // Emulator Used
                        r.emulator_id = @intCast(data & 0xFF);
                    },
                    0x11 => { // OST Disc
                        r.ost_disc = @intCast(data & 0xFF);
                    },
                    0x12 => { // OST Track (optional ASCII char (first byte) + track number (second byte))
                        r.ost_track = [_]u8 {0} ** 2;
                        r.ost_track.?[0] = @intCast(data & 0xFF);
                        r.ost_track.?[1] = @intCast(data >> 8);
                    },
                    0x14 => { // Copyright Year
                        r.copyright_year = @intCast(data);
                    },
                    0x34 => { // Muted Voices (a bit is set for each voice that's muted)
                        r.set_chan_disables(@intCast(data & 0xFF));
                    },
                    0x35 => { // Number of Times to Loop
                        r.loop_times = @intCast(data & 0xFF);
                    },
                    0x36 => { // Mixing (Preamp) Level
                        r.mixing_level = @intCast(data & 0xFF);
                    },
                    else => {
                        // Do nothing if ID and type don't match what they should be, or if ID is not in the list of known Extended ID666 IDs
                    }
                }
            }
        }

        return r;
    }

    pub fn print(self: *const SongMetadata, buf: []u8) ![]const u8 {
        const padding: u32 = 16;
        const max_len: u32 = 64;

        var md_copy = self.*;
        md_copy.strip_newlines();

        var fbs = std.io.fixedBufferStream(buf);
        var writer = fbs.writer();

        try writer.print("----------------------------------------------------------------------------------\n", .{});
        try print_str(writer, "Title:",       if (md_copy.title != null)    null_term(md_copy.title.?[0..])    else "\x1B[32m<none>\x1B[0m", padding, max_len);
        try print_str(writer, "Artist:",      if (md_copy.artist != null)   null_term(md_copy.artist.?[0..])   else "\x1B[32m<none>\x1B[0m", padding, max_len);
        try print_str(writer, "Game:",        if (md_copy.game != null)     null_term(md_copy.game.?[0..])     else "\x1B[32m<none>\x1B[0m", padding, max_len);
        try print_str(writer, "Dumper:",      if (md_copy.dumper != null)   null_term(md_copy.dumper.?[0..])   else "\x1B[32m<none>\x1B[0m", padding, max_len);
        try print_str(writer, "Comments:",    if (md_copy.comments != null) null_term(md_copy.comments.?[0..]) else "\x1B[32m<none>\x1B[0m", padding, max_len);

        if (self.date_other) |d| {
            try print_str(writer, "Date Dumped:", d[0..], padding, max_len);
        }
        else if (self.year != null and self.month != null and self.day != null) {
            var date_str = [_]u8 {'0', '0', '0', '0', '-', '0', '0', '-', '0', '0', 0};

            _ = try buf_print_padded(date_str[0..4],  self.year.?,  10_000);
            _ = try buf_print_padded(date_str[5..7],  self.month.?,    100);
            _ = try buf_print_padded(date_str[8..10], self.day.?,      100);

            try print_str(writer, "Date Dumped:", date_str[0..10], padding, max_len);
        }
        else {
            try print_str(writer, "Date Dumped:", "\x1B[32m<none>\x1B[0m", padding, max_len);
        }

        if (self.length_in_seconds) |slis| {
            const song_length = try buf_print_time(slis, false);
            try print_str(writer, "Song Length:", song_length[0..], padding, max_len);
        }
        else {
            try print_str(writer, "Song Length:", "\x1B[32m<none>\x1B[0m", padding, max_len);
        }

        if (self.fade_length_in_ms) |flims| {
            const fade_length = try buf_print_time(flims, true);
            try print_str(writer, "Fade Time:", fade_length[0..], padding, max_len);
        }
        else {
            try print_str(writer, "Fade Time:", "\x1B[32m<none>\x1B[0m", padding, max_len);
        }

        const chan_states: [8]u1 =
            if (self.channels_disabled == null)
                [_]u1 {0} ** 8
            else
                self.channels_disabled.?;

        var chan_buf = [_]u8 {' '} ** 128;

        for (0..8) |i| {
            const ii: u8 = @intCast(i);

            chan_buf[16 * i]     = '#';
            chan_buf[16 * i + 1] = ii + '0';
            chan_buf[16 * i + 2] = ':';

            _ = try std.fmt.bufPrint(
                chan_buf[(16 * i + 4)..(16 * i + 16)],
                "{s}",
                .{
                    if (chan_states[i] != 0)
                        "Disabled    "
                    else
                        " Enabled    "
                }
            );
        }

        try print_str(writer, "Channel States:", chan_buf[0..], padding, max_len);

        if (self.emulator_id) |emu_id| {
            var emu_buf = [_]u8 {' ', ' ', ' '};
            const emu_str = try std.fmt.bufPrint(emu_buf[0..], "{}", .{emu_id});
            try print_str(writer, "Emulator ID:", emu_str, padding, max_len);
        }
        else {
            try print_str(writer, "Emulator ID:", "\x1B[32m<none>\x1B[0m", padding, max_len);
        }

        try print_str(writer, "OST Title:", if (md_copy.ost_title != null) null_term(md_copy.ost_title.?[0..]) else "\x1B[32m<none>\x1B[0m", padding, max_len);
        if (self.ost_disc) |ost_disc| {
            var disc_buf = [_]u8 {' ', ' ', ' '};
            const disc_str = try std.fmt.bufPrint(disc_buf[0..], "{}", .{ost_disc});
            try print_str(writer, "OST Disc:", disc_str, padding, max_len);
        }
        else {
            try print_str(writer, "OST Disc:", "\x1B[32m<none>\x1B[0m", padding, max_len);
        }

        if (self.ost_track) |ost_track| {
            if (ost_track[0] >= 0x21 and ost_track[0] <= 0x7E) {
                var track_buf = [_]u8 {ost_track[0], ' ', ' ', ' '};
                _ = try std.fmt.bufPrint(track_buf[1..], "{}", .{ost_track[1]});
                try print_str(writer, "OST Track:", track_buf[0..], padding, max_len);
            }
            else {
                var track_buf = [_]u8 {' ', ' ', ' '};
                const track_str = try std.fmt.bufPrint(track_buf[0..], "{}", .{ost_track[1]});
                try print_str(writer, "OST Track:", track_str, padding, max_len);
            }
        }
        else {
            try print_str(writer, "OST Track:", "\x1B[32m<none>\x1B[0m", padding, max_len);
        }

        try print_str(writer, "Publisher:", if (md_copy.publisher != null) null_term(md_copy.publisher.?[0..]) else "\x1B[32m<none>\x1B[0m", padding, max_len);

        if (self.copyright_year) |cpr_year| {
            var cpr_buf = [_]u8 {' ', ' ', ' ', ' '};
            const cpr_str = try std.fmt.bufPrint(cpr_buf[0..], "{}", .{cpr_year});
            try print_str(writer, "Copyright Year:", cpr_str, padding, max_len);
        }
        else {
            try print_str(writer, "Copyright Year:", "\x1B[32m<none>\x1B[0m", padding, max_len);
        }

        if (self.intro_length_in_timer2_steps) |int_len| {
            const intro_length = try buf_print_time(int_len / 64, true);
            try print_str(writer, "Intro Length:", intro_length[0..], padding, max_len);
        }
        else {
            try print_str(writer, "Intro Length:", "\x1B[32m<none>\x1B[0m", padding, max_len);
        }

        if (self.loop_length_in_timer2_steps) |loop_len| {
            const loop_length = try buf_print_time(loop_len / 64, true);
            try print_str(writer, "Loop Length:", loop_length[0..], padding, max_len);
        }
        else {
            try print_str(writer, "Loop Length:", "\x1B[32m<none>\x1B[0m", padding, max_len);
        }

        if (self.end_length_in_timer2_steps) |end_len| {
            const end_length = try buf_print_time(end_len / 64, true);
            try print_str(writer, "End Length:", end_length[0..], padding, max_len);
        }
        else {
            try print_str(writer, "End Length:", "\x1B[32m<none>\x1B[0m", padding, max_len);
        }

        if (self.loop_times) |loop_times| {
            var loop_buf = [_]u8 {' ', ' ', ' '};
            const loop_str = try std.fmt.bufPrint(loop_buf[0..], "{}", .{loop_times});
            try print_str(writer, "Loop Count:", loop_str, padding, max_len);
        }
        else {
            try print_str(writer, "Loop Count:", "\x1B[32m<none>\x1B[0m", padding, max_len);
        }

        if (self.mixing_level) |mix_lvl| {
            var mix_buf = [_]u8 {' ', ' ', ' ', '/', '2', '5', '5'};
            const mix_str = try std.fmt.bufPrint(mix_buf[0..3], "{}", .{mix_lvl});
            try print_str(writer, "Mixing Level:", mix_str, padding, max_len);
        }
        else {
            try print_str(writer, "Mixing Level:", "\x1B[32m<none>\x1B[0m", padding, max_len);
        }
        
        try writer.print("----------------------------------------------------------------------------------\n", .{});
        try writer.print("\n", .{});

        const length = try fbs.getPos();
        return buf[0..@intCast(length)];
    }

    pub fn strip_newlines(self: *SongMetadata) void {
        if (self.title != null) {
            strip(self.title.?[0..]);
        }

        if (self.artist != null) {
            strip(self.artist.?[0..]);
        }

        if (self.game != null) {
            strip(self.game.?[0..]);
        }

        if (self.dumper != null) {
            strip(self.dumper.?[0..]);
        }

        if (self.comments != null) {
            strip(self.comments.?[0..]);
        }

        if (self.date_other != null) {
            strip(self.date_other.?[0..]);
        }

        if (self.ost_title != null) {
            strip(self.ost_title.?[0..]);
        }

        if (self.publisher != null) {
            strip(self.publisher.?[0..]);
        }
    }

    fn parse_int_null_term(buf: []const u8) ?u32 {
        var first_null: u8 = @intCast(buf.len);
        //std.debug.print("fn: {d}\n", .{first_null});

        for (buf, 0..) |char, i| {
            if (char == 0) {
                first_null = @intCast(i);
                break;
            }
        }

        const valid = first_null == buf.len or std.mem.allEqual(u8, buf[first_null..], 0);

        if (!valid) {
            return null;
        }

        return std.fmt.parseInt(u32, buf[0..first_null], 10) catch null;
    }

    fn strip(buf: []u8) void {
        for (buf, 0..) |char, i| {
            if (char == '\r' or char == '\n') {
                buf[i] = ' ';
            }
        }
    }

    fn at(buf: []const u8, index: u32) u8 {
        if (index >= buf.len) {
            return 0x00;
        }
        else {
            return buf[index];
        }
    }

    fn read_4(stream: *[]const u8) [4]u8 {
        const res_buf = [_]u8 {
            at(stream.*, 0),
            at(stream.*, 1),
            at(stream.*, 2),
            at(stream.*, 3),
        };

        if (stream.len < 4) {
            stream.* = stream.*[stream.len..];
        }
        else {
            stream.* = stream.*[4..];
        }

        return res_buf;
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

    fn print_str(writer: anytype, label: []const u8, str: []const u8, comptime padding: u32, max_len: u32) !void {
        const pad_str = [_]u8 {' '} ** padding;

        var remainder: u32 = @intCast(str.len);
        var start:     u32 = 0;

        var label_str = [_]u8 {' '} ** padding;
        for (label, 0..) |char, i| {
            label_str[i] = char;
        }

        try writer.print("{s}", .{label_str});

        while (remainder > max_len) {
            if (start > 0) {
                try writer.print("{s}", .{pad_str});
            }
            try writer.print("{s}\n", .{str[start..(start + max_len)]});

            remainder -= max_len;
            start     += max_len;
        }

        if (start > 0) {
            try writer.print("{s}", .{pad_str});
        }
        try writer.print("{s}\n", .{str[start..]});
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
        binary_score += 4;
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