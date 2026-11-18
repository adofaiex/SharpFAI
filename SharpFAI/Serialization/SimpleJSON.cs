using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;

namespace SharpFAI.Serialization
{
    public static class SimpleJSON
	{
		private sealed class Parser : IDisposable
		{
			private enum TOKEN
			{
				NONE = 0,
				CURLY_OPEN = 1,
				CURLY_CLOSE = 2,
				SQUARED_OPEN = 3,
				SQUARED_CLOSE = 4,
				COLON = 5,
				COMMA = 6,
				STRING = 7,
				NUMBER = 8,
				TRUE = 9,
				FALSE = 10,
				NULL = 11
			}

			private const string WHITE_SPACE = " \t\n\r\ufeff";

			private const string WORD_BREAK = " \t\n\r{}[],:\"";

			// Direct index over the source text. StringReader.Peek()/Read() are virtual calls that cost
			// more than the parse work itself on a 40 MB chart, so the parser walks the string.
			private readonly string source;
			private readonly string?[] shortStrings = new string?[256];

			private int position;

			private string endSection;

			private char PeekChar => (char)Peek();

			private char NextChar => (char)Read();

			private int Peek() => position < source.Length ? source[position] : -1;

			private int Read() => position < source.Length ? source[position++] : -1;

			private string NextWord
			{
				get
				{
					StringBuilder stringBuilder = new StringBuilder();
					int peek = Peek();
					while (peek != -1 && !IsWordBreak((char)peek))
					{
						stringBuilder.Append((char)Read());
						peek = Peek();
					}
					return stringBuilder.ToString();
				}
			}

			private static bool IsWordBreak(char c)
			{
				switch (c)
				{
				case ' ':
				case '\t':
				case '\n':
				case '\r':
				case '{':
				case '}':
				case '[':
				case ']':
				case ',':
				case ':':
				case '"':
					return true;
				default:
					return false;
				}
			}

			private TOKEN NextToken
			{
				get
				{
					EatWhitespace();
					if (Peek() == -1)
					{
						return TOKEN.NONE;
					}
					switch (PeekChar)
					{
					case '{':
						return TOKEN.CURLY_OPEN;
					case '}':
						Read();
						return TOKEN.CURLY_CLOSE;
					case '[':
						return TOKEN.SQUARED_OPEN;
					case ']':
						Read();
						return TOKEN.SQUARED_CLOSE;
					case ',':
						Read();
						return TOKEN.COMMA;
					case '"':
						return TOKEN.STRING;
					case ':':
						return TOKEN.COLON;
					case '-':
					case '0':
					case '1':
					case '2':
					case '3':
					case '4':
					case '5':
					case '6':
					case '7':
					case '8':
					case '9':
						return TOKEN.NUMBER;
					default:
						return NextWord switch
						{
							"false" => TOKEN.FALSE, 
							"true" => TOKEN.TRUE, 
							"null" => TOKEN.NULL, 
							_ => TOKEN.NONE, 
						};
					}
				}
			}

			private Parser(string jsonString)
			{
				source = jsonString;
				position = 0;

				// Skip the UTF-8 BOM if the text was decoded with it still attached.
				if (source.Length > 0 && source[0] == '\uFEFF')
				{
					position = 1;
				}
			}

			public static object Parse(string jsonString)
			{
				using Parser parser = new Parser(jsonString);
				return parser.ParseValue();
			}

			/// <summary>Parse into Newtonsoft tokens directly / 直接产成 Newtonsoft token.</summary>
			public static JObject ParseToken(string jsonString)
			{
				using Parser parser = new Parser(jsonString);
				return parser.ParseJValue() as JObject;
			}

			/// <summary>
			/// Parse only the requested top-level members and skip everything else. Charts routinely
			/// carry tens of megabytes of decorations that analysis never reads, so skipping them by
			/// bracket depth removes most of the work without changing the resulting values.
			/// </summary>
			/// <param name="jsonString">Source text / 源文本</param>
			/// <param name="wanted">Top-level keys to keep; null keeps everything.</param>
			public static JObject ParseTokenSelective(string jsonString, ISet<string>? wanted)
			{
				using Parser parser = new Parser(jsonString);
				return wanted is null ? parser.ParseJValue() as JObject : parser.ParseJObjectSelective(wanted);
			}

