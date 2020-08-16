// Purpose: Provide a set of routines to support JSON Object and JSON Array classes
// Author : Scott Bakker
// Created: 09/13/2019
// LastMod: 08/16/2020

// --- Notes  : DateTime and DateTimeOffset are stored in JObject and JArray properly
//              as objects of those types.
//            : When JObject/JArray are converted to a string, the formats below are
//              used depending on the value type and contents.
//            : However, when converting back from a string, any value which passes
//              the IsDateTimeValue() or IsDateTimeOffsetValue() check will be
//              converted to a DateTime or DateTimeOffset, even if it had only been
//              a string value before. This could have unanticipated consequences.
//              Be careful storing strings which look like dates.

using System;
using System.Collections;
using System.Text;

namespace JsonLibrary
{
    public static class JsonRoutines
    {

        #region constants

        private const string _dateFormat = "yyyy-MM-dd";

        // These are unspecified time zones used for local times only
        private const string _timeFormat = "HH:mm:ss";
        private const string _timeMilliFormat = "HH:mm:ss.fff";
        private const string _dateTimeFormat = "yyyy-MM-dd HH:mm:ss";
        private const string _dateTimeMilliFormat = "yyyy-MM-dd HH:mm:ss.fff";

        // These are precise ISO 8601 format, with the "T" in the 11th position
        private const string _dateTimeOffsetFormat = "yyyy-MM-ddTHH:mm:sszzz";
        private const string _dateTimeOffsetMilliFormat = "yyyy-MM-ddTHH:mm:ss.fffffffzzz";

        // TimeSpan formats are very different
        private const string _timeSpanFormat = "c"; // [-][d'.']hh':'mm':'ss['.'fffffff]

        // ToStringFormatted
        private const int _indentSpaceSize = 2;

        #endregion

        #region public routines

        public static string IndentSpace(int indentLevel)
        {
            // Purpose: Return a string with the proper number of spaces or tabs
            // Author : Scott Bakker
            // Created: 09/13/2019
            if (indentLevel <= 0)
            {
                return "";
            }
            return new string(' ', indentLevel * _indentSpaceSize);
        }

        public static string ValueToString(object value)
        {
            // Purpose: Return a value in proper JSON string format
            // Author : Scott Bakker
            // Created: 09/13/2019
            int indentLevel = -1; // don't indent
            return ValueToString(value, ref indentLevel);
        }

        #endregion 

        #region internal routines

