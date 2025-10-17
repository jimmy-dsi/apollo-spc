namespace Jimbl;

public class union<T1, T2> {
	object? value;
	
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
	
	public T1? As(T1? defaultValue) {
		return value is T1 t ? t : defaultValue;
	}
	
	public T2? As(T2? defaultValue) {
		return value is T2 t ? t : defaultValue;
	}
	
	public R Match<R>(Func<T1, R> resultIfT1, Func<T2, R> resultIfT2) {
		return value switch {
			T1 v1 => resultIfT1(v1),
			T2 v2 => resultIfT2(v2),
			_     => throw new NullReferenceException()
		};
	}
	
	public R Match<R>(Func<T1, R> resultIfT1, Func<T2, R> resultIfT2, Func<R> resultIfNull) {
		return value switch {
			T1 v1 => resultIfT1(v1),
			T2 v2 => resultIfT2(v2),
			_     => resultIfNull(),
		};
	}
	
	public void Switch(Action<T1> actionIfT1, Action<T2> actionIfT2) {
		switch (value) {
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
		switch (value) {
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

public class union<T1, T2, T3> {
	object? value;
	
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
	
	public T1? As(T1? defaultValue) {
		return value is T1 t ? t : defaultValue;
	}
	
	public T2? As(T2? defaultValue) {
		return value is T2 t ? t : defaultValue;
	}
	
	public T3? As(T3? defaultValue) {
		return value is T3 t ? t : defaultValue;
	}
	
	public R Match<R>(Func<T1, R> resultIfT1, Func<T2, R> resultIfT2, Func<T3, R> resultIfT3) {
		return value switch {
			T1 v1 => resultIfT1(v1),
			T2 v2 => resultIfT2(v2),
			T3 v3 => resultIfT3(v3),
			_     => throw new NullReferenceException()
		};
	}
	
	public R Match<R>(Func<T1, R> resultIfT1, Func<T2, R> resultIfT2, Func<T3, R> resultIfT3, Func<R> resultIfNull) {
		return value switch {
			T1 v1 => resultIfT1(v1),
			T2 v2 => resultIfT2(v2),
			T3 v3 => resultIfT3(v3),
			_     => resultIfNull(),
		};
	}
	
	public void Switch(Action<T1> actionIfT1, Action<T2> actionIfT2, Action<T3> actionIfT3) {
		switch (value) {
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
		switch (value) {
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