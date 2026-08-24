namespace Jimbl.JSON5;

using System.Text;
using System.Collections;

public class JArray: JItem, IList<JItem?>, IList {
	List<JItem?> list;

	public int  Count      => list.Count;
	public int  Length     => list.Count;
	public bool IsReadOnly => false;

	public JArray() {
		list = [];
	}

	internal JArray(List<JItem?> list) {
		this.list = list;
	}

	internal JArray(JItem?[] list) {
		this.list = list.ToList();
	}
	
	public override string Serialize(int level = 0) {
		if (Length == 0) return "[]";
		else {
			StringBuilder sb = new();
			sb.Append("[\n");
			foreach (var item in list) {
				sb.Append(new string('\t', level + 1));
				sb.Append(item is null ? "null" : item.Serialize(level + 1));
				sb.Append(",\n");
			}
			sb.Append(new string('\t', level));
			sb.Append(']');
			return sb.ToString();
		}
	}
	
	public static implicit operator List<JItem?> (JArray array) => array.ToList();
	public static implicit operator JItem?[]     (JArray array) => array.ToArray();
	
	public static implicit operator JArray (List<JItem?>  list) => new(list);
	public static implicit operator JArray (JItem?[]     array) => new(array);

	public bool   IsSynchronized => ((IList) list).IsSynchronized;
	public bool   IsFixedSize    => false;
	public object SyncRoot       => ((IList) list).SyncRoot;

	public JItem? this[int index] {
		get => list[index];
		set => list[index] = value;
	}

	public JItem? this[Index index] {
		get => list[index];
		set => list[index] = value;
	}

	public JArray this[Range range] {
		get => list[range];
	}

	object? IList.this[int index] {
		get => list[index];
		set => list[index] = (JItem?) value;
	}

	public void Add(JItem? item)               => list.Add(item);
	public void Clear()                        => list.Clear();
	public void Insert(int index, JItem? item) => list.Insert(index, item);
	public bool Remove(JItem? item)            => list.Remove(item);
	public void RemoveAt(int index)            => list.RemoveAt(index);
	
	public int  Add(object? obj)               => ((IList) list).Add(obj);
	public void Insert(int index, object? obj) => ((IList) list).Insert(index, obj);
	public void Remove(object? obj)            => ((IList) list).Remove(obj);
	
	public bool Contains(JItem? item) => list.Contains(item);
	public int  IndexOf(JItem? item)  => list.IndexOf(item);
	
	public bool Contains(object? obj) => ((IList) list).Contains(obj);
	public int  IndexOf(object? obj)  => ((IList) list).IndexOf(obj);
	
	public void CopyTo(JItem?[] array, int arrayIndex) => list.CopyTo(array, arrayIndex);
	public void CopyTo(Array    array, int arrayIndex) => list.CopyTo((JItem?[]) array, arrayIndex);
	
	public IEnumerator<JItem?> GetEnumerator() => list.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();
}