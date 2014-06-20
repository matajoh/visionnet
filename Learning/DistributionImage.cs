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
using System.Linq;
using System.Windows.Media.Imaging;

namespace VisionNET.Learning
{
    /// <summary>
    /// Encapsulates an image with a distribution over labels at each pixel.
    /// </summary>
    [Serializable]
    public sealed class DistributionImage : IMultichannelImage<float>
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
        public DistributionImage()
        {
        }
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="rows">Number of rows in the image</param>
        /// <param name="columns">Number of columns in the image</param>
        /// <param name="numLabels">Number of possible labels</param>
        public DistributionImage(int rows, int columns, int numLabels)
        {
            SetDimensions(rows, columns, numLabels);
        }

        /// <summary>
        /// Divides each value in the image by <paramref name="divisor"/>.
        /// </summary>
        /// <param name="divisor">The value by which to divide each value in the image</param>
        public unsafe void DivideThrough(float divisor)
        {
            fixed (float* dataSrc = RawArray)
            {
                int count = Rows * Columns * Channels;
                float* dataPtr = dataSrc;
                float norm = 1 / divisor;
                while (count-- > 0)
                {
                    *dataPtr = *dataPtr * norm;
                    dataPtr++;
                }
            }
        }

        /// <summary>
        /// Creates a new distribution image by appending the distributions at each pixel.
        /// </summary>
        /// <param name="lhs">The first image</param>
        /// <param name="rhs">The second image</param>
        /// <returns>The appended distribution image</returns>
        public static unsafe DistributionImage Append(DistributionImage lhs, DistributionImage rhs)
        {
            int rows = lhs.Rows;
            int columns = lhs.Columns;
            int channels0 = lhs.Channels;
            int channels1 = rhs.Channels;
            if (rhs.Rows != rows || rhs.Columns != columns)
                throw new ArgumentException("Arguments must be same dimension");
            DistributionImage combo = new DistributionImage(rows, columns, channels0 + channels1);
            fixed (float* lhsSrc = lhs.RawArray, rhsSrc = rhs.RawArray, comboSrc = combo.RawArray)
            {
                float* lhsPtr = lhsSrc;
                float* rhsPtr = rhsSrc;
                float* comboPtr = comboSrc;
                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < columns; c++)
                    {
                        for (int i = 0; i < channels0; i++)
                            *comboPtr++ = *lhsPtr++;
                        for (int i = 0; i < channels1; i++)
                            *comboPtr++ = *rhsPtr++;
                    }
            }
            return combo;
        }
       
        /// <summary>
        /// Adds a distribution to a pixel in the image.  <paramref name="distribution"/> needs to be the same dimension as this image.
        /// </summary>
        /// <param name="r">Row of the pixel</param>
        /// <param name="c">Column of the pixel</param>
        /// <param name="distribution">Distribution to add</param>
        public unsafe void Add(int r, int c, float[] distribution)
        {
            fixed (float* dataSrc = RawArray, distSrc = distribution)
            {
                float* dataPtr = dataSrc + r * Columns * Channels + c * Channels;
                float* distPtr = distSrc;
                for (int i = 0; i < distribution.Length; i++, distPtr++, dataPtr++)
                    *dataPtr = *dataPtr + *distPtr;
            }
        }

        /// <summary>
        /// Subsamples this image.
        /// </summary>
        /// <param name="subsample">Value to subsample by</param>
        /// <returns>The subsampled distribution image</returns>
        public unsafe DistributionImage Subsample(int subsample)
        {
            int rows = Rows;
            int columns = Columns;
            int channels = Channels;
            int stride = columns*channels;
            int nRows = rows/subsample;
            int nColumns = columns/subsample;
            float[, ,] dst = new float[nRows, nColumns,channels];
            fixed (float* srcBuf = RawArray, dstBuf = dst)
            {
                float* srcPtr = srcBuf;
                float* dstPtr = dstBuf;

                for (int r = 0; r < nRows; r++)
                {
                    float* srcScan = srcPtr;
                    for (int c = 0; c < nColumns; c++)
                    {
                        float* srcScan2 = srcScan;
                        for (int srcR = 0; srcR < subsample; srcR++)
                        {
                            float* srcScan3 = srcScan2;
                            for (int srcC = 0; srcC < subsample; srcC++)
                                for (int i = 0; i < channels; i++)
                                    dstPtr[i] += *srcScan3++;
                            srcScan2 += stride;
                        }
                        dstPtr += channels;
                        srcScan += subsample * channels;
                    }
                    srcPtr += subsample * stride;
                }
            }
            DistributionImage dist = new DistributionImage();
            dist.ID = ID;
            dist.SetData(dst);
            dist.Normalize();
            return dist;
        }

        /// <summary>
        /// Saves this image to disk by choosing the maximum likelihood label at each pixel and looking up colors using <paramref name="dictionary"/>.
        /// </summary>
        /// <param name="dictionary">Color lookup dictionary</param>
        /// <param name="filename">Filename to write the image to</param>
        public void Save(LabelDictionary dictionary, string filename)
        {
            ToLabelImage().Save(dictionary, filename);
        }

        /// <summary>
        /// Multiplies the distribution at each pixel by <paramref name="distribution"/>, which must be of the same dimension as the image.
        /// </summary>
        /// <param name="distribution">Distribution by which to multiply</param>
        public unsafe void Multiply(float[] distribution)
        {
            fixed (float* dataSrc = RawArray, distSrc = distribution)
            {
                float* dataPtr = dataSrc;
                int rows = Rows;
                int columns = Columns;
                int channels = Channels;
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < columns; c++)
                    {
                        float* distPtr = distSrc;
                        for (int i = 0; i < channels; i++, dataPtr++, distPtr++)
                            *dataPtr *= *distPtr;
                    }
                }
            }
        }

        /// <summary>
        /// Multiplies each pixel's distribution in this image by each pixel's distribution in <paramref name="image"/>.
        /// </summary>
        /// <param name="image">Image to multiply by</param>
        public unsafe void Multiply(DistributionImage image)
        {
            int rows = Rows;
            int columns = Columns;
            int channels = Channels;
            if (rows != image.Rows || columns != image.Columns || channels != image.Channels)
                throw new ArgumentException("Argument must be of same dimensions as image");
            fixed (float* dataSrc = RawArray, argSrc = image.RawArray)
            {
                float* dataPtr = dataSrc;
                float* argPtr = argSrc;
                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < columns; c++)
                        for (int i = 0; i < channels; i++, dataPtr++, argPtr++)
                            *dataPtr = *dataPtr * *argPtr;
            }
        }

        /// <summary>
        /// Normalizes all the distributions in the image.
        /// </summary>
        public unsafe void Normalize()
        {
            float dirichlet = .0001f/Channels;
            fixed (float* dataSrc = RawArray)
            {
                float* dataPtr = dataSrc;
                int rows = Rows;
                int columns = Columns;
                int channels = Channels;
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < columns; c++, dataPtr += channels)
                    {
                        float* dataScan = dataPtr;
                        float sum = channels * dirichlet;
                        for (int i = 0; i < channels; i++)
                            sum += *dataScan++;
                        dataScan = dataPtr;
                        float norm = 1 / sum;
                        for (int i = 0; i < channels; i++, dataScan++)
                            *dataScan = (*dataScan+dirichlet) * norm;
                    }
                }
            }
        }

        /// <summary>
        /// Computes a confusion matrix using the soft values from the distributions in <paramref name="inferredLabels"/> based upon the ground truth 
        /// pixel labels in <paramref name="groundTruth"/>.
        /// </summary>
        /// <param name="groundTruth">The ground truth labels of the image</param>
        /// <param name="inferredLabels">The inferred labels of the image</param>
        /// <returns>A confusion matrix</returns>
        public static ConfusionMatrix ComputeConfusionMatrix(LabelImage groundTruth, DistributionImage inferredLabels)
        {
            ConfusionMatrix matrix = new ConfusionMatrix(inferredLabels.Channels);
            ComputeConfusionMatrix(matrix, groundTruth, inferredLabels);
            return matrix;
        }

        /// <summary>
        /// Computes a confusion matrix using the soft values from the distributions in <paramref name="inferredLabels"/> based upon the ground truth 
        /// pixel labels in <paramref name="groundTruth"/>, and adds them to <paramref name="matrix"/>.
        /// </summary>
        /// <param name="matrix">Matrix to add the confusion values of this image to</param>
        /// <param name="groundTruth">The ground truth labels of the image</param>
        /// <param name="inferredLabels">The inferred labels of the image</param>
        public static unsafe void ComputeConfusionMatrix(ConfusionMatrix matrix, LabelImage groundTruth, DistributionImage inferredLabels)
        {
            int rows = groundTruth.Rows;
            int columns = groundTruth.Columns;
            int channels = inferredLabels.Channels;
            fixed (short* labelsSrc = groundTruth.RawArray)
            {
                fixed (float* distSrc = inferredLabels.RawArray)
                {
                    short* labelsPtr = labelsSrc;
                    float* distPtr = distSrc;
                    for (int r = 0; r < rows; r++)
                        for (int c = 0; c < columns; c++, labelsPtr++)
                        {
                            short trueLabel = *labelsPtr;
                            for (short i = 0; i < channels; i++, distPtr++)
                                matrix.Add(trueLabel, i, *distPtr);
                        }
                }
            }
        }

        /// <summary>
        /// Converts to a label image using the maximum likelihood labels of this image's pixels.
        /// </summary>
        /// <returns>A label image</returns>
        public unsafe LabelImage ToLabelImage()
        {
            LabelImage labels = new LabelImage(Rows, Columns);
            fixed (short* labelsSrc = labels.RawArray)
            {
                fixed (float* dataSrc = RawArray)
                {
                    float* dataPtr = dataSrc;
                    short* labelsPtr = labelsSrc;
                    int rows = Rows;
                    int columns = Columns;
                    int channels = Channels;
                    for (int r = 0; r < rows; r++)
                    {
                        for (int c = 0; c < columns; c++)
                        {
                            short max = -1;
                            float maxValue = float.MinValue;
                            for (short i = 0; i < channels; i++)
                            {
                                float test = *dataPtr++;
                                if (test > maxValue)
                                {
                                    maxValue = test;
                                    max = i;
                                }
                            }
                            *labelsPtr++ = max;
                        }
                    }
                }
            }
            return labels;
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
        /// Converts this image to a bitmap.  Not implemented.
        /// </summary>
        /// <returns>A bitmap version of the image</returns>
        /// <exception cref="NotImplementedException" />
        public BitmapSource ToBitmap()
        {
            throw new NotImplementedException();
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

        /// <summary>
        /// Creates a label image by sampling from the distribution.  Labels chosen are constricted to belong to <paramref name="set"/>, thus using it
        /// as a hard prior over labels.
        /// </summary>
        /// <param name="set">Set used to constrict the sampling</param>
        /// <returns>A label image</returns>
        public unsafe LabelImage GenerateLabels(LabelSet set)
        {
            int rows = Rows;
            int columns = Columns;
            int numLabels = Channels;
            int setSize = set.Count;
            LabelImage result = new LabelImage(Rows, Columns);
            int count = rows * columns;
            GibbsImage[] gibbs = new GibbsImage[set.Count];
            short[] labels = set.OrderBy(o => o).ToArray();
            float[] prior = new float[set.Count];
            for (int i = 0; i < labels.Length; i++)
            {
                gibbs[i] = new GibbsImage(rows, columns);
                gibbs[i].Add(this, labels[i]);
                prior[i] = 1f/set.Count;
            }
            int samples = rows * columns;
            int row,column;
            row = column = 0;
            for (int i = 0; i < samples; i++)
            {
                int index = (short)prior.Sample();
                gibbs[index].Sample(ref row, ref column);
                result[row, column] = labels[index];
            }
            return result;
        }
    }
}
