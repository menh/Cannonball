using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Jingzhou.Battle
{
    /// <summary>
    /// Small JSON reader for the battle file. Objects become dictionaries,
    /// arrays become lists, numbers become longs when they have no fraction.
    /// </summary>
    public static class SimpleJson
    {
        public static Dictionary<string, object> ParseObject(string json)
        {
            object value = new Parser(json).Parse();
            Dictionary<string, object> obj = value as Dictionary<string, object>;
            if (obj == null)
                throw new FormatException("JSON root must be an object.");
            return obj;
        }

        public static string RequireString(Dictionary<string, object> obj, string key)
        {
            object value;
            if (!obj.TryGetValue(key, out value) || !(value is string))
                throw new FormatException("Missing string field \"" + key + "\".");
            return (string)value;
        }

        public static int RequireInt(Dictionary<string, object> obj, string key)
        {
            object value;
            if (!obj.TryGetValue(key, out value))
                throw new FormatException("Missing number field \"" + key + "\".");
            return AsInt(value, key);
        }

        public static bool OptionalBool(Dictionary<string, object> obj, string key, bool fallback)
        {
            object value;
            if (!obj.TryGetValue(key, out value) || value == null)
                return fallback;
            if (value is bool)
                return (bool)value;
            throw new FormatException("Field \"" + key + "\" must be true or false.");
        }

        public static List<object> RequireArray(Dictionary<string, object> obj, string key)
        {
            object value;
            if (!obj.TryGetValue(key, out value) || !(value is List<object>))
                throw new FormatException("Missing array field \"" + key + "\".");
            return (List<object>)value;
        }

        public static Dictionary<string, object> AsObject(object value, string label)
        {
            Dictionary<string, object> obj = value as Dictionary<string, object>;
            if (obj == null)
                throw new FormatException(label + " must be an object.");
            return obj;
        }

        static int AsInt(object value, string key)
        {
            if (value is long)
                return (int)(long)value;
            if (value is double)
                return (int)(double)value;
            throw new FormatException("Field \"" + key + "\" must be a number.");
        }

        sealed class Parser
        {
            readonly string text;
            int index;

            public Parser(string text)
            {
                this.text = text ?? "";
            }

            public object Parse()
            {
                object value = ReadValue();
                Skip();
                if (index != text.Length)
                    throw Error("Unexpected trailing data.");
                return value;
            }

            object ReadValue()
            {
                Skip();
                if (index >= text.Length)
                    throw Error("Unexpected end of JSON.");
                char c = text[index];
                if (c == '{') return ReadObject();
                if (c == '[') return ReadArray();
                if (c == '"') return ReadString();
                if (c == 't') return ReadLiteral("true", true);
                if (c == 'f') return ReadLiteral("false", false);
                if (c == 'n') return ReadLiteral("null", null);
                if (c == '-' || (c >= '0' && c <= '9')) return ReadNumber();
                throw Error("Unexpected character '" + c + "'.");
            }

            Dictionary<string, object> ReadObject()
            {
                index++;
                Dictionary<string, object> obj = new Dictionary<string, object>();
                Skip();
                if (Consume('}'))
                    return obj;
                while (true)
                {
                    Skip();
                    if (index >= text.Length || text[index] != '"')
                        throw Error("Expected an object key.");
                    string key = ReadString();
                    Skip();
                    if (!Consume(':'))
                        throw Error("Expected ':' after key.");
                    obj[key] = ReadValue();
                    Skip();
                    if (Consume('}'))
                        return obj;
                    if (!Consume(','))
                        throw Error("Expected ',' or '}' in object.");
                }
            }

            List<object> ReadArray()
            {
                index++;
                List<object> list = new List<object>();
                Skip();
                if (Consume(']'))
                    return list;
                while (true)
                {
                    list.Add(ReadValue());
                    Skip();
                    if (Consume(']'))
                        return list;
                    if (!Consume(','))
                        throw Error("Expected ',' or ']' in array.");
                }
            }

            string ReadString()
            {
                index++;
                StringBuilder builder = new StringBuilder();
                while (index < text.Length)
                {
                    char c = text[index++];
                    if (c == '"')
                        return builder.ToString();
                    if (c == '\\')
                    {
                        if (index >= text.Length)
                            throw Error("Bad escape.");
                        char e = text[index++];
                        if (e == '"' || e == '\\' || e == '/') builder.Append(e);
                        else if (e == 'b') builder.Append('\b');
                        else if (e == 'f') builder.Append('\f');
                        else if (e == 'n') builder.Append('\n');
                        else if (e == 'r') builder.Append('\r');
                        else if (e == 't') builder.Append('\t');
                        else if (e == 'u')
                        {
                            if (index + 4 > text.Length)
                                throw Error("Bad unicode escape.");
                            int code = int.Parse(text.Substring(index, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                            builder.Append((char)code);
                            index += 4;
                        }
                        else throw Error("Unknown escape.");
                    }
                    else
                    {
                        builder.Append(c);
                    }
                }
                throw Error("Unterminated string.");
            }

            object ReadNumber()
            {
                int start = index;
                if (text[index] == '-')
                    index++;
                while (index < text.Length && text[index] >= '0' && text[index] <= '9')
                    index++;
                bool fraction = false;
                if (index < text.Length && text[index] == '.')
                {
                    fraction = true;
                    index++;
                    while (index < text.Length && text[index] >= '0' && text[index] <= '9')
                        index++;
                }
                if (index < text.Length && (text[index] == 'e' || text[index] == 'E'))
                {
                    fraction = true;
                    index++;
                    if (index < text.Length && (text[index] == '+' || text[index] == '-'))
                        index++;
                    while (index < text.Length && text[index] >= '0' && text[index] <= '9')
                        index++;
                }
                string slice = text.Substring(start, index - start);
                if (!fraction)
                {
                    long whole;
                    if (!long.TryParse(slice, NumberStyles.Integer, CultureInfo.InvariantCulture, out whole))
                        throw Error("Bad number.");
                    return whole;
                }
                double real;
                if (!double.TryParse(slice, NumberStyles.Float, CultureInfo.InvariantCulture, out real))
                    throw Error("Bad number.");
                return real;
            }

            object ReadLiteral(string word, object value)
            {
                if (index + word.Length > text.Length || text.Substring(index, word.Length) != word)
                    throw Error("Expected " + word + ".");
                index += word.Length;
                return value;
            }

            bool Consume(char c)
            {
                if (index < text.Length && text[index] == c)
                {
                    index++;
                    return true;
                }
                return false;
            }

            void Skip()
            {
                while (index < text.Length)
                {
                    char c = text[index];
                    if (c != ' ' && c != '\t' && c != '\n' && c != '\r')
                        return;
                    index++;
                }
            }

            FormatException Error(string message)
            {
                return new FormatException(message + " (at " + index + ")");
            }
        }
    }
}
