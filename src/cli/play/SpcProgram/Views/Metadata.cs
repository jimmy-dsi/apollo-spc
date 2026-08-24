namespace SpcProgram;

using Apollo;
using Jimbl;
using Jimbl.Graphics;

public static partial class CliMain {
	static void showMetadata() {
		Display.WriteBox(["Title:", "Artist:", "Game:", "Dumper:", "Comments:"], 1, 1);
		var maxFieldWidth = Display.Width - 18;
		
		var titleField    = Display.WordWrap(PrimaryEmu.SpcMetadata.Title,    maxFieldWidth, 1);
		var artistField   = Display.WordWrap(PrimaryEmu.SpcMetadata.Artist,   maxFieldWidth, 1);
		var gameField     = Display.WordWrap(PrimaryEmu.SpcMetadata.Game,     maxFieldWidth, 1);
		var dumperField   = Display.WordWrap(PrimaryEmu.SpcMetadata.Dumper,   maxFieldWidth, 1);
		var commentsField = Display.WordWrap(PrimaryEmu.SpcMetadata.Comments, maxFieldWidth, 3);
		
		if (titleField[0].TrimEnd() != "")  Display.WriteBox(titleField,  17, 1);
		else                                Display.WriteBox(["<none>"],  17, 1, AnsiColor.Green);
		if (artistField[0].TrimEnd() != "") Display.WriteBox(artistField, 17, 2);
		else                                Display.WriteBox(["<none>"],  17, 2, AnsiColor.Green);
		if (gameField[0].TrimEnd() != "")   Display.WriteBox(gameField,   17, 3);
		else                                Display.WriteBox(["<none>"],  17, 3, AnsiColor.Green);
		if (dumperField[0].TrimEnd() != "") Display.WriteBox(dumperField, 17, 4);
		else                                Display.WriteBox(["<none>"],  17, 4, AnsiColor.Green);
		
		if (commentsField.Length > 1 || commentsField[0].TrimEnd() != "") {
			Display.WriteBox(commentsField, 17, 5);
		}
		else {
			Display.WriteBox(["<none>"], 17, 5, AnsiColor.Green);
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
		if (PrimaryEmu.SpcMetadata.DateOther != "") {
			dateText = PrimaryEmu.SpcMetadata.DateOther;
		}
		else if (PrimaryEmu.SpcMetadata.Year is not null && PrimaryEmu.SpcMetadata.Month is not null && PrimaryEmu.SpcMetadata.Day is not null) {
			dateText = $"{PrimaryEmu.SpcMetadata.Year:D4}-{PrimaryEmu.SpcMetadata.Month:D2}-{PrimaryEmu.SpcMetadata.Day:D2}";
		}
		var (dateField, dateColor) = drawTextField(dateText);
		
		// Display Song Length
		string? songLengthText = null;
		if (PrimaryEmu.SpcMetadata.LengthInSeconds is not null) {
			songLengthText = formatTime(PrimaryEmu.SpcMetadata.LengthInSeconds.Value.SafeSigned(), TimeUnit.Seconds);
		}
		var (slField, slColor) = drawTextField(songLengthText);
		
		// Display Fade Length
		string? fadeLengthText = null;
		if (PrimaryEmu.SpcMetadata.FadeLengthInMS is not null) {
			fadeLengthText = formatTime(PrimaryEmu.SpcMetadata.FadeLengthInMS.Value.SafeSigned(), TimeUnit.MS);
		}
		var (flField, flColor) = drawTextField(fadeLengthText);
		
		// Display Emulator ID
		string? emuIdText = null;
		if (PrimaryEmu.SpcMetadata.EmulatorID is not null) {
			emuIdText = $"{PrimaryEmu.SpcMetadata.EmulatorID}";
		}
		var (emuField, emuColor) = drawTextField(emuIdText);
		
		// Display OST Title
		var (osttField, osttColor) = drawTextField(Display.WordWrap(PrimaryEmu.SpcMetadata.OSTTitle, maxFieldWidth, 1)[0].TrimEnd());
		
		// Display OST Disc
		string? ostDiscText = null;
		if (PrimaryEmu.SpcMetadata.OSTDisc is not null) {
			ostDiscText = $"{PrimaryEmu.SpcMetadata.OSTDisc}";
		}
		var (ostdField, ostdColor) = drawTextField(ostDiscText);
		
		// Display OST Track
		string? ostTrackText = null;
		if (PrimaryEmu.SpcMetadata.OSTTrack?[1] is not null) {
			if (PrimaryEmu.SpcMetadata.OSTTrack?[0] is >= 0x21 and <= 0x7E) {
				ostTrackText = $"{(char) PrimaryEmu.SpcMetadata.OSTTrack[0]}{PrimaryEmu.SpcMetadata.OSTTrack[1]}";
			}
			else {
				ostTrackText = $"{PrimaryEmu.SpcMetadata.OSTTrack![1]}";
			}
		}
		var (ostrField, ostrColor) = drawTextField(ostTrackText);
		
		// Display Publisher
		var (pubField, pubColor) = drawTextField(Display.WordWrap(PrimaryEmu.SpcMetadata.Publisher, maxFieldWidth, 1)[0].TrimEnd());
		
		// Display Copyright Year
		string? copyYearText = null;
		if (PrimaryEmu.SpcMetadata.CopyrightYear is not null) {
			copyYearText = $"{PrimaryEmu.SpcMetadata.CopyrightYear}";
		}
		var (cpyField, cpyColor) = drawTextField(copyYearText);
		
		// Display Intro Length
		string? introLenText = null;
		if (PrimaryEmu.SpcMetadata.IntroLengthInTimer2Steps is not null) {
			introLenText = formatTime(PrimaryEmu.SpcMetadata.IntroLengthInTimer2Steps.Value.SafeSigned(), TimeUnit.Timer2s);
		}
		var (inlenField, inlenColor) = drawTextField(introLenText);
		
		// Display Loop Length
		string? loopLenText = null;
		if (PrimaryEmu.SpcMetadata.LoopLengthInTimer2Steps is not null) {
			loopLenText = formatTime(PrimaryEmu.SpcMetadata.LoopLengthInTimer2Steps.Value.SafeSigned(), TimeUnit.Timer2s);
		}
		var (lplenField, lplenColor) = drawTextField(loopLenText);
		
		// Display End Length
		string? endLenText = null;
		if (PrimaryEmu.SpcMetadata.EndLengthInTimer2Steps is not null) {
			endLenText = formatTime(PrimaryEmu.SpcMetadata.EndLengthInTimer2Steps.Value.SafeSigned(), TimeUnit.Timer2s);
		}
		var (endlenField, endlenColor) = drawTextField(endLenText);
		
		// Display Loop Count
		string? loopCountText = null;
		if (PrimaryEmu.SpcMetadata.LoopTimes is not null) {
			loopCountText = $"{PrimaryEmu.SpcMetadata.LoopTimes}";
		}
		var (lcField, lcColor) = drawTextField(loopCountText);
		
		// Display Mixing Level
		string? MixingLevelText = null;
		if (PrimaryEmu.SpcMetadata.MixingLevel is not null) {
			MixingLevelText = $"{PrimaryEmu.SpcMetadata.MixingLevel}/255";
		}
		var (mlField, mlColor) = drawTextField(MixingLevelText);
		
		// Display Fields
		Display.Write(dateField, 17, y, dateColor); y++;
		Display.Write(slField,   17, y,   slColor); y++;
		Display.Write(flField,   17, y,   flColor); y++;
		for (var xx = 0; xx < 4; xx++) {
			for (var yy = 0; yy < 2; yy++) {
				var ci = yy * 4 + xx;
				Display.Write(PrimaryEmu.SpcMetadata.ChannelsDisabled[ci] ? $"#{ci}: Disabled" : $"#{ci}:  Enabled", 17 + 16 * xx, y + yy);
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
}