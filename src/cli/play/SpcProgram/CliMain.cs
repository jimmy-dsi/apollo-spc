namespace SpcProgram;

using Apollo;
using Jimbl;

public static class CliMain {
	static Emulator emu;
	static string spcFilePath;
	
	public static int Main(string[] args) {
		Lib.Init();
		try {
			if (args.Length == 0) {
				Console.Error.WriteLine($"error: SPC file not provided");
				return 1;
			}
			
			spcFilePath = args[0];
			
			//emu = LibTest.Test(spcFilePath);
			emu = new(setAsMain: true, makeShared: true);
			emu.LoadSpcFile(spcFilePath);
			emu.SMP.LoggingEnabled = true;
			
			Console.Clear();
			AudioOutput.Setup(emu, handleUI);
		}
		catch (SpcMissingHeaderError) {
			Console.Error.WriteLine($"error: An unknown error occurred while attempting to process SPC metadata");
			return 1;
		}
		catch (IOException) {
			Console.Error.WriteLine($"error: The SPC file '{spcFilePath}' was not found or could not be loaded");
			return 1;
		}
		finally {
			Lib.Deinit();
		}
		
		return 0;
	}
	
	enum Menu {
		Metadata,
		Help,
		ASMViewer,
		MemoryViewer,
		DSPViewer1,
		DSPViewer2,
		DSPViewer3,
		Script700Viewer,
	}
	
	static Menu   currentMenu = Menu.Metadata;
	static string menuBarMsg  = "Press CTRL+H for help menu";
	
	static void handleUI(EmuDataBuffer? buffer) {
		switch (currentMenu) {
			case Menu.Metadata: {
				showMetadata();
				break;
			}
			
			default: {
				break;
			}
		}
		
		// Display Seek Bar
		if (buffer is not null) {
			Display.ClearLine(Display.Height - 2);
			Display.Write(formatTime((int) (buffer.DSPCycle / 32), TimeUnit.Timer2s), 0, Display.Height - 3, Color.Cyan);
			
			var fullTimeInCycles = (long) (emu.SpcMetadata.LengthInSeconds ?? 600) * 2048000;
			var barLength = Display.Width - 1 - 14;
			
			var cursorPos = (int) ((double) buffer.DSPCycle / fullTimeInCycles * barLength);
			Display.Write(new string('=', cursorPos) + '|', 14, Display.Height - 3, Color.Cyan);
		}
		
		Display.Write("[", 13,                Display.Height - 3, Color.Cyan);
		Display.Write("]", Display.Width - 1, Display.Height - 3, Color.Cyan);
		
		// Display Menu Bar
		Display.ClearLine(Display.Height - 1, Color.BGBlue);
		Display.Write(menuBarMsg, 0, Display.Height - 1, Color.BGBlue);
		
		if (buffer is not null) {
			var cycleCounter = $"DSP Cycle: {buffer.DSPCycle}";
			Display.Write(cycleCounter, Display.Width - 1 - cycleCounter.Length, Display.Height - 1, Color.BGBlue);
		}
		
		Console.Write(Display.Flush());
	}
	
