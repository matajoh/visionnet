using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VisionNET.MatLab
{
    /// <summary>
    /// Enumeration of valid data types in a MAT5 file
    /// </summary>
    public enum DataType
    {
        /// <summary>
        /// Signed 8-bit integer
        /// </summary>
        miINT8 = 1,
        /// <summary>
        /// Unsigned 8-bit integer
        /// </summary>
        miUINT8 = 2,
        /// <summary>
        /// Signed 16-bit integer
        /// </summary>
        miINT16 = 3,
        /// <summary>
        /// Unsigned 16-bit integer
        /// </summary>
        miUINT16 = 4,
        /// <summary>
        /// Signed 32-bit integer
        /// </summary>
        miINT32 = 5,
        /// <summary>
        /// Unsigned 32-bit integer
        /// </summary>
        miUINT32 = 6,
        /// <summary>
        /// Single-precision floating point
        /// </summary>
        miSINGLE = 7,
        /// <summary>
        /// Double-precision floating point
        /// </summary>
        miDOUBLE = 9,
        /// <summary>
        /// Signed 64-bit integer
        /// </summary>
        miINT64 = 12,
        /// <summary>
        /// Unsigned 64-bit integer
        /// </summary>
        miUINT64 = 13,
        /// <summary>
        /// MatLab matrix
        /// </summary>
        miMATRIX = 14,
        /// <summary>
        /// MatLab compressed element
        /// </summary>
        miCOMPRESSED = 15,
        /// <summary>
        /// UTF8 character
        /// </summary>
        miUTF8 = 16,
        /// <summary>
        /// UTF16 character
        /// </summary>
        miUTF16 = 17,
        /// <summary>
        /// UTF132 character
        /// </summary>
        miUTF32 = 18
    };
}
