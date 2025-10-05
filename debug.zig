const std = @import("std");

const Emu      = @import("emu.zig").Emu;
const SDSP     = @import("s_dsp.zig").SDSP;
const SSMP     = @import("s_smp.zig").SSMP;
const SMPState = @import("smp_state.zig").SMPState;
const SPC      = @import("spc.zig").SPC;
const SPCState = @import("spc_state.zig").SPCState;

pub fn print_pc(pc: u16) void {
    print("{X:0>4}", .{pc});
}

pub fn print_spc_state(state: *const SPCState) void {
    //const state = emu.s_smp.spc.state;

    const a  = state.a;
    const x  = state.x;
    const y  = state.y;
    const sp = state.sp;
    //const pc = state.pc;

    const n: u8 = if (state.n() == 1) 'N' else 'n';
    const v: u8 = if (state.v() == 1) 'V' else 'v';
    const p: u8 = if (state.p() == 1) 'P' else 'p';
    const b: u8 = if (state.b() == 1) 'B' else 'b';
    const h: u8 = if (state.h() == 1) 'H' else 'h';
    const i: u8 = if (state.i() == 1) 'I' else 'i';
    const z: u8 = if (state.z() == 1) 'Z' else 'z';
    const c: u8 = if (state.c() == 1) 'C' else 'c';

    print(
        "A:{X:0>2} X:{X:0>2} Y:{X:0>2} SP:{X:0>2} {c}{c}{c}{c}{c}{c}{c}{c}",
        .{
            a, x, y, sp, //pc,
            n, v, p, b, h, i, z, c
        }
    );
}

