namespace Jimbl;

public static class Exts {
	public static bool Truthy(this object? obj) {
		if (obj is bool b) {
			return b;
		}
		else if (obj is null) {
			return false;
		}
		else if (obj is Exception) {
			return false;
		}
		else {
			return true;
		}
	}
	
	public static bool Truthy<T>(this object? obj, out T successVal) {
		var result = Truthy(obj);
		if (result) {
			successVal = (T) obj!;
		}
		else {
			successVal = default(T);
		}
		return result;
	}
	
	public static bool Falsy(this object? obj) => !Truthy(obj);
	
	public static bool Falsy(this object? obj, out Exception ex) {
		var result = Falsy(obj);
		if (result) {
			ex = (Exception) (obj ?? new NullReferenceException());
		}
		else {
			ex = default;
		}
		return result;
	}
	
	// Enumerable Extension Methods
	public static IEnumerable<(long, T)> Enum<T>(this IEnumerable<T> iterable) {
		long index = 0;
		foreach (var item in iterable) {
			yield return (index, item);
			index++;
		}
	}
}