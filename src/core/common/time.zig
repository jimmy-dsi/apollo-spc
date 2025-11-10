const std = @import("std");

pub fn fmt_time(ms: u64, buf: []u8) ?[]const u8 {
    const total_s = @divFloor(ms, 1000);
    const total_m = @divFloor(total_s, 60);
    const total_h = @divFloor(total_m, 60);

    const rem_ms = ms % 1000;
    const rem_s  = total_s % 60;
    const rem_m  = total_m % 60;

    const v: ?[]const u8 = std.fmt.bufPrint(buf, "{d:0>2}:{d:0>2}:{d:0>2}.{d:0>3}", .{total_h, rem_m, rem_s, rem_ms}) catch null;
    return v;
}