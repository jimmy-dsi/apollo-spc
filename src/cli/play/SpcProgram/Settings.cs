namespace SpcProgram;

using Apollo;
using Jimbl;
using Jimbl.JSON5;

public static partial class CliMain {
	public static string SettingsPath = Path.Join(Env.ProgramDirectory, "apollo.settings");
	static bool settingsLoaded = false;
	
	public static void LoadSettings(Emulator emu) {
		// Set up defaults, in case any properties could not be loaded
		string currentView    = "metadata";
		bool   useLowPass     = true;
		bool   fadeoutEnabled = true;
		bool   useSPCCycles   = false;
			
		bool[] mainChannels = new [] {
			true, true, true, true,
			true, true, true, true
		};
		bool[] echoChannels = new [] {
			true, true, true, true,
			true, true, true, true
		};
			
		int heatMap            = 0;
		int heatMapDataSizeInt = 4;
		
		JObject? settings = null;
		
		try {
			settings = JItem.Load(File.ReadAllText(SettingsPath));
		}
		catch (Exception) { }
		
		if (settings is null) {
			settingsLoaded = true;
			return;
		}
			
		// Map properties from settings
		if (settings.TryGetValue("current_view",     out var  view) && view  is JString v) currentView    = v;
		if (settings.TryGetValue("use_snes_lowpass", out var lpass) && lpass is JBool   L) useLowPass     = L;
		if (settings.TryGetValue("fadeout_enabled",  out var  fade) && fade  is JBool   f) fadeoutEnabled = f;
		
		if (settings.TryGetValue("cycle_format", out var cyclef) && cyclef is JString c) {
			string cs = c;
			if      (cs.ToLower() == "dsp") useSPCCycles = false;
			else if (cs.ToLower() == "spc") useSPCCycles = true;
		}
		
		if (settings.TryGetValue("main_channels", out var mchan) && mchan is JArray m) {
			List<bool> channels = [];
			bool convFailed = false;
			
			for (var i = 0; i < Math.Min(8, m.Count); i++) {
				if (m[i] is JBool b) channels.Add(b);
				else {
					convFailed = true;
					break;
				}
			}
			
			if (!convFailed) {
				for (var i = 0; i < channels.Count; i++) {
					mainChannels[i] = channels[i];
				}
			}
		}
		
		if (settings.TryGetValue("echo_channels", out var echan) && echan is JArray e) {
			List<bool> channels   = [];
			bool       convFailed = false;
			
			for (var i = 0; i < Math.Min(8, e.Count); i++) {
				if (e[i] is JBool b) channels.Add(b);
				else {
					convFailed = true;
					break;
				}
			}
			
			if (!convFailed) {
				for (var i = 0; i < channels.Count; i++) {
					echoChannels[i] = channels[i];
				}
			}
		}
		
		if (settings.TryGetValue("heat_map", out var hmap) && hmap is JNumber h) {
			if ((int) h is >= 0 and <= 2) heatMap = (int) h;
		}
		
		if (settings.TryGetValue("heat_map_datasize", out var hsize) && hsize is JNumber s) {
			if ((int) s is 1 or 2 or 4 or 8) heatMapDataSizeInt = (int) s;
		}
			
		// Apply settings - set values to internal UI variables and emulator
		for (var i = 0; i < 8; i++) {
			mainChannelsEnabled[i] = mainChannels[i];
			echoChannelsEnabled[i] = echoChannels[i];
			
			if (!mainChannels[i]) emu.DisableMainVoice(i);
			if (!echoChannels[i]) emu.DisableEchoVoice(i);
		}
			
		var loadView = mapView(currentView);
		if (loadView is View vw) {
			targetLoadView = vw;
		}
		
		initLPStatus  = useLowPass;
		lowpassStatus = initLPStatus;
		if (!initLPStatus) emu.LowpassEnabled = false;
		
		FadeoutsEnabled = fadeoutEnabled;
		
		cyclesInSpcClocks = useSPCCycles;
		heatMapEnabled    = heatMap > 0;
		heatMapMemMode    = heatMap == 1 ? HeatMapMode.TypeAware : HeatMapMode.Unsigned;
		heatMapDataSize   = heatMapDataSizeInt switch {
			1 => BusSize.Bit8,  2 => BusSize.Bit16,
			4 => BusSize.Bit32, 8 => BusSize.Bit64,
			_ => BusSize.Bit32
		};
		
		settingsLoaded = true;
	}
	
	static View? mapView(string view) {
		var v = view.ToLower();
		
		if (v == "metadata")  return View.Metadata;
		if (v == "dsp_1")     return View.DSPViewer1;
		if (v == "dsp_2")     return View.DSPViewer2;
		if (v == "dsp_3")     return View.DSPViewer3;
		if (v == "aram")      return View.MemoryViewer;
		if (v == "brr")       return View.BRRViewer;
		if (v == "echo")      return View.EchoViewer;
		if (v == "smp")       return View.SMPViewer;
		if (v == "script700") return View.Script700Viewer;
		if (v == "spc_asm")   return View.ASMViewer;
		
		return null;
	}
	
	static string serializeView(View view) {
		return view switch {
			View.Metadata        => "metadata",
			View.DSPViewer1      => "dsp_1",
			View.DSPViewer2      => "dsp_2",
			View.DSPViewer3      => "dsp_3",
			View.MemoryViewer    => "aram",
			View.BRRViewer       => "brr",
			View.EchoViewer      => "echo",
			View.SMPViewer       => "smp",
			View.Script700Viewer => "script700",
			View.ASMViewer       => "spc_asm",
			_ => "unknown"
		};
	}
	
	static int serializeHeatMapStatus() {
		if (heatMapEnabled) {
			if (heatMapMemMode == HeatMapMode.TypeAware) return 1;
			if (heatMapMemMode == HeatMapMode.Unsigned)  return 2;
		}
		return 0;
	}
	
	static int serializeHeatMapDataSize() {
		return heatMapDataSize switch {
			BusSize.Bit8  => 1, BusSize.Bit16 => 2,
			BusSize.Bit32 => 4, BusSize.Bit64 => 8,
			_ => 4
		};
	}
	
	public static void SaveSettings() {
		JObject settings = new() {
			["current_view"]      = serializeView(currentView),
			["use_snes_lowpass"]  = lowpassStatus,
			["fadeout_enabled"]   = FadeoutsEnabled,
			["cycle_format"]      = cyclesInSpcClocks ? "spc" : "dsp",
			["main_channels"]     = mainChannelsEnabled.Select(c => (JItem) c).ToArray(),
			["echo_channels"]     = echoChannelsEnabled.Select(c => (JItem) c).ToArray(),
			["heat_map"]          = serializeHeatMapStatus(),
			["heat_map_datasize"] = serializeHeatMapDataSize()
		};
		
		File.WriteAllText(SettingsPath, settings.Serialize());
	}
}