namespace Jimbl;

public class ItemProperty<K, V> {
	public required Func  <K, V> Get { get; init; }
	public required Action<K, V> Set { get; init; }
	
	public V this[K key] {
		get => Get(key);
		set => Set(key, value);
	}
	
	public ItemProperty() { }
	
	public ItemProperty(Func<K, V> get, Action<K, V> set) {
		Get = get;
		Set = set;
	}
}

public class ItemGetter<K, V> {
	public required Func<K, V> Get { get; init; }
	
	public V this[K key] {
		get => Get(key);
	}
	
	public ItemGetter() { }
	
	public ItemGetter(Func<K, V> get) {
		Get = get;
	}
}