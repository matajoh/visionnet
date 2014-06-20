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

namespace VisionNET
{
    /// <summary>
    /// Represents the gradient of an image.  It has four channels: magnitude, orientation, horizontal gradient, and vertical gradient
    /// (in that order).
    /// </summary>
    public sealed class GradientImage : IMultichannelImage<float>
    {
        private FloatArrayHandler _handler = new FloatArrayHandler();
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
        /// The default sigma to use when smoothing the input image.
        /// </summary>
        public const float SIGMA = 1.414f;

        /// <summary>
        /// Constructor.  Creates an empty image.
        /// </summary>
        public GradientImage() { }

        /// <summary>
        /// Returns the value requested by the character at (row,column).
        /// <list type="table">
        /// <listheader>
        /// <term>Character</term>
        /// <description>Channel</description>
        /// </listheader>
        /// <item><term>m,M</term>
        /// <description>Magnitude</description></item>
        /// <item><term>o,O</term>
        /// <description>Orientation</description></item>
        /// <item><term>x,X</term>
        /// <description>Horizontal gradient</description></item>
        /// <item><term>y,Y</term>
        /// <description>Vertical gradient</description></item>
        /// </list>
        /// <param name="row">Desired row</param>
        /// <param name="column">Desired column</param>
        /// <param name="channel">Desired channel</param>
        /// <returns>channel at (row,column)</returns>
        /// </summary>
        public float this[int row, int column, char channel]
        {
            get
            {
                switch (channel)
                {
                    case 'm':
                    case 'M':
                        return _handler[row, column, 0];

                    case 'o':
                    case 'O':
                        return _handler[row, column, 1];

                    case 'x':
                    case 'X':
                        return _handler[row, column, 2];

                    case 'y':
                    case 'Y':
                        return _handler[row, column, 3];

                    default:
                        throw new ArgumentException("Index not recognized: " + channel);
                }
            }
            set
            {
                switch (channel)
                {
                    case 'm':
                    case 'M':
                        _handler[row, column, 0] = value;
                        break;

                    case 'o':
                    case 'O':
                        _handler[row, column, 1] = value;
                        break;

                    case 'x':
                    case 'X':
                        _handler[row, column, 2] = value;
                        break;

                    case 'y':
                    case 'Y':
                        _handler[row, column, 3] = value;
                        break;

                    default:
                        throw new ArgumentException("Index not recognized: " + channel);
                }
            }
        }

        /// <summary>
        /// Returns a Bitmap version of this image.
        /// </summary>
        /// <returns>A Bitmap representing this image</returns>
        public unsafe BitmapSource ToBitmap()
        {
            return ToMagnitudeMap().ToBitmap();
        }

        /// <summary>
        /// Returns a GrayscaleImage representing the edge magnitude at each pixel.
        /// </summary>
        /// <returns>A magnitude map</returns>
        public unsafe GrayscaleImage ToMagnitudeMap()
        {
            GrayscaleImage mono = new GrayscaleImage(Rows, Columns);
            fixed (float* src = RawArray, dst = mono.RawArray)
            {
                float* srcPtr = src;
                float min = float.MaxValue;
                float max = float.MinValue;
                int length = Rows * Columns;
                for (int i = 0; i < length; i++, srcPtr += 4)
                {
                    min = Math.Min(min, *srcPtr);
                    max = Math.Max(max, *srcPtr);
                }
                float scale = max - min;
                srcPtr = src;
                float* dstPtr = dst;
                while (length-- > 0)
                {
                    float val = *srcPtr;
                    val -= min;
                    val /= scale;
                    *dstPtr++ = val;
                    srcPtr += 4;
                }
            }
            return mono;
        }

        private static float PI2 = (float)(2*Math.PI);

        private static unsafe void setData(float* data)
        {
            float gx = data[2];
            float gy = data[3];
            data[0] = (float)Math.Sqrt(gx * gx + gy * gy);
            float angle = angle_radians(gx, gy);
            data[1] = angle;
        }

        private static float angle_radians(float x, float y)
        {
            double ang = Math.Atan2(y, x);

            if (ang < 0)
                return (float)(PI2 + ang);
            return (float)ang;
        }

