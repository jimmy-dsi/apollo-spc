using System.Linq.Expressions;

namespace Jimbl.DataStructs;

using System.Collections;

public class ListUnion<T1, T2>: IList<union<T1, T2>>, Union<List<T1>, List<T2>> {
	union<List<T1>, List<T2>> list;
	
	public object Object => list.Object!;
	
	public int Count => list.Match (
		L => L.Count,
		L => L.Count
	);
	
	public bool IsReadOnly => false;
	
	ListUnion() { }
	
	public ListUnion(List<T1> list) {
		this.list = list;
	}
	
	public ListUnion(List<T2> list) {
		this.list = list;
	}
	
	public static implicit operator ListUnion<T2, T1> (ListUnion<T1, T2> union) {
		ListUnion<T2, T1> u = new() {
			list = union.list
		};
		return u;
	}
	
	public static implicit operator ListUnion<T1, T2> (union<List<T1>, List<T2>> union) {
		ListUnion<T1, T2> u = new() {
			list = union
		};
		return u;
	}
	
	public static implicit operator ListUnion<T1, T2> (union<List<T2>, List<T1>> union) {
		ListUnion<T1, T2> u = new() {
			list = union
		};
		return u;
	}
	
	public static implicit operator ListUnion<T1, T2> (List<T1> value) => new(value);
	public static implicit operator ListUnion<T1, T2> (List<T2> value) => new(value);
	
	public static explicit operator List<T1> (ListUnion<T1, T2> value) => (List<T1>) value.list;
	public static explicit operator List<T2> (ListUnion<T1, T2> value) => (List<T2>) value.list;
	
	public static implicit operator union<List<T1>, List<T2>> (ListUnion<T1, T2> union) => union.list;
	public static implicit operator union<List<T2>, List<T1>> (ListUnion<T1, T2> union) => union.list;
	
	public union<T1, T2> this[int index] {
		get {
			return list.Match (
				L => (union<T1, T2>) L[index],
				L => (union<T1, T2>) L[index]
			);
		}
		set {
			list.Switch (
				L => L[index] = value.As<T1>()!,
				L => L[index] = value.As<T2>()!
			);
		}
	}
	
	Type Union.GetType() => GetType();
	
	public new Type GetType() {
		return list.Type();
	}
	
	public void Clear() {
		list.Switch (
			L => L.Clear(),
			L => L.Clear()
		);
	}
	
	public void Add(union<T1, T2> item) {
		typeGuard(item.Object);
		
		list.Switch (
			L => L.Add(item.As<T1>()!),
			L => L.Add(item.As<T2>()!)
		);
	}
	
	public void Add(T1 item) => Add((union<T1, T2>) item);
	public void Add(T2 item) => Add((union<T1, T2>) item);
	
	public bool Contains(union<T1, T2> item) {
		typeGuard(item.Object);
		
		return list.Match (
			L => L.Contains(item.As<T1>()!),
			L => L.Contains(item.As<T2>()!)
		);
	}
	
	public bool Contains(T1 item) => Contains((union<T1, T2>) item);
	public bool Contains(T2 item) => Contains((union<T1, T2>) item);
	
	public void CopyTo(union<T1, T2>[] array, int arrayIndex) {
		union<T1[], T2[]> output = new T1[] {};
		List<int> x;
		
		list.Switch (
			L => {
				var result = new T1[Count];
				L.CopyTo(result, arrayIndex);
				output = result;
			},
			L => {
				var result = new T2[Count];
				L.CopyTo(result, arrayIndex);
				output = result;
			}
		);
		
		output.Switch (
			a => {
				foreach (var (i, v) in a.Enum()) {
					if (i >= array.Length) {
						break;
					}
					array[i] = v;
				}
			},
			a => {
				foreach (var (i, v) in a.Enum()) {
					if (i >= array.Length) {
						break;
					}
					array[i] = v;
				}
			}
		);
	}
	
