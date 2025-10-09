namespace Jimbl;

public static class Env {
	public static string ProgramDirectory => AppContext.BaseDirectory;
	public static string WorkingDirectory => Directory.GetCurrentDirectory();
}