namespace Jimbl.DataStructs;

using System.Collections;

public class Range: IEnumerable<int> {
	public int Start { get; init; }
	public int End   { get; init; }
	public int Step  { get; init; }
	
	public Range(int end): this(0, end) { }
	
	public Range(int start, int end, int step = 1) {
		if (step == 0) {
			throw new ArgumentException("Range step cannot be zero");
		}
		else if (step > 0 && end < start) {
			throw new ArgumentException("Invalid range parameters");
		}
		else if (step < 0 && end > start) {
			throw new ArgumentException("Invalid range parameters");
		}
		
		Start = start;
		End   = end;
		Step  = step;
	}
	
	public IEnumerator<int> GetEnumerator() {
		if (Step > 0) {
			for (var i = Start; i < End; i += Step) {
				yield return i;
			}
		}
		else {
			for (var i = Start; i > End; i += Step) {
				yield return i;
			}
		}
	}
	
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}