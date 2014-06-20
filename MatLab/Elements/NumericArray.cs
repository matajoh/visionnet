using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace VisionNET.MatLab.Elements
{
    /// <summary>
    /// A multi-dimensional array of numbers.
    /// </summary>
    /// <typeparam name="T">The underlying type of the multi-dimensional array</typeparam>
    public class NumericArray<T>
    {
        private int[] _dimensions;
        private int[] _lengths;
        private T[] _data;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="dimensions">The dimensionality vector, where each index indicates the size in that dimension</param>
        protected NumericArray(int[] dimensions)
        {
            _dimensions = dimensions;
            _lengths = new int[_dimensions.Length];
            _lengths[0] = 1;
            for (int i = 1; i < _dimensions.Length; i++)
                _lengths[i] = _dimensions[i - 1] * _lengths[i - 1];
            _data = new T[(int)_dimensions.Product(o=>o)];
        }

        /// <summary>
        /// Returns the value at the specified index.
        /// </summary>
        /// <param name="index">The index vector</param>
        /// <returns>The value at the index</returns>
        public T this[params int[] index]
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

        /// <summary>
        /// Copies the values in the value type into the array.
        /// </summary>
        /// <typeparam name="D">The type of the source value type</typeparam>
        /// <param name="values">The values to copy</param>
        protected void copy<D>(ValueType<D> values)
        {
            Array.Copy(values, _data, _data.Length);
        }

        /// <summary>
        /// Returns a string representation of the numeric array.
        /// </summary>
        /// <returns>The string representation of the array</returns>
        public override string ToString()
        {
            return string.Format("{{Size={0},Type={1}}}", _data.Length, typeof(T));
        }
    }

    /// <summary>
    /// Class representing an array of double-precision floating point numbers.
    /// </summary>
    public class miDOUBLEArray : NumericArray<double>
    {
        internal miDOUBLEArray(int[] dimensions, DataElement element) :
            base(dimensions)
        {
            switch (element.DataType)
            {
                case DataType.miINT8:
                    copy(element as miINT8);
                    break;

                case DataType.miUINT8:
                    copy(element as miUINT8);
                    break;

                case DataType.miINT16:
                    copy(element as miINT16);
                    break;

                case DataType.miUINT16:
                    copy(element as miUINT16);
                    break;

                case DataType.miINT32:
                    copy(element as miINT32);
                    break;

                case DataType.miUINT32:
                    copy(element as miUINT32);
                    break;

                case DataType.miINT64:
                    copy(element as miINT64);
                    break;

                case DataType.miUINT64:
                    copy(element as miUINT64);
                    break;

                case DataType.miSINGLE:
                    copy(element as miSINGLE);
                    break;

                case DataType.miDOUBLE:
                    copy(element as miDOUBLE);
                    break;

                default:
                    throw new ArgumentException(string.Format("Cannot load {0} data into a miDOUBLE array", element.DataType));
            }
        }
    }

    /// <summary>
    /// Class representing an array of single-precision floating point numbers.
    /// </summary>
    public class miSINGLEArray : NumericArray<float>
    {
        internal miSINGLEArray(int[] dimensions, DataElement element) :
            base(dimensions)
        {
            switch (element.DataType)
            {
                case DataType.miINT8:
                    copy(element as miINT8);
                    break;

                case DataType.miUINT8:
                    copy(element as miUINT8);
                    break;

                case DataType.miINT16:
                    copy(element as miINT16);
                    break;

                case DataType.miUINT16:
                    copy(element as miUINT16);
                    break;

                case DataType.miINT32:
                    copy(element as miINT32);
                    break;

                case DataType.miUINT32:
                    copy(element as miUINT32);
                    break;

                case DataType.miINT64:
                    copy(element as miINT64);
                    break;

                case DataType.miUINT64:
                    copy(element as miUINT64);
                    break;

                case DataType.miSINGLE:
                    copy(element as miSINGLE);
                    break;

                default:
                    throw new ArgumentException(string.Format("Cannot load {0} data into a miFLOAT array", element.DataType));
            }
        }
    }

    /// <summary>
    /// Class representing an array of unsigned 32-bit integers.
    /// </summary>
    public class miUINT32Array : NumericArray<uint>
    {
        internal miUINT32Array(int[] dimensions, DataElement element) :
            base(dimensions)
        {
            switch (element.DataType)
            {
                case DataType.miUINT8:
                    copy(element as miUINT8);
                    break;

                case DataType.miUINT16:
                    copy(element as miUINT16);
                    break;

                case DataType.miUINT32:
                    copy(element as miUINT32);
                    break;

                default:
                    throw new ArgumentException(string.Format("Cannot load {0} data into a miUINT32 array", element.DataType));
            }
        }
    }

    /// <summary>
    /// Class representing an array of 32-bit integers.
    /// </summary>
    public class miINT32Array : NumericArray<int>
    {
        internal miINT32Array(int[] dimensions, DataElement element) :
            base(dimensions)
        {
            switch (element.DataType)
            {
                case DataType.miINT8:
                    copy(element as miINT8);
                    break;

                case DataType.miUINT8:
                    copy(element as miUINT8);
                    break;

                case DataType.miINT16:
                    copy(element as miINT16);
                    break;

                case DataType.miUINT16:
                    copy(element as miUINT16);
                    break;

                case DataType.miINT32:
                    copy(element as miINT32);
                    break;

                default:
                    throw new ArgumentException(string.Format("Cannot load {0} data into a miINT32 array", element.DataType));
            }
        }
    }

    /// <summary>
    /// Class representing an array of unsigned 16-bit integers.
    /// </summary>
    public class miUINT16Array : NumericArray<ushort>
    {
        internal miUINT16Array(int[] dimensions, DataElement element) :
            base(dimensions)
        {
            switch (element.DataType)
            {
                case DataType.miUINT8:
                    copy(element as miUINT8);
                    break;

                case DataType.miUINT16:
                    copy(element as miUINT16);
                    break;

                default:
                    throw new ArgumentException(string.Format("Cannot load {0} data into a miUINT16 array", element.DataType));
            }
        }
    }

    /// <summary>
    /// Class representing an array of 16-bit integers.
    /// </summary>
    public class miINT16Array : NumericArray<short>
    {
        internal miINT16Array(int[] dimensions, DataElement element) :
            base(dimensions)
        {
            switch (element.DataType)
            {
                case DataType.miINT8:
                    copy(element as miINT8);
                    break;

                case DataType.miUINT8:
                    copy(element as miUINT8);
                    break;

                case DataType.miINT16:
                    copy(element as miINT16);
                    break;

                default:
                    throw new ArgumentException(string.Format("Cannot load {0} data into a miINT16 array", element.DataType));
            }
        }
    }

    /// <summary>
    /// Class representing an array of unsigned 8-bit integers.
    /// </summary>
    public class miUINT8Array : NumericArray<byte>
    {
        internal miUINT8Array(int[] dimensions, DataElement element) :
            base(dimensions)
        {
            switch (element.DataType)
            {
                case DataType.miUINT8:
                    copy(element as miUINT8);
                    break;

                default:
                    throw new ArgumentException(string.Format("Cannot load {0} data into a miUINT8 array", element.DataType));
            }
        }
    }

    /// <summary>
    /// Class representing an array of signed 8-bit integers.
    /// </summary>
    public class miINT8Array : NumericArray<sbyte>
    {
        internal miINT8Array(int[] dimensions, DataElement element) :
            base(dimensions)
        {
            switch (element.DataType)
            {
                case DataType.miINT8:
                    copy(element as miINT8);
                    break;

                default:
                    throw new ArgumentException(string.Format("Cannot load {0} data into a miINT8 array", element.DataType));
            }
        }
    }
}
