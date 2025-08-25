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
    const state = emu.*.s_smp.spc.state;

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

    const opcode    = s_smp.debug_read_data(pc);
    const operand_1 = s_smp.debug_read_data(pc +% 1);
    const operand_2 = s_smp.debug_read_data(pc +% 2);

    var operand_count: u32 = 0;

    switch (opcode) {
        0x00 => {
            std.debug.print("nop             ", .{});
        },
        0x01 => {
            std.debug.print("tcall 0         ", .{});
        },
        0x02 => {
            operand_count = 1;
            std.debug.print("set1 ${X:0>2}.0      ", .{operand_1});
        },
        0x03 => {
            operand_count = 2;
            const offset: i8 = @bitCast(operand_2);
            const offset_i16: i16 = @as(i16, offset);
            const offset_u16: u16 = @bitCast(offset_i16);
            const target_address: u16 = pc +% offset_u16 +% 3;
            std.debug.print("bbs ${X:0>2}.0, ${X:0>4}", .{operand_1, target_address});
        },
        0x04 => {
            operand_count = 1;
            std.debug.print("or a, ${X:0>2}       ", .{operand_1});
        },
        0x05 => {
            operand_count = 2;
            std.debug.print("or a, ${X:0>4}     ", .{operand_1 | @as(u16, operand_2) << 8});
        },
        0x06 => {
            std.debug.print("or a, (x)       ", .{});
        },
        0x07 => {
            operand_count = 1;
            std.debug.print("or a, [${X:0>2}+x]   ", .{operand_1});
        },
        0x08 => {
            operand_count = 1;
            std.debug.print("or a, #${X:0>2}      ", .{operand_1});
        },
        0x09 => {
            operand_count = 2;
            std.debug.print("or ${X:0>2}, ${X:0>2}     ", .{operand_2, operand_1});
        },
        0x0A => {
            operand_count = 2;
            const addr: u16 = operand_1 | @as(u16, operand_2) << 8;
            std.debug.print("or1 c, ${X:0>4}.{d}  ", .{addr & 0x1FFF, addr >> 13});
        },
        0x0B => {
            operand_count = 1;
            std.debug.print("asl ${X:0>2}         ", .{operand_1});
        },
        0x0C => {
            operand_count = 2;
            const addr: u16 = operand_1 | @as(u16, operand_2) << 8;
            std.debug.print("asl ${X:0>4}       ", .{addr});
        },
        0x0D => {
            std.debug.print("push psw        ", .{});
        },
        0x0E => {
            operand_count = 2;
            const addr: u16 = operand_1 | @as(u16, operand_2) << 8;
            std.debug.print("tset1 ${X:0>4}     ", .{addr});
        },
        0x0F => {
            std.debug.print("brk             ", .{});
        },
        0x10 => {
            operand_count = 1;
            const offset: i8 = @bitCast(operand_1);
            const offset_i16: i16 = @as(i16, offset);
            const offset_u16: u16 = @bitCast(offset_i16);
            const target_address: u16 = pc +% offset_u16 +% 2;
            std.debug.print("bpl ${X:0>4}       ", .{target_address});
        },
        0x11 => {
            std.debug.print("tcall 1         ", .{});
        },
        0x12 => {
            operand_count = 1;
            std.debug.print("clr1 ${X:0>2}.0      ", .{operand_1});
        },
        0x13 => {
            operand_count = 2;
            const offset: i8 = @bitCast(operand_2);
            const offset_i16: i16 = @as(i16, offset);
            const offset_u16: u16 = @bitCast(offset_i16);
            const target_address: u16 = pc +% offset_u16 +% 3;
            std.debug.print("bbc ${X:0>2}.0, ${X:0>4}", .{operand_1, target_address});
        },
        0x14 => {
            operand_count = 1;
            std.debug.print("or a, ${X:0>2}+x     ", .{operand_1});
        },
        0x15 => {
            operand_count = 2;
            std.debug.print("or a, ${X:0>4}+x   ", .{operand_1 | @as(u16, operand_2) << 8});
        },
        0x16 => {
            operand_count = 2;
            std.debug.print("or a, ${X:0>4}+y   ", .{operand_1 | @as(u16, operand_2) << 8});
        },
        0x17 => {
            operand_count = 1;
            std.debug.print("or a, [${X:0>2}]+y   ", .{operand_1});
        },
        0x18 => {
            operand_count = 2;
            std.debug.print("or ${X:0>2}, #${X:0>2}    ", .{operand_2, operand_1});
        },
        0x19 => {
            std.debug.print("or (x), (y)     ", .{});
        },
        0x1A => {
            operand_count = 1;
            std.debug.print("decw ${X:0>2}        ", .{operand_1});
        },
        0x1B => {
            operand_count = 1;
            std.debug.print("asl ${X:0>2}+x       ", .{operand_1});
        },
        0x1C => {
            std.debug.print("asl a           ", .{});
        },
        0x1D => {
            std.debug.print("dec x           ", .{});
        },
        0x1E => {
            operand_count = 2;
            std.debug.print("cmp x, ${X:0>4}    ", .{operand_1 | @as(u16, operand_2) << 8});
        },
        0x1F => {
            operand_count = 2;
            std.debug.print("jmp [${X:0>4}+x]   ", .{operand_1 | @as(u16, operand_2) << 8});
        },
        0x20 => {
            std.debug.print("clrp            ", .{});
        },
        0x21 => {
            std.debug.print("tcall 2         ", .{});
        },
        0x22 => {
            operand_count = 1;
            std.debug.print("set1 ${X:0>2}.1      ", .{operand_1});
        },
        0x23 => {
            operand_count = 2;
            const offset: i8 = @bitCast(operand_2);
            const offset_i16: i16 = @as(i16, offset);
            const offset_u16: u16 = @bitCast(offset_i16);
            const target_address: u16 = pc +% offset_u16 +% 3;
            std.debug.print("bbs ${X:0>2}.1, ${X:0>4}", .{operand_1, target_address});
        },
        0x24 => {
            operand_count = 1;
            std.debug.print("and a, ${X:0>2}      ", .{operand_1});
        },
        0x25 => {
            operand_count = 2;
            std.debug.print("and a, ${X:0>4}    ", .{operand_1 | @as(u16, operand_2) << 8});
        },
        0x26 => {
            std.debug.print("and a, (x)      ", .{});
        },
        0x27 => {
            operand_count = 1;
            std.debug.print("and a, [${X:0>2}+x]  ", .{operand_1});
        },
        0x28 => {
            operand_count = 1;
            std.debug.print("and a, #${X:0>2}     ", .{operand_1});
        },
        0x29 => {
            operand_count = 2;
            std.debug.print("and ${X:0>2}, ${X:0>2}    ", .{operand_2, operand_1});
        },
        0x2A => {
            operand_count = 2;
            const addr: u16 = operand_1 | @as(u16, operand_2) << 8;
            std.debug.print("or1 c, /${X:0>4}.{d} ", .{addr & 0x1FFF, addr >> 13});
        },
        0x2B => {
            operand_count = 1;
            std.debug.print("rol ${X:0>2}         ", .{operand_1});
        },
        0x2C => {
            operand_count = 2;
            const addr: u16 = operand_1 | @as(u16, operand_2) << 8;
            std.debug.print("rol ${X:0>4}       ", .{addr});
        },
        0x2D => {
            std.debug.print("push a          ", .{});
        },
        0x2E => {
            operand_count = 2;
            const offset: i8 = @bitCast(operand_2);
            const offset_i16: i16 = @as(i16, offset);
            const offset_u16: u16 = @bitCast(offset_i16);
            const target_address: u16 = pc +% offset_u16 +% 3;
            std.debug.print("cbne ${X:0>2}, ${X:0>4} ", .{operand_1, target_address});
        },
        0x2F => {
            operand_count = 1;
            const offset: i8 = @bitCast(operand_1);
            const offset_i16: i16 = @as(i16, offset);
            const offset_u16: u16 = @bitCast(offset_i16);
            const target_address: u16 = pc +% offset_u16 +% 2;
            std.debug.print("bra ${X:0>4}       ", .{target_address});
        },
        0x30 => {
            operand_count = 1;
            const offset: i8 = @bitCast(operand_1);
            const offset_i16: i16 = @as(i16, offset);
            const offset_u16: u16 = @bitCast(offset_i16);
            const target_address: u16 = pc +% offset_u16 +% 2;
            std.debug.print("bmi ${X:0>4}       ", .{target_address});
        },
        0x31 => {
            std.debug.print("tcall 3         ", .{});
        },
        0x32 => {
            operand_count = 1;
            std.debug.print("clr1 ${X:0>2}.1      ", .{operand_1});
        },
        0x33 => {
            operand_count = 2;
            const offset: i8 = @bitCast(operand_2);
            const offset_i16: i16 = @as(i16, offset);
            const offset_u16: u16 = @bitCast(offset_i16);
            const target_address: u16 = pc +% offset_u16 +% 3;
            std.debug.print("bbc ${X:0>2}.1, ${X:0>4}", .{operand_1, target_address});
        },
        0x34 => {
            operand_count = 1;
            std.debug.print("and a, ${X:0>2}+x    ", .{operand_1});
        },
        0x35 => {
            operand_count = 2;
            std.debug.print("and a, ${X:0>4}+x  ", .{operand_1 | @as(u16, operand_2) << 8});
        },
        0x36 => {
            operand_count = 2;
            std.debug.print("and a, ${X:0>4}+y  ", .{operand_1 | @as(u16, operand_2) << 8});
        },
        0x37 => {
            operand_count = 1;
            std.debug.print("and a, [${X:0>2}]+y  ", .{operand_1});
        },
        0x38 => {
            operand_count = 2;
            std.debug.print("and ${X:0>2}, #${X:0>2}   ", .{operand_2, operand_1});
        },
        0x39 => {
            std.debug.print("and (x), (y)    ", .{});
        },
        0x3A => {
            operand_count = 1;
            std.debug.print("incw ${X:0>2}        ", .{operand_1});
        },
        0x3B => {
            operand_count = 1;
            std.debug.print("rol ${X:0>2}+x       ", .{operand_1});
        },
        0x3C => {
            std.debug.print("rol a           ", .{});
        },
        0x3D => {
            std.debug.print("inc x           ", .{});
        },
        0x3E => {
            operand_count = 1;
            std.debug.print("cmp x, ${X:0>2}      ", .{operand_1});
        },
        0x3F => {
            operand_count = 2;
            std.debug.print("call ${X:0>4}      ", .{operand_1 | @as(u16, operand_2) << 8});
        },
        0x40 => {
            std.debug.print("setp            ", .{});
        },
        0x41 => {
            std.debug.print("tcall 4         ", .{});
        },
        0x42 => {
            operand_count = 1;
            std.debug.print("set1 ${X:0>2}.2      ", .{operand_1});
        },
        0x43 => {
            operand_count = 2;
            const offset: i8 = @bitCast(operand_2);
            const offset_i16: i16 = @as(i16, offset);
            const offset_u16: u16 = @bitCast(offset_i16);
            const target_address: u16 = pc +% offset_u16 +% 3;
            std.debug.print("bbs ${X:0>2}.2, ${X:0>4}", .{operand_1, target_address});
        },
        0x44 => {
            operand_count = 1;
            std.debug.print("eor a, ${X:0>2}      ", .{operand_1});
        },
        0x45 => {
            operand_count = 2;
            std.debug.print("eor a, ${X:0>4}    ", .{operand_1 | @as(u16, operand_2) << 8});
        },
        0x46 => {
            std.debug.print("eor a, (x)      ", .{});
        },
        0x47 => {
            operand_count = 1;
            std.debug.print("eor a, [${X:0>2}+x]  ", .{operand_1});
        },
        0x48 => {
            operand_count = 1;
            std.debug.print("eor a, #${X:0>2}     ", .{operand_1});
        },
        0x49 => {
            operand_count = 2;
            std.debug.print("eor ${X:0>2}, ${X:0>2}    ", .{operand_2, operand_1});
        },
        0x4A => {
            operand_count = 2;
            const addr: u16 = operand_1 | @as(u16, operand_2) << 8;
            std.debug.print("and1 c, ${X:0>4}.{d} ", .{addr & 0x1FFF, addr >> 13});
        },
        0x4B => {
            operand_count = 1;
            std.debug.print("lsr ${X:0>2}         ", .{operand_1});
        },
        0x4C => {
            operand_count = 2;
            const addr: u16 = operand_1 | @as(u16, operand_2) << 8;
            std.debug.print("lsr ${X:0>4}       ", .{addr});
        },
        0x4D => {
            std.debug.print("push x          ", .{});
        },
        0x4E => {
            operand_count = 2;
            const addr: u16 = operand_1 | @as(u16, operand_2) << 8;
            std.debug.print("tclr1 ${X:0>4}     ", .{addr});
        },
        0x4F => {
            operand_count = 1;
            std.debug.print("pcall ${X:0>2}       ", .{operand_1});
        },
        0x50 => {
            operand_count = 1;
            const offset: i8 = @bitCast(operand_1);
            const offset_i16: i16 = @as(i16, offset);
            const offset_u16: u16 = @bitCast(offset_i16);
            const target_address: u16 = pc +% offset_u16 +% 2;
            std.debug.print("bvc ${X:0>4}       ", .{target_address});
        },
        0x51 => {
            std.debug.print("tcall 5         ", .{});
        },
        0x52 => {
            operand_count = 1;
            std.debug.print("clr1 ${X:0>2}.2      ", .{operand_1});
        },
        0x53 => {
            operand_count = 2;
            const offset: i8 = @bitCast(operand_2);
            const offset_i16: i16 = @as(i16, offset);
            const offset_u16: u16 = @bitCast(offset_i16);
            const target_address: u16 = pc +% offset_u16 +% 3;
            std.debug.print("bbc ${X:0>2}.2, ${X:0>4}", .{operand_1, target_address});
        },
        0x54 => {
            operand_count = 1;
            std.debug.print("eor a, ${X:0>2}+x    ", .{operand_1});
        },
        0x55 => {
            operand_count = 2;
            std.debug.print("eor a, ${X:0>4}+x  ", .{operand_1 | @as(u16, operand_2) << 8});
        },
        0x56 => {
            operand_count = 2;
            std.debug.print("eor a, ${X:0>4}+y  ", .{operand_1 | @as(u16, operand_2) << 8});
        },
        0x57 => {
            operand_count = 1;
            std.debug.print("eor a, [${X:0>2}]+y  ", .{operand_1});
        },
        0x58 => {
            operand_count = 2;
            std.debug.print("eor ${X:0>2}, #${X:0>2}   ", .{operand_2, operand_1});
        },
        0x59 => {
            std.debug.print("eor (x), (y)    ", .{});
        },
        0x5A => {
            operand_count = 1;
            std.debug.print("cmpw ya, ${X:0>2}    ", .{operand_1});
        },
        0x5B => {
            operand_count = 1;
            std.debug.print("lsr ${X:0>2}+x       ", .{operand_1});
        },
        0x5C => {
            std.debug.print("lsr a           ", .{});
        },
        0x5D => {
            std.debug.print("mov x, a        ", .{});
        },
        0x5E => {
            operand_count = 2;
            std.debug.print("cmp y, ${X:0>4}    ", .{operand_1 | @as(u16, operand_2) << 8});
        },
        0x5F => {
            operand_count = 2;
            std.debug.print("jmp ${X:0>4}       ", .{operand_1 | @as(u16, operand_2) << 8});
        },
        0x60 => {
            std.debug.print("clrc            ", .{});
        },
        0x61 => {
            std.debug.print("tcall 6         ", .{});
        },
        0x62 => {
            operand_count = 1;
            std.debug.print("set1 ${X:0>2}.3      ", .{operand_1});
        },
        0x63 => {
            operand_count = 2;
            const offset: i8 = @bitCast(operand_2);
            const offset_i16: i16 = @as(i16, offset);
            const offset_u16: u16 = @bitCast(offset_i16);
            const target_address: u16 = pc +% offset_u16 +% 3;
            std.debug.print("bbs ${X:0>2}.3, ${X:0>4}", .{operand_1, target_address});
        },
        0x64 => {
            operand_count = 1;
            std.debug.print("cmp a, ${X:0>2}      ", .{operand_1});
        },
        0x65 => {
            operand_count = 2;
            std.debug.print("cmp a, ${X:0>4}    ", .{operand_1 | @as(u16, operand_2) << 8});
        },
        0x66 => {
            std.debug.print("cmp a, (x)      ", .{});
        },
        0x67 => {
            operand_count = 1;
            std.debug.print("cmp a, [${X:0>2}+x]  ", .{operand_1});
        },
        0x68 => {
            operand_count = 1;
            std.debug.print("cmp a, #${X:0>2}     ", .{operand_1});
        },
        0x69 => {
            operand_count = 2;
            std.debug.print("cmp ${X:0>2}, ${X:0>2}    ", .{operand_2, operand_1});
        },
        0x6A => {
            operand_count = 2;
            const addr: u16 = operand_1 | @as(u16, operand_2) << 8;
            std.debug.print("and1 c, /${X:0>4}.{d}", .{addr & 0x1FFF, addr >> 13});
        },
        0x6B => {
            operand_count = 1;
            std.debug.print("ror ${X:0>2}         ", .{operand_1});
        },
        0x6C => {
            operand_count = 2;
            const addr: u16 = operand_1 | @as(u16, operand_2) << 8;
            std.debug.print("ror ${X:0>4}       ", .{addr});
        },
        0x6D => {
            std.debug.print("push y          ", .{});
        },
        0x6E => {
            operand_count = 2;
            const offset: i8 = @bitCast(operand_2);
            const offset_i16: i16 = @as(i16, offset);
            const offset_u16: u16 = @bitCast(offset_i16);
            const target_address: u16 = pc +% offset_u16 +% 3;
            std.debug.print("dbnz ${X:0>2}, ${X:0>4} ", .{operand_1, target_address});
        },
        0x6F => {
            std.debug.print("ret             ", .{});
        },
        0x70 => {
            operand_count = 1;
            const offset: i8 = @bitCast(operand_1);
            const offset_i16: i16 = @as(i16, offset);
            const offset_u16: u16 = @bitCast(offset_i16);
            const target_address: u16 = pc +% offset_u16 +% 2;
            std.debug.print("bvc ${X:0>4}       ", .{target_address});
        },
        0x71 => {
            std.debug.print("tcall 7         ", .{});
        },
        0x72 => {
            operand_count = 1;
            std.debug.print("clr1 ${X:0>2}.3      ", .{operand_1});
        },
        0x73 => {
            operand_count = 2;
            const offset: i8 = @bitCast(operand_2);
            const offset_i16: i16 = @as(i16, offset);
            const offset_u16: u16 = @bitCast(offset_i16);
            const target_address: u16 = pc +% offset_u16 +% 3;
            std.debug.print("bbc ${X:0>2}.3, ${X:0>4}", .{operand_1, target_address});
        },
        0x74 => {
            operand_count = 1;
            std.debug.print("cmp a, ${X:0>2}+x    ", .{operand_1});
        },
        0x75 => {
            operand_count = 2;
            std.debug.print("cmp a, ${X:0>4}+x  ", .{operand_1 | @as(u16, operand_2) << 8});
        },
        0x76 => {
            operand_count = 2;
            std.debug.print("cmp a, ${X:0>4}+y  ", .{operand_1 | @as(u16, operand_2) << 8});
        },
        0x77 => {
            operand_count = 1;
            std.debug.print("cmp a, [${X:0>2}]+y  ", .{operand_1});
        },
        0x78 => {
            operand_count = 2;
            std.debug.print("cmp ${X:0>2}, #${X:0>2}   ", .{operand_2, operand_1});
        },
        0x79 => {
            std.debug.print("cmp (x), (y)    ", .{});
        },
        0x7A => {
            operand_count = 1;
            std.debug.print("addw ya, ${X:0>2}    ", .{operand_1});
        },
        0x7B => {
            operand_count = 1;
            std.debug.print("ror ${X:0>2}+x       ", .{operand_1});
        },
        0x7C => {
            std.debug.print("ror a           ", .{});
        },
        0x7D => {
            std.debug.print("mov a, x        ", .{});
        },
        0x7E => {
            operand_count = 1;
            std.debug.print("cmp y, ${X:0>2}      ", .{operand_1});
        },
        0x7F => {
            std.debug.print("reti            ", .{});
        },
        else => {
            std.debug.print("reti            ", .{});
        }
    }

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

