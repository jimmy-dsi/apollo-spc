namespace Jimbl.DataStructs;

using System.Collections;

public class ArrayUnion<T1, T2>: 
	ICloneable,
	ICollection,
	IStructuralComparable,
	IStructuralEquatable,
	IEnumerable<union<T1, T2>>, Union<T1[], T2[]>
{
	union<T1[], T2[]> array;
	
	public object Object => array.Object!;
	
	public int Count  => Length;
	public int Length => array.Match (
		L => L.Length,
		L => L.Length
	);
	
	public bool   IsReadOnly     => false;
	public bool   IsFixedSize    => true;
	public bool   IsSynchronized => false;
	public object SyncRoot       => ((Array) array.Object!).SyncRoot;

	ArrayUnion() { }
	
	public ArrayUnion(T1[] array) {
		this.array = array;
	}
	
	public ArrayUnion(T2[] array) {
		this.array = array;
	}
	
	public static implicit operator ArrayUnion<T2, T1> (ArrayUnion<T1, T2> union) {
		ArrayUnion<T2, T1> u = new() {
			array = union.array
		};
		return u;
	}
	
	public static implicit operator ArrayUnion<T1, T2> (union<T1[], T2[]> union) {
		ArrayUnion<T1, T2> u = new() {
			array = union
		};
		return u;
	}
	
	public static implicit operator ArrayUnion<T1, T2> (union<T2[], T1[]> union) {
		ArrayUnion<T1, T2> u = new() {
			array = union
		};
		return u;
	}
	
	public static implicit operator ArrayUnion<T1, T2> (T1[] value) => new(value);
	public static implicit operator ArrayUnion<T1, T2> (T2[] value) => new(value);
	
	public static explicit operator T1[] (ArrayUnion<T1, T2> value) => (T1[]) value.array;
	public static explicit operator T2[] (ArrayUnion<T1, T2> value) => (T2[]) value.array;
	
	public static implicit operator union<T1[], T2[]> (ArrayUnion<T1, T2> union) => union.array;
	public static implicit operator union<T2[], T1[]> (ArrayUnion<T1, T2> union) => union.array;
	
	public union<T1, T2> this[int index] {
		get {
			return array.Match (
				L => (union<T1, T2>) L[index],
				L => (union<T1, T2>) L[index]
			);
		}
		set {
			array.Switch (
				L => L[index] = value.As<T1>()!,
				L => L[index] = value.As<T2>()!
			);
		}
	}
	
	Type Union.GetType() => GetType();
	
	public new Type GetType() {
		return array.Type();
	}
	
	public object Clone() {
		return array.Match (
			a => a.Clone(),
			a => a.Clone()
		);
	}
	
	public void CopyTo(Array array, int arrayIndex) {
		union<T1[], T2[]> output = new T1[] {};
		
		this.array.Switch (
			a => {
				var result = new T1[Count];
				a.CopyTo(result, arrayIndex);
				output = result;
			},
			a => {
				var result = new T2[Count];
				a.CopyTo(result, arrayIndex);
				output = result;
			}
		);
		
		output.Switch (
			a => {
				foreach (var (i, v) in a.Enum()) {
					if (i >= array.Length) {
						break;
					}
					((T1[]) array)[i] = v;
				}
			},
			a => {
				foreach (var (i, v) in a.Enum()) {
					if (i >= array.Length) {
						break;
					}
					((T2[]) array)[i] = v;
				}
			}
		);
	}
	
	public void CopyTo<T>(T[] array, int arrayIndex) {
		typeGuard(typeof(T));
		
		this.array.Switch (
			a => a.CopyTo((T1[]) (object) array, arrayIndex),
			a => a.CopyTo((T2[]) (object) array, arrayIndex)
		);
	}
	
	int IStructuralComparable.CompareTo(object? obj, IComparer comparer) {
		return array.Match (
			a => ((IStructuralComparable) a).CompareTo(obj, comparer),
			a => ((IStructuralComparable) a).CompareTo(obj, comparer)
		);
	}
	
	bool IStructuralEquatable.Equals(object? other, IEqualityComparer comparer) {
		return array.Match (
			a => ((IStructuralEquatable) a).Equals(other, comparer),
			a => ((IStructuralEquatable) a).Equals(other, comparer)
		);
	}
	
	int IStructuralEquatable.GetHashCode(IEqualityComparer comparer) {
		return array.Match (
			a => ((IStructuralEquatable) a).GetHashCode(comparer),
			a => ((IStructuralEquatable) a).GetHashCode(comparer)
		);
	}
	
	public IEnumerator<union<T1, T2>> GetEnumerator() {
		foreach (var x in (..Length).Enum()) {
			yield return this[x];
		}
	}
	
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	
	public T? As<T>() => ((Union<T1[], T2[]>) this).As<T>();
	
	public T1[]? As(T1[]? defaultValue) => ((Union<T1[], T2[]>) this).As(defaultValue);
	public T2[]? As(T2[]? defaultValue) => ((Union<T1[], T2[]>) this).As(defaultValue);
	
	public bool Is<T>(out T v) => ((Union<T1[], T2[]>) this).Is(out v);
	
	public R Match<R>(Func<T1[], R> resultIfT1, Func<T2[], R> resultIfT2) =>
		((Union<T1[], T2[]>) this).Match(resultIfT1, resultIfT2);
	public R Match<R>(Func<T1[], R> resultIfT1, Func<T2[], R> resultIfT2, Func<R> resultIfNull) =>
		((Union<T1[], T2[]>) this).Match(resultIfT1, resultIfT2, resultIfNull);
	
	public void Switch(Action<T1[]> actionIfT1, Action<T2[]> actionIfT2) =>
		((Union<T1[], T2[]>) this).Switch(actionIfT1, actionIfT2);
	public void Switch(Action<T1[]> actionIfT1, Action<T2[]> actionIfT2, Action actionIfNull) =>
		((Union<T1[], T2[]>) this).Switch(actionIfT1, actionIfT2, actionIfNull);
	
	void typeGuard(object? item) {
		if (!item.Type().IsAssignableTo(array.Type().GetElementType())) {
			try {
				_ = Convert.ChangeType(item, array.Type().GetElementType()!);
			}
			catch (InvalidCastException) {
				throw new ArrayTypeMismatchException();
			}
		}
	}
	
	void typeGuard(Type itemType) {
		if (itemType != array.Type().GetElementType()) {
			throw new ArrayTypeMismatchException();
		}
	}
}

