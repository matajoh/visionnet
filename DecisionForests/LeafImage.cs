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
using VisionNET.Comparison;
using VisionNET.Learning;
using System.Windows.Media.Imaging;

namespace VisionNET.DecisionForests
{
    /// <summary>
    /// An image where each pixel has an array of node metadata associated with where that pixel in the source image was classified in a decision forest.
    /// </summary>
    /// <typeparam name="T">The type of the source image and the decision forest</typeparam>
    [Serializable]
    public sealed class LeafImage<T> : IMultichannelImage<INodeInfo<ImageDataPoint<T>, T[]>>
    {
        private INodeInfo<ImageDataPoint<T>, T[]>[, ,] _data;
        private int _rows;
        private int _columns;
        private int _trees;
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
        /// <param name="rows">Number of rows in the image</param>
        /// <param name="columns">Number of columns in the image</param>
        /// <param name="trees">Number of trees in the source decision forest</param>
        public LeafImage(int rows, int columns, int trees)
        {
            SetDimensions(rows, columns, trees);
        }

        /// <summary>
        /// Computes a label distribution at each pixel by combining its node distributions.
        /// </summary>
        /// <returns>Distribution image</returns>
        public DistributionImage ComputeDistributionImage()
        {
            DistributionImage dist = new DistributionImage(_rows, _columns, _data[0,0,0].Distribution.Length);
            dist.ID = ID;
            for (int r = 0; r < _rows; r++)
                for (int c = 0; c < _columns; c++)
                    for (int t = 0; t < _trees; t++)
                        dist.Add(r, c, _data[r, c, t].Distribution);
            dist.DivideThrough(_trees);
            dist.Normalize();
            return dist;
        }

        /// <summary>
        /// Computes a tree histogram for the source image from the nodes at each pixel.
        /// </summary>
        /// <returns>A tree histogram for the source image</returns>
        public TreeHistogram ComputeHistogram()
        {
            return ComputeHistogram(0, 0, _rows, _columns);
        }

        /// <summary>
        /// Computes a tree histogram for a sub-rectangle of the source image defined by the parameters.
        /// </summary>
        /// <param name="startRow">Starting row of the sub-rectangle</param>
        /// <param name="startColumn">Starting column of the sub-rectangle</param>
        /// <param name="rows">Number of rows in the sub-rectangle</param>
        /// <param name="columns">Number of columns in the sub-rectangle</param>
        /// <returns>A tree histogram</returns>
        public TreeHistogram ComputeHistogram(int startRow, int startColumn, int rows, int columns)
        {
            Dictionary<int, TreeNode>[] counts = new Dictionary<int, TreeNode>[_trees];
            for (int i = 0; i < counts.Length; i++)
                counts[i] = new Dictionary<int, TreeNode>();
            int[] maxIndex = new int[counts.Length];
            for (int r = 0, srcR = startRow; r < rows; r++, srcR++)
                for (int c = 0, srcC = startColumn; c < columns; c++, srcC++)
                    for (byte t = 0; t < _trees; t++)
                    {
                        INodeInfo<ImageDataPoint<T>, T[]> info = _data[srcR, srcC, t];
                        int index = info.TreeIndex;
                        if (!counts[t].ContainsKey(index)){
                            counts[t][index] = new TreeNode(t, 0, info.LeafNodeIndex, info.TreeIndex);
                            maxIndex[t] = Math.Max(maxIndex[t], index);
                        }
                        counts[t][index].Value++;
                    }
            List<TreeNode> histogram = new List<TreeNode>();
            for (byte t = 0; t < _trees; t++)
                fill(t, 1, histogram, counts[t], maxIndex[t]);
            return TreeHistogram.Divide(new TreeHistogram(histogram), rows * columns);
        }

        private static float fill(byte tree, int index, List<TreeNode> histogram, Dictionary<int, TreeNode> counts, int maxIndex)
        {
            if (index > maxIndex)
                return 0;
            if (counts.ContainsKey(index))
            {
                histogram.Add(counts[index]);
                return counts[index].Value;
            }
            else
            {
                float value = fill(tree, 2 * index, histogram, counts, maxIndex);
                value += fill(tree, 2 * index + 1, histogram, counts, maxIndex);
                if(value > 0)
                    histogram.Add(new TreeNode(tree, value, -1, index));
                return value;
            }
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
            throw new NotImplementedException();
        }

        /// <summary>
        /// Number of rows in the array.
        /// </summary>
        public int Rows
        {
            get { return _rows; }
        }

