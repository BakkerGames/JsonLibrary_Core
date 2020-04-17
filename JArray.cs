// Purpose: Provide a JSON Array class
// Author : Scott Bakker
// Created: 09/13/2019
// LastMod: 04/17/2020

// Notes  : The values in the list ARE ordered based on when they are added.
//          The values are NOT sorted, and there can be duplicates.
//        : The function ToStringFormatted() will return a string representation with
//          whitespace added. Two spaces are used for indenting, and CRLF between lines.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace JsonLibrary
{
    public class JArray : IEnumerable<object>
    {
        private List<object> _data;

        public JArray()
        {
            // Purpose: Create new JArray object
            // Author : Scott Bakker
            // Created: 09/13/2019
            _data = new List<object>();
        }

        public JArray(JArray ja)
        {
            // Purpose: Create new JArray object from an existing JArray
            // Author : Scott Bakker
            // Created: 09/13/2019
            _data = new List<object>();
            this.Append(ja);
        }

        public IEnumerator<object> GetEnumerator()
        {
            // Purpose: Provide IEnumerable access directly to _data
            // Author : Scott Bakker
            // Created: 09/13/2019
            return _data.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            // Purpose: Provide IEnumerable access directly to _data
            // Author : Scott Bakker
            // Created: 09/13/2019
            return _data.GetEnumerator();
        }

        public void Add(object value)
        {
            // Purpose: Adds a new value to the end of the JArray list
            // Author : Scott Bakker
            // Created: 09/13/2019
            // Changes: 10/03/2019 Removed extra string processing, was wrong
            _data.Add(value);
        }

        public void Append(IEnumerable list)
        {
            // Purpose: Append all values in the sent IEnumerable at the end of the JArray list
            // Author : Scott Bakker
            // Created: 09/13/2019
            // LastMod: 04/06/2020
            if (list != null)
            {
                foreach (object obj in list)
                {
                    _data.Add(obj);
                }
            }
        }

        public int Count()
        {
            // Purpose: Return the count of items in the JArray
            // Author : Scott Bakker
            // Created: 09/13/2019
            return _data.Count;
        }

        public object this[int index]
        {
            // Purpose: Give access to item values by index
            // Author : Scott Bakker
            // Created: 09/13/2019
            get
            {
                if (index < 0 || index >= _data.Count)
                {
                    throw new ArgumentOutOfRangeException();
                }
                return _data[index];
            }
            set
            {
                if (index < 0 || index >= _data.Count)
                {
                    throw new ArgumentOutOfRangeException();
                }
                if (value != null)
                {
                    if (value.GetType() == string.Empty.GetType())
                    {
                        value = JsonRoutines.FromJsonString(value.ToString());
                    }
                }
                _data[index] = value;
            }
        }

        public List<object> Items()
        {
            // Purpose: Get cloned list of all objects
            // Author : Scott Bakker
            // Created: 09/13/2019
            return new List<object>(_data);
        }

        public void RemoveAt(int index)
        {
            // Purpose: Remove the item at the specified index
            // Author : Scott Bakker
            // Created: 09/13/2019
            if (index < 0 || index >= _data.Count)
            {
                throw new ArgumentOutOfRangeException();
            }
            _data.RemoveAt(index);
        }

        #region ToString

        public override string ToString()
        {
            // Purpose: Convert this JArray into a string with no formatting
            // Author : Scott Bakker
            // Created: 09/13/2019
            // Notes  : This could be implemented as ToStringFormatted(-1) but
            //          it is separate to get better performance.
            StringBuilder result = new StringBuilder();
            result.Append("[");
            bool addComma = false;
            foreach (object obj in _data)
            {
                if (addComma)
                {
                    result.Append(",");
                }
                else
                {
                    addComma = true;
                }
                result.Append(JsonRoutines.ValueToString(obj));
            }
            result.Append("]");
            return result.ToString();
        }

        public string ToStringFormatted()
        {
            // Purpose: Convert this JArray into a string with formatting
            // Author : Scott Bakker
            // Created: 10/17/2019
            int indentLevel = 0;
            return ToStringFormatted(ref indentLevel);
        }

        internal string ToStringFormatted(ref int indentLevel)
        {
            // Purpose: Convert this JArray into a string with formatting
            // Author : Scott Bakker
            // Created: 10/17/2019
            if (_data.Count == 0)
            {
                return "[]"; // avoid indent errors
            }
            StringBuilder result = new StringBuilder();
            result.Append("[");
            if (indentLevel >= 0)
            {
                indentLevel++;
                result.AppendLine();
            }
            bool addComma = false;
            foreach (object obj in _data)
            {
                if (addComma)
                {
                    result.Append(",");
                    if (indentLevel >= 0)
                    {
                        result.AppendLine();
                    }
                }
                else
                {
                    addComma = true;
                }
                if (indentLevel > 0)
                {
                    result.Append(JsonRoutines.IndentSpace(indentLevel));
                }
                result.Append(JsonRoutines.ValueToString(obj, ref indentLevel));
            }
            if (indentLevel >= 0)
            {
                result.AppendLine();
                if (indentLevel > 0)
                {
                    indentLevel--;
                }
                result.Append(JsonRoutines.IndentSpace(indentLevel));
            }
            result.Append("]");
            return result.ToString();
        }

        #endregion

        #region Parse

        public static JArray Parse(string value)
        {
            // Purpose: Convert a string into a JArray
            // Author : Scott Bakker
            // Created: 09/13/2019
            // LastMod: 04/17/2020
            return Parse(new CharReader(value));
        }

        public static JArray Parse(TextReader textReader)
        {
            // Purpose: Convert a TextReader stream into a JArray
            // Author : Scott Bakker
            // Created: 04/17/2020
            return Parse(new CharReader(textReader));
        }

        internal static JArray Parse(CharReader reader)
        {
            // Purpose: Convert a partial string into a JArray
            // Author : Scott Bakker
            // Created: 09/13/2019
            // LastMod: 04/17/2020
            if (reader == null || reader.Peek() == -1)
            {
                return null;
            }
            JArray result = new JArray();
            JsonRoutines.SkipWhitespace(reader);
            if (reader.Peek() != '[')
            {
                throw new SystemException($"JSON Error: Unexpected token to start JArray: {reader.Peek()}");
            }
            reader.Read();
            do
            {
                JsonRoutines.SkipWhitespace(reader);
                // check for symbols
                if (reader.Peek() == ']')
                {
                    reader.Read();
                    break; // done building JArray
                }
                if (reader.Peek() == ',')
                {
                    // this logic ignores extra commas, but is ok
                    reader.Read();
                    continue; // next value
                }
                if (reader.Peek() == '{') // JObject
                {
                    JObject jo = JObject.Parse(reader);
                    result.Add(jo);
                    continue;
                }
                if (reader.Peek() == '[') // JArray
                {
                    JArray ja = JArray.Parse(reader);
                    result.Add(ja);
                    continue;
                }
                // Get value as a string, convert to object
                string tempValue = JsonRoutines.GetToken(reader);
                result.Add(JsonRoutines.JsonValueToObject(tempValue));
            } while (true);
            return result;
        }

        #endregion

        #region Clone

        public static JArray Clone(JArray ja)
        {
            // Purpose: Clones a JArray
            // Author : Scott Bakker
            // Created: 09/20/2019
            JArray result = new JArray();
            if (ja != null && ja._data != null)
            {
                result._data = new List<object>(ja._data);
            }
            return result;
        }

        #endregion
    }
}