        internal static string ValueToString(object value, ref int indentLevel)
        {
            // Purpose: Return a value in proper JSON string format
            // Author : Scott Bakker
            // Created: 09/13/2019
            // LastMod: 08/11/2020

            if (value == null)
            {
                return "null";
            }

            // Get the type for comparison
            Type t = value.GetType();

            // Check for generic list types
            if (t.IsGenericType)
            {
                StringBuilder result = new StringBuilder();
                result.Append('[');
                if (indentLevel >= 0)
                {
                    indentLevel++;
                }
                bool addComma = false;
                foreach (object obj in (IEnumerable)value)
                {
                    if (addComma)
                    {
                        result.Append(',');
                    }
                    else
                    {
                        addComma = true;
                    }
                    if (indentLevel >= 0)
                    {
                        result.AppendLine();
                    }
                    if (indentLevel > 0)
                    {
                        result.Append(IndentSpace(indentLevel));
                    }
                    result.Append(ValueToString(obj));
                }
                if (indentLevel >= 0)
                {
                    result.AppendLine();
                    if (indentLevel > 0)
                    {
                        indentLevel--;
                    }
                    result.Append(IndentSpace(indentLevel));
                }
                result.Append(']');
                return result.ToString();
            }

            // Check for byte array, return as hex string "0x00..." with quotes
            if (t.IsArray && t == typeof(byte[]))
            {
                StringBuilder result = new StringBuilder();
                result.Append("\"0x");
                foreach (byte b in (byte[])value)
                {
                    result.Append(b.ToString("x2", null));
                }
                result.Append('\"');
                return result.ToString();
            }

            // Check for array, return in JArray format
            if (t.IsArray)
            {
                StringBuilder result = new StringBuilder();
                result.Append('[');
                if (indentLevel >= 0)
                {
                    indentLevel++;
                }
                bool addComma = false;
                for (int i = 0; i < ((Array)value).Length; i++)
                {
                    if (addComma)
                    {
                        result.Append(',');
                    }
                    else
                    {
                        addComma = true;
                    }
                    if (indentLevel >= 0)
                    {
                        result.AppendLine();
                        result.Append(IndentSpace(indentLevel));
                    }
                    object obj = ((Array)value).GetValue(i);
                    result.Append(ValueToString(obj, ref indentLevel));
                }
                if (indentLevel >= 0)
                {
                    result.AppendLine();
                    if (indentLevel > 0)
                    {
                        indentLevel--;
                    }
                    result.Append(IndentSpace(indentLevel));
                }
                result.Append(']');
                return result.ToString();
            }

            // Check for individual types

            if (t == typeof(string))
            {
                StringBuilder result = new StringBuilder();
                result.Append('\"');
                foreach (char c in (string)value)
                {
                    result.Append(ToJsonChar(c));
                }
                result.Append('\"');
                return result.ToString();
            }

            if (t == typeof(char))
            {
                StringBuilder result = new StringBuilder();
                result.Append('\"');
                result.Append(ToJsonChar((char)value));
                result.Append('\"');
                return result.ToString();
            }

            if (t == typeof(Guid))
            {
                return $"\"{value}\"";
            }

            if (t == typeof(bool))
            {
                if ((bool)value)
                {
                    return "true";
                }
                else
                {
                    return "false";
                }
            }

            if (t == typeof(DateTimeOffset))
            {
                string result;
                if (((DateTimeOffset)value).Millisecond == 0)
                {
                    result = ((DateTimeOffset)value).ToString(_dateTimeOffsetFormat, null);
                }
                else
                {
                    result = ((DateTimeOffset)value).ToString(_dateTimeOffsetMilliFormat, null);
                }
                if (result.EndsWith("+00:00") || result.EndsWith("-00:00"))
                {
                    result = $"{result.Substring(0, result.Length - 6)}Z";
                }
                return $"\"{result}\"";
            }

            if (t == typeof(DateTime))
            {
                DateTime d = (DateTime)value;
                if (d.Hour + d.Minute + d.Second + d.Millisecond == 0)
                {
                    return $"\"{d.ToString(_dateFormat, null)}\"";
                }
                if (d.Year + d.Month + d.Day == 0)
                {
                    if (d.Millisecond == 0)
                    {
                        return $"\"{d.ToString(_timeFormat, null)}\"";
                    }
                    else
                    {
                        return $"\"{d.ToString(_timeMilliFormat, null)}\"";
                    }
                }
                if (d.Millisecond == 0)
                {
                    return $"\"{d.ToString(_dateTimeFormat, null)}\"";
                }
                else
                {
                    return $"\"{d.ToString(_dateTimeMilliFormat, null)}\"";
                }
            }

            if (t == typeof(TimeSpan))
            {
                return ((TimeSpan)value).ToString(_timeSpanFormat);
            }

            if (t == typeof(JObject))
            {
                return ((JObject)value).ToStringFormatted(ref indentLevel);
            }

            if (t == typeof(JArray))
            {
                return ((JArray)value).ToStringFormatted(ref indentLevel);
            }

            if (t == typeof(float) ||
                t == typeof(double) ||
                t == typeof(decimal))
            {
                // Remove trailing decimal zeros. This is not necessary or part
                // of the JSON specification, but it will be impossible to
                // compare two JSON string representations without doing this.
                return NormalizeDecimal(value.ToString());
            }

            if (t == typeof(byte) ||
                t == typeof(sbyte) ||
                t == typeof(short) ||
                t == typeof(int) ||
                t == typeof(long) ||
                t == typeof(ushort) ||
                t == typeof(uint) ||
                t == typeof(ulong))
            {
                // Let ToString do all the work
                return value.ToString();
            }

            throw new SystemException($"JSON Error: Unknown object type: {t}");
        }