        /// <summary>
        /// Number of columns in the array.
        /// </summary>
        public int Columns
        {
            get { return _columns; }
        }

        /// <summary>
        /// Number of channels in the array.
        /// </summary>
        public int Channels
        {
            get { return _trees; }
        }

        /// <summary>
        /// Clears all data from the array.
        /// </summary>
        public void Clear()
        {
            _data = new INodeInfo<ImageDataPoint<T>, T[]>[_rows, _columns, _trees];
        }

        /// <summary>
        /// Sets the data of the array to <paramref name="data"/>.  This new array will replace the current one.  No copy is created.
        /// </summary>
        /// <param name="data">Array to handle</param>
        public void SetData(INodeInfo<ImageDataPoint<T>, T[]>[, ,] data)
        {
            _data = data;
            _rows = data.GetLength(0);
            _columns = data.GetLength(1);
            _trees = data.GetLength(2);
        }

        /// <summary>
        /// Sets the dimensions of the underlying array.  The resulting new array will replace the old array completely, no data will be copied over.
        /// </summary>
        /// <param name="rows">Number of desired rows in the new array.</param>
        /// <param name="columns">Number of desired columns in the new array.</param>
        /// <param name="channels">Number of desired channels in the new array.</param>
        public void SetDimensions(int rows, int columns, int channels)
        {
            _data = new INodeInfo<ImageDataPoint<T>, T[]>[rows, columns, channels];
            _rows = rows;
            _columns = columns;
            _trees = channels;
        }

        /// <summary>
        /// Sets whether this array is an integral array.  This property influences how the rectangle sum will be computed.
        /// </summary>
        public bool IsIntegral
        {
            get
            {
                return false;
            }
            set
            {
                throw new NotImplementedException();
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
        public INodeInfo<ImageDataPoint<T>, T[]> ComputeRectangleSum(int row, int column, int channel, Rectangle rect)
        {
            throw new NotImplementedException();
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
        public INodeInfo<ImageDataPoint<T>, T[]> ComputeRectangleSum(int startRow, int startColumn, int rows, int columns, int channel)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Extracts an entire channel from the array.
        /// </summary>
        /// <param name="channel">Channel to extract</param>
        /// <returns>Extracted channel</returns>
        public INodeInfo<ImageDataPoint<T>, T[]>[,] ExtractChannel(int channel)
        {
            INodeInfo<ImageDataPoint<T>, T[]>[,] tree = new INodeInfo<ImageDataPoint<T>, T[]>[_rows, _columns];
            for (int r = 0; r < _rows; r++)
                for (int c = 0; c < _columns; c++)
                    tree[r, c] = _data[r, c, channel];
            return tree;
        }

         /// <summary>
        /// Extracts a portion of the array defined by the parameters.
        /// </summary>
        /// <param name="startRow">Starting row</param>
        /// <param name="startColumn">Starting column</param>
        /// <param name="rows">Number of rows in the portion</param>
        /// <param name="columns">Number of columns in the portion</param>
        /// <returns>A portion of the array</returns>
        public INodeInfo<ImageDataPoint<T>, T[]>[, ,] ExtractRectangle(int startRow, int startColumn, int rows, int columns)
        {
            INodeInfo<ImageDataPoint<T>, T[]>[, ,] rectangle = new INodeInfo<ImageDataPoint<T>, T[]>[rows, columns, _trees];
            for (int r = 0, srcR = startRow; r < rows; r++, srcR++)
                for (int c = 0, srcC = startColumn; c < columns; c++, srcC++)
                    for (int t = 0; t < _trees; t++)
                        rectangle[r, c, t] = _data[srcR, srcC, t];
            return rectangle;
        }

        /// <summary>
        /// The underlying array.  Breaks capsulation to allow operations using pointer arithmetic.
        /// </summary>
        public INodeInfo<ImageDataPoint<T>, T[]>[, ,] RawArray
        {
            get { return _data; }
        }

        /// <summary>
        /// Indexes the underlying array.
        /// </summary>
        /// <param name="row">Desired row</param>
        /// <param name="column">Desired column</param>
        /// <param name="channel">Desired column</param>
        /// <returns>Value at (<paramref name="row"/>, <paramref name="column"/>, <paramref name="channel"/>) within the array.</returns>
        public INodeInfo<ImageDataPoint<T>, T[]> this[int row, int column, int channel]
        {
            get
            {
                return _data[row,column,channel];
            }
            set
            {
                _data[row,column,channel] = value;
            }
        }
    }
}
