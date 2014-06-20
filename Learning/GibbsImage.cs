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

namespace VisionNET.Learning
{
    /// <summary>
    /// This class encapsulates a two dimensional probability distribution, and provides Gibbs sampling functions on that distribution.
    /// </summary>
    [Serializable]
    public sealed class GibbsImage : IMultichannelImage<float>
    {
        private FloatArrayHandler _handler = new FloatArrayHandler();
        private float _sum;
        private float[] _rowSums;
        private float[] _columnSums;
        private const int SAMPLES = 20;
        private Random _rand = new Random();
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
        public GibbsImage()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="rows">Number of rows in the image</param>
        /// <param name="columns">Number of columns in the image</param>
        public GibbsImage(int rows, int columns)
        {
            SetDimensions(rows, columns, 1);
        }

        private void clearSums()
        {
            _sum = 0;
            _rowSums = new float[Rows];
            _columnSums = new float[Columns];
        }

        /// <summary>
        /// Adds a distribution image to this image, using <paramref name="label"/> to index that image.
        /// </summary>
        /// <param name="dist">The distribution image to add</param>
        /// <param name="label">The label to extract from the image</param>
        public unsafe void Add(DistributionImage dist, short label)
        {
            int numLabels = dist.Channels;
            int rows = dist.Rows;
            int columns = dist.Columns;
            int nRows = Rows;
            int nColumns = Columns;
            float scaleR = (float)nRows / rows;
            float scaleC = (float)nColumns / columns;
            float[,] counts = new float[nRows, nColumns];
            fixed (float* labelSrc = dist.RawArray)
            {
                float* labelPtr = labelSrc;
                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < columns; c++)
                    {
                        float val = labelPtr[label];
                        float rr = r * scaleR;
                        float cc = c * scaleC;
                        int i0 = (int)rr;
                        int j0 = (int)cc;
                        float di = rr - i0 - .5f;
                        float dj = cc - j0 - .5f;
                        if (di < 0)
                        {
                            i0--;
                            di += 1;
                        }
                        if (dj < 0)
                        {
                            j0--;
                            dj += 1;
                        }
                        int i1 = i0 + 1;
                        int j1 = j0 + 1;
                        if (i0 < 0)
                            i0++;
                        if (i1 == nRows)
                            i1--;
                        if (j0 < 0)
                            j0++;
                        if (j1 == nColumns)
                            j1--;
                        float a = 1 - di;
                        float b = 1 - dj;
                        counts[i0, j0] += a * b*val;
                        counts[i0, j1] += a * dj * val;
                        counts[i1, j0] += di * b * val;
                        counts[i1, j1] += di * dj * val;

                        _sum += val;
                        _rowSums[i0] += a * val;
                        _rowSums[i1] += di * val;
                        _columnSums[j0] += b * val;
                        _columnSums[j1] += dj * val;

                        labelPtr += numLabels;
                    }
            }
            fixed (float* dataSrc = RawArray, countsSrc = counts)
            {
                float* dataPtr = dataSrc;
                float* countsPtr = countsSrc;
                int total = Rows * Columns;
                while (total-- > 0)
                {
                    *dataPtr = *dataPtr + *countsPtr;
                    dataPtr++;
                    countsPtr++;
                }
            }
        }