	public void CopyTo<T>(T[] array, int arrayIndex) {
		typeGuard(typeof(T));
		
		list.Switch (
			L => L.CopyTo((T1[]) (object) array, arrayIndex),
			L => L.CopyTo((T2[]) (object) array, arrayIndex)
		);
	}
	
	public bool Remove(union<T1, T2> item) {
		typeGuard(item.Object);

		return list.Match (
			L => L.Remove(item.As<T1>()!),
			L => L.Remove(item.As<T2>()!)
		);
	}
	
	public bool Remove(T1 item) => Remove((union<T1, T2>) item);
	public bool Remove(T2 item) => Remove((union<T1, T2>) item);
	
	public int IndexOf(union<T1, T2> item) {
		typeGuard(item.Object);
		
		return list.Match (
			L => L.IndexOf(item.As<T1>()!),
			L => L.IndexOf(item.As<T2>()!)
		);
	}
	
	public int IndexOf(T1 item) => IndexOf((union<T1, T2>) item);
	public int IndexOf(T2 item) => IndexOf((union<T1, T2>) item);
	
	public void Insert(int index, union<T1, T2> item) {
		typeGuard(item.Object);

		list.Switch (
			L => L.Insert(index, item.As<T1>()!),
			L => L.Insert(index, item.As<T2>()!)
		);
	}
	
	public void Insert(int index, T1 item) => Insert(index, (union<T1, T2>) item);
	public void Insert(int index, T2 item) => Insert(index, (union<T1, T2>) item);
	
	public void RemoveAt(int index) {
		list.Switch (
			L => L.RemoveAt(index),
			L => L.RemoveAt(index)
		);
	}
	
	public IEnumerator<union<T1, T2>> GetEnumerator() {
		foreach (var x in (..Count).Enum()) {
			yield return this[x];
		}
	}
	
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	
	public T? As<T>() => ((Union<List<T1>, List<T2>>) this).As<T>();
	
	public List<T1>? As(List<T1>? defaultValue) => ((Union<List<T1>, List<T2>>) this).As(defaultValue);
	public List<T2>? As(List<T2>? defaultValue) => ((Union<List<T1>, List<T2>>) this).As(defaultValue);
	
	public bool Is<T>(out T v) => ((Union<List<T1>, List<T2>>) this).Is(out v);
	
	public R Match<R>(Func<List<T1>, R> resultIfT1, Func<List<T2>, R> resultIfT2) =>
		((Union<List<T1>, List<T2>>) this).Match(resultIfT1, resultIfT2);
	public R Match<R>(Func<List<T1>, R> resultIfT1, Func<List<T2>, R> resultIfT2, Func<R> resultIfNull) =>
		((Union<List<T1>, List<T2>>) this).Match(resultIfT1, resultIfT2, resultIfNull);
	
	public void Switch(Action<List<T1>> actionIfT1, Action<List<T2>> actionIfT2) =>
		((Union<List<T1>, List<T2>>) this).Switch(actionIfT1, actionIfT2);
	public void Switch(Action<List<T1>> actionIfT1, Action<List<T2>> actionIfT2, Action actionIfNull) =>
		((Union<List<T1>, List<T2>>) this).Switch(actionIfT1, actionIfT2, actionIfNull);
	
	void typeGuard(object? item) {
		if (!item.Type().IsAssignableTo(list.Type().GetGenericArguments()[0])) {
			try {
				_ = Convert.ChangeType(item, list.Type().GetGenericArguments()[0]);
			}
			catch (InvalidCastException) {
				throw new ArrayTypeMismatchException();
			}
		}
	}
	
	void typeGuard(Type itemType) {
		if (itemType != list.Type().GetGenericArguments()[0]) {
			throw new ArrayTypeMismatchException();
		}
	}
}

public class ListUnion<T1, T2, T3>: IList<union<T1, T2, T3>>, Union<List<T1>, List<T2>, List<T3>> {
	union<List<T1>, List<T2>, List<T3>> list;
	
	public object Object => list.Object!;
	
