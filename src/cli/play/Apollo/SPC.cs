using System.Text;

namespace Apollo;

public class SPC {
	public class Metadata {
		// ID666 Main
		public string Title    { get; set; }
		public string Artist   { get; set; }
		public string Game     { get; set; }
		public string Dumper   { get; set; }
		public string Comments { get; set; }
		
		public UInt32? Month { get; set; }
		public UInt32? Day   { get; set; }
		public UInt32? Year  { get; set; }
		
		public string DateOther { get; set; }
		
		public UInt32? LengthInSeconds { get; set; }
		public UInt32? FadeLengthInMS  { get; set; }
		
		public bool[]? ChannelsDisabled { get; set; }
		
		public byte? EmulatorID { get; set; }
		
		// ID666 Extended
		public string  OSTTitle { get; set; }
		public byte?   OSTDisc  { get; set; }
		public byte[]? OSTTrack { get; set; }
		
		public string  Publisher     { get; set; }
		public UInt32? CopyrightYear { get; set; }
		
		public UInt32? IntroLengthInTimer2Steps { get; set; }
		public UInt32? LoopLengthInTimer2Steps  { get; set; }
		public UInt32? EndLengthInTimer2Steps   { get; set; }
		public byte?   LoopTimes                { get; set; }
		
		public byte? MixingLevel { get; set; }
		
		internal unsafe Metadata(DLL.SpcMetadata metadata) {
			Title    = getString(metadata.Title,    257);
			Artist   = getString(metadata.Artist,   257);
			Game     = getString(metadata.Game,     257);
			Dumper   = getString(metadata.Dumper,   257);
			Comments = getString(metadata.Comments, 257);
			
			Month = metadata.Month >= 0 ? (UInt32) metadata.Month : null;
			Day   = metadata.Month >= 0 ? (UInt32) metadata.Day   : null;
			Year  = metadata.Year  >= 0 ? (UInt32) metadata.Year  : null;
			
			DateOther = getString(metadata.DateOther, 12);
			
			LengthInSeconds = metadata.LengthInSeconds >= 0 ? (UInt32) metadata.LengthInSeconds : null;
			FadeLengthInMS  = metadata.FadeLengthInMS  >= 0 ? (UInt32) metadata.FadeLengthInMS  : null;
			
			ChannelsDisabled = new bool[8];
			for (var i = 0; i < 8; i++) {
				ChannelsDisabled[i] = metadata.ChannelsDisabled[i] != 0;
			}
			
			EmulatorID = metadata.EmulatorId >= 0 ? (byte) metadata.EmulatorId : null;
			
			OSTTitle = getString(metadata.OstTitle, 257);
			OSTDisc  = metadata.OstDisc >= 0 ? (byte) metadata.OstDisc : null;
			OSTTrack = metadata.HasOstTrack != 0 ? new Span<byte>(metadata.OstTrack, 2).ToArray() : null;
			
			Publisher = getString(metadata.Publisher, 257);
			CopyrightYear = metadata.CopyrightYear >= 0 ? (UInt32) metadata.CopyrightYear : null;
			
			IntroLengthInTimer2Steps = metadata.IntroLengthInTimer2Steps >= 0 ? (UInt32) metadata.IntroLengthInTimer2Steps : null;
			LoopLengthInTimer2Steps  = metadata.LoopLengthInTimer2Steps  >= 0 ? (UInt32) metadata.LoopLengthInTimer2Steps  : null;
			EndLengthInTimer2Steps   = metadata.EndLengthInTimer2Steps   >= 0 ? (UInt32) metadata.EndLengthInTimer2Steps   : null;
			LoopTimes                = metadata.LoopTimes                >= 0 ? (byte)   metadata.LoopTimes                : null;
			
			MixingLevel = metadata.MixingLevel >= 0 ? (byte) metadata.MixingLevel : null;
		}
	}
	
	static unsafe string getString(byte* ptr, int size) {
		var adjustedSize = size;
		
		for (var i = 0; i < size; i++) {
			if (*(ptr + i) == 0) {
				adjustedSize = i;
				break;
			}
		}
		
		ReadOnlySpan<byte> span = new(ptr, adjustedSize);
		
		try {
			return Encoding.UTF8.GetString(span);
		}
		catch (ArgumentException) { // Handle the case where string is not valid UTF-8 - For now, interpret each byte as code point
			StringBuilder sb = new();
			foreach (var c in span) {
				sb.Append((char) c);
			}
			return sb.ToString();
		}
	}
}