	static void showMetadata() {
		Display.WriteBox(["Title:", "Artist:", "Game:", "Dumper:", "Comments:"], 1, 1);
		var maxFieldWidth = Display.Width - 18;
		
		var titleField    = Display.WordWrap(emu.SpcMetadata.Title,    maxFieldWidth, 1);
		var artistField   = Display.WordWrap(emu.SpcMetadata.Artist,   maxFieldWidth, 1);
		var gameField     = Display.WordWrap(emu.SpcMetadata.Game,     maxFieldWidth, 1);
		var dumperField   = Display.WordWrap(emu.SpcMetadata.Dumper,   maxFieldWidth, 1);
		var commentsField = Display.WordWrap(emu.SpcMetadata.Comments, maxFieldWidth, 3);
		
		if (titleField[0].TrimEnd() != "")  Display.WriteBox(titleField,  17, 1);
		else                                Display.WriteBox(["<none>"],  17, 1, Color.CGreen);
		if (gameField[0].TrimEnd() != "")   Display.WriteBox(gameField,   17, 2);
		else                                Display.WriteBox(["<none>"],  17, 2, Color.CGreen);
		if (artistField[0].TrimEnd() != "") Display.WriteBox(artistField, 17, 3);
		else                                Display.WriteBox(["<none>"],  17, 3, Color.CGreen);
		if (dumperField[0].TrimEnd() != "") Display.WriteBox(dumperField, 17, 4);
		else                                Display.WriteBox(["<none>"],  17, 4, Color.CGreen);
		
		if (commentsField.Length > 1 || commentsField[0].TrimEnd() != "") {
			Display.WriteBox(commentsField, 17, 5);
		}
		else {
			Display.WriteBox(["<none>"], 17, 5, Color.CGreen);
		}
		
		var y = 5 + commentsField.Length;
		Display.WriteBox([
			"Date Dumped:",
			"Song Length:",
			"Fade Time:",
			"Channel States:",
			"",
			"Emulator ID:",
			"OST Title:",
			"OST Disc:",
			"OST Track:",
			"Publisher:",
			"Copyright Year:",
			"Intro Length:",
			"Loop Length:",
			"End Length:",
			"Loop Count:",
			"Mixing Level:",
		], 1, y);
		
		var bottom = Display.Y + 1;
		
		// Display Date
		string? dateText = null;
		if (emu.SpcMetadata.DateOther != "") {
			dateText = emu.SpcMetadata.DateOther;
		}
		else if (emu.SpcMetadata.Year is not null && emu.SpcMetadata.Month is not null && emu.SpcMetadata.Day is not null) {
			dateText = $"{emu.SpcMetadata.Year:D4}-{emu.SpcMetadata.Month:D2}-{emu.SpcMetadata.Day:D2}";
		}
		var (dateField, dateColor) = drawTextField(dateText);
		
		// Display Song Length
		string? songLengthText = null;
		if (emu.SpcMetadata.LengthInSeconds is not null) {
			songLengthText = formatTime(emu.SpcMetadata.LengthInSeconds.Value.SafeSigned(), TimeUnit.Seconds);
		}
		var (slField, slColor) = drawTextField(songLengthText);
		
		// Display Fade Length
		string? fadeLengthText = null;
		if (emu.SpcMetadata.FadeLengthInMS is not null) {
			fadeLengthText = formatTime(emu.SpcMetadata.FadeLengthInMS.Value.SafeSigned(), TimeUnit.MS);
		}
		var (flField, flColor) = drawTextField(fadeLengthText);
		
		// Display Emulator ID
		string? emuIdText = null;
		if (emu.SpcMetadata.EmulatorID is not null) {
			emuIdText = $"{emu.SpcMetadata.EmulatorID}";
		}
		var (emuField, emuColor) = drawTextField(emuIdText);
		
		// Display OST Title
		var (osttField, osttColor) = drawTextField(Display.WordWrap(emu.SpcMetadata.OSTTitle, maxFieldWidth, 1)[0].TrimEnd());
		
		// Display OST Disc
		string? ostDiscText = null;
		if (emu.SpcMetadata.OSTDisc is not null) {
			ostDiscText = $"{emu.SpcMetadata.OSTDisc}";
		}
		var (ostdField, ostdColor) = drawTextField(ostDiscText);
		
		// Display OST Track
		string? ostTrackText = null;
		if (emu.SpcMetadata.OSTTrack?[1] is not null) {
			if (emu.SpcMetadata.OSTTrack?[0] is >= 0x21 and <= 0x7E) {
				ostTrackText = $"{(char) emu.SpcMetadata.OSTTrack[0]}{emu.SpcMetadata.OSTTrack[1]}";
			}
			else {
				ostTrackText = $"{emu.SpcMetadata.OSTTrack![1]}";
			}
		}
		var (ostrField, ostrColor) = drawTextField(ostTrackText);
		
		// Display Publisher
		var (pubField, pubColor) = drawTextField(Display.WordWrap(emu.SpcMetadata.Publisher, maxFieldWidth, 1)[0].TrimEnd());
		
		// Display Copyright Year
		string? copyYearText = null;
		if (emu.SpcMetadata.CopyrightYear is not null) {
			copyYearText = $"{emu.SpcMetadata.CopyrightYear}";
		}
		var (cpyField, cpyColor) = drawTextField(copyYearText);
		
		// Display Intro Length
		string? introLenText = null;
		if (emu.SpcMetadata.IntroLengthInTimer2Steps is not null) {
			introLenText = formatTime(emu.SpcMetadata.IntroLengthInTimer2Steps.Value.SafeSigned(), TimeUnit.Timer2s);
		}
		var (inlenField, inlenColor) = drawTextField(introLenText);
		
		// Display Loop Length
		string? loopLenText = null;
		if (emu.SpcMetadata.LoopLengthInTimer2Steps is not null) {
			loopLenText = formatTime(emu.SpcMetadata.LoopLengthInTimer2Steps.Value.SafeSigned(), TimeUnit.Timer2s);
		}
		var (lplenField, lplenColor) = drawTextField(loopLenText);
		
		// Display End Length
		string? endLenText = null;
		if (emu.SpcMetadata.EndLengthInTimer2Steps is not null) {
			endLenText = formatTime(emu.SpcMetadata.EndLengthInTimer2Steps.Value.SafeSigned(), TimeUnit.Timer2s);
		}
		var (endlenField, endlenColor) = drawTextField(endLenText);
		
		// Display Loop Count
		string? loopCountText = null;
		if (emu.SpcMetadata.LoopTimes is not null) {
			loopCountText = $"{emu.SpcMetadata.LoopTimes}";
		}
		var (lcField, lcColor) = drawTextField(loopCountText);
		
		// Display Mixing Level
		string? MixingLevelText = null;
		if (emu.SpcMetadata.MixingLevel is not null) {
			MixingLevelText = $"{emu.SpcMetadata.MixingLevel}/255";
		}
		var (mlField, mlColor) = drawTextField(MixingLevelText);
		
		// Display Fields
		Display.Write(dateField, 17, y, dateColor); y++;
		Display.Write(slField,   17, y,   slColor); y++;
		Display.Write(flField,   17, y,   flColor); y++;
		for (var xx = 0; xx < 4; xx++) {
			for (var yy = 0; yy < 2; yy++) {
				var ci = yy * 4 + xx;
				Display.Write(emu.SpcMetadata.ChannelsDisabled[ci] ? $"#{ci}: Disabled" : $"#{ci}:  Enabled", 17 + 16 * xx, y + yy);
			}
		}
		y += 2;
		Display.Write(emuField,    17, y,    emuColor); y++;
		Display.Write(osttField,   17, y,   osttColor); y++;
		Display.Write(ostdField,   17, y,   ostdColor); y++;
		Display.Write(ostrField,   17, y,   ostrColor); y++;
		Display.Write(pubField,    17, y,    pubColor); y++;
		Display.Write(cpyField,    17, y,    cpyColor); y++;
		Display.Write(inlenField,  17, y,  inlenColor); y++;
		Display.Write(lplenField,  17, y,  lplenColor); y++;
		Display.Write(endlenField, 17, y, endlenColor); y++;
		Display.Write(lcField,     17, y,     lcColor); y++;
		Display.Write(mlField,     17, y,     mlColor); y++;
		
		Display.DrawOutline(0, 0, Display.Width, bottom, removeSides: true);
	}
	
	enum TimeUnit {
		Seconds, MS, Timer2s
	}
	
	static string formatTime(int input, TimeUnit unit) {
		TimeSpan length = new();
		
		switch (unit) {
			case TimeUnit.Seconds: {
				length = new(hours: 0, minutes: 0, seconds: input);
				break;
			}
				
			case TimeUnit.MS: {
				length = new(days: 0, hours: 0, minutes: 0, seconds: 0, milliseconds: input);
				break;
			}
				
			case TimeUnit.Timer2s: {
				const long NS_InTimer2 = 15625;
				var ns = input * NS_InTimer2;
				length = new(ticks: ns / 100);
				break;
			}
		}
		
		return $"{(int) length.TotalHours:D2}:{length.Minutes:D2}:{length.Seconds:D2}.{length.Milliseconds:D3}";
	}
	
	static (string, Color?) drawTextField(string? text, Color? col = null) {
		if ((text ?? "") == "") {
			return ("<none>", new(ansiCode: 32));
		}
		else {
			return (text!, col);
		}
	}
}