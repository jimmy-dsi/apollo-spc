namespace Apollo;

public static class Lib {
	public static void Init() {
		var status = DLL.Init();
		if (!status) {
			throw new InitError();
		}
	}
	
	public static void Deinit() {
		var status = DLL.Deinit();
		if (!status) {
			throw new DeinitError();
		}
	}
}