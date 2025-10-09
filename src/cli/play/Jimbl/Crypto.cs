namespace Jimbl;

using System.Text;
using System.Security.Cryptography;

public static class Crypto {
	public static string HashFileSHA256(string filePath) {
		using var sha256     = SHA256.Create();
		using var fileStream = File.OpenRead(filePath);
		
		// Compute hash of file stream
		var hashBytes = sha256.ComputeHash(fileStream);
		
		// Convert resulting byte array to hex string
		StringBuilder sb = new();
		foreach (var b in hashBytes) {
			sb.Append(b.ToString("X2"));
		}
		return sb.ToString();
	}
}