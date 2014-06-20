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
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;

namespace VisionNET
{
    /// <summary>
    /// A monochrome image in which brightness values are stored with integer precision.
    /// </summary>
    public class MonochromeImage : IMultichannelImage<int>
    {
        private IntegerArrayHandler _handler;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="rows">Number of rows in the image</param>
        /// <param name="columns">Number of columns in the image</param>
        public MonochromeImage(int rows, int columns)
        {
            _handler = new IntegerArrayHandler(rows, columns, 1);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="filename">Path to the source image</param>
        public MonochromeImage(string filename)
            : this(new BitmapImage(new Uri(filename)))
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="bitmap">Source image</param>
        public MonochromeImage(BitmapSource bitmap) : this(new RGBImage(bitmap)) { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="bitmap">Source image</param>
        public MonochromeImage(System.Drawing.Bitmap bitmap) : this(new RGBImage(bitmap)) { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="image">The color image to construct this image from.  The values are calculated as (min(R,G,B) + max(R,G,B))/2</param>
        public unsafe MonochromeImage(RGBImage image)
        {
            int rows = image.Rows;
            int columns = image.Columns;
            _handler = new IntegerArrayHandler(rows, columns, 1);
            fixed (int* dst = _handler.RawArray)
            {
                fixed (byte* src = image.RawArray)
                {
                    int* dstPtr = dst;
                    byte* srcPtr = src;
                    int count = rows * columns;
                    while (count-- > 0)
                    {
                        int R = *srcPtr++;
                        int G = *srcPtr++;
                        int B = *srcPtr++;

                        int min = R < G ? (R < B ? R : (B < G ? B : G)) : (G < B ? G : B);
                        int max = R > G ? (R > B ? R : (B > G ? B : G)) : (G > B ? G : B);
                        *dstPtr++ = (min + max) >> 1;
                    }
                }
            }
        }

        /// <summary>
        /// Default Constructor.
        /// </summary>
        public MonochromeImage()
        {
            _handler = new IntegerArrayHandler();
        }

        #region IArrayHandler<int> Members

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
        /// Clear all data from the array.
        /// </summary>
        public void Clear()
        {
            _handler.Clear();
        }

        /// <summary>
        /// Sets the data of the array to <paramref name="data"/>.  This new array will replace the current one.  No copy is created.
        /// </summary>
        /// <param name="data">Array to handle</param>
        public void SetData(int[, ,] data)
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
        /// Sets whether this array is an integral array.  This property influences how the rectangle sum will be computed.
        /// </summary>
        public bool IsIntegral
        {
            get
            {
                return _handler.IsIntegral;
            }
            set
            {
                _handler.IsIntegral = value;
            }
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
        public int ComputeRectangleSum(int startRow, int startColumn, int rows, int columns, int channel)
        {
            return _handler.ComputeRectangleSum(startRow, startColumn, rows, columns, channel);
        }

        /// <summary>
        /// Extracts an entire channel from the array.
        /// </summary>
        /// <param name="channel">Channel to extract</param>
        /// <returns>Extracted channel</returns>
        public int[,] ExtractChannel(int channel)
        {
            return _handler.ExtractChannel(channel);
        }

        /// <summary>
        /// Extracts a portion of the array defined by the parameters.
        /// </summary>
        /// <param name="startRow">Starting row</param>
        /// <param name="startColumn">Starting column</param>
        /// <param name="rows">Number of rows in the portion</param>
        /// <param name="columns">Number of columns in the portion</param>
        /// <returns>A portion of the array</returns>
        public int[, ,] ExtractRectangle(int startRow, int startColumn, int rows, int columns)
        {
            return _handler.ExtractRectangle(startRow, startColumn, rows, columns);
        }

        /// <summary>
        /// The underlying array.  Breaks capsulation to allow operations using pointer arithmetic.
        /// </summary>
        public int[, ,] RawArray
        {
            get { return _handler.RawArray; }
        }

        /// <summary>
        /// Indexes the underlying array.
        /// </summary>
        /// <param name="row">Desired row</param>
        /// <param name="column">Desired column</param>
        /// <param name="channel">Desired channel</param>
        /// <returns>Value at (<paramref name="row"/>, <paramref name="column"/>, <paramref name="channel"/>) within the array.</returns>
        public int this[int row, int column, int channel]
        {
            get
            {
                return _handler[row, column, channel];
            }
            set
            {
                _handler[row,column,channel] = value;
            }
        }

        /// <summary>
        /// Indexes the underlying array.
        /// </summary>
        /// <param name="row">Desired row</param>
        /// <param name="column">Desired column</param>
        /// <returns>Value at (<paramref name="row"/>, <paramref name="column"/>) within the array.</returns>
        public int this[int row, int column]
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

        #endregion

        #region IMultichannelImage<int> Members

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
        /// Converts this monochrome image to an RGB image.  If the image is an integral image, it will store the bottom 24 bits in the R, G and B values.  If
        /// it is not an integral image, it will convert each brightness value to a byte.
        /// </summary>
        /// <returns></returns>
        public unsafe RGBImage ToRGB()
        {
            int rows = Rows;
            int columns = Columns;
            RGBImage rgb;
            if (IsIntegral)
            {
                rows++;
                columns++;
                rgb = new RGBImage(rows, columns);
                fixed (int* src = _handler.RawArray)
                {
                    fixed (byte* dst = rgb.RawArray)
                    {
                        int* srcPtr = src;
                        byte* dstPtr = dst;
                        int count = rows * columns;
                        while (count-- != 0)
                        {
                            int val = *srcPtr++;
                            *dstPtr++ = (byte)((val & 0xFF0000) >> 16);
                            *dstPtr++ = (byte)((val & 0xFF00) >> 8);
                            *dstPtr++ = (byte)(val & 0xFF);
                        }
                    }
                }
            }
            else
            {
                rgb = new RGBImage(rows, columns);
                fixed (int* src = _handler.RawArray)
                {
                    fixed (byte* dst = rgb.RawArray)
                    {
                        int* srcPtr = src;
                        byte* dstPtr = dst;
                        int count = rows * columns;
                        while (count-- != 0)
                        {
                            int val = *srcPtr++;
                            byte small = (byte)val;
                            *dstPtr++ = small;
                            *dstPtr++ = small;
                            *dstPtr++ = small;
                        }
                    }
                }
            }
            return rgb;
        }

        /// <summary>
        /// Converts this image to a grayscale image.
        /// </summary>
        /// <returns>A grayscale image</returns>
        public unsafe GrayscaleImage ToGrayscale()
        {
            int rows = Rows;
            int columns = Columns;
            GrayscaleImage grayscale = new GrayscaleImage(rows, columns);
            fixed (int* src = _handler.RawArray)
            {
                fixed (float* dst = grayscale.RawArray)
                {
                    int* srcPtr = src;
                    float* dstPtr = dst;
                    int count = rows * columns;
                    while (count-- != 0)
                    {
                        *dstPtr = *srcPtr;
                        dstPtr++;
                        srcPtr++;
                    }
                }
            }
            grayscale.Normalize();
            return grayscale;
        }

        /// <summary>
        /// Converts this image to a bitmap displayable by the graphics system.
        /// </summary>
        /// <returns>A bitmapped version of the image</returns>
        public unsafe BitmapSource ToBitmap()
        {
            return ToRGB().ToBitmap();
        }

        /// <summary>
        /// Converts this image to a legacy bitmap displayable by the graphics system.
        /// </summary>
        /// <returns>A bitmapped version of the image</returns>
        public unsafe System.Drawing.Bitmap ToLegacyBitmap()
        {
            return ToRGB().ToLegacyBitmap();
        }

        
        private string _id;

        /// <summary>
        /// The unique ID of this image.
        /// </summary>
        public string ID
        {
            get
            {
                return _id;
            }
            set
            {
                _id = value;
            }
        }

        #endregion
    }
}