        /// <summary>
        /// Adds a label image to the distribution.  Each time the label in <paramref name="labels"/> matches <paramref name="label"/>, the value at that pixel
        /// is incremented by 1.
        /// </summary>
        /// <param name="labels">Label image to add</param>
        /// <param name="label">The label to extract</param>
        public unsafe void Add(LabelImage labels, short label)
        {
            int rows = labels.Rows;
            int columns = labels.Columns;
            int nRows = Rows;
            int nColumns = Columns;
            float scaleR = (float)nRows / rows;
            float scaleC = (float)nColumns / columns;
            float[,] counts = new float[nRows, nColumns];
            fixed (short* labelSrc = labels.RawArray)
            {
                short* labelPtr = labelSrc;
                for(int r=0; r<rows; r++)
                    for (int c = 0; c < columns; c++)
                    {
                        short test = *labelPtr++;
                        if (test == label)
                        {
                            float rr = r * scaleR;
                            float cc = c * scaleC;
                            int i0 = (int)rr;
                            int j0 = (int)cc;
                            float di = rr - i0 - .5f;
                            float dj = cc - j0 - .5f;
                            if (di < 0)
                            {
                                i0--;
                                di += 1;
                            }
                            if (dj < 0)
                            {
                                j0--;                                
                                dj += 1;
                            }
                            int i1 = i0 + 1;
                            int j1 = j0 + 1;
                            if (i0 < 0)
                                i0++;
                            if (i1 == nRows)
                                i1--;
                            if (j0 < 0)
                                j0++;
                            if (j1 == nColumns)
                                j1--;
                            float a = 1-di;
                            float b = 1-dj;
                            counts[i0, j0] += a * b;
                            counts[i0, j1] += a * dj;
                            counts[i1, j0] += di * b;
                            counts[i1, j1] += di * dj;

                            _sum += 1;
                            _rowSums[i0] += a;
                            _rowSums[i1] += di;
                            _columnSums[j0] += b;
                            _columnSums[j1] += dj;
                        }
                    }
            }
            fixed (float* dataSrc = RawArray, countsSrc = counts)
            {
                float* dataPtr = dataSrc;
                float* countsPtr = countsSrc;
                int total = Rows * Columns;
                while (total-- > 0)
                {
                    *dataPtr = *dataPtr + *countsPtr;
                    dataPtr++;
                    countsPtr++;
                }
            }
        }

        /// <summary>
        /// Samples a pixel from the distribution.
        /// </summary>
        /// <param name="row">The sampled row</param>
        /// <param name="column">The sampled column</param>
        public void Sample(ref int row, ref int column)
        {
            row = 0;
            column = 0;
            int rows = Rows;
            int columns = Columns;
            float[, ,] counts = RawArray;
            for (int i = 0; i < SAMPLES; i++)
            {
                // choose column
                float[] dist = new float[rows];
                float columnNorm = 1/_columnSums[column];
                for (int r = 0; r < rows; r++)
                    dist[r] = counts[r, column, 0] * columnNorm;
                double val;
                double sample = val = _rand.NextDouble();
                row = 0;
                while (sample > 0 && row < rows)
                {
                    sample -= dist[row++];
                }
                row--;
                dist = new float[columns];
                float rowNorm = 1 / _rowSums[row];
                for (int c = 0; c < columns; c++)
                    dist[c] = counts[row, c, 0] * rowNorm;
                sample = val = _rand.NextDouble();
                column = 0;
                while (sample > 0 && column < columns)
                {
                    sample -= dist[column++];
                }
                column--;
            }
        }

        /// <summary>
        /// Computes the distribution of the image by normalizing.
        /// </summary>
        /// <returns>The image distribution</returns>
        public unsafe GrayscaleImage ComputeDistribution()
        {
            int rows = Rows;
            int columns = Columns;
            float[, ,] dist = new float[rows, columns, 1];
            float norm = 1/_sum;
            fixed (float* dataSrc = RawArray, distSrc = dist)
            {
                float* dataPtr = dataSrc;
                float* distPtr = distSrc;
                int total = rows * columns;
                while (total-- > 0)
                {
                    *distPtr = *dataPtr * norm;
                    distPtr++;
                    dataPtr++;
                }
            }
            GrayscaleImage gray = new GrayscaleImage();
            gray.SetData(dist);
            return gray;
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
            return ComputeDistribution().ToBitmap();
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
        /// Clears all data from the array.
        /// </summary>
        public void Clear()
        {
            _handler.Clear();
            clearSums();
        }

        /// <summary>
        /// Sets the data of the array to <paramref name="data"/>.  This new array will replace the current one.  No copy is created.
        /// </summary>
        /// <param name="data">Array to handle</param>
        public void SetData(float[, ,] data)
        {
            _handler.SetData(data);
            clearSums();
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
            clearSums();
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
        /// Extracts an entire channel from the array.
        /// </summary>
        /// <param name="channel">Channel to extract</param>
        /// <returns>Extracted channel</returns>
        public float[,] ExtractChannel(int channel)
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
        public float[, ,] ExtractRectangle(int startRow, int startColumn, int rows, int columns)
        {
            return _handler.ExtractRectangle(startRow, startColumn, rows, columns);
        }

        /// <summary>
        /// The underlying array.  Breaks capsulation to allow operations using pointer arithmetic.
        /// </summary>
        public float[, ,] RawArray
        {
            get { return _handler.RawArray; }
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
    }
}
