const std = @import("std");
const builtin = @import("builtin");
const Atomic = std.atomic.Value;

const db = @import("debug.zig");

const Emu          = @import("emu.zig").Emu;
const SDSP         = @import("s_dsp.zig").SDSP;
const SSMP         = @import("s_smp.zig").SSMP;
const SPCState     = @import("spc_state.zig").SPCState;
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
var t_other_menu   = Atomic(u8).init('m');

var m_expect_input = std.Thread.Mutex{};

var stdout_file: std.fs.File = undefined;

var metadata: ?SongMetadata = null;

pub fn main() !void {
    db.set_cli_width(131);
    std.debug.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)

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

    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "bp",  .{.oper_1_prefix =  "",    .oper_1_value  = 0x7B8}); ix += 2;
    //emu.script700.label_addresses[0] = ix;
    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "m",   .{.oper_1_prefix =  "#",   .oper_1_value  =    0, .oper_2_prefix =  "w", .oper_2_value  =   0}); ix += 2;
    //sl = sb[ix..(ix+2)]; try Script700.compile_instruction(sl, "w",   .{.oper_1_prefix =  "#",   .oper_1_value  =  2048000}); ix += 2;
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

        db.print("SPC file \"{s}\" loaded successfully!\n\n", .{path});

        show_metadata();

        if (script700_load_error) |err| {
            report_error(err, true);
        }
    
        if (!debug_mode) {
            // Output 1.25 second of blank audio first, to compensate for ffplay nobuffer option
            //for (0..40) |_| {
            //    var stdout_writer = stdout_file.writer();
            //    try stdout_writer.writeAll(&buf);
            //}
            
            cur_action = 'c';
        }
    }

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
    last_time = std.time.nanoTimestamp();

    while (true) {
        if (cur_action == 'c') {
            const m = t_menu_mode.load(std.builtin.AtomicOrder.seq_cst);
            const h = t_other_menu.load(std.builtin.AtomicOrder.seq_cst);

            if (h == 0) {
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
            }
            else {
                switch (h) {
                    'h' => {
                        show_help_menu();
                    },
                    'm' => {
                        show_metadata();
                    },
                    else => unreachable
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
            
            //std.debug.print("\x1B[A\x1B[A\x1B[9C\x1B[9C\x1B[9C\x1B[8C", .{});
            _ = stdin.readUntilDelimiterOrEof(buffer[0..], '\n') catch "";

            if (std.ascii.toLower(buffer[0]) == 'c') {
                //cur_mode = t_menu_mode.load(std.builtin.AtomicOrder.seq_cst);

                db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
                if (cur_mode == 'i') {
                    show_metadata();
                }
            }
        }

        const last_cycle = emu.s_dsp.cur_cycle();
        const last_pc    = emu.s_smp.spc.pc();
        const prev_state = emu.s_smp.state;

        sw: switch (std.ascii.toLower(buffer[0])) {
            'q' => {
                stdout_file.close();
                std.process.exit(1);
            },
            'h' => {
                flush(null, false);
                show_help_menu();
                t_other_menu.store('h', std.builtin.AtomicOrder.seq_cst);
            },
            'm' => {
                flush(null, false);
                show_metadata();
                t_other_menu.store('m', std.builtin.AtomicOrder.seq_cst);
            },
            'n' => {
                cur_action = 'n';
                cur_page +%= 1;
                if (cur_mode == 'v') {
                    db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
                    db.print_memory_page(&emu, cur_page, cur_offset, .{});
                }
            },
            'c' => {
                t_started.store(true, std.builtin.AtomicOrder.seq_cst);
                cur_action = 'c';
                cur_mode = t_menu_mode.load(std.builtin.AtomicOrder.seq_cst);

                var s7en = emu.script700.enabled;

                var res = run_loop(&emu) catch null;
                var attempts: u32 = 0;
                var step_instr: bool = false;

                if (res != null and !res.?) {
                    emu.step_instruction() catch { // Run to the end of the next instruction upon break
                        res = null;
                        step_instr = true;
                    };
                }

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

                    if (step_instr) {
                        res = false;
                        emu.step_instruction() catch { // Try run to the end of the next instruction upon break if an error has been hit
                            res = null;
                        };
                    }
                    else {
                        res = run_loop(&emu) catch null;
                    }
                }

                if (s7en and emu.script700_error != null) {
                    const err = emu.script700_error.?;
                    s7en = false;
                    report_error(err, false);
                }

                break_signal.store(false, std.builtin.AtomicOrder.seq_cst);

                if (!res.?) {
                    t_started.store(false, std.builtin.AtomicOrder.seq_cst);
                    //db.print("Breakpoint hit. Press enter\n", .{});
                    cur_action = 's';

                    if (cur_mode == 'i') {
                        const prev_spc_state = emu.s_smp.prev_spc_state;
                        print_instruction(&emu, &prev_spc_state);
                    }
                    else if (t_other_menu.load(std.builtin.AtomicOrder.seq_cst) == 'h') {
                        show_help_menu();
                    }
                    else if (t_other_menu.load(std.builtin.AtomicOrder.seq_cst) == 'm') {
                        show_metadata();
                    }
                }
                else {
                    t_started.store(true, std.builtin.AtomicOrder.seq_cst);
                }

                if (t_other_menu.load(std.builtin.AtomicOrder.seq_cst) == 0) {
                    if (cur_mode == 'v') {
                        db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
                        db.print_memory_page(&emu, cur_page, cur_offset, .{.prev_pc = emu.s_smp.spc.pc(), .prev_state = &emu.s_smp.state});
                        t_other_menu.store(0, std.builtin.AtomicOrder.seq_cst);
                    }
                    else if (cur_mode == 'r') {
                        db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
                        db.print_dsp_map(&emu, .{.is_dsp = true, .prev_pc = emu.s_smp.spc.pc(), .prev_state = &emu.s_smp.state});

                        db.print("\n", .{});
                        db.print_dsp_state(&emu, .{.is_dsp = true, .prev_pc = emu.s_smp.spc.pc(), .prev_state = &emu.s_smp.state});

                        t_other_menu.store(0, std.builtin.AtomicOrder.seq_cst);
                    }
                    else if (cur_mode == 'e') {
                        db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
                        db.print_dsp_map(&emu, .{.is_dsp = true, .prev_pc = emu.s_smp.spc.pc(), .prev_state = &emu.s_smp.state});

                        db.print("\n", .{});
                        db.print_dsp_state_2(&emu, .{.is_dsp = true, .prev_pc = emu.s_smp.spc.pc(), .prev_state = &emu.s_smp.state});

                        t_other_menu.store(0, std.builtin.AtomicOrder.seq_cst);
                    }
                    else if (cur_mode == 'b') {
                        db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
                        db.print_dsp_debug_state(&emu, .{.is_dsp = true, .prev_pc = emu.s_smp.spc.pc(), .prev_state = &emu.s_smp.state});
                        t_other_menu.store(0, std.builtin.AtomicOrder.seq_cst);
                    }
                    else if (cur_mode == '7') {
                        db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
                        db.print_script700_state(&emu);
                        t_other_menu.store(0, std.builtin.AtomicOrder.seq_cst);
                    }
                }
            },
            'p' => {
                cur_action = 'p';
                cur_page -%= 1;
                if (cur_mode == 'v') {
                    db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
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
                    db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
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
                    db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
                    db.print_memory_page(&emu, cur_page, cur_offset, .{});
                }
            },
            'i' => {
                if (cur_mode != 'i') {
                    db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
                    flush(null, true);
                }

                if (cur_action != 'c') {
                    const prev_spc_state = emu.s_smp.prev_spc_state;
                    print_instruction(&emu, &prev_spc_state);

                    cur_mode = 'i';
                    t_other_menu.store(0, std.builtin.AtomicOrder.seq_cst);
                }
            },
            'v' => {
                cur_mode = 'v';
                t_menu_mode.store(cur_mode, std.builtin.AtomicOrder.seq_cst);
                t_other_menu.store(0, std.builtin.AtomicOrder.seq_cst);
                db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
                db.print_memory_page(&emu, cur_page, cur_offset, .{});
                
                flush(null, true);
            },
            'r' => {
                cur_mode = 'r';
                t_menu_mode.store(cur_mode, std.builtin.AtomicOrder.seq_cst);
                t_other_menu.store(0, std.builtin.AtomicOrder.seq_cst);
                db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
                db.print_dsp_map(&emu, .{.is_dsp = true});

                db.print("\n", .{});
                db.print_dsp_state(&emu, .{.is_dsp = true});

                flush(null, true);
            },
            'e' => {
                cur_mode = 'e';
                t_menu_mode.store(cur_mode, std.builtin.AtomicOrder.seq_cst);
                t_other_menu.store(0, std.builtin.AtomicOrder.seq_cst);
                db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
                db.print_dsp_map(&emu, .{.is_dsp = true});

                db.print("\n", .{});
                db.print_dsp_state_2(&emu, .{.is_dsp = true});

                flush(null, true);
            },
            'b' => {
                cur_mode = 'b';
                t_menu_mode.store(cur_mode, std.builtin.AtomicOrder.seq_cst);
                t_other_menu.store(0, std.builtin.AtomicOrder.seq_cst);
                db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
                db.print_dsp_debug_state(&emu, .{.is_dsp = true});
                flush(null, true);
            },
            '7' => {
                cur_mode = '7';
                t_menu_mode.store(cur_mode, std.builtin.AtomicOrder.seq_cst);
                t_other_menu.store(0, std.builtin.AtomicOrder.seq_cst);
                db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
                db.print_script700_state(&emu);
                flush(null, true);
            },
            'x' => {
                emu.s_smp.trigger_interrupt(null);

                db.print("\n\x1B[34m", .{});
                db.print("[{d}]\t {s}: ", .{emu.s_dsp.last_processed_cycle, "receive interrupt"});
                db.print("\x1B[0m\n", .{});
            },
            's' => {
                cur_action = 's';
                // Default behavior: Step instruction

                if (cur_mode == 'i') {
                    const prev_spc_state = emu.s_smp.prev_spc_state;

                    const prev_logs = emu.s_smp.get_access_logs_range(last_cycle);
                    var logs = db.filter_access_logs(prev_logs);

                    db.move_cursor_up();
                    
                    db.print_pc(prev_spc_state.pc);
                    db.print(" |  ", .{});
                    db.print_opcode(&emu, prev_spc_state.pc);
                    db.print("  ", .{});
                    try db.print_logs(&prev_state, logs[0..]);

                    db.print_spc_state(&prev_spc_state);
                    db.print("\n", .{});

                    print_instruction(&emu, &emu.s_smp.spc.state);
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

                    if (!is_error) {
                        db.print("\x1B[2J\x1B[H", .{});
                    }
                }

                if (s7en and emu.script700_error != null) {
                    s7en = false;
                    const err = emu.script700_error.?;
                    report_error(err, false);
                }

                const all_logs = emu.s_smp.get_access_logs_range(last_cycle);
                //var logs = db.filter_access_logs(all_logs);

                if (cur_mode == 'i') {
                    //try db.print_logs(&prev_state, logs[0..]);

                    // Print timer logs after instruction log
                    //const all_timer_logs = emu.s_smp.get_timer_logs(.{});
                    //const timer_logs = db.filter_timer_logs(all_timer_logs);

                    emu.s_smp.clear_timer_logs();

                    //for (timer_logs) |log| {
                    //    if (log == null) {
                    //        break;
                    //    }
                    //    db.print("\x1B[32m", .{});
                    //    //db.print_timer_log(&log.?, .{ .prefix =true });
                    //    db.print("\x1B[39m\n", .{});
                    //}
                }
                else if (cur_mode == 'v') {
                    db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
                    db.print_memory_page(&emu, cur_page, cur_offset, .{.prev_pc = last_pc, .prev_state = &prev_state, .logs = all_logs});
                    flush(null, true);
                }
                else if (cur_mode == 'r') {
                    db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
                    db.print_dsp_map(&emu, .{.is_dsp = true, .prev_pc = last_pc, .prev_state = &prev_state, .logs = all_logs});

                    db.print("\n", .{});
                    db.print_dsp_state(&emu, .{.is_dsp = true, .prev_pc = last_pc, .prev_state = &prev_state, .logs = all_logs});

                    flush(null, true);
                }
                else if (cur_mode == 'e') {
                    db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
                    db.print_dsp_map(&emu, .{.is_dsp = true, .prev_pc = last_pc, .prev_state = &prev_state, .logs = all_logs});

                    db.print("\n", .{});
                    db.print_dsp_state_2(&emu, .{.is_dsp = true, .prev_pc = last_pc, .prev_state = &prev_state, .logs = all_logs});

                    flush(null, true);
                }
                else if (cur_mode == 'b') {
                    db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
                    db.print_dsp_debug_state(&emu, .{.is_dsp = true, .prev_pc = last_pc, .prev_state = &prev_state, .logs = all_logs});
                    flush(null, true);
                }
                else if (cur_mode == '7') {
                    db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
                    db.print_script700_state(&emu);
                    flush(null, true);
                }
            },
            else => {
                continue :sw cur_action;
            }
        }
    }
}

