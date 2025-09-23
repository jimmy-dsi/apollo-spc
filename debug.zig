const std = @import("std");

const Emu      = @import("emu.zig").Emu;
const SDSP     = @import("s_dsp.zig").SDSP;
const SSMP     = @import("s_smp.zig").SSMP;
const SMPState = @import("smp_state.zig").SMPState;
const SPC      = @import("spc.zig").SPC;
const SPCState = @import("spc_state.zig").SPCState;

pub fn print_pc(emu: *Emu) void {
    const state = emu.s_smp.spc.state;
    const pc = state.pc;
    std.debug.print("{X:0>4}", .{pc});
}

pub fn print_spc_state(emu: *Emu) void {
    const state = emu.s_smp.spc.state;

    const a  = state.a;
    const x  = state.x;
    const y  = state.y;
    const sp = state.sp;
    const pc = state.pc;

    const n: u8 = if (state.n() == 1) 'N' else 'n';
    const v: u8 = if (state.v() == 1) 'V' else 'v';
    const p: u8 = if (state.p() == 1) 'P' else 'p';
    const b: u8 = if (state.b() == 1) 'B' else 'b';
    const h: u8 = if (state.h() == 1) 'H' else 'h';
    const i: u8 = if (state.i() == 1) 'I' else 'i';
    const z: u8 = if (state.z() == 1) 'Z' else 'z';
    const c: u8 = if (state.c() == 1) 'C' else 'c';

    std.debug.print(
        "A:{X:0>2} X:{X:0>2} Y:{X:0>2} SP:{X:0>2} PC:{X:0>4} {c}{c}{c}{c}{c}{c}{c}{c}",
        .{
            a, x, y, sp, pc,
            n, v, p, b, h, i, z, c
        }
    );
}

