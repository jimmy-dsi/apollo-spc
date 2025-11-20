const std = @import("std");

const Emu = @import("core/emu.zig").Emu;
const SongMetadata = @import("core/song_metadata.zig").SongMetadata;

const spcl = @import("core/spc_loader.zig");

const main = @import("main.zig");
const emu  = @import("emu.zig");

pub var file_metadata: ?SongMetadata = null;

pub inline fn load_spc(spc_file_data: []const u8, emu_ptr: ?*Emu) !void {
    const ep = emu.get_ptr(emu_ptr);
    try main.validate_ptr(Emu, ep);

    file_metadata = try spcl.load_spc(ep.?, spc_file_data);
}