const samples = 1000;
var buf: [samples * 4]u8 = [_]u8 {0} ** (samples * 4);
var stream_start: u32 = 0;

var last_time: i128 = 0;

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
        const l1_: []u16 = @ptrCast(l1);
        const r1_: []u16 = @ptrCast(r1);

        const a: u8 = @intCast(l1_[x] & 0xFF);
        const b: u8 = @intCast(l1_[x] >>   8);
        const c: u8 = @intCast(r1_[x] & 0xFF);
        const d: u8 = @intCast(r1_[x] >>   8);

        buf[4*x + 0] = a;
        buf[4*x + 1] = b;
        buf[4*x + 2] = c;
        buf[4*x + 3] = d;
    }

    if (l2 != null and r2 != null) {
        for (0..l2.?.len) |x| {
            const l2_: []u16 = @ptrCast(l2.?);
            const r2_: []u16 = @ptrCast(r2.?);

            const a: u8 = @intCast(l2_[x] & 0xFF);
            const b: u8 = @intCast(l2_[x] >>   8);
            const c: u8 = @intCast(r2_[x] & 0xFF);
            const d: u8 = @intCast(r2_[x] >>   8);

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

    const expected_next_time = last_time + @as(i128, samples) * std.time.ns_per_s / 32000;

    const now = std.time.nanoTimestamp();
    const amt = expected_next_time - now - 1 * std.time.ns_per_ms;
    if (amt > 0) {
        const sleep_amt: u64 = @intCast(expected_next_time - now - 1 * std.time.ns_per_ms);
        std.time.sleep(sleep_amt);
    }
    if (now > expected_next_time) {
        last_time = now;
    }
    else {
        last_time = expected_next_time;
    }

    const signal = break_signal.load(std.builtin.AtomicOrder.seq_cst);
    return !signal;
}

fn show_help_menu() void {
    //db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
    db.print("----------------------------------------------------------------------------------------------------------------------------------\n", .{});
    db.print(" Mode commands: \n", .{});
    db.print("    i = Instruction trace log viewer [default] \n", .{});
    db.print("    v = Memory viewer \n", .{});
    db.print("    r = DSP register viewer (1) \n", .{});
    db.print("    e = DSP register viewer (2) \n", .{});
    db.print("    b = DSP debug viewer \n", .{});
    db.print("    7 = Script700 debug viewer \n", .{});
    db.print(" Action commands: \n", .{});
    db.print("    s = Step instruction [default] \n", .{});
    db.print("    c = Continue to next breakpoint \n", .{});
    db.print("    k = Break execution \n", .{});
    db.print("    p = View previous page of ARAM \n", .{});
    db.print("    n = View next page of ARAM \n", .{});
    db.print("    u = Shift memory view up one row \n", .{});
    db.print("    d = Shift memory view down one row \n", .{});
    db.print(" Other: \n", .{});
    db.print("    h = Bring up this menu \n", .{});
    db.print("    m = View ID666 metadata \n", .{});
    db.print("    q = Quit \n", .{});
    db.print("----------------------------------------------------------------------------------------------------------------------------------\n\n", .{});
    db.print("Pressing enter without specifying the command repeats the previous action command. \n", .{});
    flush(null, false);
}

fn show_metadata() void {
    var print_buf: [4096]u8 = [_]u8 {' '} ** 4096;
    const metastring: []const u8 = metadata.?.print(&print_buf) catch print_buf[0..];
    //db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
    db.print("{s}\n", .{metastring});
    flush(null, false);
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
            //std.debug.print("\x1B[A\x1B[A\x1B[9C\x1B[9C\x1B[9C\x1B[8C", .{});
            _ = stdin.readUntilDelimiterOrEof(buffer[0..], '\n') catch "";

            switch (t_input_mode.load(std.builtin.AtomicOrder.seq_cst)) {
                0 => {
                    var cur_mode = t_menu_mode.load(std.builtin.AtomicOrder.seq_cst);
                    sw: switch (buffer[0]) {
                        'q' => {
                            stdout_file.close();
                            std.process.exit(1);
                        },
                        'c' => {
                            
                        },
                        'h' => {
                            show_help_menu();
                            t_other_menu.store('h', std.builtin.AtomicOrder.seq_cst);
                        },
                        'm' => {
                            show_metadata();
                            t_other_menu.store('m', std.builtin.AtomicOrder.seq_cst);
                        },
                        'i' => {
                            prev_input = buffer[0];
                        },
                        'v', 'r', 'e', 'b', '7', 'u', 'd', 'n', 'p' => {
                            cur_mode = buffer[0];
                            prev_input = buffer[0];
                            t_other_menu.store(0, std.builtin.AtomicOrder.seq_cst);
                        },
                        'k' => {
                            const bp_hit = is_breakpoint.load(std.builtin.AtomicOrder.seq_cst);
                            if (bp_hit) {
                                //db.print("\x1B[A\x1B[A\x1B[A", .{});
                                //db.print("                                                                                  \n", .{});
                                //db.print("                                                                                  \n", .{});
                                //db.print("                                                                                  \n", .{});
                                //db.print("\x1B[A\x1B[A", .{});
                                //db.print("\x1B[A\x1B[A", .{});
                            }
                            else {
                                //db.print("\x1B[A\x1B[A", .{});
                                //db.print("                                                                                  \n", .{});
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
                            db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
                            db.print("\n\x1B[38;2;250;125;25mWaiting for Script700 to continue...\x1B[39m\n", .{});
                            flush("Enter a command: ", true);
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
    db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
    db.print("\n\x1B[38;2;250;125;25mScript700 timed out. Enter one of the following:\n", .{});
    db.print("----------------------------------------------------------------------------------------------------------------------------------\n", .{});
    db.print("   w = Attempt wait until Script700 finishes or yields execution \n", .{});
    db.print("   c = Disable Script700 and continue SPC execution \n", .{});
    db.print("   q = Quit program \n", .{});
    db.print("----------------------------------------------------------------------------------------------------------------------------------\x1B[39m\n", .{});
    flush("Enter a command: ", true);

    t_input_mode.store(1, std.builtin.AtomicOrder.seq_cst);

    var buffer: [8]u8 = undefined;
    const stdin = std.io.getStdIn().reader();

    if (m_expect_input.tryLock()) {
        //std.debug.print("\x1B[A\x1B[A\x1B[9C\x1B[9C\x1B[9C\x1B[8C", .{});
        _ = stdin.readUntilDelimiterOrEof(buffer[0..], '\n') catch "";
        
        sw: switch (std.ascii.toLower(buffer[0])) {
            'w' => {
                db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
                db.print("\n\x1B[38;2;250;125;25mWaiting for Script700 to continue...\x1B[39m\n", .{});
                flush("Enter a command: ", true);
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
    db.print("\n\x1B[91mScript700 {s}: ", .{if (load) "load error" else "crashed"});

    switch (err) {
        error.out_of_memory => {
            db.print("not enough memory to resize data area.", .{});
        },
        error.fetch_range => {
            db.print("script area fetch went out of bounds.", .{});
        },
        error.bytecode_too_large => {
            db.print("script area bytecode is too large.", .{});
        },
        else => {
            db.print("unknown error.", .{});
        }
    }

    db.print("\x1B[39m\n", .{});

    t_input_mode.store(0, std.builtin.AtomicOrder.seq_cst);
}

fn flush(msg: ?[]const u8, no_clear: bool) void {
    db.flush(msg, no_clear);
}

fn print_instruction(emu: *const Emu, state: *const SPCState) void {
    db.print("\x1B[43m", .{}); // Yellow highlight

    db.print_pc(state.pc);
    db.print(" |  ", .{});
    db.print_opcode(emu, state.pc);
    db.print("  ", .{});
    db.print("                                                               ", .{});

    db.print_spc_state(state);
    db.print("\n", .{});

    db.print("\x1B[49m", .{}); // Reset color

    flush(null, true);
}