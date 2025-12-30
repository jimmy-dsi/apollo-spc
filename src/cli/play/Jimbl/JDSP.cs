namespace Jimbl;

public static class JDSP {
	public static double AmpToDB(double amplitude) {
		return 20 * Math.Log10(amplitude);
	}
	
	public static double DBToAmp(double db) {
		return Math.Pow(10, db / 20);
	}
}