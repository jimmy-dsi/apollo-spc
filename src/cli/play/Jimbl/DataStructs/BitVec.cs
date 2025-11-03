namespace Jimbl.DataStructs;

using SRange = System.Range;

public class BitVec {
	byte[] data;
	
	public int Length => data.Length * 8;
	
	public BitVec(int size) {
		data = new byte[(int) Math.Ceiling((double) size / 8)];
	}
	
	public bool this[int index] {
		get {
			var baseIndex = index * 8;
			return (data[baseIndex] >> index % 8 & 1) == 1;
		}
		set {
			var baseIndex = index * 8;
			var bitVal = value ? (byte) 1 : (byte) 0;
			
			if (value) {
				data[baseIndex] |= (byte) (bitVal << (index % 8));
			}
			else {
				data[baseIndex] &= (byte) ~(bitVal << (index % 8));
			}
		}
	}
	
	//public uint this[SRange index] {
	//	get {
	//		var start = index.Start.Normalize(Length);
	//		var end   = index.End  .Normalize(Length);
	//	}
	//	set {
	//		var start = index.Start.Normalize(Length);
	//		var end   = index.End  .Normalize(Length);
	//		
	//		foreach (var x in (start..end).Enum()) {
	//			this[x] = value >> x;
	//		}
	//	}
	//}
}