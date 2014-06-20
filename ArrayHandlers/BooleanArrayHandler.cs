/*
 * Vision.NET 2.1 Computer Vision Library
 * Copyright (C) 2009 Matthew Johnson
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace VisionNET
{
    /// <summary>
    /// Handles a three dimensional array of boolean values.
    /// </summary>
    [Serializable]
    public sealed class BooleanArrayHandler:IArrayHandler<bool>
    {
        private bool[,,] _data;
        private int _rows;
        private int _columns;
        private int _channels;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="rows">Number of rows.</param>
        /// <param name="columns">Number of columns.</param>
        /// <param name="channels">Number of channels.</param>
        public BooleanArrayHandler(int rows, int columns, int channels)
        {
            SetDimensions(rows, columns, channels);
        }
        /// <summary>
        /// Constructor.  Creates a dimensionless array.
        /// </summary>
        public BooleanArrayHandler() { }
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="data">Array to handle.</param>
        public BooleanArrayHandler(bool[, ,] data)
        {
            SetData(data);
        }

        /// <summary>
        /// Number of rows in the array.
        /// </summary>
        public int Rows
        {
            get
            {
                return _rows;
            }
        }

        /// <summary>
        /// Number of columns in the array.
        /// </summary>
        public int Columns
        {
            get
            {
                return _columns;
            }
        }

        /// <summary>
        /// Number of channels in the array.
        /// </summary>
        public int Channels
        {
            get
            {
                return _channels;
            }
        }

        /// <summary>
        /// Computes a sum of the values in the array starting at (<paramref name="row"/>, <paramref name="column"/>) in <paramref name="channel" /> 
        /// in a rectangle described by the offset and size in <paramref name="rect"/>.
        /// </summary>
        /// <param name="row">Reference row</param>
        /// <param name="column">Reference column</param>
        /// <param name="channel">Channel to draw values from</param>
        /// <param name="rect">Offset and size of the rectangle</param>
        /// <returns>The sum of all values in the rectangle</returns>
        public bool ComputeRectangleSum(int row, int column, int channel, Rectangle rect)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Extracts an entire channel from the array.
        /// </summary>
        /// <param name="channel">Channel to extract</param>
        /// <returns>Extracted channel</returns>
        public unsafe bool[,] ExtractChannel(int channel)
        {
            bool[,] data = new bool[_rows, _columns];
            fixed (bool* src = _data, dst = data)
            {
                bool* srcPtr = src;
                bool* dstPtr = dst;
                srcPtr += channel;
                for (int r = 0; r < _rows; r++)
                    for (int c = 0; c < _columns; c++, srcPtr += _channels, dstPtr++)
                        *dstPtr = *srcPtr;
            }
            return data;
        }

        /// <summary>
        /// Extracts a portion of the array defined by the parameters.
        /// </summary>
        /// <param name="startRow">Starting row</param>
        /// <param name="startColumn">Starting column</param>
        /// <param name="rows">Number of rows in the portion</param>
        /// <param name="columns">Number of columns in the portion</param>
        /// <returns>A portion of the array</returns>
        public unsafe bool[, ,] ExtractRectangle(int startRow, int startColumn, int rows, int columns)
        {
            bool[, ,] patch = new bool[rows, columns, _channels];
            int stride = _columns * _channels;
            fixed (bool* src = _data, dst = patch)
            {
                bool* tl = src;
                bool* bl = src + (_rows - 1) * stride;
                bool* srcPtr = src + startRow * stride + startColumn * _channels;
                bool* dstPtr = dst;
                for (int r = 0; r < rows; r++, srcPtr += stride)
                {
                    int rr = startRow + r;
                    bool* patchScan;
                    if (rr < 0)
                    {
                        if (startColumn < 0)
                            patchScan = tl;
                        else patchScan = tl + startColumn * _channels;
                    }
                    else if (rr >= _rows)
                    {
                        if (startColumn < 0)
                            patchScan = bl;
                        else patchScan = bl + startColumn * _channels;
                    }
                    else if (startColumn < 0)
                        patchScan = tl + rr * stride;
                    else patchScan = srcPtr;

                    for (int c = 0; c < columns; c++)
                    {
                        for (int i = 0; i < _channels; i++, dstPtr++)
                            *dstPtr = patchScan[i];
                        int cc = startColumn + c;
                        if (cc >= 0 && cc < _columns - 1)
                            patchScan += _channels;
                    }
                }
            }
            return patch;
        }

        /// <summary>
        /// The underlying array.  Breaks capsulation to allow operations using pointer arithmetic.
        /// </summary>
        public bool[, ,] RawArray
        {
            get
            {
                return _data;
            }
        }

        /// <summary>
        /// Sets the data of the array to <paramref name="data"/>.  This new array will replace the current one.  No copy is created.
        /// </summary>
        /// <param name="data">Array to handle</param>
        public void SetData(bool[, ,] data)
        {
            _data = data;
            _rows = data.GetLength(0);
            _columns = data.GetLength(1);
            _channels = data.GetLength(2);
        }

        /// <summary>
        /// Sets the dimensions of the underlying array.  The resulting new array will replace the old array completely, no data will be copied over.
        /// </summary>
        /// <param name="rows">Number of desired rows in the new array.</param>
        /// <param name="columns">Number of desired columns in the new array.</param>
        /// <param name="channels">Number of desired channels in the new array.</param>
        public void SetDimensions(int rows, int columns, int channels)
        {
            _data = new bool[rows, columns, channels];
            _rows = rows;
            _columns = columns;
            _channels = channels;
        }

        /// <summary>
        /// Indexes the underlying array.
        /// </summary>
        /// <param name="row">Desired row</param>
        /// <param name="column">Desired column</param>
        /// <param name="channel">Desired column</param>
        /// <returns>Value at (<paramref name="row"/>, <paramref name="column"/>, <paramref name="channel"/>) within the array.</returns>
        public bool this[int row, int column, int channel]
        {
            get
            {
                return _data[row, column, channel];
            }
            set
            {
                _data[row, column, channel] = value;
            }
        }

        /// <summary>
        /// Clears all data from the array.
        /// </summary>
        public void Clear()
        {
            _data = new bool[_rows, _columns, _channels];
        }

        /// <summary>
        /// Computes a sum of the values in the array within the rectangle starting at (<paramref name="startRow" />, <paramref name="startColumn"/>) in <paramref name="channel"/>
        /// with a size of <paramref name="rows"/>x<paramref name="columns"/>.
        /// </summary>
        /// <param name="startRow">Starting row</param>
        /// <param name="startColumn">Starting column</param>
        /// <param name="rows">Number of rows in the rectangle</param>
        /// <param name="columns">Number of columns in the rectangle</param>
        /// <param name="channel">Channel to draw values from</param>
        /// <returns>The sum of all values in the rectangle</returns>
        public bool ComputeRectangleSum(int startRow, int startColumn, int rows, int columns, int channel)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets whether this array is an integral array.  This property influences how the rectangle sum is computed.
        /// </summary>
        /// <exception cref="NotImplementedException" />
        public bool IsIntegral
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
    }
}
