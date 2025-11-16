const std = @import("std");

const Emu = @import("emu.zig").Emu;
const SongMetadata = @import("song_metadata.zig").SongMetadata;

pub const SPCLoadError = error {
    MissingFileHeader,
    SizeTooShort
};

pub fn load_spc(emu: *Emu, spc_file_data: []const u8) SPCLoadError! SongMetadata {
    if (spc_file_data.len < 0x10180) {
        return SPCLoadError.SizeTooShort;
    }

    const file_header: []const u8 = spc_file_data[0..33];
    if (!std.mem.eql(u8, file_header[0..28], "SNES-SPC700 Sound File Data ")) {
        return SPCLoadError.MissingFileHeader;
    }

    // For the sake of leniency, the only validation of the version portion will be that all characters are printable ASCII chars
    const version_string: []const u8 = file_header[28..33];
    for (version_string) |char| {
        if (char < 0x20 or char >= 0x7F) {
            return SPCLoadError.MissingFileHeader;
        }
    }

    // At this point, we should have a valid SPC file - continue to load metadata, reset state and load data into emulator
    const has_id666 = spc_file_data[0x23] == 0x1A;

    const metadata = SongMetadata.new(
        has_id666,
        spc_file_data[0x2E..0x100],
        if (spc_file_data.len >= 0x10208)
            spc_file_data[0x10200..]
        else
            spc_file_data[0..0]
    );

    // Reset SMP and DSP state to power on
    emu.s_dsp.power_on();
    emu.s_smp.power_on();

    // Load CPU registers
    const cpu_regs = spc_file_data[0x25..0x2C];
    emu.s_smp.spc.state.pc  = read_u16_le(cpu_regs, 0);
    emu.s_smp.spc.state.a   = cpu_regs[2];
    emu.s_smp.spc.state.x   = cpu_regs[3];
    emu.s_smp.spc.state.y   = cpu_regs[4];
    emu.s_smp.spc.state.psw = cpu_regs[5];
    emu.s_smp.spc.state.sp  = cpu_regs[6];

    // Load ARAM
    const aram = spc_file_data[0x00100..0x10100];
    @memcpy(emu.s_dsp.audio_ram[0..], aram);

    // Initialize needed SMP MMIO registers
    for (0x00F1..0x0100) |addr| {
        const a: u16 = @intCast(addr);
        if (a != 0x00F3) { // Skip DSP data (will be covered by loading in DSP map)
            emu.s_smp.debug_write_io(a, aram[a]);
        }
    }

    // Initialize DSP registers
    const dsp_map = spc_file_data[0x10100..0x10180];
    for (dsp_map, 0..) |data, i| {
        const ii: u8 = @intCast(i);
        const dd: u8 = @intCast(data);
        emu.s_dsp.debug_write(ii, dd);
    }

    // If no extra RAM section, then we're done
    if (spc_file_data.len < 0x10200) {
        return metadata;
    }

    // If extra RAM section is just the bootrom data, then we're done
    const extra_ram = spc_file_data[0x101C0..0x10200];
    if (std.mem.eql(u8, extra_ram, emu.s_smp.boot_rom)) {
        return metadata;
    }

    // Load extra RAM section if viable
    const bootrom_sect = aram[0xFFC0..];

    const use_extra_ram =
        std.mem.eql(u8, bootrom_sect, emu.s_smp.boot_rom)
        or std.mem.allEqual(u8, bootrom_sect, 0x00)
        or std.mem.allEqual(u8, bootrom_sect, 0xFF)
        or std.mem.allEqual(u8, bootrom_sect, 0x55)
        or std.mem.allEqual(u8, bootrom_sect, 0xAA)
        or std.mem.allEqual(u8, bootrom_sect, 0x5A)
        or std.mem.allEqual(u8, bootrom_sect, 0xA5);

    if (use_extra_ram) {
        @memcpy(emu.s_dsp.audio_ram[0xFFC0..], extra_ram);
    }

    // Set previous state to reflect loaded data
    emu.s_smp.prev_spc_state = emu.s_smp.spc.state;

    return metadata;
}

pub fn read_u16_le(buffer: []const u8, start: u32) u16 {
    return @as(u16, buffer[start]) | @as(u16, buffer[start + 1]) << 8;
}

pub fn read_u24_le(buffer: []const u8, start: u32) u24 {
    return @as(u24, buffer[start]) | @as(u24, buffer[start + 1]) << 8 | @as(u24, buffer[start + 2]) << 16;
}

pub fn read_u32_le(buffer: []const u8, start: u32) u32 {
    return @as(u32, buffer[start]) | @as(u32, buffer[start + 1]) << 8 | @as(u32, buffer[start + 2]) << 16 | @as(u32, buffer[start + 3]) << 24;
}