namespace Jimbl.DataStructs;

using System.Diagnostics;
using System.Collections;

using VarArray = ArrayUnion<Byte, UInt16, UInt32>;
using VarList  =  ListUnion<Byte, UInt16, UInt32>;

public class UString: IEnumerable<UChar> {
	VarArray data;
	
	public int Length { get; init; }
	
	public UString(string s) {
		VarList buffer = new List<Byte>();
		
		foreach (var rune in s.EnumerateRunes()) {
			if (!buffer.Is<List<UInt32>>(out _) && (UInt32) rune.Value >= 65536) {
				buffer = resize<UInt32>(buffer);
			}
			else if (buffer.Is<List<Byte>>(out _) && (UInt32) rune.Value >= 256) {
				buffer = resize<UInt16>(buffer);
			}
			
			append(buffer, rune.Value);
		}
		
		data = buffer.Match<VarArray>(
			b => b.ToArray(),
			b => b.ToArray(),
			b => b.ToArray()
		);
		
		Length = data.Length;
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
	
	static VarList resize<T>(VarList buffer) {
		VarList newBuffer;
		
		if (typeof(T) == typeof(UInt16)) {
			List<UInt16> buffer2 = new();
			
			foreach (var b in buffer) {
				buffer2.Add(b.As<Byte>());
			}
			
			newBuffer = buffer2;
		}
		else if (typeof(T) == typeof(UInt32)) {
			List<UInt32> buffer2 = new();
			
			foreach (var b in buffer) {
				buffer2.Add(b.Match(x => x, x => x, x => x));
			}
			
			newBuffer = buffer2;
		}
		else {
			throw new UnreachableException();
		}
		
		return newBuffer;
	}
	
	static void append(VarList buffer, UChar value) {
		buffer.Add(value);
	}
}