public class ArrayUnion<T1, T2, T3>: 
	ICloneable,
	ICollection,
	IStructuralComparable,
	IStructuralEquatable,
	IEnumerable<union<T1, T2, T3>>, Union<T1[], T2[], T3[]>
{
	union<T1[], T2[], T3[]> array;
	
	public object Object => array.Object!;
	
	public int Count  => Length;
	public int Length => array.Match (
		L => L.Length,
		L => L.Length,
		L => L.Length
	);
	
	public bool   IsReadOnly     => false;
	public bool   IsFixedSize    => true;
	public bool   IsSynchronized => false;
	public object SyncRoot       => ((Array) array.Object!).SyncRoot;

	ArrayUnion() { }
	
	public ArrayUnion(T1[] array) {
		this.array = array;
	}
	
	public ArrayUnion(T2[] array) {
		this.array = array;
	}
	
	public ArrayUnion(T3[] array) {
		this.array = array;
	}
	
	public static implicit operator ArrayUnion<T1, T3, T2> (ArrayUnion<T1, T2, T3> union) {
		ArrayUnion<T1, T3, T2> u = new() {
			array = union.array
		};
		return u;
	}
	
	public static implicit operator ArrayUnion<T2, T1, T3> (ArrayUnion<T1, T2, T3> union) {
		ArrayUnion<T2, T1, T3> u = new() {
			array = union.array
		};
		return u;
	}
	
	public static implicit operator ArrayUnion<T2, T3, T1> (ArrayUnion<T1, T2, T3> union) {
		ArrayUnion<T2, T3, T1> u = new() {
			array = union.array
		};
		return u;
	}
	
	public static implicit operator ArrayUnion<T3, T1, T2> (ArrayUnion<T1, T2, T3> union) {
		ArrayUnion<T3, T1, T2> u = new() {
			array = union.array
		};
		return u;
	}
	
	public static implicit operator ArrayUnion<T3, T2, T1> (ArrayUnion<T1, T2, T3> union) {
		ArrayUnion<T3, T2, T1> u = new() {
			array = union.array
		};
		return u;
	}
	
	public static implicit operator ArrayUnion<T1, T2, T3> (union<T1[], T2[], T3[]> union) {
		ArrayUnion<T1, T2, T3> u = new() {
			array = union
		};
		return u;
	}
	
	public static implicit operator ArrayUnion<T1, T2, T3> (union<T1[], T3[], T2[]> union) {
		ArrayUnion<T1, T2, T3> u = new() {
			array = union
		};
		return u;
	}
	
	public static implicit operator ArrayUnion<T1, T2, T3> (union<T2[], T1[], T3[]> union) {
		ArrayUnion<T1, T2, T3> u = new() {
			array = union
		};
		return u;
	}
	
	public static implicit operator ArrayUnion<T1, T2, T3> (union<T2[], T3[], T1[]> union) {
		ArrayUnion<T1, T2, T3> u = new() {
			array = union
		};
		return u;
	}
	
	public static implicit operator ArrayUnion<T1, T2, T3> (union<T3[], T1[], T2[]> union) {
		ArrayUnion<T1, T2, T3> u = new() {
			array = union
		};
		return u;
	}
	
	public static implicit operator ArrayUnion<T1, T2, T3> (union<T3[], T2[], T1[]> union) {
		ArrayUnion<T1, T2, T3> u = new() {
			array = union
		};
		return u;
	}
	
	public static implicit operator ArrayUnion<T1, T2, T3> (T1[] value) => new(value);
	public static implicit operator ArrayUnion<T1, T2, T3> (T2[] value) => new(value);
	public static implicit operator ArrayUnion<T1, T2, T3> (T3[] value) => new(value);
	
	public static explicit operator T1[] (ArrayUnion<T1, T2, T3> value) => (T1[]) value.array;
	public static explicit operator T2[] (ArrayUnion<T1, T2, T3> value) => (T2[]) value.array;
	public static explicit operator T3[] (ArrayUnion<T1, T2, T3> value) => (T3[]) value.array;
	
	public static implicit operator union<T1[], T2[], T3[]> (ArrayUnion<T1, T2, T3> union) => union.array;
	public static implicit operator union<T1[], T3[], T2[]> (ArrayUnion<T1, T2, T3> union) => union.array;
	public static implicit operator union<T2[], T1[], T3[]> (ArrayUnion<T1, T2, T3> union) => union.array;
	public static implicit operator union<T2[], T3[], T1[]> (ArrayUnion<T1, T2, T3> union) => union.array;
	public static implicit operator union<T3[], T1[], T2[]> (ArrayUnion<T1, T2, T3> union) => union.array;
	public static implicit operator union<T3[], T2[], T1[]> (ArrayUnion<T1, T2, T3> union) => union.array;
	
	public union<T1, T2, T3> this[int index] {
		get {
			return array.Match (
				L => (union<T1, T2, T3>) L[index],
				L => (union<T1, T2, T3>) L[index],
				L => (union<T1, T2, T3>) L[index]
			);
		}
		set {
			array.Switch (
				L => L[index] = value.As<T1>()!,
				L => L[index] = value.As<T2>()!,
				L => L[index] = value.As<T3>()!
			);
		}
	}
	
	Type Union.GetType() => GetType();
	
	public new Type GetType() {
		return array.Type();
	}
	
	public object Clone() {
		return array.Match (
			a => a.Clone(),
			a => a.Clone(),
			a => a.Clone()
		);
	}
	
	public void CopyTo(Array array, int arrayIndex) {
		union<T1[], T2[], T3[]> output = new T1[] {};
		
		this.array.Switch (
			a => {
				var result = new T1[Count];
				a.CopyTo(result, arrayIndex);
				output = result;
			},
			a => {
				var result = new T2[Count];
				a.CopyTo(result, arrayIndex);
				output = result;
			},
			a => {
				var result = new T3[Count];
				a.CopyTo(result, arrayIndex);
				output = result;
			}
		);
		
		output.Switch (
			a => {
				foreach (var (i, v) in a.Enum()) {
					if (i >= array.Length) {
						break;
					}
					((T1[]) array)[i] = v;
				}
			},
			a => {
				foreach (var (i, v) in a.Enum()) {
					if (i >= array.Length) {
						break;
					}
					((T2[]) array)[i] = v;
				}
			},
			a => {
				foreach (var (i, v) in a.Enum()) {
					if (i >= array.Length) {
						break;
					}
					((T3[]) array)[i] = v;
				}
			}
		);
	}
	
	public void CopyTo<T>(T[] array, int arrayIndex) {
		typeGuard(typeof(T));
		
		this.array.Switch (
			a => a.CopyTo((T1[]) (object) array, arrayIndex),
			a => a.CopyTo((T2[]) (object) array, arrayIndex),
			a => a.CopyTo((T3[]) (object) array, arrayIndex)
		);
	}
	
	int IStructuralComparable.CompareTo(object? obj, IComparer comparer) {
		return array.Match (
			a => ((IStructuralComparable) a).CompareTo(obj, comparer),
			a => ((IStructuralComparable) a).CompareTo(obj, comparer),
			a => ((IStructuralComparable) a).CompareTo(obj, comparer)
		);
	}
	
	bool IStructuralEquatable.Equals(object? other, IEqualityComparer comparer) {
		return array.Match (
			a => ((IStructuralEquatable) a).Equals(other, comparer),
			a => ((IStructuralEquatable) a).Equals(other, comparer),
			a => ((IStructuralEquatable) a).Equals(other, comparer)
		);
	}
	
	int IStructuralEquatable.GetHashCode(IEqualityComparer comparer) {
		return array.Match (
			a => ((IStructuralEquatable) a).GetHashCode(comparer),
			a => ((IStructuralEquatable) a).GetHashCode(comparer),
			a => ((IStructuralEquatable) a).GetHashCode(comparer)
		);
	}
	
	public IEnumerator<union<T1, T2, T3>> GetEnumerator() {
		foreach (var x in (..Length).Enum()) {
			yield return this[x];
		}
	}
	
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	
	public T? As<T>() => ((Union<T1[], T2[], T3[]>) this).As<T>();
	
	public T1[]? As(T1[]? defaultValue) => ((Union<T1[], T2[], T3[]>) this).As(defaultValue);
	public T2[]? As(T2[]? defaultValue) => ((Union<T1[], T2[], T3[]>) this).As(defaultValue);
	public T3[]? As(T3[]? defaultValue) => ((Union<T1[], T2[], T3[]>) this).As(defaultValue);
	
	public bool Is<T>(out T v) => ((Union<T1[], T2[], T3[]>) this).Is(out v);
	
	public R Match<R>(Func<T1[], R> resultIfT1, Func<T2[], R> resultIfT2, Func<T3[], R> resultIfT3) =>
		((Union<T1[], T2[], T3[]>) this).Match(resultIfT1, resultIfT2, resultIfT3);
	public R Match<R>(Func<T1[], R> resultIfT1, Func<T2[], R> resultIfT2, Func<T3[], R> resultIfT3, Func<R> resultIfNull) =>
		((Union<T1[], T2[], T3[]>) this).Match(resultIfT1, resultIfT2, resultIfT3, resultIfNull);
	
	public void Switch(Action<T1[]> actionIfT1, Action<T2[]> actionIfT2, Action<T3[]> actionIfT3) =>
		((Union<T1[], T2[], T3[]>) this).Switch(actionIfT1, actionIfT2, actionIfT3);
	public void Switch(Action<T1[]> actionIfT1, Action<T2[]> actionIfT2, Action<T3[]> actionIfT3, Action actionIfNull) =>
		((Union<T1[], T2[], T3[]>) this).Switch(actionIfT1, actionIfT2, actionIfT3, actionIfNull);
	
	void typeGuard(object? item) {
		if (!item.Type().IsAssignableTo(array.Type().GetElementType())) {
			try {
				_ = Convert.ChangeType(item, array.Type().GetElementType()!);
			}
			catch (InvalidCastException) {
				throw new ArrayTypeMismatchException();
			}
		}
	}
	
	void typeGuard(Type itemType) {
		if (itemType != array.Type().GetElementType()) {
			throw new ArrayTypeMismatchException();
		}
	}
}