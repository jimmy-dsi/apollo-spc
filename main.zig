const std = @import("std");
const builtin = @import("builtin");
const Atomic = std.atomic.Value;

const db = @import("debug.zig");

const Emu          = @import("emu.zig").Emu;
const SDSP         = @import("s_dsp.zig").SDSP;
const SSMP         = @import("s_smp.zig").SSMP;
const Script700    = @import("script700.zig").Script700;
const SongMetadata = @import("song_metadata.zig").SongMetadata;

const spc_loader = @import("spc_loader.zig");

const max_consecutive_timeouts: u32 = 90;
const busyloop_relief_ms:       u32 = 20;

var t_started      = Atomic(bool).init(false);
var break_signal   = Atomic(bool).init(false);
var is_breakpoint  = Atomic(bool).init(false);
var t_timeout_wait = Atomic(bool).init(false);
var t_menu_mode    = Atomic(u8).init('i');
var t_input_mode   = Atomic(u32).init(0);

var m_expect_input = std.Thread.Mutex{};

var stdout_file: std.fs.File = undefined;

pub fn main() !void {
    const stdout = std.io.getStdOut();
    stdout_file = stdout;

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

    sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "bp",  .{.oper_1_prefix =  "",    .oper_1_value  = 0x7B8}); ix += 2;
    //emu.script700.label_addresses[0] = ix;
    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "m",   .{.oper_1_prefix =  "#",   .oper_1_value  =    0, .oper_2_prefix =  "w", .oper_2_value  =   0}); ix += 2;
    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "w",   .{.oper_1_prefix =  "#",   .oper_1_value  =  64}); ix += 2;
    //emu.script700.label_addresses[1] = ix;
    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "a",   .{.oper_1_prefix =  "#",   .oper_1_value  =    1, .oper_2_prefix =  "w", .oper_2_value  =   0}); ix += 2;
    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "c",   .{.oper_1_prefix =  "#",   .oper_1_value  =    0x3FFFFFF, .oper_2_prefix =  "w", .oper_2_value  =  0}); ix += 2;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "blt", .{.oper_1_prefix =   "",   .oper_1_value  =    1}); ix += 1;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "bra", .{.oper_1_prefix =   "",   .oper_1_value  =    0}); ix += 1;
    sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "q", .{}); ix += 1;

    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "q", .{}); ix += 1;

    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "m",   .{.oper_1_prefix =  "#", .oper_1_value = 6000, .oper_2_prefix =  "w", .oper_2_value =   0}); ix += 2;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "r0",  .{}); ix += 1;
    //emu.script700.label_addresses[0] = ix;
    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "w",   .{.oper_1_prefix =  "#",   .oper_1_value  = 2048}); ix += 2;
    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "s",   .{.oper_1_prefix =  "#",   .oper_1_value  =    1, .oper_2_prefix =  "w", .oper_2_value  =  0}); ix += 2;
    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "c",   .{.oper_1_prefix =  "#",   .oper_1_value  =    0, .oper_2_prefix =  "w", .oper_2_value  =  0}); ix += 2;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "bne", .{.oper_1_prefix =   "",   .oper_1_value  =    0}); ix += 1;
    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "m",   .{.oper_1_prefix =  "#",   .oper_1_value = 2, .oper_2_prefix =  "i", .oper_2_value =   1}); ix += 2;
    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "w",   .{.oper_1_prefix =  "#",   .oper_1_value  = 4096000}); ix += 2;
    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "m",   .{.oper_1_prefix =  "#",   .oper_1_value = 0x2B, .oper_2_prefix =  "i", .oper_2_value =   0}); ix += 2;
    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "w",   .{.oper_1_prefix =  "#",   .oper_1_value  = 4096000}); ix += 2;
    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "m",   .{.oper_1_prefix =  "#",   .oper_1_value = 0x00, .oper_2_prefix =  "i", .oper_2_value =   0}); ix += 2;
    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "w",   .{.oper_1_prefix =  "#",   .oper_1_value  = 4096000}); ix += 2;
    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "m",   .{.oper_1_prefix =  "#",   .oper_1_value = 0x2B, .oper_2_prefix =  "i", .oper_2_value =   0}); ix += 2;
    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "w",   .{.oper_1_prefix =  "#",   .oper_1_value  = 4096000}); ix += 2;
    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "m",   .{.oper_1_prefix =  "#",   .oper_1_value = 0x2A, .oper_2_prefix =  "i", .oper_2_value =   0}); ix += 2;

    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "m",   .{.oper_1_prefix =  "#", .oper_1_value =   1, .oper_2_prefix =  "w", .oper_2_value =   0}); ix += 2;
    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "m",   .{.oper_1_prefix =  "#", .oper_1_value =   0, .oper_2_prefix =  "w", .oper_2_value =   1}); ix += 2;
    //emu.script700.label_addresses[0] = ix;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "c",   .{.oper_1_prefix =  "w",   .oper_1_value  =    1, .oper_2_prefix = "#?"}); ix += 1;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "m",   .{.oper_1_prefix =  "db?", .oper_2_prefix =  "w", .oper_2_value  =    2}); ix += 1;
    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "a",   .{.oper_1_prefix =  "#",   .oper_1_value  =    1, .oper_2_prefix =  "w", .oper_2_value =   1}); ix += 2;
    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "c",   .{.oper_1_prefix =  "#",   .oper_1_value  = 0xFF, .oper_2_prefix =  "w", .oper_2_value =   2}); ix += 2;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "bne", .{.oper_1_prefix =   "",   .oper_1_value  =    1}); ix += 1;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "c",   .{.oper_1_prefix =  "w",   .oper_1_value  =    1, .oper_2_prefix = "#?"}); ix += 1;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "m",   .{.oper_1_prefix =  "dd?", .oper_2_prefix =  "w", .oper_2_value  =    1}); ix += 1;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "bra", .{.oper_1_prefix =   "",   .oper_1_value  =    0}); ix += 1;
    //emu.script700.label_addresses[1] = ix;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "c",   .{.oper_1_prefix =  "w",   .oper_1_value  =    1, .oper_2_prefix = "#?"}); ix += 1;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "m",   .{.oper_1_prefix =  "db?", .oper_2_prefix =  "w", .oper_2_value  =    3}); ix += 1;
    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "a",   .{.oper_1_prefix =  "#",   .oper_1_value  =    1, .oper_2_prefix =  "w", .oper_2_value =   1}); ix += 2;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "c",   .{.oper_1_prefix =  "w",   .oper_1_value  =    1, .oper_2_prefix = "#?"}); ix += 1;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "m",   .{.oper_1_prefix =  "dd?", .oper_2_prefix =  "w", .oper_2_value  =    4}); ix += 1;
    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "a",   .{.oper_1_prefix =  "#",   .oper_1_value  =    4, .oper_2_prefix =  "w", .oper_2_value  =   1}); ix += 2;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "m",   .{.oper_1_prefix =  "w",   .oper_1_value  =    3, .oper_2_prefix =   "", .oper_2_value  =   1}); ix += 1;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "m",   .{.oper_1_prefix =  "w",   .oper_1_value  =    2, .oper_2_prefix =   "", .oper_2_value  =   2}); ix += 1;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "m",   .{.oper_1_prefix =  "w",   .oper_1_value  =    0, .oper_2_prefix =   "", .oper_2_value  =   0}); ix += 1;
    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "n",   .{.oper_1_prefix =  "#",   .oper_1_value  =    1, .operator      =  '^', .oper_2_prefix = "w", .oper_2_value = 0}); ix += 2;
    ////sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "bp",  .{.oper_1_prefix =  "",    .oper_1_value  = 0x35}); ix += 2;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "w",   .{.oper_1_prefix =  "w",   .oper_1_value  =    4}); ix += 1;
    //sl = sb[ix..(ix+1)]; try Script700.compile_instruction(sl, "bra", .{.oper_1_prefix =   "",   .oper_1_value  =    0}); ix += 1;

    var data = blk: {
        const embed_u8 = @embedFile("data/c700-test.bin");
        var table_u8: [embed_u8.len] u8 = undefined;
    
        @setEvalBranchQuota(embed_u8.len);
    
        for (0..embed_u8.len) |idx| {
            const b = embed_u8[idx];
            table_u8[idx] = b;
        }
    
        break :blk table_u8;
    };

    var script700_load_error: ?anyerror = null;

    emu.script700.load_bytecode(sb[0..]) catch |e| {
        script700_load_error = e;
    };
    if (script700_load_error == null) {
        emu.script700.load_data(data[0..]);
        try emu.script700.run(.{});
    }

    var t_break_listener = try std.Thread.spawn(.{}, break_listener, .{});
    defer t_break_listener.join();

    var metadata: ?SongMetadata = null;

    var cur_page: u8 = 0x00;
    var cur_offset: u8 = 0x00;
    var cur_mode: u8 = 'i';
    var cur_action: u8 = 's';

    // Load SPC file from path if present
    if (spc_file_path) |path| {
        var file = try std.fs.cwd().openFile(path, .{ .mode = .read_only });
        defer file.close();

        const file_size = try file.getEndPos();

        const file_alloc = std.heap.page_allocator;
        const buffer = try file_alloc.alloc(u8, file_size);
        //defer allocator.free(buffer); // The entire app appears to just die after exiting scope if this is uncommented. No idea why

        _ = try file.readAll(buffer);
        metadata = try spc_loader.load_spc(&emu, buffer);

        std.debug.print("SPC file \"{s}\" loaded successfully!\n\n", .{path});
        try metadata.?.print();

        if (script700_load_error) |err| {
            report_error(err, true);
        }
    
        if (!debug_mode) {
            // Output 1.25 second of blank audio first, to compensate for ffplay nobuffer option
            for (0..40) |_| {
                var stdout_writer = stdout_file.writer();
                try stdout_writer.writeAll(&buf);
            }
            
            cur_action = 'c';
        }
    }

    //std.debug.print("----------------------------------------------------------------------------------\n", .{});
    //std.debug.print("Mode commands: \n", .{});
    //std.debug.print("   i = Instruction trace log viewer [default] \n", .{});
    //std.debug.print("   v = Memory viewer \n", .{});
    //std.debug.print("   r = DSP register map viewer \n", .{});
    //std.debug.print("   b = DSP debug viewer \n", .{});
    //std.debug.print("   7 = Script700 debug viewer \n", .{});
    //std.debug.print("Action commands: \n", .{});
    //std.debug.print("   s = Step instruction [default] \n", .{});
    //std.debug.print("   c = Continue to next breakpoint \n", .{});
    //std.debug.print("   k = Break execution \n", .{});
    //std.debug.print("   w = Write to IO port (snes -> spc) \n", .{});
    //std.debug.print("   x = Send interrupt signal \n", .{});
    //std.debug.print("   q = Run shadow code \n", .{});
    //std.debug.print("   e = Exit shadow execution \n", .{});
    //std.debug.print("   p = View previous page \n", .{});
    //std.debug.print("   n = View next page \n", .{});
    //std.debug.print("   u = Shift memory view up one row \n", .{});
    //std.debug.print("   d = Shift memory view down one row \n", .{});
    //std.debug.print("----------------------------------------------------------------------------------\n\n", .{});
    //std.debug.print("Pressing enter without specifying the command repeats the previous action command. \n", .{});
    //std.debug.print("\n", .{});

    const stdin = std.io.getStdIn().reader();
    var buffer: [8]u8 = undefined;

    emu.s_smp.enable_access_logs = true;
    emu.s_smp.enable_timer_logs = true;
    emu.s_smp.clear_access_logs();
    emu.s_smp.clear_timer_logs();

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

    while (true) {
        if (cur_action == 'c') {
            const m = t_menu_mode.load(std.builtin.AtomicOrder.seq_cst);
            switch (m) {
                'n' => {
                    cur_page +%= 1;
                    t_menu_mode.store(cur_mode, std.builtin.AtomicOrder.seq_cst);
                },
                'p' => {
                    cur_page -%= 1;
                    t_menu_mode.store(cur_mode, std.builtin.AtomicOrder.seq_cst);
                },
                'd' => {
                    if (cur_offset > 0xEF) {
                        cur_page +%= 1;
                    }
                    cur_offset +%= 0x10;
                    t_menu_mode.store(cur_mode, std.builtin.AtomicOrder.seq_cst);
                },
                'u' => {
                    if (cur_offset < 0x10) {
                        cur_page -%= 1;
                    }
                    cur_offset -%= 0x10;
                    t_menu_mode.store(cur_mode, std.builtin.AtomicOrder.seq_cst);
                },
                else => {
                    cur_mode = m;
                }
            }
            buffer[0] = 'c';
        }
        else {
            const bp_hit = is_breakpoint.load(std.builtin.AtomicOrder.seq_cst);
            if (bp_hit) {
                var signal = break_signal.load(std.builtin.AtomicOrder.seq_cst);
                while (!signal) {
                    signal = break_signal.load(std.builtin.AtomicOrder.seq_cst);
                }

                is_breakpoint.store(false, std.builtin.AtomicOrder.seq_cst);
            }
            
            _ = stdin.readUntilDelimiterOrEof(buffer[0..], '\n') catch "";
            std.debug.print("\x1B[A\x1B[A", .{}); // ANSI escape code for cursor up (may not work on Windows)

            if (std.ascii.toLower(buffer[0]) == 'c') {
                std.debug.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
                if (cur_mode == 'i' and metadata != null) {
                    try metadata.?.print();
                }
            }
        }

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
            'c' => {
                t_started.store(true, std.builtin.AtomicOrder.seq_cst);
                cur_action = 'c';

                var s7en = emu.script700.enabled;

                var res = run_loop(&emu) catch null;
                var attempts: u32 = 0;

                while (res == null) {
                    std.time.sleep(busyloop_relief_ms * std.time.ns_per_ms);
                    attempts += 1;

                    if (attempts == max_consecutive_timeouts) {
                        report_timeout();
                        attempts = 0;

                        if (!t_timeout_wait.load(std.builtin.AtomicOrder.seq_cst)) {
                            emu.script700.enabled = false;
                        }
                    }

                    res = run_loop(&emu) catch null;
                }

                if (s7en and emu.script700_error != null) {
                    const err = emu.script700_error.?;
                    s7en = false;
                    report_error(err, false);
                }

                break_signal.store(false, std.builtin.AtomicOrder.seq_cst);

                if (!res.?) {
                    t_started.store(false, std.builtin.AtomicOrder.seq_cst);
                    std.debug.print("Breakpoint hit. Press enter\n", .{});
                    cur_action = 's';
                }
                else {
                    t_started.store(true, std.builtin.AtomicOrder.seq_cst);
                }

                if (cur_mode == 'v') {
                    std.debug.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
                    db.print_memory_page(&emu, cur_page, cur_offset, .{.prev_pc = emu.s_smp.spc.pc(), .prev_state = &emu.s_smp.state});
                }
                else if (cur_mode == 'r') {
                    std.debug.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
                    db.print_dsp_map(&emu, .{.is_dsp = true, .prev_pc = emu.s_smp.spc.pc(), .prev_state = &emu.s_smp.state});

                    std.debug.print("\n", .{});
                    db.print_dsp_state(&emu, .{.is_dsp = true, .prev_pc = emu.s_smp.spc.pc(), .prev_state = &emu.s_smp.state});
                }
                else if (cur_mode == 'b') {
                    std.debug.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
                    db.print_dsp_debug_state(&emu, .{.is_dsp = true, .prev_pc = emu.s_smp.spc.pc(), .prev_state = &emu.s_smp.state});
                }
                else if (cur_mode == '7') {
                    std.debug.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
                    db.print_script700_state(&emu);
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

                var buffer_list = std.ArrayList(u8).init(allocator);
                defer buffer_list.deinit();

                var buffer_writer = std.io.bufferedWriter(buffer_list.writer());
                var writer = buffer_writer.writer();

                if (cur_mode == 'i') {
                    try db.print_pc(&emu, writer);
                    try writer.print(" |  ", .{});
                    try db.print_opcode(&emu, writer);
                    try writer.print("  ", .{});
                    try buffer_writer.flush();
                }

                var s7en = emu.script700.enabled;
                var attempts: u32 = 0;

                var is_error = false;
                emu.step_instruction() catch {
                    is_error = true;
                };

                while (is_error) {
                    std.time.sleep(busyloop_relief_ms * std.time.ns_per_ms);
                    attempts += 1;

                    if (attempts == max_consecutive_timeouts) {
                        report_timeout();
                        attempts = 0;

                        if (!t_timeout_wait.load(std.builtin.AtomicOrder.seq_cst)) {
                            emu.script700.enabled = false;
                        }
                    }

                    is_error = false;
                    emu.step_instruction() catch {
                        is_error = true;
                    };
                }

                if (s7en and emu.script700_error != null) {
                    s7en = false;
                    const err = emu.script700_error.?;
                    report_error(err, false);
                }

                const all_logs = emu.s_smp.get_access_logs_range(last_cycle);
                var logs = db.filter_access_logs(all_logs);

                if (cur_mode == 'i') {
                    std.debug.print("{s}", .{buffer_list.items});
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

        if (cur_action != 'c' or cur_mode != 'i') {
            std.debug.print("Current DSP cycle: {d}\n", .{emu.s_dsp.cur_cycle()});
        }

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

const samples = 1000;
var buf: [samples * 4]u8 = [_]u8 {0} ** (samples * 4);
var stream_start: u32 = 0;

fn run_loop(emu: *Emu) !bool {
    const cycles = samples * 64;

    is_breakpoint.store(false, std.builtin.AtomicOrder.seq_cst);

    if (emu.script700.enabled) {
        for (stream_start..cycles) |i| {
            emu.step_cycle_safe() catch |e| {
                stream_start = @intCast(i);
                return e;
            };

            if (emu.break_check()) {
                is_breakpoint.store(true, std.builtin.AtomicOrder.seq_cst);
                stream_start = @intCast(i);
                return false;
            }
        }
    }
    else {
        for (stream_start..cycles) |i| {
            emu.step_cycle_fast();
            if (emu.break_check()) {
                is_breakpoint.store(true, std.builtin.AtomicOrder.seq_cst);
                stream_start = @intCast(i);
                return false;
            }
        }
    }

    const l1, const r1, const l2, const r2 = emu.view_dac_samples(samples);

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

    var stdout_writer = stdout_file.writer();

    try stdout_writer.writeAll(&buf);
    stream_start = 0;

    const signal = break_signal.load(std.builtin.AtomicOrder.seq_cst);
    return !signal;
}

fn break_listener() void {
    var prev_input: u8 = 'k';

    while (true) {
        // Wait until main thread starts playing
        var started = t_started.load(std.builtin.AtomicOrder.seq_cst);
        while (!started) {
            started = t_started.load(std.builtin.AtomicOrder.seq_cst);
        }

        var buffer: [8]u8 = undefined;
        const stdin = std.io.getStdIn().reader();

        if (m_expect_input.tryLock()) {
            buffer[0] = ' ';
            _ = stdin.readUntilDelimiterOrEof(buffer[0..], '\n') catch "";

            switch (t_input_mode.load(std.builtin.AtomicOrder.seq_cst)) {
                0 => {
                    var cur_mode = t_menu_mode.load(std.builtin.AtomicOrder.seq_cst);
                    sw: switch (buffer[0]) {
                        'i', 'v', 'r', 'b', '7', 'u', 'd', 'n', 'p' => {
                            cur_mode   = buffer[0];
                            prev_input = buffer[0];
                        },
                        'k' => {
                            const bp_hit = is_breakpoint.load(std.builtin.AtomicOrder.seq_cst);
                            if (bp_hit) {
                                //std.debug.print("\x1B[A\x1B[A\x1B[A", .{});
                                //std.debug.print("                                                                                  \n", .{});
                                //std.debug.print("                                                                                  \n", .{});
                                //std.debug.print("                                                                                  \n", .{});
                                //std.debug.print("\x1B[A\x1B[A", .{});
                                //std.debug.print("\x1B[A\x1B[A", .{});
                            }
                            else {
                                //std.debug.print("\x1B[A\x1B[A", .{});
                                //std.debug.print("                                                                                  \n", .{});
                            }

                            break_signal.store(true, std.builtin.AtomicOrder.seq_cst);
                            prev_input = buffer[0];
                        },
                        else => {
                            buffer[0] = prev_input;
                            continue :sw prev_input;
                        }
                    }

                    t_menu_mode.store(cur_mode, std.builtin.AtomicOrder.seq_cst);
                },
                1 => {
                    sw: switch (std.ascii.toLower(buffer[0])) {
                        'w' => {
                            t_timeout_wait.store(true, std.builtin.AtomicOrder.seq_cst);
                        },
                        'c' => {
                            t_timeout_wait.store(false, std.builtin.AtomicOrder.seq_cst);
                        },
                        'q' => {
                            stdout_file.close();
                            std.process.exit(1);
                        },
                        else => {
                            continue :sw 'w';
                        }
                    }

                    t_started.store(false, std.builtin.AtomicOrder.seq_cst);
                },
                else => unreachable
            }

            m_expect_input.unlock();
        }

        // Sleep for 200 ms to allow main thread time to block stdin waiting on this one
        std.time.sleep(200 * std.time.ns_per_ms);
    }
}

fn report_timeout() void {
    std.debug.print("\n\x1B[38;2;250;125;25mScript700 timed out. Enter one of the following:\n", .{});
    std.debug.print("----------------------------------------------------------------------------------\n", .{});
    std.debug.print("   w = Attempt wait until Script700 finishes or yields execution \n", .{});
    std.debug.print("   c = Disable Script700 and continue SPC execution \n", .{});
    std.debug.print("   q = Quit program \n", .{});
    std.debug.print("----------------------------------------------------------------------------------\x1B[39m\n", .{});

    t_input_mode.store(1, std.builtin.AtomicOrder.seq_cst);

    var buffer: [8]u8 = undefined;
    const stdin = std.io.getStdIn().reader();

    if (m_expect_input.tryLock()) {
        _ = stdin.readUntilDelimiterOrEof(buffer[0..], '\n') catch "";
        
        sw: switch (std.ascii.toLower(buffer[0])) {
            'w' => {
                t_timeout_wait.store(true, std.builtin.AtomicOrder.seq_cst);
            },
            'c' => {
                t_timeout_wait.store(false, std.builtin.AtomicOrder.seq_cst);
            },
            'q' => {
                stdout_file.close();
                std.process.exit(1);
            },
            else => {
                continue :sw 'w';
            }
        }

        t_started.store(false, std.builtin.AtomicOrder.seq_cst);
        m_expect_input.unlock();
    }

    while (!m_expect_input.tryLock()) { }
    m_expect_input.unlock();

    t_input_mode.store(0, std.builtin.AtomicOrder.seq_cst);
}

fn report_error(err: anyerror, load: bool) void {
    std.debug.print("\n\x1B[91mScript700 {s}: ", .{if (load) "load error" else "crashed"});

    switch (err) {
        error.out_of_memory => {
            std.debug.print("not enough memory to resize data area.", .{});
        },
        error.fetch_range => {
            std.debug.print("script area fetch went out of bounds.", .{});
        },
        error.bytecode_too_large => {
            std.debug.print("script area bytecode is too large.", .{});
        },
        else => {
            std.debug.print("unknown error.", .{});
        }
    }

    std.debug.print("\x1B[39m\n", .{});

    t_input_mode.store(0, std.builtin.AtomicOrder.seq_cst);
}