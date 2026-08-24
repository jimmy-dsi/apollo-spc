namespace Jimbl.JSON5;

using System.Globalization;
using System.Text;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;

delegate bool ParseDelegate<T>(out T result, out int errorIndex);
delegate bool ParseDelegate(out object? result, out int errorIndex);

public class JSON5ParseError: Exception { }

class JSON5Parser {
	string input;
	int    index;
	int    startIndex;
	char   stringChar;

	ParseState state;

	char chr => input[index];
	
	JSON5Parser() { }

	public JSON5Parser(string input) {
		this.input      = input;
		this.index      = 0;
		this.startIndex = 0;

		state = ParseState.Normal;
	}

	enum ParseState {
		Normal, LineComment, Comment, String
	}
	
	public JObject Parse() {
		if (!parseObject(out var result, out var errorIndex)) {
			throw new JSON5ParseError();
		}
		
		return result;
	}
	
	bool parseObject([MaybeNullWhen(false)] out JObject result, out int index) {
		bool wrapComma(out string? result, out int index) {
			var s = expect(JSONToken.Comma, out var res, out index);
			result = res;
			return s;
		}
		
		result = null;
		
		if (!expect(JSONToken.OpenBrace,  out _, out index)) return false;
		maybe(parseObjectInner, out result, out index);
		maybe<string?>(wrapComma, out _, out index);
		if (!expect(JSONToken.CloseBrace, out _, out index)) return false;
		
		result ??= new();
		return true;
	}
	
	bool parseObjectInner([MaybeNullWhen(false)] out JObject result, out int index) {
		result = null;
		if (!parseKeyValue(out var prop, out index)) return false;
		
		result = new();
		
		var (key, value) = prop!.Value;
		result[key] = value;
		
		while (true) {
			if (!maybe<(string, JItem?)?>(parseNextProperty, out var nextProp, out index)) break;
			var (k, v) = nextProp!.Value;
			result[k] = v;
		}
		
		return true;
	}
	
	bool parseNextProperty(out (string Key, JItem? Value)? result, out int index) {
		result = null;
		if (!expect(JSONToken.Comma, out _, out index)) return false;
		if (!parseKeyValue(out result, out index)) return false;
		return true;
	}
	
	bool parseKeyValue(out (string Key, JItem? Value)? result, out int index) {
		result = null;
		
		if (!expectOneOf([JSONToken.String, JSONToken.Ident, JSONToken.Bool, JSONToken.Null], out var propString, out index)) return false;
		if (!expect(JSONToken.Colon, out _, out index)) return false;
		if (!parseItem(out var propValue, out index)) return false;
		
		var propName = extractStringValue(propString);
		result = (Key: propName, Value: propValue);
		
		return true;
	}
	
	bool parseItem(out JItem? result, out int index) {
		bool wrapNumber(out object? result, out int index) { 
			var s = parseNumber(out var res, out index);
			result = res;
			return s;
		}
		
		bool wrapBool(out object? result, out int index) { 
			var s = parseBool(out var res, out index);
			result = res;
			return s;
		}
		
		bool wrapString(out object? result, out int index) { 
			var s = parseString(out var res, out index);
			result = res;
			return s;
		}
		
		bool wrapObject(out object? result, out int index) { 
			var s = parseObject(out var res, out index);
			result = res;
			return s;
		}
		
		bool wrapArray(out object? result, out int index) { 
			var s = parseArray(out var res, out index);
			result = res;
			return s;
		}
		
		result = null;
		
		var s = oneOf([
			wrapNumber, wrapBool, wrapString, parseNull,
			wrapObject, wrapArray
		], out var resobj, out index);
		
		if (s) result = (JItem?) resobj;
		return s;
	}
	