        internal static string NormalizeDecimal(string value)
        {
            // Purpose: Gets rid of trailing decimal zeros to normalize value
            // Author : Scott Bakker
            // Created: 03/19/2020
            // LastMod: 08/11/2020
            if (value.Contains("E") || value.Contains("e"))
            {
                // Scientific notation, leave alone
                return value;
            }
            if (!value.Contains("."))
            {
                return value;
            }
            if (value.StartsWith("."))
            {
                // Cover leading decimal place
                value = "0" + value;
            }
            return value.TrimEnd('0').TrimEnd('.');
        }

        internal static string FromSerializedString(string value)
        {
            // Purpose: Convert a string with escaped characters into control codes
            // Author : Scott Bakker
            // Created: 09/17/2019
            // LastMod: 08/11/2020
            if (value == null)
            {
                return null;
            }
            if (!value.Contains("\\"))
            {
                return value;
            }
            StringBuilder result = new StringBuilder();
            bool lastBackslash = false;
            int unicodeCharCount = 0;
            string unicodeValue = "";
            foreach (char c in value)
            {
                if (unicodeCharCount > 0)
                {
                    unicodeValue += c;
                    unicodeCharCount--;
                    if (unicodeCharCount == 0)
                    {
                        result.Append(Convert.ToChar(Convert.ToUInt16(unicodeValue, 16)));
                        unicodeValue = "";
                    }
                }
                else if (lastBackslash)
                {
                    switch (c)
                    {
                        case '\"':
                            result.Append('\"');
                            break;
                        case '\\':
                            result.Append('\\');
                            break;
                        case '/':
                            result.Append('/');
                            break;
                        case 'r':
                            result.Append('\r');
                            break;
                        case 'n':
                            result.Append('\n');
                            break;
                        case 't':
                            result.Append('\t');
                            break;
                        case 'b':
                            result.Append('\b');
                            break;
                        case 'f':
                            result.Append('\f');
                            break;
                        case 'u':
                            unicodeCharCount = 4;
                            unicodeValue = "";
                            break;
                        default:
                            throw new SystemException($"JSON Error: Unexpected escaped char: {c}");
                    }
                    lastBackslash = false;
                }
                else if (c == '\\')
                {
                    lastBackslash = true;
                }
                else
                {
                    result.Append(c);
                }
            }
            if (lastBackslash)
            {
                throw new SystemException("JSON Error: Unexpected trailing backslash");
            }
            return result.ToString();
        }

        internal static string GetToken(CharReader reader)
        {
            // Purpose: Get a single token from string value for parsing
            // Author : Scott Bakker
            // Created: 09/13/2019
            // LastMod: 04/17/2020
            // Notes  : Does not do escaped character expansion here, just passes exact value.
            //        : Properly handles \" within strings this way, but nothing else.
            if (reader == null || reader.Peek() == -1)
            {
                return null;
            }
            char c;
            // Ignore whitespace before token
            SkipWhitespace(reader);
            // Stop if one-character JSON symbol found
            if (IsJsonSymbol((char)reader.Peek()))
            {
                return ((char)reader.Read()).ToString();
            }
            // Have to build token char by char
            StringBuilder result = new StringBuilder();
            bool inQuote = false;
            bool lastBackslash = false;
            do
            {
                // Peek char for this loop
                c = (char)reader.Peek();
                // Check for whitespace or symbols to end token
                if (!inQuote)
                {
                    if (IsWhitespace(c))
                    {
                        reader.Read(); // gobble char
                        break; // end token
                    }
                    if (IsJsonSymbol(c))
                    {
                        // don't gobble char
                        break; // end token
                    }
                    // Any comments end the token
                    if (c == '/')
                    {
                        if (reader.PeekNext() == '*' || reader.PeekNext() == '/')
                        {
                            // don't gobble char
                            break; // end token
                        }
                    }
                    if (c != '\"' && !IsJsonValueChar(c))
                    {
                        throw new SystemException($"JSON Error: Unexpected character: {c}");
                    }
                }
                // Check for escaped chars
                if (inQuote && lastBackslash)
                {
                    // Add backslash and character, no expansion here
                    result.Append('\\');
                    result.Append((char)reader.Read());
                    lastBackslash = false;
                }
                else if (inQuote && c == '\\')
                {
                    // Remember backslash for next loop, but don't add it to result
                    lastBackslash = true;
                }
                else if (c == '\"')
                {
                    // Check for quotes around a string
                    if (inQuote)
                    {
                        result.Append((char)reader.Read()); // add ending quote
                        inQuote = false;
                        break; // Token is done
                    }
                    if (result.Length > 0)
                    {
                        // Quote in the middle of a token?
                        throw new SystemException("JSON Error: Unexpected quote char");
                    }
                    result.Append((char)reader.Read()); // add beginning quote
                    inQuote = true;
                }
                else
                {
                    // Add this char
                    result.Append((char)reader.Read());
                }
            }
            while (reader.Peek() != -1);
            return result.ToString();
        }