	public int Count => list.Match (
		L => L.Count,
		L => L.Count,
		L => L.Count
	);
	
	public bool IsReadOnly => false;
	
	ListUnion() { }
	
	public ListUnion(List<T1> list) {
		this.list = list;
	}
	
	public ListUnion(List<T2> list) {
		this.list = list;
	}
	
	public ListUnion(List<T3> list) {
		this.list = list;
	}
	
	public static implicit operator ListUnion<T1, T3, T2> (ListUnion<T1, T2, T3> union) {
		ListUnion<T1, T3, T2> u = new() {
			list = union.list
		};
		return u;
	}
	
	public static implicit operator ListUnion<T2, T1, T3> (ListUnion<T1, T2, T3> union) {
		ListUnion<T2, T1, T3> u = new() {
			list = union.list
		};
		return u;
	}
	
	public static implicit operator ListUnion<T2, T3, T1> (ListUnion<T1, T2, T3> union) {
		ListUnion<T2, T3, T1> u = new() {
			list = union.list
		};
		return u;
	}
	
	public static implicit operator ListUnion<T3, T1, T2> (ListUnion<T1, T2, T3> union) {
		ListUnion<T3, T1, T2> u = new() {
			list = union.list
		};
		return u;
	}
	
	public static implicit operator ListUnion<T3, T2, T1> (ListUnion<T1, T2, T3> union) {
		ListUnion<T3, T2, T1> u = new() {
			list = union.list
		};
		return u;
	}
	
	public static implicit operator ListUnion<T1, T2, T3> (union<List<T1>, List<T2>, List<T3>> union) {
		ListUnion<T1, T2, T3> u = new() {
			list = union
		};
		return u;
	}
	
	public static implicit operator ListUnion<T1, T2, T3> (union<List<T1>, List<T3>, List<T2>> union) {
		ListUnion<T1, T2, T3> u = new() {
			list = union
		};
		return u;
	}
	
	public static implicit operator ListUnion<T1, T2, T3> (union<List<T2>, List<T1>, List<T3>> union) {
		ListUnion<T1, T2, T3> u = new() {
			list = union
		};
		return u;
	}
	
	public static implicit operator ListUnion<T1, T2, T3> (union<List<T2>, List<T3>, List<T1>> union) {
		ListUnion<T1, T2, T3> u = new() {
			list = union
		};
		return u;
	}
	
	public static implicit operator ListUnion<T1, T2, T3> (union<List<T3>, List<T1>, List<T2>> union) {
		ListUnion<T1, T2, T3> u = new() {
			list = union
		};
		return u;
	}
	
	public static implicit operator ListUnion<T1, T2, T3> (union<List<T3>, List<T2>, List<T1>> union) {
		ListUnion<T1, T2, T3> u = new() {
			list = union
		};
		return u;
	}
	
	public static implicit operator ListUnion<T1, T2, T3> (List<T1> value) => new(value);
	public static implicit operator ListUnion<T1, T2, T3> (List<T2> value) => new(value);
	public static implicit operator ListUnion<T1, T2, T3> (List<T3> value) => new(value);
	
	public static explicit operator List<T1> (ListUnion<T1, T2, T3> value) => (List<T1>) value.list;
	public static explicit operator List<T2> (ListUnion<T1, T2, T3> value) => (List<T2>) value.list;
	public static explicit operator List<T3> (ListUnion<T1, T2, T3> value) => (List<T3>) value.list;
	
	public static implicit operator union<List<T1>, List<T2>, List<T3>> (ListUnion<T1, T2, T3> union) => union.list;
	public static implicit operator union<List<T1>, List<T3>, List<T2>> (ListUnion<T1, T2, T3> union) => union.list;
	public static implicit operator union<List<T2>, List<T1>, List<T3>> (ListUnion<T1, T2, T3> union) => union.list;
	public static implicit operator union<List<T2>, List<T3>, List<T1>> (ListUnion<T1, T2, T3> union) => union.list;
	public static implicit operator union<List<T3>, List<T1>, List<T2>> (ListUnion<T1, T2, T3> union) => union.list;
	public static implicit operator union<List<T3>, List<T2>, List<T1>> (ListUnion<T1, T2, T3> union) => union.list;
	