pub fn print_opcode(emu: *Emu) void {
    const s_smp = &emu.s_smp;

    const pc = s_smp.spc.pc();

    const opc =
        switch (emu.s_smp.spc.mode()) {
            SPCState.Mode.normal    => s_smp.debug_read_data(pc),
            SPCState.Mode.asleep    => 0xEF,
            SPCState.Mode.stopped   => 0xFF,
            SPCState.Mode.interrupt => null,
        };

    const opcode: u8 = opc orelse 0x00;

    const operand_1 = s_smp.debug_read_data(pc +% 1);
    const operand_2 = s_smp.debug_read_data(pc +% 2);

    var operand_count: u32 = 0;

    if (opc != null) {
        switch (opcode) {
            0x00 => {
                std.debug.print("nop              ", .{});
            },
            0x01 => {
                std.debug.print("tcall 0          ", .{});
            },
            0x02 => {
                operand_count = 1;
                std.debug.print("set1 ${X:0>2}.0       ", .{operand_1});
            },
            0x03 => {
                operand_count = 2;
                const offset: i8 = @bitCast(operand_2);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 3;
                std.debug.print("bbs ${X:0>2}.0, ${X:0>4} ", .{operand_1, target_address});
            },
            0x04 => {
                operand_count = 1;
                std.debug.print("or a, ${X:0>2}        ", .{operand_1});
            },
            0x05 => {
                operand_count = 2;
                std.debug.print("or a, ${X:0>4}      ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0x06 => {
                std.debug.print("or a, (x)        ", .{});
            },
            0x07 => {
                operand_count = 1;
                std.debug.print("or a, [${X:0>2}+x]    ", .{operand_1});
            },
            0x08 => {
                operand_count = 1;
                std.debug.print("or a, #${X:0>2}       ", .{operand_1});
            },
            0x09 => {
                operand_count = 2;
                std.debug.print("or ${X:0>2}, ${X:0>2}      ", .{operand_2, operand_1});
            },
            0x0A => {
                operand_count = 2;
                const addr: u16 = operand_1 | @as(u16, operand_2) << 8;
                std.debug.print("or1 c, ${X:0>4}.{d}   ", .{addr & 0x1FFF, addr >> 13});
            },
            0x0B => {
                operand_count = 1;
                std.debug.print("asl ${X:0>2}          ", .{operand_1});
            },
            0x0C => {
                operand_count = 2;
                const addr: u16 = operand_1 | @as(u16, operand_2) << 8;
                std.debug.print("asl ${X:0>4}        ", .{addr});
            },
            0x0D => {
                std.debug.print("push psw         ", .{});
            },
            0x0E => {
                operand_count = 2;
                const addr: u16 = operand_1 | @as(u16, operand_2) << 8;
                std.debug.print("tset1 ${X:0>4}      ", .{addr});
            },
            0x0F => {
                std.debug.print("brk              ", .{});
            },
            0x10 => {
                operand_count = 1;
                const offset: i8 = @bitCast(operand_1);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 2;
                std.debug.print("bpl ${X:0>4}        ", .{target_address});
            },
            0x11 => {
                std.debug.print("tcall 1          ", .{});
            },
            0x12 => {
                operand_count = 1;
                std.debug.print("clr1 ${X:0>2}.0       ", .{operand_1});
            },
            0x13 => {
                operand_count = 2;
                const offset: i8 = @bitCast(operand_2);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 3;
                std.debug.print("bbc ${X:0>2}.0, ${X:0>4} ", .{operand_1, target_address});
            },
            0x14 => {
                operand_count = 1;
                std.debug.print("or a, ${X:0>2}+x      ", .{operand_1});
            },
            0x15 => {
                operand_count = 2;
                std.debug.print("or a, ${X:0>4}+x    ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0x16 => {
                operand_count = 2;
                std.debug.print("or a, ${X:0>4}+y    ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0x17 => {
                operand_count = 1;
                std.debug.print("or a, [${X:0>2}]+y    ", .{operand_1});
            },
            0x18 => {
                operand_count = 2;
                std.debug.print("or ${X:0>2}, #${X:0>2}     ", .{operand_2, operand_1});
            },
            0x19 => {
                std.debug.print("or (x), (y)      ", .{});
            },
            0x1A => {
                operand_count = 1;
                std.debug.print("decw ${X:0>2}         ", .{operand_1});
            },
            0x1B => {
                operand_count = 1;
                std.debug.print("asl ${X:0>2}+x        ", .{operand_1});
            },
            0x1C => {
                std.debug.print("asl a            ", .{});
            },
            0x1D => {
                std.debug.print("dec x            ", .{});
            },
            0x1E => {
                operand_count = 2;
                std.debug.print("cmp x, ${X:0>4}     ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0x1F => {
                operand_count = 2;
                std.debug.print("jmp [${X:0>4}+x]    ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0x20 => {
                std.debug.print("clrp             ", .{});
            },
            0x21 => {
                std.debug.print("tcall 2          ", .{});
            },
            0x22 => {
                operand_count = 1;
                std.debug.print("set1 ${X:0>2}.1       ", .{operand_1});
            },
            0x23 => {
                operand_count = 2;
                const offset: i8 = @bitCast(operand_2);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 3;
                std.debug.print("bbs ${X:0>2}.1, ${X:0>4} ", .{operand_1, target_address});
            },
            0x24 => {
                operand_count = 1;
                std.debug.print("and a, ${X:0>2}       ", .{operand_1});
            },
            0x25 => {
                operand_count = 2;
                std.debug.print("and a, ${X:0>4}     ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0x26 => {
                std.debug.print("and a, (x)       ", .{});
            },
            0x27 => {
                operand_count = 1;
                std.debug.print("and a, [${X:0>2}+x]   ", .{operand_1});
            },
            0x28 => {
                operand_count = 1;
                std.debug.print("and a, #${X:0>2}      ", .{operand_1});
            },
            0x29 => {
                operand_count = 2;
                std.debug.print("and ${X:0>2}, ${X:0>2}     ", .{operand_2, operand_1});
            },
            0x2A => {
                operand_count = 2;
                const addr: u16 = operand_1 | @as(u16, operand_2) << 8;
                std.debug.print("or1 c, /${X:0>4}.{d}  ", .{addr & 0x1FFF, addr >> 13});
            },
            0x2B => {
                operand_count = 1;
                std.debug.print("rol ${X:0>2}          ", .{operand_1});
            },
            0x2C => {
                operand_count = 2;
                const addr: u16 = operand_1 | @as(u16, operand_2) << 8;
                std.debug.print("rol ${X:0>4}        ", .{addr});
            },
            0x2D => {
                std.debug.print("push a           ", .{});
            },
            0x2E => {
                operand_count = 2;
                const offset: i8 = @bitCast(operand_2);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 3;
                std.debug.print("cbne ${X:0>2}, ${X:0>4}  ", .{operand_1, target_address});
            },
            0x2F => {
                operand_count = 1;
                const offset: i8 = @bitCast(operand_1);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 2;
                std.debug.print("bra ${X:0>4}        ", .{target_address});
            },
            0x30 => {
                operand_count = 1;
                const offset: i8 = @bitCast(operand_1);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 2;
                std.debug.print("bmi ${X:0>4}        ", .{target_address});
            },
            0x31 => {
                std.debug.print("tcall 3          ", .{});
            },
            0x32 => {
                operand_count = 1;
                std.debug.print("clr1 ${X:0>2}.1       ", .{operand_1});
            },
            0x33 => {
                operand_count = 2;
                const offset: i8 = @bitCast(operand_2);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 3;
                std.debug.print("bbc ${X:0>2}.1, ${X:0>4} ", .{operand_1, target_address});
            },
            0x34 => {
                operand_count = 1;
                std.debug.print("and a, ${X:0>2}+x     ", .{operand_1});
            },
            0x35 => {
                operand_count = 2;
                std.debug.print("and a, ${X:0>4}+x   ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0x36 => {
                operand_count = 2;
                std.debug.print("and a, ${X:0>4}+y   ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0x37 => {
                operand_count = 1;
                std.debug.print("and a, [${X:0>2}]+y   ", .{operand_1});
            },
            0x38 => {
                operand_count = 2;
                std.debug.print("and ${X:0>2}, #${X:0>2}    ", .{operand_2, operand_1});
            },
            0x39 => {
                std.debug.print("and (x), (y)     ", .{});
            },
            0x3A => {
                operand_count = 1;
                std.debug.print("incw ${X:0>2}         ", .{operand_1});
            },
            0x3B => {
                operand_count = 1;
                std.debug.print("rol ${X:0>2}+x        ", .{operand_1});
            },
            0x3C => {
                std.debug.print("rol a            ", .{});
            },
            0x3D => {
                std.debug.print("inc x            ", .{});
            },
            0x3E => {
                operand_count = 1;
                std.debug.print("cmp x, ${X:0>2}       ", .{operand_1});
            },
            0x3F => {
                operand_count = 2;
                std.debug.print("call ${X:0>4}       ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0x40 => {
                std.debug.print("setp             ", .{});
            },
            0x41 => {
                std.debug.print("tcall 4          ", .{});
            },
            0x42 => {
                operand_count = 1;
                std.debug.print("set1 ${X:0>2}.2       ", .{operand_1});
            },
            0x43 => {
                operand_count = 2;
                const offset: i8 = @bitCast(operand_2);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 3;
                std.debug.print("bbs ${X:0>2}.2, ${X:0>4} ", .{operand_1, target_address});
            },
            0x44 => {
                operand_count = 1;
                std.debug.print("eor a, ${X:0>2}       ", .{operand_1});
            },
            0x45 => {
                operand_count = 2;
                std.debug.print("eor a, ${X:0>4}     ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0x46 => {
                std.debug.print("eor a, (x)       ", .{});
            },
            0x47 => {
                operand_count = 1;
                std.debug.print("eor a, [${X:0>2}+x]   ", .{operand_1});
            },
            0x48 => {
                operand_count = 1;
                std.debug.print("eor a, #${X:0>2}      ", .{operand_1});
            },
            0x49 => {
                operand_count = 2;
                std.debug.print("eor ${X:0>2}, ${X:0>2}     ", .{operand_2, operand_1});
            },
            0x4A => {
                operand_count = 2;
                const addr: u16 = operand_1 | @as(u16, operand_2) << 8;
                std.debug.print("and1 c, ${X:0>4}.{d}  ", .{addr & 0x1FFF, addr >> 13});
            },
            0x4B => {
                operand_count = 1;
                std.debug.print("lsr ${X:0>2}          ", .{operand_1});
            },
            0x4C => {
                operand_count = 2;
                const addr: u16 = operand_1 | @as(u16, operand_2) << 8;
                std.debug.print("lsr ${X:0>4}        ", .{addr});
            },
            0x4D => {
                std.debug.print("push x           ", .{});
            },
            0x4E => {
                operand_count = 2;
                const addr: u16 = operand_1 | @as(u16, operand_2) << 8;
                std.debug.print("tclr1 ${X:0>4}      ", .{addr});
            },
            0x4F => {
                operand_count = 1;
                std.debug.print("pcall ${X:0>2}        ", .{operand_1});
            },
            0x50 => {
                operand_count = 1;
                const offset: i8 = @bitCast(operand_1);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 2;
                std.debug.print("bvc ${X:0>4}        ", .{target_address});
            },
            0x51 => {
                std.debug.print("tcall 5          ", .{});
            },
            0x52 => {
                operand_count = 1;
                std.debug.print("clr1 ${X:0>2}.2       ", .{operand_1});
            },
            0x53 => {
                operand_count = 2;
                const offset: i8 = @bitCast(operand_2);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 3;
                std.debug.print("bbc ${X:0>2}.2, ${X:0>4} ", .{operand_1, target_address});
            },
            0x54 => {
                operand_count = 1;
                std.debug.print("eor a, ${X:0>2}+x     ", .{operand_1});
            },
            0x55 => {
                operand_count = 2;
                std.debug.print("eor a, ${X:0>4}+x   ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0x56 => {
                operand_count = 2;
                std.debug.print("eor a, ${X:0>4}+y   ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0x57 => {
                operand_count = 1;
                std.debug.print("eor a, [${X:0>2}]+y   ", .{operand_1});
            },
            0x58 => {
                operand_count = 2;
                std.debug.print("eor ${X:0>2}, #${X:0>2}    ", .{operand_2, operand_1});
            },
            0x59 => {
                std.debug.print("eor (x), (y)     ", .{});
            },
            0x5A => {
                operand_count = 1;
                std.debug.print("cmpw ya, ${X:0>2}     ", .{operand_1});
            },
            0x5B => {
                operand_count = 1;
                std.debug.print("lsr ${X:0>2}+x        ", .{operand_1});
            },
            0x5C => {
                std.debug.print("lsr a            ", .{});
            },
            0x5D => {
                std.debug.print("mov x, a         ", .{});
            },
            0x5E => {
                operand_count = 2;
                std.debug.print("cmp y, ${X:0>4}     ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0x5F => {
                operand_count = 2;
                std.debug.print("jmp ${X:0>4}        ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0x60 => {
                std.debug.print("clrc             ", .{});
            },
            0x61 => {
                std.debug.print("tcall 6          ", .{});
            },
            0x62 => {
                operand_count = 1;
                std.debug.print("set1 ${X:0>2}.3       ", .{operand_1});
            },
            0x63 => {
                operand_count = 2;
                const offset: i8 = @bitCast(operand_2);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 3;
                std.debug.print("bbs ${X:0>2}.3, ${X:0>4} ", .{operand_1, target_address});
            },
            0x64 => {
                operand_count = 1;
                std.debug.print("cmp a, ${X:0>2}       ", .{operand_1});
            },
            0x65 => {
                operand_count = 2;
                std.debug.print("cmp a, ${X:0>4}     ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0x66 => {
                std.debug.print("cmp a, (x)       ", .{});
            },
            0x67 => {
                operand_count = 1;
                std.debug.print("cmp a, [${X:0>2}+x]   ", .{operand_1});
            },
            0x68 => {
                operand_count = 1;
                std.debug.print("cmp a, #${X:0>2}      ", .{operand_1});
            },
            0x69 => {
                operand_count = 2;
                std.debug.print("cmp ${X:0>2}, ${X:0>2}     ", .{operand_2, operand_1});
            },
            0x6A => {
                operand_count = 2;
                const addr: u16 = operand_1 | @as(u16, operand_2) << 8;
                std.debug.print("and1 c, /${X:0>4}.{d} ", .{addr & 0x1FFF, addr >> 13});
            },
            0x6B => {
                operand_count = 1;
                std.debug.print("ror ${X:0>2}          ", .{operand_1});
            },
            0x6C => {
                operand_count = 2;
                const addr: u16 = operand_1 | @as(u16, operand_2) << 8;
                std.debug.print("ror ${X:0>4}        ", .{addr});
            },
            0x6D => {
                std.debug.print("push y           ", .{});
            },
            0x6E => {
                operand_count = 2;
                const offset: i8 = @bitCast(operand_2);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 3;
                std.debug.print("dbnz ${X:0>2}, ${X:0>4}  ", .{operand_1, target_address});
            },
            0x6F => {
                std.debug.print("ret              ", .{});
            },
            0x70 => {
                operand_count = 1;
                const offset: i8 = @bitCast(operand_1);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 2;
                std.debug.print("bvc ${X:0>4}        ", .{target_address});
            },
            0x71 => {
                std.debug.print("tcall 7          ", .{});
            },
            0x72 => {
                operand_count = 1;
                std.debug.print("clr1 ${X:0>2}.3       ", .{operand_1});
            },
            0x73 => {
                operand_count = 2;
                const offset: i8 = @bitCast(operand_2);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 3;
                std.debug.print("bbc ${X:0>2}.3, ${X:0>4} ", .{operand_1, target_address});
            },
            0x74 => {
                operand_count = 1;
                std.debug.print("cmp a, ${X:0>2}+x     ", .{operand_1});
            },
            0x75 => {
                operand_count = 2;
                std.debug.print("cmp a, ${X:0>4}+x   ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0x76 => {
                operand_count = 2;
                std.debug.print("cmp a, ${X:0>4}+y   ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0x77 => {
                operand_count = 1;
                std.debug.print("cmp a, [${X:0>2}]+y   ", .{operand_1});
            },
            0x78 => {
                operand_count = 2;
                std.debug.print("cmp ${X:0>2}, #${X:0>2}    ", .{operand_2, operand_1});
            },
            0x79 => {
                std.debug.print("cmp (x), (y)     ", .{});
            },
            0x7A => {
                operand_count = 1;
                std.debug.print("addw ya, ${X:0>2}     ", .{operand_1});
            },
            0x7B => {
                operand_count = 1;
                std.debug.print("ror ${X:0>2}+x        ", .{operand_1});
            },
            0x7C => {
                std.debug.print("ror a            ", .{});
            },
            0x7D => {
                std.debug.print("mov a, x         ", .{});
            },
            0x7E => {
                operand_count = 1;
                std.debug.print("cmp y, ${X:0>2}       ", .{operand_1});
            },
            0x7F => {
                std.debug.print("reti             ", .{});
            },
            0x80 => {
                std.debug.print("setc             ", .{});
            },
            0x81 => {
                std.debug.print("tcall 6          ", .{});
            },
            0x82 => {
                operand_count = 1;
                std.debug.print("set1 ${X:0>2}.4       ", .{operand_1});
            },
            0x83 => {
                operand_count = 2;
                const offset: i8 = @bitCast(operand_2);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 3;
                std.debug.print("bbs ${X:0>2}.4, ${X:0>4} ", .{operand_1, target_address});
            },
            0x84 => {
                operand_count = 1;
                std.debug.print("adc a, ${X:0>2}       ", .{operand_1});
            },
            0x85 => {
                operand_count = 2;
                std.debug.print("adc a, ${X:0>4}     ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0x86 => {
                std.debug.print("adc a, (x)       ", .{});
            },
            0x87 => {
                operand_count = 1;
                std.debug.print("adc a, [${X:0>2}+x]   ", .{operand_1});
            },
            0x88 => {
                operand_count = 1;
                std.debug.print("adc a, #${X:0>2}      ", .{operand_1});
            },
            0x89 => {
                operand_count = 2;
                std.debug.print("adc ${X:0>2}, ${X:0>2}     ", .{operand_2, operand_1});
            },
            0x8A => {
                operand_count = 2;
                const addr: u16 = operand_1 | @as(u16, operand_2) << 8;
                std.debug.print("eor1 c, ${X:0>4}.{d}  ", .{addr & 0x1FFF, addr >> 13});
            },
            0x8B => {
                operand_count = 1;
                std.debug.print("dec ${X:0>2}          ", .{operand_1});
            },
            0x8C => {
                operand_count = 2;
                const addr: u16 = operand_1 | @as(u16, operand_2) << 8;
                std.debug.print("dec ${X:0>4}        ", .{addr});
            },
            0x8D => {
                operand_count = 1;
                std.debug.print("mov y, #${X:0>2}      ", .{operand_1});
            },
            0x8E => {
                std.debug.print("pop psw          ", .{});
            },
            0x8F => {
                operand_count = 2;
                std.debug.print("mov ${X:0>2}, #${X:0>2}    ", .{operand_2, operand_1});
            },
            0x90 => {
                operand_count = 1;
                const offset: i8 = @bitCast(operand_1);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 2;
                std.debug.print("bvc ${X:0>4}        ", .{target_address});
            },
            0x91 => {
                std.debug.print("tcall 9          ", .{});
            },
            0x92 => {
                operand_count = 1;
                std.debug.print("clr1 ${X:0>2}.4       ", .{operand_1});
            },
            0x93 => {
                operand_count = 2;
                const offset: i8 = @bitCast(operand_2);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 3;
                std.debug.print("bbc ${X:0>2}.4, ${X:0>4} ", .{operand_1, target_address});
            },
            0x94 => {
                operand_count = 1;
                std.debug.print("adc a, ${X:0>2}+x     ", .{operand_1});
            },
            0x95 => {
                operand_count = 2;
                std.debug.print("adc a, ${X:0>4}+x   ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0x96 => {
                operand_count = 2;
                std.debug.print("adc a, ${X:0>4}+y   ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0x97 => {
                operand_count = 1;
                std.debug.print("adc a, [${X:0>2}]+y   ", .{operand_1});
            },
            0x98 => {
                operand_count = 2;
                std.debug.print("adc ${X:0>2}, #${X:0>2}    ", .{operand_2, operand_1});
            },
            0x99 => {
                std.debug.print("adc (x), (y)     ", .{});
            },
            0x9A => {
                operand_count = 1;
                std.debug.print("subw ya, ${X:0>2}     ", .{operand_1});
            },
            0x9B => {
                operand_count = 1;
                std.debug.print("dec ${X:0>2}+x        ", .{operand_1});
            },
            0x9C => {
                std.debug.print("dec a            ", .{});
            },
            0x9D => {
                std.debug.print("mov x, sp        ", .{});
            },
            0x9E => {
                std.debug.print("div ya, x        ", .{});
            },
            0x9F => {
                std.debug.print("xcn a            ", .{});
            },
            0xA0 => {
                std.debug.print("ei               ", .{});
            },
            0xA1 => {
                std.debug.print("tcall 10         ", .{});
            },
            0xA2 => {
                operand_count = 1;
                std.debug.print("set1 ${X:0>2}.5       ", .{operand_1});
            },
            0xA3 => {
                operand_count = 2;
                const offset: i8 = @bitCast(operand_2);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 3;
                std.debug.print("bbs ${X:0>2}.5, ${X:0>4} ", .{operand_1, target_address});
            },
            0xA4 => {
                operand_count = 1;
                std.debug.print("sbc a, ${X:0>2}       ", .{operand_1});
            },
            0xA5 => {
                operand_count = 2;
                std.debug.print("sbc a, ${X:0>4}     ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0xA6 => {
                std.debug.print("sbc a, (x)       ", .{});
            },
            0xA7 => {
                operand_count = 1;
                std.debug.print("sbc a, [${X:0>2}+x]   ", .{operand_1});
            },
            0xA8 => {
                operand_count = 1;
                std.debug.print("sbc a, #${X:0>2}      ", .{operand_1});
            },
            0xA9 => {
                operand_count = 2;
                std.debug.print("sbc ${X:0>2}, ${X:0>2}     ", .{operand_2, operand_1});
            },
            0xAA => {
                operand_count = 2;
                const addr: u16 = operand_1 | @as(u16, operand_2) << 8;
                std.debug.print("mov1 c, ${X:0>4}.{d}  ", .{addr & 0x1FFF, addr >> 13});
            },
            0xAB => {
                operand_count = 1;
                std.debug.print("inc ${X:0>2}          ", .{operand_1});
            },
            0xAC => {
                operand_count = 2;
                const addr: u16 = operand_1 | @as(u16, operand_2) << 8;
                std.debug.print("inc ${X:0>4}        ", .{addr});
            },
            0xAD => {
                operand_count = 1;
                std.debug.print("cmp y, #${X:0>2}      ", .{operand_1});
            },
            0xAE => {
                std.debug.print("pop a            ", .{});
            },
            0xAF => {
                std.debug.print("mov (x)+, a      ", .{});
            },
            0xB0 => {
                operand_count = 1;
                const offset: i8 = @bitCast(operand_1);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 2;
                std.debug.print("bcs ${X:0>4}        ", .{target_address});
            },
            0xB1 => {
                std.debug.print("tcall 11         ", .{});
            },
            0xB2 => {
                operand_count = 1;
                std.debug.print("clr1 ${X:0>2}.5       ", .{operand_1});
            },
            0xB3 => {
                operand_count = 2;
                const offset: i8 = @bitCast(operand_2);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 3;
                std.debug.print("bbc ${X:0>2}.5, ${X:0>4} ", .{operand_1, target_address});
            },
            0xB4 => {
                operand_count = 1;
                std.debug.print("sbc a, ${X:0>2}+x     ", .{operand_1});
            },
            0xB5 => {
                operand_count = 2;
                std.debug.print("sbc a, ${X:0>4}+x   ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0xB6 => {
                operand_count = 2;
                std.debug.print("sbc a, ${X:0>4}+y   ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0xB7 => {
                operand_count = 1;
                std.debug.print("sbc a, [${X:0>2}]+y   ", .{operand_1});
            },
            0xB8 => {
                operand_count = 2;
                std.debug.print("sbc ${X:0>2}, #${X:0>2}    ", .{operand_2, operand_1});
            },
            0xB9 => {
                std.debug.print("sbc (x), (y)     ", .{});
            },
            0xBA => {
                operand_count = 1;
                std.debug.print("movw ya, ${X:0>2}     ", .{operand_1});
            },
            0xBB => {
                operand_count = 1;
                std.debug.print("inc ${X:0>2}+x        ", .{operand_1});
            },
            0xBC => {
                std.debug.print("inc a            ", .{});
            },
            0xBD => {
                std.debug.print("mov sp, x        ", .{});
            },
            0xBE => {
                std.debug.print("das a            ", .{});
            },
            0xBF => {
                std.debug.print("mov a, (x)+      ", .{});
            },
            0xC0 => {
                std.debug.print("di               ", .{});
            },
            0xC1 => {
                std.debug.print("tcall 12         ", .{});
            },
            0xC2 => {
                operand_count = 1;
                std.debug.print("set1 ${X:0>2}.6       ", .{operand_1});
            },
            0xC3 => {
                operand_count = 2;
                const offset: i8 = @bitCast(operand_2);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 3;
                std.debug.print("bbs ${X:0>2}.6, ${X:0>4} ", .{operand_1, target_address});
            },
            0xC4 => {
                operand_count = 1;
                std.debug.print("mov ${X:0>2}, a       ", .{operand_1});
            },
            0xC5 => {
                operand_count = 2;
                std.debug.print("mov ${X:0>4}, a     ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0xC6 => {
                std.debug.print("mov (x), a       ", .{});
            },
            0xC7 => {
                operand_count = 1;
                std.debug.print("mov [${X:0>2}+x], a   ", .{operand_1});
            },
            0xC8 => {
                operand_count = 1;
                std.debug.print("cmp x, #${X:0>2}      ", .{operand_1});
            },
            0xC9 => {
                operand_count = 2;
                std.debug.print("mov ${X:0>4}, x     ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0xCA => {
                operand_count = 2;
                const addr: u16 = operand_1 | @as(u16, operand_2) << 8;
                std.debug.print("mov1 ${X:0>4}.{d}, c  ", .{addr & 0x1FFF, addr >> 13});
            },
            0xCB => {
                operand_count = 1;
                std.debug.print("mov ${X:0>2}, y       ", .{operand_1});
            },
            0xCC => {
                operand_count = 2;
                std.debug.print("mov ${X:0>4}, y     ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0xCD => {
                operand_count = 1;
                std.debug.print("mov x, #${X:0>2}      ", .{operand_1});
            },
            0xCE => {
                std.debug.print("pop x            ", .{});
            },
            0xCF => {
                std.debug.print("mul ya           ", .{});
            },
            0xD0 => {
                operand_count = 1;
                const offset: i8 = @bitCast(operand_1);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 2;
                std.debug.print("bne ${X:0>4}        ", .{target_address});
            },
            0xD1 => {
                std.debug.print("tcall 13         ", .{});
            },
            0xD2 => {
                operand_count = 1;
                std.debug.print("clr1 ${X:0>2}.6       ", .{operand_1});
            },
            0xD3 => {
                operand_count = 2;
                const offset: i8 = @bitCast(operand_2);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 3;
                std.debug.print("bbc ${X:0>2}.6, ${X:0>4} ", .{operand_1, target_address});
            },
            0xD4 => {
                operand_count = 1;
                std.debug.print("mov ${X:0>2}+x, a     ", .{operand_1});
            },
            0xD5 => {
                operand_count = 2;
                std.debug.print("mov ${X:0>4}+x, a   ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0xD6 => {
                operand_count = 2;
                std.debug.print("mov ${X:0>4}+y, a   ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0xD7 => {
                operand_count = 1;
                std.debug.print("mov [${X:0>2}]+y, a   ", .{operand_1});
            },
            0xD8 => {
                operand_count = 1;
                std.debug.print("mov ${X:0>2}, x       ", .{operand_1});
            },
            0xD9 => {
                operand_count = 1;
                std.debug.print("mov ${X:0>2}+y, x     ", .{operand_1});
            },
            0xDA => {
                operand_count = 1;
                std.debug.print("movw ${X:0>2}, ya     ", .{operand_1});
            },
            0xDB => {
                operand_count = 1;
                std.debug.print("mov ${X:0>2}+x, y     ", .{operand_1});
            },
            0xDC => {
                std.debug.print("dec y            ", .{});
            },
            0xDD => {
                std.debug.print("mov a, y         ", .{});
            },
            0xDE => {
                operand_count = 2;
                const offset: i8 = @bitCast(operand_2);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 3;
                std.debug.print("cbne ${X:0>2}+x, ${X:0>4}", .{operand_1, target_address});
            },
            0xDF => {
                std.debug.print("daa a            ", .{});
            },
            0xE0 => {
                std.debug.print("clrv             ", .{});
            },
            0xE1 => {
                std.debug.print("tcall 14         ", .{});
            },
            0xE2 => {
                operand_count = 1;
                std.debug.print("set1 ${X:0>2}.7       ", .{operand_1});
            },
            0xE3 => {
                operand_count = 2;
                const offset: i8 = @bitCast(operand_2);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 3;
                std.debug.print("bbs ${X:0>2}.7, ${X:0>4} ", .{operand_1, target_address});
            },
            0xE4 => {
                operand_count = 1;
                std.debug.print("mov a, ${X:0>2}       ", .{operand_1});
            },
            0xE5 => {
                operand_count = 2;
                std.debug.print("mov a, ${X:0>4}     ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0xE6 => {
                std.debug.print("mov a, (x)       ", .{});
            },
            0xE7 => {
                operand_count = 1;
                std.debug.print("mov a, [${X:0>2}+x]   ", .{operand_1});
            },
            0xE8 => {
                operand_count = 1;
                std.debug.print("mov a, #${X:0>2}      ", .{operand_1});
            },
            0xE9 => {
                operand_count = 2;
                std.debug.print("mov x, ${X:0>4}     ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0xEA => {
                operand_count = 2;
                const addr: u16 = operand_1 | @as(u16, operand_2) << 8;
                std.debug.print("not1 ${X:0>4}.{d}     ", .{addr & 0x1FFF, addr >> 13});
            },
            0xEB => {
                operand_count = 1;
                std.debug.print("mov y, ${X:0>2}       ", .{operand_1});
            },
            0xEC => {
                operand_count = 2;
                std.debug.print("mov y, ${X:0>4}     ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0xED => {
                std.debug.print("notc             ", .{});
            },
            0xEE => {
                std.debug.print("pop y            ", .{});
            },
            0xEF => {
                std.debug.print("sleep            ", .{});
            },
            0xF0 => {
                operand_count = 1;
                const offset: i8 = @bitCast(operand_1);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 2;
                std.debug.print("beq ${X:0>4}        ", .{target_address});
            },
            0xF1 => {
                std.debug.print("tcall 15         ", .{});
            },
            0xF2 => {
                operand_count = 1;
                std.debug.print("clr1 ${X:0>2}.7       ", .{operand_1});
            },
            0xF3 => {
                operand_count = 2;
                const offset: i8 = @bitCast(operand_2);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 3;
                std.debug.print("bbc ${X:0>2}.7, ${X:0>4} ", .{operand_1, target_address});
            },
            0xF4 => {
                operand_count = 1;
                std.debug.print("mov a, ${X:0>2}+x     ", .{operand_1});
            },
            0xF5 => {
                operand_count = 2;
                std.debug.print("mov a, ${X:0>4}+x   ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0xF6 => {
                operand_count = 2;
                std.debug.print("mov a, ${X:0>4}+y   ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0xF7 => {
                operand_count = 1;
                std.debug.print("mov a, [${X:0>2}]+y   ", .{operand_1});
            },
            0xF8 => {
                operand_count = 1;
                std.debug.print("mov x, ${X:0>2}       ", .{operand_1});
            },
            0xF9 => {
                operand_count = 1;
                std.debug.print("mov x, ${X:0>2}+y     ", .{operand_1});
            },
            0xFA => {
                operand_count = 2;
                std.debug.print("mov ${X:0>2}, #${X:0>2}    ", .{operand_2, operand_1});
            },
            0xFB => {
                operand_count = 1;
                std.debug.print("mov y, ${X:0>2}+x     ", .{operand_1});
            },
            0xFC => {
                std.debug.print("inc y            ", .{});
            },
            0xFD => {
                std.debug.print("mov y, a         ", .{});
            },
            0xFE => {
                operand_count = 1;
                const offset: i8 = @bitCast(operand_1);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 2;
                std.debug.print("dbnz y, ${X:0>4}    ", .{target_address});
            },
            0xFF => {
                std.debug.print("stop             ", .{});
            },
        }
    }
    else {
        std.debug.print("-----            ", .{});
    }

    if (opc == null) {
        std.debug.print("   --      ", .{});
    }
    else {
        switch (operand_count) {
            0 => {
                std.debug.print("   {X:0>2}      ", .{opcode});
            },
            1 => {
                std.debug.print("   {X:0>2} {X:0>2}   ", .{opcode, operand_1});
            },
            2 => {
                std.debug.print("   {X:0>2} {X:0>2} {X:0>2}", .{opcode, operand_1, operand_2});
            },
            else => unreachable
        }
    }
}

pub fn print_dsp_cycle(emu: *Emu) void {
    const s_smp = &emu.s_smp;

    const prev_cycle: i64 = @intCast(s_smp.prev_exec_cycle);
    const cycle:      i64 = @intCast(s_smp.cur_exec_cycle);

    std.debug.print("Cycle: {d} -> {d} (+{d})", .{prev_cycle, cycle, cycle - prev_cycle});
}

pub fn filter_access_logs(logs_: SSMP.LogBuffer.Iter) [16]?SSMP.AccessLog {
    var ignore_cycles = [16]?u64 {
        null, null, null, null, null, null, null, null,
        null, null, null, null, null, null, null, null
    };
    var ignore_writes = [16]?u16 {
        null, null, null, null, null, null, null, null,
        null, null, null, null, null, null, null, null
    };
    var filtered = [16]?SSMP.AccessLog {
        null, null, null, null, null, null, null, null,
        null, null, null, null, null, null, null, null
    };

    var insert_index: u32 = 0;
    var write_index:  u32 = 0;
    var filter_index: u32 = 0;

    var logs = logs_;
    while (logs.step()) {
        const log = logs.value();

        const ignore =
            switch (log.type) {
                SSMP.AccessType.fetch, SSMP.AccessType.exec, SSMP.AccessType.dummy_read => true,
                else => false
            };
        if (ignore) {
            ignore_cycles[insert_index] = log.dsp_cycle;
            insert_index += 1;
            if (insert_index == 16) {
                break;
            }
        }
    }

    logs = logs_;
    while (logs.step()) {
        const log = logs.value();

        if (log.type == SSMP.AccessType.write) {
            ignore_writes[write_index] = log.address;
            write_index += 1;
            if (write_index == 16) {
                break;
            }
        }
    }

    logs = logs_;
    while (logs.step()) {
        const log = logs.value();
        var ignore = false;

        for (ignore_cycles) |cycle| {
            if (cycle == log.dsp_cycle) {
                ignore = true;
                break;
            }
        }

        for (ignore_writes) |addr| {
            if (addr == log.address and log.type == SSMP.AccessType.read) {
                ignore = true;
                break;
            }
        }

        if (!ignore) {
            filtered[filter_index] = log;
            filter_index += 1;
            if (filter_index == 16) {
                break;
            }
        }
    }

    return filtered;
}

pub fn filter_timer_logs(logs: []SSMP.TimerLog) [32]?SSMP.TimerLog {
    var filtered = [32]?SSMP.TimerLog {
        null, null, null, null, null, null, null, null,
        null, null, null, null, null, null, null, null,
        null, null, null, null, null, null, null, null,
        null, null, null, null, null, null, null, null
    };

    var index: u32 = 0;

    for (logs) |log| {
        filtered[index] = log;
        index += 1;
        if (index == 32) {
            break;
        }
    }

    return filtered;
}

const WriterType = @TypeOf(blk: {
    var bw = std.io.countingWriter(std.io.getStdOut().writer());
    const w = bw.writer();
    break :blk w;
});

pub fn print_logs(state: *const SMPState, logs: []?SSMP.AccessLog) !void {
    var buffer_writer = std.io.countingWriter(std.io.getStdOut().writer());
    var writer = buffer_writer.writer();

    var pad_length: u32 = 63;

    for (logs, 0..) |log, i| {
        if (log) |val| {
            if (i > 0) {
                _ = try writer.print(" ", .{});
            }
            const extra_bytes = try print_log(state, &val, &writer, .{.prefix = false});
            pad_length += extra_bytes;
        }
    }

    while (buffer_writer.bytes_written < pad_length) {
        _ = try writer.print(" ", .{});
    }
}

pub fn print_log(state: *const SMPState, log: *const SSMP.AccessLog, writer: *WriterType, options: struct { prefix: bool = true }) !u32 {
    if (options.prefix) {
        _ = try writer.print("[{d}]\t {s}: ", .{log.dsp_cycle, @tagName(log.type)});
    }

    switch (log.type) {
        SSMP.AccessType.none => {
            _ = try writer.print("", .{});
            return 0;
        },
        SSMP.AccessType.read => {
            if (log.address >= 0x00F0 and log.address <= 0x00FC) {
                _ = try writer.print("\x1B[33m[{X:0>4}]={X:0>2}\x1B[39m", .{log.address, log.post_data orelse unreachable});
            }
            else if (log.address >= 0x00FD and log.address <= 0x00FF) {
                _ = try writer.print("\x1B[38;2;250;125;25m[{X:0>4}]={X:0>2}\x1B[39m", .{log.address, log.post_data orelse unreachable});
                return 23;
            }
            else if (log.address >= 0xFFC0 and state.use_boot_rom == 1) {
                _ = try writer.print("\x1B[92m[{X:0>4}]={X:0>2}\x1B[39m", .{log.address, log.post_data orelse unreachable});
            }
            else if (state.ram_disable == 1) {
                _ = try writer.print("\x1B[31m[{X:0>4}]={X:0>2}\x1B[39m", .{log.address, log.post_data orelse unreachable});
            }
            else {
                _ = try writer.print("\x1B[32m[{X:0>4}]={X:0>2}\x1B[39m", .{log.address, log.post_data orelse unreachable});
            }
            return 10;
        },
        SSMP.AccessType.write => {
            if (log.address >= 0x00F0 and log.address <= 0x00FF) {
                _ = try writer.print("\x1B[95m[{X:0>4}]={X:0>2}->{X:0>2}\x1B[39m", .{log.address, log.pre_data orelse unreachable, log.post_data orelse unreachable});
            }
            else if (state.ram_disable == 1 or state.ram_write_enable == 0) {
                _ = try writer.print("\x1B[91m[{X:0>4}]={X:0>2}->{X:0>2}\x1B[39m", .{log.address, log.pre_data orelse unreachable, log.post_data orelse unreachable});
            }
            else if (log.address >= 0xFFC0 and state.use_boot_rom == 1) {
                _ = try writer.print("\x1B[94m[{X:0>4}]={X:0>2}->{X:0>2}\x1B[39m", .{log.address, log.pre_data orelse unreachable, log.post_data orelse unreachable});
            }
            else {
                _ = try writer.print("\x1B[96m[{X:0>4}]={X:0>2}->{X:0>2}\x1B[39m", .{log.address, log.pre_data orelse unreachable, log.post_data orelse unreachable});
            }
            return 10;
        },
        SSMP.AccessType.exec => {
            _ = try writer.print("[{X:0>4}]", .{log.address});
            return 0;
        },
        SSMP.AccessType.fetch => {
            _ = try writer.print("[{X:0>4}]", .{log.address});
            return 0;
        },
        SSMP.AccessType.dummy_read => {
            _ = try writer.print("[{X:0>4}]", .{log.address});
            return 0;
        }
    }

    return 0;
}

pub fn print_timer_log(log: *const SSMP.TimerLog, options: struct { prefix: bool = true }) void {
    if (options.prefix) {
        if (log.timer_number) |tn| {
            std.debug.print("[{d}]\t: [Timer{d}-{s}] ", .{log.dsp_cycle, tn, @tagName(log.type)});
        }
        else {
            std.debug.print("[{d}]\t: [TimerGlobal-{s}] ", .{log.dsp_cycle, @tagName(log.type)});
        }
    }
    
    switch (log.type) {
        SSMP.TimerLogType.read, SSMP.TimerLogType.reset => { },
        else => {
            if (log.timer_number != null) {
                std.debug.print("internal-count: {X:0>2}, output: {X:0>1}", .{log.internal_counter, log.output});
            }
        }
    }
}

const OptionStruct = struct {
    prev_pc: ?u16 = null,
    prev_state: ?*const SMPState = null,
    is_dsp: bool = false,
    logs: ?SSMP.LogBuffer.Iter = null
};

pub fn print_memory_page(emu: *Emu, page: u8, offset: u8, options: OptionStruct) void {
    const page_start: u16 = @as(u16, page) * 0x100 + offset;

    for (0..16) |y| {
        const yy: u16 = @intCast(y);
        const line_start: u16 = page_start +% 16 * yy;
        std.debug.print("{X:0>4} | ", .{line_start});

        for (0..16) |x| {
            const xx: u16 = @intCast(x);
            const address: u16 = line_start + xx;
            const data: u8 = emu.s_smp.debug_read_data(address);

            print_mem_cell(emu, address, data, false, options);
        }

        std.debug.print("| ", .{});

        for (0..16) |x| {
            const xx: u16 = @intCast(x);
            const address: u16 = line_start + xx;
            const data: u8 = emu.s_smp.debug_read_data(address);

            print_mem_cell(emu, address, data, true, options);
        }

        std.debug.print("\n", .{});
    }
}

pub fn print_dsp_map(emu: *Emu, options: OptionStruct) void {
    for (0..8) |y| {
        const yy: u8 = @intCast(y);
        const line_start: u8 = 16 * yy;
        std.debug.print("{X:0>2} | ", .{line_start});

        for (0..16) |x| {
            const xx: u8 = @intCast(x);
            const address: u8 = line_start + xx;
            const data: u8 = emu.s_dsp.dsp_map[address];

            print_mem_cell(emu, @as(u16, address), data, false, options);
        }

        std.debug.print("| ", .{});

        for (0..16) |x| {
            const xx: u8 = @intCast(x);
            const address: u8 = line_start + xx;
            const data: u8 = emu.s_dsp.dsp_map[address];

            print_mem_cell(emu, @as(u16, address), data, true, options);
        }

        std.debug.print("\n", .{});
    }
}

pub fn print_dsp_state(emu: *Emu, options: OptionStruct) void {
    const s = &emu.s_dsp.state;

    // Print voice registers
    print_dsp_voices(emu, 0, options);
    std.debug.print("\n", .{});
    print_dsp_voices(emu, 4, options);
    std.debug.print("\n", .{});

    const kon =
          @as(u8, s.voice[0].keyon)      | @as(u8, s.voice[1].keyon) << 1
        | @as(u8, s.voice[2].keyon) << 2 | @as(u8, s.voice[3].keyon) << 3
        | @as(u8, s.voice[4].keyon) << 4 | @as(u8, s.voice[5].keyon) << 5
        | @as(u8, s.voice[6].keyon) << 6 | @as(u8, s.voice[7].keyon) << 7;

    const koff =
          @as(u8, s.voice[0].keyoff)      | @as(u8, s.voice[1].keyoff) << 1
        | @as(u8, s.voice[2].keyoff) << 2 | @as(u8, s.voice[3].keyoff) << 3
        | @as(u8, s.voice[4].keyoff) << 4 | @as(u8, s.voice[5].keyoff) << 5
        | @as(u8, s.voice[6].keyoff) << 6 | @as(u8, s.voice[7].keyoff) << 7;

    const pmon =
          @as(u8, s.voice[0].pitch_mod_on)      | @as(u8, s.voice[1].pitch_mod_on) << 1
        | @as(u8, s.voice[2].pitch_mod_on) << 2 | @as(u8, s.voice[3].pitch_mod_on) << 3
        | @as(u8, s.voice[4].pitch_mod_on) << 4 | @as(u8, s.voice[5].pitch_mod_on) << 5
        | @as(u8, s.voice[6].pitch_mod_on) << 6 | @as(u8, s.voice[7].pitch_mod_on) << 7;

    const non =
          @as(u8, s.voice[0].noise_on)      | @as(u8, s.voice[1].noise_on) << 1
        | @as(u8, s.voice[2].noise_on) << 2 | @as(u8, s.voice[3].noise_on) << 3
        | @as(u8, s.voice[4].noise_on) << 4 | @as(u8, s.voice[5].noise_on) << 5
        | @as(u8, s.voice[6].noise_on) << 6 | @as(u8, s.voice[7].noise_on) << 7;

    const eon =
          @as(u8, s.voice[0].echo_on)      | @as(u8, s.voice[1].echo_on) << 1
        | @as(u8, s.voice[2].echo_on) << 2 | @as(u8, s.voice[3].echo_on) << 3
        | @as(u8, s.voice[4].echo_on) << 4 | @as(u8, s.voice[5].echo_on) << 5
        | @as(u8, s.voice[6].echo_on) << 6 | @as(u8, s.voice[7].echo_on) << 7;

    const fir = [8]u8 {
        @bitCast(s.echo.fir[0]), @bitCast(s.echo.fir[1]),
        @bitCast(s.echo.fir[2]), @bitCast(s.echo.fir[3]),
        @bitCast(s.echo.fir[4]), @bitCast(s.echo.fir[5]),
        @bitCast(s.echo.fir[6]), @bitCast(s.echo.fir[7])
    };

    // Print general registers
    const mvoll: u8 = @bitCast(s.main_vol_left);
    const mvolr: u8 = @bitCast(s.main_vol_right);
    const evoll: u8 = @bitCast(s.echo.vol_left);
    const evolr: u8 = @bitCast(s.echo.vol_right);
    const efb:   u8 = @bitCast(s.echo.feedback);

    std.debug.print("main volume - left:   {X:0>2}      ", .{mvoll});
    std.debug.print("key on:                 {X:0>2}\n",   .{kon});
    std.debug.print("main volume - right:  {X:0>2}      ", .{mvolr});
    std.debug.print("key off:                {X:0>2}\n",   .{koff});
    std.debug.print("echo volume - left:   {X:0>2}      ", .{evoll});
    std.debug.print("source end (endx):      {X:0>2}\n",   .{0x00});
    std.debug.print("echo volume - right:  {X:0>2}      ", .{evolr});
    std.debug.print("echo feedback:          {X:0>2}\n",   .{efb});
    std.debug.print("\n", .{});

    std.debug.print("pitch modulation:     {X:0>2}      ", .{pmon});
    std.debug.print("echo buffer start:      {X:0>2}00\n", .{s.echo.esa_page});
    std.debug.print("noise enable:         {X:0>2}      ", .{non});
    std.debug.print("source directory start: {X:0>2}00\n", .{s.brr_bank});
    std.debug.print("echo enable:          {X:0>2}      ", .{eon});
    std.debug.print("echo delay:             {X:0>2}\n",   .{s.echo.delay});
    std.debug.print("\n", .{});

    std.debug.print("noise clock:     {X:0>2}\n", .{s.noise_rate});
    std.debug.print("read-only echo:  {}\n",      .{s.echo.readonly == 1});
    std.debug.print("mute:            {}\n",      .{s.mute          == 1});
    std.debug.print("reset:           {}\n",      .{s.reset         == 1});
    std.debug.print("\n", .{});
    std.debug.print(
        "fir:  {X:0>2} {X:0>2} {X:0>2} {X:0>2} {X:0>2} {X:0>2} {X:0>2} {X:0>2}\n",
        .{
            fir[0], fir[1], fir[2], fir[3],
            fir[4], fir[5], fir[6], fir[7],
        }
    );
    std.debug.print("\n", .{});
}

fn print_dsp_voices(emu: *Emu, base: u3, _: OptionStruct) void {
    const s = &emu.s_dsp.state;

    // Print voice 0-3 states
    for (0..4) |i| {
        const idx = i + base;
        const v = &s.voice[idx];
        const val: u8 = @bitCast(v.vol_left);
        std.debug.print("V{d}  left volume:  {X:0>2}       ", .{idx, val});
    }
    std.debug.print("\n", .{});

    for (0..4) |i| {
        const idx = i + base;
        const v = &s.voice[idx];
        const val: u8 = @bitCast(v.vol_right);
        std.debug.print("    right volume: {X:0>2}       ", .{val});
    }
    std.debug.print("\n", .{});

    for (0..4) |i| {
        const idx = i + base;
        const v = &s.voice[idx];
        std.debug.print("    pitch:        {X:0>4}     ", .{v.pitch});
    }
    std.debug.print("\n", .{});

    for (0..4) |i| {
        const idx = i + base;
        const v = &s.voice[idx];
        std.debug.print("    srcn:         {X:0>2}       ", .{v.source});
    }
    std.debug.print("\n", .{});

    for (0..4) |i| {
        const idx = i + base;
        const v = &s.voice[idx];
        std.debug.print("    adsr 1:       {X:0>2}       ", .{v.adsr_0});
    }
    std.debug.print("\n", .{});

    for (0..4) |i| {
        const idx = i + base;
        const v = &s.voice[idx];
        std.debug.print("    adsr 2:       {X:0>2}       ", .{v.adsr_1});
    }
    std.debug.print("\n", .{});

    for (0..4) |i| {
        const idx = i + base;
        const v = &s.voice[idx];
        std.debug.print("    gain:         {X:0>2}       ", .{v.gain});
    }
    std.debug.print("\n", .{});

    for (0..4) |i| {
        const idx = i + base;
        const v = &s.voice[idx];
        std.debug.print("    envx:         {X:0>2}       ", .{v.envx});
    }
    std.debug.print("\n", .{});

    for (0..4) |i| {
        const idx = i + base;
        _ = &s.voice[idx];
        std.debug.print("    outx:         {X:0>2}       ", .{0x00});
    }
    std.debug.print("\n", .{});
}

fn print_mem_cell(emu: *Emu, address: u16, data: u8, as_char: bool, options: OptionStruct) void {
    var pc_match: bool = false;

    if (options.prev_pc) |pc| {
        if (pc == address) {
            pc_match = true;
        }
    }

    var ram_disable      = emu.s_smp.state.ram_disable;
    var ram_write_enable = emu.s_smp.state.ram_write_enable;
    var use_boot_rom     = emu.s_smp.state.use_boot_rom;

    if (options.prev_state) |state| {
        ram_disable      = state.ram_disable;
        ram_write_enable = state.ram_write_enable;
        use_boot_rom     = state.use_boot_rom;
    }

    if (pc_match and !options.is_dsp) {
        print_byte("\x1B[44m", data, "\x1B[49m", as_char);
    }
    else if (address == emu.s_smp.spc.pc() and !options.is_dsp) {
        print_byte("\x1B[45m", data, "\x1B[49m", as_char);
    }
    else {
        var is_fetch: bool = false;
        var is_read:  bool = false;
        var is_write: bool = false;

        if (options.logs) |_logs| {
            var logs = _logs;
            while (logs.step()) {
                const log = logs.value();
                if (!options.is_dsp and log.address == address or options.is_dsp and log.address == 0x00F3 and emu.s_smp.state.dsp_address == address) {
                    switch (log.type) {
                        SSMP.AccessType.dummy_read, SSMP.AccessType.exec, SSMP.AccessType.fetch => {
                            is_fetch = true;
                        },
                        SSMP.AccessType.read => {
                            is_read = true;
                        },
                        SSMP.AccessType.write => {
                            is_write = true;
                        },
                        else => {}
                    }
                }
            }
        }

        if (is_write) {
            if (options.is_dsp) {
                print_byte("\x1B[46m", data, "\x1B[49m", as_char);
            }
            else if (address >= 0x00F0 and address <= 0x00FF) {
                print_byte("\x1B[44m", data, "\x1B[49m", as_char);
            }
            else if (ram_disable == 1 or ram_write_enable == 0) {
                print_byte("\x1B[41m", data, "\x1B[49m", as_char);
            }
            else {
                print_byte("\x1B[46m", data, "\x1B[49m", as_char);
            }
        }
        else if (is_read and !is_fetch) {
            if (options.is_dsp) {
                print_byte("\x1B[41m", data, "\x1B[49m", as_char);
            }
            else if (address >= 0x00F0 and address <= 0x00FC) {
                print_byte("\x1B[43m\x1B[37m", data, "\x1B[0m", as_char);
            }
            else if (address >= 0x00FD and address <= 0x00FF) {
                print_byte("\x1B[48;2;250;125;25m\x1B[37m", data, "\x1B[0m", as_char);
            }
            else if (address >= 0xFFC0 and use_boot_rom == 1) {
                print_byte("\x1B[42m\x1B[37m", data, "\x1B[0m", as_char);
            }
            else if (ram_disable == 1) {
                print_byte("\x1B[41m", data, "\x1B[49m", as_char);
            }
            else {
                print_byte("\x1B[42m\x1B[37m", data, "\x1B[0m", as_char);
            }
        }
        else if (is_fetch) {
            print_byte("\x1B[34m", data, "\x1B[39m", as_char);
        }
        else {
            print_byte("", data, "", as_char);
        }
    }
}

fn print_byte(before: []const u8, data: u8, after: []const u8, as_char: bool) void {
    std.debug.print("{s}", .{before});
    if (as_char) {
        std.debug.print("{c}", .{if (data >= 32 and data < 127) data else '.'});
    }
    else {
        std.debug.print("{X:0>2}", .{data});
    }
    std.debug.print("{s}", .{after});
    if (!as_char) {
        std.debug.print(" ", .{});
    }
}

pub fn print_dsp_debug_state(emu: *Emu, options: OptionStruct) void {
    const s = &emu.s_dsp.state;

    // Print voice registers
    print_dsp_debug_voices(emu, 0, options);
    std.debug.print("\n", .{});
    print_dsp_debug_voices(emu, 4, options);
    std.debug.print("\n", .{});

    _ = s;
}

fn print_dsp_debug_voices(emu: *Emu, base: u3, options: OptionStruct) void {
    print_dsp_voices(emu, base, options);
    std.debug.print("\n", .{});

    const s = emu.s_dsp.int();

    std.debug.print("\x1B[90m", .{});

    // Print voice 0-3 states
    for (0..4) |i| {
        const idx = i + base;
        const v = &s._voice[idx];
        const val: u4 = @bitCast(v._buffer_offset);
        std.debug.print("    buff. offset: {X:0>1}        ", .{val});
    }
    std.debug.print("\n", .{});

    for (0..4) |i| {
        const idx = i + base;
        const v = &s._voice[idx];
        const val: u16 = @bitCast(v._gaussian_offset);
        std.debug.print("    gauss offset: {X:0>4}     ", .{val});
    }
    std.debug.print("\n", .{});

    for (0..4) |i| {
        const idx = i + base;
        const v = &s._voice[idx];
        std.debug.print("    brr address:  {X:0>4}     ", .{v._brr_address});
    }
    std.debug.print("\n", .{});

    for (0..4) |i| {
        const idx = i + base;
        const v = &s._voice[idx];
        std.debug.print("    brr offset:   {X:0>1}        ", .{v._brr_offset});
    }
    std.debug.print("\n", .{});

    for (0..4) |i| {
        const idx = i + base;
        const v = &s._voice[idx];
        std.debug.print("    key on delay: {X:0>1}        ", .{v._key_on_delay});
    }
    std.debug.print("\n", .{});

    for (0..4) |i| {
        const idx = i + base;
        const v = &s._voice[idx];
        const res = 
            switch (v._env_mode) {
                .attack  => "attck",
                .decay   => "decay",
                .release => "reles",
                .key_off => "keyof"
            };
        std.debug.print("    env. mode:    {s}    ", .{res});
    }
    std.debug.print("\n", .{});

    for (0..4) |i| {
        const idx = i + base;
        const v = &s._voice[idx];
        std.debug.print("    env. level:   {X:0>2}.{X:0>1}     ", .{v._env_level >> 4, @as(u12, v._env_level) << 1 & 0xF});
    }
    std.debug.print("\n", .{});

    for (0..4) |_| {
        std.debug.print("    buffer:                ", .{});
    }
    std.debug.print("\n", .{});

    for (0..4) |i| {
        const idx = i + base;
        const v = &s._voice[idx];
        const cast_buf: [4]u16 = [_]u16 {
            @bitCast(v._buffer[0]), @bitCast(v._buffer[1]),
            @bitCast(v._buffer[2]), @bitCast(v._buffer[3]),
        };
        std.debug.print("      {X:0>4} {X:0>4} {X:0>4} {X:0>4}  ", .{cast_buf[0], cast_buf[1], cast_buf[2], cast_buf[3]});
    }
    std.debug.print("\n", .{});

    for (0..4) |i| {
        const idx = i + base;
        const v = &s._voice[idx];
        const cast_buf: [4]u16 = [_]u16 {
            @bitCast(v._buffer[4]), @bitCast(v._buffer[5]),
            @bitCast(v._buffer[6]), @bitCast(v._buffer[7]),
        };
        std.debug.print("      {X:0>4} {X:0>4} {X:0>4} {X:0>4}  ", .{cast_buf[0], cast_buf[1], cast_buf[2], cast_buf[3]});
    }
    std.debug.print("\n", .{});

    for (0..4) |i| {
        const idx = i + base;
        const v = &s._voice[idx];
        const cast_buf: [4]u16 = [_]u16 {
            @bitCast(v._buffer[8]),  @bitCast(v._buffer[9]),
            @bitCast(v._buffer[10]), @bitCast(v._buffer[11]),
        };
        std.debug.print("      {X:0>4} {X:0>4} {X:0>4} {X:0>4}  ", .{cast_buf[0], cast_buf[1], cast_buf[2], cast_buf[3]});
    }
    std.debug.print("\n", .{});

    std.debug.print("\x1B[0m", .{});
}

pub fn print_script700_state(emu: *Emu) void {
    const s7  = &emu.script700;
    const s7s = &s7.state;

    const smp  = &emu.s_smp;
    const smps = &smp.state;

    std.debug.print("Running?    : {}\n", .{s7.enabled});
    std.debug.print("Port In (Q) : {X:0>2} {X:0>2} {X:0>2} {X:0>2}\n",   .{s7s.port_in[0], s7s.port_in[1], s7s.port_in[2], s7s.port_in[3]});
    std.debug.print("Port In     : {X:0>2} {X:0>2} {X:0>2} {X:0>2}    ", .{smps.input_ports [0], smps.input_ports [1], smps.input_ports [2], smps.input_ports [3]});
    std.debug.print("Out : {X:0>2} {X:0>2} {X:0>2} {X:0>2}\n",           .{smps.output_ports[0], smps.output_ports[1], smps.output_ports[2], smps.output_ports[3]});

    std.debug.print("Work 0-3    : {X:0>8} {X:0>8} {X:0>8} {X:0>8}\n", .{s7s.work[0], s7s.work[1], s7s.work[2], s7s.work[3]});
    std.debug.print("     4-7    : {X:0>8} {X:0>8} {X:0>8} {X:0>8}\n", .{s7s.work[4], s7s.work[5], s7s.work[6], s7s.work[7]});

    std.debug.print("Cmp Param   : {X:0>8} {X:0>8}\n", .{s7s.cmp[0], s7s.cmp[1]});
    std.debug.print("Wait Until  : ", .{});
    if (s7s.wait_until == null) {
        std.debug.print("---------------- (none)\n", .{});
    }
    else {
        std.debug.print("{X:0>16} ({d})\n", .{s7s.wait_until.?, s7s.wait_until.?});
    }

    std.debug.print("Script Size : {X:0>6}\n",              .{s7.script_bytecode.len});
    std.debug.print("Data Size   : {X:0>6} ",               .{s7.data_area.len});
    std.debug.print("(PC={X:0>6} SP={X:0>2} ST={X:0>2})\n", .{s7s.pc, s7s.sp, s7s.sp_top});
    std.debug.print("Cur. Cycle  : {X:0>16} ({d})\n",       .{s7s.cur_cycle, s7s.cur_cycle});
    std.debug.print("Begin Cycle : {X:0>16} ({d})\n",       .{s7s.begin_cycle, s7s.begin_cycle});
    //std.debug.print("Wait Accum  : {d}\n",                  .{s7s.wait_accum});
    //std.debug.print("Clk Offset  : {d}\n",                  .{s7s.clock_offset});
}