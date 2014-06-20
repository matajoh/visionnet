using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using VisionNET.MatLab.Elements;
using Ionic.Zlib;

namespace VisionNET.MatLab
{
    /// <summary>
    /// Abstract class encapsulating the shared characteristics of all MAT5 data elements.
    /// </summary>
    public abstract class DataElement
    {
        /// <summary>
        /// The type of this element
        /// </summary>
        public DataType DataType { get; set; }

        /// <summary>
        /// Initializes the data element from the stream
        /// </summary>
        /// <param name="stream">Stream to read from</param>
        /// <param name="numBytes">The total number of bytes to read</param>
        /// <param name="reverseBytes">Whether to reverse bytes before they are converted</param>
        /// <param name="compressed">Whether the element is compressed</param>
        protected abstract void Init(Stream stream, int numBytes, bool reverseBytes, bool compressed);

        /// <summary>
        /// Read a data element from the stream.
        /// </summary>
        /// <param name="stream">The stream to read from</param>
        /// <param name="reverseBytes">Whether to reverse bytes before they are converted</param>
        /// <returns>A data element</returns>
        public static DataElement Read(Stream stream, bool reverseBytes)
        {
            byte[] buff = new byte[4];
            stream.Read(buff, reverseBytes);
            int dataType = BitConverter.ToInt32(buff, 0);
            int numBytes;
            bool compressed = false;
            if ((dataType & 0xFFFF0000) != 0)
            {
                compressed = true;
                numBytes = dataType >> 16;
                dataType = dataType & 0xFFFF;
            }
            else
            {
                stream.Read(buff, reverseBytes);
                numBytes = BitConverter.ToInt32(buff, 0);
            }

            DataElement result = null;
            switch ((DataType)dataType)
            {
                case DataType.miINT8:
                    result = new miINT8();
                    break;

                case DataType.miUINT8:
                    result = new miUINT8();
                    break;

                case DataType.miINT16:
                    result = new miINT16();
                    break;

                case DataType.miUINT16:
                    result= new miUINT16();
                    break;

                case DataType.miINT32:
                    result = new miINT32();
                    break;

                case DataType.miUINT32:
                    result = new miUINT32();
                    break;

                case DataType.miINT64:
                    result = new miUINT64();
                    break;

                case DataType.miUINT64:
                    result = new miUINT64();
                    break;

                case DataType.miSINGLE:
                    result = new miSINGLE();
                    break;

                case DataType.miDOUBLE:
                    result = new miDOUBLE();
                    break;

                case DataType.miCOMPRESSED:
                    buff = new byte[numBytes];
                    stream.Read(buff, 0, numBytes);
                    using (MemoryStream compressedStream = new MemoryStream(buff))
                    {
                        using (ZlibStream zipStream = new ZlibStream(compressedStream, CompressionMode.Decompress))
                        {
                            return DataElement.Read(zipStream, reverseBytes);
                        }
                    }
                    
                case DataType.miMATRIX:
                    result = new miMATRIX();
                    break;

                default:
                    throw new NotSupportedException("Implementation does not support the " + (DataType)dataType + " data element");
            }
            result.DataType = (DataType)dataType;
            result.Init(stream, numBytes, reverseBytes, compressed);
            return result;
        }
    }
}