	bool parseArray([MaybeNullWhen(false)] out JArray result, out int index) {
		bool wrapComma(out string? result, out int index) {
			var s = expect(JSONToken.Comma, out var res, out index);
			result = res;
			return s;
		}
		
		result = null;
		
		if (!expect(JSONToken.OpenBracket,  out _, out index)) return false;
		maybe(parseArrayInner, out result, out index);
		maybe<string?>(wrapComma, out _, out index);
		if (!expect(JSONToken.CloseBracket, out _, out index)) return false;
		
		result ??= new();
		return true;
	}
	
	bool parseArrayInner([MaybeNullWhen(false)] out JArray result, out int index) {
		result = null;
		if (!parseItem(out var item, out index)) return false;
		
		result = [item];
		
		while (true) {
			if (!maybe<JItem?>(parseNextItem, out var nextItem, out index)) break;
			result.Add(nextItem);
		}
		
		return true;
	}
	
	bool parseNextItem(out JItem? result, out int index) {
		result = null;
		if (!expect(JSONToken.Comma, out _, out index)) return false;
		if (!parseItem(out result, out index)) return false;
		return true;
	}
	
	bool parseNumber(out JNumber? result, out int index) {
		result = null;
		if (!expect(JSONToken.Number, out var numstr, out index)) return false;
		if (numstr.ToLower().StartsWith("0x")) result = int.Parse(numstr[2 ..], NumberStyles.HexNumber);
		else result = double.Parse(numstr, CultureInfo.InvariantCulture);
		return true;
	}
	
	bool parseBool(out JBool? result, out int index) {
		result = null;
		if (!expect(JSONToken.Bool, out var boolstr, out index)) return false;
		result = bool.Parse(boolstr);
		return true;
	}
	
	bool parseString(out JString? result, out int index) {
		result = null;
		if (!expect(JSONToken.String, out var str, out index)) return false;
		result = extractStringValue(str);
		return true;
	}
	
	bool parseNull(out object? result, out int index) {
		result = null;
		return expect(JSONToken.Null, out _, out index);
	}
	
	static string extractStringValue(string propString) {
		if (propString[0] is '"' or '\'') {
			var           inner = propString[1 .. ^1];
			StringBuilder sb    = new();
			
			InnerStringState state = InnerStringState.Normal;
			string uhex = "";
			
			foreach (var c in inner) {
				switch (state) {
					case InnerStringState.Normal: {
						if (c == '\\') state = InnerStringState.Backslash;
						else sb.Append(c);
						break;
					}
					
					case InnerStringState.Backslash: {
						if (c == 'u') state = InnerStringState.Unicode;
						else {
							state = InnerStringState.Normal;
							sb.Append(c switch {
								'"' or '\'' or '\\' or '/' or '\r' or '\n' => c,
								'b' => '\b', 'f' => '\f', 'n' => '\n', 'r' => '\r', 't' => '\t',
								_ => throw new UnreachableException()
							});
						}
						break;
					}
					
					case InnerStringState.Unicode: {
						uhex += c;
						if (uhex.Length == 4) {
							state = InnerStringState.Normal;
							var u = (char) int.Parse(uhex, System.Globalization.NumberStyles.HexNumber);
							sb.Append(u);
						}
						break;
					}
				}
			}
			
			return sb.ToString();
		}
		else {
			return propString;
		}
	}
	
	enum InnerStringState {
		Normal, Backslash, Unicode
	}
	
	bool expect(JSONToken type, [MaybeNullWhen(false)] out string result, out int index) {
		var (ttype, tvalue) = Next();
		
		if (ttype == type) {
			result = tvalue;
			index  = 0;
			return true;
		}
		else {
			result = null;
			index  = this.index;
			return false;
		}
	}
	
	bool expectOneOf(JSONToken[] types, [MaybeNullWhen(false)] out string result, out int index) {
		var (ttype, tvalue) = Next();
		
		foreach (var type in types) {
			if (ttype == type) {
				result = tvalue;
				index  = 0;
				return true;
			}
		}
		
		result = null;
		index  = this.index;
		return false;
	}
	
	bool maybe<T>(ParseDelegate<T> predicate, [MaybeNullWhen(false)] out T result, out int index) {
		var backup = Copy();
		if (predicate(out result, out index)) return true;
		else {
			result = default;
			CopyFrom(backup);
			return false;
		}
	}
	