pub fn print_dsp_cycle(emu: *Emu) void {
    const s_smp = &emu.s_smp;

    const prev_cycle = s_smp.prev_exec_cycle;
    const cycle      = s_smp.cur_exec_cycle;

    std.debug.print("Cycle: {d} -> {d} (+{d})", .{prev_cycle, cycle, cycle - prev_cycle});
}

pub fn filter_access_logs(logs: []SSMP.AccessLog) [8]?SSMP.AccessLog {
    var ignore_cycles = [8]?u64 {
        null, null, null, null, null, null, null, null
    };
    var ignore_writes = [8]?u16 {
        null, null, null, null, null, null, null, null
    };
    var filtered = [8]?SSMP.AccessLog {
        null, null, null, null, null, null, null, null
    };

    var insert_index: u32 = 0;
    var write_index:  u32 = 0;
    var filter_index: u32 = 0;

    for (logs) |log| {
        const ignore =
            switch (log.type) {
                SSMP.AccessType.fetch, SSMP.AccessType.exec, SSMP.AccessType.dummy_read => true,
                else => false
            };
        if (ignore) {
            ignore_cycles[insert_index] = log.dsp_cycle;
            insert_index += 1;
            if (insert_index == 8) {
                break;
            }
        }
    }

    for (logs) |log| {
        if (log.type == SSMP.AccessType.write) {
            ignore_writes[write_index] = log.address;
            write_index += 1;
            if (write_index == 8) {
                break;
            }
        }
    }

    for (logs) |log| {
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
            if (filter_index == 8) {
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
    logs: ?[]SSMP.AccessLog = null
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

    if (pc_match) {
        print_byte("\x1B[44m", data, "\x1B[49m", as_char);
    }
    else if (address == emu.*.s_smp.spc.pc()) {
        print_byte("\x1B[45m", data, "\x1B[49m", as_char);
    }
    else {
        var is_fetch: bool = false;
        var is_read:  bool = false;
        var is_write: bool = false;

        if (options.logs) |logs| {
            for (logs) |log| {
                if (log.address == address) {
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
            if (address >= 0x00F0 and address <= 0x00FF) {
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
            if (address >= 0x00F0 and address <= 0x00FC) {
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