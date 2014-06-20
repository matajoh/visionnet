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
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra.Single;

namespace VisionNET
{
    /// <summary>
    /// An image which has boolean values denoting whether an edge is present at
    /// each pixel.
    /// </summary>
    public sealed class BinaryImage : IMultichannelImage<bool>
    {

        private BooleanArrayHandler _handler = new BooleanArrayHandler();
        private string _label;

        /// <summary>
        /// Label for the image.
        /// </summary>
        public string ID
        {
            get
            {
                return _label;
            }
            set
            {
                _label = value;
            }
        }

        /// <summary>
        /// Constructor.  Creates an empty image.
        /// </summary>
        /// <param name="rows">The number of rows</param>
        /// <param name="columns">The number of columns</param>
        public BinaryImage(int rows, int columns)
        {
            _handler = new BooleanArrayHandler(rows, columns, 1);
        }

        /// <summary>
        /// Returns all indices which are set to "true"
        /// </summary>
        /// <returns>List of "true" indices</returns>
        public List<ImageCell> GetCells()
        {
            List<ImageCell> cells = new List<ImageCell>();
            for (int r = 0; r < _handler.Rows; r++)
                for (int c = 0; c < _handler.Columns; c++)
                    if (_handler[r, c, 0])
                        cells.Add(new ImageCell { Row = r, Column = c });
            return cells;
        }

        /// <summary>
        /// Creates a set of features at each pixel whose value is "true", using <paramref name="values"/> as the feature value.  Each vector 
        /// is of the form [X, Y, Value], with the origin located at the center of the image.
        /// </summary>
        /// <param name="values">The values to use for the features, of the same dimensionality as this image</param>
        /// <returns>A list of features</returns>
        public List<Vector> CreateFeatures(float[,] values)
        {
            if(values.GetLength(0) != Rows || values.GetLength(1) != Columns)
                throw new ArgumentException("Values must have the same dimensions as the image");

            List<Vector> features = new List<Vector>();
            float halfR = Rows / 2f;
            float halfC = Columns / 2f;
            for(int r=0; r<_handler.Rows; r++)
                for(int c=0; c<_handler.Columns; c++)
                    if (_handler[r, c, 0])
                    {
                        features.Add(new DenseVector(new float[]{c-halfC, r-halfR, values[r, c]}));
                    }
            return features;
        }

        /// <summary>
        /// Returns or sets whether a pixel at a particular index is an edge.
        /// </summary>
        /// <param name="row">Desired row</param>
        /// <param name="column">Desired column</param>
        /// <returns>Value at (row,column)</returns>
        public bool this[int row, int column]
        {
            get
            {
                return _handler[row, column, 0];
            }
            set
            {
                _handler[row, column, 0] = value;
            }
        }

        /// <summary>
        /// Returns a Bitmap version of this image.
        /// </summary>
        /// <returns>A Bitmap representing this image</returns>
        public BitmapSource ToBitmap()
        {
            return ToGrayscale().ToBitmap();
        }

        /// <summary>
        /// Inverts the image.
        /// </summary>
        public unsafe void Invert()
        {
            fixed (bool* src = RawArray)
            {
                bool* srcPtr = src;
                int count = Rows * Columns;

                while (count-- != 0)
                {
                    *srcPtr = !*srcPtr;
                    srcPtr++;
                }
            }
        }

        /// <summary>
        /// Returns a grayscale version of this image, with 1 for "true" and 0 for "false".
        /// </summary>
        /// <returns>A grayscale version of the image</returns>
        public unsafe GrayscaleImage ToGrayscale()
        {
            GrayscaleImage mono = new GrayscaleImage(Rows, Columns);
            fixed (float* dst = mono.RawArray)
            {
                fixed (bool* src = RawArray)
                {
                    float* dstPtr = dst;
                    bool* srcPtr = src;
                    int length = Rows * Columns;
                    while (length-- > 0)
                    {
                        bool edge = *srcPtr++;
                        *dstPtr++ = edge ? 1 : 0;
                    }
                }
            }
            return mono;
        }

        /// <summary>
        /// Width of the image (equivalent to <see cref="P:Columns" />)
        /// </summary>
        public int Width
        {
            get { return Columns; }
        }

        /// <summary>
        /// Height of the image (equivalment to <see cref="P:Rows" />)
        /// </summary>
        public int Height
        {
            get { return Rows; }
        }

        /// <summary>
        /// Sets whether this array is an integral array.  This property influences how the rectangle sum will be computed.
        /// </summary>
        public bool IsIntegral
        {
            get { return _handler.IsIntegral; }
            set { _handler.IsIntegral = value; }
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
            return _handler.ComputeRectangleSum(startRow, startColumn, rows, columns, channel);
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
            return _handler.ComputeRectangleSum(row, column, channel, rect);
        }

        /// <summary>
        /// Number of rows in the array.
        /// </summary>
        public int Rows
        {
            get { return _handler.Rows; }
        }

        /// <summary>
        /// Number of columns in the array.
        /// </summary>
        public int Columns
        {
            get { return _handler.Columns; }
        }

        /// <summary>
        /// Number of channels in the array.
        /// </summary>
        public int Channels
        {
            get { return _handler.Channels; }
        }

        /// <summary>
        /// Sets the data of the array to <paramref name="data"/>.  This new array will replace the current one.  No copy is created.
        /// </summary>
        /// <param name="data">Array to handle</param>
        public void SetData(bool[, ,] data)
        {
            _handler.SetData(data);
        }

        /// <summary>
        /// Sets the dimensions of the underlying array.  The resulting new array will replace the old array completely, no data will be copied over.
        /// </summary>
        /// <param name="rows">Number of desired rows in the new array.</param>
        /// <param name="columns">Number of desired columns in the new array.</param>
        /// <param name="channels">Number of desired channels in the new array.</param>
        public void SetDimensions(int rows, int columns, int channels)
        {
            _handler.SetDimensions(rows, columns, channels);
        }

        /// <summary>
        /// Extracts a portion of the array defined by the parameters.
        /// </summary>
        /// <param name="startRow">Starting row</param>
        /// <param name="startColumn">Starting column</param>
        /// <param name="rows">Number of rows in the portion</param>
        /// <param name="columns">Number of columns in the portion</param>
        /// <returns>A portion of the array</returns>
        public bool[, ,] ExtractRectangle(int startRow, int startColumn, int rows, int columns)
        {
            return _handler.ExtractRectangle(startRow, startColumn, rows, columns);
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
                return _handler[row, column, channel];
            }
            set
            {
                _handler[row, column, channel] = value;
            }
        }

        /// <summary>
        /// Extracts an entire channel from the array.
        /// </summary>
        /// <param name="channel">Channel to extract</param>
        /// <returns>Extracted channel</returns>
        public bool[,] ExtractChannel(int channel)
        {
            return _handler.ExtractChannel(channel);
        }

        /// <summary>
        /// The underlying array.  Breaks capsulation to allow operations using pointer arithmetic.
        /// </summary>
        public bool[, ,] RawArray
        {
            get { return _handler.RawArray; }
        }

        /// <summary>
        /// Clears all data from the array.
        /// </summary>
        public void Clear()
        {
            _handler.Clear();
        }
    }
}
