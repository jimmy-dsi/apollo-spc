namespace Jimbl;

using System.Diagnostics.CodeAnalysis;

public interface Union {
	public object? Object { get; }
	
	public Type GetType() {
		return Object.Type();
	}
	
	public T? As<T>() {
		if (Object is T t) {
			return t;
		}
		else {
			return (T?) (object?) null;
		}
	}
	
	public bool Is<T>(out T v) {
		if (Object is T t) {
			v = t;
			return true;
		}
		else {
			v = default(T);
			return false;
		}
	}
}

public interface Union<T1, T2>: Union {
	[return: MaybeNull]
	T Union.As<T>() {
		if (Object is T t) {
			return t;
		}
		else if (Object is T1 or T2) {
			return (T?) (object?) null;
		}
		else {
			throw new ArgumentException($"Union can never be of type '{typeof(T).FullName}'");
		}
	}
	
	public T1? As(T1? defaultValue) {
		return Object is T1 t ? t : defaultValue;
	}
	
	public T2? As(T2? defaultValue) {
		return Object is T2 t ? t : defaultValue;
	}
	
	bool Union.Is<T>(out T v) {
		if (Object is T t) {
			v = t;
			return true;
		}
		else if (Object is T1 or T2) {
			v = default(T);
			return false;
		}
		else {
			throw new ArgumentException($"Union can never be of type '{typeof(T).FullName}'");
		}
	}
	
	public R Match<R>(Func<T1, R> resultIfT1, Func<T2, R> resultIfT2) {
		return Object switch {
			T1 v1 => resultIfT1(v1),
			T2 v2 => resultIfT2(v2),
			_     => throw new NullReferenceException()
		};
	}
	
	public R Match<R>(Func<T1, R> resultIfT1, Func<T2, R> resultIfT2, Func<R> resultIfNull) {
		return Object switch {
			T1 v1 => resultIfT1(v1),
			T2 v2 => resultIfT2(v2),
			_     => resultIfNull(),
		};
	}
	
	public void Switch(Action<T1> actionIfT1, Action<T2> actionIfT2) {
		switch (Object) {
			case T1 v1:
				actionIfT1(v1);
				break;
			case T2 v2:
				actionIfT2(v2);
				break;
			default:
				throw new NullReferenceException();
		}
	}
	
	public void Switch(Action<T1> actionIfT1, Action<T2> actionIfT2, Action actionIfNull) {
		switch (Object) {
			case T1 v1:
				actionIfT1(v1);
				break;
			case T2 v2:
				actionIfT2(v2);
				break;
			default:
				actionIfNull();
				break;
		}
	}
}

public interface Union<T1, T2, T3>: Union {
	[return: MaybeNull]
	T Union.As<T>() {
		if (Object is T t) {
			return t;
		}
		else if (Object is T1 or T2 or T3) {
			return (T?) (object?) null;
		}
		else {
			throw new ArgumentException($"Union can never be of type '{typeof(T).FullName}'");
		}
	}
	
	public T1? As(T1? defaultValue) {
		return Object is T1 t ? t : defaultValue;
	}
	
	public T2? As(T2? defaultValue) {
		return Object is T2 t ? t : defaultValue;
	}
	
	public T3? As(T3? defaultValue) {
		return Object is T3 t ? t : defaultValue;
	}
	
	bool Union.Is<T>(out T v) {
		if (Object is T t) {
			v = t;
			return true;
		}
		else if (Object is T1 or T2 or T3) {
			v = default(T);
			return false;
		}
		else {
			throw new ArgumentException($"Union can never be of type '{typeof(T).FullName}'");
		}
	}
	
	public R Match<R>(Func<T1, R> resultIfT1, Func<T2, R> resultIfT2, Func<T3, R> resultIfT3) {
		return Object switch {
			T1 v1 => resultIfT1(v1),
			T2 v2 => resultIfT2(v2),
			T3 v3 => resultIfT3(v3),
			_     => throw new NullReferenceException()
		};
	}
	