pub fn print_opcode(emu: *const Emu, pc: u16) void {
    //@setEvalBranchQuota(4096);

    const s_smp = &emu.s_smp;

    //const pc = s_smp.spc.pc();

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
                print("nop              ", .{});
            },
            0x01 => {
                print("tcall 0          ", .{});
            },
            0x02 => {
                operand_count = 1;
                print("set1 ${X:0>2}.0       ", .{operand_1});
            },
            0x03 => {
                operand_count = 2;
                const offset: i8 = @bitCast(operand_2);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 3;
                print("bbs ${X:0>2}.0, ${X:0>4} ", .{operand_1, target_address});
            },
            0x04 => {
                operand_count = 1;
                print("or a, ${X:0>2}        ", .{operand_1});
            },
            0x05 => {
                operand_count = 2;
                print("or a, ${X:0>4}      ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0x06 => {
                print("or a, (x)        ", .{});
            },
            0x07 => {
                operand_count = 1;
                print("or a, [${X:0>2}+x]    ", .{operand_1});
            },
            0x08 => {
                operand_count = 1;
                print("or a, #${X:0>2}       ", .{operand_1});
            },
            0x09 => {
                operand_count = 2;
                print("or ${X:0>2}, ${X:0>2}      ", .{operand_2, operand_1});
            },
            0x0A => {
                operand_count = 2;
                const addr: u16 = operand_1 | @as(u16, operand_2) << 8;
                print("or1 c, ${X:0>4}.{d}   ", .{addr & 0x1FFF, addr >> 13});
            },
            0x0B => {
                operand_count = 1;
                print("asl ${X:0>2}          ", .{operand_1});
            },
            0x0C => {
                operand_count = 2;
                const addr: u16 = operand_1 | @as(u16, operand_2) << 8;
                print("asl ${X:0>4}        ", .{addr});
            },
            0x0D => {
                print("push psw         ", .{});
            },
            0x0E => {
                operand_count = 2;
                const addr: u16 = operand_1 | @as(u16, operand_2) << 8;
                print("tset1 ${X:0>4}      ", .{addr});
            },
            0x0F => {
                print("brk              ", .{});
            },
            0x10 => {
                operand_count = 1;
                const offset: i8 = @bitCast(operand_1);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 2;
                print("bpl ${X:0>4}        ", .{target_address});
            },
            0x11 => {
                print("tcall 1          ", .{});
            },
            0x12 => {
                operand_count = 1;
                print("clr1 ${X:0>2}.0       ", .{operand_1});
            },
            0x13 => {
                operand_count = 2;
                const offset: i8 = @bitCast(operand_2);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 3;
                print("bbc ${X:0>2}.0, ${X:0>4} ", .{operand_1, target_address});
            },
            0x14 => {
                operand_count = 1;
                print("or a, ${X:0>2}+x      ", .{operand_1});
            },
            0x15 => {
                operand_count = 2;
                print("or a, ${X:0>4}+x    ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0x16 => {
                operand_count = 2;
                print("or a, ${X:0>4}+y    ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0x17 => {
                operand_count = 1;
                print("or a, [${X:0>2}]+y    ", .{operand_1});
            },
            0x18 => {
                operand_count = 2;
                print("or ${X:0>2}, #${X:0>2}     ", .{operand_2, operand_1});
            },
            0x19 => {
                print("or (x), (y)      ", .{});
            },
            0x1A => {
                operand_count = 1;
                print("decw ${X:0>2}         ", .{operand_1});
            },
            0x1B => {
                operand_count = 1;
                print("asl ${X:0>2}+x        ", .{operand_1});
            },
            0x1C => {
                print("asl a            ", .{});
            },
            0x1D => {
                print("dec x            ", .{});
            },
            0x1E => {
                operand_count = 2;
                print("cmp x, ${X:0>4}     ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0x1F => {
                operand_count = 2;
                print("jmp [${X:0>4}+x]    ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0x20 => {
                print("clrp             ", .{});
            },
            0x21 => {
                print("tcall 2          ", .{});
            },
            0x22 => {
                operand_count = 1;
                print("set1 ${X:0>2}.1       ", .{operand_1});
            },
            0x23 => {
                operand_count = 2;
                const offset: i8 = @bitCast(operand_2);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 3;
                print("bbs ${X:0>2}.1, ${X:0>4} ", .{operand_1, target_address});
            },
            0x24 => {
                operand_count = 1;
                print("and a, ${X:0>2}       ", .{operand_1});
            },
            0x25 => {
                operand_count = 2;
                print("and a, ${X:0>4}     ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0x26 => {
                print("and a, (x)       ", .{});
            },
            0x27 => {
                operand_count = 1;
                print("and a, [${X:0>2}+x]   ", .{operand_1});
            },
            0x28 => {
                operand_count = 1;
                print("and a, #${X:0>2}      ", .{operand_1});
            },
            0x29 => {
                operand_count = 2;
                print("and ${X:0>2}, ${X:0>2}     ", .{operand_2, operand_1});
            },
            0x2A => {
                operand_count = 2;
                const addr: u16 = operand_1 | @as(u16, operand_2) << 8;
                print("or1 c, /${X:0>4}.{d}  ", .{addr & 0x1FFF, addr >> 13});
            },
            0x2B => {
                operand_count = 1;
                print("rol ${X:0>2}          ", .{operand_1});
            },
            0x2C => {
                operand_count = 2;
                const addr: u16 = operand_1 | @as(u16, operand_2) << 8;
                print("rol ${X:0>4}        ", .{addr});
            },
            0x2D => {
                print("push a           ", .{});
            },
            0x2E => {
                operand_count = 2;
                const offset: i8 = @bitCast(operand_2);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 3;
                print("cbne ${X:0>2}, ${X:0>4}  ", .{operand_1, target_address});
            },
            0x2F => {
                operand_count = 1;
                const offset: i8 = @bitCast(operand_1);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 2;
                print("bra ${X:0>4}        ", .{target_address});
            },
            0x30 => {
                operand_count = 1;
                const offset: i8 = @bitCast(operand_1);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 2;
                print("bmi ${X:0>4}        ", .{target_address});
            },
            0x31 => {
                print("tcall 3          ", .{});
            },
            0x32 => {
                operand_count = 1;
                print("clr1 ${X:0>2}.1       ", .{operand_1});
            },
            0x33 => {
                operand_count = 2;
                const offset: i8 = @bitCast(operand_2);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 3;
                print("bbc ${X:0>2}.1, ${X:0>4} ", .{operand_1, target_address});
            },
            0x34 => {
                operand_count = 1;
                print("and a, ${X:0>2}+x     ", .{operand_1});
            },
            0x35 => {
                operand_count = 2;
                print("and a, ${X:0>4}+x   ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0x36 => {
                operand_count = 2;
                print("and a, ${X:0>4}+y   ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0x37 => {
                operand_count = 1;
                print("and a, [${X:0>2}]+y   ", .{operand_1});
            },
            0x38 => {
                operand_count = 2;
                print("and ${X:0>2}, #${X:0>2}    ", .{operand_2, operand_1});
            },
            0x39 => {
                print("and (x), (y)     ", .{});
            },
            0x3A => {
                operand_count = 1;
                print("incw ${X:0>2}         ", .{operand_1});
            },
            0x3B => {
                operand_count = 1;
                print("rol ${X:0>2}+x        ", .{operand_1});
            },
            0x3C => {
                print("rol a            ", .{});
            },
            0x3D => {
                print("inc x            ", .{});
            },
            0x3E => {
                operand_count = 1;
                print("cmp x, ${X:0>2}       ", .{operand_1});
            },
            0x3F => {
                operand_count = 2;
                print("call ${X:0>4}       ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0x40 => {
                print("setp             ", .{});
            },
            0x41 => {
                print("tcall 4          ", .{});
            },
            0x42 => {
                operand_count = 1;
                print("set1 ${X:0>2}.2       ", .{operand_1});
            },
            0x43 => {
                operand_count = 2;
                const offset: i8 = @bitCast(operand_2);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 3;
                print("bbs ${X:0>2}.2, ${X:0>4} ", .{operand_1, target_address});
            },
            0x44 => {
                operand_count = 1;
                print("eor a, ${X:0>2}       ", .{operand_1});
            },
            0x45 => {
                operand_count = 2;
                print("eor a, ${X:0>4}     ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0x46 => {
                print("eor a, (x)       ", .{});
            },
            0x47 => {
                operand_count = 1;
                print("eor a, [${X:0>2}+x]   ", .{operand_1});
            },
            0x48 => {
                operand_count = 1;
                print("eor a, #${X:0>2}      ", .{operand_1});
            },
            0x49 => {
                operand_count = 2;
                print("eor ${X:0>2}, ${X:0>2}     ", .{operand_2, operand_1});
            },
            0x4A => {
                operand_count = 2;
                const addr: u16 = operand_1 | @as(u16, operand_2) << 8;
                print("and1 c, ${X:0>4}.{d}  ", .{addr & 0x1FFF, addr >> 13});
            },
            0x4B => {
                operand_count = 1;
                print("lsr ${X:0>2}          ", .{operand_1});
            },
            0x4C => {
                operand_count = 2;
                const addr: u16 = operand_1 | @as(u16, operand_2) << 8;
                print("lsr ${X:0>4}        ", .{addr});
            },
            0x4D => {
                print("push x           ", .{});
            },
            0x4E => {
                operand_count = 2;
                const addr: u16 = operand_1 | @as(u16, operand_2) << 8;
                print("tclr1 ${X:0>4}      ", .{addr});
            },
            0x4F => {
                operand_count = 1;
                print("pcall ${X:0>2}        ", .{operand_1});
            },
            0x50 => {
                operand_count = 1;
                const offset: i8 = @bitCast(operand_1);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 2;
                print("bvc ${X:0>4}        ", .{target_address});
            },
            0x51 => {
                print("tcall 5          ", .{});
            },
            0x52 => {
                operand_count = 1;
                print("clr1 ${X:0>2}.2       ", .{operand_1});
            },
            0x53 => {
                operand_count = 2;
                const offset: i8 = @bitCast(operand_2);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 3;
                print("bbc ${X:0>2}.2, ${X:0>4} ", .{operand_1, target_address});
            },
            0x54 => {
                operand_count = 1;
                print("eor a, ${X:0>2}+x     ", .{operand_1});
            },
            0x55 => {
                operand_count = 2;
                print("eor a, ${X:0>4}+x   ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0x56 => {
                operand_count = 2;
                print("eor a, ${X:0>4}+y   ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0x57 => {
                operand_count = 1;
                print("eor a, [${X:0>2}]+y   ", .{operand_1});
            },
            0x58 => {
                operand_count = 2;
                print("eor ${X:0>2}, #${X:0>2}    ", .{operand_2, operand_1});
            },
            0x59 => {
                print("eor (x), (y)     ", .{});
            },
            0x5A => {
                operand_count = 1;
                print("cmpw ya, ${X:0>2}     ", .{operand_1});
            },
            0x5B => {
                operand_count = 1;
                print("lsr ${X:0>2}+x        ", .{operand_1});
            },
            0x5C => {
                print("lsr a            ", .{});
            },
            0x5D => {
                print("mov x, a         ", .{});
            },
            0x5E => {
                operand_count = 2;
                print("cmp y, ${X:0>4}     ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0x5F => {
                operand_count = 2;
                print("jmp ${X:0>4}        ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0x60 => {
                print("clrc             ", .{});
            },
            0x61 => {
                print("tcall 6          ", .{});
            },
            0x62 => {
                operand_count = 1;
                print("set1 ${X:0>2}.3       ", .{operand_1});
            },
            0x63 => {
                operand_count = 2;
                const offset: i8 = @bitCast(operand_2);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 3;
                print("bbs ${X:0>2}.3, ${X:0>4} ", .{operand_1, target_address});
            },
            0x64 => {
                operand_count = 1;
                print("cmp a, ${X:0>2}       ", .{operand_1});
            },
            0x65 => {
                operand_count = 2;
                print("cmp a, ${X:0>4}     ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0x66 => {
                print("cmp a, (x)       ", .{});
            },
            0x67 => {
                operand_count = 1;
                print("cmp a, [${X:0>2}+x]   ", .{operand_1});
            },
            0x68 => {
                operand_count = 1;
                print("cmp a, #${X:0>2}      ", .{operand_1});
            },
            0x69 => {
                operand_count = 2;
                print("cmp ${X:0>2}, ${X:0>2}     ", .{operand_2, operand_1});
            },
            0x6A => {
                operand_count = 2;
                const addr: u16 = operand_1 | @as(u16, operand_2) << 8;
                print("and1 c, /${X:0>4}.{d} ", .{addr & 0x1FFF, addr >> 13});
            },
            0x6B => {
                operand_count = 1;
                print("ror ${X:0>2}          ", .{operand_1});
            },
            0x6C => {
                operand_count = 2;
                const addr: u16 = operand_1 | @as(u16, operand_2) << 8;
                print("ror ${X:0>4}        ", .{addr});
            },
            0x6D => {
                print("push y           ", .{});
            },
            0x6E => {
                operand_count = 2;
                const offset: i8 = @bitCast(operand_2);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 3;
                print("dbnz ${X:0>2}, ${X:0>4}  ", .{operand_1, target_address});
            },
            0x6F => {
                print("ret              ", .{});
            },
            0x70 => {
                operand_count = 1;
                const offset: i8 = @bitCast(operand_1);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 2;
                print("bvc ${X:0>4}        ", .{target_address});
            },
            0x71 => {
                print("tcall 7          ", .{});
            },
            0x72 => {
                operand_count = 1;
                print("clr1 ${X:0>2}.3       ", .{operand_1});
            },
            0x73 => {
                operand_count = 2;
                const offset: i8 = @bitCast(operand_2);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 3;
                print("bbc ${X:0>2}.3, ${X:0>4} ", .{operand_1, target_address});
            },
            0x74 => {
                operand_count = 1;
                print("cmp a, ${X:0>2}+x     ", .{operand_1});
            },
            0x75 => {
                operand_count = 2;
                print("cmp a, ${X:0>4}+x   ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0x76 => {
                operand_count = 2;
                print("cmp a, ${X:0>4}+y   ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0x77 => {
                operand_count = 1;
                print("cmp a, [${X:0>2}]+y   ", .{operand_1});
            },
            0x78 => {
                operand_count = 2;
                print("cmp ${X:0>2}, #${X:0>2}    ", .{operand_2, operand_1});
            },
            0x79 => {
                print("cmp (x), (y)     ", .{});
            },
            0x7A => {
                operand_count = 1;
                print("addw ya, ${X:0>2}     ", .{operand_1});
            },
            0x7B => {
                operand_count = 1;
                print("ror ${X:0>2}+x        ", .{operand_1});
            },
            0x7C => {
                print("ror a            ", .{});
            },
            0x7D => {
                print("mov a, x         ", .{});
            },
            0x7E => {
                operand_count = 1;
                print("cmp y, ${X:0>2}       ", .{operand_1});
            },
            0x7F => {
                print("reti             ", .{});
            },
            0x80 => {
                print("setc             ", .{});
            },
            0x81 => {
                print("tcall 6          ", .{});
            },
            0x82 => {
                operand_count = 1;
                print("set1 ${X:0>2}.4       ", .{operand_1});
            },
            0x83 => {
                operand_count = 2;
                const offset: i8 = @bitCast(operand_2);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 3;
                print("bbs ${X:0>2}.4, ${X:0>4} ", .{operand_1, target_address});
            },
            0x84 => {
                operand_count = 1;
                print("adc a, ${X:0>2}       ", .{operand_1});
            },
            0x85 => {
                operand_count = 2;
                print("adc a, ${X:0>4}     ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0x86 => {
                print("adc a, (x)       ", .{});
            },
            0x87 => {
                operand_count = 1;
                print("adc a, [${X:0>2}+x]   ", .{operand_1});
            },
            0x88 => {
                operand_count = 1;
                print("adc a, #${X:0>2}      ", .{operand_1});
            },
            0x89 => {
                operand_count = 2;
                print("adc ${X:0>2}, ${X:0>2}     ", .{operand_2, operand_1});
            },
            0x8A => {
                operand_count = 2;
                const addr: u16 = operand_1 | @as(u16, operand_2) << 8;
                print("eor1 c, ${X:0>4}.{d}  ", .{addr & 0x1FFF, addr >> 13});
            },
            0x8B => {
                operand_count = 1;
                print("dec ${X:0>2}          ", .{operand_1});
            },
            0x8C => {
                operand_count = 2;
                const addr: u16 = operand_1 | @as(u16, operand_2) << 8;
                print("dec ${X:0>4}        ", .{addr});
            },
            0x8D => {
                operand_count = 1;
                print("mov y, #${X:0>2}      ", .{operand_1});
            },
            0x8E => {
                print("pop psw          ", .{});
            },
            0x8F => {
                operand_count = 2;
                print("mov ${X:0>2}, #${X:0>2}    ", .{operand_2, operand_1});
            },
            0x90 => {
                operand_count = 1;
                const offset: i8 = @bitCast(operand_1);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 2;
                print("bvc ${X:0>4}        ", .{target_address});
            },
            0x91 => {
                print("tcall 9          ", .{});
            },
            0x92 => {
                operand_count = 1;
                print("clr1 ${X:0>2}.4       ", .{operand_1});
            },
            0x93 => {
                operand_count = 2;
                const offset: i8 = @bitCast(operand_2);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 3;
                print("bbc ${X:0>2}.4, ${X:0>4} ", .{operand_1, target_address});
            },
            0x94 => {
                operand_count = 1;
                print("adc a, ${X:0>2}+x     ", .{operand_1});
            },
            0x95 => {
                operand_count = 2;
                print("adc a, ${X:0>4}+x   ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0x96 => {
                operand_count = 2;
                print("adc a, ${X:0>4}+y   ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0x97 => {
                operand_count = 1;
                print("adc a, [${X:0>2}]+y   ", .{operand_1});
            },
            0x98 => {
                operand_count = 2;
                print("adc ${X:0>2}, #${X:0>2}    ", .{operand_2, operand_1});
            },
            0x99 => {
                print("adc (x), (y)     ", .{});
            },
            0x9A => {
                operand_count = 1;
                print("subw ya, ${X:0>2}     ", .{operand_1});
            },
            0x9B => {
                operand_count = 1;
                print("dec ${X:0>2}+x        ", .{operand_1});
            },
            0x9C => {
                print("dec a            ", .{});
            },
            0x9D => {
                print("mov x, sp        ", .{});
            },
            0x9E => {
                print("div ya, x        ", .{});
            },
            0x9F => {
                print("xcn a            ", .{});
            },
            0xA0 => {
                print("ei               ", .{});
            },
            0xA1 => {
                print("tcall 10         ", .{});
            },
            0xA2 => {
                operand_count = 1;
                print("set1 ${X:0>2}.5       ", .{operand_1});
            },
            0xA3 => {
                operand_count = 2;
                const offset: i8 = @bitCast(operand_2);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 3;
                print("bbs ${X:0>2}.5, ${X:0>4} ", .{operand_1, target_address});
            },
            0xA4 => {
                operand_count = 1;
                print("sbc a, ${X:0>2}       ", .{operand_1});
            },
            0xA5 => {
                operand_count = 2;
                print("sbc a, ${X:0>4}     ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0xA6 => {
                print("sbc a, (x)       ", .{});
            },
            0xA7 => {
                operand_count = 1;
                print("sbc a, [${X:0>2}+x]   ", .{operand_1});
            },
            0xA8 => {
                operand_count = 1;
                print("sbc a, #${X:0>2}      ", .{operand_1});
            },
            0xA9 => {
                operand_count = 2;
                print("sbc ${X:0>2}, ${X:0>2}     ", .{operand_2, operand_1});
            },
            0xAA => {
                operand_count = 2;
                const addr: u16 = operand_1 | @as(u16, operand_2) << 8;
                print("mov1 c, ${X:0>4}.{d}  ", .{addr & 0x1FFF, addr >> 13});
            },
            0xAB => {
                operand_count = 1;
                print("inc ${X:0>2}          ", .{operand_1});
            },
            0xAC => {
                operand_count = 2;
                const addr: u16 = operand_1 | @as(u16, operand_2) << 8;
                print("inc ${X:0>4}        ", .{addr});
            },
            0xAD => {
                operand_count = 1;
                print("cmp y, #${X:0>2}      ", .{operand_1});
            },
            0xAE => {
                print("pop a            ", .{});
            },
            0xAF => {
                print("mov (x)+, a      ", .{});
            },
            0xB0 => {
                operand_count = 1;
                const offset: i8 = @bitCast(operand_1);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 2;
                print("bcs ${X:0>4}        ", .{target_address});
            },
            0xB1 => {
                print("tcall 11         ", .{});
            },
            0xB2 => {
                operand_count = 1;
                print("clr1 ${X:0>2}.5       ", .{operand_1});
            },
            0xB3 => {
                operand_count = 2;
                const offset: i8 = @bitCast(operand_2);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 3;
                print("bbc ${X:0>2}.5, ${X:0>4} ", .{operand_1, target_address});
            },
            0xB4 => {
                operand_count = 1;
                print("sbc a, ${X:0>2}+x     ", .{operand_1});
            },
            0xB5 => {
                operand_count = 2;
                print("sbc a, ${X:0>4}+x   ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0xB6 => {
                operand_count = 2;
                print("sbc a, ${X:0>4}+y   ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0xB7 => {
                operand_count = 1;
                print("sbc a, [${X:0>2}]+y   ", .{operand_1});
            },
            0xB8 => {
                operand_count = 2;
                print("sbc ${X:0>2}, #${X:0>2}    ", .{operand_2, operand_1});
            },
            0xB9 => {
                print("sbc (x), (y)     ", .{});
            },
            0xBA => {
                operand_count = 1;
                print("movw ya, ${X:0>2}     ", .{operand_1});
            },
            0xBB => {
                operand_count = 1;
                print("inc ${X:0>2}+x        ", .{operand_1});
            },
            0xBC => {
                print("inc a            ", .{});
            },
            0xBD => {
                print("mov sp, x        ", .{});
            },
            0xBE => {
                print("das a            ", .{});
            },
            0xBF => {
                print("mov a, (x)+      ", .{});
            },
            0xC0 => {
                print("di               ", .{});
            },
            0xC1 => {
                print("tcall 12         ", .{});
            },
            0xC2 => {
                operand_count = 1;
                print("set1 ${X:0>2}.6       ", .{operand_1});
            },
            0xC3 => {
                operand_count = 2;
                const offset: i8 = @bitCast(operand_2);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 3;
                print("bbs ${X:0>2}.6, ${X:0>4} ", .{operand_1, target_address});
            },
            0xC4 => {
                operand_count = 1;
                print("mov ${X:0>2}, a       ", .{operand_1});
            },
            0xC5 => {
                operand_count = 2;
                print("mov ${X:0>4}, a     ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0xC6 => {
                print("mov (x), a       ", .{});
            },
            0xC7 => {
                operand_count = 1;
                print("mov [${X:0>2}+x], a   ", .{operand_1});
            },
            0xC8 => {
                operand_count = 1;
                print("cmp x, #${X:0>2}      ", .{operand_1});
            },
            0xC9 => {
                operand_count = 2;
                print("mov ${X:0>4}, x     ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0xCA => {
                operand_count = 2;
                const addr: u16 = operand_1 | @as(u16, operand_2) << 8;
                print("mov1 ${X:0>4}.{d}, c  ", .{addr & 0x1FFF, addr >> 13});
            },
            0xCB => {
                operand_count = 1;
                print("mov ${X:0>2}, y       ", .{operand_1});
            },
            0xCC => {
                operand_count = 2;
                print("mov ${X:0>4}, y     ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0xCD => {
                operand_count = 1;
                print("mov x, #${X:0>2}      ", .{operand_1});
            },
            0xCE => {
                print("pop x            ", .{});
            },
            0xCF => {
                print("mul ya           ", .{});
            },
            0xD0 => {
                operand_count = 1;
                const offset: i8 = @bitCast(operand_1);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 2;
                print("bne ${X:0>4}        ", .{target_address});
            },
            0xD1 => {
                print("tcall 13         ", .{});
            },
            0xD2 => {
                operand_count = 1;
                print("clr1 ${X:0>2}.6       ", .{operand_1});
            },
            0xD3 => {
                operand_count = 2;
                const offset: i8 = @bitCast(operand_2);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 3;
                print("bbc ${X:0>2}.6, ${X:0>4} ", .{operand_1, target_address});
            },
            0xD4 => {
                operand_count = 1;
                print("mov ${X:0>2}+x, a     ", .{operand_1});
            },
            0xD5 => {
                operand_count = 2;
                print("mov ${X:0>4}+x, a   ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0xD6 => {
                operand_count = 2;
                print("mov ${X:0>4}+y, a   ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0xD7 => {
                operand_count = 1;
                print("mov [${X:0>2}]+y, a   ", .{operand_1});
            },
            0xD8 => {
                operand_count = 1;
                print("mov ${X:0>2}, x       ", .{operand_1});
            },
            0xD9 => {
                operand_count = 1;
                print("mov ${X:0>2}+y, x     ", .{operand_1});
            },
            0xDA => {
                operand_count = 1;
                print("movw ${X:0>2}, ya     ", .{operand_1});
            },
            0xDB => {
                operand_count = 1;
                print("mov ${X:0>2}+x, y     ", .{operand_1});
            },
            0xDC => {
                print("dec y            ", .{});
            },
            0xDD => {
                print("mov a, y         ", .{});
            },
            0xDE => {
                operand_count = 2;
                const offset: i8 = @bitCast(operand_2);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 3;
                print("cbne ${X:0>2}+x, ${X:0>4}", .{operand_1, target_address});
            },
            0xDF => {
                print("daa a            ", .{});
            },
            0xE0 => {
                print("clrv             ", .{});
            },
            0xE1 => {
                print("tcall 14         ", .{});
            },
            0xE2 => {
                operand_count = 1;
                print("set1 ${X:0>2}.7       ", .{operand_1});
            },
            0xE3 => {
                operand_count = 2;
                const offset: i8 = @bitCast(operand_2);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 3;
                print("bbs ${X:0>2}.7, ${X:0>4} ", .{operand_1, target_address});
            },
            0xE4 => {
                operand_count = 1;
                print("mov a, ${X:0>2}       ", .{operand_1});
            },
            0xE5 => {
                operand_count = 2;
                print("mov a, ${X:0>4}     ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0xE6 => {
                print("mov a, (x)       ", .{});
            },
            0xE7 => {
                operand_count = 1;
                print("mov a, [${X:0>2}+x]   ", .{operand_1});
            },
            0xE8 => {
                operand_count = 1;
                print("mov a, #${X:0>2}      ", .{operand_1});
            },
            0xE9 => {
                operand_count = 2;
                print("mov x, ${X:0>4}     ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0xEA => {
                operand_count = 2;
                const addr: u16 = operand_1 | @as(u16, operand_2) << 8;
                print("not1 ${X:0>4}.{d}     ", .{addr & 0x1FFF, addr >> 13});
            },
            0xEB => {
                operand_count = 1;
                print("mov y, ${X:0>2}       ", .{operand_1});
            },
            0xEC => {
                operand_count = 2;
                print("mov y, ${X:0>4}     ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0xED => {
                print("notc             ", .{});
            },
            0xEE => {
                print("pop y            ", .{});
            },
            0xEF => {
                print("sleep            ", .{});
            },
            0xF0 => {
                operand_count = 1;
                const offset: i8 = @bitCast(operand_1);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 2;
                print("beq ${X:0>4}        ", .{target_address});
            },
            0xF1 => {
                print("tcall 15         ", .{});
            },
            0xF2 => {
                operand_count = 1;
                print("clr1 ${X:0>2}.7       ", .{operand_1});
            },
            0xF3 => {
                operand_count = 2;
                const offset: i8 = @bitCast(operand_2);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 3;
                print("bbc ${X:0>2}.7, ${X:0>4} ", .{operand_1, target_address});
            },
            0xF4 => {
                operand_count = 1;
                print("mov a, ${X:0>2}+x     ", .{operand_1});
            },
            0xF5 => {
                operand_count = 2;
                print("mov a, ${X:0>4}+x   ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0xF6 => {
                operand_count = 2;
                print("mov a, ${X:0>4}+y   ", .{operand_1 | @as(u16, operand_2) << 8});
            },
            0xF7 => {
                operand_count = 1;
                print("mov a, [${X:0>2}]+y   ", .{operand_1});
            },
            0xF8 => {
                operand_count = 1;
                print("mov x, ${X:0>2}       ", .{operand_1});
            },
            0xF9 => {
                operand_count = 1;
                print("mov x, ${X:0>2}+y     ", .{operand_1});
            },
            0xFA => {
                operand_count = 2;
                print("mov ${X:0>2}, #${X:0>2}    ", .{operand_2, operand_1});
            },
            0xFB => {
                operand_count = 1;
                print("mov y, ${X:0>2}+x     ", .{operand_1});
            },
            0xFC => {
                print("inc y            ", .{});
            },
            0xFD => {
                print("mov y, a         ", .{});
            },
            0xFE => {
                operand_count = 1;
                const offset: i8 = @bitCast(operand_1);
                const offset_i16: i16 = @as(i16, offset);
                const offset_u16: u16 = @bitCast(offset_i16);
                const target_address: u16 = pc +% offset_u16 +% 2;
                print("dbnz y, ${X:0>4}    ", .{target_address});
            },
            0xFF => {
                print("stop             ", .{});
            },
        }
    }
    else {
        print("-----            ", .{});
    }

    if (opc == null) {
        print("   --      ", .{});
    }
    else {
        switch (operand_count) {
            0 => {
                print("   {X:0>2}      ", .{opcode});
            },
            1 => {
                print("   {X:0>2} {X:0>2}   ", .{opcode, operand_1});
            },
            2 => {
                print("   {X:0>2} {X:0>2} {X:0>2}", .{opcode, operand_1, operand_2});
            },
            else => unreachable
        }
    }
}

pub fn print_dsp_cycle(emu: *Emu) void {
    const s_smp = &emu.s_smp;

    const prev_cycle: i64 = @intCast(s_smp.prev_exec_cycle);
    const cycle:      i64 = @intCast(s_smp.cur_exec_cycle);

    print("Cycle: {d} -> {d} (+{d})", .{prev_cycle, cycle, cycle - prev_cycle});
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
    var bw = std.io.countingWriter(std.io.getStdErr().writer());
    const w = bw.writer();
    break :blk w;
});

pub fn print_logs(state: *const SMPState, logs: []?SSMP.AccessLog) !void {
    var buf: [1024]u8 = undefined;

    var fbs = std.io.fixedBufferStream(&buf);

    var buffer_writer = std.io.countingWriter(fbs.writer());
    var writer = buffer_writer.writer();

    var pad_length: u32 = 63;

    for (logs, 0..) |log, i| {
        if (log) |val| {
            if (i > 0) {
                _ = try writer.print(" ", .{});
            }
            const extra_bytes = try print_log(state, &val, writer, .{.prefix = false});
            pad_length += extra_bytes;
        }
    }

    while (buffer_writer.bytes_written < pad_length) {
        _ = try writer.print(" ", .{});
    }

    print("{s}", .{buf[0..buffer_writer.bytes_written]});
}

pub fn print_log(state: *const SMPState, log: *const SSMP.AccessLog, writer: anytype, options: struct { prefix: bool = true }) !u32 {
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
            print("[{d}]\t: [Timer{d}-{s}] ", .{log.dsp_cycle, tn, @tagName(log.type)});
        }
        else {
            print("[{d}]\t: [TimerGlobal-{s}] ", .{log.dsp_cycle, @tagName(log.type)});
        }
    }
    
    switch (log.type) {
        SSMP.TimerLogType.read, SSMP.TimerLogType.reset => { },
        else => {
            if (log.timer_number != null) {
                print("internal-count: {X:0>2}, output: {X:0>1}", .{log.internal_counter, log.output});
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
        print("{X:0>4} | ", .{line_start});

        for (0..16) |x| {
            const xx: u16 = @intCast(x);
            const address: u16 = line_start + xx;
            const data: u8 = emu.s_smp.debug_read_data(address);

            print_mem_cell(emu, address, data, false, options);
        }

        print("| ", .{});

        for (0..16) |x| {
            const xx: u16 = @intCast(x);
            const address: u16 = line_start + xx;
            const data: u8 = emu.s_smp.debug_read_data(address);

            print_mem_cell(emu, address, data, true, options);
        }

        print("\n", .{});
    }
}

pub fn print_dsp_map(emu: *Emu, options: OptionStruct) void {
    for (0..8) |y| {
        const yy: u8 = @intCast(y);
        const line_start: u8 = 16 * yy;
        print("{X:0>2} | ", .{line_start});

        for (0..16) |x| {
            const xx: u8 = @intCast(x);
            const address: u8 = line_start + xx;
            const data: u8 = emu.s_dsp.dsp_map[address];

            print_mem_cell(emu, @as(u16, address), data, false, options);
        }

        print("| ", .{});

        for (0..16) |x| {
            const xx: u8 = @intCast(x);
            const address: u8 = line_start + xx;
            const data: u8 = emu.s_dsp.dsp_map[address];

            print_mem_cell(emu, @as(u16, address), data, true, options);
        }

        print("\n", .{});
    }
}

pub fn print_dsp_state(emu: *Emu, options: OptionStruct) void {
    // Print voice registers
    print_dsp_voices(emu, 0, options);
    print("\n", .{});
    print_dsp_voices(emu, 4, options);
    print("\n", .{});
}

pub fn print_dsp_state_2(emu: *Emu, _: OptionStruct) void {
    const s = &emu.s_dsp.state;

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

    print("main volume - left:   {X:0>2}      ", .{mvoll});
    print("key on:                 {X:0>2}\n",   .{kon});
    print("main volume - right:  {X:0>2}      ", .{mvolr});
    print("key off:                {X:0>2}\n",   .{koff});
    print("echo volume - left:   {X:0>2}      ", .{evoll});
    print("source end (endx):      {X:0>2}\n",   .{0x00});
    print("echo volume - right:  {X:0>2}      ", .{evolr});
    print("echo feedback:          {X:0>2}\n",   .{efb});
    print("\n", .{});

    print("pitch modulation:     {X:0>2}      ", .{pmon});
    print("echo buffer start:      {X:0>2}00\n", .{s.echo.esa_page});
    print("noise enable:         {X:0>2}      ", .{non});
    print("source directory start: {X:0>2}00\n", .{s.brr_bank});
    print("echo enable:          {X:0>2}      ", .{eon});
    print("echo delay:             {X:0>2}\n",   .{s.echo.delay});
    print("\n", .{});

    print("noise clock:     {X:0>2}\n", .{s.noise_rate});
    print("read-only echo:  {}\n",      .{s.echo.readonly == 1});
    print("mute:            {}\n",      .{s.mute          == 1});
    print("reset:           {}\n",      .{s.reset         == 1});
    print("\n", .{});
    print(
        "fir:  {X:0>2} {X:0>2} {X:0>2} {X:0>2} {X:0>2} {X:0>2} {X:0>2} {X:0>2}\n",
        .{
            fir[0], fir[1], fir[2], fir[3],
            fir[4], fir[5], fir[6], fir[7],
        }
    );
    print("\n", .{});
}

fn print_dsp_voices(emu: *Emu, base: u3, _: OptionStruct) void {
    const s = &emu.s_dsp.state;

    // Print voice 0-3 states
    for (0..4) |i| {
        const idx = i + base;
        const v = &s.voice[idx];
        const val: u8 = @bitCast(v.vol_left);
        print("V{d}  left volume:  {X:0>2}       ", .{idx, val});
    }
    print("\n", .{});

    for (0..4) |i| {
        const idx = i + base;
        const v = &s.voice[idx];
        const val: u8 = @bitCast(v.vol_right);
        print("    right volume: {X:0>2}       ", .{val});
    }
    print("\n", .{});

    for (0..4) |i| {
        const idx = i + base;
        const v = &s.voice[idx];
        print("    pitch:        {X:0>4}     ", .{v.pitch});
    }
    print("\n", .{});

    for (0..4) |i| {
        const idx = i + base;
        const v = &s.voice[idx];
        print("    srcn:         {X:0>2}       ", .{v.source});
    }
    print("\n", .{});

    for (0..4) |i| {
        const idx = i + base;
        const v = &s.voice[idx];
        print("    adsr 1:       {X:0>2}       ", .{v.adsr_0});
    }
    print("\n", .{});

    for (0..4) |i| {
        const idx = i + base;
        const v = &s.voice[idx];
        print("    adsr 2:       {X:0>2}       ", .{v.adsr_1});
    }
    print("\n", .{});

    for (0..4) |i| {
        const idx = i + base;
        const v = &s.voice[idx];
        print("    gain:         {X:0>2}       ", .{v.gain});
    }
    print("\n", .{});

    for (0..4) |i| {
        const idx = i + base;
        const v = &s.voice[idx];
        print("    envx:         {X:0>2}       ", .{v.envx});
    }
    print("\n", .{});

    for (0..4) |i| {
        const idx = i + base;
        _ = &s.voice[idx];
        print("    outx:         {X:0>2}       ", .{0x00});
    }
    print("\n", .{});
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
    print("{s}", .{before});
    if (as_char) {
        print("{c}", .{if (data >= 32 and data < 127) data else '.'});
    }
    else {
        print("{X:0>2}", .{data});
    }
    print("{s}", .{after});
    if (!as_char) {
        print(" ", .{});
    }
}

pub fn print_dsp_debug_state(emu: *Emu, options: OptionStruct) void {
    const s = &emu.s_dsp.state;

    // Print voice registers
    print_dsp_debug_voices(emu, 0, options);
    print("\n", .{});
    print_dsp_debug_voices(emu, 4, options);
    print("\n", .{});

    _ = s;
}

fn print_dsp_debug_voices(emu: *Emu, base: u3, _: OptionStruct) void {
    //print_dsp_voices(emu, base, options);
    //print("\n", .{});

    const s = emu.s_dsp.int();

    //print("\x1B[90m", .{});

    // Print voice 0-3 states
    for (0..4) |i| {
        const idx = i + base;
        const v = &s._voice[idx];
        const val: u4 = @bitCast(v._buffer_offset);
        print("    buff. offset: {X:0>1}        ", .{val});
    }
    print("\n", .{});

    for (0..4) |i| {
        const idx = i + base;
        const v = &s._voice[idx];
        const val: u16 = @bitCast(v._gaussian_offset);
        print("    gauss offset: {X:0>4}     ", .{val});
    }
    print("\n", .{});

    for (0..4) |i| {
        const idx = i + base;
        const v = &s._voice[idx];
        print("    brr address:  {X:0>4}     ", .{v._brr_address});
    }
    print("\n", .{});

    for (0..4) |i| {
        const idx = i + base;
        const v = &s._voice[idx];
        print("    brr offset:   {X:0>1}        ", .{v._brr_offset});
    }
    print("\n", .{});

    for (0..4) |i| {
        const idx = i + base;
        const v = &s._voice[idx];
        print("    key on delay: {X:0>1}        ", .{v._key_on_delay});
    }
    print("\n", .{});

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
        print("    env. mode:    {s}    ", .{res});
    }
    print("\n", .{});

    for (0..4) |i| {
        const idx = i + base;
        const v = &s._voice[idx];
        print("    env. level:   {X:0>2}.{X:0>1}     ", .{v._env_level >> 4, @as(u12, v._env_level) << 1 & 0xF});
    }
    print("\n", .{});

    for (0..4) |_| {
        print("    buffer:                ", .{});
    }
    print("\n", .{});

    for (0..4) |i| {
        const idx = i + base;
        const v = &s._voice[idx];
        const cast_buf: [4]u16 = [_]u16 {
            @bitCast(v._buffer[0]), @bitCast(v._buffer[1]),
            @bitCast(v._buffer[2]), @bitCast(v._buffer[3]),
        };
        print("      {X:0>4} {X:0>4} {X:0>4} {X:0>4}  ", .{cast_buf[0], cast_buf[1], cast_buf[2], cast_buf[3]});
    }
    print("\n", .{});

    for (0..4) |i| {
        const idx = i + base;
        const v = &s._voice[idx];
        const cast_buf: [4]u16 = [_]u16 {
            @bitCast(v._buffer[4]), @bitCast(v._buffer[5]),
            @bitCast(v._buffer[6]), @bitCast(v._buffer[7]),
        };
        print("      {X:0>4} {X:0>4} {X:0>4} {X:0>4}  ", .{cast_buf[0], cast_buf[1], cast_buf[2], cast_buf[3]});
    }
    print("\n", .{});

    for (0..4) |i| {
        const idx = i + base;
        const v = &s._voice[idx];
        const cast_buf: [4]u16 = [_]u16 {
            @bitCast(v._buffer[8]),  @bitCast(v._buffer[9]),
            @bitCast(v._buffer[10]), @bitCast(v._buffer[11]),
        };
        print("      {X:0>4} {X:0>4} {X:0>4} {X:0>4}  ", .{cast_buf[0], cast_buf[1], cast_buf[2], cast_buf[3]});
    }
    print("\n", .{});

    print("\x1B[0m", .{});
}

pub fn print_script700_state(emu: *Emu) void {
    const s7  = &emu.script700;
    const s7s = &s7.state;

    const smp  = &emu.s_smp;
    const smps = &smp.state;

    print("Running?    : {}\n", .{s7.enabled});
    print("Port In (Q) : {X:0>2} {X:0>2} {X:0>2} {X:0>2}\n",   .{s7s.port_in[0], s7s.port_in[1], s7s.port_in[2], s7s.port_in[3]});
    print("Port In     : {X:0>2} {X:0>2} {X:0>2} {X:0>2}    ", .{smps.input_ports [0], smps.input_ports [1], smps.input_ports [2], smps.input_ports [3]});
    print("Out : {X:0>2} {X:0>2} {X:0>2} {X:0>2}\n",           .{smps.output_ports[0], smps.output_ports[1], smps.output_ports[2], smps.output_ports[3]});

    print("Work 0-3    : {X:0>8} {X:0>8} {X:0>8} {X:0>8}\n", .{s7s.work[0], s7s.work[1], s7s.work[2], s7s.work[3]});
    print("     4-7    : {X:0>8} {X:0>8} {X:0>8} {X:0>8}\n", .{s7s.work[4], s7s.work[5], s7s.work[6], s7s.work[7]});

    print("Cmp Param   : {X:0>8} {X:0>8}\n", .{s7s.cmp[0], s7s.cmp[1]});
    print("Wait Until  : ", .{});
    if (s7s.wait_until == null) {
        print("---------------- (none)\n", .{});
    }
    else {
        print("{X:0>16} ({d})\n", .{s7s.wait_until.?, s7s.wait_until.?});
    }

    print("Script Size : {X:0>6}\n",              .{s7.script_bytecode.len});
    print("Data Size   : {X:0>6} ",               .{s7.data_area.len});
    print("(PC={X:0>6} SP={X:0>2} ST={X:0>2})\n", .{s7s.pc, s7s.sp, s7s.sp_top});
    print("Cur. Cycle  : {X:0>16} ({d})\n",       .{s7s.cur_cycle, s7s.cur_cycle});
    print("Begin Cycle : {X:0>16} ({d})\n",       .{s7s.begin_cycle, s7s.begin_cycle});
    print("Sync Point  : {X:0>16} ({d})\n",       .{s7s.sync_point, s7s.sync_point});
    //print("Wait Accum  : {d}\n",                  .{s7s.wait_accum});
    //print("Clk Offset  : {d}\n",                  .{s7s.clock_offset});
}

var cli_width: u8 = 120;

pub inline fn set_cli_width(amt: u8) void {
    cli_width = amt;
    if (amt < 60) {
        cli_width = 60;
    }
    for (0..max_lines) |i| {
        canvas_line_lengths[i] = cli_width;
    }
}

const max_lines: u32 = 30;

var print_canvas:  [max_lines * 256]u8 = [_]u8 {' '} ** (max_lines * 256);
var canvas_line_lengths: [max_lines]u8 = [_]u8 {120} ** max_lines;
var start_line: u32 = 0;
var canvas_index: u32 = 0;
var canvas_ansi: bool = false;

pub fn print(comptime fmt: []const u8, args: anytype) void {
    var temp_buffer: [1024]u8 = [_]u8 {' '} ** 1024;
    const slice: ?[]const u8 = std.fmt.bufPrint(&temp_buffer, fmt, args) catch null;

    if (slice) |slc| {
        var index: u32 = canvas_index;
        var sub_index: u32 = index % 256;

        //std.debug.print("{d} {d} {d}\n", .{index / 256, canvas_line_lengths[index / 256], cli_width});
        var noprint_offset: u8 = canvas_line_lengths[index / 256] - cli_width;
        var ansi: bool = canvas_ansi;

        var prev_start_line = start_line;

        for (slc) |c| {
            if (sub_index >= 255) {
                index -= sub_index - 255;
                sub_index = 255;
            }

            var line_index = index / 256;
            canvas_line_lengths[line_index] = cli_width + noprint_offset;

            if (sub_index == cli_width + noprint_offset) {
                break;
            }

            if (c == '\n') {
                index = ((line_index + 1) % max_lines) * 256;
                sub_index = 0;
                ansi = false;
                noprint_offset = 0;

                line_index += 1;

                if (line_index >= (start_line + max_lines - 1) % max_lines + 1) {
                    //flush(true);

                    line_index %= max_lines;
                    start_line = (start_line + 1) % max_lines;
                    index = line_index * 256;

                    for (index..(index + 256)) |i| {
                        print_canvas[i] = ' ';
                    }

                    canvas_line_lengths[line_index] = cli_width;
                }
            }
            else {
                if (sub_index >= 255) {
                    // Don't print if we have overflowed the canvas horizontally
                    continue;
                }

                print_canvas[index] = c;

                if (!ansi and c == '\x1B') {
                    ansi = true;
                    noprint_offset +|= 1;
                    canvas_line_lengths[line_index] = cli_width + noprint_offset;
                }
                else if (ansi) {
                    if (c >= 'A' and c <= 'Z' or c >= 'a' and c <= 'z') {
                        ansi = false;
                        if (c == 'J') { // Ignore clears
                            inline for (0..4) |i| {
                                print_canvas[index - i] = ' ';
                            }

                            index -%= 4;
                            sub_index -%= 4;
                            noprint_offset -%= 4;
                        }
                        else if (c == 'A') { // Handle shift up
                            inline for (0..3) |i| {
                                print_canvas[index - i] = ' ';
                            }

                            canvas_line_lengths[line_index] -= 2;

                            index = (line_index + max_lines - 1) % max_lines * 256;
                            sub_index = 0;
                            ansi = false;
                            noprint_offset = 0;

                            continue;
                        }
                        else if (c == 'H') { // Flush on position reset
                            inline for (0..3) |i| {
                                print_canvas[index - i] = ' ';
                            }

                            index -= 2;
                            sub_index -= 2;
                            noprint_offset -= 2;

                            flush(null, false);
                            return;
                        }
                    }
                    noprint_offset +%= 1;
                    canvas_line_lengths[line_index] = cli_width + noprint_offset;
                }

                index +%= 1;
                sub_index +%= 1;
            }

            prev_start_line = start_line;
        }

        canvas_index = index;
        canvas_ansi = ansi;
    }
}

var _last_canvas_index: ?u32 = null;

pub inline fn move_cursor_up() void {
    const line_index = ((canvas_index / 256) + max_lines - 1) % max_lines;
    canvas_line_lengths[line_index] = cli_width;
    canvas_line_lengths[(line_index + 1) % max_lines] = cli_width;
    canvas_index = line_index * 256;
    const blank_index = line_index * 256 + cli_width - 1;
    print_canvas[blank_index] = ' ';
}

pub inline fn goto_last_line() void {
    _last_canvas_index = canvas_index;
    canvas_index = 256 * ((start_line + max_lines - 1) % max_lines);
}

pub inline fn flush(msg: ?[]const u8, no_clear: bool) void {
    var final_buffer: [max_lines * 257]u8 = undefined;
    var total_chars: u32 = 0;

    for (0..max_lines) |L_| {
        const L: u8 = @intCast(L_);

        const len   = canvas_line_lengths[(L + start_line) % max_lines];
        const start = @as(u32, (L + start_line) % max_lines) * 256;

        const res: ?[]const u8 = std.fmt.bufPrint(final_buffer[total_chars..], "{s}\n", .{print_canvas[start..(start + len)]}) catch null;
        if (res) |r| {
            total_chars += @intCast(r.len);
        }
    }

    if (msg) |m| {
        std.debug.print("\x1B[H{s}\r{s}\n> ", .{final_buffer[0 .. (total_chars - 1)], m});
    }
    else {
        std.debug.print("\x1B[H{s}\r{s}\n> ", .{final_buffer[0 .. (total_chars - 1)], "Enter a command (h for help menu): "});
    }

    // Restore position if it was overridden.
    if (_last_canvas_index != null) {
        canvas_index = _last_canvas_index.?;
        _last_canvas_index = null;
    }

    if (!no_clear) {
        for (print_canvas, 0..) |_, i| {
            print_canvas[i] = ' ';
        }

        for (canvas_line_lengths, 0..) |_, i| {
            canvas_line_lengths[i] = cli_width;
        }

        canvas_index = 0;
        canvas_ansi = false;
        start_line = 0;
    }
}