        /// <summary>
        /// Computes a gradient image from the source image using the default sigma value.
        /// </summary>
        /// <param name="image">Source image</param>
        /// <returns>Gradient image</returns>
        public static GradientImage Compute(GrayscaleImage image)
        {
            return Compute(image, SIGMA);
        }

        /// <summary>
        /// Computes a gradient image.
        /// </summary>
        /// <param name="image">Source image</param>
        /// <param name="blurImage">Whether to blur the source image before computing the gradient</param>
        /// <returns>Gradient image</returns>
        public static GradientImage Compute(GrayscaleImage image, bool blurImage)
        {
            return Compute(image, blurImage ? SIGMA : 0f);
        }

        /// <summary>
        /// Computes a gradient image from the source image.
        /// </summary>
        /// <param name="sigma">The sigma to use when blurring the source image.</param>
        /// <param name="image">Source image</param>
        /// <returns>Gradient image</returns>        
        public static unsafe GradientImage Compute(GrayscaleImage image, float sigma)
        {
            int rows = image.Rows;
            int columns = image.Columns;
            if (sigma > 0)
            {
                image = Convolution.ConvolveGaussian<GrayscaleImage>(image, sigma);
            }
            float[, ,] data = new float[rows, columns, 4];
            fixed (float* src = image.RawArray, dst = data)
            {
                float* srcPtr = src;
                float* srcPtrP = srcPtr + 1;
                float* dstPtr = dst;
                dstPtr += 2;
                // X derivative
                for (int r = 0; r < rows; r++)
                {
                    *dstPtr = *srcPtrP - *srcPtr;
                    dstPtr += 4;
                    srcPtrP++;
                    for (int c = 1; c < columns - 1; c++, srcPtr++, srcPtrP++, dstPtr += 4)
                        *dstPtr = *srcPtrP - *srcPtr;
                    srcPtrP--;
                    *dstPtr = *srcPtrP - *srcPtr;
                    dstPtr += 4;
                    srcPtr += 2;
                    srcPtrP += 2;
                }
                srcPtr = src;
                srcPtrP = srcPtr + columns;
                dstPtr = dst;
                dstPtr += 3;
                int stride = 4 * columns;
                for (int c = 0; c < columns; c++, srcPtr++, srcPtrP++, dstPtr += 4)
                {
                    float* srcScan = srcPtr;
                    float* srcScanP = srcPtrP;
                    float* dstScan = dstPtr;
                    *dstScan = *srcScanP - *srcScan;
                    dstScan += stride;
                    srcScanP += columns;
                    for (int r = 1; r < rows - 1; r++, dstScan += stride, srcScan += columns, srcScanP += columns)
                        *dstScan = *srcScanP - *srcScan;
                    srcScanP -= columns;
                    *dstScan = *srcScanP - *srcScan;
                }
                dstPtr = dst;
                int length = rows * columns;
                for (int i = 0; i < length; i++, dstPtr += 4)
                    setData(dstPtr);
            }
            GradientImage result = new GradientImage();
            result.SetData(data);
            return result;
        }

        /// <summary>
        /// Computes a gradient image using the default sigma from the image found at the path specified.
        /// </summary>
        /// <param name="filename">Path to the source image</param>
        /// <returns>Gradient image</returns>
        public static GradientImage Compute(string filename)
        {
            return Compute(new GrayscaleImage(filename));
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
        public float ComputeRectangleSum(int startRow, int startColumn, int rows, int columns, int channel)
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
        public float ComputeRectangleSum(int row, int column, int channel, Rectangle rect)
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
        public void SetData(float[, ,] data)
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
        public float[, ,] ExtractRectangle(int startRow, int startColumn, int rows, int columns)
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
        public float this[int row, int column, int channel]
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
        public float[,] ExtractChannel(int channel)
        {
            return _handler.ExtractChannel(channel);
        }

        /// <summary>
        /// The underlying array.  Breaks capsulation to allow operations using pointer arithmetic.
        /// </summary>
        public float[, ,] RawArray
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