	bool oneOf(ParseDelegate[] predicates, out object? result, out int index) {
		var backup = Copy();
		
		index = 0;
		result = null;
		
		foreach (var p in predicates) {
			if (p(out result, out var idx)) return true;
			else {
				if (idx > index) index = idx;
				CopyFrom(backup);
			}
		}
		
		return false;
	}
	
	public JSON5Parser Copy() {
		return new() {
			input      = input,
			index      = index,
			startIndex = startIndex,
			stringChar = stringChar,
			state      = state
		};
	}
	
	public void CopyFrom(JSON5Parser other) {
		input      = other.input;
		index      = other.index;
		startIndex = other.startIndex;
		stringChar = other.stringChar;
		state      = other.state;
	}
	
	public (JSONToken, string) Next() {
		while (true) {
			switch (state) {
				case ParseState.Normal: {
					// Skip whitespace
					while (chr is ' ' or '\t' or '\r' or '\n') step();

					if (str(2) == "//") {
						state = ParseState.LineComment;
						step(2);
					}
					else if (str(2) == "/*") {
						state = ParseState.Comment;
						step(2);
					}
					else if (chr is '{' or '}' or '[' or ']' or ',' or ':') {
						startIndex = index;
						var res = (chr switch {
							'{' => JSONToken.OpenBrace,   '}' => JSONToken.CloseBrace,
							'[' => JSONToken.OpenBracket, ']' => JSONToken.CloseBracket,
							',' => JSONToken.Comma,       ':' => JSONToken.Colon,
							_ => throw new UnreachableException()
						}, input[startIndex .. index]);
						step();
						return res;
					}
					else if (chr is '"' or '\'') {
						startIndex = index;
						state = ParseState.String;
						stringChar = chr;
						step();
					}
					else if (chr is '-' or '+' or '.' or >= '0' and <= '9') {
						startIndex = index;
						if (chr is '-' or '+') step();

						if (chr == '.') return new(JSONToken.Number, parseDecimal(requiresDigits: true));
						else if (chr == '0') {
							step();
							if (chr == '.') return new(JSONToken.Number, parseDecimal());
							else if (chr is 'e' or 'E') return new(JSONToken.Number, parseExponent());
							else if (chr is 'x' or 'X') return new(JSONToken.Number, parseHex());
							else if (isLiteralEnd()) return new(JSONToken.Number, input[startIndex..index]);
							else throw new ArgumentException("JSON parsing error: invalid number literal");
						}
						else if (chr is >= '1' and <= '9') {
							step();
							while (chr is >= '0' and <= '9') step();
							if (chr == '.') return new(JSONToken.Number, parseDecimal());
							else if (chr is 'e' or 'E') return new(JSONToken.Number, parseExponent());
							else if (isLiteralEnd()) return new(JSONToken.Number, input[startIndex .. index]);
							else throw new ArgumentException("JSON parsing error: invalid number literal");
						}
						else if (chr is '_' or >= 'a' and <= 'z' or >= 'A' and <= 'Z') {
							var nextStartIndex = index;
							step();
							while (chr is '_' or >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9') step();

							if (isLiteralEnd()) {
								var v   = input[nextStartIndex .. index];
								var val = input[startIndex .. index];
								if (v == "Infinity") return new(JSONToken.Number, val);
								else if (v == "NaN") return new(JSONToken.Number, val);
								else new ArgumentException("JSON parsing error: invalid number literal");
							}
						}
					}
					else if (chr == '$') {
						startIndex = index;
						step();
						
						if (isLiteralEnd()) return (JSONToken.Ident, input[startIndex .. index]);
						else throw new ArgumentException("JSON parsing error: invalid identifier");
					}
					else if (chr is '_' or >= 'a' and <= 'z' or >= 'A' and <= 'Z') {
						startIndex = index;
						step();
						while (chr is '_' or >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9') step();

						if (isLiteralEnd()) {
							var val = input[startIndex .. index];
							if (val is "true" or "false") return new(JSONToken.Bool,   val);
							else if (val == "null")       return new(JSONToken.Null,   val);
							else if (val == "Infinity")   return new(JSONToken.Number, val);
							else if (val == "NaN")        return new(JSONToken.Number, val);
							else return new(JSONToken.Ident, val);
						}
					}
					else {
						throw new ArgumentException($"JSON parsing error: unknown character: {chr}");
					}

					break;
				}

				case ParseState.LineComment: {
					if (chr == '\n') state = ParseState.Normal;
					step();
					break;
				}

				case ParseState.Comment: {
					if (str(2) == "*/") {
						state = ParseState.Normal;
						step(2);
					}
					else step();
					break;
				}

				case ParseState.String: {
					if      (str(2) == "\\\\")   step(2);
					else if (str(2) == "\\/")    step(2);
					else if (str(2) == "\\b")    step(2);
					else if (str(2) == "\\f")    step(2);
					else if (str(2) == "\\n")    step(2);
					else if (str(2) == "\\r")    step(2);
					else if (str(2) == "\\t")    step(2);
					else if (str(3) == "\\\r\n") step(3);
					else if (str(2) == "\\\n")   step(2);
					else if (str(2) == "\\u") {
						step(2);
						for (var i = 0; i < 4; i++) {
							if (chr is >= '0' and <= '9' or >= 'A' and <= 'F' or >= 'a' and <= 'f') step();
							else throw new ArgumentException("JSON parsing error: invalid unicode control code");
						}
					}
					else if (stringChar == '"'  && str(2) == "\\\"") step(2);
					else if (stringChar == '\'' && str(2) == "\\'")  step(2);
					else if (chr == stringChar) {
						state = ParseState.Normal;
						step();
						return new(JSONToken.String, input[startIndex .. index]);
					}
					else if (chr == '\\' || chr < 32) throw new ArgumentException($"JSON parsing error: invalid control code or character in string: {chr}");
					else step();
					break;
				}

				default: throw new UnreachableException();
			}
		}
	}

	void step(int amount = 1) {
		index += amount;
		if (index > input.Length) throw new ArgumentException("JSON terminates too early");
	}

	string str(int length) {
		return input[index .. Math.Min(input.Length, index + length)];
	}

	string parseDecimal(bool requiresDigits = false) {
		step();

		var hasDigits = false;
		while (chr is >= '0' and <= '9') {
			hasDigits = true;
			step();
		}

		if (requiresDigits && !hasDigits) throw new ArgumentException("JSON parsing error: invalid number literal");
		
		if (chr is 'e' or 'E') return parseExponent(); 
		else if (isLiteralEnd()) return input[startIndex .. index];
		else throw new ArgumentException("JSON parsing error: invalid number literal");
	}

	string parseExponent() {
		step();

		if (chr is '+' or '-') step();

		var hasDigits = false;
		while (chr is >= '0' and <= '9') {
			hasDigits = true;
			step();
		}

		if (!hasDigits) throw new ArgumentException("JSON parsing error: invalid number literal");

		if (isLiteralEnd()) return input[startIndex .. index];
		else throw new ArgumentException("JSON parsing error: invalid number literal");
	}

	string parseHex() {
		step();

		var hasDigits = false;
		while (chr is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F') {
			hasDigits = true;
			step();
		}

		if (!hasDigits) throw new ArgumentException("JSON parsing error: invalid number literal");

		if (isLiteralEnd()) return input[startIndex .. index];
		else throw new ArgumentException("JSON parsing error: invalid number literal");
	}

	bool isLiteralEnd() {
		return str(2) is "//" or "/*" || chr is ' ' or '\t' or '\r' or '\n' or ':' or ',' or ']' or '}';
	}
}

enum JSONToken {
	OpenBrace, CloseBrace, OpenBracket, CloseBracket,
	Comma, Colon,
	Ident, Number, String, Bool, Null
}