	public R Match<R>(Func<T1, R> resultIfT1, Func<T2, R> resultIfT2, Func<T3, R> resultIfT3, Func<R> resultIfNull) {
		return Object switch {
			T1 v1 => resultIfT1(v1),
			T2 v2 => resultIfT2(v2),
			T3 v3 => resultIfT3(v3),
			_     => resultIfNull(),
		};
	}
	
	public void Switch(Action<T1> actionIfT1, Action<T2> actionIfT2, Action<T3> actionIfT3) {
		switch (Object) {
			case T1 v1:
				actionIfT1(v1);
				break;
			case T2 v2:
				actionIfT2(v2);
				break;
			case T3 v3:
				actionIfT3(v3);
				break;
			default:
				throw new NullReferenceException();
		}
	}
	
	public void Switch(Action<T1> actionIfT1, Action<T2> actionIfT2, Action<T3> actionIfT3, Action actionIfNull) {
		switch (Object) {
			case T1 v1:
				actionIfT1(v1);
				break;
			case T2 v2:
				actionIfT2(v2);
				break;
			case T3 v3:
				actionIfT3(v3);
				break;
			default:
				actionIfNull();
				break;
		}
	}
}

public class union<T1, T2>: Union<T1, T2> {
	object? value;
	
	public object? Object => value;
	
	union() { }
	
	public union(T1 initValue) {
		value = initValue;
	}
	
	public union(T2 initValue) {
		value = initValue;
	}
	
	public static implicit operator union<T2, T1> (union<T1, T2> union) {
		union<T2, T1> u = new() {
			value = union.value
		};
		return u;
	}
	
	public static implicit operator union<T1, T2> (T1 value) => new(value);
	public static implicit operator union<T1, T2> (T2 value) => new(value);
	
	public static explicit operator T1 (union<T1, T2> value) => (T1) value.value;
	public static explicit operator T2 (union<T1, T2> value) => (T2) value.value;
	
	public static explicit operator   Byte (union<T1, T2> value) => (  Byte) Convert.ChangeType(value.value, typeof(  Byte))!;
	public static explicit operator  SByte (union<T1, T2> value) => ( SByte) Convert.ChangeType(value.value, typeof( SByte))!;
	public static explicit operator UInt16 (union<T1, T2> value) => (UInt16) Convert.ChangeType(value.value, typeof(UInt16))!;
	public static explicit operator  Int16 (union<T1, T2> value) => ( Int16) Convert.ChangeType(value.value, typeof( Int16))!;
	public static explicit operator UInt32 (union<T1, T2> value) => (UInt32) Convert.ChangeType(value.value, typeof(UInt32))!;
	public static explicit operator  Int32 (union<T1, T2> value) => ( Int32) Convert.ChangeType(value.value, typeof( Int32))!;
	public static explicit operator UInt64 (union<T1, T2> value) => (UInt64) Convert.ChangeType(value.value, typeof(UInt64))!;
	public static explicit operator  Int64 (union<T1, T2> value) => ( Int64) Convert.ChangeType(value.value, typeof( Int64))!;
	public static explicit operator Single (union<T1, T2> value) => (Single) Convert.ChangeType(value.value, typeof(Single))!;
	public static explicit operator Double (union<T1, T2> value) => (Double) Convert.ChangeType(value.value, typeof(Double))!;
	
	public T? As<T>() => ((Union<T1, T2>) this).As<T>();
	
	public T1? As(T1? defaultValue) => ((Union<T1, T2>) this).As(defaultValue);
	public T2? As(T2? defaultValue) => ((Union<T1, T2>) this).As(defaultValue);
	
	public bool Is<T>(out T v) => ((Union<T1, T2>) this).Is(out v);
	
	public R Match<R>(Func<T1, R> resultIfT1, Func<T2, R> resultIfT2) =>
		((Union<T1, T2>) this).Match(resultIfT1, resultIfT2);
	public R Match<R>(Func<T1, R> resultIfT1, Func<T2, R> resultIfT2, Func<R> resultIfNull) =>
		((Union<T1, T2>) this).Match(resultIfT1, resultIfT2, resultIfNull);
	
