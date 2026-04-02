namespace Jimbl.DataStructs;

public class Cache<K, V> where K: IEquatable<K> {
	int sizeLimit;
	
	LinkedList<K> usageOrder = new();
	Dictionary<K, (V, LinkedListNode<K>)> cache = new();
	
	LinkedListNode<K>? nextNode = null;
	
	public int Count => cache.Count;
	
	public Cache(int sizeLimit) {
		if (sizeLimit <= 0) {
			throw new ArgumentException("sizeLimit must be greater than 0");
		}
		
		this.sizeLimit = sizeLimit;
		
		// Pre-fill usage order list - Functions as pool of node objects so we don't have to do GC allocations
		for (var i = 0; i < sizeLimit; i++) {
			usageOrder.AddLast(default(K));
		}
		
		nextNode = usageOrder.First;
	}
	
	public V this[K key] {
		get {
			if (TryGetValue(key, out V? value)) {
				return value!;
			}
			else {
				throw new KeyNotFoundException();
			}
		}
		set {
			TryAdd(key, value);
		}
	}
	
	public bool TryGetValue(K key, out V? value) {
		if (cache.TryGetValue(key, out var v)) {
			value = v.Item1;
			setAsMostRecent(key);
			return true;
		}
		else {
			value = default;
			return false;
		}
	}
	
	public bool TryAdd(K key, V value) {
		var added = !ContainsKey(key);
		
		if (added) {
			if (Count == sizeLimit) {
				// Remove LRU
				var target = usageOrder.First!;
				usageOrder.Remove(target);
				cache.Remove(target.Value);
				// Set previous LRU as MRU and add it
				target.Value = key;
				usageOrder.AddLast(target);
				// Add to dictionary
				cache.Add(key, (value, target));
			}
			else {
				nextNode!.Value = key;
				cache.Add(key, (value, nextNode));
				nextNode = nextNode.Next;
			}
		}
		
		return added;
	}
	
	public bool ContainsKey(K key) {
		return cache.ContainsKey(key);
	}
	
	void setAsMostRecent(K key) {
		var keyNode = cache[key].Item2;
		usageOrder.Remove(keyNode);
		
		if (nextNode is null) {
			usageOrder.AddLast(keyNode);
		}
		else {
			usageOrder.AddBefore(nextNode, keyNode);
		}
	}
}