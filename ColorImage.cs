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
using System.Windows.Media;

namespace VisionNET
{
    /// <summary>
    /// A color space conversion method.  It is assumed that one of the 3-value color spaces is used.  
    /// For examples, see <see cref="ColorConversion"/>.
    /// </summary>
    /// <param name="input1">Input value 1</param>
    /// <param name="input2">Input value 2</param>
    /// <param name="input3">Input value 3</param>
    /// <param name="output1">Output value 1</param>
    /// <param name="output2">Output value 2</param>
    /// <param name="output3">Output vlaue 3</param>
    public delegate void ColorSpaceConverter(float input1, float input2, float input3, ref float output1, ref float output2, ref float output3);
    /// <summary>
    /// A 3-channel color image.
    /// </summary>
    [Serializable]
    public sealed class ColorImage : IMultichannelImage<float>
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
        /// Constructor.
        /// </summary>
        public ColorImage() { }
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="rgb">Source image</param>
        /// <param name="converter">Converter to use</param>
        public unsafe ColorImage(RGBImage rgb, ColorSpaceConverter converter)
        {
            _handler = new FloatArrayHandler(rgb.Rows, rgb.Columns, 3);

            fixed (byte* src = rgb.RawArray)
            {
                fixed (float* dst = _handler.RawArray)
                {
                    byte* srcPtr = src;
                    float* dstPtr = dst;
                    int length = Rows * Columns;
                    while (length-- > 0)
                    {
                        float i1 = *srcPtr++;
                        float i2 = *srcPtr++;
                        float i3 = *srcPtr++;
                        float o1, o2, o3;
                        o1 = o2 = o3 = 0;
                        converter(i1, i2, i3, ref o1, ref o2, ref o3);
                        *dstPtr++ = o1;
                        *dstPtr++ = o2;
                        *dstPtr++ = o3;
                    }
                }
            }            
        }
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="bitmap">Source image</param>
        /// <param name="converter">Converter to use</param>
        public unsafe ColorImage(System.Drawing.Bitmap bitmap, ColorSpaceConverter converter)
        {
            int r, c;
            float o1, o2, o3;
            byte* srcPtr, srcScan;
            float* dstPtr;
            int rows = bitmap.Height;
            int columns = bitmap.Width;
            _handler = new FloatArrayHandler(rows, columns, 3);

            System.Drawing.Imaging.BitmapData srcBuf = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            srcPtr = (byte*)srcBuf.Scan0;
            int srcStride = srcBuf.Stride;

            fixed (float* dstBuf = _handler.RawArray)
            {
                dstPtr = dstBuf;
                for (r = 0; r < rows; r++, srcPtr += srcStride)
                {
                    for (c = 0, srcScan = srcPtr; c < columns; c++)
                    {
                        float i3 = *srcScan++;
                        float i2 = *srcScan++;
                        float i1 = *srcScan++;
                        o1 = o2 = o3 = 0;
                        converter(i1, i2, i3, ref o1, ref o2, ref o3);
                        *dstPtr++ = o1;
                        *dstPtr++ = o2;
                        *dstPtr++ = o3;
                    }
                }
            }

            bitmap.UnlockBits(srcBuf);
        }
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="bitmap">Source image</param>
        /// <param name="converter">Converter to use</param>
        public unsafe ColorImage(BitmapSource bitmap, ColorSpaceConverter converter)            
        {
            float o1, o2, o3;
            int rows = bitmap.PixelHeight;
            int columns = bitmap.PixelWidth;
            _handler = new FloatArrayHandler(rows, columns, 3);

            FormatConvertedBitmap bmp = new FormatConvertedBitmap();
            bmp.BeginInit();
            bmp.Source = bitmap;
            bmp.DestinationFormat = PixelFormats.Rgb24;
            bmp.EndInit();
            byte[] pixels = new byte[rows * columns * 3];
            bmp.CopyPixels(pixels, columns * 3, 0);
            fixed (byte* src = pixels)
            {
                fixed (float* dst = _handler.RawArray)
                {
                    byte* srcPtr = src;
                    float* dstPtr = dst;
                    int length = rows*columns;
                    while (length-- > 0)
                    {
                        float i1 = *srcPtr++;
                        float i2 = *srcPtr++;
                        float i3 = *srcPtr++;
                        o1 = o2 = o3 = 0;
                        converter(i1, i2, i3, ref o1, ref o2, ref o3);
                        *dstPtr++ = o1;
                        *dstPtr++ = o2;
                        *dstPtr++ = o3;
                    }
                }
            }
            pixels = null;
        }
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="filename">Path to source image</param>
        /// <param name="converter">Converter to use</param>
        public ColorImage(string filename, ColorSpaceConverter converter)
            : this(new BitmapImage(new Uri(filename)), converter)
        {
        }

        /// <summary>
        /// Constructor.  Uses the <see cref="ColorConversion.RGB2rgb"/> converter.
        /// </summary>
        /// <param name="filename">Path to source image</param>
        public ColorImage(string filename)
            : this(filename, new ColorSpaceConverter(ColorConversion.RGB2rgb))
        {
        }

        /// <summary>
        /// Constructor.  Uses the <see cref="ColorConversion.RGB2rgb"/> converter.
        /// </summary>
        /// <param name="bitmap">Source image</param>
        public ColorImage(BitmapSource bitmap)
            : this(bitmap, new ColorSpaceConverter(ColorConversion.RGB2rgb))
        {
        }

        /// <summary>
        /// Constructor.  Uses the <see cref="ColorConversion.RGB2rgb"/> converter.
        /// </summary>
        /// <param name="image">Source image</param>
        public ColorImage(RGBImage image)
            : this(image, new ColorSpaceConverter(ColorConversion.RGB2rgb))
        {
        }

        /// <summary>
        /// Converts the image to an RGB image using the <see cref="ColorConversion.RGB2rgb"/> converter.
        /// </summary>
        /// <returns>The converted image</returns>
        public RGBImage ToRGBImage()
        {
            return ToRGBImage(new ColorSpaceConverter(ColorConversion.rgb2RGB));
        }
        /// <summary>
        /// Converts the image to an RGB image.
        /// </summary>
        /// <param name="converter">The converter to use.</param>
        /// <returns>The converted image</returns>
        public RGBImage ToRGBImage(ColorSpaceConverter converter)
        {
            RGBImage rgb = new RGBImage(Rows, Columns);
            unsafe
            {
                fixed (float* src = RawArray)
                {
                    fixed (byte* dst = rgb.RawArray)
                    {
                        float* srcPtr = src;
                        byte* dstPtr = dst;
                        int length = Rows * Columns;
                        while (length-- > 0)
                        {
                            float i1 = *srcPtr++;
                            float i2 = *srcPtr++;
                            float i3 = *srcPtr++;
                            float o1, o2, o3;
                            o1 = o2 = o3 = 0;
                            converter(i1, i2, i3, ref o1, ref o2, ref o3);
                            *dstPtr++ = (byte)o1;
                            *dstPtr++ = (byte)o2;
                            *dstPtr++ = (byte)o3;
                        }
                    }
                }
            }
            return rgb;
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
        /// Converts this image to a bitmap.
        /// </summary>
        /// <returns>A bitmap version of the image</returns>
        public BitmapSource ToBitmap()
        {
            return ToRGBImage().ToBitmap();
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
