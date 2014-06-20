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
    /// A RGB image using bytes to store the values.
    /// </summary>
    public sealed class RGBImage : IMultichannelImage<byte>, IMultichannelImage<float>
    {
        private ByteArrayHandler _handler = new ByteArrayHandler();
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
        /// Constructor.  Constructs an empty image.
        /// </summary>
        public RGBImage() { }
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="filename">Path to source image.</param>
        public RGBImage(string filename) : this(new BitmapImage(new Uri(filename))) { }
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="bitmap">Source image.</param>
        public unsafe RGBImage(BitmapSource bitmap)
        {
            int rows = bitmap.PixelHeight;
            int columns = bitmap.PixelWidth;
            _handler = new ByteArrayHandler(rows, columns, 3);

            FormatConvertedBitmap bmp = new FormatConvertedBitmap();
            bmp.BeginInit();
            bmp.Source = bitmap;
            bmp.DestinationFormat = PixelFormats.Rgb24;
            bmp.EndInit();
            byte[] pixels = new byte[rows * columns * 3];
            bmp.CopyPixels(pixels, columns * 3, 0);
            fixed (byte* src = pixels, dst = _handler.RawArray)
            {
                byte* srcPtr = src;
                byte* dstPtr = dst;
                int count = pixels.Length;
                while (count-- > 0)
                    *dstPtr++ = *srcPtr++;
            }
            pixels = null;
        }
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="bitmap">Source image.</param>
        public unsafe RGBImage(System.Drawing.Bitmap bitmap)
        {
            int r,c;
            byte *srcPtr, srcScan, dstPtr;
            int rows = bitmap.Height;
            int columns = bitmap.Width;
            _handler = new ByteArrayHandler(rows, columns, 3);

            System.Drawing.Imaging.BitmapData srcBuf = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            srcPtr = (byte *)srcBuf.Scan0;
            int srcStride = srcBuf.Stride;

            fixed (byte* dstBuf = _handler.RawArray)
            {
                dstPtr = dstBuf;
                for (r = 0; r < rows; r++, srcPtr += srcStride)
                {
                    for (c = 0, srcScan = srcPtr; c < columns; c++)
                    {
                        byte B = *srcScan++;
                        byte G = *srcScan++;
                        byte R = *srcScan++;
                        *dstPtr++ = R;
                        *dstPtr++ = G;
                        *dstPtr++ = B;
                    }
                }
            }

            bitmap.UnlockBits(srcBuf);
        }
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="rows">Number of rows</param>
        /// <param name="columns">Number of columns</param>
        public RGBImage(int rows, int columns)
        {
            _handler = new ByteArrayHandler(rows, columns, 3);
        }
        
        /// <summary>
        /// Saves the image to file, using the extension to determine the file format.
        /// </summary>
        /// <param name="filename">Path to destination file.</param>
        public void Save(string filename)
        {
            Save(this, filename);
        }

        /// <summary>
        /// Saves the image to file, using the provided encoder.
        /// </summary>
        /// <param name="filename">Path to destination file</param>
        /// <param name="encoder">The encoder to use</param>
        public void Save(string filename, BitmapEncoder encoder)
        {
            Save(this, filename, encoder);
        }

        /// <summary>
        /// Returns the value requested by the character at (row,column).
        /// <list type="table">
        /// <listheader>
        /// <term>Character</term>
        /// <description>Channel</description>
        /// </listheader>
        /// <item><term>r,R</term>
        /// <description>Red</description></item>
        /// <item><term>g,G</term>
        /// <description>Green</description></item>
        /// <item><term>b,B</term>
        /// <description>Blue</description></item>
        /// </list>
        /// <param name="row">Desired row</param>
        /// <param name="column">Desired column</param>
        /// <param name="color">Desired color channel</param>
        /// <returns>color at (row,column)</returns>
        /// </summary>
        public byte this[int row, int column, char color]
        {
            get
            {
                int channel = -1;
                switch (color)
                {
                    case 'R':
                    case 'r':
                        channel = 0;
                        break;

                    case 'G':
                    case 'g':
                        channel = 1;
                        break;

                    case 'B':
                    case 'b':
                        channel = 2;
                        break;

                    default:
                        throw new ArgumentException("Unrecognized color channel: " + channel);
                }
                return _handler[row, column, channel];
            }
            set
            {
                int channel = -1;
                switch (color)
                {
                    case 'R':
                    case 'r':
                        channel = 0;
                        break;

                    case 'G':
                    case 'g':
                        channel = 1;
                        break;

                    case 'B':
                    case 'b':
                        channel = 2;
                        break;

                    default:
                        throw new ArgumentException("Unrecognized color channel: " + channel);
                }
                _handler[row, column, channel] = value;
            }
        }

        /// <summary>
        /// Returns a BitmapSource version of this image.
        /// </summary>
        /// <returns>A bitmapped representation this image</returns>
        public unsafe BitmapSource ToBitmap()
        {
            byte[] pixels = new byte[Rows * Columns * Channels];
            fixed (byte* src = _handler.RawArray, dst = pixels)
            {
                byte* srcPtr = src;
                byte* dstPtr = dst;
                int count = pixels.Length;
                while (count-- > 0)
                    *dstPtr++ = *srcPtr++;
            }
            return BitmapSource.Create(Columns, Rows, 76, 76, PixelFormats.Rgb24, null, pixels, Columns * 3);
        }

        /// <summary>
        /// Returns a legacy Bitmap version of this image.
        /// </summary>
        /// <returns>A bitmapped representation of the image</returns>
        public unsafe System.Drawing.Bitmap ToLegacyBitmap()
        {
            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(Width, Height);
            System.Drawing.Imaging.BitmapData buff = bmp.LockBits(new System.Drawing.Rectangle(0, 0, Width, Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            fixed (byte* src = _handler.RawArray)
            {
                byte* srcPtr = src;
                byte* dstPtr = (byte*)buff.Scan0;
                int rows = Rows;
                int columns = Columns;
                int stride = buff.Stride;

                for (int r = 0; r < rows; r++)
                {
                    byte* dstScan = dstPtr;
                    for (int c = 0; c < columns; c++)
                    {
                        byte R = *srcPtr++;
                        byte G = *srcPtr++;
                        byte B = *srcPtr++;

                        *dstScan++ = B;
                        *dstScan++ = G;
                        *dstScan++ = R;
                    }
                    dstPtr += stride;
                }
            }
            bmp.UnlockBits(buff);
            return bmp;
        }

        /// <summary>
        /// Saves the provided image to the destination path using the extension to determine the file format.
        /// </summary>
        /// <param name="image">Image to save</param>
        /// <param name="filename">Path to destination image</param>
        public static void Save(RGBImage image, string filename)
        {
            Save(image, filename, IO.GetEncoder(filename));
        }

        /// <summary>
        /// Saves the provided image to the destination path using the provided encoder.
        /// </summary>
        /// <param name="image">Image to save</param>
        /// <param name="filename">Path to destination file</param>
        /// <param name="encoder">Encoder to use</param>
        public static void Save(RGBImage image, string filename, BitmapEncoder encoder)
        {
            encoder.Frames.Add(BitmapFrame.Create(image.ToBitmap()));
            encoder.Save(filename);
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
        public byte ComputeRectangleSum(int startRow, int startColumn, int rows, int columns, int channel)
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
        public byte ComputeRectangleSum(int row, int column, int channel, Rectangle rect)
        {
            return _handler.ComputeRectangleSum(row + rect.R, column + rect.C, rect.Rows, rect.Columns, channel);
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
        public void SetData(byte[, ,] data)
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
        public byte[, ,] ExtractRectangle(int startRow, int startColumn, int rows, int columns)
        {
            return _handler.ExtractRectangle(startRow, startColumn, rows, columns);
        }

        /// <summary>
        /// Indexes the underlying array.
        /// </summary>
        /// <param name="row">Desired row</param>
        /// <param name="column">Desired column</param>
        /// <param name="channel">Desired channel</param>
        /// <returns>Value at (<paramref name="row"/>, <paramref name="column"/>, <paramref name="channel"/>) within the array.</returns>
        public byte this[int row, int column, int channel]
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
        public byte[,] ExtractChannel(int channel)
        {
            return _handler.ExtractChannel(channel);
        }

        /// <summary>
        /// The underlying array.  Breaks capsulation to allow operations using pointer arithmetic.
        /// </summary>
        public byte[, ,] RawArray
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

        private static unsafe byte[,,] convertFtoB(float[,,] input)
        {
            int rows = input.GetLength(0);
            int columns = input.GetLength(1);
            int channels = input.GetLength(2);
            byte[, ,] output = new byte[rows, columns, channels];
            fixed (float* inputSrc = input)
            {
                fixed (byte* outputSrc = output)
                {
                    float* inputPtr = inputSrc;
                    byte* outputPtr = outputSrc;

                    int count = rows * columns * channels;
                    while (count-- > 0)
                    {
                        byte val = (byte)*inputPtr++;
                        *outputPtr++ = val;
                    }
                }
            }
            return output;
        }

        private static unsafe float[, ,] convertBtoF(byte[,,] input)
        {
            int rows = input.GetLength(0);
            int columns = input.GetLength(1);
            int channels = input.GetLength(2);
            float[, ,] output = new float[rows, columns, channels];
            fixed (byte* inputSrc = input)
            {
                fixed (float* outputSrc = output)
                {
                    byte* inputPtr = inputSrc;
                    float* outputPtr = outputSrc;

                    int count = rows * columns * channels;
                    while (count-- > 0)
                    {
                        *outputPtr++ = *inputPtr++;
                    }
                }
            }
            return output;
        }

        /// <summary>
        /// Sets the data of the array to <paramref name="data"/>.  This new array will replace the current one.  No copy is created.
        /// </summary>
        /// <param name="data">Array to handle</param>
        public void SetData(float[, ,] data)
        {
            SetData(convertFtoB(data));
        }

        unsafe float IArrayHandler<float>.ComputeRectangleSum(int startRow, int startColumn, int rows, int columns, int channel)
        {
            int minRow = Utilities.FixValue(startRow, 0, Rows);
            int minColumn = Utilities.FixValue(startColumn, 0, Columns);
            int maxRow = Utilities.FixValue(startRow + rows, 1, Rows + 1);
            int maxColumn = Utilities.FixValue(startColumn + columns, 1, Columns + 1);
            if (maxRow == minRow)
                maxRow = minRow + 1;
            if (maxColumn == minColumn)
                maxColumn = minColumn + 1;
            rows = maxRow - minRow;
            columns = maxColumn - minColumn;
            int channels = Channels;
            int stride = Columns * channels;
            int sum = 0;
            fixed (byte* src = RawArray)
            {
                byte* ptr = src + minRow * stride + minColumn * channels + channel;
                for (int r = 0; r < rows; r++)
                {
                    byte* scan = ptr;
                    for (int c = 0; c < columns; c++, scan += channels)
                        sum += *scan;
                    ptr += stride;
                }
            }
            return sum;
        }

        unsafe float[,] IArrayHandler<float>.ExtractChannel(int channel)
        {
            int rows = Rows;
            int columns = Columns;
            float[,] extract = new float[rows, columns];
            fixed (float* extractSrc = extract)
            {
                fixed (byte* dataSrc = RawArray)
                {

                    byte* dataPtr = dataSrc + channel;
                    float* extractPtr = extractSrc;
                    for (int r = 0; r < rows; r++)
                        for (int c = 0; c < columns; c++)
                            *extractPtr++ = *dataPtr++;
                }
            }
            return extract;
        }

        unsafe float[, ,] IArrayHandler<float>.ExtractRectangle(int startRow, int startColumn, int rows, int columns)
        {
            int channels = Channels;
            int stride = Columns*Channels;
            float[, ,] rect = new float[rows, columns, channels];
            fixed (float* rectSrc = rect)
            {
                fixed (byte* dataSrc = RawArray)
                {
                    byte* dataPtr = dataSrc + startRow * stride + startColumn * channels;
                    float* rectPtr = rectSrc;

                    for (int r = 0; r < rows; r++)
                    {
                        byte* dataScan = dataPtr;
                        for (int c = 0; c < columns; c++)
                            *rectPtr++ = *dataScan++;
                        dataPtr += stride;
                    }
                }
            }
            return rect;
        }

        float[, ,] IArrayHandler<float>.RawArray
        {
            get { return convertBtoF(RawArray); }
        }

        float IArrayHandler<float>.this[int row, int column, int channel]
        {
            get
            {
                return _handler[row, column, channel];
            }
            set
            {
                _handler[row, column, channel] = (byte)value;
            }
        }
    }
}
