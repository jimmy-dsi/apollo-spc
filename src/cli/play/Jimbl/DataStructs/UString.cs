namespace Jimbl.DataStructs;

using System.Collections;

public class UString: IEnumerable<UChar> {
	union<byte[], ushort[], uint[]> data;
	
	public int Length { get; init; }
	
	public UString(string s) {
		
	}
	
	public UChar this[int index] => data.Match(d => d[index],
	                                           d => d[index],
	                                           d => d[index]);
	
	public IEnumerator<UChar> GetEnumerator() {
		foreach (var x in (..Length).Enum()) {
			yield return this[x];
		}
	}
	
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}