	public union<T1, T2, T3> this[int index] {
		get {
			return list.Match (
				L => (union<T1, T2, T3>) L[index],
				L => (union<T1, T2, T3>) L[index],
				L => (union<T1, T2, T3>) L[index]
			);
		}
		set {
			list.Switch (
				L => L[index] = value.As<T1>()!,
				L => L[index] = value.As<T2>()!,
				L => L[index] = value.As<T3>()!
			);
		}
	}
	
	public new Type GetType() {
		return list.Type();
	}
	
	public void Clear() {
		list.Switch (
			L => L.Clear(),
			L => L.Clear(),
			L => L.Clear()
		);
	}
	
	public void Add(union<T1, T2, T3> item) {
		typeGuard(item.Object);

		list.Switch (
			L => L.Add(item.As<T1>()!),
			L => L.Add(item.As<T2>()!),
			L => L.Add(item.As<T3>()!)
		);
	}
	
	public void Add(T1 item) => Add((union<T1, T2, T3>) item);
	public void Add(T2 item) => Add((union<T1, T2, T3>) item);
	public void Add(T3 item) => Add((union<T1, T2, T3>) item);
	
	public bool Contains(union<T1, T2, T3> item) {
		typeGuard(item.Object);
		
		return list.Match (
			L => L.Contains(item.As<T1>()!),
			L => L.Contains(item.As<T2>()!),
			L => L.Contains(item.As<T3>()!)
		);
	}
	
	public bool Contains(T1 item) => Contains((union<T1, T2, T3>) item);
	public bool Contains(T2 item) => Contains((union<T1, T2, T3>) item);
	public bool Contains(T3 item) => Contains((union<T1, T2, T3>) item);
	
	public void CopyTo(union<T1, T2, T3>[] array, int arrayIndex) {
		union<T1[], T2[], T3[]> output = new T1[] {};
		
		list.Switch (
			L => {
				var result = new T1[Count];
				L.CopyTo(result, arrayIndex);
				output = result;
			},
			L => {
				var result = new T2[Count];
				L.CopyTo(result, arrayIndex);
				output = result;
			},
			L => {
				var result = new T3[Count];
				L.CopyTo(result, arrayIndex);
				output = result;
			}
		);
		
		output.Switch (
			a => {
				foreach (var (i, v) in a.Enum()) {
					if (i >= array.Length) {
						break;
					}
					array[i] = v;
				}
			},
			a => {
				foreach (var (i, v) in a.Enum()) {
					if (i >= array.Length) {
						break;
					}
					array[i] = v;
				}
			},
			a => {
				foreach (var (i, v) in a.Enum()) {
					if (i >= array.Length) {
						break;
					}
					array[i] = v;
				}
			}
		);
	}
	
	public void CopyTo<T>(T[] array, int arrayIndex) {
		typeGuard(typeof(T));
		
		list.Switch (
			L => L.CopyTo((T1[]) (object) array, arrayIndex),
			L => L.CopyTo((T2[]) (object) array, arrayIndex),
			L => L.CopyTo((T3[]) (object) array, arrayIndex)
		);
	}
	
	public bool Remove(union<T1, T2, T3> item) {
		typeGuard(item.Object);

		return list.Match (
			L => L.Remove(item.As<T1>()!),
			L => L.Remove(item.As<T2>()!),
			L => L.Remove(item.As<T3>()!)
		);
	}
	
	public bool Remove(T1 item) => Remove((union<T1, T2, T3>) item);
	public bool Remove(T2 item) => Remove((union<T1, T2, T3>) item);
	public bool Remove(T3 item) => Remove((union<T1, T2, T3>) item);
	
