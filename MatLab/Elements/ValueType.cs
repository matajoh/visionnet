using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace VisionNET.MatLab.Elements
{
    /// <summary>
    /// A value type element, containing one or more values in an array which are the same underlying type.
    /// </summary>
    /// <typeparam name="T">The underlying type of the value type</typeparam>
    public class ValueType<T> : DataElement
    {
        private Func<byte[], T> _convert;
        private int _size;

        private T[] _values;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="convert">A method to convert a byte array to the underlying type</param>
        /// <param name="size">The size of underlying type in bytes</param>
        protected ValueType(Func<byte[], T> convert, int size)
        {
            _convert = convert;
            _size = size;
        }

        /// <summary>
        /// Initialize the value type element from the stream.
        /// </summary>
        /// <param name="stream">Stream from which to read</param>
        /// <param name="numBytes">The total number of bytes to read from the stream and into this element</param>
        /// <param name="reverseBytes">Whether to reverse the bytes read from the stream before converting</param>
        /// <param name="compressed">Whether the underlying format is compressed</param>
        protected override void Init(Stream stream, int numBytes, bool reverseBytes, bool compressed)
        {
            _values = new T[numBytes / _size];
            byte[] buff = new byte[_size];
            for (int i = 0; i < _values.Length; i++)
            {
                stream.Read(buff, reverseBytes);
                _values[i] = _convert(buff);
            }            
            int padding = numBytes % 8;
            if (compressed)
                padding = (padding + 4) % 8; 
            if (padding != 0)
            {
                if (padding != 0)
                {
                    padding = 8 - padding;
                    buff = new byte[padding];
                    stream.Read(buff, 0, padding);
                }
            }
        }

        /// <summary>
        /// Number of items in the element
        /// </summary>
        public int Length
        {
            get
            {
                return _values.Length;
            }
        }

        /// <summary>
        /// The value at the provided index
        /// </summary>
        /// <param name="index">Location of the value</param>
        /// <returns>The value</returns>
        public T this[int index]
        {
            get
            {
                return _values[index];
            }
        }

        /// <summary>
        /// Converts the value type to an array.
        /// </summary>
        /// <param name="value">The value type to convert</param>
        /// <returns>A basic array</returns>
        public static implicit operator T[](ValueType<T> value)
        {
            return value._values;
        }
    }

    /// <summary>
    /// Class representing a value type of signed bytes.
    /// </summary>
    public class miINT8 : ValueType<sbyte>
    {
        internal miINT8() :
            base(buff => (sbyte)buff[0], 1)
        {
        }

        /// <summary>
        /// Converts this element to an unsigned byte array.
        /// </summary>
        /// <returns>An unsigned byte array</returns>
        public byte[] ToByteArray()
        {
            byte[] values = new byte[Length];
            for (int i = 0; i < values.Length; i++)
                values[i] = (byte)this[i];
            return values;
        }
    }

    /// <summary>
    /// Class representing a value type of unsigned bytes.
    /// </summary>
    public class miUINT8 : ValueType<byte>
    {
        internal miUINT8() :
            base(buff => buff[0], 1)
        {
        }
    }

    /// <summary>
    /// Class representing a value type of signed 16-bit integers.
    /// </summary>
    public class miINT32 : ValueType<int>
    {
        internal miINT32() :
            base(buff => BitConverter.ToInt32(buff, 0), 4)
        {
        }
    }

    /// <summary>
    /// Class representing a value type of unsigned 16-bit integers.
    /// </summary>
    public class miUINT32 : ValueType<uint>
    {
        internal miUINT32() :
            base(buff => BitConverter.ToUInt32(buff, 0), 4)
        {
        }
    }

    /// <summary>
    /// Class representing a value type of signed 32-bit integers.
    /// </summary>
    public class miINT16 : ValueType<short>
    {
        internal miINT16() :
            base(buff => BitConverter.ToInt16(buff, 0), 2)
        {
        }
    }

    /// <summary>
    /// Class representing a value type of unsigned 32-bit integers.
    /// </summary>
    public class miUINT16 : ValueType<ushort>
    {
        internal miUINT16() :
            base(buff => BitConverter.ToUInt16(buff, 0), 2)
        {
        }
    }

    /// <summary>
    /// Class representing a value type of signed 64-bit integers.
    /// </summary>
    public class miINT64 : ValueType<long>
    {
        internal miINT64() :
            base(buff => BitConverter.ToInt64(buff, 0), 8)
        {
        }
    }

    /// <summary>
    /// Class representing a value type of unsigned 64-bit integers.
    /// </summary>
    public class miUINT64 : ValueType<ulong>
    {
        internal miUINT64() :
            base(buff => BitConverter.ToUInt64(buff, 0), 8)
        {
        }
    }

    /// <summary>
    /// Class representing a value type of single-precision floating point numbers.
    /// </summary>
    public class miSINGLE : ValueType<float>
    {
        internal miSINGLE() :
            base(buff => BitConverter.ToSingle(buff,0), 4)
        {
        }
    }

    /// <summary>
    /// Class representing a value type of double-precision floating point numbers.
    /// </summary>
    public class miDOUBLE : ValueType<double>
    {
        internal miDOUBLE() :
            base(buff => BitConverter.ToDouble(buff,0), 8)
        {
        }
    }
}