        internal static object JsonValueToObject(string value)
        {
            // Purpose: Convert a string representation of a value to an actual object
            // Author : Scott Bakker
            // Created: 09/13/2019
            // LastMod: 08/11/2020
            if (value == null || value.Length == 0)
            {
                return null;
            }
            try
            {
                if (value.StartsWith("\"", StringComparison.Ordinal) &&
                    value.EndsWith("\"", StringComparison.Ordinal))
                {
                    value = value.Substring(1, value.Length - 2); // remove quotes
                    if (IsTimeSpanValue(value))
                    {
                        return TimeSpan.Parse(value);
                    }
                    if (IsDateTimeOffsetValue(value))
                    {
                        return DateTimeOffset.Parse(value);
                    }
                    if (IsDateTimeValue(value))
                    {
                        return DateTime.Parse(value);
                    }
                    // Parse all escaped sequences to chars
                    return FromSerializedString(value);
                }
                if (value == "null")
                {
                    return null;
                }
                if (value == "true")
                {
                    return true;
                }
                if (value == "false")
                {
                    return false;
                }
                // must be numeric
                if (value.Contains("e") || value.Contains("E"))
                {
                    return double.Parse(value);
                }
                if (value.Contains("."))
                {
                    return decimal.Parse(value);
                }
                if (long.Parse(value) > int.MaxValue || long.Parse(value) < int.MinValue)
                {
                    return long.Parse(value);
                }
                return int.Parse(value);
            }
            catch (Exception ex)
            {
                throw new SystemException(
                    $"JSON Error: Value not recognized: {value}\r\n{ex.Message}");
            }
        }

        internal static void SkipBOM(CharReader reader)
        {
            // Purpose: Skip over Byte-Order Mark (BOM) at the beginning of a stream
            // Author : Scott Bakker
            // Created: 05/20/2020
            // LastMod: 08/11/2020
            // UTF-8 BOM = 0xEF,0xBB,0xBF = 239,187,191
            if (reader.Peek() == '\xEF' && reader.PeekNext() == '\xBB')
            {
                reader.Read();
                reader.Read();
                if (reader.Peek() != '\xBF')
                {
                    throw new SystemException($"JSON Error: Invalid BOM character: 0x{reader.Peek():X2}");
                }
                reader.Read();
            }
        }

        internal static void SkipWhitespace(CharReader reader)
        {
            // Purpose: Skip over any whitespace characters or any recognized comments
            // Author : Scott Bakker
            // Created: 09/23/2019
            // LastMod: 08/11/2020
            // Notes  : Comments consist of "/*...*/" or "//" to eol (aka line comment).
            //        : "//" comments don't need an eol if at the end, but "/*" does need "*/".
            if (reader == null)
            {
                return;
            }
            bool inComment = false;
            bool inLineComment = false;
            while (reader.Peek() != -1)
            {
                if (inComment)
                {
                    if (reader.Peek() == '*' && reader.PeekNext() == '/') // found ending "*/"
                    {
                        inComment = false;
                        reader.Read();
                    }
                    reader.Read();
                    continue;
                }
                if (inLineComment)
                {
                    if (reader.Peek() == '\r' || reader.Peek() == '\n') // found end of line
                    {
                        inLineComment = false;
                    }
                    reader.Read();
                    continue;
                }
                if (reader.Peek() == '/' && reader.PeekNext() == '*')
                {
                    inComment = true;
                    reader.Read();
                    reader.Read();
                    continue;
                }
                if (reader.Peek() == '/' && reader.PeekNext() == '/')
                {
                    inLineComment = true;
                    reader.Read();
                    reader.Read();
                    continue;
                }
                if (IsWhitespace((char)reader.Peek()))
                {
                    reader.Read();
                    continue;
                }
                break;
            }
            if (inComment)
            {
                throw new SystemException("JSON Error: Comment starting with /* is not terminated by */");
            }
        }

