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
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VisionNET.Learning
{
    /// <summary>
    /// This class encapsulates an image with a hard label at each pixel.
    /// </summary>
    [Serializable]
    public sealed class LabelImage : IMultichannelImage<short>
    {
        /// <summary>
        /// The label equivalent to the background of an image, which has no specific label
        /// </summary>
        public static short BackgroundLabel = 0;

        private ShortArrayHandler _handler = new ShortArrayHandler();
        private Dictionary<short,int> _labelCounts;
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
        public LabelImage()
        {
        }
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="rows">Number of rows in the image</param>
        /// <param name="columns">Number of columns in the image</param>
        public LabelImage(int rows, int columns)
        {
            _handler = new ShortArrayHandler(rows, columns, 1);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="filename">Color image in label image format</param>
        /// <param name="dictionary">Color dictionary lookup for interpreting the colors in the source image into labels</param>
        public LabelImage(string filename, LabelDictionary dictionary)
            : this(new RGBImage(filename), dictionary)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="image">Color image in label image format</param>
        /// <param name="dictionary">Color dictionary lookup for interpreting the colors in the source image into labels</param>
        public LabelImage(BitmapSource image, LabelDictionary dictionary)
            : this(new RGBImage(image), dictionary)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="image">Color image in label image format</param>
        /// <param name="dictionary">Color dictionary lookup for interpreting the colors in the source image into labels</param>
        public LabelImage(System.Drawing.Bitmap image, LabelDictionary dictionary)
            : this(new RGBImage(image), dictionary)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="image">Color image in label image format</param>
        /// <param name="dictionary">Color dictionary lookup for interpreting the colors in the source image into labels</param>
        public unsafe LabelImage(RGBImage image, LabelDictionary dictionary)
        {
            _handler = new ShortArrayHandler(image.Rows, image.Columns, 1);
            short index = 1;
            short[, ,] lookup = new short[256, 256, 256];
            if (dictionary != null)
            {
                Dictionary<Color, short> labelLookup = dictionary.LabelLookup;
                foreach (Color c in labelLookup.Keys)
                    lookup[c.R, c.G, c.B] = labelLookup[c];
            }
            else
            {
                for (int R = 0; R < 256; R++)
                    for (int G = 0; G < 256; G++)
                        for (int B = 0; B < 256; B++)
                            lookup[R, G, B] = -1;
            }


            fixed (byte* imageSrc = image.RawArray)
            {
                fixed (short* labelsSrc = RawArray)
                {
                    byte* imagePtr = imageSrc;
                    short* labelsPtr = labelsSrc;

                    int count = image.Rows * image.Columns;
                    while (count-- > 0)
                    {
                        byte R = *imagePtr++;
                        byte G = *imagePtr++;
                        byte B = *imagePtr++;
                        short label = lookup[R,G,B];
                        if(label < 0){
                            label = index;
                            lookup[R,G,B] = index++;
                        }
                        *labelsPtr++ = label;
                    }
                }
            }
        }

        /// <summary>
        /// Scales this image to another size using exact scaling (no interpolation).
        /// </summary>
        /// <param name="subsample">The sampling rate</param>
        /// <returns>The scaled image</returns>
        public unsafe LabelImage Subsample(int subsample)
        {
            if (_labelCounts == null)
                updateLabelCounts();
            int rows = Rows;
            int columns = Columns;
            int stride = columns;
            int nRows = rows/subsample;
            int nColumns = columns/subsample;
            int numLabels = -1;
            foreach (short key in _labelCounts.Keys)
                numLabels = Math.Max(numLabels, key);
            numLabels++;
            short[, ,] dst = new short[nRows, nColumns, 1];
            fixed (short* srcBuf = RawArray, dstBuf = dst)
            {
                short* srcPtr = srcBuf;
                short* dstPtr = dstBuf;

                for (int r = 0; r < nRows; r++)
                {
                    short* srcScan = srcPtr;
                    for (int c = 0; c < nColumns; c++)
                    {
                        int[] counts = new int[numLabels];
                        short* srcScan2 = srcScan;
                        for (int srcR = 0; srcR < subsample; srcR++)
                        {
                            short* srcScan3 = srcScan2;
                            for (int srcC = 0; srcC < subsample; srcC++)
                            {
                                counts[*srcScan3++]++;
                            }
                            srcScan2 += stride;
                        }
                        int maxValue = -1;
                        short max = -1;
                        for(short i=0; i<numLabels; i++)
                            if (counts[i] > maxValue)
                            {
                                maxValue = counts[i];
                                max = i;
                            }
                        *dstPtr++ = max;
                        srcScan += subsample;
                    }
                    srcPtr += stride*subsample;
                }
            }
            LabelImage labels = new LabelImage();
            labels.SetData(dst);
            return labels;
        }

        /// <summary>
        /// Saves this image to file, looking up the labels in <paramref name="labels"/>.
        /// </summary>
        /// <param name="labels">Label lookup dictionary</param>
        /// <param name="filename">File to save the image to</param>
        public unsafe void Save(LabelDictionary labels, string filename)
        {
            if (labels.Count < 256)
            {
                BitmapPalette palette = new BitmapPalette(labels.LabelLookup.Keys.ToList());
                byte[] pixels = new byte[Columns * Rows];
                fixed (short* dataSrc = RawArray)
                {
                    fixed (byte* pixelsSrc = pixels)
                    {
                        short* dataPtr = dataSrc;
                        byte* pixelsPtr = pixelsSrc;
                        int count = pixels.Length;
                        while (count-- > 0)
                            *pixelsPtr++ = (byte)*dataPtr++;
                    }
                }
                BitmapSource source = BitmapSource.Create(Columns, Rows, 96, 96, PixelFormats.Indexed8, palette, pixels, Columns);
                BitmapEncoder encoder = IO.GetEncoder(filename);
                encoder.Frames.Add(BitmapFrame.Create(source));
                encoder.Save(filename);
            }
            else
            {
                ToRGBImage(labels).Save(filename);
            }
        }

        /// <summary>
        /// Returns the counts of each label.
        /// </summary>
        /// <returns>A list of label counts</returns>
        public List<RankPair<short>> GetLabelCounts()
        {
            if (_labelCounts == null)
                updateLabelCounts();
            List<RankPair<short>> pairs = new List<RankPair<short>>();
            foreach (short key in _labelCounts.Keys)
                pairs.Add(new RankPair<short>{Label=key, Rank=_labelCounts[key]});
            pairs.Sort();
            return pairs;
        }

        private unsafe void updateLabelCounts()
        {            
            _labelCounts = new Dictionary<short,int>();
            fixed (short* src = RawArray)
            {
                short* ptr = src;
                int length = Rows * Columns;
                while (length-- > 0)
                {
                    short label = *ptr++;
                    if (!_labelCounts.ContainsKey(label))
                        _labelCounts[label] = 0;
                    _labelCounts[label]++;
                }
            }                    
        }

        /// <summary>
        /// The labels present within this image.
        /// </summary>
        public LabelSet Labels
        {
            get
            {
                if(_labelCounts == null)
                    updateLabelCounts();
                return new LabelSet(_labelCounts.Keys);
            }
        }

        /// <summary>
        /// Pixel counts by label.
        /// </summary>
        public Dictionary<short, int> LabelCounts
        {
            get
            {
                if (_labelCounts == null)
                    updateLabelCounts();
                return _labelCounts;
            }
        }

        /// <summary>
        /// Extracts all segments within the image.  Each "segment" is a list of all points with a common label.
        /// </summary>
        /// <returns>The segments of the image, indexed by segment label</returns>
        public unsafe Dictionary<short,List<ImageDataPoint<short>>> ExtractSegments()
        {
            LabelSet labels = Labels;
            List<ImageDataPoint<short>>[] segments = new List<ImageDataPoint<short>>[labels.Max() + 1];
            foreach(short label in labels)
                segments[label] = new List<ImageDataPoint<short>>();

            int rows = Rows;
            int columns = Columns;
            fixed (short* src = RawArray)
            {
                short* ptr = src;
                for (short r = 0; r < rows; r++)
                    for (short c = 0; c < columns; c++)
                    {
                        short label = *ptr++;
                        segments[label].Add(new ImageDataPoint<short>(this, r, c, label));
                    }
            }
            return (from label in labels
                    select new
                    {
                        Label = label,
                        Points = segments[label]
                    }).ToDictionary(o => o.Label, o => o.Points);
        }

        /// <summary>
        /// Extracts a segment from a provided image.
        /// </summary>
        /// <typeparam name="T">Type of <paramref name="image"/></typeparam>
        /// <param name="label">Label of the segment to extract</param>
        /// <param name="image">Image to extract the segment from</param>
        /// <returns>All the points in an image segment</returns>
        public unsafe List<ImageDataPoint<T>> ExtractSegment<T>(short label, IMultichannelImage<T> image)
        {
            if (image.Rows != Rows || image.Columns != Columns)
                throw new Exception("Provided image is of difference dimensions than this label image");

            List<ImageDataPoint<T>> points = new List<ImageDataPoint<T>>();
            int rows = Rows;
            int columns = Columns;
            fixed (short* src = RawArray)
            {
                short* srcPtr = src;
                for (short r = 0; r < rows; r++)
                    for (short c = 0; c < columns; c++, srcPtr++)
                        if (*srcPtr == label)
                            points.Add(new ImageDataPoint<T>(image, r, c, label));
            }
            return points;
        }

        /// <summary>
        /// Converts this image to a bitmap.
        /// </summary>
        /// <returns>A bitmap version of the image</returns>
        /// <exception cref="NotImplementedException" />
        public BitmapSource ToBitmap()
        {
            return ToRGBImage().ToBitmap();
        }

        /// <summary>
        /// Indexes the image.
        /// </summary>
        /// <param name="row">The pixel row</param>
        /// <param name="column">The pixel column</param>
        /// <returns>Pixel label</returns>
        public short this[int row, int column]
        {
            get
            {
                return _handler[row, column, 0];
            }
            set
            {
                _handler[row, column, 0] = value;
                _labelCounts = null;
            }
        }

        /// <summary>
        /// Converts this image to an RGB Image, creating a random color mapping for the label values.
        /// </summary>
        /// <returns>An RGB depiction of the label image</returns>
        public unsafe RGBImage ToRGBImage()
        {
            return ToRGBImage(null);
        }

        /// <summary>
        /// Converts the label image to an RGB Image using <paramref name="dictionary"/>.
        /// </summary>
        /// <param name="dictionary">The label lookup dictionary</param>
        /// <returns>An RGB Image</returns>
        public unsafe RGBImage ToRGBImage(LabelDictionary dictionary)
        {
            updateLabelCounts();
            int labelCount = _labelCounts.Keys.Max() + 1;
            Color[] colors = new Color[labelCount];
            colors[0] = Colors.Black;
            for (short i = 1; i < labelCount; i++)
                colors[i] = LabelDictionary.LabelToColor(i);
            if (dictionary != null)
            {
                colors = new Color[dictionary.Count];
                Dictionary<Color, short> lookupTable = dictionary.LabelLookup;
                foreach (Color key in lookupTable.Keys)
                    colors[lookupTable[key]] = key;
            }
            RGBImage rgb = new RGBImage(Rows, Columns);
            fixed (short* labelsSrc = RawArray)
            {
                fixed (byte* rgbSrc = rgb.RawArray)
                {
                    short* labelsPtr = labelsSrc;
                    byte* rgbPtr = rgbSrc;

                    int count = Rows * Columns;
                    while (count-- > 0)
                    {
                        short label = *labelsPtr++;
                        if (label < 0)
                            label = 0;
                        Color c = colors[label];
                        *rgbPtr++ = c.R;
                        *rgbPtr++ = c.G;
                        *rgbPtr++ = c.B;
                    }
                }
            }
            return rgb;
        }

        /// <summary>
        /// Computes a confusion matrix from <paramref name="inferredLabels"/> using <paramref name="trueLabels"/> as a reference.
        /// </summary>
        /// <param name="numLabels">The number of possible labels</param>
        /// <param name="trueLabels">The true labels of an image</param>
        /// <param name="inferredLabels">The inferred labels of an image</param>
        /// <returns>A confusion matrix</returns>
        public static unsafe ConfusionMatrix ComputeConfusionMatrix(int numLabels, LabelImage trueLabels, LabelImage inferredLabels)
        {
            ConfusionMatrix matrix = new ConfusionMatrix(numLabels);
            ComputeConfusionMatrix(matrix, trueLabels, inferredLabels);
            return matrix;
        }

        /// <summary>
        /// Computes a confusion matrix from <paramref name="inferredLabels"/> using <paramref name="trueLabels"/> as a reference, and adds
        /// the information into <paramref name="matrix"/>.
        /// </summary>
        /// <param name="matrix">The matrix that will hold the results</param>
        /// <param name="trueLabels">The true labels of an image</param>
        /// <param name="inferredLabels">The inferred labels of an image</param>
        public static unsafe void ComputeConfusionMatrix(ConfusionMatrix matrix, LabelImage trueLabels, LabelImage inferredLabels)
        {
            fixed (short* trueLabelsSrc = trueLabels.RawArray, inferredLabelsSrc = inferredLabels.RawArray)
            {
                short* trueLabelsPtr = trueLabelsSrc;
                short* inferredLabelsPtr = inferredLabelsSrc;

                int count = trueLabels.Rows * trueLabels.Columns;
                while (count-- > 0)
                {
                    short row = *trueLabelsPtr++;
                    short column = *inferredLabelsPtr++;
                    if (row == BackgroundLabel)
                        continue;
                    matrix.Add(row, column);
                }
            }
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
        /// Computes a sum of the values in the array starting at (<paramref name="row"/>, <paramref name="column"/>) in <paramref name="channel" /> 
        /// in a rectangle described by the offset and size in <paramref name="rect"/>.
        /// </summary>
        /// <param name="row">Reference row</param>
        /// <param name="column">Reference column</param>
        /// <param name="channel">Channel to draw values from</param>
        /// <param name="rect">Offset and size of the rectangle</param>
        /// <returns>The sum of all values in the rectangle</returns>
        public short ComputeRectangleSum(int row, int column, int channel, Rectangle rect)
        {
            throw new NotImplementedException();
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
        public void SetData(short[, ,] data)
        {
            _handler.SetData(data);
            _labelCounts = null;
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
            _labelCounts = null;
        }

        /// <summary>
        /// Extracts a portion of the array defined by the parameters.
        /// </summary>
        /// <param name="startRow">Starting row</param>
        /// <param name="startColumn">Starting column</param>
        /// <param name="rows">Number of rows in the portion</param>
        /// <param name="columns">Number of columns in the portion</param>
        /// <returns>A portion of the array</returns>
        public short[, ,] ExtractRectangle(int startRow, int startColumn, int rows, int columns)
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
        public short this[int row, int column, int channel]
        {
            get
            {
                return _handler[row, column, channel];
            }
            set
            {
                _handler[row, column, channel] = value;
                _labelCounts = null;
            }
        }

        /// <summary>
        /// Extracts an entire channel from the array.
        /// </summary>
        /// <param name="channel">Channel to extract</param>
        /// <returns>Extracted channel</returns>
        public short[,] ExtractChannel(int channel)
        {
            return _handler.ExtractChannel(channel);
        }

        /// <summary>
        /// The underlying array.  Breaks capsulation to allow operations using pointer arithmetic.
        /// </summary>
        public short[, ,] RawArray
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
        /// Computes a sum of the values in the array within the rectangle starting at (<paramref name="startRow" />, <paramref name="startColumn"/>) in <paramref name="channel"/>
        /// with a size of <paramref name="rows"/>x<paramref name="columns"/>.
        /// </summary>
        /// <param name="startRow">Starting row</param>
        /// <param name="startColumn">Starting column</param>
        /// <param name="rows">Number of rows in the rectangle</param>
        /// <param name="columns">Number of columns in the rectangle</param>
        /// <param name="channel">Channel to draw values from</param>
        /// <returns>The sum of all values in the rectangle</returns>
        public short ComputeRectangleSum(int startRow, int startColumn, int rows, int columns, int channel)
        {
            throw new NotImplementedException();
        }
    }
}