			private JObject ParseJObjectSelective(ISet<string> wanted)
			{
				JObject result = [];
				Read();

				while (true)
				{
					switch (NextToken)
					{
						case TOKEN.COMMA:
							continue;
						case TOKEN.NONE:
							return result;
						case TOKEN.CURLY_CLOSE:
							return result;
					}

					string key = ParseString();

					if (key is null)
					{
						return result;
					}

					if (NextToken != TOKEN.COLON)
					{
						return result;
					}

					Read();

					if (wanted.Contains(key))
					{
						result[key] = ParseJValue();
					}
					else
					{
						SkipValue();
					}
				}
			}

			/// <summary>Skip one value without materialising it, tracking nesting depth.</summary>
			private void SkipValue()
			{
				TOKEN token = NextToken;

				if (token is not (TOKEN.CURLY_OPEN or TOKEN.SQUARED_OPEN))
				{
					// Scalars (string/number/literal) are a single token; NextToken already consumed
					// the delimiter for closers, so nothing else to do here.
					if (token is TOKEN.STRING)
					{
						SkipString();
					}

					return;
				}

				int depth = 0;
				bool inString = false;
				bool escaped = false;

				// The opening bracket itself was not consumed by NextToken.
				Read();

				while (position < source.Length)
				{
					char c = source[position];

					if (inString)
					{
						if (escaped)
						{
							escaped = false;
						}
						else if (c == '\\')
						{
							escaped = true;
						}
						else if (c == '"')
						{
							inString = false;
						}

						position++;
						continue;
					}

					switch (c)
					{
						case '"':
							inString = true;
							break;
						case '{':
						case '[':
							depth++;
							break;
						case '}':
						case ']':
							if (depth == 0)
							{
								position++;
								return;
							}

							depth--;
							break;
					}

					position++;
				}
			}

			/// <summary>Skip a quoted string without allocating it.</summary>
			private void SkipString()
			{
				Read();

				while (position < source.Length)
				{
					char c = source[position];

					if (c == '\\')
					{
						position += 2;
						continue;
					}

					position++;

					if (c == '"')
					{
						return;
					}
				}
			}

			public static object ParsePartially(string jsonString, string upToSection)
			{
				using Parser parser = new Parser(jsonString);
				parser.endSection = upToSection;
				return parser.ParseValue();
			}

			public void Dispose()
			{
			}

			private Dictionary<string, object> ParseObject()
			{
				Dictionary<string, object> dictionary = new Dictionary<string, object>();
				Read();
				while (true)
				{
					switch (NextToken)
					{
					case TOKEN.COMMA:
						continue;
					case TOKEN.NONE:
						return null;
					case TOKEN.CURLY_CLOSE:
						return dictionary;
					}
					string text = ParseString();
					if (text == null)
					{
						return null;
					}
					if (NextToken != TOKEN.COLON)
					{
						return null;
					}
					if (endSection != null && string.Equals(text, endSection))
					{
						return dictionary;
					}
					Read();
					dictionary[text] = ParseValue();
				}
			}

			private List<object> ParseArray()
			{
				List<object> list = [];
				Read();
				bool flag = true;
				while (flag)
				{
					TOKEN nextToken = NextToken;
					switch (nextToken)
					{
					case TOKEN.NONE:
						return null;
					case TOKEN.SQUARED_CLOSE:
						flag = false;
						break;
					default:
					{
						object item = ParseByToken(nextToken);
						list.Add(item);
						break;
					}
					case TOKEN.COMMA:
						break;
					}
				}
				return list;
			}

			private object ParseValue()
			{
				TOKEN nextToken = NextToken;
				return ParseByToken(nextToken);
			}

			// ---- Direct JToken production -------------------------------------------------
			// The Level type exposes Newtonsoft tokens, and converting the loosely typed Dictionary
			// afterwards means allocating every object twice. Parsing straight into tokens removes the
			// intermediate tree entirely, which is the single largest saving on a large chart.