        internal static bool IsWhitespaceString(string value)
        {
            // Purpose: Determine if a string contains only whitespace
            // Author : Scott Bakker
            // Created: 02/12/2020
            // LastMod: 04/06/2020
            if (value == null)
            {
                return true;
            }
            if (value.Length == 0)
            {
                return true;
            }
            foreach (char c in value)
            {
                if (!IsWhitespace(c))
                {
                    return false;
                }
            }
            return true;
        }

        #endregion

        #region private routines

        private static string ToJsonChar(char c)
        {
            // Purpose: Return a character in proper JSON format
            // Author : Scott Bakker
            // Created: 09/13/2019
            if (c == '\\') return "\\\\";
            if (c == '\"') return "\\\"";
            if (c == '\r') return "\\r";
            if (c == '\n') return "\\n";
            if (c == '\t') return "\\t";
            if (c == '\b') return "\\b";
            if (c == '\f') return "\\f";
            if (c < 32 || c >= 127)
            {
                return "\\u" + ((int)c).ToString("x4", null); // always lowercase
            }
            return c.ToString();
        }

        private static bool IsWhitespace(char c)
        {
            // Purpose: Check for recognized whitespace characters
            // Author : Scott Bakker
            // Created: 09/13/2019
            // LastMod: 05/21/2020
            if (c == ' ') return true;
            if (c == '\r') return true;
            if (c == '\n') return true;
            if (c == '\t') return true;
            return false;
        }

        private static bool IsJsonSymbol(char c)
        {
            // Purpose: Check for recognized JSON symbol chars which are tokens by themselves
            // Author : Scott Bakker
            // Created: 09/13/2019
            if (c == '{') return true;
            if (c == '}') return true;
            if (c == '[') return true;
            if (c == ']') return true;
            if (c == ':') return true;
            if (c == ',') return true;
            return false;
        }

        private static bool IsJsonValueChar(char c)
        {
            // Purpose: Check for any valid characters in a non-string value
            // Author : Scott Bakker
            // Created: 09/23/2019
            switch (c)
            {
                case 'n': // null
                case 'u':
                case 'l':
                case 't': // true
                case 'r':
                case 'e':
                case 'f': // false
                case 'a':
                case 's':
                case '0': // numeric
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                case '-':
                case '+':
                case '.':
                case 'E': // also 'e' checked for above
                    return true;
            }
            return false;
        }

        private static bool IsTimeSpanValue(string value)
        {
            // Purpose: Determine if value converts to a Time without a Date
            // Author : Scott Bakker
            // Created: 04/06/2020
            // LastMod: 08/16/2020
            if (value == null || value.Length == 0) return false;
            if (!value.Contains(":")) return false;
            if (value.Contains("/")) return false;
            if (value.Substring(1).Contains("-")) return false; // allowed as first char
            return TimeSpan.TryParse(value, out _);
        }

        private static bool IsDateTimeValue(string value)
        {
            // Purpose: Determine if value converts to a DateTime
            // Author : Scott Bakker
            // Created: 02/19/2020
            // LastMod: 08/16/2020
            if (value == null || value.Length == 0) return false;
            return DateTime.TryParse(value, out _);
        }

        private static bool IsDateTimeOffsetValue(string value)
        {
            // Purpose: Determine if value converts to a DateTimeOffset
            // Author : Scott Bakker
            // Created: 02/19/2020
            // LastMod: 08/16/2020
            if (value == null || value.Length == 0) return false;
            // The "T" in the 11th position is used to indicate DateTimeOffset
            if (value.Length < 11 || value[10] != 'T')
            {
                return false;
            }
            return DateTimeOffset.TryParse(value, out _);
        }

        #endregion
    }
}
