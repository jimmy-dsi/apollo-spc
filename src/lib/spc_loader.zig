const std = @import("std");

const Emu = @import("core/emu.zig").Emu;
const SongMetadata = @import("core/song_metadata.zig").SongMetadata;

const spcl = @import("core/spc_loader.zig");

const main = @import("main.zig");
const emu  = @import("emu.zig");

pub const Error = error {
    spc_not_loaded
};

pub const Metadata = extern struct {
    is_valid: u8 = 0,

    title:    [257]u8 = [_]u8{0} ** 257,
    artist:   [257]u8 = [_]u8{0} ** 257,
    game:     [257]u8 = [_]u8{0} ** 257,
    dumper:   [257]u8 = [_]u8{0} ** 257,
    comments: [257]u8 = [_]u8{0} ** 257,

    month: i64 = -1,
    day:   i64 = -1,
    year:  i64 = -1,

    date_other: [12]u8 = [_]u8{0} ** 12,

    length_in_seconds: i64 = -1,
    fade_length_in_ms: i64 = -1,

    channels_disabled: [8]u8 = [_]u8{0} ** 8,

    emulator_id: i64 = -1,

    _has_ost_track: u8 = 0,
    ost_title: [257]u8 = [_]u8{0} ** 257,
    ost_disc:  i64 = -1,
    ost_track: [2]u8 = [2]u8 {0, 0},

    publisher: [257]u8 = [_]u8{0} ** 257,
    copyright_year: i64 = -1,

    intro_length_in_timer2_steps: i64 = -1,
    loop_length_in_timer2_steps:  i64 = -1,
    end_length_in_timer2_steps:   i64 = -1,
    loop_times:                   i64 = -1,

    mixing_level: i64 = -1,
};

pub inline fn load_spc(spc_file_data: []const u8, emu_ptr: ?*Emu) !void {
    const ep = emu.get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    ep.?.lib_metadata = try spcl.load_spc(ep.?, spc_file_data);
}

pub inline fn get_metadata(emu_ptr: ?*Emu) !Metadata {
    const ep = emu.get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    if (ep.?.lib_metadata == null) {
        return Error.spc_not_loaded;
    }

    const md = ep.?.lib_metadata.?;

    var rmd = Metadata{ .is_valid = 1 };

    if (md.title)    |t| { rmd.title    = t; }
    if (md.artist)   |a| { rmd.artist   = a; }
    if (md.game)     |g| { rmd.game     = g; }
    if (md.dumper)   |d| { rmd.dumper   = d; }
    if (md.comments) |c| { rmd.comments = c; }

    if (md.month) |m| { rmd.month = to_i64(u32, m); }
    if (md.day)   |d| { rmd.day   = to_i64(u32, d); }
    if (md.year)  |y| { rmd.year  = to_i64(u32, y); }

    if (md.date_other) |d| { rmd.date_other = d; }

    if (md.length_in_seconds) |L| { rmd.length_in_seconds = to_i64(u32, L); }
    if (md.fade_length_in_ms) |f| { rmd.fade_length_in_ms = to_i64(u32, f); }

    if (md.channels_disabled) |c| {
        for (0..8) |i| {
            rmd.channels_disabled[i] = @intCast(c[i]);
        }
    }

    if (md.emulator_id) |e| { rmd.emulator_id = to_i64(u8, e); }

    if (md.ost_title) |o| { rmd.ost_title = o; }
    if (md.ost_disc)  |o| { rmd.ost_disc  = to_i64(u8, o); }
    if (md.ost_track) |o| {
        rmd._has_ost_track = 1;
        rmd.ost_track = o;
    }

    if (md.publisher)      |p| { rmd.publisher = p; }
    if (md.copyright_year) |c| { rmd.copyright_year = to_i64(u32, c); }

    if (md.intro_length_in_timer2_steps) |i| { rmd.intro_length_in_timer2_steps = to_i64(u32, i); }
    if (md.loop_length_in_timer2_steps)  |L| { rmd.loop_length_in_timer2_steps  = to_i64(u32, L); }
    if (md.end_length_in_timer2_steps)   |e| { rmd.end_length_in_timer2_steps   = to_i64(u32, e); }
    if (md.loop_times)                   |L| { rmd.loop_times                   = to_i64(u32, L); }

    if (md.mixing_level) |m| { rmd.mixing_level = to_i64(u8, m); }

    return rmd;
}

inline fn to_i64(comptime T: type, value: T) i64 {
    const v: u64 = @intCast(value);
    return @bitCast(v);
}