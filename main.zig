const std = @import("std");

const db = @import("debug.zig");

const Emu  = @import("emu.zig").Emu;
const SDSP = @import("s_dsp.zig").SDSP;
const SSMP = @import("s_smp.zig").SSMP;

pub fn main() !void {
    Emu.static_init();

    var emu = Emu.new();
    emu.init(
        SDSP.new(&emu),
        SSMP.new(&emu, .{})
    );

    std.debug.print("Mode commands: \n", .{});
    std.debug.print("   i = Instruction trace log viewer [default] \n", .{});
    std.debug.print("   v = Memory viewer \n", .{});
    std.debug.print("Action commands: \n", .{});
    std.debug.print("   s = Step instruction [default] \n", .{});
    std.debug.print("   w = Write to IO port (snes -> spc) \n", .{});
    std.debug.print("   x = Send interrupt signal \n", .{});
    std.debug.print("   p = View previous page \n", .{});
    std.debug.print("   n = View next page \n", .{});
    std.debug.print("   u = Shift memory view up one row \n", .{});
    std.debug.print("   d = Shift memory view down one row \n", .{});
    std.debug.print("\n", .{});
    std.debug.print("Pressing enter without specifying the command repeats the previous action command. \n", .{});
    std.debug.print("\n", .{});

    const stdin = std.io.getStdIn().reader();
    var buffer: [8]u8 = undefined;

    emu.s_smp.enable_access_logs = true;
    emu.s_smp.enable_timer_logs = true;
    emu.s_smp.clear_access_logs();
    emu.s_smp.clear_timer_logs();
    emu.step();
    emu.step();

    //var last_second: u64 = 0;
    //
    //for (0..2048000000) |_| {
    //    emu.step();
    //    const cur_second = emu.s_dsp.cur_cycle() / 2048000;
    //    if (cur_second != last_second) {
    //        last_second = cur_second;
    //        std.debug.print("{d}\n", .{last_second});
    //    }
    //}

    var cur_page: u8 = 0x00;
    var cur_offset: u8 = 0x00;
    var cur_mode: u8 = 'i';
    var cur_action: u8 = 's';
    
    //var shadow_uploaded = false;

    while (true) {
        _ = stdin.readUntilDelimiterOrEof(buffer[0..], '\n') catch "";
        std.debug.print("\x1B[A\x1B[A", .{}); // ANSI escape code for cursor up (may not work on Windows)
        //std.debug.print("\x1B[A", .{}); // ANSI escape code for cursor up (may not work on Windows)

        const last_pc = emu.s_smp.spc.pc();
        const prev_state = emu.s_smp.state;

        //const shadow_routine: [19]u8 = [19]u8 {
        //    0x20,             //    clrp
        //    0xE5, 0x00, 0x02, //    mov a, $0200
        //    0xBC,             //    inc a
        //    0xC5, 0x00, 0x02, //    mov $0200, a
        //    0x8F, 0x01, 0xFC, //    mov $FC, #$01 (Set timer 2 period to 1)
        //    0x8F, 0x84, 0xF1, //    mov $F1, #$84 (Enable timer 2)
        //    0x8D, 0x3F,       //    mov y, #$3F
        //    0xFE, 0xFE,       // -: dbnz y, -
        //    0xC5,             //    mov ----, a
        //};

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

                const all_logs = emu.s_smp.get_access_logs(.{.exclude_at_end = 1});
                var logs = db.filter_access_logs(all_logs);

                //var buffer_writer = std.io.countingWriter(std.io.getStdOut().writer());
                //var writer = buffer_writer.writer();
//
                //for (all_logs) |log| {
                //    _ = db.print_log(&prev_state, &log, &writer, .{}) catch 0;
                //    std.debug.print("\n", .{});
                //}

                emu.s_smp.clear_access_logs();

                // Test:
                //if (last_pc == 0x0001 and !shadow_uploaded) {
                //    emu.s_smp.spc.upload_shadow_code(0x0200, shadow_routine[0..]);
                //    emu.enable_shadow_mode(.{.set_as_master = true});
                //    shadow_uploaded = true;
                //}
                //else if (last_pc == 0x0210) {
                //    emu.disable_shadow_execution(.{});
                //}

                if (cur_mode == 'i') {
                    db.print_logs(&prev_state, logs[0..]) catch {
                        std.debug.print("                                                ", .{});
                    };

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
                        db.print_timer_log(&log.?, .{.prefix =true });
                        std.debug.print("\x1B[39m\n", .{});
                    }
                }
                else if (cur_mode == 'v') {
                    std.debug.print("\x1B[2J\x1B[H", .{}); // Clear console and reset console position (may not work on Windows)
                    db.print_memory_page(&emu, cur_page, cur_offset, .{.prev_pc = last_pc, .prev_state = &prev_state, .logs = all_logs[0..]});
                }
            },
            else => {
                continue :sw cur_action;
            }
        }
        std.debug.print("Current DSP cycle: {d}\n", .{emu.s_dsp.last_processed_cycle});

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
