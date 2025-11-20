const std = @import("std");

const main = @import("main.zig");

pub inline fn create(num_bytes: u32) ![]u8 {
    try main.validate();
    return main.alloc.alloc(u8, num_bytes);
}

pub inline fn destroy(buffer: []u8) !void {
    try main.validate();
    main.alloc.free(buffer);
}