			private JObject ParseJObject()
			{
				JObject result = [];
				Read();

				while (true)
				{
					switch (NextToken)
					{
						case TOKEN.COMMA:
							continue;
						case TOKEN.NONE:
						case TOKEN.CURLY_CLOSE:
							return result;
					}

					int before = position;
					string key = ParseString();

					if (key is null)
					{
						return result;
					}

					if (NextToken != TOKEN.COLON)
					{
						return result;
					}

					Read();

					// A chart can repeat a property name; the Dictionary-based reader overwrote it, so
					// the token path has to keep that behaviour instead of throwing on Add.
					result[key] = ParseJValue();

					if (position == before)
					{
						return result;
					}
				}
			}

			private JArray ParseJArray()
			{
				JArray result = [];
				Read();

				while (true)
				{
					int before = position;

					// The token has to be passed on: reading it a second time inside ParseJValue
					// consumes the following token as well, which desynchronises the parser on any
					// literal element such as [null, null] and then loops forever on the next ':'.
					TOKEN token = NextToken;

					switch (token)
					{
						case TOKEN.NONE:
						case TOKEN.SQUARED_CLOSE:
							return result;
						case TOKEN.COMMA:
							continue;
					}

					result.Add(ParseJValue(token));

					// A malformed document must never turn into an endless append loop.
					if (position == before)
					{
						return result;
					}
				}
			}

			private JToken ParseJValue() => ParseJValue(NextToken);

			private JToken ParseJValue(TOKEN token)
			{
				switch (token)
				{
					case TOKEN.STRING:
						return new JValue(ParseString());
					case TOKEN.NUMBER:
						return new JValue(ParseNumber());
					case TOKEN.CURLY_OPEN:
						return ParseJObject();
					case TOKEN.SQUARED_OPEN:
						return ParseJArray();
					case TOKEN.TRUE:
						return new JValue(true);
					case TOKEN.FALSE:
						return new JValue(false);
					case TOKEN.NULL:
						return JValue.CreateNull();
					default:
						return JValue.CreateNull();
				}
			}

			private object ParseByToken(TOKEN token)
			{
				return token switch
				{
					TOKEN.STRING => ParseString(), 
					TOKEN.NUMBER => ParseNumber(), 
					TOKEN.CURLY_OPEN => ParseObject(), 
					TOKEN.SQUARED_OPEN => ParseArray(), 
					TOKEN.TRUE => true, 
					TOKEN.FALSE => false, 
					TOKEN.NULL => null, 
					_ => null, 
				};
			}

			private string ParseString()
			{
				Read();

				// Scan plain strings directly. Event keys and names recur throughout a chart,
				// so reuse short strings within this parse without retaining them globally.
				int start = position;

				while (position < source.Length)
				{
					char c = source[position];

					if (c == '"')
					{
						string plain = GetPlainString(start, position - start);
						position++;
						return plain;
					}

					if (c == '\\')
					{
						break;
					}

					position++;
				}

				// Escape sequence (or end of input) ahead: rebuild the string piecewise.
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.Append(source, start, position - start);

				bool flag = true;
				while (flag)
				{
					if (Peek() == -1)
					{
						flag = false;
						break;
					}
					char nextChar = NextChar;
					switch (nextChar)
					{
					case '"':
						flag = false;
						break;
					case '\\':
						if (Peek() == -1)
						{
							flag = false;
							break;
						}
						nextChar = NextChar;
						switch (nextChar)
						{
						case '"':
						case '/':
						case '\\':
							stringBuilder.Append(nextChar);
							break;
						case 'b':
							stringBuilder.Append('\b');
							break;
						case 'f':
							stringBuilder.Append('\f');
							break;
						case 'n':
							stringBuilder.Append('\n');
							break;
						case 'r':
							stringBuilder.Append('\r');
							break;
						case 't':
							stringBuilder.Append('\t');
							break;
						case 'u':
						{
							int code = 0;
							for (int i = 0; i < 4; i++)
							{
								code = code * 16 + HexValue(NextChar);
							}
							stringBuilder.Append((char)code);
							break;
						}
						}
						break;
					default:
						stringBuilder.Append(nextChar);
						break;
					}
				}
				return stringBuilder.ToString();
			}

