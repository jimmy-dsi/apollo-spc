namespace Jimbl.JSON5;

using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

public class JObject: JItem, IDictionary<string, JItem?>, IDictionary {
	Dictionary<string, JItem?> dict;

	public int Count => dict.Count;
	public bool IsReadOnly => false;

	public JObject() {
		dict = [];
	}

	internal JObject(Dictionary<string, JItem?> dict) {
		this.dict = dict;
	}
	
	public override string Serialize(int level = 0) {
		if (Count == 0) return "{}";
		else {
			StringBuilder sb = new();
			sb.Append("{\n");
			foreach (var (propName, item) in dict) {
				sb.Append(new string('\t', level + 1));
				if (propName == "$"
				    || propName.Length > 0
				    && propName[0] is not >= '0' or not <= '9'
				    && propName.FirstOrDefault(c => !(c is '_' or >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9')) == 0)
				{
					sb.Append(propName);
				}
				else sb.Append(JString.Escape(propName));
				
				sb.Append(": ");
				
				sb.Append(item is null ? "null" : item.Serialize(level + 1));
				sb.Append(",\n");
			}
			sb.Append(new string('\t', level));
			sb.Append('}');
			return sb.ToString();
		}
	}
	
	public static implicit operator Dictionary<string, JItem?> (JObject obj) => obj.ToDictionary();
	public static implicit operator JObject (Dictionary<string, JItem?> obj) => new(obj);

	public ICollection<string> Keys   => dict.Keys;
	public ICollection<JItem?> Values => dict.Values;

	public bool IsFixedSize => false;

	ICollection IDictionary.Keys   => (ICollection) Keys;
	ICollection IDictionary.Values => (ICollection) Values;

	public bool   IsSynchronized => (dict as ICollection).IsSynchronized;
	public object SyncRoot       => (dict as ICollection).SyncRoot;

	public new JItem? this[string propName] {
		get => dict[propName];
		set => dict[propName] = value;
	}

	public object? this[object key] {
		get => dict[(string) key];
		set => dict[(string) key] = (JItem?) value;
	}

	public void Add(string key, JItem? value)          => dict.Add(key,      value);
	public void Add(KeyValuePair<string, JItem?> item) => dict.Add(item.Key, item.Value);
	
	public void Clear() => dict.Clear();
	
	public bool Remove(string key)                        => dict.Remove(key);
	public bool Remove(KeyValuePair<string, JItem?> item) => dict.Remove(item.Key);

	public void Add(object key, object? value) => dict.Add((string) key, (JItem?) value);
	public void Remove(object key)             => dict.Remove((string) key);

	public bool Contains(KeyValuePair<string, JItem?> item) {
		if (TryGetValue(item.Key, out var value)) return value == item.Value;
		else return false;
	}

	public bool ContainsKey(string key) => dict.ContainsKey(key);
	public bool    Contains(object key) => ContainsKey((string) key);

	public void CopyTo(KeyValuePair<string, JItem?>[] array, int arrayIndex) {
		var arr = Keys.Select(k => new KeyValuePair<string, JItem?>(k, dict[k])).Skip(arrayIndex).ToArray();
		for (var i = 0; i < Math.Min(arr.Length, array.Length); i++) {
			array[i] = arr[i];
		}
	}

	public void CopyTo(Array array, int index) {
		var arr = Keys.Select(k => new KeyValuePair<string, JItem?>(k, dict[k])).Skip(index).ToArray();
		for (var i = 0; i < Math.Min(arr.Length, array.Length); i++) {
			((KeyValuePair<string, JItem?>[]) array)[i] = arr[i];
		}
	}

	public IEnumerator<KeyValuePair<string, JItem?>> GetEnumerator() {
		foreach (var key in Keys) yield return new(key, dict[key]);
	}

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	IDictionaryEnumerator IDictionary.GetEnumerator() => throw new InvalidOperationException();

	public bool TryGetValue(string key, [MaybeNullWhen(false)] out JItem value) {
		value = null;
		return dict.TryGetValue(key, out value);
	}
}