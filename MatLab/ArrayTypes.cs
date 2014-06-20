using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VisionNET.MatLab
{
    /// <summary>
    /// Enumeration of the different MAT5 array types
    /// </summary>
    public enum ArrayType
    {
        /// <summary>
        /// Cell array
        /// </summary>
        miCELL_CLASS = 1,
        /// <summary>
        /// Struct array
        /// </summary>
        mxSTRUCT_CLASS = 2,
        /// <summary>
        /// Object array
        /// </summary>
        mxOBJECT_CLASS = 3,
        /// <summary>
        /// Character array
        /// </summary>
        mxCHAR_CLASS = 4,
        /// <summary>
        /// Sparse array
        /// </summary>
        mxSPARSE_CLASS = 5,
        /// <summary>
        /// Double-precision floating point array
        /// </summary>
        mxDOUBLE_CLASS = 6,
        /// <summary>
        /// Single-precision floating point array
        /// </summary>
        mxSINGLE_CLASS = 7,
        /// <summary>
        /// Signed 8-bit integer array
        /// </summary>
        mxINT8_CLASS = 8,
        /// <summary>
        /// Unsigned 8-bit integer array
        /// </summary>
        mxUINT8_CLASS = 9,
        /// <summary>
        /// Signed 16-bit integer array
        /// </summary>
        mxINT16_CLASS = 10,
        /// <summary>
        /// Unsigned 16-bit integer array
        /// </summary>
        mxUINT16_CLASS = 11,
        /// <summary>
        /// Unsigned 32-bit integer array
        /// </summary>
        mxINT32_CLASS = 12,
        /// <summary>
        /// Unsigned 64-bit integer array
        /// </summary>
        mxUINT32_CLASS = 13
    }
}
