namespace Jimbl.DataStructs;

using System.Collections;
using System.Numerics;

using SRange = System.Range;

public class BitVec: IEnumerable<bool> {
	byte[] data;
	int position = 0;
	
	public int Length => data.Length * 8;
	
	public BitVec(int size) {
		data = new byte[(int) Math.Ceiling((double) size / 8)];
		foreach (var i in (0..data.Length).Iter()) {
			data[i] = 0;
		}
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
	
	public BigInteger this[SRange index] {
		get {
			var start = index.Start.Normalize(Length);
			var end   = index.End  .Normalize(Length);
			
			BigInteger value = 0;
			
			foreach (var (i, x) in (start..end).Iter().Enum()) {
				var bitVal = (BigInteger) (this[x] ? 1 : 0);
				value |= bitVal << i;
			}
			
			return value;
		}
		set {
			var start = index.Start.Normalize(Length);
			var end   = index.End  .Normalize(Length);
			
			foreach (var (i, x) in (start..end).Iter().Enum()) {
				this[x] = (value >> i & 1) != 0;
			}
		}
	}
	
	public void ResetPosition() {
		position = 0;
	}
	
	public void Seek(int position) {
		this.position = position;
	}
	
	public BigInteger ReadNBits(int count) {
		var res = this[position .. (position + count)];
		position += count;
		return res;
	}
	
	public   byte ReadByte()   =>   (byte) ReadNBits(8);
	public  sbyte ReadSByte()  =>  (sbyte) ReadNBits(8);
	public UInt16 ReadUInt16() => (UInt16) ReadNBits(16);
	public  Int16 ReadInt16()  =>  (Int16) ReadNBits(16);
	public UInt32 ReadUInt32() => (UInt32) ReadNBits(32);
	public  Int32 ReadInt32()  =>  (Int32) ReadNBits(32);
	public UInt64 ReadUInt64() => (UInt64) ReadNBits(64);
	public  Int64 ReadInt64()  =>  (Int64) ReadNBits(64);

	public IEnumerator<bool> GetEnumerator() {
		foreach (var i in (0..Length).Iter()) {
			yield return this[i];
		}
	}
	
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}