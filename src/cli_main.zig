const std = @import("std");
const builtin = @import("builtin");
const Atomic = std.atomic.Value;

const db = @import("debug.zig");

const Emu             = @import("core/emu.zig").Emu;
const SDSP            = @import("core/s_dsp.zig").SDSP;
const SSMP            = @import("core/s_smp.zig").SSMP;
const SPCState        = @import("core/spc_state.zig").SPCState;
const Script700       = @import("core/script700.zig").Script700;
const Script700Loader = @import("core/script700_loader.zig").Script700Loader;
const SongMetadata    = @import("core/song_metadata.zig").SongMetadata;

const spc_loader = @import("core/spc_loader.zig");

const max_consecutive_timeouts: u32 = 90;
const busyloop_relief_ms:       u32 = 20;

var t_started            = Atomic(bool).init(false);
var break_signal         = Atomic(bool).init(false);
var is_breakpoint        = Atomic(bool).init(false);
var t_timeout_wait       = Atomic(bool).init(false);
var t_menu_mode          = Atomic(u8).init('i');
var t_input_mode         = Atomic(u32).init(0);
var t_other_menu         = Atomic(u8).init('m');
var t_voice_toggle       = Atomic(u8).init(9);
var t_main_only          = Atomic(bool).init(false);
var t_seek_signal        = Atomic(i8).init(0);
var t_cur_clock          = Atomic(u64).init(0);
var t_instr_clear        = Atomic(bool).init(false);
var t_script700_canceled = Atomic(bool).init(false);
var t_script700_restored = Atomic(bool).init(false);

var m_save_buffer  = std.Thread.Mutex{};
var m_expect_input = std.Thread.Mutex{};

var stdout_file: std.fs.File = undefined;

var metadata: ?SongMetadata = null;