			private string GetPlainString(int start, int length)
			{
				if (length is < 1 or > 32)
					return source.Substring(start, length);

				int slot = (length * 31 + source[start] * 7 + source[start + length - 1]) & (shortStrings.Length - 1);
				string? cached = shortStrings[slot];
				if (cached != null && cached.Length == length && string.CompareOrdinal(source, start, cached, 0, length) == 0)
					return cached;

				return shortStrings[slot] = source.Substring(start, length);
			}

			private static int HexValue(char c) => c switch
			{
				>= '0' and <= '9' => c - '0',
				>= 'a' and <= 'f' => c - 'a' + 10,
				>= 'A' and <= 'F' => c - 'A' + 10,
				_ => 0,
			};

			// 数字解析用的复用缓冲：20 万个数字复用同一块内存，避免每格分配字符串/数组
			// Reusable buffer for numeric parsing: 200k numbers share this one buffer instead of
			// allocating a string (or an array) per number
			private readonly char[] numberBuffer = new char[64];

			/// <summary>
			/// 直接扫描数字字符再一次性解析，避免每个数字都创建临时字符串。
			/// 语义与原实现保持一致：无小数点/指数的数字返回 int，其余返回 float。
			/// Scans the numeric characters and parses them in one go, so no temporary string is
			/// allocated per number. The semantics match the original: a number without a decimal
			/// point or exponent is returned as int, everything else as float.
			/// </summary>
			private object ParseNumber()
			{
				char[] buffer = numberBuffer;
				int length = 0;
				bool hasPoint = false;
				bool hasExponent = false;

				int peek = Peek();
				while (peek != -1)
				{
					char c = (char)peek;
					if (IsWordBreak(c))
					{
						break;
					}

					if (c == '.')
					{
						hasPoint = true;
					}
					else if (c is 'e' or 'E')
					{
						hasExponent = true;
					}

					// 超长数字退回字符串路径，正常数字远小于 64 位
					// Very long numbers fall back to the string path; real ones are far shorter
					if (length >= buffer.Length)
					{
						return ParseNumberSlow(new string(buffer, 0, length));
					}

					buffer[length] = c;
					length++;
					Read();
					peek = Peek();
				}

#if NET6_0_OR_GREATER
				ReadOnlySpan<char> text = buffer.AsSpan(0, length);
#else
				string text = length == 0 ? "0" : new string(buffer, 0, length);
#endif

				if (!hasPoint && !hasExponent)
				{
					int.TryParse(text, System.Globalization.NumberStyles.Integer,
						System.Globalization.CultureInfo.InvariantCulture, out int integer);
					return integer;
				}

				float.TryParse(text, System.Globalization.NumberStyles.Float,
					System.Globalization.CultureInfo.InvariantCulture, out float value);
				return value;
			}

			/// <summary>超长数字的兜底解析 / Fallback for numbers that do not fit the inline buffer.</summary>
			private object ParseNumberSlow(string head)
			{
				StringBuilder tail = new StringBuilder(head);
				int peek = Peek();
				while (peek != -1 && !IsWordBreak((char)peek))
				{
					tail.Append((char)Read());
					peek = Peek();
				}

				string text = tail.ToString();
				if (text.IndexOf('.') == -1 && text.IndexOf('e') == -1 && text.IndexOf('E') == -1)
				{
					int.TryParse(text, out int result);
					return result;
				}

				float.TryParse(text, System.Globalization.NumberStyles.Float,
					System.Globalization.CultureInfo.InvariantCulture, out float value);
				return value;
			}

			private void EatWhitespace()
			{
				int peek = Peek();
				while (peek != -1 && IsWhitespace((char)peek))
				{
					Read();
					peek = Peek();
				}
			}

			private static bool IsWhitespace(char c) => c is ' ' or '\t' or '\n' or '\r' or '\ufeff';
		}

		private sealed class Serializer
		{
			private StringBuilder builder;

			private Serializer()
			{
				builder = new StringBuilder();
			}

