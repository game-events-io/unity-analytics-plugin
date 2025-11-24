using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Whalytics.Utils
{
    /// <summary>
    /// A very simple JSON serializer to avoid external dependencies.
    /// Supports basic types, Dictionaries, and Lists.
    /// </summary>
    public static class SimpleJson
    {
        /// <summary>
        /// Serializes an object to a JSON string.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        /// <returns>A JSON string representation of the object.</returns>
        public static string Serialize(object obj)
        {
            return SerializeValue(obj);
        }

        private static string SerializeValue(object value)
        {
            if (value == null)
                return "null";

            if (value is string str)
                return "\"" + EscapeString(str) + "\"";

            if (value is bool b)
                return b ? "true" : "false";

            if (value is IDictionary dict)
                return SerializeDictionary(dict);

            if (value is IEnumerable list)
                return SerializeList(list);

            if (value is int || value is long || value is float || value is double || value is decimal)
                return Convert.ToString(value, CultureInfo.InvariantCulture);

            return "\"" + EscapeString(value.ToString()) + "\"";
        }

        private static string SerializeDictionary(IDictionary dict)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            bool first = true;
            foreach (DictionaryEntry entry in dict)
            {
                if (!first)
                    sb.Append(",");
                sb.Append("\"" + EscapeString(entry.Key.ToString()) + "\":");
                sb.Append(SerializeValue(entry.Value));
                first = false;
            }
            sb.Append("}");
            return sb.ToString();
        }

        private static string SerializeList(IEnumerable list)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            bool first = true;
            foreach (object item in list)
            {
                if (!first)
                    sb.Append(",");
                sb.Append(SerializeValue(item));
                first = false;
            }
            sb.Append("]");
            return sb.ToString();
        }

        private static string EscapeString(string str)
        {
            if (str == null) return "";
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < ' ')
                        {
                            sb.Append("\\u" + ((int)c).ToString("x4"));
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Deserializes a JSON string into an object (Dictionary, List, or primitive).
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>The deserialized object.</returns>
        public static object Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            return Parser.Parse(json);
        }

        private static class Parser
        {
            public static object Parse(string json)
            {
                int index = 0;
                return ParseValue(json, ref index);
            }

            private static object ParseValue(string json, ref int index)
            {
                SkipWhitespace(json, ref index);
                if (index >= json.Length) return null;

                char c = json[index];
                if (c == '{') return ParseObject(json, ref index);
                if (c == '[') return ParseArray(json, ref index);
                if (c == '"') return ParseString(json, ref index);
                if (char.IsDigit(c) || c == '-') return ParseNumber(json, ref index);
                if (json.Substring(index, 4) == "true") { index += 4; return true; }
                if (json.Substring(index, 5) == "false") { index += 5; return false; }
                if (json.Substring(index, 4) == "null") { index += 4; return null; }

                return null; // Error or unknown
            }

            private static Dictionary<string, object> ParseObject(string json, ref int index)
            {
                Dictionary<string, object> dict = new Dictionary<string, object>();
                index++; // skip '{'

                while (index < json.Length)
                {
                    SkipWhitespace(json, ref index);
                    if (json[index] == '}')
                    {
                        index++;
                        return dict;
                    }

                    string key = ParseString(json, ref index);
                    SkipWhitespace(json, ref index);
                    if (json[index] == ':') index++;

                    object value = ParseValue(json, ref index);
                    dict[key] = value;

                    SkipWhitespace(json, ref index);
                    if (json[index] == ',') index++;
                }
                return dict;
            }

            private static List<object> ParseArray(string json, ref int index)
            {
                List<object> list = new List<object>();
                index++; // skip '['

                while (index < json.Length)
                {
                    SkipWhitespace(json, ref index);
                    if (json[index] == ']')
                    {
                        index++;
                        return list;
                    }

                    list.Add(ParseValue(json, ref index));

                    SkipWhitespace(json, ref index);
                    if (json[index] == ',') index++;
                }
                return list;
            }

            private static string ParseString(string json, ref int index)
            {
                StringBuilder sb = new StringBuilder();
                index++; // skip '"'

                while (index < json.Length)
                {
                    char c = json[index];
                    if (c == '"')
                    {
                        index++;
                        return sb.ToString();
                    }

                    if (c == '\\')
                    {
                        index++;
                        c = json[index];
                        // Handle escapes roughly
                        if (c == 'n') sb.Append('\n');
                        else if (c == 'r') sb.Append('\r');
                        else if (c == 't') sb.Append('\t');
                        else sb.Append(c);
                    }
                    else
                    {
                        sb.Append(c);
                    }
                    index++;
                }
                return sb.ToString();
            }

            private static object ParseNumber(string json, ref int index)
            {
                int start = index;
                while (index < json.Length && (char.IsDigit(json[index]) || json[index] == '.' || json[index] == '-'))
                {
                    index++;
                }
                string numStr = json.Substring(start, index - start);
                if (long.TryParse(numStr, out long l)) return l;
                if (double.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double d)) return d;
                return 0;
            }

            private static void SkipWhitespace(string json, ref int index)
            {
                while (index < json.Length && char.IsWhiteSpace(json[index]))
                {
                    index++;
                }
            }
        }
    }
}