	public void Switch(Action<T1> actionIfT1, Action<T2> actionIfT2) =>
		((Union<T1, T2>) this).Switch(actionIfT1, actionIfT2);
	public void USwitch(Action<T1> actionIfT1, Action<T2> actionIfT2, Action actionIfNull) =>
		((Union<T1, T2>) this).Switch(actionIfT1, actionIfT2, actionIfNull);
}

public class union<T1, T2, T3>: Union<T1, T2, T3> {
	object? value;
	
	public object? Object => value;
	
	union() { }
	
	public union(T1 initValue) {
		value = initValue;
	}
	
	public union(T2 initValue) {
		value = initValue;
	}
	
	public union(T3 initValue) {
		value = initValue;
	}
	
	public static implicit operator union<T1, T3, T2> (union<T1, T2, T3> union) {
		union<T1, T3, T2> u = new() {
			value = union.value
		};
		return u;
	}
	
	public static implicit operator union<T2, T1, T3> (union<T1, T2, T3> union) {
		union<T2, T1, T3> u = new() {
			value = union.value
		};
		return u;
	}
	
	public static implicit operator union<T2, T3, T1> (union<T1, T2, T3> union) {
		union<T2, T3, T1> u = new() {
			value = union.value
		};
		return u;
	}
	
	public static implicit operator union<T3, T1, T2> (union<T1, T2, T3> union) {
		union<T3, T1, T2> u = new() {
			value = union.value
		};
		return u;
	}
	
	public static implicit operator union<T3, T2, T1> (union<T1, T2, T3> union) {
		union<T3, T2, T1> u = new() {
			value = union.value
		};
		return u;
	}
	
	public static implicit operator union<T1, T2, T3> (T1 value) => new(value);
	public static implicit operator union<T1, T2, T3> (T2 value) => new(value);
	public static implicit operator union<T1, T2, T3> (T3 value) => new(value);
	
	public static explicit operator T1 (union<T1, T2, T3> value) => (T1) value.value;
	public static explicit operator T2 (union<T1, T2, T3> value) => (T2) value.value;
	public static explicit operator T3 (union<T1, T2, T3> value) => (T3) value.value;
	
	public T? As<T>() => ((Union<T1, T2, T3>) this).As<T>();
	
	public T1? As(T1? defaultValue) => ((Union<T1, T2, T3>) this).As(defaultValue);
	public T2? As(T2? defaultValue) => ((Union<T1, T2, T3>) this).As(defaultValue);
	public T3? As(T3? defaultValue) => ((Union<T1, T2, T3>) this).As(defaultValue);
	
	public bool Is<T>(out T v) => ((Union<T1, T2, T3>) this).Is(out v);
	
	public R Match<R>(Func<T1, R> resultIfT1, Func<T2, R> resultIfT2, Func<T3, R> resultIfT3) =>
		((Union<T1, T2, T3>) this).Match(resultIfT1, resultIfT2, resultIfT3);
	public R Match<R>(Func<T1, R> resultIfT1, Func<T2, R> resultIfT2, Func<T3, R> resultIfT3, Func<R> resultIfNull) =>
		((Union<T1, T2, T3>) this).Match(resultIfT1, resultIfT2, resultIfT3, resultIfNull);
	
	public void Switch(Action<T1> actionIfT1, Action<T2> actionIfT2, Action<T3> actionIfT3) =>
		((Union<T1, T2, T3>) this).Switch(actionIfT1, actionIfT2, actionIfT3);
	public void Switch(Action<T1> actionIfT1, Action<T2> actionIfT2, Action<T3> actionIfT3, Action actionIfNull) =>
		((Union<T1, T2, T3>) this).Switch(actionIfT1, actionIfT2, actionIfT3, actionIfNull);
}