			public static string Serialize(object obj)
			{
				Serializer serializer = new Serializer();
				serializer.SerializeValue(obj);
				return serializer.builder.ToString();
			}

			private void SerializeValue(object value)
			{
				if (value == null)
				{
					builder.Append("null");
				}
				else if (value is string str)
				{
					SerializeString(str);
				}
				else if (value is bool)
				{
					builder.Append(value.ToString().ToLower());
				}
				else if (value is IList anArray)
				{
					SerializeArray(anArray);
				}
				else if (value is IDictionary obj)
				{
					SerializeObject(obj);
				}
				else if (value is char)
				{
					SerializeString(value.ToString());
				}
				else
				{
					SerializeOther(value);
				}
			}

			private void SerializeObject(IDictionary obj)
			{
				bool flag = true;
				builder.Append("{\n");
				foreach (object key in obj.Keys)
				{
					if (!flag)
					{
						builder.Append(",\n");
					}
					SerializeString(key.ToString());
					builder.Append(':');
					SerializeValue(obj[key]);
					flag = false;
				}
				builder.Append("\n}");
			}

			private void SerializeArray(IList anArray)
			{
				builder.Append('[');
				bool flag = true;
				foreach (object item in anArray)
				{
					if (!flag)
					{
						builder.Append(',');
					}
					SerializeValue(item);
					flag = false;
				}
				builder.Append(']');
			}

			private void SerializeString(string str)
			{
				builder.Append('"');
				char[] array = str.ToCharArray();
				foreach (char c in array)
				{
					switch (c)
					{
					case '"':
						builder.Append("\\\"");
						continue;
					case '\\':
						builder.Append("\\\\");
						continue;
					case '\b':
						builder.Append("\\b");
						continue;
					case '\f':
						builder.Append("\\f");
						continue;
					case '\n':
						builder.Append("\\n");
						continue;
					case '\r':
						builder.Append("\\r");
						continue;
					case '\t':
						builder.Append("\\t");
						continue;
					}
					int num = Convert.ToInt32(c);
					if (num >= 32 && num <= 126)
					{
						builder.Append(c);
					}
					else
					{
						builder.Append("\\u" + Convert.ToString(num, 16).PadLeft(4, '0'));
					}
				}
				builder.Append('"');
			}

			private void SerializeOther(object value)
			{
				if (value is float || value is int || value is uint || value is long || value is double || value is sbyte || value is byte || value is short || value is ushort || value is ulong || value is decimal)
				{
					builder.Append(value);
				}
				else
				{
					SerializeString(value.ToString());
				}
			}
		}

		public static Dictionary<string,object> Deserialize(string json)
		{
			if (json == null)
			{
				return null;
			}
			return Parser.Parse(json) as Dictionary<string,object>;
		}

		/// <summary>
		/// 直接解析成 Newtonsoft 的 JObject，跳过 Dictionary 中间层。
		/// Parses straight into a <see cref="JObject"/>, skipping the intermediate Dictionary.
		/// </summary>
		/// <remarks>
		/// The Level type exposes tokens, so building a Dictionary only to convert it costs a second
		/// full allocation of the whole document. On a 40 MB chart that is several seconds.
		/// </remarks>
		public static JObject DeserializeToken(string json)
		{
			if (json == null)
			{
				return null;
			}
			return Parser.ParseToken(json);
		}

		/// <summary>直接解析成 JObject（从文件）/ Parses a file straight into a <see cref="JObject"/>.</summary>
		public static JObject DeserializeTokenFile(string path)
		{
			if (path == null) throw new ArgumentNullException(nameof(path));
			return Parser.ParseToken(File.ReadAllText(path));
		}

		public static Dictionary<string,object> DeserializeFile(string path)
		{
			if (path == null) throw new ArgumentNullException(nameof(path));
			return Parser.Parse(File.ReadAllText(path)) as Dictionary<string,object>;
		}

		public static object DeserializePartially(string json, string upToSection)
		{
			if (json == null)
			{
				return null;
			}
			return Parser.ParsePartially(json, upToSection);
		}

		public static string Serialize(object obj)
		{
			return Serializer.Serialize(obj);
		}
	}
}
