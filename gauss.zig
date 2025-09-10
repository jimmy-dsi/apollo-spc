const DSPStateInternal = @import("dsp_state_int.zig").DSPStateInternal;

pub fn interpolate(s: *const DSPStateInternal, v_idx: u3, tbl: [*]const u16) i16 {
    const v = &s._voice[v_idx];

    // Make accesses into gaussian table based on fractional position between samples
    const g_idx: u9 = @intCast(v._gaussian_offset >> 4 & 0x00FF);

    const sample_offset: u5 = @intCast(v._gaussian_offset >> 12);
    const offset: u4 = @intCast((@as(u5, v._buffer_offset) + sample_offset) % 12);

    // Perform gaussian interpolation algorithm on offset[0:4] samples
    var output: i32 = undefined;
    output  = @intCast(@as(i32, tbl[0x0FF - g_idx]) * @as(i32, v._buffer[ offset          ]) >> 11);
    output += @intCast(@as(i32, tbl[0x1FF - g_idx]) * @as(i32, v._buffer[(offset + 1) % 12]) >> 11);
    output += @intCast(@as(i32, tbl[0x100 + g_idx]) * @as(i32, v._buffer[(offset + 2) % 12]) >> 11);
    output  = output & 0xFFFF; // Partial overflow handling after 2nd addition
    output += @intCast(@as(i32, tbl[0x000 + g_idx]) * @as(i32, v._buffer[(offset + 3) % 12]) >> 11);

    // Clamp result to -8000..=7FFF
    if (output >= 0x8000) {
        output = 0x7FFF;
    }
    else if (output < -0x8000) {
        output = -0x8000;
    }

    // Zero out LSB as Ares does - However, FullSNES says result should be SAR'd by 1. The perceived bit depth is the same either way
    return @intCast(output & 0xFFFE);
}