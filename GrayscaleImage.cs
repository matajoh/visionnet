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
    /// A grayscale image, with real-valued pixels.
    /// </summary>
    [Serializable]
    public sealed class GrayscaleImage : IMultichannelImage<float>
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
        /// Constructor.  Creates an empty image.
        /// </summary>
        public GrayscaleImage() { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="values">Values to use for populating this image</param>
        public unsafe GrayscaleImage(float[,] values)
        {
            int rows = values.GetLength(0);
            int columns = values.GetLength(1);
            float[, ,] data = new float[rows, columns, 1];
            fixed (float* src = values, dst = data)
            {
                int length = rows * columns;
                float* srcPtr = src;
                float* dstPtr = dst;
                while (length-- > 0)
                    *dstPtr++ = *srcPtr++;
            }
            _handler = new FloatArrayHandler(data, false);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="filename">Path to the source image</param>
        public GrayscaleImage(string filename)
            : this(new BitmapImage(new Uri(filename)))
        {
            ID = filename;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="bitmap">Source image</param>
        public GrayscaleImage(BitmapSource bitmap) : this(new RGBImage(bitmap)) { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="bitmap">Source image</param>
        public GrayscaleImage(System.Drawing.Bitmap bitmap) : this(new RGBImage(bitmap)) { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="image">Source image</param>
        public unsafe GrayscaleImage(RGBImage image)
        {
            _handler = new FloatArrayHandler(image.Rows, image.Columns, 1);
            fixed (byte* src = image.RawArray)
            {
                fixed (float* dst = _handler.RawArray)
                {
                    byte* srcPtr = src;
                    float* dstPtr = dst;
                    int length = Rows * Columns;
                    while (length-- > 0)
                    {
                        int R = *srcPtr++;
                        int G = *srcPtr++;
                        int B = *srcPtr++;

                        float min = Math.Min(R, Math.Min(G, B));
                        float max = Math.Max(R, Math.Max(G, B));
                        float val = (min + max) / 2;
                        *dstPtr++ = val / 255;
                    }
                }
            }
            ID = image.ID;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="image">Source image</param>
        /// <param name="transform">Transform that converts RGB to grayscale</param>
        public unsafe GrayscaleImage(RGBImage image, float[] transform)
        {
            float t0 = transform[0];
            float t1 = transform[1];
            float t2 = transform[2];
            _handler = new FloatArrayHandler(image.Rows, image.Columns, 1);
            fixed (byte* src = image.RawArray)
            {
                fixed (float* dst = _handler.RawArray)
                {
                    byte* srcPtr = src;
                    float* dstPtr = dst;
                    int length = Rows * Columns;
                    while (length-- > 0)
                    {
                        int R = *srcPtr++;
                        int G = *srcPtr++;
                        int B = *srcPtr++;

                        *dstPtr++ = t0 * R + t1 * G + t2 * B;
                    }
                }
            }
            ID = image.ID;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="rows">Number of rows</param>
        /// <param name="columns">Number of columns</param>
        public GrayscaleImage(int rows, int columns)
        {
            _handler = new FloatArrayHandler(rows, columns, 1);
        }

        /// <summary>
        /// Accesses the value at the specified row and column.
        /// </summary>
        /// <param name="row">The desired row</param>
        /// <param name="column">The desired column</param>
        /// <returns>The value at (row,column)</returns>
        public float this[int row, int column]
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
        /// Saves this image to the provided filename, detecting the correct file format from the extension.
        /// </summary>
        /// <param name="filename">The path to the destination image</param>
        public void Save(string filename)
        {
            Save(this, filename);
        }

        /// <summary>
        /// Saves the image using the provided minimum and maximum values for scaling.
        /// </summary>
        /// <param name="filename">Destination filename</param>
        /// <param name="min">The value to be scaled to 0</param>
        /// <param name="max">The value to be scaled to 255</param>
        public void Save(string filename, float min, float max)
        {
            Save(this, filename, min, max);
        }

        /// <summary>
        /// Returns a Bitmap version of this image using the computed minimum and maximum values.
        /// </summary>
        /// <returns>A Bitmap representing this image</returns>
        public unsafe BitmapSource ToBitmap()
        {
            float min, max;
            getMinMax(out min, out max);
            return ToBitmap(min, max);
        }

        /// <summary>
        /// Converts this image to an RGB image, scaling the values so that the maximum value corresponds to a brightness of 255 and
        /// the minimum value corresponds to a brightness of 0.
        /// </summary>
        /// <returns>An RGB depiction of this grayscale image</returns>
        public RGBImage ToRGBImage()
        {
            float min, max;
            getMinMax(out min, out max);
            return ToRGBImage(min, max);
        }

        /// <summary>
        /// Returns a legacy Bitmap version of this image using the computed minimum and maximum values.
        /// </summary>
        /// <returns>A Bitmap representing this image</returns>
        public System.Drawing.Bitmap ToLegacyBitmap()
        {
            float min, max;
            getMinMax(out min, out max);
            return ToRGBImage(min, max).ToLegacyBitmap();
        }

        /// <summary>
        /// Converts the image to a legacy bitmap using the provided minimum and maximum values.
        /// </summary>
        /// <param name="min">The value to map to 0</param>
        /// <param name="max">The value to map to 255</param>
        /// <returns>A Bitmap representing this image</returns>
        public System.Drawing.Bitmap ToLegacyBitmap(float min, float max)
        {
            return ToRGBImage(min, max).ToLegacyBitmap();
        }

        private unsafe void getMinMax(out float min, out float max)
        {
            min = float.MaxValue;
            max = float.MinValue;
            fixed (float* src = _handler.RawArray)
            {
                float* srcPtr = src;
                int length = Rows * Columns;
                while (length-- > 0)
                {
                    float val = *srcPtr++;
                    min = val < min ? val : min;
                    max = val > max ? val : max;
                }
            }
        }

        /// <summary>
        /// Scales the values of the image to land within the provided min and max values while maintaining relative proportions.
        /// </summary>
        /// <param name="min">Desired minimum value</param>
        /// <param name="max">Desired maximum value</param>
        public unsafe void Rescale(float min, float max)
        {
            float oldMin, oldMax;
            getMinMax(out oldMin, out oldMax);
            float oldScale = oldMax - oldMin;
            float scale = max - min;
            float rescale = scale / oldScale;

            fixed (float* src = _handler.RawArray)
            {
                float* srcPtr = src;
                int length = Rows * Columns;
                while (length-- > 0)
                {
                    float val = *srcPtr;
                    *srcPtr++ = (val - oldMin) * rescale + min;
                }
            }            
        }

        /// <summary>
        /// Normalize the values in the image to have zero mean and unit variance.
        /// </summary>
        public unsafe void Normalize()
        {
            fixed (float* src = _handler.RawArray)
            {
                float* srcPtr;
                int i;

                int length = Rows * Columns;
                double mean = 0;
                for (srcPtr = src, i = 0; i < length; i++, srcPtr++)
                {
                    mean += *srcPtr;
                }
                mean /= length;
                double variance = 0;
                for (srcPtr = src, i = 0; i < length; i++, srcPtr++)
                {
                    double val = *srcPtr - mean;
                    variance += val * val;
                    *srcPtr = (float)val;
                }
                variance /= length;
                if (variance == 0.0)
                    variance = 1.0;
                double scale = 1.0 / Math.Sqrt(variance);
                for (srcPtr = src, i = 0; i < length; i++, srcPtr++)
                    *srcPtr = (float)(*srcPtr * scale);
            }
        }

        /// <summary>
        /// Returns a Bitmap version of this image.
        /// </summary>
        /// <param name="min">Minimum value, equivalent to a gray value of 0</param>
        /// <param name="max">Maximum value, equivalent to a gray value of 1</param>
        /// <returns>A Bitmap representing this image</returns>
        public unsafe BitmapSource ToBitmap(float min, float max)
        {
            return ToRGBImage(min, max).ToBitmap();
        }

        /// <summary>
        /// Converts the grayscale image to an RGB image using the provided <paramref name="min"/> and <paramref name="max"/> values.
        /// </summary>
        /// <param name="min">Minimum value (corresponds to a brightness of 0)</param>
        /// <param name="max">Maximum value (corresponds to a brightness of 255)</param>
        /// <returns></returns>
        public unsafe RGBImage ToRGBImage(float min, float max)
        {
            RGBImage result = new RGBImage(Rows, Columns);
            float scale = max - min;
            if (scale == 0)
                scale = 1;
            fixed (byte* dst = result.RawArray)
            {
                fixed (float* src = _handler.RawArray)
                {
                    float* srcPtr = src;
                    byte* dstPtr = dst;
                    int length = Rows * Columns;
                    while (length-- > 0)
                    {
                        float tmp = *srcPtr++;
                        if (tmp > max)
                            tmp = max;
                        if (tmp < min)
                            tmp = min;
                        byte val = (byte)((255 * (tmp - min)) / scale);
                        *dstPtr++ = val;
                        *dstPtr++ = val;
                        *dstPtr++ = val;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Creates a "heat" image wherein values are shown on a scale from blue to red for better visualization of gradients.
        /// </summary>
        /// <returns>An RGB Image</returns>
        public unsafe RGBImage ToHeatImage()
        {
            float min, max;
            getMinMax(out min, out max);
            return ToHeatImage(min, max);
        }

        /// <summary>
        /// Creates a "heat" image wherein values are shown on a scale from blue to red for better visualization of gradients using
        /// the provided min and max values.
        /// </summary>
        /// <returns>An RGB Image</returns>
        public unsafe RGBImage ToHeatImage(float min, float max)
        {
            RGBImage result = new RGBImage(Rows, Columns);
            float scale = max - min;
            if (scale == 0)
                scale = 1;
            fixed (byte* dst = result.RawArray)
            {
                fixed (float* src = _handler.RawArray)
                {
                    float* srcPtr = src;
                    byte* dstPtr = dst;
                    int length = Rows * Columns;
                    while (length-- != 0)
                    {
                        float tmp = *srcPtr++;
                        float x = (tmp - min) / scale;
                        if (x == 0)
                        {
                            *dstPtr++ = 0;
                            *dstPtr++ = 0;
                            *dstPtr++ = 0;
                        }
                        else if (x < .5f)
                        {
                            float val = 255 * x / .5f;
                            *dstPtr++ = 0;
                            *dstPtr++ = (byte)val;
                            *dstPtr++ = (byte)(255 - val);
                        }
                        else
                        {
                            x -= .5f;
                            float val = 255 * x / .5f;
                            *dstPtr++ = (byte)val;
                            *dstPtr++ = (byte)(255 - val);
                            *dstPtr++ = 0;
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Saves the provided image to file, determining the correct file format from the extension.
        /// </summary>
        /// <param name="image">The image to save</param>
        /// <param name="filename">The path to the destination image</param>
        public static void Save(GrayscaleImage image, string filename)
        {
            BitmapEncoder encoder = IO.GetEncoder(filename);
            encoder.Frames.Add(BitmapFrame.Create(image.ToBitmap()));
            encoder.Save(filename);
        }

        /// <summary>
        /// Saves the image to a file, using the provided minimum and maximum values for scaling.
        /// </summary>
        /// <param name="image">The image to save</param>
        /// <param name="filename">The destination filename</param>
        /// <param name="min">The value to map to 0</param>
        /// <param name="max">The value to map to 255</param>
        public static void Save(GrayscaleImage image, string filename, float min, float max)
        {
            BitmapEncoder encoder = IO.GetEncoder(filename);
            encoder.Frames.Add(BitmapFrame.Create(image.ToBitmap(min, max)));
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

        /// <summary>
        /// Sets whether this array is an integral array.  This property influences how the rectangle sum will be computed.
        /// </summary>
        public bool IsIntegral
        {
            get { return _handler.IsIntegral; }
            set { _handler.IsIntegral = value; }
        }
    }
}
