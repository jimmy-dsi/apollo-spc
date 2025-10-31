namespace Jimbl;

using System.Collections;

public class view<T>: IEnumerable<T> {
	public IEnumerable<T> Source { get; init; }
		
	public int Start  { get; init; }
	public int Length { get; init; }
	
	public int Count => Length;
	
	public view(IEnumerable<T> source, int start, int length) {
		Source = source;
		Start  = start;
		Length = length;
	}
	
	public T this[int index] {
		get {
			switch (Source) {
				case T[] array: {
					return array[index + Start];
				}
				case IList<T> list: {
					return list[index + Start];
				}
				case string str: {
					return (T) (object) str[index + Start];
				}
				default: {
					var arr = Source.Skip(Start).Take(Length).ToArray();
					return arr[index];
				}
			}
		}
	}
	
	public IEnumerator<T> GetEnumerator() {
		foreach (var x in (..Length).Enum()) {
			yield return this[Start + x];
		}
	}
	
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}