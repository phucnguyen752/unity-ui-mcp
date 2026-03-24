using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace UnityMCP.Core
{
    /// <summary>
    /// Lightweight JSON parser / serializer for Unity.
    /// Replaces Newtonsoft.Json — no external dependency needed.
    ///
    /// Parse:  object result = MiniJson.Deserialize(jsonString);
    ///   - JSON object  → Dictionary&lt;string, object&gt;
    ///   - JSON array   → List&lt;object&gt;
    ///   - JSON string  → string
    ///   - JSON number  → double (or long if no decimal point)
    ///   - JSON bool    → bool
    ///   - JSON null    → null
    ///
    /// Serialize:  string json = MiniJson.Serialize(obj);
    ///   - Supports Dictionary, List, string, number, bool, null, and nested combinations.
    /// </summary>
    public static class MiniJson
    {
        // ══════════════════════════════════════════════════════════════
        //  PUBLIC API
        // ══════════════════════════════════════════════════════════════

        public static object Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            return new Parser(json).ParseValue();
        }

        public static Dictionary<string, object> DeserializeObject(string json)
        {
            return Deserialize(json) as Dictionary<string, object>;
        }

        public static string Serialize(object obj, bool pretty = false)
        {
            var sb = new StringBuilder(256);
            SerializeValue(obj, sb, pretty, 0);
            return sb.ToString();
        }

        // ══════════════════════════════════════════════════════════════
        //  REFLECTION-BASED DESERIALIZE
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Deserialize JSON string into an object of type T using reflection.
        /// Supports nullable types (float?, int?), nested objects, and arrays.
        /// </summary>
        public static T DeserializeTo<T>(string json) where T : new()
        {
            var dict = DeserializeObject(json);
            if (dict == null) return new T();
            return (T)PopulateObject(typeof(T), dict);
        }

        private static object PopulateObject(Type type, Dictionary<string, object> dict)
        {
            var obj = Activator.CreateInstance(type);
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (var field in fields)
            {
                if (!dict.TryGetValue(field.Name, out var val) || val == null)
                    continue;

                field.SetValue(obj, ConvertValue(val, field.FieldType));
            }

            return obj;
        }

        private static object ConvertValue(object val, Type targetType)
        {
            if (val == null) return null;

            // Handle Nullable<T>
            var underlying = Nullable.GetUnderlyingType(targetType);
            if (underlying != null)
                return ConvertValue(val, underlying);

            // Primitives
            if (targetType == typeof(string))
                return val.ToString();
            if (targetType == typeof(int))
                return val is long l ? (int)l : val is double d ? (int)d : Convert.ToInt32(val);
            if (targetType == typeof(float))
                return val is double d ? (float)d : val is long l ? (float)l : Convert.ToSingle(val);
            if (targetType == typeof(double))
                return val is double d ? d : val is long l ? (double)l : Convert.ToDouble(val);
            if (targetType == typeof(long))
                return val is long l ? l : val is double d ? (long)d : Convert.ToInt64(val);
            if (targetType == typeof(bool))
                return val is bool b ? b : Convert.ToBoolean(val);

            // Nested object
            if (val is Dictionary<string, object> nested && targetType.IsClass)
                return PopulateObject(targetType, nested);

            // Array/List not needed for current models, fall through
            return val;
        }

        // ══════════════════════════════════════════════════════════════
        //  HELPER: Get values from Dictionary safely
        // ══════════════════════════════════════════════════════════════

        public static string GetString(this Dictionary<string, object> dict, string key, string fallback = "")
        {
            if (dict != null && dict.TryGetValue(key, out var v) && v != null)
                return v.ToString();
            return fallback;
        }

        public static int GetInt(this Dictionary<string, object> dict, string key, int fallback = 0)
        {
            if (dict != null && dict.TryGetValue(key, out var v) && v != null)
            {
                if (v is double d) return (int)d;
                if (v is long l) return (int)l;
                if (int.TryParse(v.ToString(), out var i)) return i;
            }
            return fallback;
        }

        public static float GetFloat(this Dictionary<string, object> dict, string key, float fallback = 0f)
        {
            if (dict != null && dict.TryGetValue(key, out var v) && v != null)
            {
                if (v is double d) return (float)d;
                if (v is long l) return l;
                if (float.TryParse(v.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var f)) return f;
            }
            return fallback;
        }

        public static bool GetBool(this Dictionary<string, object> dict, string key, bool fallback = false)
        {
            if (dict != null && dict.TryGetValue(key, out var v) && v != null)
            {
                if (v is bool b) return b;
                if (bool.TryParse(v.ToString(), out var b2)) return b2;
            }
            return fallback;
        }

        public static Dictionary<string, object> GetObject(this Dictionary<string, object> dict, string key)
        {
            if (dict != null && dict.TryGetValue(key, out var v))
                return v as Dictionary<string, object>;
            return null;
        }

        public static List<object> GetArray(this Dictionary<string, object> dict, string key)
        {
            if (dict != null && dict.TryGetValue(key, out var v))
                return v as List<object>;
            return null;
        }

        public static bool HasKey(this Dictionary<string, object> dict, string key)
        {
            return dict != null && dict.ContainsKey(key);
        }

        // ══════════════════════════════════════════════════════════════
        //  PARSER
        // ══════════════════════════════════════════════════════════════

        private class Parser
        {
            private readonly string _json;
            private int _pos;

            public Parser(string json)
            {
                _json = json;
                _pos = 0;
            }

            public object ParseValue()
            {
                SkipWhitespace();
                if (_pos >= _json.Length) return null;

                char c = _json[_pos];
                switch (c)
                {
                    case '{': return ParseObject();
                    case '[': return ParseArray();
                    case '"': return ParseString();
                    case 't': case 'f': return ParseBool();
                    case 'n': return ParseNull();
                    default:
                        if (c == '-' || (c >= '0' && c <= '9'))
                            return ParseNumber();
                        throw new FormatException($"Unexpected char '{c}' at position {_pos}");
                }
            }

            private Dictionary<string, object> ParseObject()
            {
                var dict = new Dictionary<string, object>();
                _pos++; // skip '{'
                SkipWhitespace();

                if (_pos < _json.Length && _json[_pos] == '}')
                {
                    _pos++;
                    return dict;
                }

                while (_pos < _json.Length)
                {
                    SkipWhitespace();
                    var key = ParseString();
                    SkipWhitespace();
                    Expect(':');
                    var value = ParseValue();
                    dict[key] = value;

                    SkipWhitespace();
                    if (_pos < _json.Length && _json[_pos] == ',')
                    {
                        _pos++;
                        continue;
                    }
                    break;
                }

                SkipWhitespace();
                Expect('}');
                return dict;
            }

            private List<object> ParseArray()
            {
                var list = new List<object>();
                _pos++; // skip '['
                SkipWhitespace();

                if (_pos < _json.Length && _json[_pos] == ']')
                {
                    _pos++;
                    return list;
                }

                while (_pos < _json.Length)
                {
                    list.Add(ParseValue());
                    SkipWhitespace();
                    if (_pos < _json.Length && _json[_pos] == ',')
                    {
                        _pos++;
                        continue;
                    }
                    break;
                }

                SkipWhitespace();
                Expect(']');
                return list;
            }

            private string ParseString()
            {
                Expect('"');
                var sb = new StringBuilder();

                while (_pos < _json.Length)
                {
                    char c = _json[_pos++];
                    if (c == '"') return sb.ToString();
                    if (c == '\\')
                    {
                        if (_pos >= _json.Length) break;
                        char esc = _json[_pos++];
                        switch (esc)
                        {
                            case '"':  sb.Append('"'); break;
                            case '\\': sb.Append('\\'); break;
                            case '/':  sb.Append('/'); break;
                            case 'b':  sb.Append('\b'); break;
                            case 'f':  sb.Append('\f'); break;
                            case 'n':  sb.Append('\n'); break;
                            case 'r':  sb.Append('\r'); break;
                            case 't':  sb.Append('\t'); break;
                            case 'u':
                                if (_pos + 4 <= _json.Length)
                                {
                                    var hex = _json.Substring(_pos, 4);
                                    sb.Append((char)int.Parse(hex, NumberStyles.HexNumber));
                                    _pos += 4;
                                }
                                break;
                            default: sb.Append(esc); break;
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }

                return sb.ToString();
            }

            private object ParseNumber()
            {
                int start = _pos;
                bool isFloat = false;

                if (_pos < _json.Length && _json[_pos] == '-') _pos++;
                while (_pos < _json.Length && _json[_pos] >= '0' && _json[_pos] <= '9') _pos++;

                if (_pos < _json.Length && _json[_pos] == '.')
                {
                    isFloat = true;
                    _pos++;
                    while (_pos < _json.Length && _json[_pos] >= '0' && _json[_pos] <= '9') _pos++;
                }

                if (_pos < _json.Length && (_json[_pos] == 'e' || _json[_pos] == 'E'))
                {
                    isFloat = true;
                    _pos++;
                    if (_pos < _json.Length && (_json[_pos] == '+' || _json[_pos] == '-')) _pos++;
                    while (_pos < _json.Length && _json[_pos] >= '0' && _json[_pos] <= '9') _pos++;
                }

                var numStr = _json.Substring(start, _pos - start);

                if (isFloat)
                    return double.Parse(numStr, CultureInfo.InvariantCulture);

                if (long.TryParse(numStr, out var l))
                    return l;
                return double.Parse(numStr, CultureInfo.InvariantCulture);
            }

            private bool ParseBool()
            {
                if (_json.Substring(_pos, 4) == "true")  { _pos += 4; return true; }
                if (_json.Substring(_pos, 5) == "false") { _pos += 5; return false; }
                throw new FormatException($"Expected bool at position {_pos}");
            }

            private object ParseNull()
            {
                if (_json.Substring(_pos, 4) == "null") { _pos += 4; return null; }
                throw new FormatException($"Expected null at position {_pos}");
            }

            private void SkipWhitespace()
            {
                while (_pos < _json.Length)
                {
                    char c = _json[_pos];
                    if (c == ' ' || c == '\t' || c == '\n' || c == '\r')
                        _pos++;
                    else
                        break;
                }
            }

            private void Expect(char c)
            {
                if (_pos >= _json.Length || _json[_pos] != c)
                    throw new FormatException($"Expected '{c}' at position {_pos}");
                _pos++;
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  SERIALIZER
        // ══════════════════════════════════════════════════════════════

        private static void SerializeValue(object val, StringBuilder sb, bool pretty, int indent)
        {
            if (val == null)
            {
                sb.Append("null");
            }
            else if (val is string s)
            {
                SerializeString(s, sb);
            }
            else if (val is bool b)
            {
                sb.Append(b ? "true" : "false");
            }
            else if (val is IDictionary dict)
            {
                SerializeDict(dict, sb, pretty, indent);
            }
            else if (val is IList list)
            {
                SerializeList(list, sb, pretty, indent);
            }
            else if (val is float f)
            {
                sb.Append(f.ToString(CultureInfo.InvariantCulture));
            }
            else if (val is double d)
            {
                sb.Append(d.ToString(CultureInfo.InvariantCulture));
            }
            else if (val is int || val is long || val is short || val is byte)
            {
                sb.Append(val);
            }
            else
            {
                // Fallback: reflection-based serialization (anonymous types, POCOs)
                SerializeReflection(val, sb, pretty, indent);
            }
        }

        private static void SerializeString(string s, StringBuilder sb)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"':  sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20)
                            sb.AppendFormat("\\u{0:x4}", (int)c);
                        else
                            sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }

        private static void SerializeDict(IDictionary dict, StringBuilder sb, bool pretty, int indent)
        {
            sb.Append('{');
            bool first = true;

            foreach (DictionaryEntry entry in dict)
            {
                if (!first) sb.Append(',');
                if (pretty)
                {
                    sb.Append('\n');
                    Indent(sb, indent + 1);
                }
                SerializeString(entry.Key.ToString(), sb);
                sb.Append(pretty ? ": " : ":");
                SerializeValue(entry.Value, sb, pretty, indent + 1);
                first = false;
            }

            if (pretty && !first)
            {
                sb.Append('\n');
                Indent(sb, indent);
            }
            sb.Append('}');
        }

        private static void SerializeList(IList list, StringBuilder sb, bool pretty, int indent)
        {
            sb.Append('[');
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0) sb.Append(',');
                if (pretty)
                {
                    sb.Append('\n');
                    Indent(sb, indent + 1);
                }
                SerializeValue(list[i], sb, pretty, indent + 1);
            }
            if (pretty && list.Count > 0)
            {
                sb.Append('\n');
                Indent(sb, indent);
            }
            sb.Append(']');
        }

        private static void SerializeReflection(object obj, StringBuilder sb, bool pretty, int indent)
        {
            var type = obj.GetType();
            sb.Append('{');

            // Prefer properties (for anonymous types), fallback to fields
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            bool first = true;

            if (props.Length > 0)
            {
                foreach (var prop in props)
                {
                    if (!prop.CanRead) continue;
                    if (!first) sb.Append(',');
                    if (pretty) { sb.Append('\n'); Indent(sb, indent + 1); }
                    SerializeString(prop.Name, sb);
                    sb.Append(pretty ? ": " : ":");
                    SerializeValue(prop.GetValue(obj), sb, pretty, indent + 1);
                    first = false;
                }
            }
            else
            {
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                foreach (var field in fields)
                {
                    if (!first) sb.Append(',');
                    if (pretty) { sb.Append('\n'); Indent(sb, indent + 1); }
                    SerializeString(field.Name, sb);
                    sb.Append(pretty ? ": " : ":");
                    SerializeValue(field.GetValue(obj), sb, pretty, indent + 1);
                    first = false;
                }
            }

            if (pretty && !first) { sb.Append('\n'); Indent(sb, indent); }
            sb.Append('}');
        }

        private static void Indent(StringBuilder sb, int level)
        {
            for (int i = 0; i < level; i++) sb.Append("  ");
        }
    }
}
