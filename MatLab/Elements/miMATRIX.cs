using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace VisionNET.MatLab.Elements
{
    /// <summary>
    /// Class representing a MatLab matrix.
    /// </summary>
    public class miMATRIX : DataElement
    {
        /// <summary>
        /// Whether the values in the matrix are complex.
        /// </summary>
        public bool Complex { get; set; }
        /// <summary>
        /// N/A
        /// </summary>
        public bool Global { get; set; }
        /// <summary>
        /// N/A
        /// </summary>
        public bool Logical { get; set; }
        /// <summary>
        /// The type of array represented in this matrix
        /// </summary>
        public ArrayType Class { get; set; }
        /// <summary>
        /// The dimensionality vector
        /// </summary>
        public int[] Dimensions { get; set; }
        /// <summary>
        /// The name of the matrix
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Cells (if this is a Cell matrix)
        /// </summary>
        public miMATRIX[] Cells { get; set; }
        /// <summary>
        /// Array object (if this is an Array matrix)
        /// </summary>
        public object Array { get; set; }

        internal miMATRIX()
        {
        }

        /// <summary>
        /// Initializes the data element from the stream
        /// </summary>
        /// <param name="stream">Stream to read from</param>
        /// <param name="numBytes">The total number of bytes to read</param>
        /// <param name="reverseBytes">Whether to reverse bytes before they are converted</param>
        /// <param name="compressed">Whether the element is compressed</param>
        protected override void Init(Stream stream, int numBytes, bool reverseBytes, bool compressed)
        {
            miUINT32 arrayFlags = DataElement.Read(stream, reverseBytes) as miUINT32;
            miINT32 dimensionsArray = DataElement.Read(stream, reverseBytes) as miINT32;
            miINT8 arrayName = DataElement.Read(stream, reverseBytes) as miINT8;

            uint flags = (0xFF00 & arrayFlags[0]) >> 8;
            Complex = (flags & 0x04) > 0;
            Global = (flags & 0x02) > 0;
            Logical = (flags & 0x01) > 0;
            Class = (ArrayType)(arrayFlags[0] & 0xFF);

            Dimensions = dimensionsArray;

            Name = Encoding.ASCII.GetString(arrayName.ToByteArray());

            switch (Class)
            {
                case ArrayType.miCELL_CLASS:
                    Cells = new miMATRIX[(int)Dimensions.Product(o => o)];
                    for (int i = 0; i < Cells.Length; i++)
                        Cells[i] = (miMATRIX)DataElement.Read(stream, reverseBytes);
                    break;

                case ArrayType.mxSTRUCT_CLASS:
                    Array = new StructArray(Dimensions, stream, reverseBytes);
                    break;                    

                case ArrayType.mxCHAR_CLASS:
                    Array = new CharacterArray(Dimensions, (miINT8)DataElement.Read(stream, reverseBytes));
                    break;

                case ArrayType.mxDOUBLE_CLASS:
                    Array = new miDOUBLEArray(Dimensions, DataElement.Read(stream, reverseBytes));
                    break;

                case ArrayType.mxSINGLE_CLASS:
                    Array = new miSINGLEArray(Dimensions, DataElement.Read(stream, reverseBytes));
                    break;

                case ArrayType.mxUINT32_CLASS:
                    Array = new miUINT32Array(Dimensions, DataElement.Read(stream, reverseBytes));
                    break;

                case ArrayType.mxINT32_CLASS:
                    Array = new miINT32Array(Dimensions, DataElement.Read(stream, reverseBytes));
                    break;

                case ArrayType.mxUINT16_CLASS:
                    Array = new miUINT16Array(Dimensions, DataElement.Read(stream, reverseBytes));
                    break;

                case ArrayType.mxINT16_CLASS:
                    Array = new miINT16Array(Dimensions, DataElement.Read(stream, reverseBytes));
                    break;

                case ArrayType.mxUINT8_CLASS:
                    Array = new miUINT8Array(Dimensions, DataElement.Read(stream, reverseBytes));
                    break;

                case ArrayType.mxINT8_CLASS:
                    Array = new miINT8Array(Dimensions, DataElement.Read(stream, reverseBytes));
                    break;
            }
        }

        /// <summary>
        /// Returns a string representation of the matrix.
        /// </summary>
        /// <returns>A string representation of the matrix.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{{Complex={0},Global={1},Logical={2},Class={3},Dimensions=[{4}],Name={5},", Complex, Global, Logical, Class, string.Join(",", Dimensions.Select(o => o.ToString())), Name);
            if (Cells != null)
                sb.AppendFormat("Cells=[{0}]}}", string.Join(",", Cells.Select(o => o.ToString())));
            else sb.AppendFormat("Array={0}}}", Array);
            return sb.ToString();
        }
    }
}