pub fn main() !void {
    db.set_cli_width(131);

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
    var parse_script700: bool = false;

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
            else if (std.mem.eql(u8, str, "--script700")) {
                parse_script700 = true;
            }
        }
        i += 1;
    }

    Emu.static_init();

    var singleton = Emu.Singleton { };

    var emu = Emu.new();
    emu.init(
        SDSP.new(&emu),
        SSMP.new(&emu, .{}),
        Script700.new(&emu),
        &singleton
    );
    defer emu.script700.deinit();

    const file_alloc = std.heap.page_allocator;

    if (parse_script700 and spc_file_path != null) {
        const bin_data = try compile_script700();
        defer alloc.free(bin_data);

        const L = spc_file_path.?.len;

        var script700_src_path = try file_alloc.alloc(u8, L + 4);
        defer alloc.free(script700_src_path);
        @memcpy(script700_src_path[0..L], spc_file_path.?);

        var file_path_adjusted = false;
        var final_script700_path: ?[]const u8 = null;

        if (L >= 4) {
            var script700_src_path_lower = try file_alloc.alloc(u8, L + 4);
            defer alloc.free(script700_src_path_lower);
            _ = std.ascii.lowerString(script700_src_path_lower, script700_src_path);

            if (
                   std.mem.eql(u8, script700_src_path_lower[(L - 4) .. L], ".spc")
                or std.mem.eql(u8, script700_src_path_lower[(L - 4) .. L], ".700")
                or std.mem.eql(u8, script700_src_path_lower[(L - 4) .. L], ".7se")
            ) {
                // Replace extension in above path
                script700_src_path[L - 3] = '7';
                script700_src_path[L - 2] = 's';
                script700_src_path[L - 1] = 'b';

                final_script700_path = script700_src_path[0..L];
                file_path_adjusted = true;
            }
        }

        if (!file_path_adjusted) {
            // Append '.700' extension if file path does not end with '.spc'
            script700_src_path[L]     = '.';
            script700_src_path[L + 1] = '7';
            script700_src_path[L + 2] = 's';
            script700_src_path[L + 3] = 'b';
            final_script700_path = script700_src_path[0 .. (L + 4)];
        }

        // Write out to script700 file and replace it if it exists
        var file = try std.fs.cwd().createFile(final_script700_path.?, .{});
        defer file.close();

        try file.writeAll(bin_data);

        std.process.exit(0);
    }

    var script700_load_error: ?anyerror = null;
    script700_load_error = null;

    var t_break_listener = try std.Thread.spawn(.{}, break_listener, .{});
    defer t_break_listener.join();

    //var t_audio_writer = try std.Thread.spawn(.{}, audio_writer, .{});
    //defer t_audio_writer.join();

    var cur_page: u8 = 0x00;
    var cur_offset: u8 = 0x00;
    var cur_mode: u8 = 'i';
    var cur_action: u8 = 's';

    // Load SPC file from path if present
    if (spc_file_path) |path| {
        var file = std.fs.cwd().openFile(path, .{ .mode = .read_only }) catch {
            std.debug.print("error: The SPC file '{s}' was not found or could not be loaded\n", .{path});
            std.process.exit(1);
        };

        defer file.close();

        const file_size = try file.getEndPos();
        const buffer = try file_alloc.alloc(u8, file_size);
        //defer allocator.free(buffer); // The entire app appears to just die after exiting scope if this is uncommented. No idea why

        _ = try file.readAll(buffer);
        metadata = spc_loader.load_spc(&emu, buffer) catch null;

        if (metadata == null) {
            std.debug.print("error: An unknown error occurred while attempting to process SPC metadata\n", .{});
            std.process.exit(1);
        }

        std.debug.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position
        db.print("SPC file \"{s}\" loaded successfully!\n\n", .{path});

        show_metadata();

        if (script700_load_error) |err| {
            report_error(err, true);
        }
    
        if (!debug_mode) {
            cur_action = 'c';
        }
    }

    if (metadata == null) {
        std.debug.print("error: SPC file not provided\n", .{});
        std.process.exit(1);
    }

    const L = spc_file_path.?.len;
    var script700_bin_path   = try file_alloc.alloc(u8, L + 4);
    var script700_bin_path_2 = try file_alloc.alloc(u8, L + 10);

    defer alloc.free(script700_bin_path);
    defer alloc.free(script700_bin_path_2);

    @memcpy(script700_bin_path[0..L],   spc_file_path.?);
    @memcpy(script700_bin_path_2[0..L], spc_file_path.?);

    var file_path_adjusted = false;
    var final_script700_path: ?[]const u8 = null;

    if (L >= 4) {
        var script700_bin_path_lower = try file_alloc.alloc(u8, L + 4);
        defer alloc.free(script700_bin_path_lower);
        _ = std.ascii.lowerString(script700_bin_path_lower, script700_bin_path);

        if (std.mem.eql(u8, script700_bin_path_lower[(L - 4) .. L], ".spc")) {
            // Replace extension in above path
            script700_bin_path[L - 3] = '7';
            script700_bin_path[L - 2] = 's';
            script700_bin_path[L - 1] = 'b';

            final_script700_path = script700_bin_path[0..L];
            file_path_adjusted = true;
        }
    }

    if (!file_path_adjusted) {
        // Append '.7sb' extension if file path does not end with '.spc'
        script700_bin_path[L]     = '.';
        script700_bin_path[L + 1] = '7';
        script700_bin_path[L + 2] = 's';
        script700_bin_path[L + 3] = 'b';
        final_script700_path = script700_bin_path[0 .. (L + 4)];
    }

    // Load script700 file if it exists
    var script700_file: ?std.fs.File = std.fs.cwd().openFile(final_script700_path.?, .{ .mode = .read_only }) catch null;
    
    if (script700_file == null) {
        // Try again with 65816.7sb
        var si: u32 = @intCast(spc_file_path.?.len);

        while (si != 0) {
            const ssi = si - 1;
            const char = spc_file_path.?[ssi];

            if (char == '/' or char == '\\') {
                break;
            }

            si -= 1;
        }

        script700_bin_path_2[si    ] = '6';
        script700_bin_path_2[si + 1] = '5';
        script700_bin_path_2[si + 2] = '8';
        script700_bin_path_2[si + 3] = '1';
        script700_bin_path_2[si + 4] = '6';
        script700_bin_path_2[si + 5] = '.';
        script700_bin_path_2[si + 6] = '7';
        script700_bin_path_2[si + 7] = 's';
        script700_bin_path_2[si + 8] = 'b';

        const path: []u8 = script700_bin_path_2[0..(si+9)];

        script700_file = std.fs.cwd().openFile(path, .{ .mode = .read_only }) catch null;
    }

    if (script700_file) |s7f| {
        defer s7f.close();

        const file_size = try s7f.getEndPos();
        const buffer    = try file_alloc.alloc(u8, file_size);

        _ = try s7f.readAll(buffer);

        try Script700Loader.load_script(&emu.script700, buffer);
    }

    defer Script700.deinit(&emu.script700);
    if (script700_file) |_| {
        emu.script700.enabled = true; // Enable Script700 if load is successful
    }

    // After all load is done, make copy of initial emulator state
    var emu_run_ahead: Emu = undefined;
    emu_run_ahead.singleton = null;
    emu_run_ahead.s_dsp = SDSP.new(&emu_run_ahead);
    emu_run_ahead.s_smp = SSMP.new(&emu_run_ahead, .{});
    emu_run_ahead.script700 = Script700.new(&emu_run_ahead);
    emu_run_ahead.load_from(&emu, .{ .copy_everything = true }) catch {
        std.debug.print("Save memory error\n", .{});
        std.process.exit(1);
    };

    // Set debug values
    db.emu_           = &emu;
    db.run_ahead_emu_ = &emu_run_ahead;
    db.total_length_ms = @as(u64, metadata.?.length_in_seconds orelse 600) * 1000;

    // Start run ahead thread after run ahead emu object has been created
    var t_run_ahead = try std.Thread.spawn(.{}, run_ahead, .{&emu_run_ahead});
    defer t_run_ahead.join();

    const stdin = std.io.getStdIn().reader();
    var buffer: [8]u8 = undefined;

    emu.s_smp.enable_access_logs = true;
    emu.s_smp.enable_timer_logs = true;
    emu.s_smp.clear_access_logs();
    emu.s_smp.clear_timer_logs();

    last_time = std.time.nanoTimestamp();

    while (true) {
        if (cur_action == 'c') {
            const m = t_menu_mode.load(std.builtin.AtomicOrder.seq_cst);
            const h = t_other_menu.load(std.builtin.AtomicOrder.seq_cst);

            if (h == 0) {
                switch (m) {
                    'n' => {
                        cur_page +%= 1;
                        cur_mode = 'v';
                        t_menu_mode.store(cur_mode, std.builtin.AtomicOrder.seq_cst);
                    },
                    'p' => {
                        cur_page -%= 1;
                        cur_mode = 'v';
                        t_menu_mode.store(cur_mode, std.builtin.AtomicOrder.seq_cst);
                    },
                    'd' => {
                        if (cur_offset > 0xEF) {
                            cur_page +%= 1;
                        }
                        cur_offset +%= 0x10;
                        cur_mode = 'v';
                        t_menu_mode.store(cur_mode, std.builtin.AtomicOrder.seq_cst);
                    },
                    'u' => {
                        if (cur_offset < 0x10) {
                            cur_page -%= 1;
                        }
                        cur_offset -%= 0x10;
                        cur_mode = 'v';
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
                t_started.store(false, std.builtin.AtomicOrder.seq_cst);
            }
            
            _ = stdin.readUntilDelimiterOrEof(buffer[0..], '\n') catch "";

            if (std.ascii.toLower(buffer[0]) == 'c') {
                break_signal.store(false, std.builtin.AtomicOrder.seq_cst);

                set_msg(0, 0, false);
                if (cur_mode == 'i') {
                    db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
                    t_menu_mode.store(cur_mode, std.builtin.AtomicOrder.seq_cst);
                    show_metadata_noclear();
                }
            }
        }

        const last_cycle = emu.s_dsp.cur_cycle();
        const last_pc    = emu.s_smp.spc.pc();
        const prev_state = emu.s_smp.state;

        sw: switch (std.ascii.toLower(buffer[0])) {
            'q' => {
                stdout_file.close();
                quit();
            },
            'h' => {
                set_msg(0, 0, false);
                flush(null, false);
                show_help_menu();
                t_other_menu.store('h', std.builtin.AtomicOrder.seq_cst);
            },
            'm' => {
                set_msg(0, 0, false);
                flush(null, false);
                show_metadata();
                t_other_menu.store('m', std.builtin.AtomicOrder.seq_cst);
            },
            'n' => {
                cur_action = 'n';
                cur_page +%= 1;

                cur_mode = 'v';
                t_menu_mode.store(cur_mode, std.builtin.AtomicOrder.seq_cst);
                t_other_menu.store(0, std.builtin.AtomicOrder.seq_cst);
                
                db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position
                db.print_memory_page(&emu, cur_page, cur_offset, .{});

                set_msg(0, 0, false);
                flush(null, true);
            },
            'c' => {
                t_started.store(true, std.builtin.AtomicOrder.seq_cst);
                cur_action = 'c';
                cur_mode = t_menu_mode.load(std.builtin.AtomicOrder.seq_cst);

                var s7en = emu.script700.enabled;

                var res = run_loop(&emu) catch null;
                var attempts: u32 = 0;
                var step_instr: bool = false;

                if (res != null and !res.? and !is_breakpoint.load(std.builtin.AtomicOrder.seq_cst)) {
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
                    
                    emu.script700_error = null;
                }
                else if (db.cur_info_msg == 2 or db.cur_info_msg == 3) {
                    set_msg(0, 0, false);
                    flush(null, true);
                }

                break_signal.store(false, std.builtin.AtomicOrder.seq_cst);

                if (!res.?) {
                    t_started.store(false, std.builtin.AtomicOrder.seq_cst);
                    cur_action = 's';

                    if (cur_mode == 'i') {
                        db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
                        print_instruction(&emu, &emu.s_smp.spc.state);
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

                if (t_other_menu.load(std.builtin.AtomicOrder.seq_cst) == 0 or is_breakpoint.load(std.builtin.AtomicOrder.seq_cst)) {
                    t_other_menu.store(0, std.builtin.AtomicOrder.seq_cst);

                   if (cur_mode == 'v') {
                        db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position
                        db.print_memory_page(&emu, cur_page, cur_offset, .{.prev_pc = emu.s_smp.spc.pc(), .prev_state = &emu.s_smp.state});
                        t_other_menu.store(0, std.builtin.AtomicOrder.seq_cst);
                    }
                    else if (cur_mode == 'r') {
                        db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position
                        db.print_dsp_map(&emu, .{.is_dsp = true, .prev_pc = emu.s_smp.spc.pc(), .prev_state = &emu.s_smp.state});

                        db.print("\n", .{});
                        db.print_dsp_state(&emu, &emu_run_ahead, .{.is_dsp = true, .prev_pc = emu.s_smp.spc.pc(), .prev_state = &emu.s_smp.state});

                        t_other_menu.store(0, std.builtin.AtomicOrder.seq_cst);
                    }
                    else if (cur_mode == 'e') {
                        db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position
                        db.print_dsp_map(&emu, .{.is_dsp = true, .prev_pc = emu.s_smp.spc.pc(), .prev_state = &emu.s_smp.state});

                        db.print("\n", .{});
                        db.print_dsp_state_2(&emu, .{.is_dsp = true, .prev_pc = emu.s_smp.spc.pc(), .prev_state = &emu.s_smp.state});

                        t_other_menu.store(0, std.builtin.AtomicOrder.seq_cst);
                    }
                    else if (cur_mode == 'b') {
                        db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position
                        db.print_dsp_debug_state(&emu, .{.is_dsp = true, .prev_pc = emu.s_smp.spc.pc(), .prev_state = &emu.s_smp.state});
                        t_other_menu.store(0, std.builtin.AtomicOrder.seq_cst);
                    }
                    else if (cur_mode == '9') {
                        db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position
                        db.print_script700_state(&emu);
                        t_other_menu.store(0, std.builtin.AtomicOrder.seq_cst);
                    }
                    else if (cur_mode == 'i') {
                        //db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
                        //t_menu_mode.store(cur_mode, std.builtin.AtomicOrder.seq_cst);
                        //show_metadata();
                        flush(null, true);
                    }
                }

                if (is_breakpoint.load(std.builtin.AtomicOrder.seq_cst)) {
                    set_msg(1, 0, false);
                    flush(null, true);
                }
            },
            'p' => {
                cur_action = 'p';
                cur_page -%= 1;
                
                cur_mode = 'v';
                t_menu_mode.store(cur_mode, std.builtin.AtomicOrder.seq_cst);
                t_other_menu.store(0, std.builtin.AtomicOrder.seq_cst);
                
                db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position
                db.print_memory_page(&emu, cur_page, cur_offset, .{});

                set_msg(0, 0, false);
                flush(null, true);
            },
            'd' => {
                cur_action = 'd';
                if (cur_offset > 0xEF) {
                    cur_page +%= 1;
                }
                cur_offset +%= 0x10;
                
                cur_mode = 'v';
                t_menu_mode.store(cur_mode, std.builtin.AtomicOrder.seq_cst);
                t_other_menu.store(0, std.builtin.AtomicOrder.seq_cst);
                
                db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position
                db.print_memory_page(&emu, cur_page, cur_offset, .{});

                set_msg(0, 0, false);
                flush(null, true);
            },
            'u' => {
                cur_action = 'u';
                if (cur_offset < 0x10) {
                    cur_page -%= 1;
                }
                cur_offset -%= 0x10;
                
                cur_mode = 'v';
                t_menu_mode.store(cur_mode, std.builtin.AtomicOrder.seq_cst);
                t_other_menu.store(0, std.builtin.AtomicOrder.seq_cst);
            
                db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position
                db.print_memory_page(&emu, cur_page, cur_offset, .{});

                set_msg(0, 0, false);
                flush(null, true);
            },
            'i' => {
                if (cur_mode != 'i') {
                    db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position
                    set_msg(0, 0, false);
                    flush(null, true);
                }

                if (cur_action != 'c') {
                    if (cur_mode != 'i') {
                        print_instruction(&emu, &emu.s_smp.spc.state);
                    }
                    else {
                        flush(null, true);
                    }

                    cur_mode = 'i';
                    t_other_menu.store(0, std.builtin.AtomicOrder.seq_cst);
                }
            },
            'v' => {
                cur_mode = 'v';
                t_menu_mode.store(cur_mode, std.builtin.AtomicOrder.seq_cst);
                t_other_menu.store(0, std.builtin.AtomicOrder.seq_cst);
                db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position
                db.print_memory_page(&emu, cur_page, cur_offset, .{});

                set_msg(0, 0, false);
                flush(null, true);
            },
            'r' => {
                cur_mode = 'r';
                t_menu_mode.store(cur_mode, std.builtin.AtomicOrder.seq_cst);
                t_other_menu.store(0, std.builtin.AtomicOrder.seq_cst);
                db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position
                db.print_dsp_map(&emu, .{.is_dsp = true});

                db.print("\n", .{});
                db.print_dsp_state(&emu, &emu_run_ahead, .{.is_dsp = true});

                set_msg(0, 0, false);
                flush(null, true);
            },
            'e' => {
                cur_mode = 'e';
                t_menu_mode.store(cur_mode, std.builtin.AtomicOrder.seq_cst);
                t_other_menu.store(0, std.builtin.AtomicOrder.seq_cst);
                db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position
                db.print_dsp_map(&emu, .{.is_dsp = true});

                db.print("\n", .{});
                db.print_dsp_state_2(&emu, .{.is_dsp = true});

                set_msg(0, 0, false);
                flush(null, true);
            },
            'b' => {
                cur_mode = 'b';
                t_menu_mode.store(cur_mode, std.builtin.AtomicOrder.seq_cst);
                t_other_menu.store(0, std.builtin.AtomicOrder.seq_cst);
                db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position
                db.print_dsp_debug_state(&emu, .{.is_dsp = true});

                set_msg(0, 0, false);
                flush(null, true);
            },
            '9' => {
                cur_mode = '9';
                t_menu_mode.store(cur_mode, std.builtin.AtomicOrder.seq_cst);
                t_other_menu.store(0, std.builtin.AtomicOrder.seq_cst);
                db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position
                db.print_script700_state(&emu);

                set_msg(0, 0, false);
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
                var no_cursor_up = false;

                if (t_instr_clear.load(std.builtin.AtomicOrder.seq_cst)) {
                    db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
                    flush(null, false);
                    t_instr_clear.store(false, std.builtin.AtomicOrder.seq_cst);
                    no_cursor_up = true;
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
                else if (db.cur_info_msg == 2 or db.cur_info_msg == 3) {
                    set_msg(0, 0, false);
                }

                const all_logs = emu.s_smp.get_access_logs_range(last_cycle);

                _ = emu.break_check(); // Consume the breakpoint if we hit one while in step mode

                if (cur_mode == 'i') {
                    if (emu.script700_error == null) {
                        set_msg(0, 0, false);
                    }
                    const prev_spc_state = emu.s_smp.prev_spc_state;

                    const prev_logs = emu.s_smp.get_access_logs_range(last_cycle);
                    var logs = db.filter_access_logs(prev_logs);

                    if (!no_cursor_up) {
                        db.move_cursor_up();
                    }
                    
                    db.print_pc(prev_spc_state.pc);
                    db.print(" |  ", .{});
                    db.print_opcode(&emu, prev_spc_state.pc);
                    db.print("  ", .{});
                    try db.print_logs(&prev_state, logs[0..]);

                    db.print_spc_state(&prev_spc_state);
                    db.print("\n", .{});

                    print_instruction(&emu, &emu.s_smp.spc.state);

                    emu.s_smp.clear_timer_logs();
                }
                else if (cur_mode == 'v') {
                    db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position
                    db.print_memory_page(&emu, cur_page, cur_offset, .{.prev_pc = last_pc, .prev_state = &prev_state, .logs = all_logs});
                    flush(null, true);
                    set_msg(0, 0, false);
                }
                else if (cur_mode == 'r') {
                    db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position
                    db.print_dsp_map(&emu, .{.is_dsp = true, .prev_pc = last_pc, .prev_state = &prev_state, .logs = all_logs});

                    db.print("\n", .{});
                    db.print_dsp_state(&emu, &emu_run_ahead, .{.is_dsp = true, .prev_pc = last_pc, .prev_state = &prev_state, .logs = all_logs});

                    flush(null, true);
                    set_msg(0, 0, false);
                }
                else if (cur_mode == 'e') {
                    db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position
                    db.print_dsp_map(&emu, .{.is_dsp = true, .prev_pc = last_pc, .prev_state = &prev_state, .logs = all_logs});

                    db.print("\n", .{});
                    db.print_dsp_state_2(&emu, .{.is_dsp = true, .prev_pc = last_pc, .prev_state = &prev_state, .logs = all_logs});

                    flush(null, true);
                    set_msg(0, 0, false);
                }
                else if (cur_mode == 'b') {
                    db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position
                    db.print_dsp_debug_state(&emu, .{.is_dsp = true, .prev_pc = last_pc, .prev_state = &prev_state, .logs = all_logs});
                    //set_msg(0, 0, false);
                    flush(null, true);
                    set_msg(0, 0, false);
                }
                else if (cur_mode == '9') {
                    db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position
                    db.print_script700_state(&emu);
                    flush(null, true);
                    set_msg(0, 0, false);
                }

                emu.script700_error = null;
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
var savestates = std.ArrayList(Emu).init(alloc);

fn savestate(emu: *Emu) void {
    if (savestates.items.len < 600) { // 10 minutes is enough run ahead - stop here to avoid blowing up memory
        const new_emu = Emu {
            .s_dsp = SDSP.new(emu),
            .s_smp = SSMP.new(emu, .{}),
            .script700 = Script700.new(emu),
            .singleton = null,
        };

        savestates.append(new_emu) catch {};
        savestates.items[savestates.items.len - 1].load_from(emu, .{}) catch {
            std.debug.print("Save memory error\n", .{});
            std.process.exit(1);
        };
    }
}

fn run_ahead(emu: *Emu) void {
    var cycles: u32 = samples * 64;
    cycles = @divFloor(cycles, 8);

    while (true) {
        db.m_run_ahead.lock();

        // No point in running ahead past the point where emu states are no longer saved
        if (@divFloor(emu.s_dsp.cur_cycle(), 2048000) >= 600) {
            db.m_run_ahead.unlock();
            std.time.sleep(1 * std.time.ns_per_ms);
            continue;
        }

        if (emu.script700.enabled) {
            for (0..cycles) |_| {
                if (emu.s_dsp.cur_cycle() % 2048000 == 0) {
                    m_save_buffer.lock();
                    savestate(emu);
                    m_save_buffer.unlock();
                }

                var retry = true;
                while (retry) {
                    retry = false;
                    // If Script700 times out, sleep for a bit and try again
                    emu.step_cycle_safe() catch {
                        retry = true;
                        db.m_run_ahead.unlock();
                        std.time.sleep(2 * std.time.ns_per_ms);
                        db.m_run_ahead.lock();
                    };
                }
            }
        }
        else {
            for (0..cycles) |_| {
                if (emu.s_dsp.cur_cycle() % 2048000 == 0) {
                    m_save_buffer.lock();
                    savestate(emu);
                    m_save_buffer.unlock();
                }

                emu.step_cycle_fast();
            }
        }

        db.m_run_ahead.unlock();
        std.time.sleep(10 * std.time.ns_per_us);
    }
}

fn run_loop(emu: *Emu) !bool {
    const cycles = samples * 64;

    is_breakpoint.store(false, std.builtin.AtomicOrder.seq_cst);

    const seek_amt = t_seek_signal.load(std.builtin.AtomicOrder.seq_cst);
    const target_cycle = @as(i128, emu.s_dsp.cur_cycle()) + @as(i128, seek_amt) * @as(i128, 2048000);

    // Remove all saved emu states past this point if Script700 has been canceled.
    if (t_script700_canceled.load(std.builtin.AtomicOrder.seq_cst)) {
        db.m_run_ahead.lock();

        var any_enabled = false;

        var index: i32 = @intCast(savestates.items.len - 1);
        while (index >= 0) {
            const val = savestates.items[savestates.items.len - 1];
            if (val.script700.enabled) {
                any_enabled = true;
                break;
            }
            index -= 1;
        }

        if (any_enabled) {
            var top = savestates.items[savestates.items.len - 1];
            while (top.s_dsp.cur_cycle() > emu.s_dsp.cur_cycle()) {
                const res = savestates.pop();
                if (res == null) {
                    break;
                }
                top = res.?;
            }
            db.run_ahead_emu_.?.load_from(emu, .{}) catch {
                std.debug.print("Save memory error\n", .{});
                std.process.exit(1);
            };
            savestate(db.run_ahead_emu_.?);
        }

        db.m_run_ahead.unlock();

        t_script700_canceled.store(false, std.builtin.AtomicOrder.seq_cst);
    }

    m_save_buffer.lock();

    if (seek_amt < 0 and savestates.items.len > 0) {
        var last_save: ?*Emu = null;
        var i: u32 = @intCast(savestates.items.len - 1);

        while (true) {
            const save: ?*Emu = &savestates.items[i];

            if (save != null) {
                last_save = save;
                if (last_save.?.s_dsp.cur_cycle() < target_cycle) {
                    break;
                }
            }

            if (i == 0) {
                break;
            }

            i -= 1;
        }

        if (last_save) |ls| {
            emu.load_from(ls, .{}) catch {
                std.debug.print("Save memory error\n", .{});
                std.process.exit(1);
            };
        }

        t_seek_signal.store(0, std.builtin.AtomicOrder.seq_cst);
    }
    else if (seek_amt > 0) {
        t_cur_clock.store(emu.s_dsp.cur_cycle(), std.builtin.AtomicOrder.seq_cst);
        t_seek_signal.store(0, std.builtin.AtomicOrder.seq_cst);
    }

    const cur_clock = t_cur_clock.load(std.builtin.AtomicOrder.seq_cst);

    if (cur_clock > 0 and savestates.items.len > 0) {
        const last = &savestates.items[savestates.items.len - 1];
        const cur_s = @divFloor(cur_clock, 2048000);

        if ((cur_s + 5) * 2048000 <= last.s_dsp.cur_cycle()) {
            const last_s = @divFloor(last.s_dsp.cur_cycle(), 2048000);
            const diff_s = last_s - (cur_s + 5);

            if (diff_s <= savestates.items.len - 1) {
                const idx = savestates.items.len - 1 - diff_s;
                emu.load_from(&savestates.items[idx], .{}) catch {
                    std.debug.print("Save memory error\n", .{});
                    std.process.exit(1);
                };

                t_cur_clock.store(0, std.builtin.AtomicOrder.seq_cst);
            }
        }
    }
    
    m_save_buffer.unlock();

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

    // Remove all saved emu states past this point if Script700 has been restored.
    if (t_script700_restored.load(std.builtin.AtomicOrder.seq_cst)) {
        db.m_run_ahead.lock();

        var any_disabled = false;

        var index: i32 = @intCast(savestates.items.len - 1);
        while (index >= 0) {
            const val = savestates.items[savestates.items.len - 1];
            if (!val.script700.enabled) {
                any_disabled = true;
                break;
            }
            index -= 1;
        }

        if (any_disabled) {
            var top = savestates.items[savestates.items.len - 1];
            while (top.s_dsp.cur_cycle() > emu.s_dsp.cur_cycle()) {
                const res = savestates.pop();
                if (res == null) {
                    break;
                }
                top = res.?;
            }

            db.run_ahead_emu_.?.load_from(emu, .{}) catch {
                std.debug.print("Save memory error\n", .{});
                std.process.exit(1);
            };
            savestate(db.run_ahead_emu_.?);
        }

        db.m_run_ahead.unlock();

        t_script700_restored.store(false, std.builtin.AtomicOrder.seq_cst);
    }

    stream_start = 0;

    if (emu.singleton == null) {
        return true;
    }

    const l1, const r1, const l2, const r2 = try emu.view_dac_samples(samples);

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

    const v_idx = t_voice_toggle.load(std.builtin.AtomicOrder.seq_cst);
    if (v_idx == 0) {
        for (0..8) |c| {
            if (emu.singleton) |s| {
                s.pipeline_2.enable_voice(@intCast(c));
            }
        }
        t_voice_toggle.store(9, std.builtin.AtomicOrder.seq_cst);
        set_msg(12, 0, false);
    }
    else if (v_idx <= 8) {
        const c = v_idx - 1;
        if (t_main_only.load(std.builtin.AtomicOrder.seq_cst)) {
            if (emu.singleton) |s| {
                s.pipeline_2.toggle_main_voice(@intCast(c));
                if (s.pipeline_2.settings.channels_enabled[c]) {
                    s.pipeline_2.enable_voice(@intCast(c));
                }
            }
        }
        else {
            if (emu.singleton) |s| {
                s.pipeline_2.toggle_voice(@intCast(c));
            }
        }

        t_voice_toggle.store(9, std.builtin.AtomicOrder.seq_cst);

        if (emu.singleton) |s| {
            if (s.pipeline_2.settings.channels_enabled[c]) {
                set_msg(10, v_idx, false);
            }
            else {
                set_msg(11, v_idx, false);
            }
        }
    }
    else if (v_idx == 10) {
        t_voice_toggle.store(9, std.builtin.AtomicOrder.seq_cst);
    }

    var stdout_writer = stdout_file.writer();

    stdout_writer.writeAll(&buf) catch {
        std.debug.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position
        std.debug.print("\x1B[91mError writing to stdout (broken pipe?)\x1B[39m\n", .{});
        std.time.sleep(2 * std.time.ns_per_s);
        std.process.exit(1);
    };

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
    db.print("----------------------------------------------------------------------------------------------------------------------------------\n", .{});
    db.print(" Mode commands: \n", .{});
    db.print("    i  = Instruction trace log viewer [default] \n", .{});
    db.print("    v  = Memory viewer \n", .{});
    db.print("    r  = DSP register viewer (1) \n", .{});
    db.print("    e  = DSP register viewer (2) \n", .{});
    db.print("    b  = DSP debug viewer \n", .{});
    db.print("    9  = Script700 debug viewer \n", .{});
    db.print(" Action commands: \n", .{});
    db.print("    s  = Step instruction [default] \n", .{});
    db.print("    c  = Continue to next breakpoint \n", .{});
    db.print("    k  = Break execution \n", .{});
    db.print("    p  = View previous page of ARAM \n", .{});
    db.print("    n  = View next page of ARAM \n", .{});
    db.print("    u  = Shift memory view up one row \n", .{});
    db.print("    d  = Shift memory view down one row \n", .{});
    db.print("    l  = Skip back 5 seconds \n", .{});
    db.print("    f  = Skip ahead 5 seconds \n", .{});
    db.print(" Other: \n", .{});
    db.print("    h  = Bring up this menu \n", .{});
    db.print("    m  = View ID666 metadata \n", .{});
    db.print("   1-8 = Toggle channel # output \n", .{});
    db.print("    0  = Enable all channels \n", .{});
    db.print("    q  = Quit \n", .{});
    db.print("----------------------------------------------------------------------------------------------------------------------------------\n\n", .{});
    db.print("Pressing enter without specifying the command repeats the previous action command. \n", .{});
    flush(null, false);
}

fn show_metadata() void {
    var print_buf: [4096]u8 = [_]u8 {' '} ** 4096;
    const result = metadata.?.print(&print_buf);

    if (result) |metastring| {
        db.print("{s}\n", .{metastring});
    }
    else |_| {
        db.print("{s}\n", .{print_buf[0..]});
    }

    flush(null, false);
}

fn show_metadata_noclear() void {
    var print_buf: [4096]u8 = [_]u8 {' '} ** 4096;
    const result = metadata.?.print(&print_buf);

    if (result) |metastring| {
        db.print("{s}\n", .{metastring});
    }
    else |_| {
        db.print("{s}\n", .{print_buf[0..]});
    }

    flush(null, true);
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

            if (is_breakpoint.load(std.builtin.AtomicOrder.seq_cst)) {
                break_signal.store(true, std.builtin.AtomicOrder.seq_cst);
                t_instr_clear.store(true, std.builtin.AtomicOrder.seq_cst);
                set_msg(0, 0, false);
            }
            else {
                switch (t_input_mode.load(std.builtin.AtomicOrder.seq_cst)) {
                    0 => {
                        var cur_mode = t_menu_mode.load(std.builtin.AtomicOrder.seq_cst);
                        sw: switch (buffer[0]) {
                            'q' => {
                                stdout_file.close();
                                quit();
                            },
                            'c' => {
                                
                            },
                            'h' => {
                                show_help_menu();
                                t_other_menu.store('h', std.builtin.AtomicOrder.seq_cst);
                                set_msg(0, 0, false);
                            },
                            'm' => {
                                show_metadata();
                                t_other_menu.store('m', std.builtin.AtomicOrder.seq_cst);
                                set_msg(0, 0, false);
                            },
                            'i' => {
                                prev_input = buffer[0];
                                set_msg(0, 0, false);
                            },
                            '0' => {
                                t_voice_toggle.store(0, std.builtin.AtomicOrder.seq_cst);
                            },
                            '1', '2', '3', '4', '5', '6', '7', '8' => {
                                t_voice_toggle.store(@intCast(buffer[0] - '0'), std.builtin.AtomicOrder.seq_cst);
                            },
                            'g' => {
                                const main_only = t_main_only.load(std.builtin.AtomicOrder.seq_cst);
                                t_main_only.store(!main_only, std.builtin.AtomicOrder.seq_cst);
                            },
                            'v', 'r', 'e', 'b', '9', 'u', 'd', 'n', 'p' => { // Test
                                cur_mode = buffer[0];
                                prev_input = buffer[0];

                                t_other_menu.store(0, std.builtin.AtomicOrder.seq_cst);
                                
                                set_msg(0, 0, false);
                            },
                            'k' => {
                                break_signal.store(true, std.builtin.AtomicOrder.seq_cst);
                                t_instr_clear.store(true, std.builtin.AtomicOrder.seq_cst);
                                prev_input = buffer[0];
                                
                                set_msg(0, 0, false);
                            },
                            'l' => {
                                t_seek_signal.store(-5, std.builtin.AtomicOrder.seq_cst);
                                prev_input = buffer[0];
                                
                                set_msg(14, 0, false);
                            },
                            'f' => {
                                const cc = t_cur_clock.load(std.builtin.AtomicOrder.seq_cst);

                                if (cc == 0) {
                                    t_seek_signal.store(5, std.builtin.AtomicOrder.seq_cst);
                                    prev_input = buffer[0];
                                }
                                
                                set_msg(13, 0, false);
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
                                set_msg(3, 0, false);
                                flush(null, true);
                                t_timeout_wait.store(true, std.builtin.AtomicOrder.seq_cst);
                                t_script700_restored.store(true, std.builtin.AtomicOrder.seq_cst);
                            },
                            'c' => {
                                t_timeout_wait.store(false, std.builtin.AtomicOrder.seq_cst);
                                t_script700_canceled.store(true, std.builtin.AtomicOrder.seq_cst);
                            },
                            'q' => {
                                stdout_file.close();
                                quit();
                            },
                            else => {
                                continue :sw 'w';
                            }
                        }

                        t_started.store(false, std.builtin.AtomicOrder.seq_cst);
                    },
                    else => unreachable
                }
            }

            m_expect_input.unlock();
        }

        // Sleep for 50 ms to allow main thread time to block stdin waiting on this one
        std.time.sleep(50 * std.time.ns_per_ms);
    }
}

fn report_timeout() void {
    db.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position
    db.print("\n\x1B[38;2;250;125;25mScript700 timed out. Enter one of the following:\n", .{});
    db.print("----------------------------------------------------------------------------------------------------------------------------------\n", .{});
    db.print("   w = Attempt wait until Script700 finishes or yields execution \n", .{});
    db.print("   c = Disable Script700 and continue SPC execution \n", .{});
    db.print("   q = Quit program \n", .{});
    db.print("----------------------------------------------------------------------------------------------------------------------------------\x1B[39m\n", .{});
    set_msg(2, 0, false);
    flush(null, true);

    t_input_mode.store(1, std.builtin.AtomicOrder.seq_cst);

    var buffer: [8]u8 = undefined;
    const stdin = std.io.getStdIn().reader();

    if (m_expect_input.tryLock()) {
        _ = stdin.readUntilDelimiterOrEof(buffer[0..], '\n') catch "";
        
        sw: switch (std.ascii.toLower(buffer[0])) {
            'w' => {
                set_msg(3, 0, false);
                flush(null, true);
                t_timeout_wait.store(true, std.builtin.AtomicOrder.seq_cst);
                t_script700_restored.store(true, std.builtin.AtomicOrder.seq_cst);
            },
            'c' => {
                t_timeout_wait.store(false, std.builtin.AtomicOrder.seq_cst);
                t_script700_canceled.store(true, std.builtin.AtomicOrder.seq_cst);
            },
            'q' => {
                stdout_file.close();
                quit();
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
    var msg: u8 = undefined;

    if (load) {
        msg = 4;
    }
    else {
        msg = 5;
    }

    switch (err) {
        error.out_of_memory => {
            set_msg(msg, 6, true);
        },
        error.fetch_range => {
            set_msg(msg, 7, true);
        },
        error.bytecode_too_large => {
            set_msg(msg, 8, true);
        },
        else => {
            set_msg(msg, 9, true);
        }
    }

    db.flush(null, true);

    t_input_mode.store(0, std.builtin.AtomicOrder.seq_cst);
}

fn flush(msg: ?[]const u8, no_clear: bool) void {
    db.flush(msg, no_clear);
}

fn set_msg(msg_id: u8, sub_msg_id: u8, is_error: bool) void {
    db.is_error     = is_error;
    db.cur_info_msg = msg_id;
    db.cur_err_msg  = sub_msg_id;
}

fn print_instruction(emu: *const Emu, state: *const SPCState) void {
    db.print("\x1B[45m", .{}); // Highlight

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

const ParseMode = enum {
    script, data
};

const alloc = std.heap.page_allocator;

fn compile_script700() ![]const u8 {
    const stdin = std.io.getStdIn().reader();
    var buffer: [40]u8 = undefined;

    var mode = ParseMode.script;

    var bin_data = std.ArrayList(u8).init(alloc);
    defer bin_data.deinit();

    // Pre-initialize with label address table
    for (0..1024) |_| {
        inline for (0..4) |_| {
            try bin_data.append(0xFF);
        }
    }

    const script_size_offset: u32 = @intCast(bin_data.items.len);
    var   data_size_offset:   u32 = undefined;

    // Allocate 4 bytes for where the script size will be stored
    inline for (0..4) |_| {
        try bin_data.append(0x00);
    }

    var bytes: [4][4]u8 = undefined;

    var instr_size: u32 = 1;
    var first_iter = true;

    var pc: u32 = 0;
    var dc: u32 = 0;

    var ignore = false;

    while (true) {
        _token_parse_end = 0;
        const str: ?[]const u8 = stdin.readUntilDelimiterOrEof(buffer[0..], '\n') catch "";

        if (std.mem.eql(u8, str.?, ";@line")) {
            ignore = false;
            continue;
        }

        if (str.?.len == 0 or std.mem.eql(u8, str.?, "\r\n") or std.mem.eql(u8, str.?, "\n")) {
            if (mode == ParseMode.script) {
                if (!ignore) {
                    for (bytes, 0..) |bb, k| {
                        if (instr_size <= k) {
                            break;
                        }
                        for (0..4) |i| {
                            try bin_data.append(bb[i]);
                        }
                    }
                }

                const size_bytes = u32le_to_u8_array(pc);
                inline for (0..4) |x| {
                    bin_data.items[script_size_offset +% x] = size_bytes[x];
                }

                data_size_offset = @intCast(bin_data.items.len);

                // Allocate 4 bytes for where the data size will be stored
                inline for (0..4) |_| {
                    try bin_data.append(0x00);
                }

                dc = 0;

                first_iter = true;
                mode = ParseMode.data;

                if (ignore) {
                    ignore = false;
                }
            }
            else if (mode == ParseMode.data) {
                break;
            }
        }
                
        if (ignore) {
            continue;
        }

        const mnemonic = peek_token(str.?);
    
        if (mnemonic.?.len >= 2 and mnemonic.?[0] == ':') {
            _ = next_token(str.?);
            var label_num = std.fmt.parseInt(i32, mnemonic.?[1..], 10) catch -1;

            if (label_num < 0) {
                ignore = true;
                continue;
            }

            label_num &= 1023;
            const label_start: u32 = @intCast(label_num * 4);

            inline for (0..4) |i| {
                const ii: u32 = @intCast(i);
                if (mode == .script) {
                    bin_data.items[label_start +% ii] = @intCast(pc >> ii * 8 & 0xFF);
                }
                else {
                    bin_data.items[label_start +% ii] = @intCast((0x8000_0000 +% dc) >> ii * 8 & 0xFF);
                }
            }

            continue;
        }
        else if (mode == .script) {
            _ = next_token(str.?);
            if (!first_iter and (mnemonic == null or mnemonic.?.len < 2 or mnemonic.?.len >= 2 and mnemonic.?[0] != ':')) {
                for (bytes, 0..) |bb, k| {
                    if (instr_size <= k) {
                        break;
                    }
                    for (0..4) |i| {
                        try bin_data.append(bb[i]);
                    }
                }
            }

            instr_size = 1;

            if (mnemonic == null) {
                ignore = true;
                continue;
            }

            first_iter = false;

            // Set instructions to NOP tentatively
            var w: [4]u32 = .{
                0x8000_0000,
                0x8000_0000,
                0x8000_0000,
                0x8000_0000
            };

            var w_slc: ?[]u32 = null;

            const prefix_value = next_token(str.?);

            if (prefix_value == null) {
                const w_err = Script700.compile_instruction(&w, mnemonic.?, .{});
                if (w_err) |ww| {
                    w_slc = ww;
                }
                else |_| {
                    ignore = true;
                    continue;
                }

                for (w_slc.?, 0..) |word, i| {
                    bytes[i] = u32le_to_u8_array(word);
                }

                instr_size = @intCast(w_slc.?.len);

                pc +%= @intCast(w_slc.?.len);

                continue;
            }

            const prefix, const value = split_token(prefix_value.?);

            const next = next_token(str.?);
            if (next == null) {
                const w_err = Script700.compile_instruction(&w, mnemonic.?, .{
                    .oper_1_prefix = prefix.?,
                    .oper_1_value = value
                });

                if (w_err) |ww| {
                    w_slc = ww;
                }
                else |_| {
                    ignore = true;
                    continue;
                }

                for (w_slc.?, 0..) |word, i| {
                    bytes[i] = u32le_to_u8_array(word);
                }

                instr_size = @intCast(w_slc.?.len);

                pc +%= @intCast(w_slc.?.len);

                continue;
            }

            var operator: ?u8 = null;

            if (next.?.len == 1) {
                operator = switch (next.?[0]) {
                    '+', '-', '*', '/', '\\', '%', '$', '&', '|', '^', '<', '_', '>', '!' => next.?[0],
                    else => null
                };
            }

            var prefix_2: ?[]const u8 = null;
            var value_2:  ?u32        = null;

            var prefix_value_2: ?[]const u8 = null;

            if (operator == null) {
                prefix_value_2 = next.?;
            }
            else {
                prefix_value_2 = next_token(str.?);
            }
            
            prefix_2, value_2 = split_token(prefix_value_2.?);

            const w_err = Script700.compile_instruction(&w, mnemonic.?, .{
                .oper_1_prefix = prefix, .oper_1_value = value,
                .operator = operator, .oper_2_prefix = prefix_2, .oper_2_value = value_2
            });

            if (w_err) |ww| {
                w_slc = ww;
            }
            else |_| {
                ignore = true;
                continue;
            }

            for (w_slc.?, 0..) |word, i| {
                bytes[i] = u32le_to_u8_array(word);
            }

            instr_size = @intCast(w_slc.?.len);

            pc +%= @intCast(w_slc.?.len);
        }
        else {
            var data_byte_str = next_token(str.?);

            while (data_byte_str) |dbs| {
                // TODO: Figure out what to do with unparsable data sections
                const data_byte = try std.fmt.parseInt(u8, dbs, 16);
                try bin_data.append(data_byte);
                dc +%= 1;

                data_byte_str = next_token(str.?);
            }
        }
    }

    const data_bytes = u32le_to_u8_array(dc);
    inline for (0..4) |x| {
        bin_data.items[data_size_offset +% x] = data_bytes[x];
    }

    var final_data = try alloc.alloc(u8, bin_data.items.len);

    for (bin_data.items, 0..) |b, i| {
        final_data[i] = b;
    }
    
    return final_data;
}

var _token_parse_end: u32 = 0;

fn next_token(buffer: []const u8) ?[]const u8 {
    const buf_ = buffer[_token_parse_end ..];

    var start_found = false;
    
    var start: u32 = 0;
    var end:   u32 = 0;

    for (buf_, 0..) |b, i| {
        if (b != ' ' and b != '\t' and b != '\r' and b != '\n') {
            start = @intCast(i);
            start_found = true;
            break;
        }
    }

    if (!start_found) {
        return null;
    }

    for (start..buf_.len) |i| {
        if (buf_[i] == ' ' or buf_[i] == '\t' or buf_[i] == '\r' or buf_[i] == '\n') {
            end = @intCast(i);
            break;
        }
    }

    if (end == 0) {
        end = @intCast(buf_.len);
    }

    _token_parse_end +%= end;

    return buf_[start .. end];
}

fn peek_token(buffer: []const u8) ?[]const u8 {
    const buf_ = buffer[_token_parse_end ..];

    var start_found = false;
    
    var start: u32 = 0;
    var end:   u32 = 0;

    for (buf_, 0..) |b, i| {
        if (b != ' ' and b != '\t' and b != '\r' and b != '\n') {
            start = @intCast(i);
            start_found = true;
            break;
        }
    }

    if (!start_found) {
        return null;
    }

    for (start..buf_.len) |i| {
        if (buf_[i] == ' ' or buf_[i] == '\t' or buf_[i] == '\r' or buf_[i] == '\n') {
            end = @intCast(i);
            break;
        }
    }

    if (end == 0) {
        end = @intCast(buf_.len);
    }

    return buf_[start .. end];
}

fn split_token(str: []const u8) struct { ?[]const u8,
                                         ?u32         }
{
    var prefix: ?[]const u8 = null;
    var value:  ?u32        = null;

    var split_index: u32 = 0;
    var num_found = false;

    for (str, 0..) |c, i| {
        if (c == '$' or c >= '0' and c <= '9') {
            split_index = @intCast(i);
            num_found = true;
            break;
        }
    }

    if (!num_found) {
        split_index = @intCast(str.len);
    }

    prefix = str[0..split_index];
    var num_index = split_index;

    var is_hex = false;

    if (num_found) {
        if (str[split_index] == '$') {
            num_index +%= 1;
            is_hex = true;
        }
        else if (str.len >= 2 and std.mem.eql(u8, str[split_index .. (split_index + 2)], "0x")) {
            num_index +%= 2;
            is_hex = true;
        }

        if (is_hex) {
            value = std.fmt.parseInt(u32, str[num_index..], 16) catch null;
        }
        else {
            value = std.fmt.parseInt(u32, str[num_index..], 10) catch null;
        }
    }

    return .{
        prefix,
        if (str[num_index..].len == 0) null else value
    };
}

fn u32le_to_u8_array(in: u32) [4]u8 {
    var arr: [4]u8 = undefined;

    inline for (0..4) |b| {
        const bb: u32 = @intCast(b);
        arr[b] = @intCast(in >> bb * 8 & 0xFF);
    }

    return arr;
}

fn u8_array_to_u32le(in: []const u8) u32 {
    var res: u32 = 0;

    inline for (0..4) |b| {
        const bb: u32 = @intCast(b);
        res |= @as(u32, in[b]) << bb * 8;
    }

    return res;
}

fn quit() void {
    // Clear console and reset position
    std.debug.print("\x1B[H", .{});
    for (0..(db.max_lines + 2)) |_| {
        for (0..(db.cli_width)) |_| {
            std.debug.print(" ", .{});
        }
        std.debug.print("\n", .{});
    }
    std.debug.print("\x1B[H", .{});
    std.process.exit(0);
}