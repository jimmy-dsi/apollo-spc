namespace Jimbl.JMath;

using System.Numerics;

public static class JMath {
	/// <summary>
	/// True modulus for floating points
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <returns>The mathematical modulus of x and y</returns>
	public static double Mod(double x, double y) {
		var r = x % y;
		if (r < 0) {
			r += Math.Abs(y);
		}
		return r;
	}
	
	/// <summary>
	/// Integer Power-of-2 Saturation algorithm
	/// </summary>
	/// <param name="n">Any arbitrary integer</param>
	/// <returns>The lowest power-of-2 value greater-than or equal to the input integer value</returns>
	public static int Sat(int n) {
		if (n == 0) {
			return 1;
		}
		
		var m = (uint) n;
		
		m--;
		m |= m >> 1;
		m |= m >> 2;
		m |= m >> 4;
		m |= m >> 8;
		m |= m >> 16;
		m++;
		
		return (int) m;
	}
	
	/// <summary>
	/// A Fast-Fourier-Transform method using an implementation of the Cooley-Tukey algorithm
	/// </summary>
	/// <param name="input">Time-domain array of complex number values</param>
	/// <param name="includeNegFreqs">Boolean specifying whether or not to include the second-half of the resulting array [default is false]</param>
	/// <returns>Transformed frequency-domain array, as complex numbers</returns>
	public static Complex[] FFT(Complex[] input, bool includeNegFreqs = false) {
		// Pad input to a power-of-2 length
		var N = Sat(input.Length);
		var padded = new Complex[N];
		
		for (var i = 0; i < input.Length; i++) {
			padded[i] = input[i];
		}
		
		var result = fft(padded);
		return includeNegFreqs ? result : result[0..(N / 2 + 1)];
	}
	
	/// <summary>
	/// A Fast-Fourier-Transform method using an implementation of the Cooley-Tukey algorithm
	/// </summary>
	/// <param name="input">Time-domain array of floating-point number values</param>
	/// <param name="includeNegFreqs">Boolean specifying whether or not to include the second-half of the resulting array [default is false]</param>
	/// <returns>Transformed frequency-domain array, as complex numbers</returns>
	public static Complex[] FFT(double[] input, bool includeNegFreqs = false) =>
		FFT(input.Select(c => (Complex) c).ToArray(), includeNegFreqs);
	
	/// <summary>
	/// A Fast-Fourier-Transform method using an implementation of the Cooley-Tukey algorithm
	/// </summary>
	/// <param name="input">Time-domain array of integer values</param>
	/// <param name="magnitude">The maximum possible integer value, for normalization</param>
	/// <param name="includeNegFreqs">Boolean specifying whether or not to include the second-half of the resulting array [default is false]</param>
	/// <returns>Transformed frequency-domain array, as complex numbers</returns>
	public static Complex[] FFT(int[] input, int magnitude, bool includeNegFreqs = false) =>
		FFT(input.Select(c => (Complex) c / magnitude).ToArray(), includeNegFreqs);
	
	/// <summary>
	/// A Fast-Fourier-Transform method using an implementation of the Cooley-Tukey algorithm
	/// </summary>
	/// <param name="input">Time-domain array of complex number values</param>
	/// <param name="includeNegFreqs">Boolean specifying whether or not to include the second-half of the resulting array [default is false]</param>
	/// <returns>The magnitude array of the transformed frequency-domain result, as floating-point numbers</returns>
	public static double[] FFT_Gain(Complex[] input, bool includeNegFreqs = false) =>
		FFT(input, includeNegFreqs).Select(c => c.Magnitude).ToArray();
	
	/// <summary>
	/// A Fast-Fourier-Transform method using an implementation of the Cooley-Tukey algorithm
	/// </summary>
	/// <param name="input">Time-domain array of floating-point number values</param>
	/// <param name="includeNegFreqs">Boolean specifying whether or not to include the second-half of the resulting array [default is false]</param>
	/// <returns>The magnitude array of the transformed frequency-domain result, as floating-point numbers</returns>
	public static double[] FFT_Gain(double[] input, bool includeNegFreqs = false) =>
		FFT(input, includeNegFreqs).Select(c => c.Magnitude).ToArray();
	
	/// <summary>
	/// A Fast-Fourier-Transform method using an implementation of the Cooley-Tukey algorithm
	/// </summary>
	/// <param name="input">Time-domain array of integer values</param>
	/// <param name="magnitude">The maximum possible integer value, for normalization</param>
	/// <param name="includeNegFreqs">Boolean specifying whether or not to include the second-half of the resulting array [default is false]</param>
	/// <returns>The magnitude array of the transformed frequency-domain result, as floating-point numbers</returns>
	public static double[] FFT_Gain(int[] input, int magnitude, bool includeNegFreqs = false) =>
		FFT(input, magnitude, includeNegFreqs).Select(c => c.Magnitude).ToArray();
	
	static Complex[] fft(Complex[] input) {
		var N = input.Length;
		
		if (N <= 1) {
			return input;
		}
		
		var even = new Complex[N / 2];
		var odd  = new Complex[N / 2];
		
		for (var i = 0; i < N / 2; i++) {
			even[i] = input[i * 2];
			 odd[i] = input[i * 2 + 1];
		}
		
		var P = fft(even);
		var Q = fft(odd);
		
		var output = new Complex[N];
		var angleFactor = (-Math.PI * 2) / N;
		
		for (var k = 0; k < N / 2; k++) {
			var w = Complex.FromPolarCoordinates(1, angleFactor * k);
			
			var p = P[k];
			var q = Q[k] * w;
			
			output[k]         = p + q;
			output[k + N / 2] = p - q;
		}
		
		return output;
	}
}