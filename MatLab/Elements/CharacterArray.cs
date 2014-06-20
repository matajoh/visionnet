using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VisionNET.MatLab.Elements
{
    /// <summary>
    /// Class representing an array of signed bytes (ASCII characters)
    /// </summary>
    public class CharacterArray
    {
        private int[] _dimensions;
        private int[] _lengths;
        private char[] _data;

        internal CharacterArray(int[] dimensions, miINT8 characters)
        {
            _dimensions = dimensions;
            _lengths = new int[_dimensions.Length];
            _lengths[_dimensions.Length - 1] = 1;
            for (int i = _dimensions.Length - 2; i >= 0; i--)
                _lengths[i] = _dimensions[i] * _lengths[i + 1];
            _data = new char[(int)_dimensions.Product(o=>o)];
            Array.Copy(characters, _data, _data.Length);
        }

        /// <summary>
        /// The value at the provided index vector.
        /// </summary>
        /// <param name="index">The index of the value</param>
        /// <returns>The value</returns>
        public char this[params int[] index]
        {
            get
            {
                if(index.Length != _dimensions.Length)
                    throw new ArgumentException(string.Format("Number of indices must be equal to the dimensionality of the array ({0} != {1})", index.Length, _dimensions.Length));
                int rawIndex = 0;
                for (int i = 0; i < index.Length; i++)
                    rawIndex += index[i] * _lengths[i];
                return _data[rawIndex];
            }
        }
    }
}