	public int IndexOf(union<T1, T2, T3> item) {
		typeGuard(item.Object);

		return list.Match (
			L => L.IndexOf(item.As<T1>()!),
			L => L.IndexOf(item.As<T2>()!),
			L => L.IndexOf(item.As<T3>()!)
		);
	}
	
	public int IndexOf(T1 item) => IndexOf((union<T1, T2, T3>) item);
	public int IndexOf(T2 item) => IndexOf((union<T1, T2, T3>) item);
	public int IndexOf(T3 item) => IndexOf((union<T1, T2, T3>) item);
	
	public void Insert(int index, union<T1, T2, T3> item) {
		typeGuard(item.Object);

		list.Switch (
			L => L.Insert(index, item.As<T1>()!),
			L => L.Insert(index, item.As<T2>()!),
			L => L.Insert(index, item.As<T3>()!)
		);
	}
	
	public void Insert(int index, T1 item) => Insert(index, (union<T1, T2, T3>) item);
	public void Insert(int index, T2 item) => Insert(index, (union<T1, T2, T3>) item);
	public void Insert(int index, T3 item) => Insert(index, (union<T1, T2, T3>) item);
	
	public void RemoveAt(int index) {
		list.Switch (
			L => L.RemoveAt(index),
			L => L.RemoveAt(index),
			L => L.RemoveAt(index)
		);
	}
	
	public IEnumerator<union<T1, T2, T3>> GetEnumerator() {
		foreach (var x in (..Count).Enum()) {
			yield return this[x];
		}
	}
	
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	
	public T? As<T>() => ((Union<List<T1>, List<T2>, List<T3>>) this).As<T>();
	
	public List<T1>? As(List<T1>? defaultValue) => ((Union<List<T1>, List<T2>, List<T3>>) this).As(defaultValue);
	public List<T2>? As(List<T2>? defaultValue) => ((Union<List<T1>, List<T2>, List<T3>>) this).As(defaultValue);
	public List<T3>? As(List<T3>? defaultValue) => ((Union<List<T1>, List<T2>, List<T3>>) this).As(defaultValue);
	
	public bool Is<T>(out T v) => ((Union<List<T1>, List<T2>, List<T3>>) this).Is(out v);
	
	public R Match<R>(Func<List<T1>, R> resultIfT1, Func<List<T2>, R> resultIfT2, Func<List<T3>, R> resultIfT3) =>
		((Union<List<T1>, List<T2>, List<T3>>) this).Match(resultIfT1, resultIfT2, resultIfT3);
	public R Match<R>(Func<List<T1>, R> resultIfT1, Func<List<T2>, R> resultIfT2, Func<List<T3>, R> resultIfT3, Func<R> resultIfNull) =>
		((Union<List<T1>, List<T2>, List<T3>>) this).Match(resultIfT1, resultIfT2, resultIfT3, resultIfNull);
	
	public void Switch(Action<List<T1>> actionIfT1, Action<List<T2>> actionIfT2, Action<List<T3>> actionIfT3) =>
		((Union<List<T1>, List<T2>, List<T3>>) this).Switch(actionIfT1, actionIfT2, actionIfT3);
	public void Switch(Action<List<T1>> actionIfT1, Action<List<T2>> actionIfT2, Action<List<T3>> actionIfT3, Action actionIfNull) =>
		((Union<List<T1>, List<T2>, List<T3>>) this).Switch(actionIfT1, actionIfT2, actionIfT3, actionIfNull);
	
	void typeGuard(object? item) {
		if (!item.Type().IsAssignableTo(list.Type().GetGenericArguments()[0])) {
			try {
				_ = Convert.ChangeType(item, list.Type().GetGenericArguments()[0]);
			}
			catch (InvalidCastException) {
				throw new ArrayTypeMismatchException();
			}
		}
	}
	
	void typeGuard(Type itemType) {
		if (itemType != list.Type().GetGenericArguments()[0]) {
			throw new ArrayTypeMismatchException();
		}
	}
}