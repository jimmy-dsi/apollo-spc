const std = @import("std");

const db = @import("debug.zig");

const Emu       = @import("emu.zig").Emu;
const SDSP      = @import("s_dsp.zig").SDSP;
const SSMP      = @import("s_smp.zig").SSMP;
const Script700 = @import("script700.zig").Script700;

const spc_loader = @import("spc_loader.zig");

pub fn main() !void {
    // Get SPC file path from cmd line argument - if present
    var gpa = std.heap.GeneralPurposeAllocator(.{}){};
    const allocator = gpa.allocator();
    defer _ = gpa.deinit();

    var args = try std.process.argsWithAllocator(allocator);
    defer args.deinit();

    var spc_file_path: ?[]const u8 = null;
    var debug_mode: bool = false;

    var i: usize = 0;
    while (args.next()) |arg| {
        if (i == 1) {
            spc_file_path = arg;
        }
        else if (i == 2) {
            const str: []const u8 = arg;
            if (std.mem.eql(u8, str, "--debug") or std.mem.eql(u8, str, "-d")) {
                debug_mode = true;
            }
        }
        i += 1;
    }

    Emu.static_init();

    var emu = Emu.new();
    emu.init(
        SDSP.new(&emu),
        SSMP.new(&emu, .{}),
        Script700.new(&emu),
    );
    defer emu.script700.deinit();

    // Load and run Script700 test script
    var sb: [100]u32 = [_]u32 {0x8000_0000} ** 99 ++ [_]u32 {0x80FF_FFFF}; // Pre-fill with all NOPs and a QUIT instruction at the end.
    var ix: u32 = 0;
    
    var sl: []u32 = undefined;

    sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "m",   .{.oper_1_prefix =  "o", .oper_1_value =   0, .oper_2_prefix =  "w", .oper_2_value =   0}); ix += 1;
    sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "w",   .{.oper_1_prefix =  "#", .oper_1_value =  31}); ix += 2;
    sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "m",   .{.oper_1_prefix =  "#", .oper_1_value =   1, .oper_2_prefix =  "i", .oper_2_value =   0}); ix += 2;
    sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "f",   .{}); ix += 1;
    sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "m",   .{.oper_1_prefix =  "o", .oper_1_value =   0, .oper_2_prefix =  "w", .oper_2_value =   1}); ix += 1;
    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "m",   .{.oper_1_prefix =  "#", .oper_1_value =   2, .oper_2_prefix =  "i", .oper_2_value =   0}); ix += 2;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "f",   .{}); ix += 1;
    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "m",   .{.oper_1_prefix =  "#", .oper_1_value =   3, .oper_2_prefix =  "i", .oper_2_value =   0}); ix += 2;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "f",   .{}); ix += 1;
    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "m",   .{.oper_1_prefix =  "#", .oper_1_value =   5, .oper_2_prefix =  "i", .oper_2_value =   0}); ix += 2;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "f",   .{}); ix += 1;
    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "m",   .{.oper_1_prefix =  "#", .oper_1_value =   3, .oper_2_prefix =  "i", .oper_2_value =   0}); ix += 2;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "f",   .{}); ix += 1;
    sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "w",   .{.oper_1_prefix =  "#", .oper_1_value =  33}); ix += 2;
    sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "m",   .{.oper_1_prefix =  "o", .oper_1_value =   0, .oper_2_prefix =  "w", .oper_2_value =   2}); ix += 1;
    sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "w",   .{.oper_1_prefix =  "#", .oper_1_value =  32}); ix += 2;
    sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "m",   .{.oper_1_prefix =  "o", .oper_1_value =   0, .oper_2_prefix =  "w", .oper_2_value =   3}); ix += 1;
    sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "w",   .{.oper_1_prefix =  "#", .oper_1_value =  32}); ix += 2;
    sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "m",   .{.oper_1_prefix =  "o", .oper_1_value =   0, .oper_2_prefix =  "w", .oper_2_value =   4}); ix += 1;
    sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "w",   .{.oper_1_prefix =  "#", .oper_1_value =  32}); ix += 2;
    sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "m",   .{.oper_1_prefix =  "o", .oper_1_value =   0, .oper_2_prefix =  "w", .oper_2_value =   5}); ix += 1;
    sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "w",   .{.oper_1_prefix =  "#", .oper_1_value =  32}); ix += 2;
    sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "m",   .{.oper_1_prefix =  "o", .oper_1_value =   0, .oper_2_prefix =  "w", .oper_2_value =   6}); ix += 1;
    sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "w",   .{.oper_1_prefix =  "#", .oper_1_value =  32}); ix += 2;
    sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "bra", .{.oper_1_prefix =   "", .oper_1_value =   3}); ix += 1;

    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "m",   .{.oper_1_prefix =  "o", .oper_1_value =   0, .oper_2_prefix =  "w", .oper_2_value =   1}); ix += 1;
    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "w",   .{.oper_1_prefix =  "#", .oper_1_value =  32}); ix += 2;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "m",   .{.oper_1_prefix =  "o", .oper_1_value =   0, .oper_2_prefix =  "w", .oper_2_value =   2}); ix += 1;
    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "w",   .{.oper_1_prefix =  "#", .oper_1_value =  32}); ix += 2;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "m",   .{.oper_1_prefix =  "o", .oper_1_value =   0, .oper_2_prefix =  "w", .oper_2_value =   3}); ix += 1;
    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "w",   .{.oper_1_prefix =  "#", .oper_1_value =  32}); ix += 2;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "m",   .{.oper_1_prefix =  "o", .oper_1_value =   0, .oper_2_prefix =  "w", .oper_2_value =   4}); ix += 1;
    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "w",   .{.oper_1_prefix =  "#", .oper_1_value =  32}); ix += 2;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "m",   .{.oper_1_prefix =  "o", .oper_1_value =   0, .oper_2_prefix =  "w", .oper_2_value =   5}); ix += 1;
    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "w",   .{.oper_1_prefix =  "#", .oper_1_value =  32}); ix += 2;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "m",   .{.oper_1_prefix =  "o", .oper_1_value =   0, .oper_2_prefix =  "w", .oper_2_value =   6}); ix += 1;
    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "w",   .{.oper_1_prefix =  "#", .oper_1_value =  32}); ix += 2;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "bra", .{.oper_1_prefix =   "", .oper_1_value =   3}); ix += 1;

    emu.script700.label_addresses[3] = ix;

    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "m", .{.oper_1_prefix =  "#", .oper_1_value =   6, .oper_2_prefix =  "w", .oper_2_value =   0}); ix += 2;
    //sl = sb[ix..(ix+4)]; try Script700.compile_instruction(sl, "c", .{.oper_1_prefix =  "#", .oper_1_value =   3, .oper_2_prefix =  "#", .oper_2_value =  15}); ix += 4;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "nop", .{}); ix += 1;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "sw",  .{}); ix += 1;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "i",   .{}); ix += 1;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "ib",  .{}); ix += 1;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "r",   .{}); ix += 1;
    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "w",   .{.oper_1_prefix =  "#", .oper_1_value =       16}); ix += 2;
    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "m",   .{.oper_1_prefix =  "#", .oper_1_value =        4, .oper_2_prefix =  "w", .oper_2_value =   2}); ix += 2;
    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "w",   .{.oper_1_prefix =  "#", .oper_1_value =       17}); ix += 2;
    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "m",   .{.oper_1_prefix =  "#", .oper_1_value =        7, .oper_2_prefix =  "w", .oper_2_value =   3}); ix += 2;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "bp",  .{.oper_1_prefix =  "o", .oper_1_value =       10}); ix += 1;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "iw",  .{.oper_1_prefix = "rd", .oper_1_value =  0x1FEDC}); ix += 1;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "iv",  .{.oper_1_prefix = "dw", .oper_1_value = 0x1FFEDC}); ix += 1;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "wi",  .{.oper_1_prefix =  "x", .oper_1_value =     0x41}); ix += 1;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "wo",  .{.oper_1_prefix =  "l", .oper_1_value =     1026}); ix += 1;
    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "a",   .{.oper_1_prefix =  "l", .oper_1_value =     2043, .oper_2_prefix =  "l", .oper_2_value =     2043}); ix += 2;
    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "s",   .{.oper_1_prefix =  "d", .oper_1_value = 0x1FFEDC, .oper_2_prefix =  "d", .oper_2_value = 0x1FFEDC}); ix += 2;
    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "u",   .{.oper_1_prefix =  "r", .oper_1_value =  0x1FEDC, .oper_2_prefix =  "r", .oper_2_value =  0x1FEDC}); ix += 2;
    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "d",   .{.oper_1_prefix =  "x", .oper_1_value =  0x1FEDC, .oper_2_prefix =  "x", .oper_2_value =  0x1FEDC}); ix += 2;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "r0",  .{}); ix += 1;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "r1",  .{}); ix += 1;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "f",   .{}); ix += 1;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "f0",  .{}); ix += 1;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "f1",  .{}); ix += 1;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "q",   .{}); ix += 1;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "bra", .{.oper_1_prefix =  "", .oper_1_value = 22}); ix += 1;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "bne", .{.oper_1_prefix = "#", .oper_1_value = 21}); ix += 1;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "bcs", .{.oper_1_prefix = "w", .oper_1_value =  9}); ix += 1;
    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "m", .{.oper_1_prefix =  "#", .oper_1_value =  16, .oper_2_prefix =  "i", .oper_2_value =   2}); ix += 2;

    // Test print out instructions
    for (0..ix) |i_| {
        const word = sb[i_];
        std.debug.print("[{b:0>8} {b:0>8} {b:0>8} {b:0>8}]\n", .{word >> 24, word >> 16 & 0xFF, word >> 8 & 0xFF, word & 0xFF});
    } 

    emu.script700.load_bytecode(sb[0..]);
    emu.script700.run(.{});

    // Load SPC file from path if present
    if (spc_file_path) |path| {
        var file = try std.fs.cwd().openFile(path, .{ .mode = .read_only });
        defer file.close();

        const file_size = try file.getEndPos();

        const file_alloc = std.heap.page_allocator;
        const buffer = try file_alloc.alloc(u8, file_size);
        //defer allocator.free(buffer); // The entire app appears to just die after exiting scope if this is uncommented. No idea why

        _ = try file.readAll(buffer);
        const metadata = try spc_loader.load_spc(&emu, buffer);

        std.debug.print("SPC file \"{s}\" loaded successfully!\n\n", .{path});
        try metadata.print();
    
        if (!debug_mode) {
            while (true) {
                for (0..2048000) |_| {
                    emu.step_cycle();
                }

                const l1, const r1, const l2, const r2 = emu.consume_dac_samples();
                var buf: [128000]u8 = [_]u8 {0} ** 128000;

                for (0..l1.len) |x| {
                    const a: u8 = @intCast(l1[x] & 0xFF);
                    const b: u8 = @intCast(l1[x] >>   8);
                    const c: u8 = @intCast(r1[x] & 0xFF);
                    const d: u8 = @intCast(r1[x] >>   8);

                    buf[4*x + 0] = a;
                    buf[4*x + 1] = b;
                    buf[4*x + 2] = c;
                    buf[4*x + 3] = d;
                }

                if (l2 != null and r2 != null) {
                    for (0..l2.?.len) |x| {
                        const a: u8 = @intCast(l2.?[x] & 0xFF);
                        const b: u8 = @intCast(l2.?[x] >>   8);
                        const c: u8 = @intCast(r2.?[x] & 0xFF);
                        const d: u8 = @intCast(r2.?[x] >>   8);

                        const y = x + l1.len;

                        buf[4*y + 0] = a;
                        buf[4*y + 1] = b;
                        buf[4*y + 2] = c;
                        buf[4*y + 3] = d;
                    }
                }

                const stdout_file   = std.io.getStdOut();
                var   stdout_writer = stdout_file.writer();

                try stdout_writer.writeAll(&buf);
            }
        }
    }

    std.debug.print("----------------------------------------------------------------------------------\n", .{});
    std.debug.print("Mode commands: \n", .{});
    std.debug.print("   i = Instruction trace log viewer [default] \n", .{});
    std.debug.print("   v = Memory viewer \n", .{});
    std.debug.print("   r = DSP register map viewer \n", .{});
    std.debug.print("   b = DSP debug viewer \n", .{});
    std.debug.print("   7 = Script700 debug viewer \n", .{});
    std.debug.print("Action commands: \n", .{});
    std.debug.print("   s = Step instruction [default] \n", .{});
    std.debug.print("   w = Write to IO port (snes -> spc) \n", .{});
    std.debug.print("   x = Send interrupt signal \n", .{});
    std.debug.print("   q = Run shadow code \n", .{});
    std.debug.print("   e = Exit shadow execution \n", .{});
    std.debug.print("   p = View previous page \n", .{});
    std.debug.print("   n = View next page \n", .{});
    std.debug.print("   u = Shift memory view up one row \n", .{});
    std.debug.print("   d = Shift memory view down one row \n", .{});
    std.debug.print("----------------------------------------------------------------------------------\n\n", .{});
    std.debug.print("Pressing enter without specifying the command repeats the previous action command. \n", .{});
    std.debug.print("\n", .{});

    const stdin = std.io.getStdIn().reader();
    var buffer: [8]u8 = undefined;

    emu.s_smp.enable_access_logs = true;
    emu.s_smp.enable_timer_logs = true;
    emu.s_smp.clear_access_logs();
    emu.s_smp.clear_timer_logs();

    var cur_page: u8 = 0x00;
    var cur_offset: u8 = 0x00;
    var cur_mode: u8 = 'i';
    var cur_action: u8 = 's';

    const shadow_routine: [23]u8 = [23]u8 {
        0x20,             //    clrp
        0xE5, 0x00, 0x02, //    mov a, $0200
        0xBC,             //    inc a
        0xC5, 0x00, 0x02, //    mov $0200, a
        0x8F, 0x01, 0x00, //    mov $FC, #$01 (Set timer 2 period to 1)
        0x8F, 0x84, 0x00, //    mov $F1, #$84 (Enable timer 2)
        0x8D, 0x03,       //    mov y, #$10
        0x3F, 0x8A, 0x15, //    call $158A
        0xFE, 0xFE,       // -: dbnz y, -
        0x7F,             //    reti
        0xC5,             //    mov ----, a
    };
    
    emu.s_smp.spc.upload_shadow_code(0x0200, shadow_routine[0..]);

    //emu.s_dsp.audio_ram[0x0002] = 0x40;
    //emu.s_dsp.audio_ram[0x0003] = 0x00;
    //emu.s_dsp.audio_ram[0x0004] = 0x20;
    //emu.s_dsp.audio_ram[0x0005] = 0x8F;
    //emu.s_dsp.audio_ram[0x0006] = 0x01;
    //emu.s_dsp.audio_ram[0x0007] = 0xFC;
    //emu.s_dsp.audio_ram[0x0008] = 0x8F;
    //emu.s_dsp.audio_ram[0x0009] = 0x84;
    //emu.s_dsp.audio_ram[0x000A] = 0xF1;

    while (true) {
        _ = stdin.readUntilDelimiterOrEof(buffer[0..], '\n') catch "";
        std.debug.print("\x1B[A\x1B[A", .{}); // ANSI escape code for cursor up (may not work on Windows)
        //std.debug.print("\x1B[A", .{}); // ANSI escape code for cursor up (may not work on Windows)

        const last_cycle = emu.s_dsp.cur_cycle();
        const last_pc    = emu.s_smp.spc.pc();
        const prev_state = emu.s_smp.state;

        sw: switch (std.ascii.toLower(buffer[0])) {
            'n' => {
                cur_action = 'n';
                cur_page +%= 1;
                if (cur_mode == 'v') {
                    std.debug.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
                    db.print_memory_page(&emu, cur_page, cur_offset, .{});
                }
            },
            'p' => {
                cur_action = 'p';
                cur_page -%= 1;
                if (cur_mode == 'v') {
                    std.debug.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
                    db.print_memory_page(&emu, cur_page, cur_offset, .{});
                }
            },
            'd' => {
                cur_action = 'd';
                if (cur_offset > 0xEF) {
                    cur_page +%= 1;
                }
                cur_offset +%= 0x10;
                if (cur_mode == 'v') {
                    std.debug.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
                    db.print_memory_page(&emu, cur_page, cur_offset, .{});
                }
            },
            'u' => {
                cur_action = 'u';
                if (cur_offset < 0x10) {
                    cur_page -%= 1;
                }
                cur_offset -%= 0x10;
                if (cur_mode == 'v') {
                    std.debug.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
                    db.print_memory_page(&emu, cur_page, cur_offset, .{});
                }
            },
            'i' => {
                cur_mode = 'i';
                std.debug.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
            },
            'v' => {
                cur_mode = 'v';
                std.debug.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
                db.print_memory_page(&emu, cur_page, cur_offset, .{});
            },
            'r' => {
                cur_mode = 'r';
                std.debug.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
                db.print_dsp_map(&emu, .{.is_dsp = true});

                std.debug.print("\n", .{});
                db.print_dsp_state(&emu, .{.is_dsp = true});
            },
            'b' => {
                cur_mode = 'b';
                std.debug.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
                db.print_dsp_debug_state(&emu, .{.is_dsp = true});
            },
            '7' => {
                cur_mode = '7';
                std.debug.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
                db.print_script700_state(&emu);
            },
            'w' => {
                cur_action = 'w';

                std.debug.print("\nEnter APU IO port and byte value. Format: PP XX (Example: F4 0A)\n", .{});
                _ = stdin.readUntilDelimiterOrEof(buffer[0..], '\n') catch "";

                const port_num = std.fmt.parseInt(u8, buffer[0..2], 16) catch 0x00;
                const value    = std.fmt.parseInt(u8, buffer[3..5], 16) catch null;

                if (port_num >= 0xF4 and port_num <= 0xF7 and value != null) {
                    const v = value.?;
                    const prev_port_val = emu.s_smp.state.input_ports[port_num - 0xF4];

                    emu.s_smp.receive_port_value(@intCast(port_num - 0xF4), v);

                    std.debug.print("\x1B[34m", .{});
                    std.debug.print("[{d}]\t {s}: ", .{emu.s_dsp.last_processed_cycle, "receive"});
                    std.debug.print("[{X:0>4}]={X:0>2}->{X:0>2}", .{port_num, prev_port_val, v});
                    std.debug.print("\x1B[0m\n", .{});
                }
            },
            'x' => {
                emu.s_smp.trigger_interrupt(null);

                std.debug.print("\n\x1B[34m", .{});
                std.debug.print("[{d}]\t {s}: ", .{emu.s_dsp.last_processed_cycle, "receive interrupt"});
                std.debug.print("\x1B[0m\n", .{});
            },
            'q' => {
                emu.enable_shadow_mode(.{.set_as_master = true});

                std.debug.print("\n\x1B[34m", .{});
                std.debug.print("[{d}]\t {s}: ", .{emu.s_dsp.last_processed_cycle, "entering shadow mode"});
                std.debug.print("\x1B[0m\n", .{});
            },
            'e' => {
                emu.disable_shadow_execution(.{.force_exit = true});

                std.debug.print("\n\x1B[34m", .{});
                std.debug.print("[{d}]\t {s}: ", .{emu.s_dsp.last_processed_cycle, "exiting shadow execution"});
                std.debug.print("\x1B[0m\n", .{});
            },
            's' => {
                cur_action = 's';
                // Default behavior: Step instruction

                if (cur_mode == 'i') {
                    db.print_pc(&emu);
                    std.debug.print(" |  ", .{});
                    db.print_opcode(&emu);
                    std.debug.print("  ", .{});
                }

                emu.step_instruction();

                const all_logs = emu.s_smp.get_access_logs_range(last_cycle);
                var logs = db.filter_access_logs(all_logs);

                //var buffer_writer = std.io.countingWriter(std.io.getStdOut().writer());
                //var writer = buffer_writer.writer();
                //
                //for (all_logs) |log| {
                //    _ = db.print_log(&prev_state, &log, &writer, .{}) catch 0;
                //    std.debug.print("\n", .{});
                //}

                //emu.s_smp.clear_access_logs();

                if (cur_mode == 'i') {
                    try db.print_logs(&prev_state, logs[0..]);

                    db.print_spc_state(&emu);
                    std.debug.print(" | ", .{});
                    db.print_dsp_cycle(&emu);
                    std.debug.print("\n", .{});

                    // Print timer logs after instruction log
                    const all_timer_logs = emu.s_smp.get_timer_logs(.{});
                    const timer_logs = db.filter_timer_logs(all_timer_logs);

                    emu.s_smp.clear_timer_logs();

                    for (timer_logs) |log| {
                        if (log == null) {
                            break;
                        }
                        std.debug.print("\x1B[32m", .{});
                        db.print_timer_log(&log.?, .{ .prefix =true });
                        std.debug.print("\x1B[39m\n", .{});
                    }
                }
                else if (cur_mode == 'v') {
                    std.debug.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
                    db.print_memory_page(&emu, cur_page, cur_offset, .{.prev_pc = last_pc, .prev_state = &prev_state, .logs = all_logs});
                }
                else if (cur_mode == 'r') {
                    std.debug.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
                    db.print_dsp_map(&emu, .{.is_dsp = true, .prev_pc = last_pc, .prev_state = &prev_state, .logs = all_logs});

                    std.debug.print("\n", .{});
                    db.print_dsp_state(&emu, .{.is_dsp = true, .prev_pc = last_pc, .prev_state = &prev_state, .logs = all_logs});
                }
                else if (cur_mode == 'b') {
                    std.debug.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
                    db.print_dsp_debug_state(&emu, .{.is_dsp = true, .prev_pc = last_pc, .prev_state = &prev_state, .logs = all_logs});
                }
                else if (cur_mode == '7') {
                    std.debug.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
                    db.print_script700_state(&emu);
                }
            },
            else => {
                continue :sw cur_action;
            }
        }
        std.debug.print("Current DSP cycle: {d}\n", .{emu.s_dsp.cur_cycle()});

        //std.debug.print("\n", .{});
    }

    emu.event_loop();

    //std.debug.print("Hello, {d}!\n", .{emu.s_smp.?.boot_rom.len});
//
    //for (emu.s_smp.?.boot_rom) |item| {
    //    std.debug.print("{X:0>2}\n", .{item});
    //}
//
    //std.debug.print("Hello, {d}!\n", .{SDSP.gauss_table.len});
//
    //for (SDSP.gauss_table) |item| {
    //    std.debug.print("{X:0>4}\n", .{item});
    //}
}
