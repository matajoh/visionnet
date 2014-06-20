using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace VisionNET.MatLab.Elements
{
    /// <summary>
    /// A key/value collection of miMATRIX elements.
    /// </summary>
    public class StructArray
    {
        private Dictionary<string, miMATRIX> _values;

        internal StructArray(int[] dimensions, Stream stream, bool swapBytes)
        {
            _values = new Dictionary<string, miMATRIX>();
            miINT32 fieldNameLength = DataElement.Read(stream, swapBytes) as miINT32;

            int length = (int)dimensions.Sum(o=>o);
            string[] fieldNames = new string[length];
            miINT8 fieldNamesElement = DataElement.Read(stream, swapBytes) as miINT8;
            byte[] nameBytes = fieldNamesElement.ToByteArray();
            for (int i = 0; i < length; i++)
                fieldNames[i] = Encoding.ASCII.GetString(nameBytes, i * fieldNameLength[0], fieldNameLength[0]).TrimEnd('\0');

            for (int i = 0; i < length; i++)
                _values[fieldNames[i]] = DataElement.Read(stream, swapBytes) as miMATRIX;
        }

        /// <summary>
        /// The miMATRIX at the provided key
        /// </summary>
        /// <param name="key">The key identifying the miMATRIX</param>
        /// <returns>The value for the key</returns>
        public miMATRIX this[string key]
        {
            get
            {
                return _values[key];
            }
        }

        /// <summary>
        /// The identifying keys in this struct array.
        /// </summary>
        public List<string> Keys
        {
            get
            {
                return _values.Keys.ToList();
            }
        }

        /// <summary>
        /// Returns a string representation of the struct array.
        /// </summary>
        /// <returns>The string representation</returns>
        public override string ToString()
        {            
            return string.Format("{{{0}}}", string.Join(",", _values.Select(o => string.Format("{0}:{1}", o.Key, o.Value))));
        }
    }
}
