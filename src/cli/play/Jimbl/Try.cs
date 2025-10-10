namespace Jimbl;

public static class Try {
	public static object? Catch<T>(Func<T> attemptor) {
		try {
			return attemptor();
		}
		catch (Exception ex) {
			return ex;
		}
	}
	
	public static T Catch<T>(Func<T> attemptor, T defaultValue) {
		try {
			return attemptor();
		}
		catch (Exception) {
			return defaultValue;
		}
	}
	
	public static T Catch<T>(Func<T> attemptor, Func<T> resolver) {
		try {
			return attemptor();
		}
		catch (Exception) {
			return resolver();
		}
	}
	
	public static T Catch<T, E>(Func<T> attemptor, Func<E, T> resolver) where E: Exception {
		try {
			return attemptor();
		}
		catch (E ex) {
			return resolver(ex);
		}
	}
	
	public static object? CatchFinally<T>(Func<T> attemptor, Action finalizer) {
		try {
			return attemptor();
		}
		catch (Exception ex) {
			return ex;
		}
		finally {
			finalizer();
		}
	}
	
	public static T CatchFinally<T>(Func<T> attemptor, T defaultValue, Action finalizer) {
		try {
			return attemptor();
		}
		catch (Exception) {
			return defaultValue;
		}
		finally {
			finalizer();
		}
	}
	
	public static T CatchFinally<T>(Func<T> attemptor, Func<T> resolver, Action finalizer) {
		try {
			return attemptor();
		}
		catch (Exception) {
			return resolver();
		}
		finally {
			finalizer();
		}
	}
	
	public static T CatchFinally<T, E>(Func<T> attemptor, Func<E, T> resolver, Action finalizer) where E: Exception {
		try {
			return attemptor();
		}
		catch (E ex) {
			return resolver(ex);
		}
		finally {
			finalizer();
		}
	}
	
	public static T? CatchNull<T>(Func<T?> attemptor) where T: class {
		return Catch(attemptor, (T?) null);
	}
	
	public static T? CatchNull<T>(Func<T?> attemptor) where T: struct {
		return Catch(attemptor, (T?) null);
	}
	
	public static T? CatchNullFinally<T>(Func<T?> attemptor, Action finalizer) where T: class {
		return CatchFinally(attemptor, (T?) null, finalizer);
	}
	
	public static T? CatchNullFinally<T>(Func<T?> attemptor, Action finalizer) where T: struct {
		return CatchFinally(attemptor, (T?) null, finalizer);
	}

	public static T Finally<T>(Func<T> attemptor, Action finalizer) {
		try {
			return attemptor();
		}
		finally {
			finalizer();
		}
	}
}